---
name: gd-prototype-sim
model: claude-opus-5-5
effort: medium
description: "Build the level-generation simulation tooling (Editor-only, never shipped, developer-only) on the game's Model: a pure-C# simulation a bot can play, with no graphics — from the existing GDD, flow map and playstyle rules. Detects an existing simulation and switches to audit-and-extend instead of building a duplicate. Use when typing /gd-prototype-sim or saying 'prototype from the GDD', 'build the sim', 'let a bot play it before we do art'."
argument-hint: "[assembly name, defaults to <Game>.Sim]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, AskUserQuestion
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

The game's first prototype needs **no pixel on screen**: a pure-C# simulation + three bots,
playing a whole match in one frame.

> **This skill's position** — the generic `/prototype` skill says *"throwaway code, standards
> relaxed"*. For a resource-flow game that is **only true of the View**.
> The simulation is architecture (contract rule 1): losing its purity at the start loses it
> forever. **Prototype the simulation properly, throw away the view.**

### How it differs from `/prototype`, and what if that already ran?

The two commands **do not replace each other** — different questions, different lifespans:

| | `/prototype` | `/gd-prototype-sim` |
|---|---|---|
| Answers | "is this idea fun?" | "how hard is this level?" |
| Standards | *skips normal standards*, throwaway | real architecture, pure C# |
| Lifespan | discarded once the question is answered | the **Model** survives to ship; the **Sim tools** assembly is level-generation tooling and **never ships** |
| Output location | `prototypes/<name>/` + REPORT.md, separate worktree | the game's Model assembly + an Editor-only tools assembly under `Assets/_Tools/LevelSim/` |

**Having run `/prototype` does not remove the need for this skill** — but do not waste it:
`prototypes/*/REPORT.md` already contains *Lessons Learned* and *If Proceeding → Architecture
requirements*, which are exactly the input for section 0 below. Skipping it means the
simulation may contradict precisely what the prototype just learned, **and nobody notices
because the two never meet**.

🚫 Conversely, **do not carry code** from `prototypes/` into the simulation: every file there
is stamped *"PROTOTYPE - NOT FOR PRODUCTION"*, hardcodes freely, and copies instead of
importing. Take the **conclusions**, not the code.

**In**: GDD + `gd-flow-map.md` + `gd-difficulty-model.md` + `bot-playstyle.md` ·
**Out**: the Sim tools assembly (bots + solver API + batch runner), runnable from tests, built
on the game's own Model

### Two assemblies — the rules ship, the tooling does not ⭐

| | **Model** (the rules) | **Sim tools** (`<Game>.Sim`) |
|---|---|---|
| Holds | state, legal moves, win/lose, `SimGame` facade, `SimSnapshot`, level POCO, `SimConfig` | `IBotPolicy` + 3 bots, `Solver.Play`, `PlayResult`, batch runner, tests |
| Ships in the player build | **yes** — it is the game's logic | **never** — Editor-only asmdef, nothing in the game references it |
| Used by | the game (View proposes, Model rules) and the Sim | the developer's level generation and audit, only |
| Location | the project's existing Model assembly (or `<Game>.Model`) | `Assets/_Tools/LevelSim/` |
| Who may touch it | developer | **developer only** — the designer never opens, edits or runs it (`gd-mode` §3.0) |

There is **one** copy of the rules, shared. Separating the *tooling* from the shipped game is
free; separating the *rules* would create two sources of truth that drift apart (section 0b).
Dependency points one way only: `Sim tools → Model`, never the reverse, never from the game.

### Built for thousands of parallel matches, from the developer's own documents ⭐

**A. Parallel by design.** Generation and audit run thousands of matches; they must run **at
the same time**, across every core, not one after another.

- One match = one `SimGame` instance with **its own state and its own seeded RNG** (never
  `UnityEngine.Random`, never a `System.Random` shared between matches). A single instance
  does not need to be thread-safe; **instances share nothing mutable**.
- **No static mutable state** in the Model or the Sim tools — statics only for constants and
  pure functions. Config is loaded once, immutable, and shared read-only.
- No UnityEngine API in either assembly (it is not safe off the main thread) — the purity fence
  already guarantees it.
- `BatchRunner.Run(levels, bots, seeds, maxDegreeOfParallelism)` spreads matches over all
  cores (`Parallel.ForEach` / thread pool), keeps **per-thread accumulators** merged at the
  end (no lock per match), and supports cancellation and progress.
- **Deterministic:** the same `(level, bot, seed)` gives an identical `PlayResult`, and the
  same batch gives identical aggregates, whatever the thread count or scheduling order.
- Low allocation per match (sized buffers, reused snapshots) so thousands of matches do not
  stall on garbage collection.
- Throughput is **measured and reported** (matches per second, cores used), never assumed.

**B. Same documents as the game — one source of truth.** The developer's Phase 1A documents
(`design/dev-system/dev-*.md` — rules, formulas, tuning knobs — and `dev-architecture.md`)
are what the game builds its **configs, feel and logic** from. The simulation is built from
those same documents and reads the **same config assets** the game reads:

- **No numbers of its own.** Every gameplay value reaches the Model through the shared config
  (mapped onto `SimConfig` at the boundary). A value typed into the simulation is a defect
  (`CLAUDE.md` §6).
- **Conformance matrix** `design/dev-system/dev-sim-conformance.md` (developer-owned): one row
  per rule/formula/knob in the GDDs → the Model type that implements it → the config asset →
  the test that proves it. A section that is View-only (feel, animation, sound) is marked
  *View-only*; a rule not modelled yet is marked *not modelled* and its levels are `unscored`.
- **Feel and timing stay in the View.** The simulation ignores them but must not depend on
  them. A GDD rule that depends on real time is expressed in the Model as discrete turns or
  ticks driven by config; if that is impossible, **stop and ask the developer** — never
  approximate silently.
- **Parity before trust.** Replay scripted move sequences taken from the GDD examples, plus a
  few real recorded sessions, through the Model **and** the real game, comparing a per-move
  state hash (`SimSnapshot.Hash()`). Any divergence blocks `/gd-level-gen` until fixed.

## 0. Load context and check preconditions

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 1, 2, 6, 7**.
2. Read `design/gdd/gd-flow-map.md`, `design/gdd/gd-difficulty-model.md` and
   `design/bot/bot-playstyle.md`. Any missing → stop, point at `/gd-map-flow`,
   `/gd-core-difficulty` or `/gd-bot-playstyle`.
3. Read the gameplay GDDs in `design/dev-system/` — the source of the detailed rules.
4. Read `.claude/docs/technical-preferences.md` for naming conventions and the engine.
5. **Harvest any prototype that already ran** — see 0a.
6. **Scan for an existing simulation** — see 0b. *Do this before designing anything.*

### 0a. Harvest `/prototype` (if it ran)

Glob `prototypes/*/REPORT.md`. If found → read **Lessons Learned** and
**If Proceeding → Architecture requirements**, then present them for the developer to confirm
before designing the simulation.

Those are expensive conclusions just bought with a whole prototype round; skipping them means
the simulation may be written **contradicting exactly what the prototype learned**, with nobody
noticing because the two never meet.

- Contradicts `gd-flow-map.md` / the GDD → **stop, ask the developer**, never pick a side yourself.
- The report says `Recommendation: PIVOT` or `KILL` → stop. Building a simulation for a design
  that was just rejected is doing the work twice.
- 🚫 **Do not carry code** across from `prototypes/`: those files are stamped *"NOT FOR
  PRODUCTION"*, hardcode freely, and copy instead of importing. Take the **conclusions**, not the code.

### 0b. Does a simulation already exist? ⭐

Before designing, scan the project for:

- an asmdef with `"noEngineReferences": true`
- directories like `*/Core/`, `*/Match/`, `*/Headless/`, `*/Sim/`
- types like `*Match`, `*State`, `*Bot`, `HeadlessRunner`, `*Solver`

**Found → SWITCH TO "AUDIT AND EXTEND" MODE. Never build a second copy of the rules.**

If a Model with the rules already exists (for example a pure-C# `Gameplay.Model`), build **only**
the Sim tools assembly on top of it — bots, solver, runner. If tooling already exists too,
extend it. A second implementation of the rules running alongside the existing Model is two
sources of truth — they will drift apart, and every number measured afterwards belongs to
neither.

Audit-and-extend does three things, in order:

1. **Reconcile** the existing code against the contract in `bot-playstyle.md` — its playstyle
   rules and its perceptual premise. Does the current bot follow those rules? Does it respect
   the observation budget? Does the simulation grant exactly the declared knowledge?
2. **List the gaps** as concrete work items, naming the existing files and types to extend.
3. **Present that list for approval** before editing. Section 1 (present the architecture)
   becomes "present the delta", not a re-presentation of what already exists.

Also report clearly if the existing simulation **violates** the purity contract (touching
MonoBehaviour, coroutines, `deltaTime`) — that is a fix-first item, not an extension.

**Hard stop**: `bot-playstyle.md` missing, or its *playstyle rules* section reading `UNDEFINED` → stop,
point at `/gd-bot-playstyle`. Without a stated tactic there is no way to write `GreedyBot`, and
the simulation has nobody to play it.

**Hard stop 2**: if factor 5 is High but there is no quantified perceptual rule (visibility
threshold, discrete direction count) → stop and ask the designer. Without it the legal move set
cannot be enumerated.

## 1. Present the architecture before generating code

Present this table for approval:

| Component | Proposed name | Notes |
|---|---|---|
| Model assembly *(ships; existing or new)* | `<Game>.Model` | **no UnityEngine** — `noEngineReferences: true` |
| Facade | `SimGame` | `Tap/Play(action)`, `State`, `OnEvent` — in the Model |
| State | `SimSnapshot` | read-only, for the view and the bots — in the Model |
| Level payload | `<Game>LevelData` | POCO, not a ScriptableObject — in the Model |
| Config | `SimConfig` | POCO; the SO maps onto it at the boundary — in the Model |
| **Sim tools assembly *(never ships)*** | `<Game>.Sim` at `Assets/_Tools/LevelSim/` | **Editor-only**: `includePlatforms: ["Editor"]`, `autoReferenced: false`, references **only** `<Game>.Model` |
| Bots | `IBotPolicy` + 3 classes | Random / Greedy / Lookahead — in the Sim tools |
| Result | `PlayResult` | frame section 7 — win, moves, pressureCurve, safetyMargin, failPoint — in the Sim tools |
| Parallel runner | `BatchRunner` | Sim tools — all cores, per-thread accumulators, deterministic, reports matches/second; the developer states the target throughput here |
| State hash | `SimSnapshot.Hash()` | Model — used by the parity test against the real game |
| Conformance matrix | `design/dev-system/dev-sim-conformance.md` | developer-owned: GDD section → Model type → config asset → test |
| Tests | `<Game>.Sim.Tests` | EditMode, no scene |

**Spawn `lead-programmer` and `unity-specialist` in parallel** (Task) to challenge it
**before generating code** — getting the boundary wrong here is permanent (contract rule 1), so
this is exactly where a review round is worth paying for.

The prompt for both must include:
- excerpts from **sections 1, 2, 6, 7** of `resource-flow-difficulty-framework.md`
- `gd-flow-map.md`, `gd-difficulty-model.md`, `bot-playstyle.md`, and the architecture table above
- `.claude/docs/technical-preferences.md`

Their individual requests:
- `lead-programmer`: *"review the `SimGame` contract — is the API surface sufficient, what is
  missing or superfluous, can the calling invariants be stated"*
- `unity-specialist`: *"review the asmdef boundary — what is the most reliable way to make the
  compiler block any UnityEngine reference from the simulation assembly"*

Both: **return analysis, DO NOT write files.**

> 🚫 **Do not route this to `prototyper`.** That agent is defined as *"throwaway
> implementations, standards intentionally relaxed"* — exactly what this skill exists to
> prevent. It would build a MonoBehaviour-bound simulation and contract rule 1 would be lost at
> the first step.

Present the review results alongside the architecture table, then ask:
"Are these names and this layout fine, or change anything before generating?"

## 2. Generate the skeleton

Generate a **compilable skeleton**, not the full rules — the rules are filled in by the
developer from the GDD. Every place needing content leaves a `// TODO(GDD:<file>#<section>)`
pointing straight at the source.

Must be present from the start:
- the Model's `asmdef` with `"noEngineReferences": true` and `"references": []` — the compiler
  fence, the single most important point
- the Sim tools `asmdef` at `Assets/_Tools/LevelSim/`: Editor-only (`"includePlatforms":
  ["Editor"]`), `"autoReferenced": false`, references `<Game>.Model` and nothing else
- the `SimGame` facade with its calling contract: **one instance is used by one thread at a
  time**, no reentrancy inside handlers, and **no static mutable state** — so any number of
  instances can run in parallel on different threads
- `BatchRunner` (parallel, per-thread accumulators, seed-deterministic) and
  `SimSnapshot.Hash()`
- `IBotPolicy` + a complete `RandomBot` (which needs no game rules) + a `GreedyBot` skeleton
  with the designer's tactical statement copied verbatim into the class comment
- `Solver.Play(level, bot, seed)` returning a `PlayResult`
- One test: *"play a whole match with RandomBot in one frame, with no engine reference"*

Ask permission before writing, listing every file path to be created.

## 3. Verify

Run the compile gate and report the real result:

```
dotnet build Assembly-CSharp.csproj
```

Then three verification questions, answered with evidence rather than assumption:

1. Does the Model reference `UnityEngine`? → `grep -r "UnityEngine" <Model>/` must be empty
2. How long does one match take? → run the test, report the real number
3. Does `Σ(station counts) == total units` hold after every move? → assert it in a test
4. Does anything outside the Sim tools reference it? → `grep -rl "<Game>.Sim" Assets --include=*.asmdef`
   must list **only** the Sim tools' own test asmdef. A game, Model, View or framework
   asmdef in that list is a defect: the tooling would leak into the player build.
5. Is the Sim tools asmdef Editor-only? → it must contain `"includePlatforms": ["Editor"]`,
   and a player build must not include `<Game>.Sim.dll`. Report what you checked.
6. Is anything shared and mutable between matches? → scan both assemblies for non-`readonly`
   `static` fields and for `UnityEngine.Random`; the list must be empty.
7. Does it run in parallel and stay deterministic? → run one batch of **at least 10,000
   matches** with 1 thread and again with all cores: the aggregates must be **identical**.
   Report matches per second for each, and the core count. No number measured = not verified.
8. Does it match the real game? → run the parity test (section B above) and report how many
   sequences and moves were compared. State plainly if it was not run.

## 4. The boundary with the View

Write this into the assembly's README and remind the developer:
- The view **proposes**, the simulation **rules** — the view never decides move legality itself
- Physics, tweens and cameras exist only in the view; the simulation has no notion of time
- The simulation decides first, the view illustrates afterwards — never the reverse
- Dependency is one-way: **Sim tools → Model**. The Model never references the tools; the game
  never references the tools; the tools never change a rule (a rule change goes through the
  Model and the developer, from `bot-playstyle.md` or the GDD).
- **Developer-only.** The designer does not open, edit or run anything under
  `Assets/_Tools/LevelSim/` (`gd-mode` §3.0). Results reach the designer only as the
  `/gd-level-gen` candidate tables and `level-audit-<date>.md`.

## 5. Mandatory closing warning

> **If the simulation diverges from the real game, every number measured afterwards is
> fiction.** This prototype proves the simulation *runs*, not that it is *correct*. Before
> trusting the evaluator, reconcile the simulation against a real build in a few key situations.

## Next

- `/gd-level-gen` — phase 3 starts here: generate and filter candidates with the bots
- `/gd-mechanic-difficulty` for any mechanic still missing its rule branch
  (its levels stay `unscored` until then)

Full pipeline: `/gd-workflow-help`
