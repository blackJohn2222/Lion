# matchmaking / 多人匹配设计（借鉴 Card）

> 日期：2026-08-19
> 状态：**已规划（待实施）**——Day 4 UI 基础做好之后再实施（便于 debug）
> 背景：调研 Card 项目（`F:\Unity_project\Card\Assets\Tcg\Scripts`）的 matchmaking 实现后定的方案；Lion 已有独立服务器（GameServer/GameClient + INetworkTransport 抽象，服务器权威，Host/Dedicated 双部署）
> 前置：Day 4 多人状态同步 + UI 基础完成

## 1. 目标形态（已确认）

- **多房间**：几个人一个房间一把游戏；服务端按 `game_uid` 懒创建房间，房间空则销毁
- **账号系统**：带账号才能匹配（注册/登录）；若需改用户名系统则同步修改
- **Elo 撮合**：匹配时按 Elo 撮合，等待越久容差越大
- **部署**：匹配服务与游戏服务**同进程同端口**（Dedicated/Host 双部署，与 Card 一致）

## 2. 待定项（只定方向，到时按需设计）

| 项 | 方向 | 待定细节 |
|---|---|---|
| 多房间管理 | 一房一局，按 game_uid 路由 | 房间容量、房主概念、房间生命周期策略 |
| 账号系统 | 服务器权威注册/登录；Elo 存服务器内存 | 是否持久化、密码方案、会话 token、显示名规则 |
| Elo 具体规则 | 等待越久容差越大（Card 式 0→2000） | 初始分、加减分、匹配窗口时长 |
| 玩家 ID 分配 | 当前 = 传输层连接 ID（身份由连接决定） | 多房间化时评估独立分配器 `_nextPlayerId++`（与连接解耦，重连保持身份） |

## 3. 借鉴 Card 的决策点（2026-08-19 逐点敲定）

| # | 决策点 | 敲定 |
|---|---|---|
| 1 | 客户端匹配会话 | ✅ 短轮询状态机：发首包 → 2s 周期 refresh → 收结果 → C# event 回调（天然解决掉线/超时） |
| 2 | 服务端撮合 | ✅ 滑动窗口队列：group + nb_players + elo（时间扩大容差），队列约 20s 清理 |
| 3 | group 前缀约定 | ✅ `""` 公共 / `u_` 好友 / `code_` 房间码（可作未来扩展） |
| 4 | 匹配结果分发 | ✅ 成功 → 生成 `game_uid` + 分配 `server_url`，玩家**凭 game_uid 加入**（匹配与房间解耦） |
| 5 | 房间生命周期 | ✅ 服务端按 game_uid 懒创建房间；对局结束 `EndMatch(uid)` 清理 |
| 6 | 匹配/游戏部署 | ✅ 同进程同端口（匹配服 = 游戏服） |
| 7 | 响应方式 | ⏸ 先按 Card：响应只发当前请求者，其他玩家靠轮询自取（最坏 2s 延迟）；若需秒级进房再改撮合成功即定向推送 |
| 8 | 端口分发 | ⏸ Lion 同进程部署 → 不分发端口（较 Card 改进点，Card 也固定端口） |

## 4. 消息协议（新增/修改）

### 新增消息（MessageType 扩展）

```
1xxx（客户端 → 服务器）：
  MatchmakingRequest = 1005   group / players / elo / refresh / time
  RegisterRequest     = 1006   username / password
  LoginRequest        = 1007   username / password
2xxx（服务器 → 客户端）：
  MatchmakingResult   = 2005   success / players(队列人数) / group / server_url / game_uid
  RegisterResponse    = 2006   success / reason / userId
  LoginResponse       = 2007   success / reason / userId / elo
```

### 修改已有消息

- `JoinRequest` 加 `game_uid` 字段（多房间加入必须带房号）→ Write/Read 变化
- `PlayerInfo` 加 `userId` / `elo` 字段（房间快照带账号信息）→ 影响 `JoinResponse` / `SnapshotMessage` 序列化

### 序列化

- `SerializeTool` 字典工厂注册 6 条新消息
- 新增序列化测试（匹配/账号 + 改过的 JoinRequest/PlayerInfo）

## 5. 服务端改动

| 改动 | 说明 |
|---|---|
| `GameServer` 语义变化 | 从"一个服务器 = 一个房间"变为"一个房间实例"；加入/断线/广播按房间收口 |
| 新增 `ServerManager` | `Dictionary<game_uid, GameServer>`，按消息里的 game_uid 路由；房间空则销毁（借鉴 Card ServerManager） |
| 新增 `Matchmaker` | 撮合队列 + group/人数/Elo（等待越久放宽容差）→ 生成 game_uid + 分配 server_url |
| 新增 `AccountManager` | 注册/登录校验 + Elo 数据（服务器权威，内存存储；持久化待定） |
| 广播改造 | 现 `BroadcastToClients` 全局广播 → 多房间下按房间内广播（遍历房间成员 SendToClient，或给 transport 加按组广播接口，待定） |

## 6. 客户端改动

| 改动 | 说明 |
|---|---|
| 新增 `MatchmakerClient` | 轮询状态机（发首包 → 2s refresh → 收结果 → C# event 回调），非 MonoBehaviour，纯逻辑可测 |
| 新增 `AccountClient` | 注册/登录请求 + 保存登录态 |
| `GameClient` | 注册匹配/账号 handler；`JoinRequest` 构造带 game_uid |

流程：登录 →（UI 发起）匹配 → 收到 `MatchmakingResult` → 带 game_uid 走现有 `JoinRequest` 加入房间。

## 7. 宿主 / 部署

- `NetworkBootstrap` 组装：ServerManager + Matchmaker + AccountManager
- Host 模式：本机即匹配服 + 游戏服，server_url = 127.0.0.1
- Dedicated 模式：独立服务器进程，server_url = 配置的公网/内网地址
- `INetworkTransport`：视广播方案而定，可能加"按组广播"能力（待定）

## 8. 测试

- 新增：Matchmaker 撮合（多客户端 FakeTransport）、账号注册/登录、多房间路由（模拟多客户端多房间）
- 修改：现有 `GameServerTests` 适配（GameServer 语义变化 + JoinRequest 带 game_uid）

## 9. UI 影响（Day 4 预留）

- Day 4 主菜单骨架 + HUD 照做；UI 结构**预留面板注册机制**（登录/匹配/房间面板作为后续 Panel 挂入）
- 具体登录/匹配/房间界面 = UI 基础做好之后再补（按既定顺序，便于 debug）

## 10. 开发顺序（实施时）

1. UI 基础（Day 4）完成
2. 消息/序列化（含测试）→ 撮合（Matchmaker + 测试）→ 账号（AccountManager/Client + 测试）→ 多房间（ServerManager + GameServer 改造 + 测试）→ 端到端 debug（Host 多实例）

## 11. 面试叙事（新增点）

- "匹配服务与游戏服务同进程、服务器权威撮合，支持 Host/Dedicated 双部署"
- "客户端短轮询 + 服务端滑动窗口队列：不用长连接也天然处理掉线/超时"
- "匹配与房间通过 game_uid 解耦：匹配器只发房号，房间按需懒创建"
- "group 前缀约定一个字段吃下公共/好友/房间码三种匹配模式"
