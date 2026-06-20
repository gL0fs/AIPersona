# Persona — AI 性格分析师

像朋友一样聊天，AI 从对话中读懂你的性格。基于大五人格（OCEAN）和 MBTI 双重分析框架。

## 它不是什么

- **不是问卷**：没有"1-5 分你给自己打几分"，没有"你觉得自己是外向还是内向"
- **不是 AI 算命**：分析基于心理学验证的量表框架，不是随意发挥

## 它是什么

你和 AI 像朋友一样在微信式界面里聊天。AI 会自然地引导话题——聊聊你的日常生活、社交习惯、做事风格、情绪感受。聊着聊着，AI 在后台分析你的回答，当你还没察觉的时候，一份性格报告已经准备好了。

---

## 核心架构：双 Agent 系统

```
┌──────────────────────────────────────────────┐
│                  用户界面                      │
│           Blazor Server (SignalR)             │
└──────────────┬───────────────────────────────┘
               │
    ┌──────────▼──────────┐
    │     QuizService     │   ← 对话调度中心
    └────┬───────────┬────┘
         │           │
    ┌────▼────┐  ┌───▼─────┐
    │对话Agent │  │评分Agent │
    │生成问题  │  │分析回答  │
    └────┬────┘  └───┬─────┘
         │           │
    ┌────▼───────────▼────┐
    │  Semantic Kernel    │  ← 统一 LLM 调用层
    │  (DeepSeek API)     │
    └────────────────────┘
         │
    ┌────▼───────────┐
    │  QuestionBank  │  ← STIPO 风格开放题库
    │ (维度 + 话题)   │
    └────────────────┘
```

### 对话 Agent

负责和用户聊天。每轮对话时：

1. 接收当前评分状态（各维度分数 + 置信度）
2. 找出置信度最低的维度作为本轮探索目标
3. 从题库中拉取该维度的参考话题（排除已经聊过的）
4. 结合对话历史，生成一个**承接上一句、自然引出新方向**的开放式问题

Prompt 设计上，明确禁止了"你觉得自己 XXX 吗"这类问卷句式，鼓励"上次 XXX 的时候你是怎么处理的"这类引导叙事的提问。

### 评分 Agent

不面向用户，只在后台运行。每 2 轮对话触发一次：

1. 读取完整对话历史
2. 对 5 个维度分别给出 1-100 的分数和 0-1 的置信度
3. 输出 JSON，被系统解析后存入 Session

评分采用**指数移动平均（EMA）**：新的评分与历史评分按 6:4 加权合并，避免单次评分的大幅波动。置信度取历史最高值——已建立的认识不会因为新一轮回答而降低信心。

### 自动结束机制

当以下条件**同时**满足时，对话自动进入收尾阶段：

- 每个维度至少被探索了 2 轮
- 全部 5 个维度的置信度 ≥ 0.70
- 已完成至少 4 轮对话

收尾时 AI 会用 1-2 句话自然告别，然后自动生成报告。

你也可以在第 2 轮之后随时点击"查看结果"手动结束。

---

## 题库设计

题库参考了两种心理学工具：

| 来源 | 作用 |
|------|------|
| **IPIP-NEO-120** | 提供 5 维度 × 6 细面 × 4 题 = 120 个量表条目作为"锚点" |
| **STIPO**（结构化人格组织访谈） | 提供开放式临床访谈的提问策略和示例 |

题库的每道题都不是直接问用户的——它们是给 AI 做**参考**用的。AI 读完题目后，理解"这个维度想探索什么特质"，然后转换成自然的日常对话问题。

例如，开放性维度下"是否喜欢尝试新鲜事物"的量表题，AI 不会直接问"你喜欢尝试新东西吗"，而是可能问"有没有什么你最近刚开始尝试的事情？是什么让你起了兴趣？"

每轮只注入当前目标维度的参考题库（已用话题会被排除），避免给 AI 太多无关信息。

---

## 评分与报告

### Big Five 评分

5 个维度分别累计评分。最终报告中的分数是对话过程中多轮评分的 EMA 结果。每个维度配有一条彩色进度条和简短解读文字。

### MBTI 推导

大五人格和 MBTI 之间有已验证的映射关系。系统根据大五分数直接推导：

| 大五维度 | MBTI 对应 | 规则 |
|---------|----------|------|
| 外向性 (E) | E / I | ≥50 → E，<50 → I |
| 开放性 (O) | N / S | ≥50 → N，<50 → S |
| 宜人性 (A) | F / T | ≥50 → F，<50 → T |
| 尽责性 (C) | J / P | ≥50 → J，<50 → P |

### AI 人格画像

最终的个性化解读文本由 AI 生成——不是模板填空，而是基于具体的分数模式写一段 150-200 字的人格画像，描述典型行为模式、优势和成长建议。明显的高分或低分维度会被重点解读。

---

## 技术栈

| 组件 | 技术 | 用途 |
|------|------|------|
| Web 框架 | ASP.NET Core 9 | 应用宿主、中间件管道 |
| 前端 | Blazor Server (InteractiveServer) | 服务端渲染 + SignalR 实时通信 |
| AI | Semantic Kernel 1.54 + DeepSeek | 对话生成、回答分析、报告撰写 |
| 数据库 | EF Core 9 + SQLite | 用户、会话、消息持久化 |
| 认证 | JWT Bearer | 注册/登录/游客鉴权 |
| 密码 | BCrypt.Net | 密码哈希 |
| 配置 | IOptions\<T\> | 强类型配置绑定 |
| 部署 | Docker (多阶段构建) | 容器化 |

**为什么不选某些技术：**

- **不用 Controller / MVC**：项目不复杂，Minimal API + Razor Components 更简洁
- **不用 SignalR Hub**：Blazor Server 自带 SignalR 电路，无需额外 Hub
- **不用 LangChain / Python**：全 .NET 技术栈，统一开发体验
- **不用 React / Vue**：Blazor Server 用 C# 写全栈，适合 .NET 课程展示

---

## 项目结构

```
Persona/
├── Program.cs                         # 服务注册、中间件、端点
├── appsettings.template.json          # 配置模板（复制为 appsettings.json 使用）
├── Dockerfile + docker-compose.yml    # Docker 部署
│
├── Models/Models.cs                   # EF 实体 + record DTO
│
├── Data/
│   ├── AppDbContext.cs                 # Entity Framework Core 上下文
│   └── QuestionBank.cs                # 题库（5 维 × 5+ 话题，含开放题示例）
│
├── Services/
│   ├── QuizService.cs                 # 对话调度 + 双 Agent + 评分逻辑
│   ├── ReportService.cs               # AI 报告生成 + Big Five→MBTI 推导
│   ├── AuthService.cs                 # JWT 注册/登录/游客
│   ├── SessionState.cs                # Blazor 电路内的会话状态
│   └── DeepSeekOptions.cs            # IOptions<T> 配置模型
│
├── Components/
│   ├── Pages/
│   │   ├── Login.razor                # 登录 / 注册 / 游客登录
│   │   ├── Home.razor                 # 首页（介绍 + 开始按钮）
│   │   ├── Quiz.razor                 # 对话测试（气泡式聊天 + 动画过渡）
│   │   ├── Report.razor               # 分析报告（MBTI 大卡 + Big Five 条 + AI 解读）
│   │   └── History.razor              # 历史测试记录
│   └── Layout/
│       ├── MainLayout.razor           # 侧边栏布局
│       └── NavMenu.razor              # 导航菜单
│
└── wwwroot/app.css                    # 全局样式（渐变主题 + 动画系统）
```

## 快速开始

### 前置条件

- .NET 9 SDK
- [DeepSeek API Key](https://platform.deepseek.com/)

### 本地运行

```bash
# 1. 配置
cp appsettings.template.json appsettings.Development.json
# 编辑 appsettings.Development.json，填入你的 API Key

# 2. 运行
dotnet run
```

访问 `http://localhost:5137`

### Docker 运行

```bash
# 编辑 appsettings.Development.json 填入 API Key
docker compose up -d --build
```

访问 `http://localhost:8080`

## License

MIT
