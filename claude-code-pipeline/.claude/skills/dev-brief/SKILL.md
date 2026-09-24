---
name: dev-brief
description: "Switch Claude's replies to short, dense output for a developer — every point still there, fewer words, sharper word choice. Changes how answers are written, never what is checked or done. Use when the developer types /dev-brief on|off or says 'brief me', 'be brief', 'shorter answers', 'cut the fluff', 'tóm tắt thôi', 'nói ngắn gọn'."
argument-hint: "on | off | (empty = show status)"
user-invocable: true
---

This mode **cuts words, not content**. The reader is a developer who knows the codebase and
the domain: they need the verdict, the evidence, the risk, and what to decide — not the
journey that produced them.

## State

| Command | Effect |
|---|---|
| `/dev-brief on` | Every later reply in this session follows the rules below. Print `📋 Dev brief ON — trả lời tắt được: 1A 2B · ok · giữ/đổi · skip N` |
| `/dev-brief off` | Back to the normal style. Print `Dev brief OFF` |
| `/dev-brief` | Report the current state only; change nothing |

State lives in the conversation, never on disk (same reason as `use-mcp`: a flag file
would outlive the session). After a context compaction with no trace of `on` left, treat it
as **off**. A new session starts **off**.

## 1. What is never cut

Brevity loses to these every time. If a reply cannot fit them, it is not too long.

- **The answer to the question actually asked**, first.
- **Confidence labels** (`[VERIFIED]` / `[INFERRED]` / `[UNVERIFIED]`) and
  *"compiles, not tested"* — the project rules require them in every mode.
- **Risks and side effects** of what was done or proposed (a shared asset, a changed default,
  a file left unstaged).
- **What was not done, skipped, or failed**, with the reason.
- **Decisions the developer must make** — the question itself, with the options.
- **Numbers with their units**, and `file:line` references for any claim about code.
- **Approval requests** the project protocol demands (`CLAUDE.md` §4). Short, not skipped.

## 2. What is cut

- Restating the question, preambles ("Let me…", "Great question", "I'll now…").
- Narrating the steps taken — report the result, not the search that found it. Mention a step
  only if the developer needs it to trust the result (e.g. "grepped `X`, `Y`, `Z`: none").
- Explaining things the developer already knows: Unity basics, what a prefab is, how git works.
- Hedging stacked on hedging. One confidence label replaces "it seems / probably / likely".
- Closing summaries that repeat the body, and offers of further help.
- Adjectives and adverbs that carry no information ("really", "quite", "very important").

## 3. Word choice — the main job

- **Use the codebase's own names** — the class, the field, the asset, the event — exactly as
  written, in backticks. A precise identifier replaces a sentence of description.
- **Keep technical terms in English** even when replying in another language (`prefab`,
  `pool`, `streak`, `callback`); translating them makes the reply longer and vaguer.
- **One term per concept.** Once something is called "the Claim flight", do not rename it
  "the fly animation" two lines later.
- **Verbs over nouns**: "Joker stacks 5 sounds" beats "there is an issue of sound stacking
  occurring when the Joker is used".
- **Concrete over abstract**: "5 × `sfx_card_sort` in one frame" beats "multiple overlapping
  audio events".
- **Say the consequence, not the mechanism**, when the mechanism is not what the developer asked
  about: "the popup closes before the icon lands" rather than how the tween ordering causes it.

### Replying in Vietnamese

- Drop the subject on actions: "Đã sửa hook", "Chạy thử: 4 case pass" — never "Tôi đã…",
  "Tôi sẽ…". The developer knows who is talking.
- Imperative for anything the developer must do: "Mở lại session", "Gọi `/commit`".
- Names of rules, skills, hooks, files and fields stay exactly as written in the repo:
  `/use-mcp`, `coding_convention.md`, `streakEnabled` — never translated or paraphrased.
- Status words are fixed: **Đã làm · Lệch spec · Chưa làm · Cần chốt** (matching the four
  markers below), **Hiện tại** after 🧭.

## 4. Shape

- **Verdict line first**, then the evidence. A yes/no question starts with yes or no.
- **TL;DR line** on any reply longer than ~15 lines: the verdict in one sentence, then
  `→ cần bạn: …` when something is waiting on the developer, `→ không cần gì` otherwise. It is
  the first line of the reply, before any heading.
- **Tables** for comparisons, status across several items, or option trade-offs.
- **Bullets** of one line each. A bullet needing three lines is two bullets or a table row.
- **No headings** in a reply under ~10 lines. Headings only when there are 3+ distinct parts.
- **Code only when it is the point** — a diff the developer must review, a command to run.
  Never paste code the developer can open at the `file:line` given.
- **Anything waiting on the developer goes last**, in the closing block below — never in the
  body.

### Status tables and checklists — fixed row order

Any table or checklist of work items (done / not done, spec vs code, audit results) lists its
rows in this order, grouped, never interleaved. The status column uses exactly these markers:

| Order | Marker | Meaning |
|---|---|---|
| 1 | ✅ Done | Built and matching the spec |
| 2 | ⚠️ Partial / off-spec | Built but differs from the sheet/spec, or started and not finished |
| 3 | ❌ Not done | Nothing built yet |
| 4 | ❓ Needs decision | Cannot proceed until someone settles it — name **who** (designer, art, dev) |

A group with no rows is left out, not shown empty. Within a group, keep the order the work
would be done in.

### Decision questions — highlight what is asked and what is true today

Every question put to the developer (or passed on to the designer) is one numbered item of
three parts, each on its own line so the eye finds it without reading the whole sentence:

1. **Title line** — the 2–5 words in bold that say *what kind of decision* it is (timing,
   scope, which asset, keep/drop…), then the question. Bold only that phrase.
2. **One option per line**, lettered `A` / `B` / `C`, indented under the title. Never run
   options together on one line with `/`. Mark the option the sheet or spec asks for.
3. **Current state on the last line, after `🧭`, in italics** — what the project does
   *today*, so the answer can be "keep it" or "change it". Always a fact with its source,
   never a guess; unknown → say so.

> 1. **Streak sound timing** — which sound does the milestone play with?
>    - A · `SFX_Completed` (sheet)
>    - B · `SFX_Correct_X`
>    🧭 *Today: B, 0.15 s after `Card_Sort` (`BoardView.Audio.cs:52`).*
> 2. **Joker counts toward combo?**
>    - A · no
>    - B · yes
>    🧭 *Today: A — boosters never touch the streak (`cate-drop-boosters.md:173`).*

A yes/no question still gets its two lettered lines: the developer answers `2A`, not "yes".
Markdown in the terminal has no colour, so bold vs. 🧭-italics is the contrast; do not use
HTML tags or ANSI codes.

### "Bạn cần làm gì" — one block at the end, only when there is something

Everything waiting on the developer goes into **one** closing block, so nothing has to be
hunted for in the body. Three kinds, numbered together so a reply can name them:

| Kind | Marker | Shape |
|---|---|---|
| Decision | ❓ | the three-part layout above: title, one option per line, `🧭` line |
| Approval | 🔒 | one line: `2. 🔒 Ghi `docs/features/cate-drop-combo.md`?` |
| Action only the developer can take | 🛠 | one line: `3. 🛠 Mở lại session — hook chỉ nạp khi bắt đầu` |

> 1. ❓ **Thời điểm phát Streak sound** — phát cùng tiếng nào?
>    - A · `SFX_Completed` (sheet)
>    - B · `SFX_Correct_X`
>    🧭 *Hiện tại: B, sau `Card_Sort` 0.15 s (`BoardView.Audio.cs:52`).*
> 2. 🔒 Ghi `docs/features/cate-drop-combo.md`?
> 3. 🛠 Mở lại session — hook chỉ nạp khi bắt đầu.

Rules: the block exists only when it has at least one line — a reply with nothing pending
ends without it. A single pending item still uses the block, as one item.

### Short answers are enough

State this once when the mode is switched on, then honour it: the developer can answer the
closing block by number and letter, or with one word, and it is read as the full sentence.

| Developer types | Read as |
|---|---|
| `1A 2B` | question 1 → option A, question 2 → option B |
| `ok` / `làm đi` | approve every 🔒 in the last block |
| `giữ` / `đổi` | keep / change the 🧭 current state of the only open ❓ |
| `1 giữ, 2 đổi` | the same, per question |
| `skip 3` | leave item 3 undone; say so in the next reply |

An answer that fits none of these, or is ambiguous ("ok" when there are 3 decisions and no
approval) → ask which item, do not guess.

Length budget, as a guide not a quota: a status/answer ≈ 3–8 lines; a finding with evidence
≈ 10–15; a plan ≈ one table + the question. Exceeding it is fine when §1 requires it.

## 5. Example

> ❌ Normal: "I've gone ahead and investigated the Joker booster to see whether it might be
> playing multiple sounds at the same time. After reading through `BoardView.Audio.cs` and the
> card-correct configuration, it looks like the answer is yes — because the cards all land in
> the same frame, the sort sound and the correct sound are each triggered once per card, which
> means that for a 5-card category you'd hear quite a lot of overlapping audio…"
>
> ✅ Simplify: "Yes. Joker lands the whole Category in one frame (`CardCorrectConfig.flightDuration`
> is fixed at 0.12 s), and `PlaySortSfx` (`BoardView.Audio.cs:40`) fires per card: 5 cards =
> 5 × `sfx_card_sort` + 5 × `sfx_correct_X` at once. Fix: once per frame, as `sfx_card_fall`
> already does. Apply?"

## 6. Interaction with other modes

- **`gd-mode` on at the same time** → `gd-mode` decides the *vocabulary* (experience, not
  implementation); this mode only decides the *length and shape*. Never cut a felt consequence
  to save words — it is the content in that mode.
- This mode never relaxes a rule elsewhere: approvals, the Editor-bridge gate, verification,
  and "ask when ambiguous" all still apply, just phrased briefly.
- If the developer asks for detail ("explain", "tại sao", "chi tiết hơn") → give the full
  explanation for that reply, then return to brief replies.

## Exit

`/dev-brief off`, or the developer asking for the normal/detailed style again.
