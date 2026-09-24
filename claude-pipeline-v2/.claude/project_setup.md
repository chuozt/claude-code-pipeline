# Project Setup & Environment

---

## 1. Editor Environment & Version Safety

- **The exact Unity version in `CLAUDE.md` §1 is mandatory.** Before proposing any Unity API,
  check it against that version. If an API may have changed after the training cutoff,
  **warn clearly** and propose a way to check (a one-line `eval` probe through the bridge,
  after `/use-mcp`).
- Game code and logic live **only in the code root declared in `CLAUDE.md` §1**.
  Third-party SDKs are never modified.
- **The project path should contain no spaces** — some CLI/MCP tools, Gradle and build
  scripts mishandle them.

### Technical Standards For This Project

Filled in by the developer, from a `/project-overview` snapshot or by hand. Leave nothing
as `<...>` once the project starts.

| Item | Value | Notes |
|---|---|---|
| Render pipeline | `<...>` | |
| UI system | `<...>` | UGUI, UI Toolkit, or a third-party screen framework. Whatever is written here is the only UI system new code uses; agents, skills and the KB that recommend another one are overruled (CLAUDE.md §3 precedence) |
| Async | `<...>` | UniTask vs `Awaitable` — do not mix them in one project |
| Tween | `<...>` | DOTween / PrimeTween — `Kill()` required in `OnDisable`/`OnDestroy` regardless of which |
| DI | `<...>` | VContainer / Zenject / none — name the scopes if any |
| Input | `<...>` | New Input System vs the legacy `Input` class |
| Asset loading | `<...>` | `Resources` vs Addressables — if both, say which data uses which and why |
| Level format | `<...>` | readable/git-diffable in development; state the release format too |
| Reference device | `<...>` | |
| Target FPS | `<...>` | state the frame budget in ms |
| Localisation | `<...>` | how many languages; display strings still go through localisation even at one |
| Editor bridge | `<...>` | `unity` CLI and `com.unity.pipeline` versions pinned, or "not used" — both are beta, re-verify `mcp_unity.md` after an upgrade |

> If this project starts from a reusable template/framework layer (a "neutral placeholder
> game" meant to be replaced piece by piece, or a closed framework folder that must stay
> immutable), describe it here: which folders are replacement points, which are closed
> infrastructure, and what gate proves the closed part was not touched
> (e.g. `git diff -- <framework folder>` must produce no output). Leave this note out
> entirely if the project has no such layer.
>
> Genre reference project (if any): `<...>`.

---

## 2. Design Document Standard

Every mechanic design document **must have all 8 sections**:

1. **Overview** — one-paragraph summary
2. **Player Fantasy** — the feeling the player should get
3. **Detailed Rules** — unambiguous rules
4. **Formulas** — every formula, with variables defined
5. **Edge Cases** — the exceptional situations already handled
6. **Dependencies** — which systems it depends on
7. **Tuning Knobs** — the tunable variables
8. **Acceptance Criteria** — **verifiable** acceptance conditions

A missing section → the AI writes `UNDEFINED` and asks the designer. **Never fill it in.**
Template: `.claude/templates/GDD_Mechanic.md`

---

## 3. Commands & Pipeline

- **No plain CLI builds** outside the project's own build system.
- **The level editor is the only tool** for creating/editing levels. Never hand-edit a level file.
- Levels export in a readable, git-diffable format during development and an encrypted
  format for release.
- Every script-generated asset must ship with its `.meta`.

---

## 4. Scripting Defines

Gate features with `#if` (`ENABLE_LOGS`, `ENABLE_CHEAT`, SDK defines…).
When editing code inside an `#if`, **make sure it still compiles on both branches**
(with and without the define).

At minimum there should be:
- `ENABLE_LOGS` — enables `GameDebug`, Development builds only
- `ENABLE_CHEAT` — cheat menu, Development builds only

---

## 5. Reference Folder Structure

```
Assets/_Project/
├── Scripts/
│   ├── <Game>/
│   │   ├── Data/          ← POCOs, loaders   (pure C#)
│   │   ├── Gameplay/      ← MODEL            (pure C#, NO MonoBehaviour — rules/gameplay-code.md)
│   │   │   └── Mechanics/
│   │   ├── View/          ← MonoBehaviour, presentation (outside Gameplay/, or the model rule applies to it)
│   │   ├── Config/        ← ScriptableObjects
│   │   ├── Editor/        ← level tool, validator, simulator
│   │   └── Playtest/      ← bots, measurement scenarios
│   ├── UI/
│   ├── Manager/
│   └── Common/
├── ScriptableObjects/     ← config assets
├── Resources/             ← levels and other runtime-loaded data
├── Prefabs/  Scenes/  Art/  Audio/  FX/
└── (docs/features/ at the repo root holds living per-feature documentation)
```

---

## 6. Setting Up A New Project — Order Of Work

1. Copy the kit's `CLAUDE.md` + `.claude/` into the Unity project root
2. Delete `.claude/settings.local.json` if it came along, and gitignore it
3. Run `/project-overview` — a read-only snapshot of what is already in the project (it
   asks nothing, touches no Unity Editor bridge, writes nothing)
4. Fill the `<...>` placeholders in `CLAUDE.md`, `project_setup.md` §1 and `rules.md` §4
   from that snapshot, by hand or by asking Claude to draft them for confirmation
5. If the Unity Editor bridge will be used: install Unity CLI, run `unity pipeline install`
   in the project, then `unity mcp configure claude-code`, and confirm with `unity status`
   that it lists this project's path and the exact version
6. Create the `.asmdef` for the code root
7. Create the `GameDebug` wrapper + the `ENABLE_LOGS` define
