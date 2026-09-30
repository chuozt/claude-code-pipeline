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
**Out**: `design/gdd/gd-level-definition.md`

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
| Number and kinds of mechanics | `gd-mechanic-mix.md` section 4 |
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

## Next

- `/gd-level-intent` — **optional**, only if the designer has a per-level intent sheet
- Then phase 2 (`/gd-bot-playstyle` → `/gd-prototype-sim`) before any generation can be
  measured. Without it, `/gd-level-gen` has nothing to filter or score with.

Full pipeline: `/gd-workflow-help`
