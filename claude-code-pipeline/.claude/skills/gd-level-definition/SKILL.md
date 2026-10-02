---
name: gd-level-definition
description: "Interview the designer to define HOW levels are generated — what is human-owned identity, what is a free variable for the generator, which constraints are mandatory, and the target profile per tier. Use when typing /gd-level-definition or saying 'define how levels are generated', 'what is the machine allowed to invent', 'the level generation formula'."
argument-hint: "[empty]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

Define the **generation space** before generating. Without this step the generator either
produces nonsense or destroys the game's artistic identity.

**In**: flow-map · difficulty-model · mechanic-mix · mechanic-object-mix ·
**Out**: `design/gdd/gd-level-definition.md` (+ `design/gdd/gd-level-roadmap.md` when levels come in blocks)

> **Question style — `gd-mode` §1, §3.1, §3.2, §5, §7 apply, mode on or off.** "Free
> variable", "constraint", "validator", "DIG profile", "search strategy" are words for the
> file. Search strategy and violation handling are **developer defaults, not questions**.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **section 10** (the generation
   engine) and **section 2** (the conservation invariant).
2. Read `design/gdd/gd-flow-map.md`, `gd-difficulty-model.md`, `gd-mechanic-mix.md`,
   `gd-mechanic-object-mix.md`. Any missing → stop and point at the matching skill. Use the
   object names from the flow map in every question.
3. Read the level/tool GDD if it exists, and any existing level table (if the designer has
   hand-built a few levels — they are valuable examples for asking "was this deliberate or
   incidental?").

## 1. Separate Identity from Difficulty ⭐

This is the central question. Explain the principle first, in one line:
- EN: *"Art and theme stay human-made. Whatever only decides how hard a level is, the
  machine may change — from the same art it makes the whole Normal → SuperHard range."*
- VI: *"Art và theme là của người. Cái gì chỉ quyết định độ khó thì máy được đổi — từ cùng
  một bộ art, máy làm ra cả dải Normal → SuperHard."*

Then, one row at a time:

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| **identity** list (human-owned) | "What in a level must stay exactly as you made it, or the level stops looking right? For example: the colours on the outside, the shape, the theme." | "Trong một level, cái gì phải giữ nguyên như bạn làm, đổi là level nhìn sai? Ví dụ: màu lớp ngoài, hình dáng, theme." |
| **free** list (machine-generated) | "What is the player not able to see at the start, or does not care how it looks — so the machine may decide it? For example: the colours underneath, the order objects come out." | "Cái gì người chơi chưa thấy lúc bắt đầu, hoặc không quan tâm nó trông thế nào — nên máy được tự quyết? Ví dụ: màu lớp dưới, thứ tự object xuất hiện." |
| if the designer built levels by hand | "In level [N], was [this detail] on purpose, or did it just end up that way?" | "Ở level [N], [chi tiết này] là cố ý, hay ngẫu nhiên nó thành thế?" |

Settle these into two explicit lists. Anything the designer is unsure about → leave
`UNDEFINED`, and the generator **will not touch it** until there is a decision.

## 2. Mandatory constraints

Read them back **as plain rules** and get each confirmed — these are what the validator guards:

| Writes to file | Say (EN) | Nói (VI) |
|---|---|---|
| conservation invariant (`=` / `≥`, from flow map) | "Each colour comes out [exactly even / with extras allowed] — still right?" | "Mỗi màu [vừa khít / được dư] — vẫn đúng chứ?" |
| valid types + max counts | "Which colours or kinds are allowed, and at most how many of each in one level?" | "Được dùng những màu / loại nào, mỗi loại tối đa bao nhiêu trong một level?" |
| capacity (if BUFFER) | "The [waiting spot] has [N] slots — fixed for every level, or can it vary?" | "[Chỗ chờ] có [N] ô — cố định mọi level, hay có level khác?" |
| level-scale bans (`gd-mechanic-mix.md`) | "These mechanics never appear in the same level: [list]. Confirm?" | "Các mechanic này không bao giờ chung một level: [list]. Đúng chứ?" |
| object-scale bans (`gd-mechanic-object-mix.md`) | "These never sit on the same [object]: [list]. Confirm?" | "Các cặp này không bao giờ nằm chung một [object]: [list]. Đúng chứ?" |
| game-specific | "Any other rule a level must never break?" | "Còn luật nào khác mà level không được phá không?" |

**Violation handling — not asked.** Write `[DEV DEFAULT]`: skip the candidate during
generation, raise an error when validating a hand-authored level.

## 3. Target profile per tier

For each tier (Normal / Hard / SuperHard), assemble one profile:

| Component | Source |
|---|---|
| `GreedyBot` win-rate band | `gd-difficulty-model.md` |
| `pressure` curve shape | frame section 8 |
| Tension levers (room, digging, late no-undo) | `gd-mechanic-mix.md` section 4 |
| Which mechanics, how many | the roadmap row of each level (§6), within the matrices |
| Target DIG profile | **asked here** |

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| DIG profile per tier | "In a [tier] level, how deep should the player usually have to dig for the colour they need? Almost never, a layer or two, or several layers with stretches where they must throw things away first?" | "Trong level [tier], người chơi thường phải đào sâu bao nhiêu mới tới màu cần? Gần như không, một hai lớp, hay nhiều lớp và có đoạn phải bỏ bớt mới đào được?" |

Turn the answer into something quantifiable for the file (e.g. "average DIG ≤ 1 layer" /
"2–3 layers" / "≥ 3 layers with stretches that force discarding").

## 4. Search strategy and candidate count

**Search strategy — not asked.** Write `[DEV DEFAULT]`: hill-climb toward the target DIG
(alternatives for the developer: random + filter; exhaustive when the space is small).

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| candidates per run | "Each time the machine makes levels, how many options do you want to see before picking? 3 to 5 is usual." | "Mỗi lần máy sinh level, bạn muốn xem mấy phương án rồi mới chọn? Thường 3 đến 5." |

State clearly: **a human is always the final gate** — the generator never puts a level into
the build by itself. *(VI: "Người luôn là cửa cuối — máy không tự đưa level nào vào build.")*

## 5. Write the file

Present the draft, then ask:
> "Write this to `design/gdd/gd-level-definition.md`?"

Structure: the two identity/free lists · constraints + `[DEV DEFAULT]` violation behaviour ·
the three tier profiles · `[DEV DEFAULT]` search strategy + candidate count · date + who signed off.
The roadmap (§6) goes in its own file, `gd-level-roadmap.md`.

## 6. Level roadmap — only if levels come in staged blocks

Skip when levels are not staged (one designer, one flat set). Ask once:

| Ask (EN) | Hỏi (VI) |
|---|---|
| "Do levels come in blocks, with a new mechanic introduced at set levels (for example every 10 levels)?" | "Level chia theo block không, mỗi block giới thiệu một mechanic mới ở level cố định (ví dụ mỗi 10 level)?" |

If yes, this is the **lead designer's file** — one owner, because several block owners will
read it. It lets each block owner work alone: they follow the roadmap, they do not need
anybody else's levels. Nothing here is learned from existing levels; the generator never uses
other levels as examples.

### 6a. First, the block rhythm — the designer defines it, not you

Before any per-level row, ask the designer what one block of levels should feel like, as a
string of tiers, one letter per level: **N** = Normal · **H** = Hard · **S** = SuperHard.

| Ask (EN) | Hỏi (VI) |
|---|---|
| "How should one block of [10] levels flow, level by level? For example `N N N N H N N N N S`: four easy, one hard, four easy, then the big one. Keep this, or change it?" | "Một block [10] level nên chảy thế nào, từng level một? Ví dụ `N N N N H N N N N S`: bốn dễ, một khó, bốn dễ, rồi level lớn cuối block. Giữ nguyên, hay đổi?" |

- `N N N N H N N N N S` is **only a suggestion** (the usual rhythm), shown as an example so
  the designer has something to react to. Whatever they answer is what gets written.
- Then ask, one at a time: "Is every block the same, or does some block differ — the first
  block with no mechanic yet, for instance?" and "Is the new mechanic introduced at position 1
  of the block, and are positions 2–4 practice with that mechanic alone?" *(Position 1 =
  introduces, 2–4 = practice alone is the common answer; record whatever they say.)*
- Tier names mean what `gd-level-definition` §3 and `gd-difficulty-model` say. Do not
  re-define them here, and do not suggest tiers for the designer.

Writes to the file: the rhythm string, the list of blocks it applies to, per-block overrides,
and which positions introduce / practise a mechanic. **Every level's tier then comes from its
position** — it is not written again per level.

### 6b. One row per level — role · mechanics allowed

Tier is read from the rhythm; the row only adds what the rhythm cannot say:

| Level | Position | Tier (from rhythm) | Role | Mechanics allowed |
|---|---|---|---|---|
| 11 | 1 | N | introduces A | A alone |
| 12–14 | 2–4 | N | practice A | A |
| 15 | 5 | H | first mix | A + core |
| 20 | 10 | S | block finale | A |
| 21 | 1 | N | introduces B | B alone |
| 25 | 5 | H | first mix of A and B | A + B |

*(Illustrative — rows follow the rhythm the designer actually chose.)*

- "Mechanics allowed" may only contain mechanics already introduced at or before that level,
  and every pair must be allowed by `gd-mechanic-mix.md`. A conflict is reported, not fixed
  silently (same rule as `/gd-level-intent` §2).
- Roles reuse the vocabulary of `/gd-level-intent` ("teaches X", "breather", "combined
  challenge / boss"). Tier and rhythm are the designer's call; you only record them.
- If the rhythm puts a tier on a position whose role cannot reach it (for example `S` on a
  level that only allows the core), report it — a tier is a difficulty profile, not a mechanic
  count — and let the designer decide.

**One boundary contract per block** — what the block must hand over and receive, so the next
owner does not wait for this one:

| Ask (EN) | Hỏi (VI) |
|---|---|
| "At the first level of block [N], out of 10 players trying it for the first time, how many should win? And at the last level?" | "Ở level đầu block [N], 10 người chơi lần đầu thì mấy người nên thắng? Còn level cuối block?" |
| "Right after a new mechanic appears, should difficulty drop, or keep climbing?" (the rhythm usually answers this: first level of a block = N) | "Ngay khi có mechanic mới, độ khó nên thả xuống hay vẫn leo tiếp?" (nhịp block thường đã trả lời: level đầu block = N) |

Writes to the file as a table: block · entry level + tier + win-rate band · exit level + tier +
win-rate band. Changing a contract later is the **lead's decision**, announced to the affected
owners, not agreed between two owners in passing.

Write to `design/gdd/gd-level-roadmap.md` (ask first). Anything undecided → `UNDEFINED`.
Optional, never required: each owner may finish 1–2 **anchor levels** (the introducing level
and one practice level) first, so neighbours can eyeball the mechanic early. The generator
does not need them.

## Next

- `/gd-level-intent` — **optional**, only if the designer has a per-level intent sheet.
  With a roadmap, each block owner runs it for their own range (`/gd-level-intent <file> 21-30`)
- Then phase 2 (`/gd-bot-playstyle` → `/gd-prototype-sim`) before any generation can be
  measured. Without it, `/gd-level-gen` has nothing to filter or score with.

Full pipeline: `/gd-workflow-help`
