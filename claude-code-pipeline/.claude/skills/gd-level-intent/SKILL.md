---
name: gd-level-intent
description: "Read the designer's sheet/docx describing per-level intent — pacing, difficulty curve (easy start, hard middle, easing off, fast-paced…) — and translate it into a machine-usable profile for the generator. OPTIONAL before gd-level-gen: without it the generator uses the default tier profile. Use when typing /gd-level-intent or saying 'the designer has a level intent table', 'read the desired curve per level', 'level 30 should have a different rhythm'."
argument-hint: "<path to xlsx/docx/md> [level range, e.g. 1-40]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Write, Edit, Bash, AskUserQuestion
---

The translation bridge between **designer language** ("easy at the start, hard in the middle,
easing off toward the end") and **generator language** (pressure curve shape, per-segment DIG
profile).

**OPTIONAL**: skip this and `gd-level-gen` uses the default tier profile from
`gd-level-definition.md`. Run it and any level with its own intent **overrides** the tier default
— per-level intent beats the tier profile.

**In**: the designer's xlsx / docx / md file · **Out**: `design/gdd/gd-level-intent.md` +
the **designed** curve per level in `design/levels/level-curves.md`

> **Question style — `gd-mode` §1, §3.1, §3.2, §5, §7 apply, mode on or off.** This skill
> mostly reads; it asks only when a phrase is unclear or an intent is impossible. The
> designer's words are kept **verbatim, in the language they wrote them** — the translation
> column is for the file.

## 0. Load context

1. Read `.claude/reference/resource-flow-difficulty-framework.md` **section 8** (the three
   standard curve shapes) and `design/gdd/gd-level-definition.md` (the default tier
   profiles — if missing, warn that intent will have no baseline to override and suggest
   running `/gd-level-definition` first, but do not block).
2. Read the source file the designer supplied:
   - **xlsx**: use the parser `.claude/tools/gdd-sync/parse-sheet.sh` (procedure in
     `.claude/tools/gdd-sync/README.md`)
   - **docx**: unzip and extract `word/document.xml` (this machine may have no
     Python/pandoc — see the procedure used for the GDD)
   - **md/text**: read directly
3. Record the **md5 hash of the source file** — same sync rule as the GDD xlsx: when the
   source changes, the derived file must be re-synced.

## 1. Translate designer language → machine profile

Starting vocabulary table (extend it as the designer's phrasing reveals more; Vietnamese
phrases map the same way — add them as they appear):

| Designer writes | Translates to |
|---|---|
| "easy at the start, hard in the middle, easing off at the end" · "đầu dễ, giữa khó, cuối thả" | pressure: single peak at ~50% of the match, then decaying; expected failPoint in the middle |
| "fast-paced" · "dồn dập" | dense decision rhythm: Buffer Room tighter than the tier default, low DIG (no long thinking) |
| "tension building steadily to the end" · "căng dần đến cuối" | monotonically rising pressure, peak at 80–90% of the match (the standard SuperHard shape) |
| "sawtooth / rhythmic ups and downs" · "lên xuống nhịp nhàng" | 2–3 peaks alternating with release stretches (the standard Hard shape) |
| "a breather after a hard level" · "màn thở" | Normal curve, peak ≤ 0.5, no new mechanic |
| "teaches mechanic X" · "dạy mechanic X" | mechanic X appears in isolation, few simultaneous types, low DIG |
| "combined challenge / boss" · "tổng hợp / boss" | many mechanics (per the mix matrix), late peak, small margin |

Any phrase **not in the table and not inferable** → ask immediately, do not guess:

| Ask (EN) | Hỏi (VI) |
|---|---|
| "For level [N] you wrote '[phrase]'. When the player plays it, what should that feel like — where is the hardest moment, and how much room do they have?" | "Level [N] bạn ghi '[cụm từ]'. Người chơi vào sẽ thấy thế nào — chỗ khó nhất ở đâu, còn rộng hay hẹp?" |

Then record the new meaning in the vocabulary table of the output file so the machine
understands it next time.

## 2. Cross-check — is the intent feasible?

For each level with an intent, check three things before writing. Report each conflict in
plain words and let the designer choose — never silently pick one:

| Conflict | Say (EN) | Nói (VI) |
|---|---|---|
| tier vs intent | "Level [N] is tagged [SuperHard] but you wrote '[relaxed phrase]'. Change the tier, or change the intent?" | "Level [N] đang gắn [SuperHard] nhưng bạn ghi '[cụm thả lỏng]'. Đổi tier, hay đổi ý định?" |
| forbidden mechanic pair | "Level [N] wants [X] and [Y] together, but they were banned in the same level because [plain reason from the matrix]. Drop one, or lift the ban?" | "Level [N] muốn có cả [X] và [Y], nhưng hai cái này đã bị cấm chung level vì [lý do]. Bỏ một cái, hay bỏ lệnh cấm?" |
| `unscored` mechanic | "Level [N] uses [X], which the bot has not been taught yet. The level can be made, but it cannot be scored until then. OK?" | "Level [N] dùng [X], bot chưa được dạy. Level làm ra được nhưng chưa chấm điểm được. Chấp nhận không?" |

## 3. Write the file

Present the draft, then ask:
> "Write this to `design/gdd/gd-level-intent.md`?"

Structure:

```markdown
# Level Intent
> Source: <file> (md5 <hash>) · Date: <date> · Designer: <name>

## Vocabulary used
| Designer writes | Translates to | (new entries marked ★)

## Intent per level
| Level | Designer's exact words | Target curve | DIG | Mechanics | Conflict notes |
| 12 | "đầu dễ, giữa khó, cuối thả" | single peak ~50% | ... | ... | — |

## Levels with NO specific intent
(using the tier default — listed so nothing looks accidentally omitted)
```

Keep the designer's **exact words, in their language**, next to the translation — when a
generated result is not what they wanted, that is how you tell a translation error from a
generator error.

## 3b. Draw the designed curve

For every level with an intent, turn "Target curve" into a chart the designer can check by eye,
in `design/levels/level-curves.md`, following `.claude/docs/templates/level-difficulty-curve.md`:

1. File missing → create it from the template (header, How To Read, Tolerances with every value
   `UNDEFINED`, Reference Shapes). Never fill a tolerance yourself — ask the designer.
2. Start from the tier's reference shape (framework §8), then move the peaks, valleys and rest
   points to where the designer's words put them. Write **20 values** (5%…100%, 0.0–1.0, one
   decimal) — the only thing the chart is drawn from.
3. Plot them with ● on the 11-row grid, one column per 5% mark, and put the designer's rhythm
   description in their own words under the chart. Leave the **Measured** row empty —
   `/gd-level-audit` fills it.
4. A level whose block already exists: replace only the **Designed** row and the ● marks, never
   a Measured row.

Show the charts with the level-intent draft and ask for both in one approval:

| Ask (EN) | Hỏi (VI) |
|---|---|
| "Does each curve look like what you meant?" | "Mỗi đường cong có đúng như bạn hình dung không?" |

The chart is how the designer catches a translation error before a single level is generated.

## 4. Handoff to gd-level-gen

State clearly at the end:
- `gd-level-gen` reads this file **if it exists**; any level present here has its tier profile
  **overridden**; absent levels use the default.
- If the designer's source changes (different hash) → re-run this skill before the next
  generation batch.
- After generation, `/gd-level-audit` plots the measured curve next to each designed one in
  `level-curves.md`.

## Next

- Phase 2 if the simulation does not exist yet: `/gd-bot-playstyle` → `/gd-prototype-sim`
- `/gd-level-gen <tier|level>` — generate with the loaded intent, once the simulation runs

Full pipeline: `/gd-workflow-help`
