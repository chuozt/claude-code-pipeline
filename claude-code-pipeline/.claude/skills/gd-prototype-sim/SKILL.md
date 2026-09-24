---
name: gd-prototype-sim
model: claude-opus-5-5
effort: medium
description: "Build the game's first prototype as a pure-C# simulation a bot can play, with no graphics — from the existing GDD, flow map and playstyle rules. Detects an existing simulation and switches to audit-and-extend instead of building a duplicate. Use when typing /gd-prototype-sim or saying 'prototype from the GDD', 'build the sim', 'let a bot play it before we do art'."
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
| Lifespan | discarded once the question is answered | **survives to ship** |
| Output location | `prototypes/<name>/` + REPORT.md, separate worktree | an assembly in the project |

**Having run `/prototype` does not remove the need for this skill** — but do not waste it:
`prototypes/*/REPORT.md` already contains *Lessons Learned* and *If Proceeding → Architecture
requirements*, which are exactly the input for section 0 below. Skipping it means the
simulation may contradict precisely what the prototype just learned, **and nobody notices
because the two never meet**.

🚫 Conversely, **do not carry code** from `prototypes/` into the simulation: every file there
is stamped *"PROTOTYPE - NOT FOR PRODUCTION"*, hardcodes freely, and copies instead of
importing. Take the **conclusions**, not the code.

**In**: GDD + `flow-map.md` + `difficulty-model.md` + `bot-playstyle.md` ·
**Out**: the simulation assembly + bots + solver API, runnable from tests

## 0. Load context and check preconditions

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 1, 2, 6, 7**.
2. Read `design/pipeline/flow-map.md`, `design/pipeline/difficulty-model.md` and
   `design/pipeline/bot-playstyle.md`. Any missing → stop, point at `/gd-map-flow`,
   `/gd-core-difficulty` or `/gd-bot-playstyle`.
3. Read the gameplay GDDs in `design/gdd/` — the source of the detailed rules.
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

- Contradicts `flow-map.md` / the GDD → **stop, ask the developer**, never pick a side yourself.
- The report says `Recommendation: PIVOT` or `KILL` → stop. Building a simulation for a design
  that was just rejected is doing the work twice.
- 🚫 **Do not carry code** across from `prototypes/`: those files are stamped *"NOT FOR
  PRODUCTION"*, hardcode freely, and copy instead of importing. Take the **conclusions**, not the code.

### 0b. Does a simulation already exist? ⭐

Before designing, scan the project for:

- an asmdef with `"noEngineReferences": true`
- directories like `*/Core/`, `*/Match/`, `*/Headless/`, `*/Sim/`
- types like `*Match`, `*State`, `*Bot`, `HeadlessRunner`, `*Solver`

**Found → SWITCH TO "AUDIT AND EXTEND" MODE. Never build a second assembly.**

A second simulation running alongside the existing one is two sources of truth for the same
rules — they will drift apart, and every number measured afterwards belongs to neither.

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
| Simulation assembly | `<Game>.Sim` | **references: [] — no UnityEngine** |
| Facade | `SimGame` | `Tap/Play(action)`, `State`, `OnEvent` |
| State | `SimSnapshot` | read-only, for the view and the bots |
| Level payload | `<Game>LevelData` | POCO, not a ScriptableObject |
| Config | `SimConfig` | POCO; the SO maps onto it at the boundary |
| Bots | `IBotPolicy` + 3 classes | Random / Greedy / Lookahead |
| Result | `PlayResult` | frame section 7 — win, moves, pressureCurve, safetyMargin, failPoint |
| Tests | `<Game>.Sim.Tests` | EditMode, no scene |

**Spawn `lead-programmer` and `unity-specialist` in parallel** (Task) to challenge it
**before generating code** — getting the boundary wrong here is permanent (contract rule 1), so
this is exactly where a review round is worth paying for.

The prompt for both must include:
- excerpts from **sections 1, 2, 6, 7** of `resource-flow-difficulty-framework.md`
- `flow-map.md`, `difficulty-model.md`, `bot-playstyle.md`, and the architecture table above
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
- the simulation's `asmdef` with `"references": []` — the compiler fence, the single most important point
- the `SimGame` facade with its calling contract: main thread only, no reentrancy inside handlers
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

1. Does the simulation reference `UnityEngine`? → `grep -r "UnityEngine" <Sim>/` must be empty
2. How long does one match take? → run the test, report the real number
3. Does `Σ(station counts) == total units` hold after every move? → assert it in a test

## 4. The boundary with the View

Write this into the assembly's README and remind the developer:
- The view **proposes**, the simulation **rules** — the view never decides move legality itself
- Physics, tweens and cameras exist only in the view; the simulation has no notion of time
- The simulation decides first, the view illustrates afterwards — never the reverse

## 5. Mandatory closing warning

> **If the simulation diverges from the real game, every number measured afterwards is
> fiction.** This prototype proves the simulation *runs*, not that it is *correct*. Before
> trusting the evaluator, reconcile the simulation against a real build in a few key situations.

## Next

- `/gd-level-gen` — phase 3 starts here: generate and filter candidates with the bots
- `/gd-mechanic-difficulty` for any mechanic still missing its rule branch
  (its levels stay `unscored` until then)

Full pipeline: `/gd-workflow-help`
