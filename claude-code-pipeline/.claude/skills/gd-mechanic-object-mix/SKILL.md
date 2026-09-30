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

> **Question style — `gd-mode` §1, §3.1, §3.2, §5, §7 apply, mode on or off.** The three
> checks are **run by you, not asked**. The designer sees each verdict as one plain sentence
> about what would happen to the player, then confirms or overrides. "Self-referential
> deadlock", "slot contention", "state dimension" never reach the designer.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **section 5.3, especially
   5.3a** — the three checks are the law for this skill.
2. Read every `design/pipeline/mechanics/*.md`. Each must have an **attachment profile**
   (host · slot · what it hides/locks). If one is missing, ask the three questions from
   `/gd-mechanic-difficulty` §1 (host / slot / state rows) and offer to update that file.
3. Grep the source GDD for combination bans the designer wrote by hand. **Separate the scale
   before using them**: a sentence like "on the same piece/container…" is object scale; "in the
   same level…" is level scale (hand that to `gd-mechanic-mix`). A sentence with unclear
   scale → ask, do not guess:

| Ask (EN) | Hỏi (VI) |
|---|---|
| "You wrote that X and Y do not go together. Do you mean on the same [object], or anywhere in the same level?" | "Bạn có ghi X và Y không đi cùng nhau. Ý là trên cùng một [object], hay trong cùng một level?" |

## 1. Build each mechanic's attachment profile

For each mechanic, three facts (from the mechanic file, or by asking):

| Fact | Example (a shooter-based game) |
|---|---|
| **Host**: which object type it attaches to | Shooter |
| **Slot occupied**: which physical/visual attachment point | Key/Lock replaces the arrow → occupies the marker slot |
| **State dimension touched**: what it hides, locks, or alters | Lock blocks movement; Super raises capacity |

## 2. Run the three checks on every same-host pair — you do this, silently

In the order given by frame section 5.3a:

1. **Self-referential deadlock** — is A's unlock condition located on the very object B locks?
   → 🚫 FORBIDDEN.
2. **Slot contention** — do both mechanics occupy the same attachment/display slot?
   → 🚫 FORBIDDEN, or ⚠️ if the designer is willing to redesign the display.
3. **Overlapping state dimension** — do both hide/lock the same dimension of information?
   → 🚫 FORBIDDEN (stacking is redundant or confusing, and adds no difficulty).

Passing all three → ✅ ALLOWED (orthogonal properties).

Present the matrix **per host type** (one table per object type). Each cell carries the
verdict **and one plain sentence** the designer can picture:

| Check that fired | Say to the designer (EN) | Nói với designer (VI) |
|---|---|---|
| deadlock | "The key would sit on the very [object] that is locked — the player could never get it." | "Chìa khóa nằm ngay trên [object] đang bị khóa — người chơi không bao giờ lấy được." |
| slot | "Both want to show in the same spot on the [object] — one would cover the other." | "Cả hai đều hiện ở cùng một chỗ trên [object] — cái này sẽ che cái kia." |
| state | "Both hide the same thing — stacking them adds confusion, not difficulty." | "Cả hai cùng giấu một thứ — chồng lên nhau chỉ thêm rối, không thêm khó." |
| none | "Independent — one changes what the [object] holds, the other what it shows." | "Độc lập — một cái đổi thứ [object] chứa, cái kia đổi thứ nó hiện." |

## 3. Designer confirmation + cross-check

`AskUserQuestion`: `Confirm all` / `Override some pairs` / `Go pair by pair`.

| Ask (EN) | Hỏi (VI) |
|---|---|
| "Here is which mechanics can share one [object]. Anything you disagree with?" | "Đây là bảng mechanic nào được nằm chung một [object]. Có chỗ nào bạn thấy sai không?" |
| for an override: "Why should this pair be allowed / forbidden?" | với override: "Vì sao cặp này nên được / không được?" |

An override must state its reason — it is recorded in the file.

Cross-check against the hand-written object-scale bans from step 0.3: agreement → evidence;
disagreement → **report it immediately, present both possibilities** (the attachment profile
is wrong, or the old rule is a leftover) — never silently side with one:

| Say (EN) | Nói (VI) |
|---|---|
| "Your doc says X and Y cannot share a [object], but by their profiles they could. Either the doc rule is old, or one of the profiles is wrong. Which?" | "Doc của bạn ghi X và Y không nằm chung [object] được, nhưng theo profile thì được. Hoặc luật trong doc cũ rồi, hoặc profile sai. Bạn thấy sao?" |

## 4. Write the file

Present the draft, then ask:
> "Write this to `design/pipeline/mechanic-object-mix.md`?"

Structure: each mechanic's attachment profile · the matrix per host (each cell: verdict ·
the check · the plain sentence) · overridden cells + reasons · the agreement/disagreement
table · date + who signed off.

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
