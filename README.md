# Persona — AI 性格分析师

基于大五人格模型和 MBTI 的智能性格分析工具。AI 像朋友一样和你自然对话，从聊天中分析你的性格特质。

## 功能

- **自然对话** — AI 像朋友一样聊天，不填问卷不选量表
- **双重分析** — 大五人格（OCEAN）+ MBTI 类型
- **渐进式评分** — 对话过程中 AI 内部分析每个维度的置信度，针对性追问
- **游客模式** — 无需注册即可体验
- **自动生成报告** — 聊完后 AI 生成个性化人格画像解读

## 技术栈

| 组件 | 技术 |
|------|------|
| 框架 | ASP.NET Core 9 |
| 前端 | Blazor Server |
| AI | Semantic Kernel + DeepSeek |
| 数据库 | EF Core + SQLite |
| 认证 | JWT（游客 + 注册用户） |
| 部署 | Docker |

## 快速开始

### 前置条件

- .NET 9 SDK
- DeepSeek API Key（[获取](https://platform.deepseek.com/)）

### 本地运行

```bash
# 配置 API Key
# 编辑 appsettings.Development.json，填入你的 DeepSeek API Key

dotnet run
```

访问 `http://localhost:5137`

### Docker 运行

```bash
# 编辑 appsettings.Development.json 填入 API Key
docker compose up -d --build
```

访问 `http://localhost:8080`

## 项目结构

```
Persona/
├── Program.cs                    # 启动配置、DI、JWT
├── Models/Models.cs              # 数据模型和 DTO
├── Data/
│   ├── AppDbContext.cs            # EF Core 数据库
│   └── QuestionBank.cs           # STIPO 风格对话题库
├── Services/
│   ├── AuthService.cs            # JWT 认证
│   ├── QuizService.cs            # 对话流程 + AI 评分
│   ├── ReportService.cs          # AI 报告生成
│   ├── DeepSeekOptions.cs        # 配置模型
│   └── SessionState.cs           # 会话状态
├── Components/Pages/
│   ├── Login.razor               # 登录/注册/游客
│   ├── Home.razor                # 首页
│   ├── Quiz.razor                # 对话测试页
│   ├── Report.razor              # 分析报告页
│   └── History.razor             # 历史记录
└── wwwroot/app.css               # 全局样式
```

## 题库来源

题库基于 IPIP-NEO-120 量表改编，对话引导策略参考 STIPO（结构化人格组织访谈）临床访谈方法。
