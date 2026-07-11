# Pulse Arena

> A mobile-first 3D arena brawler where you fight with a **rope**, not a gun: lasso an enemy, wind up the
> spin, and fling them out of the ring or down a pit. Built in **Unity 6** to showcase a clean, fully-tested,
> DI-driven game architecture — and an end-to-end solo pipeline (code, 3D models, UI art).

---

## About this project

Pulse Arena is a **portfolio project**. The goal isn't "ship a huge game" — it's to show *how I build one*.
Specifically, it demonstrates:

- **A testable, extensible architecture** — strict one-directional assembly layering, dependency injection,
  an actor pattern that cleanly separates *coordination / logic / per-frame behaviour*, and config-driven
  design with **zero gameplay magic numbers in code**.
- **My end-to-end workflow** — I build it largely solo and procedurally: gameplay code, 3D models, UI art,
  and a unit-test suite.
- **My usual toolset** — the packages and patterns I reach for on real Unity projects.

If you're reviewing this, the parts worth your time are **[Architecture](#architecture)** and
**[Testing](#testing)** — that's the point of the repo.

## The game

You drop into a hostile tournament ring swarming with enemies. You have no weapon — you have a **rope**:

- **Lasso** an enemy and **spin** them around you. The longer you wind up, the more the rope tenses
  (and the louder it screams before it snaps).
- **Fling** them — into other enemies, out of the ring for a **ring-out**, or straight down an **arena pit**
  that opens and swallows them.
- Grab **health orbs**, dodge **turret** fire, chain kills into a **combo multiplier**, and unleash an
  **ultimate shockwave** once the super meter fills.
- Survive the **waves**. The arena doesn't wait.

Physics-driven and built to feel juicy — camera kick, bullet-time on big launches, screen shake, haptics.

## Built with

| Area | Tech |
| --- | --- |
| Engine / render | **Unity 6** (6000.4.0f1) · Universal Render Pipeline |
| Dependency Injection | **Zenject / Extenject** — two-tier (global `ProjectContext` + per-match `SceneContext`) |
| Input | Unity **Input System** — keyboard **and** on-screen touch controls |
| Camera | **Cinemachine** |
| Navigation | **NavMesh** / AI Navigation (`NavMeshAgent`) |
| Tweening | **DOTween** |
| Text | **TextMeshPro** |
| **3D models** | **ProBuilder** — every actor & prop modeled procedurally, in-editor |
| **UI / 2D art** | Generated via the **fal.ai** image-generation API |
| Testing | **Unity Test Framework** (NUnit) · **NSubstitute** for mocking |

## Art & assets

Almost everything is **hand-built, not bought**:

- **Player, enemies, pits, turrets, health orbs** — modeled procedurally in **ProBuilder** and tinted per
  enemy type. No external character or prop assets.
- **UI / 2D art** — generated through the **fal.ai** image API against a locked casual-RPG art direction.
- The **one imported set is the arena environment** — the grassy tournament "battle field" (ground tiles,
  rocks, bones, skulls) that dresses the ring.

## Architecture

The project is organised so gameplay logic can be **tested in isolation** and features can be added **without
touching unrelated systems**.

- **One-directional assembly layering.** Dependencies only ever point up:
  `Data ← Core ← Game ← UI ← Bootstrap`. `Data` depends on nobody; `Bootstrap` (the composition root) is the
  only assembly that sees everything. Gameplay never references UI directly — it talks through interfaces bound
  in the composition root.
- **Two-tier DI (Zenject).** A global `ProjectContext` holds cross-scene singletons (input, audio,
  score / combo / super meter, mechanical pause). A per-match `SceneContext` holds that match's factories,
  spawners and world builder — so every run starts from a clean slate and nothing leaks between matches.
- **Actor pattern.** Each gameplay actor is a *thin coordinator*: the `MonoBehaviour` owns only Unity
  lifecycle, its public API + events, and DI. Every per-object concern becomes a plain-C# helper, and all
  per-frame behaviour lives in **FSM state classes**. (The enemy is the reference — a controller + focused
  collaborators + seven states behind one lean context handle.) This split is exactly what makes the logic
  unit-testable.
- **Mechanical pause.** Pausing never touches `Time.timeScale`. Each system caches and restores its *own*
  engine state, so on resume music continues from the same sample, animations from the same frame, and physics
  from the same velocity.
- **Config-driven.** All feel/balance lives in ScriptableObjects behind a single `GameSettings` facade — there
  are no gameplay magic numbers in code. Swapping an `.asset` re-tunes the game without recompiling.

## Testing

**177 automated tests** covering the pure-logic layer:

- **EditMode (176)** — the pure-logic layer in isolation: health/damage rules, cooldowns, rope-tension math,
  the object pool, the pause / score / combo / super-meter services, the actor FSM, enemy timers & registry,
  and camera FX. Uses **NSubstitute** to fake interface dependencies.
- **PlayMode (1)** — a physics smoke test confirming the runtime loop is live.

Tests mirror the code layout (`Assets/Tests/EditMode/<System>/`).

Run locally: **Window → General → Test Runner → Run All**.

## Getting started

**Requirements:** Unity **6000.4.0f1** (Unity 6). Packages restore automatically on first open.

1. Clone the repo and open the `Pulse Arena` sub-folder as the project in Unity Hub.
2. Open `Assets/Scenes/Boot.unity` — the composition root that boots the flow **Boot → Main Menu → Game**.
3. Press **Play**; you land in the main menu — hit **Play** there to start a match.

### Controls

| Action | Keyboard / Mouse | Touch |
| --- | --- | --- |
| Move | `W A S D` / arrows | on-screen stick |
| Lasso — grab & wind up | hold `E` or Right Mouse | lasso button |
| Fling the grabbed enemy | release the lasso, or `Space` / Left Mouse | button |
| Dash / dodge | `Left Shift` | dash button |
| Ultimate (shockwave) | `Q` | ultimate button |

## Project structure

```
Pulse Arena/
└─ Assets/
   ├─ Scripts/         Data · Architecture (Core) · Game · UI · Bootstrap   (one asmdef per layer)
   ├─ Tests/           EditMode (176) + PlayMode (1)  — mirrors the code layout
   ├─ Prefabs/         ProBuilder-built actors, arena, pits, turrets, orbs, HUD
   ├─ Settings/        ScriptableObject configs (player / enemy / combat / level / presentation)
   └─ Scenes/          Boot · MainMenu · Game
```
