# Pulse Arena

[![Tests](https://github.com/RenatVasetskyi/Pulse-Arena/actions/workflows/tests.yml/badge.svg)](https://github.com/RenatVasetskyi/Pulse-Arena/actions/workflows/tests.yml)

A mobile-first (portrait) 3D arena game built in **Unity 6 / URP**. You play a lasso-slinger who grabs
enemies with a rope and flings them out of the ring or into pits.

## Tech

**Unity 6** · URP · **Zenject** (DI) · new Input System · Cinemachine · NavMesh · DOTween · TextMeshPro.
Config lives in ScriptableObjects — no gameplay magic numbers in code.

## Architecture

Assembly layering points one direction (`Data ← Core ← Game ← UI ← Bootstrap`). Gameplay actors follow a
thin-coordinator pattern: a lean `MonoBehaviour` owns lifecycle + events + DI, per-object concerns live in
plain-C# helpers, and per-frame behaviour lives in FSM state classes. Pause is mechanical (`IPausable`,
never `Time.timeScale = 0`) so music resumes from the same sample and animations from the same frame.

## Tests

`177` automated tests run in the Unity Test Runner and on every push via GitHub Actions:

- **EditMode (176)** — pure logic in isolation: health/damage rules, cooldowns, rope-tension math, the
  object pool, the pause/score/combo/super-meter services, the actor FSM, enemy timers/registry and camera FX.
  Uses **NSubstitute** for interface dependencies.
- **PlayMode (1)** — a physics smoke test that the runtime loop is live.

Run locally: `Window → General → Test Runner → Run All`.
