---
name: design-system
description: "Guided, section-by-section GDD authoring for every undesigned system in the systems index, run back-to-back in one invocation. Gathers context from existing docs, walks through each required section collaboratively, cross-references dependencies, and writes incrementally to file. A single system or a retrofit path can still be targeted explicitly."
argument-hint: "[all | <system-name> | retrofit <path>]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Task, AskUserQuestion, TodoWrite
---

When this skill is invoked:

## 1. Parse Arguments & Validate

**Default mode is `all`.** No argument, or the literal argument `all`, means: design every
undesigned system in the index, back-to-back, in one run — not one system per invocation.
A specific `<system-name>` or `retrofit <path>` still targets just that one system, for
picking up a single gap without running the whole queue.

**Resolve which mode this run is in:**

1. Check if `design/gdd/systems-index.md` exists. If it does not exist, fail with:
   > "Usage: `/design-system` (all undesigned systems) · `/design-system <system-name>`
   > (just one) · `/design-system retrofit design/gdd/[system-name].md` (fill gaps in an
   > existing GDD).
   > No systems index found. Run `/map-systems` first to map your systems and get the
   > design order."
2. No argument, or argument is `all` → **All-systems mode.** Read the index and build the
   **ordered queue**: every system with status "Not Started" or equivalent, in the index's
   priority order. Present the queue before starting anything:
   > "**Design queue (N systems, priority order):** 1. [name] ([priority]|[layer]) · 2. […] · …
   > I'll run the full section-by-section process for each, one after another, checking in
   > with you between systems. Start with **[first system]**?"
   Options via `AskUserQuestion`: `[A] Yes — start the queue` / `[B] Reorder or drop some
   systems first` / `[C] Design just one system instead` / `[D] Stop here`.
   - `[A]`: proceed, current target = queue[0].
   - `[B]`: ask which systems to include/reorder (plain text), rebuild the queue, re-confirm.
   - `[C]`: ask for a system name, fall through to single-system mode below.
   - `[D]`: exit.
   If the queue is empty (nothing "Not Started"): report "All systems in the index are
   already designed or in review — nothing to queue" and stop.
3. Argument is a specific `<system-name>` (not `all`, not `retrofit …`) → **Single-system
   mode.** Only that system is designed this run; no queue, no auto-advance.
4. Argument starts with `retrofit`, or is a path to an existing `.md` in `design/gdd/` →
   **Retrofit mode** (below). Retrofit always targets one file, never the whole queue.

**Detect retrofit mode:**
If the argument starts with `retrofit` or the argument is a file path to an
existing `.md` file in `design/gdd/`, enter **retrofit mode**:

1. Read the existing GDD file.
2. Identify which of the 8 required sections are present (scan for section headings).
   Required sections: Overview, Player Fantasy, Detailed Design/Rules, Formulas,
   Edge Cases, Dependencies, Tuning Knobs, Acceptance Criteria.
3. Identify which sections contain only placeholder text (`[To be designed]` or
   equivalent — blank, a single line, or obviously incomplete).
4. Present to the user before doing anything:
   ```
   ## Retrofit: [System Name]
   File: design/gdd/[filename].md

   Sections already written (will not be touched):
   ✓ [section name]
   ✓ [section name]

   Missing or incomplete sections (will be authored):
   ✗ [section name] — missing
   ✗ [section name] — placeholder only
   ```
5. Ask: "Shall I fill the [N] missing sections? I will not modify any existing content."
6. If yes: proceed to **Phase 2 (Gather Context)** as normal, but in **Phase 3**
   skip creating the skeleton (file already exists) and in **Phase 4** skip
   sections that are already complete. Only run the section cycle for missing/
   incomplete sections.
7. **Never overwrite existing section content.** Use Edit tool to replace only
   `[To be designed]` placeholders or empty section bodies.

If NOT in retrofit mode, normalize the system name to kebab-case for the
filename (e.g., "combat system" becomes `combat-system`).

---

## 2. Gather Context (Read Phase)

Read all relevant context **before** asking the user anything. This is the skill's
primary advantage over ad-hoc design — it arrives informed.

### 2a: Required Reads

- **Game concept**: Read `design/gdd/game-concept.md` — fail if missing:
  > "No game concept found. Run `/brainstorm` first."
- **Systems index**: Read `design/gdd/systems-index.md` — fail if missing:
  > "No systems index found. Run `/map-systems` first to map your systems."
- **Target system**: Find the system in the index. If not listed, warn:
  > "[system-name] is not in the systems index. Would you like to add it, or
  > design it as an off-index system?"
- **Entity registry**: Read `design/registry/entities.yaml` if it exists.
  Extract all entries referenced by or relevant to this system (grep
  `referenced_by.*[system-name]` and `source.*[system-name]`). Hold these
  in context as **known facts** — values that other GDDs have already
  established and this GDD must not contradict.
- **Reflexion log**: Read `docs/consistency-failures.md` if it exists.
  Extract entries whose Domain matches this system's category. These are
  recurring conflict patterns — present them under "Past failure patterns"
  in the Phase 2d context summary so the user knows where mistakes have
  occurred before in this domain.

### 2b: Dependency Reads

From the systems index, identify:
- **Upstream dependencies**: Systems this one depends on. Read their GDDs if they
  exist (these contain decisions this system must respect).
- **Downstream dependents**: Systems that depend on this one. Read their GDDs if
  they exist (these contain expectations this system must satisfy).

For each dependency GDD that exists, extract and hold in context:
- Key interfaces (what data flows between the systems)
- Formulas that reference this system's outputs
- Edge cases that assume this system's behavior
- Tuning knobs that feed into this system

### 2c: Optional Reads

- **Game pillars**: Read `design/gdd/game-pillars.md` if it exists
- **Existing GDD**: Read `design/gdd/[system-name].md` if it exists (resume, don't
  restart from scratch)
- **Related GDDs**: Glob `design/gdd/*.md` and read any that are thematically related
  (e.g., if designing a system that overlaps with another in scope, read the related GDD
  even if it's not a formal dependency)

### 2c-bis: The Designer's Docs — the source for every design decision

This skill is run by the **developer alone**; design decisions come from what the game
designer has already written, never from an interview. Search, in this order, for anything
about this system — rules, numbers, ranges, edge cases, intent:

1. `design/pipeline/*.md` — `difficulty-model.md`, `level-definition.md`, `mechanics/*.md`,
   `mechanic-mix.md`, `mechanic-object-mix.md`, `level-intent.md`
2. The designer's `.xlsx` workbook, via `.claude/tools/gdd-sync` (the workbook wins over its
   derived `.md` when they disagree)
3. `design/gdd/*.md` written by the designer, `docs/features/*.md`

Hold every hit as a **designer fact**: the value or rule, plus `file:line` (or sheet!cell).
Sections C, D, E and G propose from these facts first (§4). Say which paths were searched —
"nothing found" is a claim too (`CLAUDE.md` §5).

### 2d: Present Context Summary

Before starting design work, present a brief summary to the user:

> **Designing: [System Name]**
> - Priority: [from index] | Layer: [from index]
> - Depends on: [list, noting which have GDDs vs. undesigned]
> - Depended on by: [list, noting which have GDDs vs. undesigned]
> - Existing decisions to respect: [key constraints from dependency GDDs]
> - Pillar alignment: [which pillar(s) this system primarily serves]
> - **Known cross-system facts (from registry):**
>   - [entity_name]: [attribute]=[value], [attribute]=[value] (owned by [source GDD])
>   - [item_name]: [attribute]=[value], [attribute]=[value] (owned by [source GDD])
>   - [formula_name]: variables=[list], output=[min–max] (owned by [source GDD])
>   - [constant_name]: [value] [unit] (owned by [source GDD])
>   *(These values are locked — if this GDD needs different values, surface
>   the conflict before writing. Do not silently use different numbers.)*
>
> - **Designer facts** (from 2c-bis): [value or rule] — `[source]` · … or "none found in
>   [paths searched]"
>
> If no registry entries are relevant: omit the "Known cross-system facts" section.

If any upstream dependencies are undesigned, warn:
> "[dependency] doesn't have a GDD yet. We'll need to make assumptions about
> its interface. Consider designing it first, or we can define the expected
> contract and flag it as provisional."

### 2e: Technical Feasibility Pre-Check

Before asking the user to begin designing, load engine context and surface any
constraints or knowledge gaps that will shape the design.

**Step 1 — Determine the engine domain for this system:**
Map the system's category (from systems-index.md) to an engine domain:

| System Category | Engine Domain |
|----------------|--------------|
| Combat, physics, collision | Physics |
| Rendering, visual effects, shaders | Rendering |
| UI, HUD, menus | UI |
| Audio, sound, music | Audio |
| AI, pathfinding, behavior trees | Navigation / Scripting |
| Animation, IK, rigs | Animation |
| Networking, multiplayer, sync | Networking |
| Input, controls, keybinding | Input |
| Save/load, persistence, data | Core |
| Dialogue, quests, narrative | Scripting |

**Step 2 — Read engine context (if available):**
- Read `.claude/docs/technical-preferences.md` to identify the engine and version
- If engine is configured, read `docs/engine-reference/[engine]/VERSION.md`
- Read `docs/engine-reference/[engine]/modules/[domain].md` if it exists
- Read `docs/engine-reference/[engine]/breaking-changes.md` for domain-relevant entries
- Glob `docs/architecture/adr-*.md` and read any ADRs whose domain matches
  (check the Engine Compatibility table's "Domain" field)

**Step 3 — Present the Feasibility Brief:**

If engine reference docs exist, present before starting design:

```
## Technical Feasibility Brief: [System Name]
Engine: [name + version]
Domain: [domain]

### Known Engine Capabilities (verified for [version])
- [capability relevant to this system]
- [capability 2]

### Engine Constraints That Will Shape This Design
- [constraint from engine-reference or existing ADR]

### Knowledge Gaps (verify before committing to these)
- [post-cutoff feature this design might rely on — mark HIGH/MEDIUM risk]

### Existing ADRs That Constrain This System
- ADR-XXXX: [decision summary] — means [implication for this GDD]
  (or "None yet")
```

If no engine reference docs exist (engine not yet configured), show a short note:
> "The stack is not filled in yet — skipping the technical feasibility check. Fill
> `project_setup.md` §1 before moving to architecture."

**Step 4 — Ask before proceeding:**

Use `AskUserQuestion`:
- "Any constraints to add before we begin, or shall we proceed with these noted?"
  - Options: "Proceed with these noted", "Add a constraint first", "I need to check the engine docs — pause here"

---

Use `AskUserQuestion`:
- "Ready to start designing [system-name]?"
  - Options: "Yes, let's go", "Show me more context first", "Design a dependency first"

---

## 3. Create File Skeleton

Once the user confirms, **immediately** create the GDD file with empty section
headers. This ensures incremental writes have a target.

Use the template structure from `.claude/docs/templates/game-design-document.md`:

```markdown
# [System Name]

> **Status**: In Design
> **Author**: [developer] — design values from the designer's docs, `[PLACEHOLDER]` where none exist
> **Last Updated**: [today's date]
> **Implements Pillar**: [from context]

## Overview

[To be designed]

## Player Fantasy

[To be designed]

## Detailed Design

### Core Rules

[To be designed]

### States and Transitions

[To be designed]

### Interactions with Other Systems

[To be designed]

## Formulas

[To be designed]

## Edge Cases

[To be designed]

## Dependencies

[To be designed]

## Tuning Knobs

[To be designed]

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Acceptance Criteria

[To be designed]

## Open Questions

[To be designed]
```

Ask: "May I create the skeleton file at `design/gdd/[system-name].md`?"

After writing, update `docs/_session/active.md`:
- Use Glob to check if the file exists.
- If it **does not exist**: use the **Write** tool to create it. Never attempt Edit on a file that may not exist.
- If it **already exists**: use the **Edit** tool to update the relevant fields.

File content:
- Task: Designing [system-name] GDD
- Current section: Starting (skeleton created)
- File: design/gdd/[system-name].md

---

## 4. Section-by-Section Design

**Developer-only.** Only the developer takes part; the designer is never asked mid-session
(`docs/team-workflow.md`). Questions stay technical — flow, states, interfaces, what the code
needs. Anything that is a **design decision** (a rule's intent, a number, a range, a
balance trade-off) follows the **designer-fact rule**:

1. **Found in the designer's docs** (2c-bis) → propose it with its source, e.g.
   *"`moveLimit = 25` (`level-definition.md:48`)"*, and ask the developer to confirm.
2. **Not found** → the developer may enter a working value, written **`[PLACEHOLDER]`** in
   the GDD and in the config default, plus an Open Question "confirm [value] — owner: game
   designer". It makes the code run; it is not a design decision (`CLAUDE.md` §10).
3. **Never** invent a number or a rule yourself and present it as settled, and never ask the
   developer taste questions ("what should it feel like?", "linear or logarithmic?").

**No agents by default.** Spawn a specialist only when the developer explicitly asks for one
(the routing table in §6 says which).

Walk through each section in order. For **each section**, follow this cycle:

### The Section Cycle

```
Context  ->  Questions  ->  Options  ->  Decision  ->  Draft  ->  Approval  ->  Write
```

1. **Context**: State what this section needs to contain, and surface any relevant
   decisions from dependency GDDs that constrain it.

2. **Questions**: Ask clarifying questions specific to this section. Use
   `AskUserQuestion` for constrained questions, conversational text for open-ended
   exploration.

3. **Options**: Only for **technical** choices (data shape, interface, state layout) —
   present 2-4 approaches with pros/cons, then use `AskUserQuestion`. A **design** choice is
   never put to the developer as options; it follows the designer-fact rule above.

4. **Decision**: User picks an approach or provides custom direction.

5. **Draft**: Write the section content in conversation text for review. Flag any
   provisional assumptions about undesigned dependencies.

6. **Approval**: Immediately after the draft — in the SAME response — use
   `AskUserQuestion`. **NEVER use plain text. NEVER skip this step.**
   - Prompt: "Approve the [Section Name] section?"
   - Options: `[A] Approve — write it to file` / `[B] Make changes — describe what to fix` / `[C] Start over`

   **The draft and the approval widget MUST appear together in one response.
   If the draft appears without the widget, the user is left at a blank prompt
   with no path forward — this is a protocol violation.**

7. **Write**: Use the Edit tool to replace the placeholder with the approved content.
   **CRITICAL**: Always include the section heading in the `old_string` to ensure
   uniqueness — never match `[To be designed]` alone, as multiple sections use the
   same placeholder and the Edit tool requires a unique match. Use this pattern:
   ```
   old_string: "## [Section Name]\n\n[To be designed]"
   new_string: "## [Section Name]\n\n[approved content]"
   ```
   Confirm the write.

8. **Registry conflict check** (Sections C and D only — Detailed Design and Formulas):
   After writing, scan the section content for entity names, item names, formula
   names, and numeric constants that appear in the registry. For each match:
   - Compare the value just written against the registry entry.
   - If they differ: **surface the conflict immediately** before starting the next
     section. Do not continue silently.
     > "Registry conflict: [name] is registered in [source GDD] as [registry_value].
     > This section just wrote [new_value]. Which is correct?"
   - If new (not in registry): flag it as a candidate for registry registration
     (will be handled in Phase 5).

After writing each section, update `docs/_session/active.md` with the
completed section name. Use Glob to check if the file exists — use Write to create
it if absent, Edit to update it if present.

### Section-Specific Guidance

---

### Section A: Overview

**Goal**: One paragraph a stranger could read and understand.

**Draft it directly — no framing widget.** Write it from the systems index entry, the
concept doc and the designer facts: what the system is in one sentence, how the player meets
it (active / passive / automatic), and what the game loses without it. Foundation/
Infrastructure systems get a technical framing. If an ADR in `docs/architecture/adr-*.md`
names this system, cite it. The developer corrects the draft in the normal approval step.

**Cross-reference**: Check that the description aligns with how the systems index
describes it. Flag discrepancies.

**Design vs. implementation boundary**: Overview questions must stay at the behavior
level — what the system *does*, not *how it is built*. If implementation questions
arise during the Overview (e.g., "Should this use an Autoload singleton or a signal
bus?"), note them as "→ becomes an ADR" and move on. Implementation patterns belong
in `/architecture-decision`, not the GDD. The GDD describes behavior; the ADR
describes the technical approach used to achieve it.

---

### Section B: Player Fantasy

**Goal**: The emotional target — what the player should *feel* — taken from the docs, not
invented in this session.

**No questions, no agent.** Fill this section from what already exists:

1. Foundation/Infrastructure layer → write `N/A — infrastructure; players feel what it
   enables: [the systems it serves]`.
2. Otherwise, find the pillar or core-fantasy line in `design/gdd/game-concept.md` (or
   `game-pillars.md`) that this system serves, and **quote it** with its source.
3. Nothing in the docs fits → write `UNDEFINED` and add an Open Question: "Player Fantasy
   for [system] — owner: game designer". Never write a fantasy yourself.

Show the result with the Section A draft; it needs no approval step of its own.

---

### Section C: Detailed Design (Core Rules, States, Interactions)

**Goal**: Unambiguous specification a programmer could implement without questions.

This is usually the largest section. Break it into sub-sections:

1. **Core Rules**: The fundamental mechanics. Use numbered rules for sequential
   processes, bullets for properties.
2. **States and Transitions**: If the system has states, map every state and
   every valid transition. Use a table.
3. **Interactions with Other Systems**: For each dependency (upstream and downstream),
   specify what data flows in, what flows out, and who owns the interface.

**Draft first, then ask.** Build the rules from the designer facts, then ask the developer
only technical questions:
- Is this step-by-step flow what the code must do?
- Is the state/transition table complete — any state or transition missing?
- For each interface: what data goes in and out, and which side owns it?

A rule the designer's docs do not settle → the designer-fact rule (§4): a working rule marked
`[PLACEHOLDER]` if the developer supplies one, otherwise `UNDEFINED`, and an Open Question.

**Cross-reference**: For each interaction listed, verify it matches what the
dependency GDD specifies. If a dependency defines a value or formula and this
system expects something different, flag the conflict.

---

### Section D: Formulas

**Goal**: Every mathematical formula, with variables defined, ranges specified,
and edge cases noted.

**Completion Steering — always begin each formula with this exact structure:**

```
The [formula_name] formula is defined as:

`[formula_name] = [expression]`

**Variables:**
| Variable | Symbol | Type | Range | Description |
|----------|--------|------|-------|-------------|
| [name] | [sym] | float/int | [min–max] | [what it represents] |

**Output Range:** [min] to [max] under normal play; [behaviour at extremes]
**Example:** [worked example with real numbers]
```

Do NOT write `[Formula TBD]` or describe a formula in prose without the variable
table. A formula without defined variables cannot be implemented without guesswork.

**The developer writes the structure; the numbers come from the designer.** Derive the
calculations the rules in Section C require — the expression, each variable and its type.
For every **value** (a constant, a range, a curve's shape) apply the designer-fact rule (§4):

| Case | What goes in the GDD |
|---|---|
| Value in the designer's docs | the value + its source, confirmed by the developer |
| Not in the docs, developer gives a working value | the value marked `[PLACEHOLDER]` + an Open Question for the designer |
| Neither | `UNDEFINED` in the Range column + an Open Question |

The only question for the developer is: *"Is this the calculation the code must do?"* Never
ask about scaling shape or output ranges per game stage — those are balance decisions.
Every variable that is a value, not a computed input, becomes a Tuning Knob (Section G).

**Cross-reference**: If a dependency GDD defines a formula whose output feeds into
this system, reference it explicitly. Don't reinvent — connect.

---

### Section E: Edge Cases

**Goal**: Explicitly handle unusual situations so they don't become bugs.

**Completion Steering — format each edge case as:**
- **If [condition]**: [exact outcome]. [rationale if non-obvious]

Example (adapt terminology to the game's domain):
- **If [resource] reaches 0 while [protective condition] is active**: hold at minimum until condition ends, then apply consequence.
- **If two [triggers/events] fire simultaneously**: resolve in [defined priority order]; ties use [defined tiebreak rule].

Do NOT write vague entries like "handle appropriately" — each must name the exact
condition and the exact resolution. An edge case without a resolution is an open
design question, not a specification.

**List them yourself, the developer ticks.** From Sections C and D, list the candidates:
every value at zero, at its maximum, out of range; two rules or events firing in the same
frame; a dependency missing or not ready. Give each a resolution — from the designer facts
when they settle it, otherwise the safe technical default (clamp, ignore, queue) marked
`[PLACEHOLDER]`. Present them as one list; the developer keeps, edits or drops each.

**Cross-reference**: Check edge cases against dependency GDDs. If a dependency
defines a floor, cap, or resolution rule that this system could violate, flag it.

---

### Section F: Dependencies

**Goal**: Map every system connection with direction and nature.

This section is partially pre-filled from the context gathering phase. Present the
known dependencies from the systems index and ask:
- Are there dependencies I'm missing?
- For each dependency, what's the specific data interface?
- Which dependencies are hard (system cannot function without it) vs. soft
  (enhanced by it but works without it)?

**Cross-reference**: This section must be bidirectionally consistent. If this system
lists "depends on Combat", then the Combat GDD should list "depended on by [this
system]". Flag any one-directional dependencies for correction.

---

### Section G: Tuning Knobs

**Goal**: Every designer-adjustable value, exposed as config so the designer tunes it later
without a code change.

**Derive, do not ask.** One knob per value-variable from Section D, plus any value the rules
in Section C use. For each:

| Knob | Config field | Default | Source | Safe range |
|---|---|---|---|---|
| [name] | `[ScriptableObject].[field]` | [value] | [designer doc `file:line`] / `[PLACEHOLDER]` / `UNDEFINED` | [from the docs, or `UNDEFINED`] |

Every `[PLACEHOLDER]` default and `UNDEFINED` range gets an Open Question for the designer.
Never ask the developer what breaks when a knob is too high or too low — that is the
designer's call, made later in the config.

**Cross-reference**: If a dependency GDD lists tuning knobs that affect this system,
reference them here. Don't create duplicate knobs — point to the source of truth.

---

### Section H: Acceptance Criteria

**Goal**: Testable conditions that prove the system works as designed.

**Completion Steering — format each criterion as Given-When-Then:**
- **GIVEN** [initial state], **WHEN** [action or trigger], **THEN** [measurable outcome]

Example (adapt terminology to the game's domain):
- **GIVEN** [initial state], **WHEN** [player action or system trigger], **THEN** [specific measurable outcome].
- **GIVEN** [a constraint is active], **WHEN** [player attempts an action], **THEN** [feedback shown and action result].

Include at least: one criterion per core rule from Section C, and one per formula
from Section D. Do NOT write "the system works as designed" — every criterion must
be independently verifiable by a QA tester without reading the GDD.

Before presenting, self-check that each criterion can be verified without reading the GDD
and that every core rule and formula has one. **One question for the developer:** the
performance budget this system gets on the reference device (frame time, memory) — or
`UNDEFINED` if the project has none yet (`project_setup.md` §1).

**Cross-reference**: Include criteria that verify cross-system interactions work,
not just this system in isolation.

---

### Optional Sections: Visual/Audio, UI Requirements, Open Questions

These sections are included in the template. Visual/Audio is **REQUIRED** for visual system categories — not optional. Determine the requirement level before asking:

**Visual/Audio is REQUIRED (mandatory — do not offer to skip) for these system categories:**
- Combat, damage, health
- UI systems (HUD, menus)
- Animation, character movement
- Visual effects, particles, shaders
- Character systems
- Dialogue, quests, lore
- Level/world systems

For required systems: list the **events that need feedback** (from Section C) and, for each,
what the code must expose (an event, a hook point) and the performance budget on the target
device (`project_setup.md` §1). Do NOT leave this section as `[To be designed]` for visual
systems. How each feedback looks or sounds is the artists' and designer's call — mark it
`UNDEFINED` rather than inventing it.

For **all other system categories** (Foundation/Infrastructure, Economy, AI/pathfinding, Camera/input), offer the optional sections after the required sections:

Use `AskUserQuestion`:
- "The 8 required sections are complete. Do you want to also define Visual/Audio
  requirements, UI requirements, or capture open questions?"
  - Options: "Yes, all three", "Just open questions", "Skip — I'll add these later"

For **Visual/Audio** (non-required systems): a brief note suffices at the GDD stage.

For **UI Requirements**: list the data each screen shows and the commands it sends.
After writing this section, check whether it contains real content (not just
`[To be designed]` or a note that this system has no UI). If it does have real
UI requirements, output this flag immediately:

> **📌 UX Flag — [System Name]**: This system has UI requirements. In Phase 4
> (Pre-Production), run `/ux-design` to create a UX spec for each screen or
> HUD element this system contributes to **before** writing epics. Stories that
> reference UI should cite `design/ux/[screen].md`, not the GDD directly.
>
> Note this in the systems index for this system if you update it.

For **Open Questions**: Capture anything that came up during design that wasn't
fully resolved. Each question should have an owner and target resolution date. **Always
written, never skipped** when the GDD holds any `[PLACEHOLDER]` or `UNDEFINED`: one row per
item, owner "game designer" — this list is how the designer picks the GDD up afterwards.

---

## 5. Post-Design Validation

After all sections are written:

### 5a: Self-Check

Read back the complete GDD from file (not from conversation memory — the file is
the source of truth). Verify:
- All 8 required sections have real content (not placeholders)
- Formulas reference defined variables
- Edge cases have resolutions
- Dependencies are listed with interfaces
- Acceptance criteria are testable

---

### 5b: Update Entity Registry

Scan the completed GDD for cross-system facts that should be registered:
- Named entities (enemies, NPCs, bosses) with stats or drops
- Named items with values, weights, or categories
- Named formulas with defined variables and output ranges
- Named constants referenced by value in more than one place

For each candidate, check if it already exists in `design/registry/entities.yaml`:
```
Grep pattern="  - name: [candidate_name]" path="design/registry/entities.yaml"
```

Present a summary:
```
Registry candidates from this GDD:
  NEW (not yet registered):
    - [entity_name] [entity]: [attribute]=[value], [attribute]=[value]
    - [item_name] [item]: [attribute]=[value], [attribute]=[value]
    - [formula_name] [formula]: variables=[list], output=[min–max]
  ALREADY REGISTERED (referenced_by will be updated):
    - [constant_name] [constant]: value=[N] ← matches registry ✅
```

Ask: "May I update `design/registry/entities.yaml` with these [N] new entries
and update `referenced_by` for the existing entries?"

If yes: append new entries and update `referenced_by` arrays. Never modify
existing `value` / attribute fields without surfacing it as a conflict first.

### 5c: Offer Design Review

Present a completion summary:

> **GDD Complete: [System Name]**
> - Sections written: [list]
> - Provisional assumptions: [list any assumptions about undesigned dependencies]
> - Cross-system conflicts found: [list or "none"]

> **To validate this GDD, open a fresh Claude Code session and run:**
> `/design-review design/gdd/[system-name].md`
>
> **Never run `/design-review` in the same session as `/design-system`.** The reviewing
> agent must be independent of the authoring context. Running it here would inherit
> the full design history, making independent critique impossible.

**NEVER offer to run `/design-review` inline.** Always direct the user to a fresh window.

### 5d: Update Systems Index

After the GDD is complete (and optionally reviewed):

- Read the systems index
- Update the target system's row:
  - If design-review was run and verdict is APPROVED: Status → "Approved"
  - If design-review was run and verdict is NEEDS REVISION: Status → "In Review"
  - If design-review was skipped: Status → "Designed" (pending review)
  - If the user chose "I'll review it myself first": Status → "Designed"
  - Design Doc: link to `design/gdd/[system-name].md`
- Update the Progress Tracker counts

Ask: "May I update the systems index at `design/gdd/systems-index.md`?"

### 5d: Update Session State

Update `docs/_session/active.md` with:
- Task: [system-name] GDD
- Status: Complete (or In Review if design-review was run)
- File: design/gdd/[system-name].md
- Sections: All 8 written
- Next: [suggest next system from design order]

**All-systems mode additionally records the queue itself**, so an interruption mid-queue is
resumable:
- Mode: all-systems
- Queue (in order): [full ordered list from step 1, with each marked done / current / pending]
- Current: [system-name] (just completed)

### 5e: Next Steps — differs by mode

**Single-system mode or retrofit mode** (this run only ever targeted one system):
Use `AskUserQuestion`:
- "What's next?"
  - Options:
    - "Run `/consistency-check` — verify this GDD's values don't conflict with existing GDDs"
    - "Design another system" — asks for a name, or run `/design-system` with no
      argument to pick up the full queue
    - "Fix review findings" — if design-review flagged issues
    - "Stop here for this session"
    - "Run `/review-all-gdds`" — if enough MVP systems are designed

**All-systems mode** (this run is working through the queue): do not ask an open-ended
"what's next" — advance the queue itself.

1. Remove the just-finished system from the queue and report progress:
   > "**[System Name] done.** [K] of [N] in the queue complete. Remaining: [list]."
2. If the queue is now empty: congratulate, then suggest `/consistency-check` and
   `/review-all-gdds` as the natural next commands, and stop — there is no next system to
   auto-advance to.
3. If systems remain: check in **once** before continuing (this is still a long,
   interactive process per system — do not silently chain into the next one without a
   pause point), via `AskUserQuestion`:
   > "Continue to **[next system]** ([K+1] of [N])?"
   - Options: `[A] Yes — continue the queue` / `[B] Run \`/consistency-check\` first, then
     continue` / `[C] Skip [next system] for now — do the one after` / `[D] Stop the queue
     here for this session`
   - `[A]`: loop back to **Section 2 (Gather Context)** for the next system in the queue.
   - `[B]`: run consistency-check inline, then loop back to Section 2 for the next system.
   - `[C]`: move that system to the end of the queue, re-ask for the new next one.
   - `[D]`: stop; the queue and progress are already recorded in session state (7. Recovery
     & Resume), so a later `/design-system` with no argument resumes exactly here.

---

## 6. Specialist Agent Routing

**Only on the developer's explicit request** (§4 "No agents by default"). When the developer
asks for a second opinion on a section, this table says whom to spawn. An agent's proposal
of a number is still not a designer fact — it goes in as `[PLACEHOLDER]`.

| System Category | Primary Agent | Supporting Agent(s) |
|----------------|---------------|---------------------|
| **Foundation/Infrastructure** (event bus, save/load, scene mgmt, service locator) | `systems-designer` | `gameplay-programmer` (feasibility), `unity-specialist` (engine integration) |
| Combat, damage, health | `game-designer` | `systems-designer` (formulas), `gameplay-programmer` (enemy AI), `technical-artist` (hit feedback, VFX intent) |
| Economy, loot, crafting | `economy-designer` | `systems-designer` (curves), `game-designer` (loops) |
| Progression, XP, skills | `game-designer` | `systems-designer` (curves), `economy-designer` (sinks) |
| Dialogue, quests, lore | `game-designer` | `localization-lead` (string volume, text expansion) |
| UI systems (HUD, menus) | `game-designer` | `ux-designer` (flows), `ui-programmer` (feasibility), `technical-artist` (render/shader constraints) |
| Audio systems | `game-designer` | `sound-designer` (specs) |
| AI, pathfinding, behavior | `game-designer` | `gameplay-programmer` (implementation), `systems-designer` (scoring) |
| Level/world systems | `game-designer` | `systems-designer` (level data, constraints) |
| Camera, input, controls | `game-designer` | `ux-designer` (feel), `gameplay-programmer` (feasibility) |
| Animation, character movement | `game-designer` | `technical-artist` (rig/blend constraints), `gameplay-programmer` (feel) |
| Visual effects, particles, shaders | `game-designer` | `technical-artist` (performance budget, shader complexity), `systems-designer` (trigger/state integration) |
| Character systems (stats, archetypes) | `game-designer` | `systems-designer` (stat formulas) |

**When delegating via Task tool**:
- Provide: system name, game concept summary, dependency GDD excerpts, the specific
  section being worked on, and what question needs expert input
- The agent returns analysis/proposals to the main session
- The main session presents the agent's output to the user via `AskUserQuestion`
- The user decides; the main session writes to file
- Agents do NOT write to files directly — the main session owns all file writes

---

## 7. Recovery & Resume

If the session is interrupted (compaction, crash, new session, or the queue was stopped
deliberately in step 5e):

1. Read `docs/_session/active.md` — it records the current system, which
   sections are complete, and (in all-systems mode) the full queue with each system marked
   done / current / pending.
2. Read `design/gdd/[system-name].md` for the **current** system — sections with real
   content are done; sections with `[To be designed]` still need work.
3. Resume from the next incomplete section of the current system — no need to re-discuss
   completed ones.
4. **All-systems mode**: once the current system is complete, continue the recorded queue
   from wherever it left off (do not re-run already-done systems, do not re-ask which
   systems to include — the queue from session state is authoritative). Running
   `/design-system` with no argument after an interruption picks this up automatically;
   it does not start building a new queue while one is already recorded as in-progress.

This is why incremental writing matters: every approved section survives any disruption,
and in all-systems mode so does the queue itself.

---

## Collaborative Protocol

This skill follows the collaborative design principle at every step:

1. **Question -> Options -> Decision -> Draft -> Approval** for every section
2. **AskUserQuestion** at every decision point (Explain -> Capture pattern):
   - Phase 2: "Ready to start, or need more context?"
   - Phase 3: "May I create the skeleton?"
   - Phase 4 (each section): technical questions and draft approval — design values come
     from the designer's docs, confirmed by the developer (§4 designer-fact rule)
   - Phase 5: "Run design review? Update systems index? What's next?" (single-system mode) /
     "Continue to the next system in the queue?" (all-systems mode)
3. **"May I write to [filepath]?"** before the skeleton and before each section write
4. **Incremental writing**: Each section is written to file immediately after approval
5. **Session state updates**: After every section write
6. **Cross-referencing**: Every section checks existing GDDs for conflicts
7. **Specialist routing**: only when the developer asks (§6) — never by default

**In all-systems mode, steps 2 through 5 repeat once per system in the queue** — the loop
is at the queue level (section 5e), not inside any individual step. Every rule above still
applies per system; running the whole index does not relax any single system's rigor.

**Never** auto-generate the full GDD and present it as a fait accompli.
**Never** write a section without user approval.
**Never** contradict an existing approved GDD without flagging the conflict.
**Never** silently skip the between-systems check-in (5e) in all-systems mode — advancing
the queue without it is the multi-system equivalent of skipping approval.
**Always** show where decisions come from (designer docs `file:line`, dependency GDDs,
pillars, or `[PLACEHOLDER]` from the developer).

## Context Window Awareness

This is a long-running skill, and all-systems mode is longer still. After writing each
section, check if the status line shows context at or above 70%. If so, append this notice
to the response:

> **Context is approaching the limit (≥70%).** Your progress is saved — all approved
> sections are written to `design/gdd/[system-name].md|design/gdd/systems-index.md]`. When
> you're ready to continue, open a fresh Claude Code session and run `/design-system`
> (all-systems mode: resumes the recorded queue exactly where it left off) or
> `/design-system [system-name]` (single-system mode) — it will detect which sections are
> complete and resume from the next one.

Treat 70% context as a valid answer to the all-systems "continue to the next system?"
check-in (5e) — recommend stopping there even if the user would otherwise say yes.

---

## Recommended Next Steps

- Run `/design-review design/gdd/[system-name].md` in a **fresh session** to validate each completed GDD independently
- Run `/consistency-check` to verify a GDD's values don't conflict with other GDDs
  (offered inline between systems in all-systems mode — see 5e)
- Run `/design-system` with no argument to design every remaining undesigned system in
  priority order; `/design-system [system-name]` to target just one
- Run `/review-all-gdds` when all MVP GDDs are authored and reviewed
