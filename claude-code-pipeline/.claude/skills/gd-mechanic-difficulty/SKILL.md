---
name: gd-mechanic-difficulty
description: "Interview the designer to define where one mechanic creates difficulty — how it changes the legal move set, how play changes, which factor it loads, and the bot rule branch that goes with it. Run once per mechanic. Use when typing /gd-mechanic-difficulty or saying 'where does this mechanic get hard', 'add a new mechanic to the pipeline'."
argument-hint: "<mechanic-name> | all"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion
---

Every mechanic creates difficulty in **its own way**. This skill names that way, and produces
the bot rule branch that keeps the bot from playing ignorantly on levels containing it.

**In**: the mechanic GDD + `gd-difficulty-model.md` · **Out**: `design/gdd/gd-mechanics/<name>.md`

> **Question style — `gd-mode` §1, §3.1, §3.2, §5, §7 apply, mode on or off.** "Legal move
> set", "GOAL structure", "attachment slot", "state dimension", "loading factor" are words
> for the file. Use the object names from `gd-flow-map.md` and the mechanic's own name.

> **Base rule (frame section 5)**: you may not call a level "easy" or "hard" until every
> mechanic cluster in it is defined. And a mechanic must be defined on **both halves —
> the mechanism and the play**. Missing the second half → no bot → no measurement →
> "easy/hard" is just an opinion.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **sections 4, 5**.
2. Read `design/gdd/gd-flow-map.md`, `gd-difficulty-model.md`. Missing → stop, point at the
   earlier skill.
3. Find the mechanic's GDD: `design/dev-system/dev-*<name>*.md`. If present, read it — **take the
   mechanism from there, do not re-ask the designer**. If absent, interview both halves.
4. `all` → list every mechanic with no file in `design/gdd/gd-mechanics/`, ask which to do
   first, then loop through them.

## 1. Half one — the mechanism

If a GDD exists: **restate** what it says in plain words and ask the designer to confirm,
rather than asking from scratch. Otherwise ask, one row at a time:

| Writes to file | Ask (EN) | Hỏi (VI) |
|---|---|---|
| legal move set: adds / removes / locks | "With this mechanic, what can the player do that they could not before? What can they no longer do, or not yet?" | "Có mechanic này thì người chơi làm được thêm gì? Không làm được gì nữa, hoặc chưa làm được ngay?" |
| GOAL structure: defers / merges / hides / capacity | "Does it change what has to be filled? For example: a [goal object] that opens later, two that count as one, one whose colour is hidden, one that holds more." | "Nó có đổi cái phải fill không? Ví dụ: [chỗ nhận] mở muộn hơn, hai cái tính là một, một cái bị giấu màu, một cái chứa nhiều hơn." |
| GIVEN / BUFFER change | "Does it change the objects the player taps, or the [waiting spot]? How?" | "Nó có đổi gì ở object người chơi bấm, hay ở [chỗ chờ] không? Đổi thế nào?" |
| **host** | "Which object does this mechanic sit on? What is that object called?" | "Mechanic này gắn lên object nào? Tên gọi của object đó là gì?" |
| **slot occupied** | "Where on that object does it show? Does it replace something already shown there, like an arrow or a number?" | "Nó hiện ở chỗ nào trên object đó? Có thay thế thứ gì đang hiện sẵn không, ví dụ mũi tên hay con số?" |
| **state dimension** | "What does it hide, lock, or change on that object?" | "Nó giấu, khóa, hay đổi cái gì của object đó?" |

The last three rows are the **attachment profile** — the input to `/gd-mechanic-object-mix`.
Anything the GDD leaves unclear → ask, do not infer.

## 2. Half two — the play ⭐

| Ask (EN) | Hỏi (VI) |
|---|---|
| "In a level with this mechanic, what does a good player do differently?" | "Trong level có mechanic này, người chơi giỏi sẽ chơi khác đi thế nào?" |

Dig until a machine could execute it. Two useful probes:

| Ask (EN) | Hỏi (VI) | Why |
|---|---|---|
| "What mistake do new players make with it?" | "Người mới hay mắc lỗi gì với mechanic này?" | that mistake is what `Mistake` needs to simulate |
| "In this situation: [describe one]. Which move does your rule pick?" | "Tình huống này: [mô tả]. Theo luật đó thì đi nước nào?" | if the designer has to think, the rule is still incomplete |

## 3. Loading factor

`AskUserQuestion`, **multiSelect**, each option a kind of hard **in this mechanic's terms**.
Then ask which one is the main one.

| Writes to file | Option (EN) | Lựa chọn (VI) |
|---|---|---|
| DIG | "The player knows what they need but this mechanic buries it deeper" | "Biết cần gì nhưng mechanic này vùi nó sâu hơn" |
| Buffer Room | "It makes the [waiting spot] fill up faster" | "Làm [chỗ chờ] đầy nhanh hơn" |
| Hiddenness | "It makes the player guess more" | "Bắt người chơi đoán nhiều hơn" |
| Commitment | "It creates moves that cannot be undone" | "Tạo ra nước đi không rút lại được" |
| Perceptual | "It makes things harder to see or find" | "Làm khó nhìn, khó tìm hơn" |

Follow-up: "Which of those is the main one?" / "Cái nào là chính?" — `/gd-mechanic-mix` uses
the **primary factor** to derive the ban rules.

Reference example (from a tray-sorting game) to help the designer picture it:

| Mechanic | Primary factor | Secondary |
|---|---|---|
| Big tray (2X) | Commitment | — |
| Hidden tray | Hiddenness | — |
| Ice tray | DIG | Hiddenness |
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

Include the **ignorant-bot trap warning** (frame section 5.4), in plain words:
- EN: *"Until the bot is taught this mechanic, levels that use it can be generated but not
  scored — a bot that does not know the rule loses because it is ignorant, not because the
  level is hard."*
- VI: *"Chừng nào bot chưa được dạy mechanic này thì level dùng nó sinh ra được nhưng chưa chấm
  điểm được — bot thua vì không biết luật, không phải vì level khó."*

## 5. Write the file

Present the draft, then ask:
> "Write this to `design/gdd/gd-mechanics/<name>.md`?"

Structure: mechanism (and which GDD it came from) · attachment profile · play · primary +
secondary factors · bot rule branch · `scored` / `unscored` status · date + who was interviewed.
Designer's words on the left, framework value on the right (`gd-mode` §8).

`UNDEFINED` for anything the designer has not decided. **Never fill it in.**

## Next

- More mechanics to do → run this skill again
- All mechanics done → `/gd-mechanic-object-mix` (object scale) **then**
  `/gd-mechanic-mix` (level scale). Both are needed before `/gd-level-definition`

Full pipeline: `/gd-workflow-help`
