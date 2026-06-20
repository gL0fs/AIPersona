using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Persona.Data;
using Persona.Models;

namespace Persona.Services;

public class QuizService
{
    private readonly AppDbContext _db;
    private readonly Kernel _kernel;
    private readonly string _model;

    private const string WarmupPrompt = """
        你正在和一个新认识的朋友在微信上聊天。这是你说的第一句话。

        你的目标是让ta愿意多说点——所以提的问题要具体、有代入感、让人有话可聊。
        不要问太虚的（比如"你喜欢什么"），问有具体场景的（比如"最近在忙什么有意思的事"）。

        举个例子，这些是比较容易引出回答的开场：
        {TOPICS}

        要求：就发一句话，自然随意，像一个真正好奇的朋友，不自报家门也不寒暄。
        """;

    private const string QuestionPrompt = """
        你正在和一个朋友在微信上聊天，想通过自然的对话来了解ta是个什么样的人。
        你不是在做测试，不是在做访谈，不是在填问卷——就是普通朋友之间的聊天。

        （内部参考，绝对不要对用户提及）：当前评分 {SCORES}，下一轮侧重「{DIM}」

        下面是一些你还没聊过的话题方向，仅供参考：
        {ANGLES}

        【聊天记录】
        {HISTORY}

        请继续聊天。注意：
        - 先顺着ta上一句话自然地回应一下，做个正常人该有的反应（共情、共鸣、或者追问一个细节）
        - 然后很自然地聊到你还需要了解的方向
        - 整体就是朋友微信聊天的感觉，两句话就够了

        绝对禁止的提问方式：
        - "你觉得你是一个XXX的人吗？"（这是问卷不是聊天）
        - "你会XXX还是YYY？"（二选一等于没问）
        - "你一般会怎么做？"后面没有具体场景（太抽象）
        - 连续两轮问同一个维度的问题
        - 任何像面试官、心理咨询师、问卷调查的腔调

        正确的提问方式：
        - "上次XXX的时候，你当时..."（带入具体场景）
        - "有没有过XXX的经历？当时你是怎么..."（让ta讲故事）
        - "说到XXX，我突然想到..."（用联想自然过渡）
        """;

    private const string ScoringPrompt = """
        根据对话内容，定量分析受试者的大五人格维度。

        - O (开放性)：想象力、好奇心、审美、喜欢新事物
        - C (尽责性)：自律、条理、成就动机、细节
        - E (外向性)：社交、活力、刺激寻求、积极情绪
        - A (宜人性)：信任、共情、合作、利他
        - N (神经质)：情绪波动、焦虑（低分=情绪稳定）

        每个维度 1-100，置信度 0-1。
        只输出 JSON：{"O":分,"O_conf":信,"C":分,"C_conf":信,"E":分,"E_conf":信,"A":分,"A_conf":信,"N":分,"N_conf":信}

        对话：
        {HISTORY}
        """;

    private const string ConcludePrompt = """
        你是专业的性格分析师。根据对话，你已经收集了足够信息。
        请用 1-2 句话自然地结束对话：感谢对方真诚的分享，
        告诉对方分析已完成。语气自然随意，不要像写感谢信。不要提问。
        """;

    private const int MinRoundsPerDim = 2;
    private const int MinTotalRounds = 4;
    private const double ConfidenceThreshold = 0.70;

    private static readonly string[] DimOrder = ["E", "A", "C", "N", "O"];

    public QuizService(AppDbContext db, IOptions<DeepSeekOptions> options)
    {
        _db = db;
        _model = options.Value.ModelName;
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(modelId: _model, apiKey: options.Value.ApiKey,
            endpoint: new Uri(options.Value.BaseUrl));
        _kernel = builder.Build();
    }

    public async Task StartConversationAsync(int sessionId)
    {
        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var prompt = WarmupPrompt.Replace("{TOPICS}", QuestionBank.WarmupQuestionExamples);
        var history = new ChatHistory(prompt);
        history.AddMessage(AuthorRole.User, "开始吧。");
        var content = "";
        await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(history))
            content += chunk.Content ?? "";
        _db.Messages.Add(new QuizMessage { SessionId = sessionId, Role = "assistant", Content = content });
        await _db.SaveChangesAsync();
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(int sessionId, string userMessage)
    {
        var session = await _db.Sessions.FindAsync(sessionId)
            ?? throw new InvalidOperationException("Session not found");

        _db.Messages.Add(new QuizMessage { SessionId = sessionId, Role = "user", Content = userMessage });
        await _db.SaveChangesAsync();

        // Score after user responds (don't score after warmup)
        var userCount = session.Messages.Count(m => m.Role == "user");
        if (userCount >= 2)
        {
            await RunScoringAsync(sessionId);
            await _db.Entry(session).ReloadAsync();
        }

        var chat = _kernel.GetRequiredService<IChatCompletionService>();

        // Check if we should auto-conclude
        if (ShouldConclude(session))
        {
            var concludeHistory = new ChatHistory(ConcludePrompt);
            var msgs = session.Messages.OrderBy(m => m.Timestamp).ToList();
            foreach (var m in msgs)
                concludeHistory.AddMessage(m.Role == "user" ? AuthorRole.User : AuthorRole.Assistant, m.Content);

            var content = "";
            await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(concludeHistory))
            {
                content += chunk.Content ?? "";
                yield return chunk.Content ?? "";
            }
            _db.Messages.Add(new QuizMessage { SessionId = sessionId, Role = "assistant", Content = content });
            await _db.SaveChangesAsync();
            yield break;
        }

        // Normal: generate next question
        var history = await BuildQuestionHistory(session);
        var assistantContent = "";
        await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(history))
        {
            assistantContent += chunk.Content ?? "";
            yield return chunk.Content ?? "";
        }

        _db.Messages.Add(new QuizMessage { SessionId = sessionId, Role = "assistant", Content = assistantContent });
        await _db.SaveChangesAsync();

        // Track used angle for this dimension
        if (!string.IsNullOrEmpty(session.ScoresJson))
        {
            var scores = JsonSerializer.Deserialize<DimensionScores>(session.ScoresJson)!;
            var dim = FindLowestConfidence(scores);
            TrackUsedAngle(session, dim);
            await _db.SaveChangesAsync();
        }
    }

    private bool ShouldConclude(QuizSession session)
    {
        if (string.IsNullOrEmpty(session.ScoresJson)) return false;

        var scores = JsonSerializer.Deserialize<DimensionScores>(session.ScoresJson)!;
        var minConf = new[] { scores.OConf, scores.CConf, scores.EConf, scores.AConf, scores.NConf }.Min();
        if (minConf < ConfidenceThreshold) return false;

        var userRounds = session.Messages.Count(m => m.Role == "user");
        if (userRounds < MinTotalRounds) return false;

        // Check each dim has enough coverage
        var counts = GetDimCounts(session);
        foreach (var dim in DimOrder)
            if (counts.GetValueOrDefault(dim, 0) < MinRoundsPerDim)
                return false;

        return true;
    }

    public int GetProgress(QuizSession session)
    {
        if (string.IsNullOrEmpty(session.ScoresJson)) return 0;

        var scores = JsonSerializer.Deserialize<DimensionScores>(session.ScoresJson)!;
        var confidences = new[] { scores.OConf, scores.CConf, scores.EConf, scores.AConf, scores.NConf };
        var avgConf = confidences.Average();

        // Also factor in dim coverage
        var counts = GetDimCounts(session);
        var coverageProgress = counts.Values.Sum() / (double)(DimOrder.Length * MinRoundsPerDim);
        coverageProgress = Math.Min(coverageProgress, 1.0);

        // 60% confidence + 40% coverage
        return (int)((avgConf * 0.6 + coverageProgress * 0.4) * 100);
    }

    private async Task<ChatHistory> BuildQuestionHistory(QuizSession session)
    {
        var messages = session.Messages.OrderBy(m => m.Timestamp).ToList();

        DimensionScores? scores = null;
        if (!string.IsNullOrEmpty(session.ScoresJson))
            scores = JsonSerializer.Deserialize<DimensionScores>(session.ScoresJson);

        string scoresText = "尚未评分";
        string targetDim = "E";
        if (scores != null)
        {
            scoresText = $"O={scores.O}(信{scores.OConf:F2}) C={scores.C}(信{scores.CConf:F2}) "
                       + $"E={scores.E}(信{scores.EConf:F2}) A={scores.A}(信{scores.AConf:F2}) N={scores.N}(信{scores.NConf:F2})";

            // Pick dim: lowest confidence, but prefer dims with fewer questions
            var counts = GetDimCounts(session);
            var ordered = DimOrder.OrderBy(d => counts.GetValueOrDefault(d, 0))
                                  .ThenBy(d => GetConfidenceForDim(scores, d));
            targetDim = ordered.First();
        }

        var dimName = QuestionBank.Dimensions.TryGetValue(targetDim, out var dg) ? dg.Name : targetDim;
        var usedTopics = GetUsedTopicsForDim(session, targetDim);
        var angles = QuestionBank.FormatTopics(targetDim, usedTopics);

        var dialogText = string.Join("\n",
            messages.Select(m => $"{(m.Role == "user" ? "用户" : "分析师")}: {m.Content}"));

        var prompt = QuestionPrompt
            .Replace("{SCORES}", scoresText)
            .Replace("{DIM}", dimName)
            .Replace("{ANGLES}", angles)
            .Replace("{HISTORY}", dialogText);

        return new ChatHistory(prompt);
    }

    private async Task RunScoringAsync(int sessionId)
    {
        var session = await _db.Sessions.FindAsync(sessionId);
        if (session == null) return;

        var newScores = await ScoreAsync(session);
        if (newScores == null) return;

        if (!string.IsNullOrEmpty(session.ScoresJson))
        {
            var prev = JsonSerializer.Deserialize<DimensionScores>(session.ScoresJson)!;
            double a = 0.6;
            newScores = new DimensionScores(
                (int)(a * newScores.O + (1 - a) * prev.O), Math.Max(newScores.OConf, prev.OConf),
                (int)(a * newScores.C + (1 - a) * prev.C), Math.Max(newScores.CConf, prev.CConf),
                (int)(a * newScores.E + (1 - a) * prev.E), Math.Max(newScores.EConf, prev.EConf),
                (int)(a * newScores.A + (1 - a) * prev.A), Math.Max(newScores.AConf, prev.AConf),
                (int)(a * newScores.N + (1 - a) * prev.N), Math.Max(newScores.NConf, prev.NConf));
        }

        session.ScoresJson = JsonSerializer.Serialize(newScores);
        session.ScoreRounds++;
        await _db.SaveChangesAsync();

        var minC = new[] { newScores.OConf, newScores.CConf, newScores.EConf, newScores.AConf, newScores.NConf }.Min();
        Console.WriteLine($"[Score #{session.ScoreRounds}] minConf={minC:F2} O={newScores.O}({newScores.OConf:F2}) C={newScores.C}({newScores.CConf:F2}) E={newScores.E}({newScores.EConf:F2}) A={newScores.A}({newScores.AConf:F2}) N={newScores.N}({newScores.NConf:F2})");
    }

    private async Task<DimensionScores?> ScoreAsync(QuizSession session)
    {
        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var messages = session.Messages.OrderBy(m => m.Timestamp).ToList();
        var dialog = string.Join("\n",
            messages.Select(m => $"{(m.Role == "user" ? "用户" : "分析师")}: {m.Content}"));

        var prompt = ScoringPrompt.Replace("{HISTORY}", dialog);
        var history = new ChatHistory(prompt);
        history.AddMessage(AuthorRole.User, "请评分。");

        try
        {
            var result = await chat.GetChatMessageContentAsync(history);
            var json = result.Content!.Trim().Replace("```json", "").Replace("```", "").Trim();
            return JsonSerializer.Deserialize<DimensionScores>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Scoring] Failed: {ex.Message}");
            return null;
        }
    }

    // --- Topic usage tracking ---
    private void TrackUsedAngle(QuizSession session, string dim)
    {
        var dict = GetUsedAnglesDict(session);
        if (!dict.TryGetValue(dim, out var list))
            dict[dim] = list = new();
        list.Add($"t{list.Count + 1}");
        session.UsedAnglesJson = JsonSerializer.Serialize(dict);
    }

    private Dictionary<string, List<string>> GetUsedAnglesDict(QuizSession session)
    {
        if (string.IsNullOrEmpty(session.UsedAnglesJson))
            return new();
        return JsonSerializer.Deserialize<Dictionary<string, List<string>>>(session.UsedAnglesJson) ?? new();
    }

    private Dictionary<string, int> GetDimCounts(QuizSession session)
        => GetUsedAnglesDict(session).ToDictionary(kv => kv.Key, kv => kv.Value.Count);

    private HashSet<string> GetUsedTopicsForDim(QuizSession session, string dim)
    {
        var dict = GetUsedAnglesDict(session);
        return dict.TryGetValue(dim, out var list) ? new HashSet<string>(list) : new();
    }

    private static double GetConfidenceForDim(DimensionScores s, string dim) => dim switch
    {
        "O" => s.OConf, "C" => s.CConf, "E" => s.EConf, "A" => s.AConf, "N" => s.NConf,
        _ => 1.0
    };

    private static string FindLowestConfidence(DimensionScores s)
    {
        var pairs = new (string, double)[] { ("O", s.OConf), ("C", s.CConf), ("E", s.EConf), ("A", s.AConf), ("N", s.NConf) };
        return pairs.OrderBy(p => p.Item2).First().Item1;
    }
}
