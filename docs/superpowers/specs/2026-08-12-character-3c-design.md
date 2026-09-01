# 角色控制器 / 3C 设计（第三人称，Odyssey 复刻）

> 日期：2026-08-21（执行顺序重排；架构决策沿用 2026-08-19）
> 状态：**已确认（架构与执行顺序均已收口），待实施**
> 背景：用户想用 Lion 复习 Odyssey 项目（`F:\Unity_project\Odyssey\Assets\PLAYER TWO\Platformer Project\Scripts`）的角色层架构；2026-08-19 grilling 全程逐条敲定
> 旧的 2026-08-12 手写 `StateMachine<T>` 基类 + PlayerController 组装方案**已删除**，不再作为实现依据。

## 1. 目标

- 用 Lion 复刻 **Odyssey 角色层架构**：继承式实体 + 组件式状态机 + 模板方法 + 门面输入 + 数据驱动数值
- **内容按小体量**：只做 Day 3 需要的 3 状态（Idle/Walk/Fall）+ 基础跑跳手感，不照抄 Odyssey 的 20 状态/多段跳/冲刺/敌人（学架构非手感）

## 2. 已确认决策（2026-08-19 grilling 逐条敲定）

| # | 决策点 | 敲定 |
|---|---|---|
| 1 | 复习目标 | **学架构**（四件套配合），不是手感堆料；多段跳/冲刺等能力 Day 3 不做 |
| 2 | 实体架构 | **继承式 `Entity<T>` + CRTP**（`Player : Entity<Player>`）；基类只放最核心通用代码（移动/重力/地面检测/状态机驱动），避免膨胀 |
| 3 | 程序集 | **新开 `Entity`**（EntityBase/Entity/EntityState/EntityStateManager 等 MonoBehaviour 那套）；**Core 不动**；Gameplay 引用 Entity |
| 4 | 状态机管理器 | **MonoBehaviour 组件式**（实体 `GetComponent` 拿）；拆两层：`EntityStateManagerBase`（非泛型，放 events 字段）+ `EntityStateManager<T>`（泛型主体） |
| 5 | 状态创建 | **编译期 new（方案 B）**，不用反射；**保留升级选项 A**（反射 + Inspector 配置），改动集中在 `GetStateList`，系统完成后由用户决定是否升级 |
| 6 | 状态基类 | **抽象类 + 模板方法**（`EntityState<T>`：Enter/Exit/Step 外壳 + 计时 + onEnter/onExit 事件 + OnContact virtual 空实现），照抄 Odyssey 结构 |
| 7 | 数值层 | **升 ScriptableObject**（`EntityStats<T>` + `EntityStatsManager<T>`，数据驱动，多套切换，`stats.current` 取当前值） |
| 8 | 输入层 | **InputActionAsset + 门面方法**（框架照学；动作只做需要的 4-5 个：移动/跳跃/相机环绕缩放/暂停，不抄 15 个） |
| 9 | 测试 | **角色层不写单测**，Play Mode 手感验证 + 调试日志（分工：网络层/全局流程用单测） |
| 10 | Enemy | **YAGNI**，只做 Player；继承式已天然预留（`EnemyState : EntityState<Enemy>` 复用同一套），Day 5+ 再做 |
| 11 | 推翻 | Day 3 先前"泛型类状态机 `EntityStateMachine<TEntity>` 放 Core"方案**作废** |
| 12 | **跳跃是动作非状态** | ✅ 照抄 Odyssey：**无 Jump 状态**；`Player.Jump()` 是动作方法（条件检查→施加垂直速度→切 `FallPlayerState`→触发 `OnJump`）；Fall 是"空中枢纽"，落地回 Idle；跳缓冲落地跳靠 Idle 每帧调 `Jump()` 天然成立 |
| 13 | Day 3 状态集 | ✅ **3 状态：Idle/Walk/Fall**（旧设计的 JumpPlayerState 废弃）；Brake/Crouch/Dash/Spin/Glide 等全部后续再补 |
| 14 | Day 3 砍掉项 | Health（无生命值）、GameTags（无 Tag 需求）、HandleContacts 检测循环（无交互物，但 `OnContact` virtual 保留）、多段跳/土狼跳/短按矮跳（Day 5 手感调优再加） |
| 15 | 事件三层 | `EntityEvents`（实体层，砍到 OnGroundEnter/Exit）+ `EntityStateManagerEvents`（状态机层）+ `PlayerEvents`（玩家层，砍到 OnJump） |

## 3. 程序集结构（改造后）

```
Core      → 纯逻辑（枚举状态机 StateMachine<T>/EventBus/ServiceLocator/GameStateManager）—— 不动
Entity    → 新增：EntityBase / Entity<T> / EntityState<T>
             / EntityStateManagerBase + EntityStateManager<T>（两层）+ EntityStateManagerEvents
             / EntityStats<T> + EntityStatsManager<T> / EntityEvents（MonoBehaviour 那套）
Gameplay  → Player / PlayerState / PlayerStateManager / PlayerInputManager / PlayerEvents
             / PlayerStats(SO) + PlayerStatsManager / 3 个状态类（Idle/Walk/Fall）+ .inputactions 资产（引用 Entity + Core）
Network / UI / Test → 不动
```

依赖方向：`Gameplay → Entity → Core`（Entity 不依赖 Gameplay）

## 4. 新增文件清单（约 19 个脚本 + 1 个输入资产）

**Entity 程序集**：
- `EntityBase.cs`（精简版 ~200 行：CharacterController 移动、地面检测、velocity、基础能力方法）
- `Entity.cs`（泛型 CRTP 部分：`Entity<T>`，状态机驱动 + 能力方法）
- `EntityState.cs`（模板方法外壳 + 计时 + 事件 + OnContact virtual 空实现）
- `EntityStateManager.cs`（**两个类**：`EntityStateManagerBase` 非泛型放 events 字段 + `EntityStateManager<T>` 泛型主体：current/last/Change/Step/字典）
- `EntityStateManagerEvents.cs`（onChange/onEnter(Type)/onExit(Type)）
- `EntityStats.cs` + `EntityStatsManager.cs`（SO 数值基类）
- `EntityEvents.cs`（OnGroundEnter/OnGroundExit）

**Gameplay**：
- `Player.cs`（: Entity<Player>，RequireComponent 三组件；能力封装 + **Jump/Fall 等动作方法**）
- `PlayerState.cs`（: EntityState<Player>）
- `PlayerStateManager.cs`（MonoBehaviour，GetStateList 编译期 new 状态）
- `PlayerEvents.cs`（精简：OnJump）
- `PlayerInputManager.cs`（门面：动作缓存 + GetMovementCameraDirection/GetJumpDown 含缓冲等）
- `PlayerStats.cs`（改 ScriptableObject，基础字段集 ~12 个）+ `PlayerStatsManager.cs`
- **3 个状态类**：`IdlePlayerState / WalkPlayerState / FallPlayerState`（**无 JumpPlayerState**——跳跃是 Player.Jump() 动作）
- `ThirdPersonCamera.cs`（跟随、环绕、缩放和平滑阻尼）
- `Player Input Actions.inputactions`（最小动作集：Movement/Jump/Look/Pause）

## 4.1 Day 3 执行顺序（学习优先）

> 第 4 节是最终文件结构清单，不代表实际写代码顺序。实际开发采用“薄骨架 + 功能纵向切片”：只在当前功能需要时创建类，一个切片可以跨 Entity、Gameplay、输入资产和数值资产；每个切片先通过编译检查，所有切片完成后再统一进入 Play Mode。

### Slice 0A：Entity 通用基座

- 只处理 `Entity` 程序集，不创建 Player 文件。
- 依次建立 `EntityBase`、`EntityState<T>`、`EntityStateManagerBase`、`EntityStateManager<T>`、`EntityStateManagerEvents` 和 `Entity<T>` 的类型关系与最小生命周期。
- 暂不实现完整移动、重力、跳跃、PlayerStats 或 PlayerEvents。
- 完成标准：Entity 程序集编译通过，能解释继承、CRTP、模板方法和组件式管理器的关系。

### Slice 0B：Player 接入

- 创建 `Player`、`PlayerState`、`PlayerStateManager` 和 `IdlePlayerState`。
- `Player : Entity<Player>`，状态管理器通过组件关系接入，初始状态由编译期 `new` 创建。
- 补齐 `Gameplay.asmdef` 对 `Entity` 的引用；Core、Network、UI 和现有测试不动。
- 完成标准：Gameplay 与 Entity 编译通过，Player 能驱动 Idle 状态的生命周期。

### Slice 1：平面移动

- 创建 Movement 输入、`PlayerInputManager` 的移动门面、`EntityStats<T>`、`EntityStatsManager<T>`、`PlayerStats` 和 `PlayerStatsManager`。
- 在 `EntityBase` 中实现水平移动原语，在 `Player` 中用当前数值封装 `Move()`，由 Idle/Walk 做状态决策。
- 完成标准：编译通过，并能说明“输入 → 状态 → Player 能力 → 数值 → Entity 物理执行”的调用链。

### Slice 2：重力与落地

- 在 `EntityBase` 中加入重力、地面检测和地面吸附。
- 创建 `EntityEvents`，加入 `FallPlayerState`，完成离地和落地状态流转。
- 完成标准：编译通过，并能说明实体事件与状态切换事件的职责区别。

### Slice 3：跳跃动作

- 加入 Jump 输入和跳跃缓冲，创建 `PlayerEvents`，实现 `Player.Jump()`。
- 跳跃施加垂直速度并切换到 Fall；不创建 Jump 状态。
- 完成标准：编译通过，并能说明为什么 Jump 是动作、Fall 是空中枢纽。

### Slice 4：第三人称相机

- 加入 Look 输入和第三人称相机脚本，实现跟随、环绕、缩放和平滑阻尼。
- 相机只消费输入和目标 Transform，不参与 Player 状态决策。

### Slice 5：统一 Play Mode 验证

- 前面切片只做编译检查；所有角色代码和资产完成后，再统一创建测试场景。
- 验证 Idle/Walk/Fall、跑步、重力、跳跃缓冲、落地和相机。
- 角色层不写单元测试，使用 Play Mode 手感验证和调试日志。

## 5. 涉及的设计模式（面试可讲）

- **模板方法**：`EntityState<T>` 的 Enter/Exit/Step 外壳 + 子类钩子
- **门面**：`PlayerInputManager` 包住 InputActionAsset，上层不碰 InputAction
- **数据驱动**：SO 数值资产 + 多套切换（`EntityStatsManager<T>`）
- **CRTP**：`Entity<T>` 自引用泛型，实体/状态机/状态强类型绑定
- **策略容器 + 状态机**：管理器持有状态集合、转发 Step/OnContact
- **依赖倒置**：`GetStateList()` 是状态创建单一注册点 → 支撑方案 B→A 平滑升级
- **功能纵向切片**：按用户可理解的功能跨模块实现，而不是先把单个类或单个程序集全部写满。

## 6. 升级路径（方案 B → A）

- 当前：**B**（编译期 new 状态，`GetStateList` 里 `new XxxState()`）——编译期安全、无反射坑（多程序集下 `Type.GetType` 需程序集限定名，易错）
- 未来可选：**A**（反射 + Inspector 配置）——改动仅 4 处：`EntityState<T>` 加反射工厂、`PlayerStateManager` 加 `string[] states` 字段、`GetStateList` 换实现、加 `ClassTypeName` 特性 + Editor 绘制器
- **前提**：实现 B 时保持 `GetStateList()` 为唯一状态注册点，A 路径永远畅通
- 决定权：系统完成后由用户拍板是否升级（2026-08-19 记）

## 7. 与现有代码的关系

- `Core` / `Network` / `UI` / 现有测试**全部不动**
- 旧的 `Assets/Scripts/Gameplay/PlayerStats.cs` 普通类已删除；新的 `PlayerStats` 按 Slice 1 以 ScriptableObject 重建。
- `Gameplay.asmdef` 在 Slice 0B 补充对 `Entity` 的引用。
- 2026-08-19 曾写的旧方案代码已删除，不重写（走新架构）

## 8. 后续（Day 5+，非 Day 3）

- Enemy（继承式天然复用：`Enemy : Entity<Enemy>` / `EnemyState : EntityState<Enemy>` / 同一套 Manager/Stats/Events，Odyssey 已验证 8 条复用证据）
- 更多状态/动作（照 Odyssey 加：Brake/Crouch/Dash/Spin/Glide/多段跳/土狼跳/短按矮跳...）——加状态 = 加类 + GetStateList 注册一行
- **重力分段系统（Day 5 手感调优时评估）**：Odyssey 的 `Player.Gravity()` 三段式——上升用 `gravity`（小，跳跃上升轻）、下落用 `fallGravity`（大，下坠加速猛）、`gravityTopSpeed` 钳制最大下落速度（防无限加速穿模）。当前 Slice 2 用恒定重力，届时按此升级（Entity 原语加参数 + PlayerStats 加 3 字段）。**⚠️ 跳跃初速度推导基于上升段 `gravity`（`√(2 × jumpHeight × gravity)`），改重力分段时同步跳跃公式，勿沿用旧的恒定 gravity 值**
- 交互链：`HandleContacts` 检测循环 + `IEntityContact` + 交互物（Spring/弹簧、Pole/爬杆、Hazard 等，走 OnContact 路由）
- 战斗链：`Health` 组件 + `EntityHitbox` + PlayerEvents 扩充（OnHurt/OnDie）+ Player.ApplyDamage
- 表现层（三挂钩模式，Odyssey 已验证）：A 订阅 `states.events.onChange`（动画/拖尾）、B 订阅 PlayerEvents/EntityEvents（音效/粒子）、C 每帧轮询 `IsCurrentOfType`（倾斜/相机）
- `EntityStateManagerListener`（按状态名挂钩子，动画期补）
- `EntityVolumeEffector`（区域效果，玩法定后补）
- `GameTags`（用到 Tag 时补：移动平台 Platform 父子绑定、敌人检测等）
- 反射工厂（升级方案 A 时加）
- **暂停衔接（现在没有，Day 5 做暂停时考虑）**：Odyssey 的 `EntityStateManager.Change()/Step()` 内建 `Time.timeScale > 0` 检查——暂停靠 `Time.timeScale = 0` 实现，状态机自动停摆。Day 5 做 LevelPauser 式暂停系统时照抄这几行，与全局 GameState.Paused 衔接（现有框架下很好实现，几行防御代码）

## 8.1 设计原则备忘（2026-08-19 全量阅读 Odyssey 后补充，Day 3 实现时遵守）

- **UnityEvent vs EventBus 分工**：角色层事件三层（EntityEvents/EntityStateManagerEvents/PlayerEvents）用 UnityEvent（照抄 Odyssey，Inspector 可视化挂钩表现层）；Day 1 的手写 EventBus\<T\>/C# event 管系统间逻辑解耦（可测）。两者不冲突，分工明确：**EventBus = 系统间逻辑解耦，UnityEvent = 表现层 Inspector 挂钩**
- **物理能力两层结构**：`EntityBase` 提供参数化原语（`Accelerate(dir, turningDrag, accel, topSpeed)` / `Gravity(g)` / `SnapToGround(force)`）；`Player` 提供无参封装（读 `stats.current` 填参数再调原语）。Day 3 写代码按此两层结构写
- **动作方法三段式模板**（加新动作照抄）：
  ```csharp
  public virtual void Jump() {
      if (条件检查) {              // ① 条件（地面/土狼跳/多段跳...）
          Jump(height);            // ② 执行：改 velocity + Change 状态 + 触发事件
      }
  }
  ```
  Day 3 的 `Player.Jump()` 按此模板写；Day 5+ 加 Dash/Glide 等动作 = 加方法 + 状态类 OnStep 加一行调用

## 9. 验收（Day 3）

- 单人跑跳手感 OK（WASD 移动、跳跃、重力、坡度）
- **3 状态正确流转（Idle/Walk/Fall）+ 跳跃动作正常**（跳跃 = Player.Jump() 动作切 Fall，非独立状态）
- 门面输入方法化 + 跳跃缓冲生效（含落地瞬间缓冲起跳）
- 状态类持实体（Player）可正常调用物理能力
- 架构验收：`Entity` 程序集分层正确（Core 未被污染）、四件套 + 事件三层齐全、状态编译期 new 创建
- 阶段验收：Slice 0A、0B、1、2、3、4 分别通过编译检查；所有切片完成后再统一进行 Play Mode 验证。

## 10. 面试叙事

- "我复刻了商业项目 Odyssey 的角色层架构：继承式实体 + 组件式状态机 + 模板方法 + 门面输入 + 数据驱动数值，内容按自己的项目裁剪"
- "跳跃是动作不是状态——能力方法化 + Fall 作为空中枢纽，状态集合最小化"（Odyssey 架构精髓）
- "状态创建用编译期泛型替代反射——规避多程序集下反射的运行期风险，并预留了反射配置的平滑升级路径（B→A，改动集中在 GetStateList 单一注册点）"
- "事件三层：实体事件（落地/离地）+ 状态机事件（切换广播）+ 玩家事件（动作），表现层三种挂钩模式（订阅切换事件/订阅语义事件/轮询状态）"
- "设计模式清单：模板方法、门面、数据驱动、CRTP、策略容器、依赖倒置"
- "手感参数 ScriptableObject 数据驱动，支持多套数值切换"
