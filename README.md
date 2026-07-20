# Sling Ring

## 🎮 Gameplay trailer

https://github.com/user-attachments/assets/2124a6f1-7ef8-433e-9007-b14c656ac37c

---

> A mobile-first 3D arena brawler where you fight with a **rope**, not a gun: lasso an enemy, wind up the
> spin, and fling them into other enemies, into the walls, or down a pit. Built in **Unity 6** to showcase
> a clean, fully-tested, DI-driven game architecture, with art assembled pragmatically (AI-generated,
> ProBuilder, Asset Store) so the engineering stays the focus.

---

## About this project

Sling Ring is a **portfolio project**. The goal isn't "ship a huge game" — it's to show *how I build one*.
Specifically, it demonstrates:

- **A testable, extensible architecture** — strict one-directional assembly layering, dependency injection,
  an actor pattern that cleanly separates *coordination / logic / per-frame behaviour*, and config-driven
  design with **zero gameplay magic numbers in code**.
- **My end-to-end workflow** — gameplay code and a unit-test suite by hand, with art assembled pragmatically
  (AI-generated characters, ProBuilder props, a Unity Asset Store environment, generated UI) so the time goes
  into the engineering.
- **My usual toolset** — the packages and patterns I reach for on real Unity projects.

If you're reviewing this, the parts worth your time are **[Architecture](#architecture)** and
**[Testing](#testing)** — that's the point of the repo.

## The game

You drop into a hostile tournament ring swarming with enemies. You have no weapon — you have a **rope**:

- **Lasso** an enemy and **spin** them around you. The longer you wind up, the more the rope tenses
  (and the louder it screams before it snaps).
- **Fling** them — into other enemies, hard into the **arena walls**, or straight down an **arena pit**
  that opens and swallows them.
- Grab **health orbs**, dodge **turret** fire, chain kills into a **combo multiplier**, and unleash an
  **ultimate shockwave** once the super meter fills.
- Survive the **waves**. The arena doesn't wait.

Progress a short **campaign** (levels unlock as you clear them, scored with **stars** for the health you keep)
or chase a high score in **endless survival** — with a first-run **onboarding** that teaches the rope.

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
| **3D — characters** | **AI-generated** meshes, rigged + animated (player, enemies) |
| **3D — props / arena** | **ProBuilder** (pits, turrets, primitives) · **Unity Asset Store** arena pack |
| Animation | Unity **Animator** — per-character state machines (idle / run / attack / death / …) |
| **UI / 2D art** | Generated via the **fal.ai** image-generation API |
| Testing | **Unity Test Framework** (NUnit) · **NSubstitute** for mocking |

## Art & assets

Art is assembled **pragmatically** — the engineering is the point, so each asset takes the cheapest path that
looks right:

- **Characters** (player + enemies) — **AI-generated** 3D meshes, rigged and animated (idle / run / attack /
  death / …) and driven by per-character Animator state machines. The player mesh carries a higher triangle
  count than a shipping mobile budget would allow — left as-is on purpose, since this is a portfolio piece,
  not a production build.
- **Pits, turrets and simple props** — modeled in **ProBuilder**, in-editor.
- **Arena environment** — a **Unity Asset Store** pack (the grassy tournament ring: ground tiles, rocks,
  bones, skulls).
- **UI / 2D art** — generated through the **fal.ai** image API against a locked casual-RPG art direction.

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
  collaborators + a dozen states behind one lean context handle.) This split is exactly what makes the logic
  unit-testable.
- **Mechanical pause.** Pausing never touches `Time.timeScale`. Each system caches and restores its *own*
  engine state, so on resume music continues from the same sample, animations from the same frame, and physics
  from the same velocity.
- **Config-driven.** All feel/balance lives in ScriptableObjects behind a single `GameSettings` facade — there
  are no gameplay magic numbers in code. Swapping an `.asset` re-tunes the game without recompiling.

## Testing

**200 automated tests** covering the pure-logic layer:

- **EditMode (199)** — the pure-logic layer in isolation: health/damage rules, cooldowns, rope-tension math,
  the object pool, the pause / score / combo / super-meter services, level progress + star rating, onboarding
  state, the actor FSM, enemy timers & registry, and camera FX. Uses **NSubstitute** to fake interface
  dependencies.
- **PlayMode (1)** — a physics smoke test confirming the runtime loop is live.

Tests mirror the code layout (`Assets/Tests/EditMode/<System>/`).

Run locally: **Window → General → Test Runner → Run All**.

## Getting started

**Requirements:** Unity **6000.4.0f1** (Unity 6). Packages restore automatically on first open.

1. Clone the repo and open the `Sling Ring` sub-folder as the project in Unity Hub.
2. Open `Assets/Scenes/Boot.unity` — the composition root that boots the flow **Boot → Main Menu → Game**.
3. Press **Play**; you land in the main menu — hit **Play**, then pick a campaign level or endless survival.

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
Sling Ring/
└─ Assets/
   ├─ Scripts/         Data · Architecture (Core) · Game · UI · Bootstrap   (one asmdef per layer)
   ├─ Tests/           EditMode (199) + PlayMode (1)  — mirrors the code layout
   ├─ Prefabs/         Player · Enemies · Arena · Pickups · Turrets · UI · VFX  (sorted by domain)
   ├─ Settings/        ScriptableObject configs (player / enemy / combat / level / presentation)
   └─ Scenes/          Boot · MainMenu · Game
```
