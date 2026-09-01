# Day 3 Character Feature-Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 以学习优先的功能纵向切片方式完成 Lion Day 3 的 Entity、Player、输入、数值、跳跃和第三人称相机，并在最后统一进行 Play Mode 验证。

**Architecture:** 保持 `Gameplay → Entity → Core` 的程序集依赖。实现不按单个类写满，而是先建立能支撑当前功能的薄骨架，再让一个功能跨所需的 `Entity`、`Gameplay`、输入资产和数值资产形成完整调用链。状态创建使用编译期 `new`，角色状态使用 `EntityStateManager<T>`，跳跃是 `Player.Jump()` 动作而不是状态。

**Tech Stack:** Unity `6000.3.11f1`, URP `17.3.0`, Input System `1.19.0`, `CharacterController`, `ScriptableObject`, `UnityEvent`, C# Assembly Definition。

## Global Constraints

- 用户是初学者，理解和学会高于完成速度；每个切片开始前先讲目的、设计模式、类关系、执行流程和验证方式。
- 每个切片开始前明确询问“要我写还是你自己写”；用户自己写时只提供接口草图、提示和检查点，不代写。
- 每个切片先通过 Unity 编译检查；所有切片完成后再统一进入 Play Mode。
- 角色层不写单元测试，使用编译检查、调试日志和最终 Play Mode 验证。
- `Entity.asmdef` 的程序集名是 `Entity`，并继续只引用 `Core`。
- Slice 0B 修改 `Gameplay.asmdef`，增加对 `Entity` 的引用；不修改 `Core`、`Network`、`UI` 和现有测试。
- `Player` 最终继承 `Entity<Player>`；`EntityState<T>` 使用模板方法；状态通过 `GetStateList()` 编译期 `new` 创建，不使用反射。
- Day 3 只有 `IdlePlayerState`、`WalkPlayerState`、`FallPlayerState`；不创建 `JumpPlayerState`、Health、GameTags、接触扫描循环、多段跳或冲刺。
- 角色层事件使用 `UnityEvent`；Core 的 `EventBus<T>` 继续负责系统间逻辑解耦。
- 保留 Unity 初始化时生成的 `Assets/InputSystem_Actions.inputactions`；它没有现有代码或场景引用，不作为 Day 3 输入资产使用。
- Day 3 新建 `Assets/Examples/Input Actions/Player Input Actions.inputactions`，最终只包含 `Movement`、`Jump`、`Look`、`Pause` 四个动作。
- Unity 生成的 `.meta` 文件由 Unity 编辑器生成，不手写。
- 发现编译或运行错误时，先保留完整原文并停止修改，分析原因后等待用户审核。
- 不自动执行 Git commit；只有用户明确要求时才提交。

## File Map

| Slice | Files | Responsibility |
|---|---|---|
| 0A | `Assets/Scripts/Entity/EntityBase.cs`, `Entity.cs`, `EntityState.cs`, `EntityStateManager.cs`, `EntityStateManagerEvents.cs` | Entity 通用实体、状态模板和状态管理器的最小关系 |
| 0B | `Assets/Scripts/Gameplay/Gameplay.asmdef`, `Gameplay/Player/Player.cs`, `PlayerState.cs`, `PlayerStateManager.cs`, `Player/States/IdlePlayerState.cs` | Player 接入 Entity 并运行初始 Idle |
| 1 | Entity 物理水平能力、`EntityStats.cs`, `EntityStatsManager.cs`; Gameplay 数值、输入、Player 移动和 Walk | 打通 Movement → 状态 → Player → 数值 → CharacterController |
| 2 | `EntityEvents.cs`, Entity 重力/地面逻辑, `FallPlayerState.cs` | 打通离地、重力、地面检测和落地 |
| 3 | Jump 输入/缓冲, `PlayerEvents.cs`, `Player.Jump()`, Idle/Fall 调整 | 打通动作式跳跃，不创建 Jump 状态 |
| 4 | Look/Pause 输入定义, `ThirdPersonCamera.cs` | 完成跟随、环绕、缩放和平滑阻尼 |
| 5 | `Assets/Scenes/Day3CharacterTest.unity` 和 PlayerStats 资产 | 统一场景搭建与 Play Mode 验收 |

每个任务都包含一个教学门：先讲解并确认写作方式，再允许进入该任务的代码或资产操作。

---

### Task 1: Slice 0A Entity 通用基座

**Files:**
- Create: `Assets/Scripts/Entity/EntityBase.cs`
- Create: `Assets/Scripts/Entity/Entity.cs`
- Create: `Assets/Scripts/Entity/EntityState.cs`
- Create: `Assets/Scripts/Entity/EntityStateManager.cs`
- Create: `Assets/Scripts/Entity/EntityStateManagerEvents.cs`
- Verify unchanged: `Assets/Scripts/Entity/Entity.asmdef`

**Interfaces:**
- `EntityBase` exposes the shared `CharacterController` reference, `velocity`, `lateralVelocity`, `verticalVelocity`, and `isGrounded` state needed by subsequent slices. It does not reference `Player`, `Gameplay`, input, or PlayerStats.
- `Entity<T>` is `EntityBase` with `where T : Entity<T>` and exposes the strongly typed `EntityStateManager<T> states` reference.
- `EntityState<T>` uses `where T : Entity<T>` and exposes `Enter(T entity)`, `Exit(T entity)`, `Step(T entity)`, and virtual `OnContact(T entity, Collider other)`. `Enter`, `Exit`, and `Step` call protected subclass hooks and maintain `timeSinceEntered`.
- `EntityStateManagerBase : MonoBehaviour` owns the serializable `EntityStateManagerEvents events` field.
- `EntityStateManager<T> : EntityStateManagerBase where T : Entity<T>` owns `current`, `last`, a type-indexed state dictionary, the abstract `GetStateList()`, typed `Change<TState>()`, `Step()`, and `OnContact(Collider other)`.
- `EntityStateManagerEvents` exposes `UnityEvent onChange`, `UnityEvent<Type> onEnter`, and `UnityEvent<Type> onExit` to mirror the approved Odyssey event split.

- [ ] **Step 1: 教学门**

讲清继承、CRTP、模板方法、策略容器和组件式管理器的关系，并确认本任务由用户自己写还是由 AI 写。

- [ ] **Step 2: 建立 Entity 类型骨架**

创建 5 个 Entity 文件，统一使用 `namespace Entity`，只添加当前接口所需的 Unity 和 Core 引用。不要在此任务创建 Player、Stats、Input 或地面事件文件。

- [ ] **Step 3: 实现状态模板和管理器最小生命周期**

让管理器可以从唯一的 `GetStateList()` 获取状态实例、设置 `current`、调用 `Enter` 和 `Step`，但先不实现 Player 状态和物理行为。

- [ ] **Step 4: Unity 编译检查**

在 Unity 编辑器中等待 `Entity` 程序集重新编译。检查目标是无 Entity 相关编译错误；若有错误，停止并按 Global Constraints 报告原文。

- [ ] **Step 5: 学习回顾**

用户用自己的话说明 `EntityBase`、`Entity<T>`、`EntityState<T>`、`EntityStateManager<T>` 的职责和一帧内的调用关系，再进入 Slice 0B。

### Task 2: Slice 0B Player 接入与 Idle

**Files:**
- Modify: `Assets/Scripts/Gameplay/Gameplay.asmdef` to add the `Entity` reference.
- Create: `Assets/Scripts/Gameplay/Player/Player.cs`
- Create: `Assets/Scripts/Gameplay/Player/PlayerState.cs`
- Create: `Assets/Scripts/Gameplay/Player/PlayerStateManager.cs`
- Create: `Assets/Scripts/Gameplay/Player/States/IdlePlayerState.cs`

**Interfaces:**
- `Player : Entity<Player>` is the concrete entity type. At this stage it only wires the state manager and does not read input or stats.
- `PlayerState : EntityState<Player>` is the typed state base for Player.
- `PlayerStateManager : EntityStateManager<Player>` overrides `GetStateList()` and returns exactly one state: `new IdlePlayerState()`.
- `IdlePlayerState` implements the template hooks without movement or jump decisions; its purpose in this task is to prove that the manager can enter and step a concrete Player state.

- [ ] **Step 1: 教学门**

说明 Gameplay 如何依赖 Entity、为什么 `Player : Entity<Player>` 能获得强类型状态管理器，并确认写作方式。

- [ ] **Step 2: 接通程序集依赖**

在 `Gameplay.asmdef` 的 `references` 中加入 `Entity`，保留现有 `Core`、`Network` 和 `Unity.InputSystem` 引用。

- [ ] **Step 3: 创建 Player 和 Idle 最小实现**

让 PlayerStateManager 在唯一注册点编译期创建 Idle，并让 Player 的生命周期能够调用状态管理器。

- [ ] **Step 4: Unity 编译检查**

确认 `Entity` 和 `Gameplay` 都能编译。检查重点是泛型约束、程序集引用和 `GetStateList()` 的返回类型。

- [ ] **Step 5: 学习回顾**

用户说明 Player、PlayerState、PlayerStateManager 和 Idle 的依赖方向，并解释为什么此时还不创建 Input、Stats 和 Fall。

### Task 3: Slice 1 平面移动

**Files:**
- Create: `Assets/Scripts/Entity/EntityStats.cs`
- Create: `Assets/Scripts/Entity/EntityStatsManager.cs`
- Modify: `Assets/Scripts/Entity/EntityBase.cs`
- Modify: `Assets/Scripts/Entity/Entity.cs`
- Create: `Assets/Scripts/Gameplay/Player/PlayerStats.cs`
- Create: `Assets/Scripts/Gameplay/Player/PlayerStatsManager.cs`
- Create: `Assets/Scripts/Gameplay/Player/PlayerInputManager.cs`
- Modify: `Assets/Scripts/Gameplay/Player/Player.cs`
- Modify: `Assets/Scripts/Gameplay/Player/PlayerStateManager.cs`
- Modify: `Assets/Scripts/Gameplay/Player/States/IdlePlayerState.cs`
- Create: `Assets/Scripts/Gameplay/Player/States/WalkPlayerState.cs`
- Create: `Assets/Examples/Input Actions/Player Input Actions.inputactions`
- Preserve unchanged: `Assets/InputSystem_Actions.inputactions`

**Interfaces:**
- `EntityStats<T> : ScriptableObject where T : EntityStats<T>` is the reusable data base.
- `EntityStatsManager<T> : MonoBehaviour where T : EntityStats<T>` exposes `T[] stats`, `T current`, `Change(int to)`, and initializes `current` from the first configured asset.
- `PlayerStats : EntityStats<PlayerStats>` contains only the horizontal movement fields required now: acceleration, deceleration, top speed, and turning drag. Gravity and jump fields are added in the slices that use them.
- `PlayerStatsManager : EntityStatsManager<PlayerStats>` is the Player-specific stats adapter; the concrete asset is assigned during Task 7 scene setup.
- `PlayerInputManager` owns an `InputActionAsset`, caches the Movement action, enables/disables the asset with the component lifecycle, and exposes `GetMovementCameraDirection()` without exposing `InputAction` to states.
- `Player` adds `PlayerStatsManager` and `PlayerInputManager` as required components, exposes `Move()` and uses `stats.current` to call EntityBase horizontal primitives.
- `IdlePlayerState` changes to `WalkPlayerState` when movement intent is non-zero and otherwise decelerates.
- `WalkPlayerState` calls `Player.Move()` while movement intent exists and changes to Idle after horizontal velocity has decelerated to zero.
- `PlayerStateManager.GetStateList()` returns `IdlePlayerState` and `WalkPlayerState` in a deterministic order.
- The new input asset begins with a `Player` action map and a `Movement` Vector2 action bound to WASD, arrow keys, and a gamepad left stick. Jump, Look, and Pause are added in their owning slices.

- [ ] **Step 1: 教学门**

Explain the input facade, ScriptableObject data-driven design, and the two-level physics interface: `EntityBase` receives parameters while `Player` reads `stats.current`. Confirm writing mode.

- [ ] **Step 2: Create the minimal Player input asset**

Create `Assets/Examples/Input Actions/Player Input Actions.inputactions` in the Unity Input System editor. Leave the Unity template asset untouched. Do not add Attack, Interact, Crouch, Sprint, or other actions to the new asset.

- [ ] **Step 3: Implement movement data and facade**

Create the generic stats base, Player stats manager, and Movement-only input facade. Keep asset assignment for Task 7 scene setup; do not add jump buffering yet.

- [ ] **Step 4: Implement horizontal physics and Player wrapper**

Add `Accelerate(Vector3 direction, float turningDrag, float acceleration, float topSpeed)`, `Decelerate(float deceleration)`, and the controller movement step to EntityBase. Keep horizontal and vertical velocity updates independent. Add Player’s parameterless `Move()` wrapper.

- [ ] **Step 5: Implement Idle/Walk decisions**

Register Walk through `GetStateList()`. Keep movement decisions inside the states and physical execution inside Player/EntityBase.

- [ ] **Step 6: Unity 编译检查**

Confirm the Entity and Gameplay assemblies compile, the new input asset imports, and no existing test or network file changed.

- [ ] **Step 7: 学习回顾**

User explains the complete Movement call chain and identifies which object owns input intent, state decision, movement parameters, and physical execution.

### Task 4: Slice 2 重力、地面检测与 Fall

**Files:**
- Create: `Assets/Scripts/Entity/EntityEvents.cs`
- Modify: `Assets/Scripts/Entity/EntityBase.cs`
- Modify: `Assets/Scripts/Entity/Entity.cs`
- Modify: `Assets/Scripts/Gameplay/Player/PlayerStats.cs`
- Modify: `Assets/Scripts/Gameplay/Player/Player.cs`
- Modify: `Assets/Scripts/Gameplay/Player/PlayerStateManager.cs`
- Modify: `Assets/Scripts/Gameplay/Player/States/IdlePlayerState.cs`
- Modify: `Assets/Scripts/Gameplay/Player/States/WalkPlayerState.cs`
- Create: `Assets/Scripts/Gameplay/Player/States/FallPlayerState.cs`

**Interfaces:**
- `EntityEvents` exposes only `UnityEvent OnGroundEnter` and `UnityEvent OnGroundExit`.
- `EntityBase` adds an `EntityEvents entityEvents` field, `Gravity(float gravity)`, `SnapToGround(float force)`, ground transition detection, and the post-move `CharacterController.isGrounded` update.
- `PlayerStats` adds the gravity and snap-force values consumed by Player wrappers.
- `Player` adds parameterless `Gravity()` and `SnapToGround()` wrappers that read `stats.current`.
- `FallPlayerState` applies gravity and chooses Idle or Walk after the entity becomes grounded.
- Idle and Walk transition to Fall when the entity is no longer grounded.

- [ ] **Step 1: 教学门**

Explain why ground detection belongs to EntityBase, why gravity is a reusable physical primitive, and why `EntityEvents` is a notification layer rather than a state decision layer. Confirm writing mode.

- [ ] **Step 2: Add gravity and ground data**

Add the minimal PlayerStats fields, EntityBase wrappers, and transition-only ground events. Ensure ground events fire only when the boolean changes, not every frame.

- [ ] **Step 3: Add Fall and transitions**

Register Fall through `GetStateList()`. Update Idle, Walk, and Fall so that the state manager remains the single owner of state transitions while EntityBase remains the owner of physical ground facts.

- [ ] **Step 4: Unity 编译检查**

Confirm all current Entity and Gameplay files compile and the state list contains exactly Idle, Walk, and Fall.

- [ ] **Step 5: 学习回顾**

User explains the difference between `isGrounded`, `FallPlayerState`, `EntityEvents`, and `EntityStateManagerEvents`.

### Task 5: Slice 3 跳跃动作与跳跃缓冲

**Files:**
- Modify: `Assets/Examples/Input Actions/Player Input Actions.inputactions`
- Modify: `Assets/Scripts/Gameplay/Player/PlayerInputManager.cs`
- Create: `Assets/Scripts/Gameplay/Player/PlayerEvents.cs`
- Modify: `Assets/Scripts/Gameplay/Player/Player.cs`
- Modify: `Assets/Scripts/Gameplay/Player/States/IdlePlayerState.cs`
- Modify: `Assets/Scripts/Gameplay/Player/States/WalkPlayerState.cs`
- Modify: `Assets/Scripts/Gameplay/Player/States/FallPlayerState.cs`

**Interfaces:**
- The input asset adds a `Jump` Button action bound to Space and the gamepad south button.
- `PlayerInputManager` caches Jump, records the press timestamp in a `0.15f` buffer, and exposes `GetJumpDown()` that consumes a valid buffered press once.
- `PlayerEvents` exposes only `UnityEvent OnJump` for Day 3, and `Player` owns the serializable PlayerEvents instance.
- `Player.Jump()` checks the grounded condition, computes the initial vertical velocity from the configured jump height and gravity, changes to `FallPlayerState`, and invokes `OnJump` only when the jump succeeds.
- Idle and Walk call `Player.Jump()` during their step. Fall does not become a Jump state; it remains the airborne hub.

- [ ] **Step 1: 教学门**

Explain the action-method pattern, the three phases of `Player.Jump()` (condition, execution, notification), and why input buffering belongs in the input facade. Confirm writing mode.

- [ ] **Step 2: Add Jump input and buffer**

Add the action and binding in Unity, cache it in PlayerInputManager, and implement the timestamp window without allowing one physical press to trigger multiple jumps.

- [ ] **Step 3: Add Player.Jump and OnJump**

Implement the successful-jump path only for grounded players. The method must update velocity, switch to Fall, and invoke the player event in that order.

- [ ] **Step 4: Wire the three existing states**

Let Idle and Walk consume jump input through `Player.Jump()`. Fall handles airborne motion and transitions to a grounded state; the grounded state consumes a still-valid buffered press on its next step without adding a fourth state.

- [ ] **Step 5: Unity 编译检查**

Confirm the input asset imports, all assemblies compile, and no `JumpPlayerState` file or type exists.

- [ ] **Step 6: 学习回顾**

User explains why Jump is a command/action, why Fall is a state, and how a buffered press survives until the next valid grounded check.

### Task 6: Slice 4 第三人称相机

**Files:**
- Modify: `Assets/Examples/Input Actions/Player Input Actions.inputactions`
- Modify: `Assets/Scripts/Gameplay/Player/PlayerInputManager.cs`
- Create: `Assets/Scripts/Gameplay/ThirdPersonCamera.cs`

**Interfaces:**
- The input asset adds `Look` as a Vector2 action and `Pause` as a Button action. Pause is exposed for the agreed minimal action set but has no pause-system behavior in Day 3.
- `PlayerInputManager` exposes look, zoom, and pause facade methods without exposing raw InputAction objects to the camera or Player states. `GetZoom()` reads mouse-wheel input inside the facade and does not add a fifth action.
- `ThirdPersonCamera` consumes a target Transform, look direction, zoom amount, and smoothing parameters. It follows the target, orbits around it, clamps vertical rotation, applies zoom limits, and uses damping. It does not read Player state or change Player state.

- [ ] **Step 1: 教学门**

Explain why the camera is a separate Gameplay module, why it consumes the input facade rather than the asset directly, and why it must not participate in the Player state machine. Confirm writing mode.

- [ ] **Step 2: Add Look and Pause action definitions**

Add only the agreed actions and bindings to the Day 3 asset. Leave the Unity template asset unchanged.

- [ ] **Step 3: Implement camera follow, orbit, zoom, and damping**

Create the camera script with serialized target and tuning fields. Keep camera math local to the camera module and avoid adding camera knowledge to Entity or Player state classes.

- [ ] **Step 4: Unity 编译检查**

Confirm the Gameplay assembly compiles and the camera script has no dependency on Core state-machine types.

- [ ] **Step 5: 学习回顾**

User explains the camera’s input path and why camera behavior is not a Player state.

### Task 7: Slice 5 场景组装与统一 Play Mode 验收

**Files:**
- Create through Unity Editor: `Assets/Scenes/Day3CharacterTest.unity`
- Use: `Assets/Examples/Player Stats/PlayerStats.asset`（Slice 1 已创建；Slice 5 时确认引用与 jumpHeight 值）
- Use: `Assets/Examples/Input Actions/Player Input Actions.inputactions`
- Use: the completed Player, state manager, input manager, stats manager, and `ThirdPersonCamera` components

**Interfaces:**
- The test scene contains a Player with CharacterController, Player, PlayerStateManager, PlayerInputManager, and PlayerStatsManager; a configured PlayerStats asset; a ground plane; at least one raised platform; and a third-person camera.
- The PlayerStatsManager’s `current` asset is non-null before entering Play Mode.
- The input manager references the Day 3 asset, not the Unity template asset.

- [ ] **Step 1: 教学门**

Explain how serialized asset references connect the runtime objects and why scene setup is intentionally delayed until all code slices compile. Confirm writing mode for the scene setup.

- [ ] **Step 2: Build the test scene**

Create the Player, ground, raised platform, camera, and default PlayerStats asset. Assign references in the Inspector and save the scene under the exact path above.

- [ ] **Step 3: Run the final Play Mode checklist**

Verify all of the following in one session:

1. Player starts in Idle.
2. Movement input changes Idle to Walk.
3. Releasing movement returns Walk to Idle after deceleration.
4. Leaving the ground changes the state to Fall.
5. Gravity and ground snap behave correctly.
6. Jump changes the state from Idle or Walk to Fall without a Jump state.
7. A buffered jump triggers on the first valid landing window.
8. Ground enter/exit and OnJump events fire once per transition/action.
9. The camera follows, orbits, zooms, and damps without changing Player state.
10. The Unity Console has no new errors or warnings caused by Day 3.

- [ ] **Step 4: Record the learning result**

User explains the final input, state, stats, physical execution, and event notification paths, then independently changes one movement or jump value in the ScriptableObject and repeats the relevant Play Mode check.

## Plan Self-Review

- All approved architecture decisions map to Tasks 1-7: CRTP and template method in Task 1, component state manager in Tasks 1-2, ScriptableObject data in Task 3, input facade in Tasks 3 and 5-6, three states and action-based jump in Tasks 2-5, camera in Task 6, Play Mode acceptance in Task 7.
- The obsolete historical design is not used; no task creates `JumpPlayerState`, the old Core state-machine role, or the old `PlayerStats` ordinary class.
- The default Unity input asset is preserved and the Day 3 asset has one explicit path and one final action set.
- Every subsequent task uses types introduced by an earlier task, and every task ends with a compile or Play Mode learning gate.
- No unit-test task is included because the approved role-layer testing strategy is compile checks, debug logs, and Play Mode verification.
