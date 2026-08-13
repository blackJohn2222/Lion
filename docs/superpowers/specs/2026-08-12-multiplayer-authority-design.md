# 网络层改造设计：多人联机 + 服务器权威 + Host 模式

> 日期：2026-08-12
> 状态：已确认（七节逐节过审）
> 背景：原设计为双人（1 host + 1 client）简单转发模型，现升级为多人服务器权威模型。

---

## 1. 需求确认

- **多人规模**：4-8 人（协议按此设计，可扩展）
- **权威程度**：全量权威——连接、加入/离开、消息广播由服务器裁决；客户端只上报与表现
- **部署形态**：独立服务器（Dedicated）+ Host 模式两种都要
  - 独立服务器：GameServer 模块在独立进程/机器运行（headless，不碰渲染）
  - Host 模式：同一进程跑 GameServer + GameClient，客户端逻辑连 127.0.0.1 本机回环
- **开发验证**：先以 Host 模式验证（独立服务器打包放 Day 5 收尾）

## 2. 总体架构

```
[应用层] GameServer 模块（纯逻辑，不依赖渲染/场景，两种部署形态共用一份代码）
           ├── 房间/玩家管理（加入/离开/ID 分配）
           ├── 权威裁决（校验 SenderId、广播状态）
           └── 部署：独立进程 或 嵌入 Host

[传输层] INetworkTransport（补事件后的接口）
           ├── CustomNetAdapter（Unity Transport 实现）
           └── NgoAdapter（空壳）

[应用层] GameClient 模块
           ├── 本地玩家输入上报（自动填 SenderId）
           ├── 接收服务器状态更新
           └── Host 模式下连 127.0.0.1
```

### 关键设计原则

1. GameServer 是**纯逻辑模块**——不依赖渲染、不依赖场景，同一份代码两种部署形态共用
2. Host = 同进程跑 GameServer + GameClient，客户端逻辑走本机回环（`ConnectToServer("127.0.0.1", port)`），代码路径与真实客户端完全一致，无特殊分支
3. 传输层只管收发，不知道"权威"存在
4. Host 玩家的数据同样走"客户端→GameServer→广播"完整路径（保证所有人同一套权威流程）

## 3. 接口补丁（对 INetworkTransport 的唯一改动）

现有成员全部保留，新增两个服务器侧事件：

```csharp
event Action<ulong> OnClientConnected;     // 服务器：有客户端连入（参数 = 分配的玩家 ID）
event Action<ulong> OnClientDisconnected;  // 服务器：有客户端断开
```

**IMessage 升级**：

```csharp
public interface IMessage
{
    ulong SenderId { get; set; }   // 所有消息自带发送者
    MessageType Type { get; }      // 消息类自报 ID（字典工厂写 ID 用）
    void Write(DataStreamWriter writer);   // 分布式：消息类自己写字段
    void Read(DataStreamReader reader);    // 分布式：消息类自己读字段
}
```

说明：Host 模式不需要新接口方法——Host 玩家也是 `ConnectToServer("127.0.0.1", port)` 连自己。接口保持纯传输职责，服务器/Host 的区别是应用层组装方式。

## 4. 消息协议（多人化 + Card 借鉴）

### 借鉴决策汇总（2026-08-12 用户确认）

| # | 借鉴点 | 决策 |
|---|---|---|
| 1 | ID 分段（1xxx 客户端指令 / 2xxx 服务器通知） | ✅ 学 |
| 2 | 消息类带 Type 属性 | ✅ 保留（无 NGO，无法学 Card 的注册绑定） |
| 3 | 共享协议常量文件 | ✅ 天然如此（Network 程序集两端共用） |
| 4 | 服务器强制覆盖身份 | ✅ 学——统一入口一行覆盖所有消息 |
| 5 | 连接审批 | ❌ 不学（JoinRequest 校验够用，YAGNI） |
| 6 | 指令排队 | ❌ 不学（YAGNI，玩法复杂后再加） |
| 7 | 双注册表对称分发 | ✅ 学——分发也用字典注册表 |
| 8 | 离线模式 | ❌ 不学（YAGNI，测试用 FakeTransport 替代） |
| 9 | 网络帧 tick | ❌ 不学（YAGNI，每帧 Pump 够用） |

### ID 分段（借鉴 GameAction.cs 风格）

```
1xxx = 客户端 → 服务器的指令
2xxx = 服务器 → 客户端的通知
```

| ID | 消息 | 方向 |
|---|---|---|
| 1001 | JoinRequest | 客户端→服务器 |
| 1002 | JoinResponse | 服务器→客户端 |
| 1003 | PlayerStateSync | 双向 |
| 1004 | ChatMessage | 双向 |
| 1005 | DisconnectNotice | 双向 |
| 2001 | PlayerJoinedNotice | 服务器→所有客户端 |
| 2002 | PlayerLeftNotice | 服务器→所有客户端 |
| 2003 | SnapshotMessage | 服务器→客户端 |

### 序列化（字典工厂，借鉴 Card 注册表模式）

```csharp
public static class Serializer
{
    // ID → 创建对应消息类型空对象（字典工厂，替代 switch）
    private static readonly Dictionary<MessageType, Func<IMessage>> _factories = new()
    {
        [MessageType.JoinRequest]       = () => new JoinRequest(),
        // ... 8 个
    };

    public static byte[] Serialize(IMessage msg)
    {
        var writer = new DataStreamWriter(64, Allocator.Temp);
        writer.WriteByte((byte)msg.Type);   // 写 ID：消息类自报
        msg.Write(writer);                  // 字段：消息类自己写
        return writer.AsNativeArray().ToArray();
    }

    public static IMessage Deserialize(byte[] data)
    {
        var reader = new DataStreamReader(data);
        var id = (MessageType)reader.ReadByte();
        if (!_factories.TryGetValue(id, out var factory))
            throw new InvalidOperationException($"未知消息类型: {id}");
        var msg = factory();                // 查表创建（类型由 ID 确定）
        msg.Read(reader);                   // 字段：消息类自己读
        return msg;
    }
}
```

### 现有消息（保留并升级语义）

| 消息 | 方向 | 通道 | 内容 |
|---|---|---|---|
| JoinRequest | 客户端→服务器 | 可靠 | 玩家名字 |
| JoinResponse | 服务器→客户端 | 可靠 | 分配的玩家 ID + 当前玩家列表 |
| PlayerStateSync | 双向 | 不可靠 | 玩家ID + 位置 + 旋转 |
| ChatMessage | 双向 | 可靠 | 文本 + TargetId（0=广播，非0=私信） |
| DisconnectNotice | 双向 | 可靠 | 断开原因 |

### 新增消息

| 消息 | 方向 | 通道 | 内容 |
|---|---|---|---|
| PlayerJoinedNotice | 服务器→所有客户端 | 可靠 | 广播"有新人加入"（新人 ID/名字） |
| PlayerLeftNotice | 服务器→所有客户端 | 可靠 | 广播"有人离开" |
| SnapshotMessage | 服务器→客户端 | 可靠 | 游戏状态全量快照（开局/重建时同步） |

关键点：
- JoinResponse 带玩家列表 → 新玩家加入后知道房间里已有哪些人
- PlayerJoined/LeftNotice → 其他玩家收到人数变化通知（UI 显示）

## 5. GameServer 模块设计（核心）

```csharp
public class GameServer
{
    private readonly INetworkTransport _transport;
    private readonly Dictionary<ulong, PlayerInfo> _players = new();  // 玩家表
    private ulong _nextPlayerId = 1;   // 玩家 ID 分配器（递增，永不重复）
    private readonly Dictionary<MessageType, Action<ClientConnection, IMessage>> _handlers = new();  // 双注册表分发（借鉴 Card）

    public void Start(int port);   // 启动服务器（绑定 transport 事件）

    // transport 事件驱动：
    // OnClientConnected      → 登记连接
    // OnClientDisconnected   → 清理玩家表，广播 PlayerLeftNotice
    // OnMessageReceived      → 统一入口：先强制覆盖 SenderId，再按类型分发
    //     JoinRequest       → 分配 ID，发 JoinResponse（带玩家列表），广播 PlayerJoinedNotice
    //     PlayerStateSync   → 广播给所有其他客户端
    //     ChatMessage       → 按 TargetId 广播或定向转发
    //     DisconnectNotice  → 清理
}
```

### 身份校验：统一入口强制覆盖（借鉴 Card ReceiveChat，GameServer.cs:479）

```csharp
void OnMessageReceived(IMessage msg)
{
    msg.SenderId = _connToPlayer[当前连接];   // 身份由连接决定，不由客户端声明——一行覆盖所有消息类型
    Dispatch(msg);                            // 再按注册表分发处理
}
```

原理：客户端不可信——消息里填的身份一律不算数，服务器用**连接**查真实玩家并覆盖。Card 只有聊天消息带身份字段所以只覆盖聊天；我们的消息全带 SenderId，统一入口一行覆盖全部，更简洁。

### 双注册表分发（借鉴 Card GameServer/GameClient registered_commands）

```csharp
// 注册：ID → 处理函数（替代 switch）
_handlers[MessageType.ChatMessage] = OnChatMessage;
_handlers[MessageType.PlayerStateSync] = OnPlayerStateSync;

// 分发：查表调用
void Dispatch(IMessage msg)
{
    if (_handlers.TryGetValue(msg.Type, out var handler))
        handler(当前连接, msg);
}
```

加消息 = 注册一行，零改分发逻辑。

```
客户端 A 移动 → SendToServer(PlayerStateSync)
    → 服务器 OnMessageReceived → 校验（SenderId == 该连接真实 ID？不匹配则丢弃）
    → BroadcastToClients（除 A 自己）→ B、C 收到
```

- **玩家 ID 分配**：`_nextPlayerId++`，服务器权威的唯一 ID 来源
- **SenderId 校验**：客户端可能伪造"我是别的玩家"——服务器检查消息 SenderId 与连接真实 ID 是否一致，不一致丢弃。这是权威的最基本防线
- 位置校验（更严格的反作弊）第一周不做，只做 SenderId 校验

## 6. GameClient 模块设计

```csharp
public class GameClient
{
    private readonly INetworkTransport _transport;
    public ulong LocalPlayerId { get; private set; }  // 服务器分配的 ID

    public void Connect(string ip, int port);
    public void SendToServer(IMessage msg);   // 上行（自动填 SenderId）

    // 内部：
    // OnConnected     → 发 JoinRequest
    // JoinResponse   → 记录 LocalPlayerId，保存玩家列表
    // Joined/LeftNotice → 更新本地玩家列表
    // PlayerStateSync → 更新本地其他玩家状态
}
```

职责边界：GameClient 管"本客户端视角"（我的 ID、我认识的玩家列表）；GameServer 管"全局视角"（所有玩家、权威广播）。

### Host 组装（应用层）

```csharp
// Host 进程：
var server = new GameServer();
server.Start(7777);
var client = new GameClient();
client.Connect("127.0.0.1", 7777);   // 连自己
```

## 7. 开发顺序（按依赖排，每步独立验证 + 提交）

```
Step 1：接口补丁（+2 事件 + SenderId）→ 编译过 + NgoAdapter 空壳更新
Step 2：消息协议（新增 3 种 + 升级现有）→ 序列化单元测试
Step 3：CustomNetAdapter（连接管理 + 双通道 + 心跳断线检测）
Step 4：GameServer（玩家管理 + 广播 + SenderId 校验）→ FakeTransport 单元测试
Step 5：GameClient + Host 组装 → Editor 多实例验证
Step 6：Day 3 控制器接入 → Day 4 多人可见
```

## 8. 验收策略（三层递进）

1. **纯逻辑单元测试**（最快，每次改动跑）：
   - 消息协议：序列化 → 反序列化 → 断言字段一致
   - GameServer：**FakeTransport**（30 行内存直通假传输）模拟 3 客户端连服务器 → 断言 ID 分配正确、广播只发给"别人"、SenderId 伪造被拦截
2. **Host 真实联调**（开发中随时验证）：一个 Editor 开 Host + 2 个实例加入，验证加入/离开广播、聊天互通、断线触发
3. **独立服务器验证**（Day 5 收尾）：Dedicated Server 构建 headless，客户端连它，验证同一份 GameServer 代码在独立进程跑通

## 9. 涉及修改的既有代码清单

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/Network/INetworkTransport.cs` | +2 事件 |
| `Assets/Scripts/Network/IMessage.cs` | +SenderId 属性 |
| `NgoAdapter.cs`（未建） | 按新接口实现空壳 |
| `DEVELOPMENT_PLAN.md` | 网络设计节重写为多人版 |
| `Game.Core` 全部 | **零改动**（机制与决策分离，改造不触碰地基） |
| 消息类型文件（未建） | 按新协议建 |

## 10. 面试叙事更新

原：手写网络层 + NGO 适配器口子。
新：多人服务器权威 + 双部署形态（Dedicated/Host）+ 同一份 GameServer 代码复用 + FakeTransport 测试架构。叙事更强："我不仅手写网络层，还设计了服务器权威模型，并且用可复用的纯逻辑服务器模块同时支撑独立服务器和 Host 两种部署。"
