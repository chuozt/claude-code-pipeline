---
name: gd-map-flow
description: "Map a puzzle game onto the GIVEN/BUFFER/GOAL model of the resource-flow frame — the mandatory gateway before any difficulty or level-generation work. Use when the developer/designer types /gd-map-flow or says 'map the game onto the frame', 'what is this game's GIVEN', 'start the level pipeline'."
argument-hint: "[game name, or leave empty to use the current game]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

The gateway to the whole level pipeline. Run **once per game**. Without this file the later
skills have no shared vocabulary to talk in.

**In**: the existing GDDs (`design/dev-system/`) · **Out**: `design/gdd/gdd-flow-map.md`

> **Question style — `gd-mode` §1, §3.1, §3.2, §5, §7 apply to every question below, mode on
> or off.** GIVEN / BUFFER / GOAL / invariant / `pressure(t)` are words for the **file**. The
> designer gets short questions, one idea each, in the language they are typing in. Once
> the designer names an object ("tray", "khay", "tube"), use **that name** in every later
> question — never the example nouns from this file.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **section 2** — that is the
   model definition and the law for this skill.
2. Read `design/gdd/gdd-game-concept.md` and `design/dev-system/dev-map-systems.md` if they exist. Note
   the object names already used there; still ask the designer to confirm them (step 1).
3. If `design/gdd/gdd-flow-map.md` already exists → read it, ask "update or rewrite?".

If there is no GDD at all: warn that mapping from memory will drift, suggest running
`/design-system` first — but allow continuing if the designer wants to.

## 1. Interview — the five components

Ask in order, **one row at a time**. Each row: what the answer becomes in the file, then the
question in English and in Vietnamese. Pick the language the designer is using.

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| **GIVEN** · ordered? · partially hidden? | "What does the player tap to play? What is that object called? Must they be taken in some order? Are all of them visible from the start, or are some covered?" | "Người chơi bấm vào cái gì để chơi? Tên gọi của object đó là gì? Có phải bấm theo thứ tự gì không? Ngay từ đầu đã thấy hết các object, hay có object bị che?" |
| **BUFFER** · capacity (`AskUserQuestion`) | "When the player taps an object and there is nowhere for it to go yet, where does it wait?" → `A waiting spot with a fixed number of slots` · `A waiting spot with no limit` · `Nothing waits — it can only be taken when there is a place for it` — if it waits: "What is that spot called? How many slots?" | "Bấm một object mà chưa có chỗ nhận thì nó đi đâu?" → `Nằm chờ ở chỗ có số ô cố định` · `Nằm chờ, không giới hạn` · `Không có chỗ chờ, chỉ bấm được khi có chỗ nhận` — nếu có chỗ chờ: "Tên gọi của chỗ chờ đó là gì? Chứa được mấy ô?" |
| **GOAL** (must be passive) | "What has to be filled to win? What is that object called? Does the player choose which one to fill, or does the object go to the right place by itself?" — if the player chooses, say so and redo: *"then the [name] is also something the player picks — let me redraw the picture"* | "Muốn thắng thì fill đầy cái gì? Tên gọi của object đó là gì? Người chơi có được chọn fill vào cái nào không, hay object tự bay về chỗ đúng?" — nếu chọn được thì nói luôn: *"vậy [tên] cũng là thứ người chơi chọn, để tôi vẽ lại"* |
| **ACTION** | "What is one move? Besides tapping, does anything else count as a move: rotate, swap, hold?" | "Một nước đi gồm những gì? Ngoài bấm ra còn thao tác nào tính là một nước không: xoay, đổi chỗ, giữ?" |
| **VISIBILITY** | "Before a move, what can the player already see coming? What do they have to guess?" | "Trước khi đi, người chơi thấy trước được gì? Cái gì phải đoán?" |

**Never say** "resource", "freely choose", "intermediate queue", "consume", "passive". If the
designer uses a framework word first, keep it.

## 2. Win / Lose

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| Win = `every GOAL satisfied` (default) | "What is the win condition? Just fill everything, or is there also a score target or a timer?" | "Điều kiện thắng là gì? Chỉ cần fill đầy hết, hay còn mốc điểm, đồng hồ?" |
| Lose, **with** BUFFER = `BUFFER full ∧ no unit matches an open GOAL` | "The player loses when the [waiting spot] is full and nothing in it fits any open [goal object] — right? Any other way to lose?" | "Thua là lúc [chỗ chờ] đầy mà không object nào khớp [chỗ nhận] đang mở, đúng không? Còn cách thua nào khác không?" |
| Lose, **no** BUFFER — **must ask**, cannot be blank | "When does the player lose? Out of moves, out of turns, out of time, or something else?" | "Người chơi thua khi nào? Hết nước đi, hết lượt, hết giờ, hay khác?" |

## 3. Conservation invariant

`AskUserQuestion`. One line of why first: *"this is the one rule every generated level is
checked against — get it wrong and every level is wrong"* / *"đây là luật mà mọi level sinh
ra đều bị check — sai luật này là sai hết level"*.

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| `Σ GIVEN(c) = Σ GOAL(c)` or `Σ GIVEN(c) ≥ Σ GOAL(c)` | "Pick any colour. Is the number of objects of that colour always exactly equal to the slots for it, or can a level have extra objects left over?" → `Always exactly equal` · `Extras allowed` | "Đếm một màu bất kỳ. Số object màu đó luôn vừa khít số ô nhận, hay có level dư object?" → `Luôn vừa khít` · `Được dư` |

## 4. The three consequences of BUFFER — derived, not asked

Fill this table into the file; the values follow from the BUFFER answer:

| | Value for this game |
|---|---|
| Lose condition | *(from section 2)* |
| Factor 2 *Buffer Room* | applies / **N/A** |
| `pressure(t)` implementation | `BUFFER occupancy ÷ capacity` / `[DEV DEFAULT]` *(see below)* |

If there is **no BUFFER**: write `pressure(t) = 1 − (legal moves remaining ÷ at match start)`
tagged `[DEV DEFAULT]` (alternative: `turns used ÷ turns allowed`). Tell the designer in one
line, nothing to decide (`gd-mode` §3.2):
- EN: *"How 'close to losing' gets counted in a game with no waiting spot is the developer's
  call — nothing for you here."*
- VI: *"Cách đếm 'sắp thua đến đâu' dev sẽ chốt, không cần quyết ở đây."*

## 5. Mismatches

| Ask (EN) | Hỏi (VI) |
|---|---|
| "Is there anything in the game the picture we just drew does not cover? For example: a clock, an opponent, randomness between levels, a booster that breaks the rules." | "Có gì trong game mà khung vừa vẽ chưa nói tới không? Ví dụ: đồng hồ, đối thủ, random giữa các level, booster phá luật." |

Write it straight into the file. **Never hide it** — this is where the frame needs extending
or the game needs its own proxy, and later skills read this section to know where the
numbers cannot be trusted.

## 6. Write the file — two columns

Present the full draft in conversation, then ask:
> "Write this to `design/gdd/gdd-flow-map.md`?"

The five components and the invariant are a **two-column table** (`gd-mode` §8) — the
designer's own words and object names on the left, the framework value on the right:

| Component | In the designer's words | Framework value |
|---|---|---|
| GIVEN | "viên" on the board, any order, some covered | ordered: no · partially hidden: yes |
| BUFFER | "khay chờ", 7 slots | capacity 7 |
| … | | |

File structure: the 5-component table · win/lose · invariant · the three-BUFFER-consequences
table · mismatches · date + who was interviewed.

Anything the designer has not decided → write `UNDEFINED`. **Never fill it in.**

## Next

`/gd-core-difficulty` — define what makes it hard and how the core is played.

Full pipeline: `/gd-workflow-help`
