---
name: gd-mechanic-object-mix
description: "Decide which mechanics one OBJECT can carry at the same time — checking self-referential deadlock, attachment-slot contention, and overlapping state dimensions. Different from gd-mechanic-mix (level scale). Use when typing /gd-mechanic-object-mix or saying 'can this piece be both X and Y', 'how many mechanics can one container carry', 'which mechanics can share an object'."
argument-hint: "[empty | <object> to examine one object type | <mech-A> <mech-B> to examine one pair]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

**Object** scale: which mechanics can one object (piece, container, tile, pipe…) carry simultaneously?
Different from `gd-mechanic-mix` (**level** scale — which mechanics may appear in one level).
A pair can be forbidden here yet allowed at level scale: Key and Lock are forbidden on the
same object, but of course a level must contain both.

**In**: `design/pipeline/mechanics/*.md` · **Out**: `design/pipeline/mechanic-object-mix.md`

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **section 5.3, especially
   5.3a** — the three checks are the law for this skill.
2. Read every `design/pipeline/mechanics/*.md`. Check each file has a section on
   **"which object it attaches to / which slot it occupies"** — if missing, ask for it here
   (and offer to update that mechanic file in return).
3. Grep the source GDD for combination bans the designer wrote by hand. **Separate the scale
   before using them**: a sentence like "on the same piece/container…" is object scale; "in the
   same level…" is level scale (hand that to `gd-mechanic-mix`). A sentence with unclear
   scale → ask the designer, do not guess.

## 1. Build each mechanic's attachment profile

For each mechanic, settle three facts (from the mechanic file, or by asking the designer):

| Fact | Example (a shooter-based game) |
|---|---|
| **Host**: which object type it attaches to | Shooter |
| **Slot occupied**: which physical/visual attachment point | Key/Lock replaces the arrow → occupies the marker slot |
| **State dimension touched**: what it hides, locks, or alters | Lock blocks movement; Super raises capacity |

## 2. Run the three checks on every same-host pair

In the order given by frame section 5.3a:

1. **Self-referential deadlock** — is A's unlock condition located on the very object B locks?
   → 🚫 FORBIDDEN. *(Key + Lock on the same shooter: the key sits on the locked object.)*
2. **Slot contention** — do both mechanics occupy the same attachment/display slot?
   → 🚫 FORBIDDEN, or ⚠️ if the designer is willing to redesign the display.
3. **Overlapping state dimension** — do both hide/lock the same dimension of information?
   → 🚫 FORBIDDEN (stacking is redundant or confusing, and adds no difficulty).

Passing all three → ✅ ALLOWED (orthogonal properties). *(A super shooter also carrying a key:
capacity and carried-object are independent.)*

Present the matrix **per host type** (one table per object type), each cell naming the check
that decided the verdict.

## 3. Designer confirmation + cross-check

- `AskUserQuestion`: confirm the whole matrix / override specific pairs (an override must
  state its reason).
- Cross-check against the hand-written object-scale bans from step 0.3: agreement → evidence;
  disagreement → **report it immediately, present both possibilities** (the attachment profile
  is wrong, or the old rule is a leftover) — never silently side with one.

## 4. Write the file

Present the draft, then ask:
> "Write this to `design/pipeline/mechanic-object-mix.md`?"

Structure: each mechanic's attachment profile · the matrix per host (each cell naming the
deciding check) · overridden cells + reasons · the agreement/disagreement table · date + who signed off.

`UNDEFINED` for anything the designer has not decided. **Never fill it in.**

## Boundary with gd-mechanic-mix

| | This skill (object) | `gd-mechanic-mix` (level) |
|---|---|---|
| Unit examined | one object | one level |
| Rule | 3 checks (deadlock/slot/state dimension) | loading factor (same factor = forbidden) |
| Consumer | the level tool / validator (blocking attachment at authoring time) | `gd-level-definition` (tier mixing formula) |

Both must be finished before `gd-level-definition` — the definition needs both matrices.

## Next

- `/gd-mechanic-mix` — the level-scale matrix, if not yet run
- `/gd-level-definition` — once both matrices are settled

Full pipeline: `/gd-workflow-help`
