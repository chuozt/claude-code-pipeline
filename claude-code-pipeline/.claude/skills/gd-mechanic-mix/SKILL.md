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

**In**: `design/gdd/gdd-mechanics/*.md` · **Out**: `design/gdd/gdd-mechanic-mix.md`

> **Question style — `gd-mode` §1, §3.1, §3.2, §5, §7 apply, mode on or off.** The matrix is
> **derived by you, not asked**. The designer sees each verdict as one plain sentence, then
> confirms or overrides. "Loading factor", "same primary factor" never reach the designer —
> say what the player would feel instead.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 5.2, 5.3b**.
2. Read **every** file in `design/gdd/gdd-mechanics/`. If a mechanic has no file → list them
   and ask: run `/gd-mechanic-difficulty` for those first, or derive the matrix from what
   exists and mark the gaps?
3. Grep the source GDD for combination bans the designer **already wrote**. **Separate the
   scale**: a sentence like "on the same piece/container…" is OBJECT scale → hand it to
   `/gd-mechanic-object-mix`, do not judge it here; keep only level-scale sentences ("does not
   combine with…", "can combine with…"). Retain them for the cross-check in section 3.

## 1. Derive the matrix — you do this, silently

For every mechanic pair, apply the frame's rule:

| Factor relationship | Verdict | Say to the designer (EN) | Nói với designer (VI) |
|---|---|---|---|
| **Same primary factor** | 🚫 FORBIDDEN | "Both make the game hard in the same way — together the player feels unlucky, not challenged." | "Cả hai làm khó theo cùng một kiểu — gộp lại người chơi thấy xui chứ không thấy khó." |
| Different primary, overlapping secondary | ⚠️ CAUTION | "Different kinds of hard, but they overlap a bit — needs a playtest to be sure." | "Hai kiểu khó khác nhau nhưng có chồng một phần — cần playtest mới chắc." |
| Entirely different | ✅ ALLOWED | "Two different kinds of hard — they add up and the player can still read the level." | "Hai kiểu khó khác nhau — cộng dồn được, người chơi vẫn đọc được level." |

Present the full matrix as a table, each cell: verdict · the plain sentence · (factor name in
small print for the file).

## 1b. Challenge the matrix

The "same factor = forbidden" rule is **one-directional** reasoning — it catches pairs that
break through factor overlap, but not pairs on **different factors that still conflict** (for
example two mechanics both demanding spare capacity for different reasons).

**Spawn `systems-designer`** (Task) to look the other way. The prompt must include:
- excerpts from **sections 4 and 5.2–5.3** of `resource-flow-difficulty-framework.md`
- all of `design/gdd/gdd-mechanics/*.md`
- the matrix just derived in section 1
- the request: *"find pairs marked ALLOWED that actually conflict, and pairs marked FORBIDDEN
  that might still work; justify by factor. **Return analysis, DO NOT write files**"*

Put the agent's findings into section 2 as a separate opinion column, **rewritten as plain
sentences** — never edit the matrix to match the agent. The designer decides.

## 2. Present to the designer for confirmation

`AskUserQuestion`: `Confirm all` / `Override some pairs` / `Go pair by pair`.

| Ask (EN) | Hỏi (VI) |
|---|---|
| "Here is which mechanics can appear in the same level. Anything you disagree with?" | "Đây là bảng mechanic nào được xuất hiện chung một level. Có chỗ nào bạn thấy sai không?" |
| for an override: "Why should this pair be allowed / forbidden?" | với override: "Vì sao cặp này nên được / không được?" |

An override must state its reason — it is recorded in the file. An override without a reason
destroys the ability to derive anything for future mechanics.

## 3. Cross-check against the designer's existing bans ⭐

Compare the derived matrix against what the designer hand-wrote in the GDD (collected in 0.3):

- **Agreement** → cite it as evidence the frame is sound, raising confidence in future derivations
- **Disagreement** → **report immediately, never silently side with either**:

| Say (EN) | Nói (VI) |
|---|---|
| "Your doc bans X with Y in one level, but by the kinds of hard they add they should be fine together. Either the ban is from an older version, or one of them is a different kind of hard than we said. Which?" | "Doc của bạn cấm X đi cùng Y trong một level, nhưng theo kiểu khó của từng cái thì đi chung được. Hoặc lệnh cấm này từ bản cũ, hoặc một trong hai có kiểu khó khác với lúc nãy nói. Bạn thấy sao?" |

This is the most valuable section of the skill — it tests the frame itself against the
designer's accumulated intuition.

## 4. Mixing guidance per tier

Propose, then confirm with `AskUserQuestion`:

| Tier | Starting proposal (file) | Say to the designer (EN) | Nói với designer (VI) |
|---|---|---|---|
| Normal | 0–1 mechanic; wide slack | "At most one mechanic, lots of room in the [waiting spot]" | "Tối đa một mechanic, [chỗ chờ] còn rộng" |
| Hard | 2 mechanics on **different factors**; medium slack | "Two mechanics that are hard in different ways, less room" | "Hai mechanic khó theo hai kiểu khác nhau, chỗ chờ hẹp hơn" |
| SuperHard | 2–3 mechanics on different factors + tightened slack + a late commitment point | "Two or three, tight room, and one big can't-undo moment near the end" | "Hai đến ba mechanic, chỗ chờ rất hẹp, và một nước không rút lại được gần cuối" |

Say clearly that this is a **starting point**; the real win-rate bands in
`gdd-difficulty-model.md` are the arbiter — after mixing, it still has to be measured.

## 5. Write the file

Present the draft, then ask:
> "Write this to `design/gdd/gdd-mechanic-mix.md`?"

Structure: the full matrix · overridden cells + reasons · **the cross-check table against the
old bans (agreement / disagreement)** · the three-tier mixing formula · date + who signed off.

## Next

- `/gd-mechanic-object-mix` if not yet run — `/gd-level-definition` needs BOTH matrices
- `/gd-level-definition` — define the level generation space
- Adding a new mechanic later → `/gd-mechanic-difficulty` then re-run this skill;
  the matrix is extended, not rewritten

Full pipeline: `/gd-workflow-help`
