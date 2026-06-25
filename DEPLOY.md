# Persona 部署文档

## 前置条件

- [Docker](https://www.docker.com/) 或 .NET 9 SDK
- [DeepSeek API Key](https://platform.deepseek.com/)（注册送 500 万 token）

---

## 方式一：Docker 部署（推荐）

```bash
# 1. 创建环境变量文件
cp .env.example .env

# 2. 编辑 .env，填入你的 Key
#    DEEPSEEK_API_KEY=sk-你的key
#    JWT_SECRET_KEY=任意至少32字符的长字符串

# 3. 启动
docker compose up -d --build
```

打开 `http://localhost:8080`

> 镜像基于 `aspnet:9.0-noble-chiseled`，体积约 120MB。

---

## 方式二：dotnet publish 部署

适用于无 Docker 环境、需离线部署的场景。

### 构建

```bash
dotnet publish -c Release -o publish
```

### 部署

将 `publish/` 目录拷贝到目标服务器，然后：

**Linux / macOS：**
```bash
export Jwt__SecretKey="至少32字符的密钥"
export DeepSeek__ApiKey="sk-你的key"
export ASPNETCORE_URLS="http://+:8080"
cd publish && dotnet Persona.dll
```

**Windows（PowerShell）：**
```powershell
$env:Jwt__SecretKey = "至少32字符的密钥"
$env:DeepSeek__ApiKey = "sk-你的key"
$env:ASPNETCORE_URLS = "http://+:8080"
cd publish
dotnet Persona.dll
```

也可以在 `publish/` 目录下创建 `appsettings.Development.json`，内容参考 `appsettings.template.json`，则无需设置环境变量。

打开 `http://localhost:8080`

> 目标服务器需安装 [.NET 9 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)。

---

## 方式三：本地开发运行

```bash
cp appsettings.template.json appsettings.Development.json
# 编辑 appsettings.Development.json，填入 Jwt:SecretKey 和 DeepSeek:ApiKey
dotnet run
```

打开 `http://localhost:5137`

---

## 配置项说明

| 配置键 | 说明 | 必填 |
|--------|------|------|
| `DeepSeek:ApiKey` | DeepSeek API 密钥 | 是 |
| `DeepSeek:BaseUrl` | API 端点，默认 `https://api.deepseek.com/v1` | 否 |
| `DeepSeek:ModelName` | 模型名，默认 `deepseek-chat` | 否 |
| `Jwt:SecretKey` | JWT 签名密钥，至少 32 字符 | 是 |

> 注意：Docker 中用双下划线替代冒号，如 `DeepSeek__ApiKey`、`Jwt__SecretKey`。
