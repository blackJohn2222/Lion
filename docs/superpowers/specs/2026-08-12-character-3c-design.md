# 角色控制器 / 3C 设计（第三人称）

> 日期：2026-08-12
> 状态：已确认（含 Odyssey 借鉴决策，2026-08-12 逐点敲定）
> 背景：Day 3 单人跑跳手感目标；借鉴 Odyssey（PLAYER TWO Platformer Project）的角色架构

## 1. Odyssey 借鉴决策汇总（逐点敲定）

| # | 决策点 | 敲定 |
|---|---|---|
| 1 | 状态类持有实体引用 | ✅ 借鉴——状态类方法接收 Player 引用，状态能操作玩家 |
| 2 | 物理能力方法化 | ✅ 借鉴——Player 提供物理方法（Gravity/Move/ApplyJump），状态调用 |
| 3 | 数值存储方式 | ✅ 先用字段（PlayerStats 类），Day 5 手感调优升级 ScriptableObject |
| 4 | 输入系统 | ✅ 输入方法化（GetMoveDir/GetJumpDown）+ 跳跃缓冲 |
| 5 | 事件系统 | ✅ 主用手写 EventBus<T>（系统间解耦可测）；UI 交互时按需用 UnityEvent（Unity 按钮自带） |
| 6 | 状态切换方式 | ✅ 角色状态机用 Change<TState>() 类型切换；全局 GameState 保持枚举 |
| 7 | AI 状态机复用 | ✅ 确认 StateMachine<T> 复用（角色/连接/AI），Odyssey 佐证 |
| 8 | 关卡/存档系统 | ⏸ 超出阶段，暂不设计（到该阶段自然设计） |

## 2. 总体架构（三层）

```
PlayerController（MonoBehaviour，挂角色上）
    ├── states（StateMachine<角色状态类>）   ← 状态机，纯逻辑可测
    ├── inputs（InputReader）                 ← 输入层抽象
    ├── stats（PlayerStats）                  ← 手感参数（先用类，Day5 升 SO）
    └── 物理能力方法（Gravity/Move/ApplyJump...）← 能力方法化
        ↓ 状态类方法接收 Player，调用这些能力
```

## 3. 角色状态机（核心）

### 状态类（Day 3 写 4 个）

```
IdleState / WalkState / JumpState / FallState
每个状态类：
    Enter(Player)  — 进入（播动画等）
    Step(Player)   — 每帧逻辑（调用 Player 物理方法 + 决策切换）
    Exit(Player)   — 退出（清理）
```

### 状态切换（类型方式）

```csharp
// 状态内决策切换：
player.states.Change<WalkPlayerState>();   // 类型安全

// 全局 GameState 保持枚举（Day 1 的 StateMachine<GameState> 不动）
```

### 实体引用（Odyssey 核心借鉴）

```csharp
// 状态类方法接收 Player 引用：
protected override void OnStep(Player player)
{
    player.Gravity();               // 调用 Player 的物理能力
    player.SnapToGround();
    // 决策：移动输入 → 切 Walk；跳跃键 → 切 Jump
}
```

## 4. 物理能力方法化（Player 承载）

```
Player 提供（状态调用，能力复用）：
    Gravity()            — 重力
    SnapToGround()       — 贴地吸附
    ApplyJump()          — 跳跃
    Move()               — 移动
    Friction()           — 摩擦力
    ...（Day 3 按需增减）

原则：能力放 Player（复用），决策放状态（内聚）
```

## 5. 手感参数（PlayerStats）

```csharp
// Day 3：普通类（字段）
public class PlayerStats
{
    public float moveSpeed;      // 移动速度
    public float jumpForce;      // 跳跃力
    public float gravity;        // 重力
    public float acceleration;   // 加速度
    // ...
}

// Day 5：升级为 ScriptableObject（Inspector 配置调手感）
public class PlayerStats : ScriptableObject { ... }
```

## 6. 输入层（InputReader）

```csharp
// 输入方法化（状态/控制器调用，不碰 InputAction 细节）：
Vector2 GetMoveDir();       // 移动方向
bool GetJumpDown();         // 跳跃（带缓冲，Odyssey 借鉴：提前按 0.15s 也能触发）
```

## 7. 相机（第三人称跟随）

- 环绕 + 缩放 + 平滑阻尼，默认跟随视角（设计文档原有，无 Odyssey 变更）

## 8. 事件系统（维持 EventBus）

- 系统间通信用手写 EventBus<T>（Day 1，可测解耦）
- UI 交互（Day 4）按需用 UnityEvent（Unity 按钮默认自带）

## 9. 面试叙事（新增点）

- "我参考商业项目（Odyssey）的角色架构：状态类持实体引用、物理能力方法化——能力复用、决策内聚"
- "手感参数先用类，可升级 ScriptableObject 配置化"
- "输入方法化 + 跳跃缓冲，手感细节"
- "状态机泛型复用玩家/敌人/AI，被 Odyssey 验证"

## 10. 验收（Day 3）

- 单人跑跳手感 OK（WASD 移动、跳跃、重力、坡度）
- 状态机 4 状态正确流转（Idle/Walk/Jump/Fall）
- 输入方法化 + 跳跃缓冲生效
- 状态类持实体（Player）可正常调用物理能力
