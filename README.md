# ZaoMengXiYou3.Server

《造梦西游3》Unity 客户端的配套游戏服务器，负责玩家账号（注册、登录、注销）和云存档（上传、下载、列表、删除）。

## 技术栈

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core + SQLite
- JWT Bearer 认证
- ASP.NET Core Identity `PasswordHasher`（密码只保存哈希，不保存明文）

## 目录结构

```
Controllers/   API 接口（账号、存档、服务器状态）
Data/          数据库上下文 GameDbContext
Dtos/          请求体定义与参数校验
Migrations/    EF Core 数据库迁移
Models/        数据模型（PlayerAccount、CloudSave）
Services/      JWT 令牌生成
Program.cs     服务注册与中间件配置
StartServer.bat  Windows 一键启动脚本
```

## 快速开始

### 1. 安装环境

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- EF Core 命令行工具：

```bash
dotnet tool install --global dotnet-ef
```

### 2. 配置 JWT 密钥

密钥**不写在 `appsettings.json` 里**，开发环境用 User Secrets 保存（存放在 `%APPDATA%\Microsoft\UserSecrets`，不在项目目录内，不会被提交）。

密钥要求是 Base64 字符串，解码后至少 32 字节。在 PowerShell 中生成一个随机密钥：

```powershell
$b = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
```

在项目目录下保存密钥：

```bash
dotnet user-secrets set "Jwt:Key" "<上一步生成的密钥>"
```

### 3. 创建数据库

数据库文件 `game.db` 不随仓库提交，首次运行前根据迁移生成：

```bash
dotnet ef database update
```

### 4. 启动服务器

双击 `StartServer.bat`，或在项目目录下运行：

```bash
dotnet run --launch-profile http
```

默认地址为 `http://localhost:5223`。访问 `GET /api/game/status` 返回 `"status": "online"` 即说明启动成功。

## 配置说明

| 配置项 | 位置 | 说明 |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | appsettings.json | SQLite 数据库路径，默认 `game.db` |
| `Jwt:Key` | User Secrets / 环境变量 | 签名密钥，**禁止写入仓库** |
| `Jwt:Issuer` / `Jwt:Audience` | appsettings.json | 令牌签发方与接收方 |
| `Jwt:ExpirationMinutes` | appsettings.json | 令牌有效期（分钟），默认 60 |

正式部署时用环境变量传入密钥（双下划线表示层级）：

```powershell
$env:Jwt__Key = "<密钥>"
```

## API 接口

请求和响应均为 JSON，字段名使用小驼峰。标注 🔒 的接口需要在请求头中携带登录返回的令牌：

```
Authorization: Bearer <accessToken>
```

### 服务器状态

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/game/status` | 返回在线状态和服务器时间 |

### 账号

| 方法 | 路径 | 说明 |
|---|---|---|
| POST | `/api/account/register` | 注册 |
| POST | `/api/account/login` | 登录，返回 `accessToken` |
| GET | `/api/account/me` 🔒 | 获取当前登录玩家 |
| POST | `/api/account/delete` 🔒 | 注销账号（需再次输入密码），同时删除全部云存档 |

注册 / 登录请求体：

```json
{ "username": "player_01", "password": "12345678" }
```

- 用户名：3—32 位，只能包含字母、数字、下划线；不区分大小写（统一转为小写保存）
- 密码：8—100 位

登录成功返回：

```json
{ "id": 1, "username": "player_01", "accessToken": "...", "tokenType": "Bearer" }
```

注销账号请求体：

```json
{ "password": "12345678" }
```

### 云存档 🔒

| 方法 | 路径 | 说明 |
|---|---|---|
| GET | `/api/save/list` | 当前玩家的全部存档概要 |
| POST | `/api/save/upload` | 上传存档（槽位已有存档则覆盖） |
| GET | `/api/save/download/{slotNumber}` | 下载指定槽位存档 |
| DELETE | `/api/save/delete/{slotNumber}` | 删除指定槽位存档 |

上传请求体（`dataJson` 为存档内容的 JSON 字符串，最长 100 万字符）：

```json
{ "slotNumber": 1, "dataJson": "{\"name\":\"存档1\",\"gameLevel\":5,\"rebirth\":0}" }
```

存档列表会从 `dataJson` 中读取 `name`、`gameLevel`、`rebirth` 三个字段作为概要：

```json
{
  "saves": [
    { "slotNumber": 1, "name": "存档1", "gameLevel": 5, "rebirth": 0, "updatedAtUtc": "..." }
  ]
}
```

每个玩家只能读写、删除自己账号下的存档。

### 错误码

| HTTP 状态码 | `code` | 含义 |
|---|---|---|
| 400 | — | 参数校验失败，或存档 JSON 格式错误 |
| 401 | — | 未登录或令牌已过期，需要重新登录 |
| 401 | `wrong_password` | 登录时密码错误 |
| 403 | `wrong_password` | 注销账号时密码错误 |
| 404 | `account_not_found` | 账号不存在 |
| 404 | — | 存档不存在 |
| 409 | `account_exists` | 注册时账号已存在 |

## 数据库迁移

修改 `Models/` 或 `GameDbContext` 后，新建迁移并更新数据库：

```bash
dotnet ef migrations add <迁移名称>
dotnet ef database update
```

## 安全说明

- `game.db`（账号与存档数据）、User Secrets、证书和本地配置文件已在 `.gitignore` 中排除，**不要提交**。
- 如果密钥曾经泄露，请立即更换 `Jwt:Key`；更换后所有已签发的令牌都会失效，玩家需重新登录。
