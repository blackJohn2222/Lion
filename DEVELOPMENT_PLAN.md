# Lion — Unity 求职作品集开发计划

> 本文档用于跨对话交接：切换对话时，让新的 AI 先完整阅读本文件，再继续工作。
> 最近更新：2026-08-10（第一周尚未开工）

---

## 1. 项目定位

- **类型**：Unity 6 + URP 客户端游戏开发求职作品（**非引擎岗**）
- **时间预算**：约一个多月 → 定位"垂直切片"：一个核心玩法循环 + 精致打磨，**完成度优先**
- **宗旨**：激发玩家创造力和好奇心
- **玩法未定**：候选方向 A 建造探索 / B 合作解谜 / C 混合。**第一周不做玩法决定，只搭通用地基；第二周定玩法**

## 2. 技术栈展示点（面试卖点）

网络、游戏架构、状态机、必要时 DOTS、可涉及 UI。

## 3. 架构决策（已确认）

- **程序集划分（Assembly Definition）**：
  - `Game.Core` — 纯逻辑：状态机、事件总线、轻量手写 DI 容器（ServiceLocator 或简单构造注入，**不引第三方库**）、可复用数据类
  - `Game.Network` — 网络消息层、连接管理（依赖 Transport）
  - `Game.Gameplay` — 角色控制、世界交互（依赖 Core + Network）
  - `Game.UI` — 主菜单、HUD、调试面板
- **事件总线**：手写 `EventBus<T>`，系统间解耦
- **全局场景状态机**：Bootstrap → MainMenu → Connecting → InGame → Paused → Disconnected

## 4. 网络层设计（本周主攻，第 2 节）

### 抽象接口 `INetworkTransport`
```
bool IsServer { get; }
bool IsConnected { get; }
void StartServer(int port);
void ConnectToServer(string ip, int port);
void Disconnect();
void SendToServer(IMessage msg);
void BroadcastToClients(IMessage msg);
void SendToClient(ulong clientId, IMessage msg);
event Action OnConnected;
event Action OnDisconnected;
event Action<IMessage> OnMessageReceived;
```

### 实现
- **`CustomNetAdapter`（做实）**：基于 **Unity Transport**。消息协议（IMessage 基类 + 消息 ID 注册表 + 序列化）；可靠/不可靠通道（位置同步走不可靠，连接/事件走可靠）；心跳与断线检测；**服务器权威模型**；连接管理 + 玩家 ID 分配
- **`NgoAdapter`（本周只写空壳骨架）**：方法 NotImplementedException 或最小实现，验证切换机制可行

### 本周消息类型
`JoinRequest/JoinResponse`、`PlayerStateSync`（高频不可靠）、`ChatMessage`（可靠）、`DisconnectNotice`

### 连接流程状态机
Idle → Connecting → Connected → Disconnecting，并入全局状态机

### 调试面板
连接状态、RTT、收发消息计数

## 5. 第三人称角色控制器（第 3 节）

- 移动：CharacterController + WASD + 空格跳跃 + 重力
- 相机：第三人称跟随相机（环绕 + 缩放 + 平滑阻尼），默认跟随视角
- 手感调校：加速度/减速度、地面摩擦、跳跃力度、坡度限制
- 角色状态机：手写通用 `StateMachine<T>` 基类（Enter/Update/Exit + 条件转移表），状态类 `IdleState/WalkState/JumpState/FallState`，每类职责单一；**可复用到 AI、连接流程**
- 输入层抽象：新输入系统（Input System Package）+ 轻量 `InputReader` 包装
- **双人可见性**：两个客户端各控一角色，服务器转发 PlayerStateSync，能看到对方走动

## 6. UI + 环境 + 开发配套（第 4 节）

- **UI 分层策略**：主菜单用 **UI Toolkit**（UXML + USS）；HUD/调试面板用 **uGUI** 快速搭（第二周再统一迁 UI Toolkit）
- 环境：免费低多边形包或 URP 示例场景搭测试场地（地面、障碍物、高处平台）；重点调好 URP 光照/天空盒，让截图录屏好看
- 日志系统：统一 GameLog，分级 + 前缀过滤（网络日志/状态机日志分开）
- 调试快捷键：F1 连接详情、F2 模拟断线
- **Git**：初始化 git，规范提交
- 多实例联调：一个工程开两个实例直接局域网测

## 7. 第一周日程

| 天 | 内容 | 产出 |
|---|---|---|
| Day 1 | 工程初始化 + 程序集分层 + DI + 事件总线 + Git | 架构骨架 |
| Day 2 | 网络抽象接口 + 自研 CustomNetAdapter（协议/心跳/通道） | 能 Host/Join + 收发光消息 |
| Day 3 | 第三人称控制器 + 相机 + 输入层 + 角色状态机 | 单人跑跳手感 OK |
| Day 4 | 玩家状态同步打通 + UI + 环境 | 双人可见 + 可演示 |
| Day 5 | 手感调优 + 多实例联调 + 日志/快捷键 + 缓冲 | 第一周成果稳定 |

Day 5 有余力可提前试玩法原型（摆方块试放置/交互）——加分项，非必须。

## 8. 已确认技术决策汇总

- Unity 6 + URP
- 网络层：可切换适配器 `INetworkTransport`（CustomNetAdapter 做实，NgoAdapter 留空壳）
- 角色：CharacterController + 手写通用 FSM
- UI：主菜单 UI Toolkit，HUD/调试 uGUI
- 架构：程序集分层 + 手写 DI + 事件总线
- 状态机：`StateMachine<T>` 基类，复用到角色/连接流程/AI

## 9. 面试叙事

原理（手写网络层学透底层）+ 框架（NGO 适配器口子）两手复习；适配器模式体现架构能力；垂直切片展示完成度。

---

## 10. 当前工程状态（2026-08-10 查）

- Unity `6000.3.11f1` + URP `17.3.0`
- 已装：Input System `1.19.0`、uGUI `2.0.0`、Test Framework `1.6.0`、AI Navigation `2.0.11`
- UI Toolkit 可用（uielements 模块内置）
- **未装**：`com.unity.transport`（CustomNetAdapter 依赖，Day 2 前必须装）
- **未装**：NGO（本周只要空壳口子，接口即可验证，可不装）
- **git 未初始化**
- Assets 只有 URP 模板默认内容 + 空 SampleScene，无自定义代码
- 已存在 `DEVELOPMENT_PLAN.md`（本文件）

## 11. 下一步行动

1. **第 0 步（零代码环境准备）**：安装 `com.unity.transport`；初始化 git + `.gitignore`（Unity 标准忽略）
2. 按 Day 1 开工：程序集分层 + DI + 事件总线 + 全局场景状态机（Game.Core，纯逻辑，可先写单元测试）

## 12. 用户协作习惯（重要，新对话必须遵守）

- **语言**：默认中文交流
- **图片**：用户图片在**剪贴板**里，用 `vision-web-bridge` MCP 的 `read_image_with_model` 读取（`use_clipboard=true`、`use_latest_upload=false`），图片本身不能直接粘贴给模型
- 用户发来设计/图片时，先确认理解，**不要急着写代码**，等用户明确指示再动手
