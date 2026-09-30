---
name: gd-core-difficulty
description: "Interview the designer to define what makes the core gameplay hard — rank the 5 factors, settle the visibility rule, define the perceptual proxy, set weights and target win-rate bands. Does NOT define the bot (that is /gd-bot-playstyle). Use when typing /gd-core-difficulty or saying 'define difficulty', 'what makes this game hard', 'what does easy or hard mean for a level'."
argument-hint: "[empty]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

Turn "difficulty" from a feeling into something measurable. Run **once per game**, right
after `gd-map-flow`.

**In**: `design/pipeline/flow-map.md` · **Out**: `design/pipeline/difficulty-model.md`

> **Question style — `gd-mode` §1, §3.1, §3.2, §5, §7 apply, mode on or off.** Factor names
> (DIG, Buffer Room, Hiddenness, Commitment, Perceptual) and `w₁..w₅` are for the file. The
> designer hears each factor as a moment of play, using the object names from `flow-map.md`.

> **Scope — only the "what makes it hard" half.** The other half, *"what does playing well
> mean"* → the bot's playstyle rules, which belong to `/gd-bot-playstyle` and run later, right before
> `/gd-prototype-sim`.
>
> They are separated because **generating levels needs no bot**: the layout, the colours, the
> order pieces arrive in, difficulty points — none of them ask what a bot thinks. A bot is only needed once
> **measurement** starts. That lets the designer go straight from here to
> `/gd-level-definition` and produce real levels before a single line of simulation exists.
>
> **One exception must be settled here and cannot be deferred: the visibility rule
> (section 1b).** That is a game rule, not a bot rule — if a piece is never sufficiently
> exposed, the generated level is unwinnable no matter what the colours and the arrival order are.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 4, 5.1, 6, 8**.
2. Read `design/pipeline/flow-map.md`. **Missing → stop**:
   > "There is no flow map. Run `/gd-map-flow` first — without the model there is no way to
   > know which factors apply."
   Take the object names from its designer column and use them in every question below.
3. Read the gameplay GDDs in `design/gdd/` for concrete examples to use in the questions.

## 1. Rank the 5 factors

For **each factor**, one question, then `AskUserQuestion`: `Often` / `Sometimes` / `Rarely` /
`Never` (writes to file as High / Medium / Low / N/A).

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| 1 · **DIG** | "How often does the player know which colour they need but it is buried and they have to dig for it?" | "Người chơi có hay gặp cảnh biết cần màu gì nhưng nó bị vùi, phải đào mới tới không?" |
| 2 · **Buffer Room** *(skip if flow map says N/A)* | "How often is the [waiting spot] filling up the main reason a level feels tense?" | "Cảm giác căng của level có hay đến từ việc [chỗ chờ] sắp đầy không?" |
| 3 · **Hiddenness** | "How often does the player have to guess because they cannot see what is coming?" | "Người chơi có hay phải đoán vì chưa thấy được cái gì sắp tới không?" |
| 4 · **Commitment** | "How often does the player make a move they cannot take back?" | "Có hay gặp nước đi mà đi rồi không rút lại được không?" |
| 5 · **Perceptual** | "How often does the player know exactly what to do but struggle to see or find it on screen?" | "Có hay gặp cảnh biết phải làm gì rồi nhưng khó nhìn ra hoặc khó tìm trên màn hình không?" |

Warn the designer about factor 5, in plain words: this is the one thing **a bot cannot
feel**. If it is `Often`, every later bot number is missing a piece, and a round with real
players becomes mandatory. *(VI: "cái này bot không cảm được — nếu Thường xuyên thì số của bot
sẽ thiếu một phần, bắt buộc phải test với người thật.")*

## 1b. The visibility rule ⭐ *(mandatory — hard-blocks level generation)*

Skip if the flow map says GIVEN is **never** covered (every move is always visible).

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| exposure threshold | "An object is showing only a sliver from behind another. The player taps it. Does the game accept it, or must it be clearly uncovered first?" | "Một object chỉ ló ra một mẩu sau object khác. Người chơi bấm vào. Game nhận hay phải lộ rõ mới nhận?" |
| direction count | "How often is it OK for the player to clearly see something, tap it, and have the game refuse — never, or rarely?" | "Người chơi thấy rõ, bấm mà game không nhận — chấp nhận không bao giờ, hay thỉnh thoảng?" |
| circle / sphere | "Does the view turn left-right only, or can the player also tilt it up-down?" | "Góc nhìn chỉ xoay trái-phải, hay còn nghiêng lên-xuống được?" |

Then translate into the two numbers the developer needs and present the **two-column
table** for confirmation (`gd-mode` §7):

| Decision (designer reads) | Number (developer reads) |
|---|---|
| generous tapping / must be clearly exposed | **exposure threshold** (e.g. ~3/4 of projected area) |
| "refuse rarely" / "refuse never" | **number of discrete directions** in the visibility set |
| turns one way / two ways | the direction set lies on a **circle** or a **sphere** |

Three things to tell the designer, in plain words:

1. **The stricter "must be clearly uncovered" is, the fewer view angles the game has to
   check** — the edge cases are already excluded.
2. **Strict exposure makes factor 5 (hard to see) heavier** — the player must find the object
   *and* turn until it is fully out. If factor 5 was just ranked, revisit it.
3. **Changing this later means re-checking the angle count.** Write that warning into the file.

This is the `coveredBy(unit, direction)` data that both the level tool and the simulation
read — without it, no trustworthy level can be generated.

## 2. PerceptualProxy

Only if factor 5 ≠ `Never`.

| Ask (EN) | Hỏi (VI) |
|---|---|
| "What exactly makes it hard to see? Similar colours, too many objects, things behind other things, small objects, the view angle?" | "Cụ thể cái gì làm khó nhìn? Màu giống nhau, quá nhiều object, bị che, object nhỏ, góc nhìn?" |

**Spawn `systems-designer`** (Task) to propose a formula — the designer knows *what* causes
eye strain but usually cannot write it as something countable. The prompt must include:
- excerpts from **sections 4 and 6** of `resource-flow-difficulty-framework.md`
- `flow-map.md` (this game's model)
- the designer's answer above
- the request: *"propose 2–3 candidate PerceptualProxy formulas, each with a variable table +
  value ranges; **return analysis, DO NOT write files**"*

Present the candidates with `AskUserQuestion`, each option described by **what it counts, in
plain words** ("counts how many similar colours sit next to each other"), never by the
formula. The designer decides; the session owner writes.

Coefficients → `UNDEFINED` — calibration will find them. Do not ask the designer for
numbers here; what matters is settling **what gets counted**.

## 3. `pressure(t)` — not asked

Take the value declared in the flow map (`BUFFER occupancy ÷ capacity`, or the
`[DEV DEFAULT]` proxy). Write it through unchanged.

## 4. Starting weights and win-rate bands

- `w₁..w₅` → derived from section 1 (`Often` = high, `Sometimes` = medium, `Rarely` = low,
  `Never` = 0). Write `[DEV DEFAULT]`; the calibration round settles the real numbers.
  **Do not ask the designer for coefficients.**
- **Target win-rate bands** per tier — this one *is* a designer decision. Ask per tier:

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| `GreedyBot` win-rate band, Normal / Hard / SuperHard | "Out of 10 players trying a [tier] level for the first time, how many should win? A range is fine, e.g. 7–9." | "10 người chơi lần đầu vào một level [tier], bao nhiêu người nên thắng? Cho khoảng cũng được, ví dụ 7–9." |

If the designer has no view: spawn `systems-designer` (same prompt rules as section 2 —
include the frame excerpt, the genre and the session length) to propose candidate bands, then
`AskUserQuestion` for the designer to choose. Say clearly: this is a **starting point**, not
truth — the calibration round (frame section 9) settles the real numbers.

## 5. Write the file

Present the draft, then ask:
> "Write this to `design/pipeline/difficulty-model.md`?"

Structure: the 5-factor table (designer's answer | level | `w`) · **the visibility rule
(two-column table + the reversal warning)** · PerceptualProxy · pressure proxy · the three
tier win-rate bands · date + who was interviewed.

`UNDEFINED` for everything the designer has not decided. **Never fill it in.**

## Next

- `/gd-mechanic-difficulty` — repeat per mechanic *(also only the "where it gets hard" half;
  the mechanic bot branch waits for `/gd-bot-playstyle` in phase 2)*
- Then `/gd-mechanic-object-mix` → `/gd-mechanic-mix` → `/gd-level-definition`

Full pipeline: `/gd-workflow-help`
