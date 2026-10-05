---
name: gd-level-audit
model: claude-opus-5-5
effort: medium
description: Measure the difficulty and colour pairing of an entire level set with headless bots, never by eye. Produces win-rate tables, pressure curves, dangerous colour pairs, and a list of levels needing fixes with their root causes. Use when the developer types /gd-level-audit or says "measure level difficulty", "this level is too hard", "review the levels", "check the colour pairing".
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

# /gd-level-audit — reviewing levels with numbers

**First principle: measure, do not look.** Humans are extremely good at seeing what they expect to see.

> **Who runs it — the game designer, in `gd-mode`.** No Editor, no `/use-mcp`, no code. The
> measuring is done by one command, `bash .claude/tools/level-audit/run-audit.sh …`
> (developer-owned; contract and setup in the README in that folder). This skill asks the
> questions, runs **that command and nothing else**, **reads the JSON it writes** to
> `design/levels/audit-data/`, and writes the report. It never writes C#, never calls the
> Editor bridge, and never opens the code or the level `.json` files behind the runner. A
> developer may run it too, in a session where `gd-mode` is off.
>
> If `run-audit.sh` exits non-zero, say what its exit code means (README table) in the
> designer's words and **stop** — ask the developer. Do not look for the cause in code, do not
> retry with other flags to get around it, and do not use `--allow-stale` unless the designer
> asks for it after you explained that the numbers will describe an older game.

---

## SAFETY RULE — read first

**Never overwrite, edit, or delete an existing level.** Proposed levels are exported to a
**separate folder**. The developer/designer decides what replaces what. If the game is live on
a store, live levels are especially untouchable.

---

## The warning to state before starting

> **A clean validator does NOT mean the levels are playable.**
> On a previous project the validator reported **0 errors and 0 warnings across all 40 levels**,
> but bot measurement found **14/40 levels with a 0–3% win rate**, including one where only
> **2% of the content** could be cleared before hard-locking.
>
> The validator checks *whether the data contradicts itself*. The simulator checks *whether a
> winning path exists and how hard it is*. Entirely different questions.

If the developer says *"but the validator is clean"* → repeat exactly this passage.

---

## Phase 1 — Ask before measuring

Ask the designer in their own terms (`gd-mode` §1, §3.1, §5), one at a time, and turn the
answers into the command's flags. The **suggested** answer is what to use when the designer
has no view.

| Writes to the command | Ask (EN) | Hỏi (VI) | Suggested |
|---|---|---|---|
| `--bot` | "Which kind of player should the test use: a player who taps anything legal, a decent player who follows your playing rule, or a strong player who thinks several moves ahead?" | "Chạy thử với kiểu người chơi nào: bấm bừa mọi thứ hợp lệ, người chơi khá theo luật bạn đã viết, hay người chơi mạnh nghĩ trước nhiều nước?" | `greedy` |
| `--misclick` | "How often should that player tap the wrong thing — never, 1 in 4, 1 in 10?" | "Người chơi đó nên bấm nhầm bao nhiêu lần: không bao giờ, 1 trên 4, 1 trên 10?" | 0.25 (1 in 4) |
| *(not a flag)* boosters, revives | "Should the test player use boosters or revive? If yes the numbers describe the helpers as much as the level." | "Người chơi thử có dùng booster hay hồi sinh không? Nếu có thì số đo phản ánh cả các thứ hỗ trợ chứ không chỉ level." | no — and say so in the report |
| `--attempts` | "How many times should each level be played? More is steadier but slower." | "Mỗi level chơi thử bao nhiêu lần? Nhiều hơn thì ổn định hơn nhưng chậm hơn." | 100 |
| `--levels` | "Which levels: all, or a range like 21-30?" | "Đo level nào: tất cả, hay một khoảng như 21-30?" | the designer's block |
| *(not a flag)* band per label | "Out of 10 players on a Normal / Hard / SuperHard level, how many should win?" | "10 người chơi một level Normal / Hard / SuperHard thì mấy người nên thắng?" | from `gd-difficulty-model.md`; reference Normal 85–100% · Hard 35–55% · SuperHard 20–35% |
| *(not a flag)* touchable levels | "Which levels may we propose changes to, and which are already live and must not be touched?" | "Level nào được đề xuất sửa, level nào đã live và tuyệt đối không đụng?" | ask — no default |

Never ask for a seed: use `--seed 1` and, to see the noise, repeat with `--seed 2`.

---

## Phase 2 — Measure difficulty

**Run the measurement:**

```bash
bash .claude/tools/level-audit/run-audit.sh --levels <range|all> --bot <…> --attempts <N> --seed 1 [--misclick <p>]
```

Read the JSON path it prints. The file holds, **for each level**, the 4 metrics below plus the
colour data of Phase 3 and the run's matches per second, core count and model commit; quote
those next to the attempt counts in the report. Take the numbers **only from this file** —
never estimate one, never re-derive it from code. A level marked `"unscored": true` has no win
rate: list it as *unscored*, not as hard or easy.

Metrics, per level:

| Metric | Meaning |
|---|---|
| **Win rate** | **How** hard the level is |
| **Pressure curve** | **Where** it is hard — average free resource remaining at each 5% progress mark → 20 numbers |
| **% of content cleared on a loss** | A level that hard-locks early shows up here immediately |
| **Dominant loss type** | Points at which mechanic is killing the player |

**The pressure curve matters more than the win rate.** A win rate is one number; the pressure
curve shows the level's *shape*. Good levels usually have one clear bottleneck and then open
up, rather than being uniformly tight from start to finish.

Reading a pressure curve (average free slots, 20 marks):
```
L10  5 4 3 3 3 3 3 3 3 2 2 2 1 1 1 2 3 3 4 4   easy → tight at 50-70% → opens up   ✅
L29  3 2 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0   hard-locked from 15%                ❌
```

### Plot against the designed curve

If `design/levels/level-curves.md` — or the per-block shards `level-curves-L*.md` — exists
(each shard belongs to one owner; fill only the shard that contains the level; written by `/gd-level-intent`, format in
`.claude/docs/templates/level-difficulty-curve.md`), for every level that has a block there:

1. Convert the 20 free-slot averages to `pressure(t)` with the proxy in
   `design/gdd/gd-flow-map.md` (with a BUFFER: `pressure = 1 − free ÷ capacity`), one decimal.
2. Fill the **Measured** row — with the run, bot, attempt count and date — and plot it with ○
   (◉ where it lands on a ●).
3. Compare with the **Tolerances** table: peak position, peak height, peak count, Δ max. Any
   tolerance still `UNDEFINED` → report the raw gaps and say the verdict cannot be given until
   the designer sets it; never pick a tolerance yourself.
4. Write the verdict line (OK / Revise + which check failed + suspected root cause from Phase 4).

Update only Measured rows, ○ marks and verdict lines — never a Designed row, which is the
designer's. Ask before writing: *"Fill the measured curves into `level-curves.md`?"*
Levels with no block (no specific intent) are compared against their tier's reference shape
in the report only.

### Human check — only if `design/levels/play-records/` has records for the level

Designers play levels in the Level Player (`.claude/tools/level-player/README.md`) and each play is
saved as a record. For every audited level that has records, set the human beside the bot:

| Compare | Say |
|---|---|
| human result and moves vs the bot's win rate and typical moves | "You won this level on your 2nd try; the bot wins 14% of the time" |
| human pressure curve vs the bot's mean curve | where they diverge (the bot is squeezed at 40%, the human never was) |
| `invalidTaps`, `restarts`, `seconds` | signs of a level that is hard to *read*, which a bot cannot feel |

Caveats to state every time: **N is tiny** (a few plays, not a win rate); the designer is **the
best player of this game** (expert blindness); a **replay is not a first impression** (`attempt`
> 1 inflates wins); a record made on an older model commit than the audit's `modelCommit` is
stale — say so and do not compare. A large gap is a finding to investigate (a bot rule that does not
match how people play, or a level that plays differently by hand), never a verdict.

### Block boundaries — only if `design/gdd/gd-level-roadmap.md` exists

For each block, compare the measured win rate and pressure of its **first and last level**
with the block's boundary contract (entry/exit tier and win-rate band), and compare each
block's exit with the next block's entry. List a mismatch as a plain sentence for the lead —
*"level 20 ends at ~60% win but block 2's contract says ~50%"* — never edit the contract or
the roadmap. A block-to-block jump the contract did not plan is a finding for the lead, not
for either owner alone.

All bot matches go through the Sim tools' `BatchRunner` (parallel, seed-deterministic); report matches per second next to the attempt counts.

The report itself is written to `design/levels/level-audit-<date>.md` (ask before writing) —
one file per run, never overwriting a previous audit.

Do not write measuring code and do not use the Editor bridge for this: the runner behind
`run-audit.sh` is the only way numbers enter this skill.

---

## Phase 3 — Colour pairing across the play session

**Never judge the level's overall palette.** The player does not see the whole level at once.

The runner computes these from the swatches in the level data and reports, per level, the worst
pair and where in the level it occurs (`colour` in the JSON). Read and judge them against the
thresholds below; do not recompute them. What the runner does, per window position:

Slide a **window** along play progress (sized to the number of elements visible on screen at
once). At each window position, consider only the colours present **simultaneously**, and compute:

1. **The most similar pair** — CIEDE2000 distance
2. **Recompute after simulating red–green colour blindness** (protan + deutan)
3. Are two colours of the **same hue family** adjacent (check every neighbouring direction)?
4. Does it violate the designer's **forbidden colour cluster** rules?

**Thresholds:** ΔE2000 ≥ **18** for normal vision · ≥ **12** after colour-blindness simulation.

> **Why the colour-blindness simulation is mandatory:** a Blue + Cyan pair on a previous
> project measured ΔE **43.7** to normal vision — perfectly fine, and no manual review session
> ever caught it — but only **2.2** after simulation. For roughly 8% of male players that is
> effectively one colour. It appeared in **17/40** levels.
>
> Other pairs caught the same way: Mint+Pink colour-blind ΔE **0.4** · Blue+Purple **1.8**
> (10 levels) · Yellow+Green **2.3** (12 levels).

---

## Phase 4 — Find the root cause, not the symptom

A bad level is rarely bad because "there are too many mechanics". On a previous project, the
real cause of most dead levels was the **supply order**: resources needed early were placed
behind resources needed late. Judged by eye, the conclusion was "too cluttered", mechanics were
removed — and the levels stayed broken.

Four suspects, in order of frequency. Judge them from the JSON (pressure curve, loss type, share of the level cleared) and the designer's documents. A suspect that needs the level data itself to confirm is written as *"cannot be confirmed from the audit data — needs the developer"*, never guessed:
1. **Resource ordering** — what is needed first is buried behind what is needed later
2. **Resources only usable once exposed** — sitting dead and occupying space for the whole match
3. **A mechanic's simultaneity condition** — several things must be true at once; missing one deadlocks permanently
4. **Mechanic stuffing** (*fake difficulty*) — confusing rather than hard, and it breaks the teaching rhythm

---

## Phase 5 — Report

1. **An N-row table:** level · designer label · win rate · difficulty verdict · curve verdict
   (designed vs measured, or vs the tier's reference shape) · worst colour pair · rule violations
2. **The list of levels needing fixes + their ROOT CAUSE** — not "too hard" but
   *"missing the right resource at the 40–60% stretch"*
3. **Proposed changes, in words** (which level, which stretch, what to change). In designer mode the skill writes no level files: new candidate levels are produced by `/gd-level-gen`, run by the developer, into a separate folder with a read-me-first file
4. **Suggested validator additions** for the developer, so the same problem is caught earlier next time

---

## Honesty about error margins — MANDATORY in the report

- These numbers come from a **bot**, not real players. The difficulty ranking between levels
  is trustworthy; **the absolute values are not**.
- Two runs of 100 attempts on the same Hard level can differ by up to **±8 percentage points**.
- A metric correlating weakly with real outcomes → call it a **diagnostic** tool, not a
  **predictive** one. *(Real example: a supply-skew metric explained the badly broken levels
  well but correlated only r = −0.20 with win rate across the fixed level set — unusable for estimation.)*
- The tool's "solvable" column is usually a **conservative** check (running a single greedy
  path). *(One level was reported "unsolvable" while the bot won 100% of the time.)*
  Do not read it as "impossible".
- Colours are computed from the swatches in the data, which may differ from what actually
  renders on a device.

---

## What NOT to do

- Decide on the designer's behalf which levels get replaced. Measure and propose; **the designer decides**.
- Change a difficulty label in the data without asking.
- Present an estimate as if it were a measurement.

---

## Next

- `/gd-calibrate` — phase 4: turn these bot predictions into real difficulty, using designer
  scoring or live player data
- Levels needing fixes → `/gd-level-gen` again for those tiers

Full pipeline: `/gd-workflow-help`
