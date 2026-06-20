using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Persona.Data;
using Persona.Models;

namespace Persona.Services;

public class ReportService
{
    private readonly AppDbContext _db;
    private readonly Kernel _kernel;
    private readonly string _model;

    private const string ReportPrompt = """
        你是资深心理学家。基于以下人格评分写一份分析报告。

        大五人格：O(开放性)={O} C(尽责性)={C} E(外向性)={E} A(宜人性)={A} N(神经质)={N}
        MBTI：{MBTI}（{MBTI_DESC}）

        写 150-200 字人格画像：典型行为模式、优势、成长建议。重点解读明显的高分/低分维度。
        语言温暖专业，纯文本。
        """;

    public ReportService(AppDbContext db, IOptions<DeepSeekOptions> options)
    {
        _db = db;
        _model = options.Value.ModelName;
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(modelId: _model, apiKey: options.Value.ApiKey,
            endpoint: new Uri(options.Value.BaseUrl));
        _kernel = builder.Build();
    }

    public async Task<ReportResponse> GenerateAsync(int sessionId)
    {
        var session = await _db.Sessions.FindAsync(sessionId)
            ?? throw new InvalidOperationException("Session not found");

        DimensionScores scores;
        if (!string.IsNullOrEmpty(session.ScoresJson))
        {
            scores = JsonSerializer.Deserialize<DimensionScores>(session.ScoresJson)!;
        }
        else
        {
            // Fallback: approximate from available conversations
            scores = new DimensionScores(50, 0.3, 50, 0.3, 50, 0.3, 50, 0.3, 50, 0.3);
        }

        var mbti = ToMbti(scores);
        var mbtiDesc = MbtiDesc(mbti);

        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var prompt = ReportPrompt
            .Replace("{O}", scores.O.ToString()).Replace("{C}", scores.C.ToString())
            .Replace("{E}", scores.E.ToString()).Replace("{A}", scores.A.ToString())
            .Replace("{N}", scores.N.ToString()).Replace("{MBTI}", mbti)
            .Replace("{MBTI_DESC}", mbtiDesc);

        var history = new ChatHistory(prompt);
        history.AddMessage(AuthorRole.User, "写报告。");
        var result = await chat.GetChatMessageContentAsync(history);

        var report = new ReportResponse(
            scores.O, scores.C, scores.E, scores.A, scores.N,
            result.Content ?? "生成失败", mbti);

        session.Completed = true;
        session.ReportJson = JsonSerializer.Serialize(report);
        await _db.SaveChangesAsync();

        Console.WriteLine($"[Report] MBTI={mbti}");
        return report;
    }

    private static string ToMbti(DimensionScores s)
        => $"{(s.E >= 50 ? 'E' : 'I')}{(s.O >= 50 ? 'N' : 'S')}" +
           $"{(s.A >= 50 ? 'F' : 'T')}{(s.C >= 50 ? 'J' : 'P')}";

    private static string MbtiDesc(string t) => t switch
    {
        "INTJ" => "建筑师——独立、战略性的思考者", "INTP" => "逻辑学家——创新、好奇的分析者",
        "ENTJ" => "指挥官——果断、有远见的领导者", "ENTP" => "辩论家——机智、有创造力的思想者",
        "INFJ" => "提倡者——安静而富有远见的理想家", "INFP" => "调停者——忠于价值观的理想主义者",
        "ENFJ" => "主人公——富有魅力的激励者", "ENFP" => "竞选者——热情、爱社交的自由精神",
        "ISTJ" => "物流师——务实、可靠、有条不紊", "ISFJ" => "守卫者——细心、有奉献精神的保护者",
        "ESTJ" => "总经理——高效、有组织的管理者", "ESFJ" => "执行官——热心、有责任感的维护者",
        "ISTP" => "鉴赏家——务实、灵活的动手者", "ISFP" => "探险家——温和、敏感的艺术家",
        "ESTP" => "企业家——精力充沛的冒险者", "ESFP" => "表演者——自发性强、热爱生活的娱乐者",
        _ => "独特的混合型人格"
    };
}
