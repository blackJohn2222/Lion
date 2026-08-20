# 角色控制器 / 3C 设计（第三人称，Odyssey 复刻）

> 日期：2026-08-19（重大修订；原稿 2026-08-12）
> 状态：**已确认（grilling 收口），待实施**
> 背景：用户想用 Lion 复习 Odyssey 项目（`F:\Unity_project\Odyssey\Assets\PLAYER TWO\Platformer Project\Scripts`）的角色层架构；2026-08-19 grilling 全程逐条敲定
> 原设计（2026-08-12：手写 `StateMachine<T>` 基类 + PlayerController 组装）**已推翻**，原文保留于文末"附：历史设计"

## 1. 目标

- 用 Lion 复刻 **Odyssey 角色层架构**：继承式实体 + 组件式状态机 + 模板方法 + 门面输入 + 数据驱动数值
- **内容按小体量**：只做 Day 3 需要的 4 状态 + 基础跑跳手感，不照抄 Odyssey 的 20 状态/多段跳/冲刺/敌人（学架构非手感）

## 2. 已确认决策（2026-08-19 grilling 逐条敲定）

| # | 决策点 | 敲定 |
|---|---|---|
| 1 | 复习目标 | **学架构**（四件套配合），不是手感堆料；多段跳/冲刺等能力 Day 3 不做 |
| 2 | 实体架构 | **继承式 `Entity<T>` + CRTP**（`Player : Entity<Player>`）；基类只放最核心通用代码（移动/重力/地面检测/状态机驱动），避免膨胀 |
| 3 | 程序集 | **新开 `Game.Entity`**（EntityBase/Entity/EntityState/EntityStateManager 等 MonoBehaviour 那套）；**Core 不动**；Gameplay 引用 Entity |
| 4 | 状态机管理器 | **MonoBehaviour 组件式**（实体 `GetComponent` 拿） |
| 5 | 状态创建 | **编译期 new（方案 B）**，不用反射；**保留升级选项 A**（反射 + Inspector 配置），改动集中在 `GetStateList`，系统完成后由用户决定是否升级 |
| 6 | 状态基类 | **抽象类 + 模板方法**（`EntityState<T>`：Enter/Exit/Step 外壳 + 计时 + onEnter/onExit 事件 + OnContact 碰撞），照抄 Odyssey 结构 |
| 7 | 数值层 | **升 ScriptableObject**（`EntityStats<T>` + `EntityStatsManager<T>`，数据驱动，多套切换，`stats.current` 取当前值） |
| 8 | 输入层 | **InputActionAsset + 门面方法**（框架照学；动作只做需要的 4-5 个：移动/跳跃/相机环绕缩放/暂停，不抄 15 个） |
| 9 | 测试 | **角色层不写单测**，Play Mode 手感验证 + 调试日志（分工：网络层/全局流程用单测） |
| 10 | Enemy | **YAGNI**，只做 Player；继承式已天然预留（`EnemyState : EntityState<Enemy>` 复用同一套），Day 5+ 再做 |
| 11 | 推翻 | Day 3 先前"泛型类状态机 `EntityStateMachine<TEntity>` 放 Core"方案**作废** |

## 3. 程序集结构（改造后）

```
Core      → 纯逻辑（枚举状态机 StateMachine<T>/EventBus/ServiceLocator/GameStateManager）—— 不动
Entity    → 新增：EntityBase / Entity<T> / EntityState<T> / EntityStateManager<T>
             / EntityStats<T> / EntityStatsManager<T> / EntityEvents（MonoBehaviour 那套）
Gameplay  → Player / PlayerState / PlayerStateManager / PlayerInputManager / PlayerStats(SO)
             / PlayerStatsManager / 4 个状态类 + .inputactions 资产（引用 Entity + Core）
Network / UI / Test → 不动
```

依赖方向：`Gameplay → Entity → Core`（Entity 不依赖 Gameplay）

## 4. 新增文件清单（约 13-15 个脚本 + 1 资产）

**Game.Entity 程序集**：
- `EntityBase.cs`（精简版 ~200 行：CharacterController 移动、地面检测、velocity）
- `Entity.cs`（泛型 CRTP 部分：`Entity<T>`，状态机驱动 + 能力方法）
- `EntityState.cs`（模板方法外壳 + 计时 + 事件 + OnContact + 可选反射工厂）
- `EntityStateManager.cs`（MonoBehaviour 管理器：current/last/Change/Step/字典）
- `EntityStats.cs` + `EntityStatsManager.cs`（SO 数值基类）
- `EntityEvents.cs`（OnGroundEnter/Exit 等）

**Gameplay**：
- `Player.cs`（: Entity<Player>，能力封装 + 动作方法）
- `PlayerState.cs`（: EntityState<Player>）
- `PlayerStateManager.cs`（MonoBehaviour，GetStateList 编译期 new 状态）
- `PlayerInputManager.cs`（门面：GetMoveDir/GetJumpDown/GetLook 等）
- `PlayerStats.cs`（改 ScriptableObject）+ `PlayerStatsManager.cs`
- 4 个状态类：`IdlePlayerState/WalkPlayerState/JumpPlayerState/FallPlayerState`
- `Player Input Actions.inputactions`（最小动作集）

## 5. 涉及的设计模式（面试可讲）

- **模板方法**：`EntityState<T>` 的 Enter/Exit/Step 外壳 + 子类钩子
- **门面**：`PlayerInputManager` 包住 InputActionAsset，上层不碰 InputAction
- **数据驱动**：SO 数值资产 + 多套切换（`EntityStatsManager<T>`）
- **CRTP**：`Entity<T>` 自引用泛型，实体/状态机/状态强类型绑定
- **策略容器 + 状态机**：管理器持有状态集合、转发 Step/OnContact
- **依赖倒置**：`GetStateList()` 是状态创建单一注册点 → 支撑方案 B→A 平滑升级

## 6. 升级路径（方案 B → A）

- 当前：**B**（编译期 new 状态，`GetStateList` 里 `new XxxState()`）——编译期安全、无反射坑（多程序集下 `Type.GetType` 需程序集限定名，易错）
- 未来可选：**A**（反射 + Inspector 配置）——改动仅 4 处：`EntityState<T>` 加反射工厂、`PlayerStateManager` 加 `string[] states` 字段、`GetStateList` 换实现、加 `ClassTypeName` 特性 + Editor 绘制器
- **前提**：实现 B 时保持 `GetStateList()` 为唯一状态注册点，A 路径永远畅通
- 决定权：系统完成后由用户拍板是否升级（2026-08-19 记）

## 7. 与现有代码的关系

- `Core` / `Network` / `UI` / 现有测试**全部不动**
- `Assets/Scripts/Gameplay/PlayerStats.cs`（普通类）→ 随数值层升为 ScriptableObject
- 2026-08-19 曾写的旧方案代码已删除，不重写（走新架构）

## 8. 后续（Day 5+，非 Day 3）

- Enemy（继承式天然复用，`EnemyState : EntityState<Enemy>`）
- 更多状态/动作（照 Odyssey 加：Dash/Glide/多段跳/土狼跳/冲刺...）——加状态 = 加类 + GetStateList 注册一行

## 9. 验收（Day 3）

- 单人跑跳手感 OK（WASD 移动、跳跃、重力、坡度）
- 4 状态正确流转（Idle/Walk/Jump/Fall）
- 门面输入方法化 + 跳跃缓冲生效
- 状态类持实体（Player）可正常调用物理能力
- 架构验收：`Game.Entity` 程序集分层正确（Core 未被污染）、四件套齐全、状态编译期 new 创建

## 10. 面试叙事

- "我复刻了商业项目 Odyssey 的角色层架构：继承式实体 + 组件式状态机 + 模板方法 + 门面输入 + 数据驱动数值，内容按自己的项目裁剪"
- "状态创建用编译期泛型替代反射——规避多程序集下反射的运行期风险，并预留了反射配置的平滑升级路径（B→A，改动集中在 GetStateList 单一注册点）"
- "设计模式清单：模板方法、门面、数据驱动、CRTP、策略容器、依赖倒置"
- "手感参数 ScriptableObject 数据驱动，支持多套数值切换"

---

## 附：历史设计（2026-08-12，已推翻，仅存档）

### 1. Odyssey 借鉴决策汇总（逐点敲定）

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

### 2. 总体架构（三层）

```
PlayerController（MonoBehaviour，挂角色上）
    ├── states（StateMachine<角色状态类>）   ← 状态机，纯逻辑可测
    ├── inputs（InputReader）                 ← 输入层抽象
    ├── stats（PlayerStats）                  ← 手感参数（先用类，Day5 升 SO）
    └── 物理能力方法（Gravity/Move/ApplyJump...）← 能力方法化
        ↓ 状态类方法接收 Player，调用这些能力
```

### 3. 角色状态机（核心）

#### 状态类（Day 3 写 4 个）

```
IdleState / WalkState / JumpState / FallState
每个状态类：
    Enter(Player)  — 进入（播动画等）
    Step(Player)   — 每帧逻辑（调用 Player 物理方法 + 决策切换）
    Exit(Player)   — 退出（清理）
```

#### 状态切换（类型方式）

```csharp
// 状态内决策切换：
player.states.Change<WalkPlayerState>();   // 类型安全

// 全局 GameState 保持枚举（Day 1 的 StateMachine<GameState> 不动）
```

#### 实体引用（Odyssey 核心借鉴）

```csharp
// 状态类方法接收 Player 引用：
protected override void OnStep(Player player)
{
    player.Gravity();               // 调用 Player 的物理能力
    player.SnapToGround();
    // 决策：移动输入 → 切 Walk；跳跃键 → 切 Jump
}
```

### 4. 物理能力方法化（Player 承载）

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

### 5. 手感参数（PlayerStats）

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

### 6. 输入层（InputReader）

```csharp
// 输入方法化（状态/控制器调用，不碰 InputAction 细节）：
Vector2 GetMoveDir();       // 移动方向
bool GetJumpDown();         // 跳跃（带缓冲，Odyssey 借鉴：提前按 0.15s 也能触发）
```

### 7. 相机（第三人称跟随）

- 环绕 + 缩放 + 平滑阻尼，默认跟随视角（设计文档原有，无 Odyssey 变更）

### 8. 事件系统（维持 EventBus）

- 系统间通信用手写 EventBus<T>（Day 1，可测解耦）
- UI 交互（Day 4）按需用 UnityEvent（Unity 按钮默认自带）

### 9. 面试叙事（新增点）

- "我参考商业项目（Odyssey）的角色架构：状态类持实体引用、物理能力方法化——能力复用、决策内聚"
- "手感参数先用类，可升级 ScriptableObject 配置化"
- "输入方法化 + 跳跃缓冲，手感细节"
- "状态机泛型复用玩家/敌人/AI，被 Odyssey 验证"

### 10. 验收（Day 3）

- 单人跑跳手感 OK（WASD 移动、跳跃、重力、坡度）
- 状态机 4 状态正确流转（Idle/Walk/Jump/Fall）
- 输入方法化 + 跳跃缓冲生效
- 状态类持实体（Player）可正常调用物理能力
