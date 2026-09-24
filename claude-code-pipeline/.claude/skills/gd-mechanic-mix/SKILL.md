---
name: gd-mechanic-mix
description: "Derive the LEVEL-scale mechanic combination matrix from loading factors — which pairs are forbidden, which are allowed — and the mixing guidance that produces Normal/Hard/SuperHard levels. Use when typing /gd-mechanic-mix or saying 'which mechanics go together', 'how do I mix for difficulty', 'is this mechanic pair OK'."
argument-hint: "[empty | <mechanic-A> <mechanic-B> to examine one pair]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

**Level** scale: which mechanics may appear together in one level — derived from their
loading factors. Different from `/gd-mechanic-object-mix` (**object** scale: what one object can
carry). A pair can be forbidden at object scale yet allowed at level scale.

Once each mechanic's loading factor is known, which pairs combine well and which combine badly
is **derivable** — no trial and error needed.

**In**: `design/pipeline/mechanics/*.md` · **Out**: `design/pipeline/mechanic-mix.md`

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 5.2, 5.3b**.
2. Read **every** file in `design/pipeline/mechanics/`. If a mechanic has no file → list them
   and ask: run `/gd-mechanic-difficulty` for those first, or derive the matrix from what
   exists and mark the gaps?
3. Grep the source GDD for combination bans the designer **already wrote**. **Separate the
   scale**: a sentence like "on the same piece/container…" is OBJECT scale → hand it to
   `/gd-mechanic-object-mix`, do not judge it here; keep only level-scale sentences ("does not
   combine with…", "can combine with…"). Retain them for the cross-check in section 3.

## 1. Derive the matrix

For every mechanic pair, apply the frame's rule:

| Factor relationship | Verdict | Why |
|---|---|---|
| **Same primary factor** | 🚫 FORBIDDEN | they multiply → the player experiences **luck**, not difficulty |
| Different primary factor, overlapping secondary | ⚠️ CAUTION | additive but partly redundant — needs playtest confirmation |
| Entirely different | ✅ ALLOWED | additive and still readable |

Present the full matrix as a table, each cell naming the factor that produced the verdict.

## 1b. Challenge the matrix

The "same factor = forbidden" rule is **one-directional** reasoning — it catches pairs that
break through factor overlap, but not pairs on **different factors that still conflict** (for
example two mechanics both demanding spare capacity for different reasons).

**Spawn `systems-designer`** (Task) to look the other way. The prompt must include:
- excerpts from **sections 4 and 5.2–5.3** of `resource-flow-difficulty-framework.md`
- all of `design/pipeline/mechanics/*.md`
- the matrix just derived in section 1
- the request: *"find pairs marked ALLOWED that actually conflict, and pairs marked FORBIDDEN
  that might still work; justify by factor. **Return analysis, DO NOT write files**"*

Put the agent's findings into section 2 as a separate opinion column — **never edit the matrix
to match the agent**. The designer decides.

## 2. Present to the designer for confirmation

`AskUserQuestion`: "This is the derived matrix. Confirm, or are there pairs to override?"
→ `Confirm all` / `Override some pairs` / `Review pair by pair`.

For any override: **ask for the reason and record it in the file**. An override without a
reason destroys the ability to derive anything for future mechanics.

## 3. Cross-check against the designer's existing bans ⭐

Compare the derived matrix against what the designer hand-wrote in the GDD (collected in 0.3):

- **Agreement** → cite it as evidence the frame is sound, raising confidence in future derivations
- **Disagreement** → **report immediately, never silently side with either**. A disagreement
  means one of the two is wrong: either the assigned loading factor is off, or the old ban is a
  leftover or a mistake. Present both possibilities for the designer to choose.

This is the most valuable section of the skill — it tests the frame itself against the
designer's accumulated intuition.

## 4. Mixing guidance per tier

From the matrix, propose a mixing formula for the three tiers and get it confirmed:

| Tier | Starting proposal |
|---|---|
| Normal | 0–1 mechanic; wide slack |
| Hard | 2 mechanics on **different factors**; medium slack |
| SuperHard | 2–3 mechanics on different factors + tightened slack + a late commitment point |

Say clearly that this is a **starting point**; the real win-rate bands in
`difficulty-model.md` are the arbiter — after mixing, it still has to be measured.

## 5. Write the file

Present the draft, then ask:
> "Write this to `design/pipeline/mechanic-mix.md`?"

Structure: the full matrix · overridden cells + reasons · **the cross-check table against the
old bans (agreement / disagreement)** · the three-tier mixing formula · date + who signed off.

## Next

- `/gd-mechanic-object-mix` if not yet run — `/gd-level-definition` needs BOTH matrices
- `/gd-level-definition` — define the level generation space
- Adding a new mechanic later → `/gd-mechanic-difficulty` then re-run this skill;
  the matrix is extended, not rewritten

Full pipeline: `/gd-workflow-help`
