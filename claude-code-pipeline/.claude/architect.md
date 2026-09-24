# System Architecture

The overall architecture. Every technical proposal must match this document, or state
explicitly why it deviates.

---

## 1. Principle #1 — Separate Model From View

This is the most important architectural decision in the whole project. Getting it wrong
makes every later measurement, test and polish task several times more expensive.

```
┌─────────────────────────────────────────────┐
│ MODEL — pure C#                             │
│  · No MonoBehaviour inheritance             │
│  · Never touches UnityEngine.Object         │
│  · No coroutines, no deltaTime              │
│  · Every operation instant and deterministic│
└───────────────┬─────────────────────────────┘
                │ event / UniRx / UniTask  (one-way)
                ▼
┌─────────────────────────────────────────────┐
│ VIEW — MonoBehaviour                        │
│  · Reads model state and draws it           │
│  · Owns every notion of time                │
│  · NEVER mutates the model itself           │
└─────────────────────────────────────────────┘
```

A clean model makes it possible to:
- Run a whole match in one frame → **bots play hundreds of matches to measure difficulty**
- Unit test logic without building a scene or entering Play mode
- Replace the entire visual layer during polish without touching a line of logic
- Reproduce a match exactly from a **seed** → rare bugs become debuggable

**Quick check:** grep `UnityEngine` in the model folder. Any hit means it is already wrong.

---

## 2. App Bootstrap

Initialise **sequentially** through `GameInitializer` / `BootstrapManager` to avoid race
conditions. Priority order: `Network → RemoteConfig → UMP/Consent → IAP → Ads → Gameplay`.

- Every step must report success/failure, and the game must still run when one step fails.
- No system may assume another is ready — check, do not trust.

---

## 3. Data-Driven Design

Every gameplay value, configuration and economy number lives **outside the code**:

| Data type | Stored in | Edited by |
|---|---|---|
| Balance, gameplay constants | ScriptableObject in `ScriptableObjects/` | Designer |
| Large data tables (levels, items, shop) | Excel/Sheet → loaded through `DataManager` | Designer |
| Levels | Files exported by the level tool — **JSON in development and in release** | Designer, via the tool |
| Per-environment config | RemoteConfig | Producer |

**Nobody hand-edits a level file outside the tool.**

---

## 4. Screen & UI Navigation

- Use the project's **stack-based UI framework**. **Never instantiate UI prefabs directly.**
- One screen = **one prefab**. Never merge several screens into one large prefab
  (this is the single biggest source of merge conflicts on a team).
- UI **does not own** game state. It displays, and raises commands/events requesting change.
- UI **never** blocks the game loop.

---

## 5. Gameplay Orchestration

- `GamePlayController` **orchestrates**; it holds no individual mechanic's logic.
- Mechanic systems register through an **interface** (dependency injection), never wired directly.
- `if (mechanicType == X)` in a central controller is **forbidden**.
- State machines need an **explicit transition table**, written down and matching the code.
- Events via Event / UniRx. Subscribe and unsubscribe **in matching places** (`OnDisable`/`OnDestroy`).

### The 5 Touch Points Of A Mechanic

A complete mechanic touches all five. Missing one produces silent bugs:

1. **Logic** — the mechanic class implements the shared interface, with no controller change
2. **Config** — its own ScriptableObject for tuning knobs
3. **Level format** — the new data field plus its place in the level tool
4. **Validator** — validity rules when this mechanic is present,
   **and a check for the case where it should be present but is missing**
5. **Simulator** — the bot must understand this mechanic, or every later measurement is wrong

---

## 6. Level Pipeline

```
Level Tool (EditorWindow)
   ├── Validator   ── checks data invariants          → "is anything contradictory?"
   ├── Simulator   ── bot plays N headless attempts   → "is it winnable? how hard?"
   └── Colour check ── CIEDE2000 + colour-blind sim   → "can they be told apart?"
            │
            └── Export JSON  (readable and diffable in git, in development and release alike)
```

`<...>` — state this project's level format decision here (e.g. "ships level JSON
unencrypted: readable/diffable at every stage, at the cost of players being able to open
and edit level files" vs. "readable in development, encrypted in release"). Anything that
must not be player-editable cannot live in a format the player can open.

**The validator does NOT replace the simulator.** The validator checks whether the data
contradicts itself; the simulator checks whether a winning path exists and how hard it is.
They are entirely different questions. Only having the first is a false sense of safety.

---

## 7. Analytics & Monetisation

Two **separate** systems that never call each other:
- **Analytics**: through `AnalyticManager`. Event names are constants, never loose strings.
- **Monetisation**: `IAPManager` + the ad SDK, behind their own interface layer so the SDK can be swapped.

---

## 8. Assembly Definitions

- All feature code lives in an `.asmdef`.
- References are **one-way**: `Core ← Gameplay ← UI`. No dependency cycles.
- Editor code lives in its own `.asmdef` with `includePlatforms: [Editor]`.
- Tests live in their own `.asmdef` referencing the runtime asmdef.
