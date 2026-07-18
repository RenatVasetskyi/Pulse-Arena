# Pulse Arena — Project Guide (CLAUDE.md)

Mobile-first (portrait) 3D arena game built in **Unity 6** with **URP**. The player is a lasso-slinger who grabs enemies with a rope/slingshot and flings them out of a ring or into pits. This file is the contract for how to reason about, ask about, and extend this codebase. **The two things that matter most here are architecture and code style — treat both sections as law.**

**Tech stack:** Unity 6 · URP · **Zenject** (DI) · **new Input System** · **Cinemachine** · **NavMesh** · **DOTween** · **TextMeshPro**. Config lives in ScriptableObjects; there are no gameplay magic numbers in code.

---

## 1. Collaboration rules (read first)

- **Language:** talk to Renat in **Ukrainian**; keep technical terms in English (`IPauseService`, `SceneContext`, `readonly`, …).
- **Report format** for every change, in Ukrainian: **`що змінив / файли / як тестував / що перевірити вручну`**.
- **Git:** the assistant **NEVER** commits or pushes. Renat owns every commit. Stage nothing, run no `git commit`/`git push`.
- **Art division of labor:** Renat owns all 3D — characters are **AI-generated** (rigged + animated FBX in `Assets/Generated/Characters/`), pits/turrets/simple props are **ProBuilder**, and the arena environment is a **Unity Asset Store** pack (`Assets/Brawl Arena/`). **Nothing is authored in Blender.** The assistant generates **UI / 2D art only** (fal.ai pipeline via Unity `generate_image`). Do not attempt to author 3D models.
- **Method-level SRP is a hard rule** (see §4). Every method does one job.

---

## 2. Architecture

### 2.1 Assembly layering (asmdef) — dependencies point one direction

```
PulseArena.Data        (rootns Data; no project refs — pure data, UnityEngine/System only)
   ▲
PulseArena.Core        (rootns Architecture; refs Data, InputSystem, UGUI)
   ▲
PulseArena.Game        (refs Data, Core, Zenject, DOTween, Cinemachine)
   ▲
PulseArena.UI          (refs Data, Core, Game, DOTween, TMP, InputSystem, UGUI)
   ▲
PulseArena.Bootstrap   (refs Data, Core, Game, UI, Zenject, InputSystem, UGUI) — the composition root, the ONLY assembly that sees everything
```

Editor tooling lives outside runtime asmdefs: `Assets/Scripts/Editor/ActorVisualBaker.cs`.

**Rule:** never add an "upward" reference. `Data` knows nothing about anyone. Gameplay (`Game`) never references `UI` — it talks to UI through interfaces bound in `Bootstrap` (`IScorePopupService`, `IWorldHealthBar`, `ILoadingScreen`, `ISceneLoader`). If you need gameplay→UI, add an interface in `Core`/`Game` and implement it in `UI`.

### 2.2 Directory map (`Assets/Scripts/`)

```
Data/                      GameSettings facade + all [Serializable] *Data bags + Configs/ sub-config SOs
  Configs/                 PlayerConfig, EnemyConfig, CombatConfig, LevelConfig, PresentationConfig
Architecture/
  Services/                Global service impls (AudioService, InputService, PauseService, ComboService…)
    Interfaces/            I* contracts for every service
  Services/SceneLoader.cs  ISceneLoader impl
  States/                  App-flow FSM: StateMachine, LoadGameState, Interfaces/
Bootstrap/                 Composition root: BootstrapInstaller, ServiceInstaller, GameInstaller,
                           GameWorldBuilder, GameFlowController, HudPresenter, GameplayFeedbackDirector,
                           PauseController, SettingsController, LoadingScreen, BootstrapState, LoadMainMenuState
Game/
  Player/                  PlayerController + PlayerMovement/PlayerDash/PlayerUltimate/PlayerFactory + States/
  Enemy/                   EnemyController + collaborators + EnemyFactory/Spawner/Registry + States/
  Combat/                  EnemySlingshot + RopeTension/RopeRenderer/EnemyTargetFinder/SnapBurstEffect/marker
  Cameras/                 BattleCamera + CameraShaker/CameraKickFx/CameraZoomController
  Common/                  ActorHealth, HitFlash, Cooldown, ActorGroundingUtility, ActorPhysicsUtility,
                           StateMachine/ (ActorState/ActorStateMachine/IActorState)
  Arena/                   ArenaFactory, PitFactory, Pit
  Pickups/                 HealthOrbPickup + OrbIdleAnimator/OrbCollectFeedback + PickupFactory
  Pooling/                 ComponentPool<T>
  Scene/                   GameSceneReferences (arena-wiring seam)
  Spawning/                SafeSpawnFinder, PitPlacementFinder, PickupSpawner, PitSpawner (+ I* alongside)
  Visuals/                 PlayerPrimitiveVisual, EnemyPrimitiveVisual
UI/
  Hud/                     GameHud facade + Hud*View sub-views + touch controls + UiTween
  MainMenu/ Pause/ Settings/ Loading/   passive Views
  (root)                   GameOverView, ScorePopupService, FloatingScoreText, WorldHealthBar
Editor/                    ActorVisualBaker.cs
```

> **Namespaces mirror folders and are block-scoped** — with a documented exception in `Bootstrap/`, where files sit in namespaces `Bootstrap`, `Game.Scene`, `Architecture.Services`, or `Architecture.States` depending on ROLE, not directory (e.g. `HudPresenter`, `GameWorldBuilder`, `GameFlowController`, `PauseController` are `namespace Game.Scene`; `BootstrapState`/`LoadMainMenuState` live physically in `Bootstrap/` but are `namespace Architecture.States`, matching `LoadGameState` in `Architecture/States/`).

### 2.3 Zenject DI — two-tier scoping is the core decision

There are **two containers**. Getting the scope right is the single most important architectural rule.

**ProjectContext (global, cross-scene singletons)** — hosted in the **Boot scene** (`SceneName.Boot = "Boot"`, `Data/SceneName.cs`), bound by `Bootstrap/BootstrapInstaller.cs` + `Bootstrap/ServiceInstaller.cs`:
- FSM: `IStateMachine` + `BootstrapState` / `LoadMainMenuState` / `LoadGameState`.
- Services: `GameSettings` (`FromScriptableObject`), `CoroutineRunner`, `EventSystem` (`FromInstance`; an `InputSystemUIInputModule` is `AddComponent`'d onto the same GameObject but is **not** bound/resolvable), `LoadingScreen`, `SceneLoader`, `InputService`, `ScoreService`/`ComboService`/`SlowMoService`/`SuperMeterService`/`ScorePopupService`, `SettingsService`+`SettingsController`, **`PauseService`**, `AudioService`.

**SceneContext (per-match, game scene only)** — bound by `Bootstrap/GameInstaller.cs`. The SceneContext's parent is the ProjectContext, so scene objects resolve both scopes; the ProjectContext never sees scene bindings.
- Factories (`BindFactories`): `IEnemyRegistry`, `IArenaFactory`, `IPlayerFactory`, `IEnemyFactory`, `IPickupFactory` (five bindings).
- Spawners (`BindSpawners`): `IEnemySpawner`, `IPickupSpawner`, `IPitSpawner` — **plus `IPitFactory`**, which is bound here (not in `BindFactories`) alongside `IPitSpawner`.
- World composition: `GameplayFeedbackDirector`, `HudPresenter`, `GameFlowController`, and **`GameWorldBuilder`** (`NonLazy`) (`BindWorldComposition`).

**Why the split:** factories are scene-scoped so the `DiContainer` each one captures is the SceneContext one — `InstantiatePrefabForComponent` then resolves scene-scoped deps on spawned actors. `IEnemyRegistry` is scene-scoped = a fresh registry per match. Binding these on ProjectContext would break resolution on spawned enemies/players. This is documented in `GameInstaller`'s XML doc; do not move factories up.

**Load-bearing ordering gotchas:**
- In `ServiceInstaller.InstallBindings()`, `BindPauseService` and `BindSettingsService` **must** run before `BindAudioService`, which `Container.Resolve`s both to call `AudioService.Initialize(...)`. Reordering breaks boot.
- **`PauseService` is deliberately ProjectContext-scoped**, not scene — the global `AudioService` and per-scene pausables share one mechanical pause. Do not move it to SceneContext.

**Binding style:** one `Bind<Thing>()` private method per binding, called in an ordered `InstallBindings()`. Fluent chains one call per line: `Container.Bind<IFoo>().To<Foo>().AsSingle().NonLazy()`. Use `.NonLazy()` only when the object must exist without being injected (self-subscribing services, `GameWorldBuilder`).

### 2.4 App-flow state machine (hand-rolled, not Zenject-driven)

`Architecture/States/StateMachine.cs` holds a `Dictionary<Type, IExitableState>`; `Enter<TState>()` exits the active state then `Enter()`s the new one. Contracts: `IExitableState{Exit()}` → `IState{Enter()}` → `IStateMachine{AddState, Enter}`.

**Boot with no entry-point MonoBehaviour:** the app starts in the **Boot scene**, which hosts the ProjectContext. `BootstrapInstaller` binds itself with `BindInterfacesTo<BootstrapInstaller>().FromInstance(this).AsSingle().NonLazy()`, so the ProjectContext kernel calls its `IInitializable.Initialize()`, which registers states and calls `Enter<BootstrapState>()`.

**Flow:**
```
[Boot scene → ProjectContext]
BootstrapState (set targetFrameRate) 
  → LoadMainMenuState (load menu scene, spin up MainMenuPresenter, play menu music)
    → [Play button] LoadGameState (pure "which scene" step — loads GameSceneName, then does nothing)
       → game-over/quit → Enter<LoadMainMenuState>()
```

**Crucial:** the match is **NOT owned by the FSM**. `LoadGameState` only loads the scene; all match composition/teardown lives in the game SceneContext. Every state entered must be **both** bound (`BindStates`) **and** `AddState`-registered (`RegisterStates`) — `StateMachine` throws on duplicate registration and on entering an unregistered state.

Scene changes go through `ISceneLoader`/`SceneLoader`: shows loading screen, `LoadSceneAsync` with `allowSceneActivation=false`, drives progress to 0.9, enforces `GameSettings.MinLoadingScreenTime`, activates, fires `onLoaded`, hides. Skips if already on the target scene.

### 2.5 Match lifecycle — `IInitializable`/`IDisposable` on the SceneContext

`Bootstrap/GameWorldBuilder.cs` is bound `BindInterfacesAndSelfTo<GameWorldBuilder>().AsSingle().NonLazy()`. Because it is the SceneContext's own object, the SceneContext kernel runs its lifecycle automatically:
- **`Initialize() → Build()`** on scene load: reset session → **spawn arena** (via `IArenaFactory`) → **validate** `GameSceneReferences` → preload pools → spawn player (via `IPlayerFactory`) → `Bind` `HudPresenter`/`GameplayFeedbackDirector`/`GameFlowController` → initialize + start spawners.
- **`Dispose() → Teardown()`** on scene unload: unbind collaborators, stop spawners, `_enemyFactory.Clear()`, `Object.Destroy(_arena)`, reset `Time.timeScale = 1`.

No state manages match teardown; cleanup can't be forgotten. `Build` is **staged with a guard** — it early-returns if `SpawnArena()` fails (arena prefab missing `GameSceneReferences`/`BattleCamera`), leaving `_player` null; later stages must not run. Build order is load-bearing (arena NavMesh must bake before spawners need `_player.transform` + arena center).

`GameWorldBuilder` is a **coordinator + focused collaborators**. It hands the player/camera/HUD to three collaborators via a uniform `Bind(...)/Unbind()` pair; each subscribes in `Bind` and symmetrically unsubscribes + destroys instantiated objects in `Unbind`:
- **`GameFlowController`** — owns win (`AllWavesCleared`)/lose (player `Died`)/`EndGame`, game-over screen, pause (creates `PauseController`), restart (raw `SceneManager.LoadScene` reload), quit-to-menu (`Enter<LoadMainMenuState>`).
- **`HudPresenter`** — MVP presenter; instantiates `GameHud`, binds it, registers it as `ITouchInput`, translates model events (combo/super/wave/rare pickup) into HUD calls.
- **`GameplayFeedbackDirector`** — turns gameplay events (health/dash/lasso/ultimate/waves/victory) into SFX / camera shake / bullet-time / haptics.

Restart is a raw active-scene reload (not through the FSM) — safe because Unity defers the load to end of frame; the SceneContext rebuild resets everything.

### 2.6 The actor pattern — coordinator MonoBehaviour + plain-C# helpers + FSM

**This is THE reference pattern of the codebase.** A gameplay actor is a **thin MonoBehaviour coordinator** that owns only: Unity lifecycle, the public API + C# events, Zenject injection, and mechanical pause. All per-object concerns become plain-C# helper collaborators, and all per-frame behavior lives in FSM state classes.

**`Game/Enemy/EnemyController.cs` — the canonical example (the most decomposed actor):**
- Constructs helpers as `private readonly X _x = new();` fields (`EnemyMovement`, `EnemyImpact`, `EnemyCollisionHandler`, `EnemyTimers`, `GroundRecoveryController`, `RingoutHandler`, `EnemyHealthBarPresenter`, plus shared `ActorHealth`/`HitFlash`).
- Wires them in `Awake` via one-job methods (`ResolveComponents(); InitializeCollaborators(); SetupVisualsAndFlash(); BuildContext();`).
- Delegates every per-frame behavior to **seven `ActorState` subclasses** (`EnemyChaseState`, `EnemyKnockbackState`, `EnemyGroundRecoveryState`, `EnemyGrabbedState`, `EnemyRingoutState`, `EnemyDeadState`, `EnemyStasisState`) in `Game/Enemy/States/`.
- States reach collaborators + controller callbacks through **one lean handle, `EnemyContext`** (sealed): direct collaborator refs + the three shared mutable flags (`IsGrabbed`, `IsImpactProjectile`, `NeedsGroundRecovery`) + `Func`/`Action` callbacks (target reads, `IsDead`, `ChangeToChase/GroundRecovery`, `ReturnToPool`, `StartDeathReturn`, `StopForDeath`, `ResolveRingout`). One source of truth.
- FSM = `Game/Common/StateMachine/ActorStateMachine` (shared with the player). States are lazily built once in `EnsureStateMachine()`; transitions go through `internal ChangeToXState()` methods each guarded `if (!_isDead)`.

**`Game/Player/PlayerController.cs`** is the same shape (thinner): readonly interface-typed collaborators (`IPlayerMovement`, `IPlayerDash`, `IActorHealth`, `HitFlash`), an `ActorStateMachine` of `PlayerMoveState`/`PlayerDashState`/`PlayerHitState`/`PlayerDeadState`, and **thin-delegate methods** (`internal void MoveByInput() { _movement.MoveByInput(); }` — block-bodied, never `=>`) that the states call. Helpers own the **how**; states own the **when**; the controller owns lifecycle + events + transitions.

**Two FSM flavors — don't confuse them:** the player and enemy use the polymorphic `ActorState`/`ActorStateMachine` hierarchy. `EnemySlingshot` (combat) instead uses a **private `enum LassoState`** with if-branches in `Update`/`FixedUpdate` — a lighter FSM for the linear grab sequence.

**DI at the actor boundary only:** MonoBehaviours use `[Inject] public void Construct(...)` for services + `GameSettings`; helpers are pure objects wired via a plain `Initialize(...)`. Everything reads feel/balance from `GameSettings.*` ScriptableObject data.

**Load-bearing actor ordering (do not "simplify"):**
- In `EnemyController.Knockback`/`Launch`, `ChangeToXState()` runs **before** `AddForce`/velocity — the state's `Enter` wakes the rigidbody first.
- The knockback timer ticks in **exactly one place** (`EnemyKnockbackState.FixedTick`), not in `EnemyTimers.TickFixed`.
- `EnemyStasisState` is intentionally **unreachable** today but kept re-armable — do not delete as dead code.
- `PlayerDeadState` is intentionally inert; every `ChangeTo*State` guards `!_isDead`.
- `PlayerController.Resume` restores the rigidbody **before** `_visual.SetPaused(false)` (the visual's move-blend reads `linearVelocity`).

### 2.7 Combat & camera

- **`Game/Combat/EnemySlingshot.cs`** — lasso orchestrator (MonoBehaviour, `IPausable`) running the `LassoState` FSM (Idle→Throwing→Wrapping→Pulling→Spinning) and spin/pull/launch physics. Delegates to `IRopeTension` (pure, unit-testable math), `IEnemyTargetFinder`, `ISnapBurstEffect`, `IHookTargetMarkerPresenter`, and `RopeRenderer`. Rebuilds an immutable `RopeFrame` struct each `Update` and passes it to `RopeRenderer.Render` (slingshot decides **what**, renderer knows **how**).
- **`Game/Cameras/BattleCamera.cs`** — `IBattleCamera` facade over Cinemachine; composites base rig + settings-coupled zoom + transient shake/kick each frame from `CameraShaker`/`CameraKickFx`/`CameraZoomController`. **Not bound in an installer** — `GameWorldBuilder` resolves it via `GetComponentInChildren<BattleCamera>()` off the arena prefab. `CameraZoomController` routes zoom through `ISettingsService` so +/- buttons and the settings slider share one persisted value.

### 2.8 Factories + `ComponentPool<T>`

Every factory wraps Zenject prefab instantiation so DI reaches every spawned MonoBehaviour, pulling prefabs from `GameSettings.Prefabs`:
- `ArenaFactory` → `Container.InstantiatePrefab` (throws if unassigned).
- `PlayerFactory` → `InstantiatePrefabForComponent<PlayerController>`, snaps to ground, then `AddCombatComponents` adds+injects `EnemySlingshot` at runtime.
- `EnemyFactory` → owns a `ComponentPool<EnemyController>`; `Create` DI-instantiates once, then `pool.Get` + place + snap + `Initialize`; `Preload` warms; `Clear` destroys the pool root on teardown.
- `PitFactory`/`PickupFactory` → `InstantiatePrefabForComponent<T>` + `Initialize(...)`; log + return null if prefab unassigned (spawners null-check).

`Game/Pooling/ComponentPool.cs` only tracks membership (`_active` HashSet + `_inactive` Queue). **Reset lives in the caller-supplied release delegate**, not the pool — a new pooled type must supply a release action that fully resets state or stale state leaks across reuse. `EnemyFactory.Clear()` must **not** create/reparent GameObjects (runs mid scene-unload; doing so trips Unity's "objects not cleaned up" error).

### 2.9 Config facade (`GameSettings` → sub-config SOs)

`Data/GameSettings.cs` is a single facade ScriptableObject injected into virtually every gameplay system. It holds five swappable sub-config SO refs (`_player`, `_enemy`, `_combat`, `_level`, `_presentation`) and re-exposes their contents through **~20 read-only expression-bodied pass-through properties** (`PlayerData => _player.Data`, `SlingshotData`, `Feel`, …). Consumers keep calling `gameSettings.PlayerData` and never know the data was split across assets.
- Sub-configs: `PlayerConfig`, `EnemyConfig`, `CombatConfig`, `LevelConfig`, `PresentationConfig` (menuName `Pulse Arena/Configs/...`).
- The only logic: `GetEnemyType` lazily builds+caches an `EnemyTypeId → EnemyTypeData` lookup, returns `EnemyTypeData.Default` (a shared static) on miss, never null.
- Shared cross-actor game-feel (`FeelData`: RingoutHeight, HitFlash) deliberately lives once in `CombatConfig` so player and enemy can't drift.
- **Values live in `.asset` files, not code.** Changing a C# field default only affects newly-created assets. All five sub-config slots must be wired on `GameSettings.asset` or the pass-through properties NRE. `SlingshotData.EnemyLayer`/`ObstacleLayer` LayerMasks must be set in the Inspector (default 0 = Nothing silently breaks grab).

### 2.10 Mechanical pause — `IPauseService` / `IPausable` (NOT `Time.timeScale = 0`)

Pause freezes each system **mechanically** so music resumes from the same sample and animations from the same frame. `Architecture/Services/PauseService.cs` holds a `HashSet<IPausable>` and broadcasts `Pause()`/`Resume()` over a **copied List snapshot** (a pausable can unregister mid-broadcast) and **auto-Pauses** anything registered while already paused.

Each `IPausable` caches and restores its own engine-driven state:
- `AudioService` — `AudioSource.Pause()/UnPause()` (never `Stop`) → exact sample.
- `SlowMoService` — caches the scaled `Time.timeScale`/`fixedDeltaTime` and stops its ease coroutine; `Resume` restores and continues the dip. (SlowMo *drives* `timeScale` for bullet-time; PauseService does **not**.)
- Actors/spawners/pits/orbs — cache rigidbody velocity+gravity+kinematic then freeze; `Update`/`FixedUpdate` early-return on `_paused`, so FSMs and delta-timers freeze for free; NavMeshAgent uses `isStopped`+cached velocity (never `enabled=false`); DOTween `Pause()`/`Play()`.

**Only `EndGame` and the loading path touch `Time.timeScale`** (freeze at 0 on game-over; always restored to 1 in `Teardown`/`QuitToMenu`/`RestartScene`). `EndGame`/`QuitToMenu`/`RestartScene` all call `_pauseService.Unpause()` before changing timeScale or scene — a paused state must never cross a scene change or the game-over freeze. Register on init/spawn, **Unregister on teardown AND OnDestroy** (belt-and-suspenders); `Clear()` is the group safety net that `GameWorldBuilder.Teardown` relies on.

### 2.11 UI — passive Views + Presenters/Controllers (MVP)

Views (`Assets/Scripts/UI/**`) are passive MonoBehaviours: `[SerializeField]` refs, DOTween "juice", `event Action` buttons, and `Show/Hide/Bind/Set*` methods that translate model state into visuals. **No game decisions.** Logic lives in plain-C# Presenters/Controllers **outside** the UI namespace (`Bootstrap`, `Game.Scene`, `Architecture.Services`).

- **Views are never bound in Zenject** — they are `Object.Instantiate(GameSettings.Prefabs.XPrefab).GetComponent<T>()` by their owner; presenters are `new`'d. Only logic services are bound: `IScorePopupService → ScorePopupService`, `ISettingsController → SettingsController`.
- **`GameHud`** is a null-safe facade over **~13 serialized refs total (~9 sub-views + 4 touch controls)**; implements `ITouchInput` explicitly and registers via `IInputService.SetTouchInput(_hud)` so gameplay reads on-screen controls through the interface only.
- Two subscription styles: sub-views self-bind to a service/model (`HudHealthView.Bind(player)`, `HudScoreView.Bind(IScoreService)`) and unsubscribe in their own `OnDestroy`; `HudPresenter` subscribes to broader services and forwards via `GameHud.Set*`.
- **Freeze model split:** game-over uses `Time.timeScale=0` — tweens that must animate while frozen (game-over, pause, settings, loading) **must** use `.SetUpdate(true)`. In-game pause uses `IPauseService`, not timeScale. Canonical MVP example: `MainMenuView` (passive) + `MainMenuPresenter` (`IDisposable`, subscribe in `Initialize`, mirror-unsubscribe in `Dispose`).

### 2.12 Spawning — finders (where) separated from spawners (when/how-many)

Geometry is a small finder new'd directly inside its spawner (**not DI-bound**): `SafeSpawnFinder`/`PitPlacementFinder` — `Initialize(context…)` then `bool TryFind(out position)` with a bounded try budget. The spawner (`PickupSpawner`, `PitSpawner`, `EnemySpawner`) owns cadence, cap, active-count bookkeeping, and `IPausable` via a `PausableWait` coroutine.
- Blocker mask = `ObstacleLayer.value | EnemyLayer.value`, probed with `Physics.CheckSphere(..., QueryTriggerInteraction.Collide)` — **triggers are included on purpose** so orbs don't spawn on pits/other orbs/walls.
- `EnemyRegistry` (scene-scoped, fresh per match) is a `Dictionary<Rigidbody, EnemyController>` reverse lookup so the impact sweep resolves collider→controller in O(1) without per-frame `GetComponent`.

---

## 3. Code style & conventions (enforced — follow exactly)

Authoritative sources: member ordering = `Pulse Arena.sln.DotSettings` (Rider `CSharpFileLayoutPatterns`, applied via **Rider Cleanup**); access modifiers = `.editorconfig` (`IDE0040` warning). **Run Rider Cleanup so a diff conforms.**

### Member ordering
Order every type to the Rider layout profile — don't hand-order, run Cleanup and let the profile decide. For **Unity types** (MonoBehaviour/ScriptableObject) the profile broadly yields:
1. Constants + static fields → 2. `[SerializeField]` fields (**declaration order, no alpha sort**) → 3. Instance fields → 4. Events (alpha) → 5. Properties (alpha) → 6. Constructors → 7. `[Inject]`/`Construct`/`Initialize` (pinned here) → 8. **Unity event functions in canonical order** (Awake, OnEnable, Start, Update, FixedUpdate, OnCollision*, OnTrigger*, OnDestroy) → 9. Public methods → 10. Private helpers → 11. Nested types.

**Non-Unity types** (services/presenters/states): identical minus the SerializeField and Unity-event groups. Large coordinators additionally use `// --- section ---` dashed banner comments (Unity lifecycle / public API / pool lifecycle / state machine + transitions). Exemplars: `Game/Enemy/EnemyController.cs`, `Game/Player/PlayerController.cs`.

> **Note on instance-field order:** the profile does not strictly put all `readonly` collaborators before non-readonly fields. `PlayerController` happens to lead with its four readonly collaborators, but the canonical `EnemyController` interleaves cached-state fields before *and* after its readonly collaborator block. **Don't hand-enforce "readonly-first" — whatever Rider Cleanup produces is correct.**

### Access modifiers & field qualifiers
- **DO** put an explicit access modifier on **every** member that can take one (enforced). The **only** sanctioned modifier-less members are interface members and **explicit interface implementations** — the `.editorconfig` exempts these because C# forbids modifiers on them (`CS0106`), e.g. `CoroutineRunner`'s `Coroutine ICoroutineRunner.StartCoroutine(...)` and `GameHud`'s `Vector2 ITouchInput.Move`.
- **DO** mark every write-once field `readonly`; construct collaborators inline: `private readonly EnemyMovement _movement = new();`. (Fields assigned in `Construct` after construction are **not** readonly.)
- `const` is `private const` PascalCase. Use `internal` deliberately for members only same-assembly states/collaborators call (`EnemyController.ChangeToChaseState`, `PlayerController.MoveByInput`). `static` for pure stateless helpers.
- **DON'T** omit modifiers (triggers `IDE0040`).

### Naming
- Private/instance **and serialized** fields: `_camelCase` (serialized fields are `[SerializeField] private`, never public inspector fields). Public members: PascalCase. Interfaces: `I`-prefixed. Enums: PascalCase type + members. Locals/params: plain `camelCase`.
- One public type per file, filename == type name. **Only sanctioned exception:** aggregate `[Serializable]` config data (`Data/GameSettings.cs` holds `GameSettings` + ~25 `*Data` classes). Serialized data-class fields are public PascalCase with initializers (`public float MoveSpeed = 6f;`).

### Comments
- **DO** give **every** class, interface, and enum an XML `<summary>` describing its architectural role (use `<see cref>`, `<paramref>`, `<c>`).
- **DO** add member XML or inline `//` **only** to explain WHY / a non-obvious invariant / an ordering requirement / an engine gotcha. Voice: present-tense, terse, senior-engineer.
- **DON'T** comment WHAT the code does. Routine getters/thin-delegates/obvious methods get no XML; interface members stay bare (the interface summary covers intent).

### Method-level SRP (hard rule)
- **DO** make every method do one job; a coordinator is a short ordered list of calls to single-purpose named helpers. Model: `EnemyController.Awake()` → `ResolveComponents(); InitializeCollaborators(); SetupVisualsAndFlash(); BuildContext();`. Helpers are 3–12 lines, named for their single effect.
- **DO** open methods with guard/early-return clauses (`if (_paused) return;`), then a blank line, then the one main action.
- **DON'T** cram multiple responsibilities into a method or inline logic a named helper should own.

### Interfaces
- **DO** depend on an interface for anything DI-bound, mocked/swapped, or crossing a system boundary. `I`-prefixed; normally in an `Interfaces/` subfolder that **is** part of the namespace. Small/local modules keep the interface beside the impl (`Game/Spawning` has no `Interfaces/` folder).
- Purely internal single-owner helpers stay concrete with no interface (`EnemyMovement`, `EnemyTimers`, `HitFlash`, `EnemyCollisionHandler`). States use the `IActorState`/`ActorState` abstract-base pair (empty virtuals → override only what you need).

### DI: Construct vs Initialize
- **MonoBehaviours:** method injection `[Inject] public void Construct(...)` receives only container-resolved services + `GameSettings` and does one-time wiring. A **separate** public `Initialize(...)` receives per-spawn/runtime data (target, type) passed by the spawner/pool. This split is firm.
- **Plain classes:** constructor injection with `readonly` fields, bound `.To<>().AsSingle()`.
- **Pooled/reusable collaborators:** parameterless `new()` + manual `Initialize(...)` (so they can reset and reuse: `ActorHealth.Initialize`, `EnemyMovement.Initialize`).
- **DON'T** do real work in a MonoBehaviour constructor; **DON'T** inject per-spawn data through `[Inject]`; **DON'T** scatter bindings outside installer `Bind<>()` methods.

### Namespaces & usings
- Block-scoped `namespace X { }` mirroring the folder path (never file-scoped).
- Usings: `System.*` first, then all others alphabetical. Alias UnityEngine collisions: `using Random = UnityEngine.Random;`. No `global using`.
- **`var` is BANNED** — zero occurrences in the codebase. Always write the concrete type (`Vector3 candidate = SampleRing();`).

### Lifecycle & pooling
- **DO** mirror every subscribe/register with an unsubscribe/unregister. For pooled MonoBehaviours pair `ResetForSpawn()` (spawn) with `PrepareForPool()` (return) as exact mirror images, and unregister from registry + pause in **both** `PrepareForPool` **and** `OnDestroy` (only one runs per path).
- **DO** reset the `_paused` flag (and all flags/timers/velocity/constraints) in `ResetForSpawn` — a stale flag makes a pooled object spawn frozen.
- **DO** guard optional/injected refs with null-conditional access (`_visual?.`, `_pauseService?.Register(this)`).
- **DON'T** pause with `Time.timeScale=0` or by disabling components/agents — cache and restore per-object state.

### Braces
- Omit braces on single-statement `if` bodies (brace-less one-liner + following blank line); keep Allman braces for multi-line blocks.
- **Methods are always block-bodied, never expression-bodied** — `void A() { Foo(); }`, not `void A() => Foo();` (enforced via `.editorconfig` `csharp_style_expression_bodied_methods = false`). Expression bodies (`=>`) stay allowed **only** on properties, indexers and accessors (e.g. the `GameSettings` pass-through properties, `Cooldown.Remaining`).

**Pointer files worth opening before writing code:** `Game/Enemy/EnemyController.cs` (the whole pattern), `Game/Player/PlayerController.cs`, `Architecture/Services/PauseService.cs`, `Game/Common/ActorHealth.cs`, `UI/MainMenu/MainMenuPresenter.cs`, `Bootstrap/ServiceInstaller.cs`, `Data/GameSettings.cs`, `Pulse Arena.sln.DotSettings`, `.editorconfig`.

---

## 4. How to extend (cookbook)

**Add a global service** → create `IFoo` in `Architecture/Services/Interfaces` (+`<summary>`), implement `Foo` in `Architecture/Services` (constructor injection, or `Initialize` if it's a MonoBehaviour), add `BindFooService()` to `ServiceInstaller` called from `InstallBindings` (`Bind<IFoo>().To<Foo>().AsSingle()`, `.NonLazy()` if it self-registers/has startup side effects). If it needs other services at bind time, place the call after theirs.

**Add an app-flow state** → class implementing `IState` in `Architecture.States`/`Bootstrap`, constructor-inject deps; in `BootstrapInstaller` add `Container.Bind<NewState>().AsSingle()` in `BindStates()` **and** `stateMachine.AddState(Container.Resolve<NewState>())` in `RegisterStates()`; transition with `_stateMachine.Enter<NewState>()`.

**Add a per-match system (spawner/factory/collaborator)** → bind it in `GameInstaller` (SceneContext) in the matching group. If it has a Bind/Unbind lifecycle, call them from `GameWorldBuilder.Build/Teardown`; if it's an independent `IInitializable`/`IDisposable`, use `BindInterfacesAndSelfTo(...).NonLazy()`.

**Add an enemy FSM state** → `Game/Enemy/States/EnemyXState.cs : ActorState` taking an `EnemyContext`; add a `private EnemyXState _xState;` field, construct it in `EnsureStateMachine()`, add `internal void ChangeToXState()` (calls `EnsureStateMachine()`, guards `!_isDead`). Drive behavior off `_context` flags/timers; never store per-frame state on the controller.

**Add an enemy TYPE** → add a value to the `EnemyTypeId` enum (contiguous explicit keys), add an `EnemyTypeData` element (**unique Id**) to `EnemyConfig.Types` in the Inspector, tune its multipliers/visual overrides. `GetEnemyType` picks it up automatically.

**Add a player ability** → plain-C# helper + `I<Name>` interface in `Game.Player[.Interfaces]` with `Initialize(...)` + `Tick`/action methods; add `readonly I<Name> _x = new <Name>();`, `Initialize` in `Awake`, tick in `Update`, add thin `internal` delegate methods for any state-driven action.

**Add a HUD widget** → `HudXView : MonoBehaviour` in `UI/Hud` with `[SerializeField]` refs + `Bind`/`SetX` (no game logic; use `UiTween`); add `[SerializeField] private HudXView _x;` to `GameHud` with a null-safe forwarder; in `HudPresenter` subscribe the driving service event in `Bind()` → forward to `GameHud`, unsubscribe in `Unbind()`; drop the component on the GameHud prefab.

**Add an overlay panel** → passive View exposing `event Action`s + `Show/Hide` (use `UiTween.OpenWindow/CloseWindow`, `.SetUpdate(true)`); plain Presenter/Controller subscribing to those events; add the prefab to `GameSettings.Prefabs`; `Object.Instantiate(prefab).GetComponent<View>()` from the owning state/controller; dispose (unsubscribe + Destroy) in the owner's `Exit`/`Unbind`.

**Add a tunable** → add a public field (default + optional `[Tooltip]`/`[Range]`/`[Min]`) to the relevant `[Serializable] *Data` class. It appears automatically on the owning sub-config asset; the consumer reads it via the existing `GameSettings` pass-through. A whole new data group needs a forwarding property on `GameSettings` (`public XxxData Xxx => _combat.Xxx;`) and `= new()` init on the sub-config.

**Make a system pausable** → implement `IPausable` (`Pause` caches whatever the engine keeps mutating, `Resume` restores it exactly), inject `IPauseService`, `Register(this)` on init/spawn, `Unregister(this)` on teardown/pool-return **and** `OnDestroy`. Rely on `GameWorldBuilder.Teardown` calling `IPauseService.Clear()` as a safety net.

**Add a gameplay→feedback reaction** → handler in `GameplayFeedbackDirector`, subscribe in `Bind()`, unsubscribe symmetrically in `Unbind()`. Do **not** add feedback to `HudPresenter` (HUD-visual state only).

**Show floating score** → depend on `IScorePopupService` (already bound) and call `Show(position, value)`. Never reference `FloatingScoreText`/UI directly.

---

## 5. Working with this repo

- **Config-driven — no magic numbers.** Never hardcode a gameplay number in logic. Add it to the relevant `*Data` SO reached via `GameSettings`. Editing a C# default only affects newly-created assets; existing `.asset` files keep their serialized values.
- **Mechanical pause is inviolable.** Never introduce `Time.timeScale=0` for pause or disable components/agents to freeze. Music must resume from the same sample, animations from the same frame (§2.10).
- **Tooling gotcha — Rider clobber:** if Rider is open during multi-file C# edits it can re-save stale buffers over disk edits. **Close Rider or disable save-on-focus-lost before multi-file edits.**
- **Tooling gotcha — Unity-MCP `execute_code` CodeDom:** with compiler `'auto'` it falls back to CodeDom (C# 6) — **no top-level `using` directives, fully-qualify types**, and `Object`/`Random` are ambiguous (qualify them).
- **`CoroutineRunner` recursion trap:** it implements `StartCoroutine`/`StopCoroutine` as **explicit** interface members (the one modifier-less-member case the `.editorconfig` sanctions). A public same-signature override would bind to itself → infinite recursion → StackOverflow → editor crash. Keep it explicit.
- **Git:** never commit/push — Renat owns every commit.
- **Art:** generate UI/2D only (fal.ai via `generate_image`); Renat owns all 3D (AI-generated characters · ProBuilder props · Asset Store arena) — **nothing in Blender**.
- **Report every change** in Ukrainian as `що змінив / файли / як тестував / що перевірити вручну`.
