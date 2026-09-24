---
name: gd-mechanic-difficulty
description: "Interview the designer to define where one mechanic creates difficulty — how it changes the legal move set, how play changes, which factor it loads, and the bot rule branch that goes with it. Run once per mechanic. Use when typing /gd-mechanic-difficulty or saying 'where does this mechanic get hard', 'add a new mechanic to the pipeline'."
argument-hint: "<mechanic-name> | all"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

Every mechanic creates difficulty in **its own way**. This skill names that way, and produces
the bot rule branch that keeps the bot from playing ignorantly on levels containing it.

**In**: the mechanic GDD + `difficulty-model.md` · **Out**: `design/pipeline/mechanics/<name>.md`

> **Base rule (frame section 5)**: you may not call a level "easy" or "hard" until every
> mechanic cluster in it is defined. And a mechanic must be defined on **both halves —
> the mechanism and the play**. Missing the second half → no bot → no measurement →
> "easy/hard" is just an opinion.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 4, 5**.
2. Read `design/pipeline/flow-map.md`, `difficulty-model.md`. Missing → stop, point at the
   earlier skill.
3. Find the mechanic's GDD: `design/gdd/*<name>*.md`. If present, read it — **take the
   mechanism from there, do not re-ask the designer**. If absent, interview both halves.
4. `all` → list every mechanic with no file in `design/pipeline/mechanics/`, ask which to do
   first, then loop through them.

## 1. Half one — the mechanism

If a GDD exists: **restate** the mechanism in the model's language and ask the designer to
confirm, rather than asking from scratch:

- How does this mechanic change the **legal move set**? (adds / removes / temporarily locks)
- How does it change the **OUTPUT structure**? (defers demand, merges demand, hides demand,
  changes capacity)
- Does it change **INPUT** or **MID** at all?
- **Attachment profile** (the input to `/gd-mechanic-object-mix` — ask all three):
  which **object type** does it attach to (host)? which **attachment/display slot** does it
  occupy (e.g. Key/Lock replacing the arrow = occupying the marker slot)? which **state
  dimension** does it touch (what it hides, locks, or alters)?

Anything the GDD leaves unclear → ask, do not infer.

## 2. Half two — the play ⭐

The core question, asked in prose:

> **"When a level has this mechanic, what does playing well mean — how does it differ from the core?"**

Dig until a machine could execute it. Two useful probes:
- "What mistake do new players make with this mechanic?" → that mistake is exactly what
  `Mistake` needs to simulate
- "In situation X, which move does your rule pick?" → if the designer has to think, the rule
  is still incomplete

## 3. Loading factor

Explain the 5 factors **using the mechanic being discussed**, then `AskUserQuestion`
(multiSelect): "Which factor does this mechanic mainly load?"
→ `DSL` / `MID slack` / `Hiddenness` / `Commitment` / `Perceptual`

Get the designer to name **one primary factor** and (if any) secondary factors. The distinction
matters: `/gd-mechanic-mix` uses the **primary factor** to derive the ban rules.

Reference example (from a tray-sorting game) to help the designer picture it:

| Mechanic | Primary factor | Secondary |
|---|---|---|
| Big tray (2X) | Commitment | — |
| Hidden tray | Hiddenness | — |
| Ice tray | DSL | Hiddenness |
| Connected trays | Commitment | merged demand |
| Pipe | Hiddenness | — |

## 4. Bot rule branch *(deferrable — see note)*

From half two, state the rule branch `GreedyBot` must learn, in the form
*"when facing <mechanic state>, prefer <action>"*.

> **This section belongs to the bot stage, not the level-generation stage.** If
> `/gd-bot-playstyle` has not run yet (no `bot-playstyle.md`) then **write `UNDEFINED` and move
> on** — half one, half two and the loading factor above are enough to generate levels and for
> `/gd-mechanic-mix` to derive the matrix. Fill this branch in when `/gd-bot-playstyle` runs,
> then update the mechanic table in `bot-playstyle.md`.

Include the **ignorant-bot trap warning** (frame section 5.4), stated plainly to the designer:

> If this mechanic has no rule branch in code, every level containing it must be marked
> `unscored` — the bot plays badly because **the bot is ignorant**, not because the level is
> hard. Scoring it then inflates difficulty for the wrong reason.

## 5. Write the file

Present the draft, then ask:
> "Write this to `design/pipeline/mechanics/<name>.md`?"

Structure: mechanism (and which GDD it came from) · play · primary + secondary factors · bot
rule branch · `scored` / `unscored` status · date + who was interviewed.

`UNDEFINED` for anything the designer has not decided. **Never fill it in.**

## Next

- More mechanics to do → run this skill again
- All mechanics done → `/gd-mechanic-object-mix` (object scale) **then**
  `/gd-mechanic-mix` (level scale). Both are needed before `/gd-level-definition`

Full pipeline: `/gd-workflow-help`
