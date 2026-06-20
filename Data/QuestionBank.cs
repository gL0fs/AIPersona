namespace Persona.Data;

public record ConversationTopic(string Topic, string[] ExampleQuestions);

public record DimensionGroup(string Name, List<ConversationTopic> Topics);

public static class QuestionBank
{
    public static readonly Dictionary<string, DimensionGroup> Dimensions = new()
    {
        ["E"] = new("外向性", new()
        {
            new("社交场合的表现", new[]{
                "聊聊最近一次参加聚会或者社交活动的经历吧，那天你是什么状态？",
                "一群人相处的时候，你一般扮演什么角色——带气氛的那个，还是默默观察的那个？能举个例子吗？",
                "有没有哪次社交活动让你觉得特别自在或者特别不自在？发生了什么？",
            }),
            new("独处时光", new[]{
                "一个人待着的时候，你会做什么？那种状态让你舒服吗？",
                "你觉得自己需要多少独处时间？有没有因为独处不够而感到烦躁的经历？",
            }),
            new("认识新的人", new[]{
                "最近一次认识新朋友是什么时候，怎么认识的？",
                "到一个陌生的社交场合，你是会主动找人搭话，还是等别人来找你？",
                "有没有过第一印象完全错误的人？后来怎么发现的？",
            }),
            new("精力来源", new[]{
                "忙完一天之后，什么方式最能让你恢复精力？",
                "和别人待久了之后，你是觉得充了电还是耗了电？",
            }),
        }),
        ["A"] = new("宜人性", new()
        {
            new("处理分歧", new[]{
                "有没有和亲近的人意见不合的经历？当时你是怎么做的，后来怎么样了？",
                "如果团队里有个人一直说你不认同的话，你会怎么做？能举个真实的例子吗？",
            }),
            new("信任他人", new[]{
                "你觉得一个人值不值得信任，你一般靠什么来判断？",
                "你有没有被人辜负过信任的经历？那件事之后你变了什么？",
            }),
            new("帮助别人", new[]{
                "陌生人向你求助的时候，你心里是什么感受？有没有印象深刻的一次经历？",
                "朋友遇到困难，你一般会怎么帮忙？说一个最近的例子。",
            }),
            new("竞争心态", new[]{
                "你参加过什么比赛或者竞争吗？当时的体验怎么样？",
                "在一个团队里如果其他人都在比来比去，你一般怎么做？",
            }),
        }),
        ["C"] = new("尽责性", new()
        {
            new("做事方式", new[]{
                "接到一个新任务的时候，你一般怎么开始？说一个最近的事。",
                "你的桌子或者工作环境现在是什么样？你觉得舒服吗？",
            }),
            new("坚持与放弃", new[]{
                "你有没有一个坚持了很久的习惯或者事情？是怎么坚持下来的？",
                "有没有一个你放弃了但后来觉得可惜的目标？当时为什么放弃了？",
            }),
            new("时间管理", new[]{
                "你一般怎么安排自己的一天？有计划和没计划的日子差别大吗？",
                "快到截止日期的时候，你一般是什么状态？有没有拖延到最后一刻的经历？",
            }),
            new("对自己的要求", new[]{
                "你觉得自己是个要求高的人吗？能举一个最近的例子吗？",
                "做完一件事之后，你一般会回头检查还是觉得差不多就行？",
            }),
        }),
        ["N"] = new("情绪稳定性", new()
        {
            new("情绪起伏", new[]{
                "最近一次情绪波动比较大是什么时候？当时发生了什么？",
                "你一般用了多长时间才恢复平静？做了什么帮到自己？",
            }),
            new("担忧与焦虑", new[]{
                "有没有什么事情，你明知道不太可能发生但还是忍不住担心？",
                "面对一个不确定的结果的时候，你脑子里一般会想什么？",
            }),
            new("看待自己", new[]{
                "如果用三个词形容自己，你会选什么？为什么是这三个？",
                "你觉得自己身上最想改变的一点是什么？它对你的生活影响大吗？",
            }),
            new("应对困难", new[]{
                "你有没有经历过一段特别难熬的时期？你是怎么走过来的？",
                "遇到困难的时候你会找别人聊，还是自己扛？",
            }),
        }),
        ["O"] = new("开放性", new()
        {
            new("探索新奇", new[]{
                "有没有什么你最近才开始尝试的事情？是什么让你起了兴趣？",
                "对于完全陌生的东西，你一般是什么态度——先试试还是先观望？举个具体例子。",
            }),
            new("艺术与美感", new[]{
                "有没有某个电影、音乐或者艺术作品让你特别有感触？它打动你的是什么？",
                "你觉得自己是个对美有要求的人吗？体现在哪些地方？",
            }),
            new("思考方式", new[]{
                "你有没有过因为读了一本书或者和人聊了一次天而完全改变了某个想法的经历？",
                "遇到一个复杂问题的时候，你一般是怎么思考的？喜欢从哪里入手？",
            }),
            new("创造力", new[]{
                "你有没有自己动手做过什么东西？不管是写东西、做手工、做菜、还是别的什么。",
                "遇到问题的时候，你喜欢用以前管用的老办法，还是想想有没有新思路？",
            }),
        }),
    };

    public static readonly ConversationTopic WarmupTopics = new("暖身", new[]
    {
        "你平时是做什么的呀？最近工作上或者学习上有没有什么有意思的事？",
        "最近有没有在学什么新东西、或者说迷上了什么事情？",
        "今天过得怎么样？有没有发生什么特别的事？",
    });

    public static string FormatTopics(string dimension, HashSet<string>? usedTopics = null)
    {
        if (!Dimensions.TryGetValue(dimension, out var group)) return "";
        var topics = group.Topics;
        if (usedTopics != null)
            topics = topics.Where(t => !usedTopics.Contains(t.Topic)).ToList();
        if (topics.Count == 0) topics = group.Topics;

        return string.Join("\n", topics.Select(t =>
            $"- {t.Topic}（例如：{t.ExampleQuestions.First()}）"));
    }

    public static string WarmupQuestionExamples =>
        string.Join("\n", WarmupTopics.ExampleQuestions.Select(q => $"- 「{q}」"));
}
