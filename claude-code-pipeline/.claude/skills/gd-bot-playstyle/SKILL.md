---
name: gd-bot-playstyle
description: "Interview the designer to turn 'what playing this game well means' into three machine-executable playstyle rules, with an observation budget for each. Designer-only: needs no simulation, no code and no Editor bridge — the designer can finish it alone, before /gd-prototype-sim exists. Use when typing /gd-bot-playstyle or saying 'define how the bot plays', 'what does playing well mean', 'we are about to build the sim'."
argument-hint: "[empty]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

Turn "what playing this game well means" into **rules a machine can run**. Run **once per
game**, then extend one row per mechanic as mechanics are defined.

**In**: `gd-flow-map.md` + `gd-difficulty-model.md` · **Out**: `design/bot/bot-playstyle.md`

> **Designer-only — finishable without the simulation.** This skill never reads code, never
> needs `/gd-prototype-sim`, Phase 1A, a running game or the Editor bridge. The designer can
> complete it **alone, in any order after `/gd-core-difficulty`**. The file is the *contract*
> `/gd-prototype-sim` is built from, not the other way round — the developer does not get to
> change a rule; a disagreement goes back to the designer.
>
> **Question style — `gd-mode` §1, §3.1, §3.2, §5, §7 apply, mode on or off.** Bot names,
> `k`, "observation budget" and "perceptual premise" are words for the file. The designer
> hears moments of play, in the object names from `gd-flow-map.md`.

> **Why this skill is separate.** Generating levels needs no bot — the layout, the colours, the
> order pieces arrive in and the difficulty points never ask what a bot thinks. A bot is only
> needed once **measurement** starts. This is frame section 5.1: **a bot's playstyle is a design
> document, not a code detail.** Writing it down makes the argument happen once, with the
> designer, instead of silently in code, decided by whoever types fastest.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 5.1 and 6.1**.
2. Read `design/gdd/gd-flow-map.md`. **Missing → stop**:
   > "There is no flow map. Run `/gd-map-flow` first."
3. Read `design/gdd/gd-difficulty-model.md`. **Missing → stop**:
   > "There is no difficulty model. Run `/gd-core-difficulty` first — without knowing which
   > factor is High, there is no way to know where the bot must be careful."
4. Read `design/gdd/gd-mechanics/*.md` if present — every mechanic needs a **branch** in these
   rules. A mechanic that has no file yet gets `UNDEFINED` and is listed (section 6); this is
   not a reason to wait.
5. Read the gameplay GDD for concrete situations to use in the questions.
6. If `design/bot/bot-playstyle.md` exists → read it and ask "extend with new mechanics, or
   rewrite?". Adding a mechanic later means adding **one row**, never redoing the file.

## 1. The root question

Ask in prose, **not** with `AskUserQuestion`:

| Ask (EN) | Hỏi (VI) |
|---|---|
| "What does playing this game well mean? Explain it as if teaching a beginner, in 1–3 sentences." | "Chơi game này giỏi nghĩa là gì? Giải thích như dạy người mới, trong 1–3 câu." |

If the GDD already contains a sentence describing the AI/bot (many do, in the level-authoring
guidance), **show that sentence first** and ask the designer to confirm or amend it — faster
than asking from scratch, and it avoids creating a second version that contradicts the GDD.

## 2. Dig until a machine could execute it ⭐

The first answer is almost always incomplete. Dig with **concrete situations**, one at a
time, using `AskUserQuestion` where the options are finite. Use situations from the
designer's own GDD or hand-built levels:

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| tie-break rule | "Several moves are possible. Which one does a good player pick first, and why?" | "Có nhiều nước đi được. Người chơi giỏi chọn nước nào trước, vì sao?" |
| stuck rule *(where DIG bites — drives win rate more than anything)* | "Nothing the player can do helps any open [goal object]. What does a good player do now?" | "Không nước nào giúp được [chỗ nhận] đang mở. Người chơi giỏi làm gì lúc này?" |
| side-information rule | "Can the player peek at something for free: rotate the view, X-ray, preview? Does a good player use it to choose a move?" | "Người chơi có được xem trước thứ gì miễn phí không: xoay góc nhìn, X-ray, xem trước? Người giỏi có dùng để chọn nước đi không?" |

(Designers routinely forget to mention a reveal mechanic; yet it is often exactly what
separates a good player from a beginner.)

### How to know it is enough — the paper test

No simulation is needed to check a rule. Pick **three situations** from the designer's own
levels or GDD, show each, and ask:

| Ask (EN) | Hỏi (VI) |
|---|---|
| "In this situation [describe it], which move does your rule pick?" | "Tình huống này [mô tả]. Theo luật của bạn thì đi nước nào?" |

If the designer has to think, the rule is missing a branch — add it and ask again. Done when
all three are answered instantly **and** the rule written down gives the same answer as the
designer. Record the three situations and answers in the file; the developer later uses them
as the first tests of `GreedyBot`.

## 3. Observation budget ⭐

Only if the flow map's *"mismatches"* section, or the answer to section 2's side-information
row, records a **free observation action**. Otherwise write "none — everyone sees the same"
and skip. If it applies and is left out, the bot sees the whole board for free and **always
finds levels easier than a human does**.

`AskUserQuestion`, each option in the designer's terms:

| Writes to file | Option (EN) | Lựa chọn (VI) |
|---|---|---|
| weak bots limited, strong bots see everything *(usually best)* | "A beginner does not peek much; a skilled player peeks a lot" | "Người mới ít xem trước; người giỏi xem trước nhiều" |
| all bots see everything | "Everyone sees everything — simplest, levels will look a bit easier than for real players" | "Ai cũng thấy hết — đơn giản nhất, level sẽ dễ hơn người chơi thật một chút" |
| all bots pay to observe | "Every peek has a cost, like for a real player — most realistic, one more number to tune" | "Mỗi lần xem phải trả giá như người thật — sát thực tế nhất, thêm một con số phải chỉnh" |

The first option turns the win-rate gap between the weak and the strong bot into a
**measurement of how much peeking helps**, i.e. a free Hiddenness metric.

## 4. Restate as three playstyle rules

Read the three back to the designer in plain words, then write them with their file names:

| Bot (file) | In plain words | Where it comes from |
|---|---|---|
| `GreedyBot` | "a decent player who follows the rule you just gave" | the designer's rule, every branch from section 2 |
| `RandomBot` | "a player who taps anything legal" — the floor | nothing to ask |
| `LookaheadBot(k)` | "a strong player who thinks several moves ahead" | `k` — see below |

`k` is a **felt** question, not a number the designer must know:

| Ask (EN) | Hỏi (VI) |
|---|---|
| "How far ahead does a strong player think: just the next move, two or three moves, or the whole level?" | "Người chơi mạnh nghĩ trước bao xa: chỉ nước kế tiếp, hai ba nước, hay cả level?" |

The developer turns the answer into `k` and may suggest a value from the game's structure
(e.g. moves to the lose threshold); write it `[DEV DEFAULT]` if the designer has no view.

**Perceptual premise** — what the bot is allowed to know when it picks a move. Read it back
as one sentence and ask the designer to confirm:

| Ask (EN) | Hỏi (VI) |
|---|---|
| "When choosing a move the player can see [the exposed objects, per your visibility rule], knows [what any peek reveals], and sees the next [N] [goal objects]. Anything else they know, or anything on this list they can't?" | "Khi chọn nước đi, người chơi thấy [các object lộ ra, theo luật nhìn thấy của bạn], biết [thứ mà việc xem trước cho thấy], và thấy [N] [chỗ nhận] kế tiếp. Họ còn biết gì nữa, hoặc có điều nào ở đây họ không biết?" |

This sentence is the contract: the simulation grants exactly this knowledge, no more.

## 5. `pressure(t)` — not asked

Copy the proxy declared in the flow map and record **how to read it**: with a coarse quantum
(one action pushing the BUFFER up several steps) the curve is a **sawtooth** — read it by
step, not by slope. Nothing for the designer to decide.

## 6. Write the file

Present the draft, then ask:
> "Write this to `design/bot/bot-playstyle.md`?"

Structure: the designer's original sentence · the three playstyle rules in words, designer
column on the left, bot name / `k` on the right · the **three paper-test situations with their
answers** · the perceptual premise · the observation budget table · `pressure(t)` · **a branch
per mechanic** · date + who was interviewed.

The per-mechanic table has one row per mechanic file found, `UNDEFINED` for any not yet
decided. Adding a mechanic later = adding one row.

`UNDEFINED` for everything the designer has not decided. **Never fill it in.**

⚠️ **A mechanic with no rule branch → levels containing it are `unscored`** and must not be
scored. List the mechanics still missing a branch inside the file.

### Done when (all designer-checkable, none needs the simulation)

- [ ] the designer's sentence, the three rules and `k` (or `[DEV DEFAULT]`) are written
- [ ] the three paper-test situations were answered instantly and match the written rule
- [ ] the perceptual premise was read back and confirmed
- [ ] the observation budget is chosen, or "none" is recorded with the reason
- [ ] every mechanic file found has a branch, or is listed as `UNDEFINED`

## Next

`/gd-prototype-sim` — build the simulation + 3 bots from this document (needs a developer and
Phase 1A). This file is the **contract**: the simulation must grant exactly the knowledge
declared in section 4, and no more; if the developer finds a rule that cannot be implemented,
it comes back to the designer as a question, not a silent change.

Full pipeline: `/gd-workflow-help`
