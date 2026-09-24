# Resource-Flow Puzzle — Difficulty & Level Generation Framework (game-agnostic)

> **Status**: Framework draft v1
> **Scope**: every mobile puzzle in the "constrained resource-flow" family —
> pixel sort, candy sort, cubeland, bus jam, water sort, marble, tray sort…
> **Document relationship**: this is the **shared frame**; a project's own
> `level-pipeline-standard.md` is one applied case. When the frame changes, the
> applied case must be reconciled with it — never the other way round.
> **The principle in one sentence**: **hard frame, soft weights** — structure,
> metrics and process are 100% shared; the coefficients are per-game.
>
> The three games named below (pixel sort, candy sort, cubeland) are **real
> reference cases from this studio**, kept because they carry real numbers.
> Read them as examples, not as the current project's data.

---

## 1. Entry conditions — the 5-rule contract

A game wanting to use this frame must sign all five. Rules 1–2 are architecture
(lose them at the start and they are lost forever — retrofitting is expensive);
rules 3–5 are shared tooling (written once).

| # | Rule | Type |
|---|---|---|
| 1 | **Sim purity** — all game rules in a pure C# assembly: no engine types, every operation instantaneous, deterministic by seed | Architecture |
| 2 | **Serializable LevelData** — a level is plain data, generatable and readable outside the Editor, with an invariant validator | Architecture |
| 3 | **Standard Solver API** (section 7) | Tooling |
| 4 | **Bot family** — Tier A archetypes (mandatory) + Tier B Player DNA (once telemetry exists) + a per-game perceptual proxy slot (section 6) | Tooling |
| 5 | **Calibration loop** — real analytics reconciled against bot predictions, with a **mandatory 20% holdout** (section 9) | Tooling |

Status of the three reference games: the pixel-sort project satisfies 1–2 (by
design, settled in an ADR); the candy-sort and cubeland projects have their
logic inside Assembly-CSharp mixed with MonoBehaviour — they **do not satisfy
1–2**, and retrofitting is expensive. Their value to the frame is as paper
validation cases (section 3), not as retrofit targets.

---

## 2. The abstract model: a 5-component structure

Every game in the family is an instance of:

```
INPUT   — the resource the player FREELY CHOOSES to solve the puzzle / reach the goal.
          Ordered and/or partially hidden.
MID     — an intermediate queue; capacity may be bounded or not.
          OPTIONAL — a game may have no MID.
OUTPUT  — what the player must consume / solve to reach the goal.
          PASSIVE: it reacts to the chosen INPUT; the player never picks it directly.
ACTION  — choosing which INPUT unit to release / route, under incomplete information
VISIBILITY — which part of INPUT / OUTPUT is visible in advance
```

**The agency factor is the distinguishing feature: the player holds the INPUT, not
the OUTPUT.** All difficulty arises from the gap between those two ends.

**Win** = every OUTPUT satisfied.
**Lose** = no legal move leads to a win. For games **with a MID**, the most
common form is *the MID is full ∧ no unit in it matches an open OUTPUT*.

**Conservation invariant** (game-agnostic): for every type c,
`Σ INPUT(c) ≥ Σ OUTPUT(c)` (equality if the game forbids surplus) — every
validator must guard it.

**Having a MID or not decides three things in the frame — declare them at map time:**

| | With MID | No MID |
|---|---|---|
| Lose condition | MID overflow (standard form) | out of moves / out of turns — **declare separately** |
| Factor 2 *MID slack* (section 4) | applies | **N/A** — drop it from the section 6 formula |
| `pressure(t)` (section 8) | MID occupancy | **pick another proxy** — declare at map time |

If it maps onto the model, the whole frame applies; only the simulation (the
specific rules) is written per game.

---

## 3. Mapping table for the three reference games

| Component | Pixel sort | Candy sort | Cubeland |
|---|---|---|---|
| INPUT | 3D model: coloured parts/layers, peeled outside→in | rows of candy / tubes in order | stacked voxel blocks |
| MID | funnel + conveyor (Y pixels) | waiting slots / holding trays | queue + shooter slots |
| OUTPUT | columns of coloured trays, front row Open | orders / destination trays | elevator / colour destinations |
| ACTION | tap an exposed piece (releases X units/cluster) | choose a tube to pour / candy to release | tap an exposed block |
| VISIBILITY | shell visible, interior hidden (X-ray peek) | tubes fully visible or partly hidden | outer faces visible, interior hidden |
| Mismatches | crowd physics (View-only, does not affect the model) | *(fill in at real mapping time)* | *(fill in at real mapping time)* |

> The "mismatches" column is mandatory when mapping a new game: wherever the
> model cannot describe something, write it down — that is where the frame needs
> extending, or the game needs its own proxy.

---

## 4. The five difficulty factors (hard frame)

Factors 1–4 are **properties of the level data** — statically computable, same
formula in every game. Factor 5 is a per-game slot.

| # | Factor | Measured definition | Example |
|---|---|---|---|
| 1 | **Demand–Supply Lag (DSL)** | the delay between an OUTPUT needing type c and c becoming selectable in the INPUT | the needed tray colour is buried deep / the right candy sits at the bottom of a tube |
| 2 | **MID slack** *(N/A without a MID)* | `MID capacity ÷ units per action` | Y/X = 4 taps / number of waiting slots |
| 3 | **Hiddenness** | the fraction of INPUT+OUTPUT not visible in advance; the depth of meaningful lookahead | uncoloured interior / face-down tubes |
| 4 | **Commitment** | how irreversible a single action is | releasing a whole X-unit cluster / pouring a whole tube — not retractable |
| 5 | **Perceptual cost** *(per-game slot)* | the human eye's search cost — a bot CANNOT measure it | 3D rotation + similar colours / ~0 on a flat board |

**DSL is the family's primary factor** — it connects straight to the lose
condition: a large lag forces the player to release "junk" to dig toward the
type they need → the MID swells → it overflows.
In a game without a MID, a large lag gradually drains the set of legal moves.

---

## 5. Mechanic → Play → Bot playstyle (do this BEFORE measuring)

> **Base rule: you may not call a level "easy" or "hard" until every mechanic
> cluster present in it is defined. And defining a mechanic means defining BOTH
> halves — the mechanism and the play.**

| Half | Content | Written by |
|---|---|---|
| **Mechanism** | how the mechanic changes the move set / demand structure | designer |
| **Play** | what "playing well with this mechanic" means | designer |

Missing half 2 → no bot can be written → nothing can be measured → "easy/hard"
is just an opinion.

### 5.1 A bot's playstyle is a design document, not a code detail

`GreedyBot` = the game's tactical statement, written as machine-executable rules.
Pixel-sort core: *"tap the colour the front-row tray needs, prioritising trays
close to full."* That sentence was written by the designer before anyone coded.
**Every new mechanic = one new rule branch, written at the same time as the
mechanic design.**

Consequence: the perceptual rule must be quantified FIRST too (pixel sort: what
percentage of screen coverage makes a piece tappable, reduced to how many
discrete directions) — without it the bot cannot even enumerate the legal moves.

### 5.2 Each mechanic loads one factor (section 4)

Pixel-sort example:

| Mechanic | Factor loaded | How play changes |
|---|---|---|
| Hidden tray (lv4) | Hiddenness | cannot plan ahead → keep spare capacity |
| Ice tray (lv11) | DSL (+Hiddenness) | demand deferred by N trays → feed other types while waiting |
| Connected trays (lv18) | Commitment (+merged demand) | two trays must finish together, no abandoning one |
| Pipe (lv25) | Hiddenness | the next tray is unknown → keep a wide safety margin |

> **Note**: a later GDD revision on that project **removed the Big Tray** — all
> trays now share one capacity X. See the consequence in 5.3b.

### 5.3 Mechanic mixing rules — TWO SCALES, two different rule sets

Mixing happens at two scales, and **neither scale's rules can be derived from
the other** — each must be judged separately:

| Scale | Question | Rule set |
|---|---|---|
| **Object** (5.3a) | which mechanics can one object carry at once? | deadlock / slot contention / overlapping state dimension |
| **Level** (5.3b) | which mechanics may appear together in one level? | by loading factor (section 4) |

A pair can be **forbidden at object scale yet allowed at level scale** — Key and
Lock are forbidden on the same object, but of course a level must contain both.

#### 5.3a Object scale — three checks

Applied when two mechanics attach to the **same object** (one tray, one shooter,
one pipe):

1. **Self-referential deadlock**: is mechanic A's unlock condition located on the
   very object B locks? → FORBIDDEN.
   *Example (candy sort): Key + Lock on the same shooter — the unlocking key sits
   on the very object that is locked, so it can never be freed.*
2. **Slot contention**: do both mechanics occupy the same physical/visual
   attachment point on the object (same marker position, same status display)?
   → FORBIDDEN, or the display must be redesigned.
   *Example (candy sort): Key/Lock "replaces the arrow" — it occupies the shooter's
   marker slot; two mechanics both needing that slot cannot coexist.*
3. **Overlapping state dimension**: do both mechanics hide/lock the same dimension
   of the object's information (two things hiding the colour, two things blocking
   intake)? → FORBIDDEN — stacking is redundant or confusing, and adds no difficulty.
   *Example (pixel sort): Hidden + Ice on the same tray — both conceal the tray's state.*

Everything else → ALLOWED: the two mechanics touch **orthogonal properties**.
*Example (candy sort): one shooter that is both a Super Shooter (raised capacity)
and carries a Key — capacity and carried-object are independent dimensions.*

#### 5.3b Level scale — by loading factor

- **Same primary factor → multiplicative, broken.** Hidden + Ice in one level =
  both invisible and unguessable → the player experiences luck, not difficulty.
- **Different factors → additive, sound.** Big + Hidden = "I do not know what is
  needed, but when I do I must commit hard" — still readable.

**Evidence the frame is right — plus an unexpected validation case:**

*The earlier GDD* had 5 mechanics. The designer banned combinations by intuition:
Ice not with Hidden/Connected/Pipe; Hidden not with Ice/Pipe; Connected not with
Ice/Pipe. Only **Big Tray combined with everything** — and Big Tray was the
**only** mechanic loading Commitment, while the other three all loaded
Hiddenness/DSL. The designer banned exactly the same-factor pairs with no theory.

*The revised GDD* **removed Big Tray**. The remaining four: Hidden (Hiddenness),
Ice (DSL+Hiddenness), Connected (Commitment+merged demand), Pipe (Hiddenness) —
and **three of the four ban each other almost completely**, exactly as the
same-factor rule predicts.

**This both confirms and warns:**
- ✅ The same-factor = forbidden rule still held after the mechanic set changed
- ⚠️ The current mechanic set is **factor-poor**: 3 of 4 load Hiddenness/DSL, and
  only Connected touches Commitment. The consequence is **very few combinable
  pairs** → the tool of creating difficulty through combination is lost, leaving
  only DSL (burying colours deeper).
- If more mechanics are needed later, **prioritise the empty factors** (Commitment,
  MID slack) rather than adding yet another Hiddenness variant — the frame
  predicts this in advance, without trial and error.

Note: the combination bans a designer writes in a GDD usually **mix both scales**
without distinguishing them — when reconciling (`gd-mechanic-mix` /
`gd-mechanic-object-mix`) they must be separated by scale before comparing.

### 5.4 The ignorant-bot trap (measurement validity)

Adding a mechanic without teaching the bot → the bot plays badly on levels
containing it → the level is scored "hard" when really **the bot is ignorant**.
The score is inflated for the wrong reason.

**Rule: a mechanic with no rule branch means its levels are not scored** —
mark them `unscored` and exclude them from the `w` calibration set.

---

## 6. Score formula & bot family (hard frame, soft coefficients)

```
DifficultyScore = w₁·DSL + w₂·(1/MidSlack) + w₃·Hiddenness
                + w₄·Commitment + w₅·PerceptualProxy
```

- The vector `w = (w₁..w₅)` is **per-game** — see section 9. Never compare
  absolute scores between two games (different scales); the shared comparison
  language is the pressure curve + bot win rate.
- `PerceptualProxy` is per-game, e.g. pixel sort:
  `a·(forced viewpoint changes) + b·(surface colour dispersion) + c·(simultaneous colours)`.

### 6.1 The bot family — two tiers

| Tier | Needs data? | Used when | Produces |
|---|---|---|---|
| **A — hand-written archetypes** | no | pre-release | **relative** ranking between levels |
| **B — Player DNA** | needs telemetry (section 9.1) | post-release | **absolute** prediction (real-player win rate) |

**Tier A** (playstyle rules written per game, shared shape):

| Bot | Simulates | Playstyle rule |
|---|---|---|
| `RandomBot` | the worst player | picks uniformly among legal moves |
| `GreedyBot` | a competent player | always serves an open OUTPUT |
| `LookaheadBot(k)` | a strong player | looks k moves ahead |

**Tier B** replaces three discrete points with a **continuous space**. Player DNA:

```
Skill        — ability to pick the right move
Planning     — lookahead depth
Mistake      — probability of choosing wrongly despite knowing better
Exploration  — willingness to try unfamiliar moves
Patience     — endurance before quitting
```

The three Tier A bots are **anchor points in that space**, not three separate
things: `RandomBot ≈ (Skill 0, Mistake 1.0)` · `GreedyBot ≈ (high Skill, low
Planning, Mistake ~0.1)` · `LookaheadBot ≈ (high Planning)`.

`Mistake` is the trait all three Tier A bots lack, and the most important one for
resembling a human: **good players occasionally err** — neither perfect nor
random. Tier A only covers the two extremes.

---

## 7. Standard Solver API

```csharp
PlayResult Play(LevelData level, IBotPolicy bot, int seed);

struct PlayResult {
    bool     win;
    int      moves;
    float[]  pressureCurve;   // pressure(t) per step — see section 8
    int      safetyMargin;    // how many wrong moves still allow a win
    float    failPoint;       // at what % of the match it was lost (NaN on a win)
    float?   quitProxy;       // quit estimate — Tier B ONLY, null at Tier A
    // + per-game metrics appended, without changing the standard part
}
```

This is the contract between the evaluator/generator and each game's simulation.
The measurement tool (`/gd-level-audit`) and the generator speak only through it.

`failPoint` matters as much as the win rate: two levels with the same loss rate,
one losing at 20% of the match and one at 90% — the first is broken design, the
second is tension in the right place.

`quitProxy` **exists only at Tier B**: a Tier A bot does not know how to quit.
Modelling quitting requires the `Patience` trait — the bot gives up when pressure
stays high for N consecutive moves. Never fabricate a quit rate from Tier A.

---

## 8. Pressure curve — the shared language of "difficulty"

`pressure(t)` = **proximity to the lose condition, normalised 0–1**, along match
progress. The definition is shared; the *implementation* is per-game:

| | `pressure(t)` implementation |
|---|---|
| With MID | `MID occupancy ÷ MID capacity` — measured on the conveyor in pixel sort, on waiting slots in candy sort |
| Without MID | a proxy declared at map time, e.g. `1 − (legal moves remaining ÷ at match start)` or `turns used ÷ turns allowed` |

Curve shapes remain comparable across games even when the proxies differ — which
is exactly why the frame uses the curve, not the score, as its shared language.

Three target shapes (shared shapes; the numeric thresholds are calibrated per game):

| Tier | Standard shape |
|---|---|
| Normal | peak ≤ ~0.6, at most 1 peak, relaxed ending |
| Hard | sawtooth of 2–3 peaks, highest ~0.7–0.85 at ~70% of the match |
| SuperHard | peak ~0.85–0.95 late (80–90% of the match), safetyMargin ≤ 2 |

A flat curve is boring; a vertical spike at the start is frustrating — both fail
review even when the win rate is on target.

---

## 9. Weights & the calibration loop (soft — per game)

Example starting point (designer estimates, uncalibrated):

| Factor | Pixel sort | Candy sort (estimated) |
|---|---|---|
| w₁ DSL | high | high |
| w₂ 1/slack | medium | high |
| w₃ hiddenness | medium | medium |
| w₄ commitment | medium | high |
| w₅ perceptual | **high** (3D + camera visibility) | **~0** (flat board) |

**Conceptual frame:** `DifficultyScore` is the **feature**; real-player win rate
is the **label**. The factors (section 4) are still needed even with a GA, because a
GA only says *that* it is hard; the factors say *why* — and the designer needs the
latter in order to tune anything.

### 9.1 Three calibration modes

| | Mode 3 — designer scoring | Mode 1 — regress `w` | Mode 2 — evolve DNA with a GA |
|---|---|---|---|
| Labels come from | **the designer replaying and judging easy/hard** | real-player win rate | real-player action patterns |
| Data needed | **none** | win rate per level | `action → state → result` per move |
| Runnable when | **as soon as the simulation exists** | after soft launch | after soft launch + fine telemetry |
| Cost | cheap, costs human time | cheap | expensive |
| Produces | starting labels for `w` | relative difficulty score | **absolute** win rate / moves / failPoint / quit |

The three modes **stack over the project timeline**; they do not replace each
other. Mode 3 fills the gap between having a simulation and soft launch — a
period in which the frame previously had **no calibration signal at all**, only
relative ranking.

> ⚠️ **Expert blindness — mode 3's limitation.** The designer is one person, and
> the person best at this game. Their sense of difficulty is **systematically**
> skewed relative to a newcomer. Mode 3 gives **starting labels** that must be
> overwritten by mode 1 once real data exists — never treated as truth, and never
> used to override real data.

The mode 3 procedure and its loop: section 13.7 (M3).

**Mode 1 — the procedure:**

1. The designer fills in a starting `w` (the table above is a format example)
2. Tier A bots run every level → predicted DifficultyScore
3. Soft launch → real analytics per level
4. Regress: adjust `w` so predictions match real win rates
5. Once `w` stabilises the generator uses it as its objective function; repeat when a mechanic is added

**Mode 2 — the GA loop:**

```
Generate a DNA population → play levels that ALREADY have telemetry
        ↓
Fitness = RESEMBLANCE TO REAL PLAYERS, not optimal play
          (win rate + moves + failPoint + action pattern + retry/quit)
        ↓
Select → crossover → mutate → next generation  ↺
        ↓
DNA converges → play UNRELEASED levels → predict behaviour
```

The crucial point: **fitness measures likeness, not skill.** The frame previously
assumed "GreedyBot = a competent player" — an unverified assumption; mode 2
replaces the assumption with data.

### 9.2 Holdout — mandatory for both modes

Hold **20% of levels** out of the calibration. Train on 80%, predict on the
unseen 20%, and **report the error on the holdout set**.

Without this step, every claim that "the frame predicts accurately" is unfounded
— the model may simply have memorised the training set.

### 9.3 Data requirements for Tier B

Mode 2 requires telemetry at the level of **`action → game state → result` per
move**, not just `level_start` / `level_end`.

⚠️ **This cannot be retrofitted.** Lacking that granularity in the first release
means waiting an entire further release cycle before there is data to evolve DNA
from. The decision to enable it must come **before** the first build is locked,
not after.

---

## 10. The level generation engine (hard frame)

The principle of **separating Content from Difficulty** (generalised from the
shell/interior insight of the pixel-sort project): every game in the family has
an *identity* part (model shape, candy types — human-owned, untouched by the
generator) and a *free* part (order/colour of the hidden portion + OUTPUT order —
machine-generated).

```
INPUT  : identity content (human) + the target difficulty tier
SEARCH : permute the INPUT (free part) × permute the OUTPUT (consumption order)
GUIDE  : hill-climb toward the tier's target DSL profile (directed search,
         not blind generate-and-test)
FILTER : pass 1 — static DSL (cheap, rejects obvious deadlocks)
         pass 2 — the bot family through the Solver API (expensive):
         GreedyBot win rate within the tier band, pressure curve of the right shape
OUTPUT : top-k candidates → A HUMAN APPROVES (the designer)
```

Easy = INPUT and OUTPUT nearly in phase + wide slack + little concealment.
Hard = deliberate phase offset + tightened slack + more concealment + a placed
commitment point.

---

## 11. Known limits — read before expecting anything

1. **No absolute comparison between games** — different `w` vectors make the two
   scales incompatible. Compare by curve + bot win rate.
2. **Factor 5 cannot be standardised in content** — only its *socket* and its
   *calibration procedure* can be.
3. **Model-breaking mechanics**: real-time elements (timers, turn-counting bombs)
   and adversarial elements add a dimension outside the model; they can be
   accommodated but must be declared in the "mismatches" column at map time.
4. **If the simulation diverges from the real game, every number is fiction** —
   validate the simulation with a prototype before trusting the evaluator. No exceptions.
5. **The frame is only cheap for new games** — an existing game without a
   Model/View split must first pay the architectural debt (rules 1–2), which is
   usually not worth it unless the game has a long remaining life.
6. **Tier A must stand on its own** — a GA (Tier B) needs telemetry at scale,
   which does not exist pre-release. Never design the frame to depend on the GA;
   the GA is a reward for having data, not a precondition for the frame to work.
7. **Overfitting** — 5 DNA parameters over 20–50 levels memorise very easily.
   The holdout (section 9.2) is mandatory, not optional.
8. **The ignorant-bot trap is worse under a GA** — a mechanic the bot does not
   understand makes the GA evolve distorted DNA to absorb the error, **hiding**
   the problem instead of exposing it. The `unscored` rule (section 5.4) must be
   tightened when a GA is enabled, never relaxed.

---

## 12. Checklist for applying this to a new game

- [ ] Sign the 5-rule contract (rules 1–2 from the architecture design onward)
- [ ] Fill in the model mapping table (section 3) + the "mismatches" column
- [ ] Define the per-game PerceptualProxy (factor 5) + quantify the perceptual rule
- [ ] Write MECHANISM + PLAY for the core and for each mechanic; each mechanic declares its loading factor + bot rule branch (section 5)
- [ ] Write the simulation + a three-level `IBotPolicy`, wired to the Solver API
- [ ] The designer fills in a starting `w` + target win-rate bands for three tiers
- [ ] Run the evaluator on the first hand-built level — sanity check the curve
- [ ] Decide telemetry granularity BEFORE the first build if Tier B is wanted (section 9.3)
- [ ] Soft launch → the `w` calibration loop, reporting error on the 20% holdout (section 9)
- [ ] Enable the generator (section 10) once `w` is stable

**Once this checklist runs end to end for the first time on a real project** →
package it as a skill next to `gd-level-audit`, so the next title starts from the
frame rather than from a blank page.

---

## 13. Extension — an engine-based validation loop (Unity MCP)

> **Status: roadmap, not implemented.** Requires a working simulation + MCP
> attached to the right project. Recorded here so earlier design steps do not
> block it.

### 13.1 It solves two places where the frame is currently stuck

| Current limit | How the MCP loop solves it |
|---|---|
| **Limit #4** — *"if the simulation diverges from the real game every number is fiction"*, but **there is no automated check** | replay the bot's move sequence through a real build and compare the outcome with the simulation. A divergence means the simulation is wrong, and it is machine-detectable |
| **Factor 5, Perceptual** — *"a bot CANNOT measure it"* | screenshot at each decision point and analyse the image: is the needed thing **actually visible**, how occluded is it, how many rotations were required |

Factor 5 moves from being calibrated indirectly through win rate to being
**measured directly**.

### 13.2 Architecture: a sampling layer, NOT a replacement for the simulation ⚠️

This is the most important constraint in this section.

| | Speed |
|---|---|
| Pure C# simulation, headless | thousands of matches / minute |
| Play mode + screenshots through MCP | ~1 match / 10–60 seconds |

A gap of **100–1000x**. Letting the MCP loop replace the simulation destroys the
entire reason the simulation exists (contract rule 1), and automated generation
dies with it.

**The right way**: the simulation measures in bulk → pick a **small sample**
(e.g. top-5 candidates × 3 seeds = 15 runs ≈ 5–15 minutes) → only that sample
goes through the engine to **verify and capture**.

### 13.3 The loop

```
/gd-level-gen generates candidates
      ↓  the simulation measures in bulk (fast, headless)
   pick the top-k sample
      ↓  MCP: enter Play mode, load the level, the bot drives
   play + capture/record at DECISION POINTS (not every frame)
      ↓
   Win / Loss  +  a set of images
      ↓  image analysis → 3 signals (13.4)
   tune  →  repeat until the target band is reached
```

### 13.4 Three signals readable from images — and one that is NOT

| Signal | How it is measured | Used for |
|---|---|---|
| **Simulation agreement** | outcome + board state in the real build vs the simulation's prediction | checks limit #4. A divergence means fix the simulation, **not** the level |
| **Perceptual cost** | at a decision point: what % of screen the needed type occupies, how occluded, how many rotations needed | real data for `PerceptualProxy` (factor 5) |
| **Legibility** | is the board readable at all: colour contrast, overlap, elements too small | **a UX bug, not difficulty** — fix it in the View, do not adjust `w` |
| ~~Human-likeness~~ | **NOT measurable from images** | needs action-sequence telemetry + a GA (section 9.1 mode 2). Images say *what is visible*, not *what a real player chooses* |

Separating **difficulty** from **legibility** is the easiest confusion here: a
board too cluttered to read is not a hard level — it is a display bug, and
adjusting `w` to compensate treats the symptom.

### 13.5 An unexpected strength: validating the visibility model itself

The simulation **already knows** which piece should be visible (directional
exposure data produced by the tool). The screenshot shows which piece is
**actually** on screen.

Comparing the two **validates the visibility model itself**, and is the only
experimental way currently available to answer the open question *"what
percentage of exposure counts as tappable, reduced to how many discrete
directions"* — instead of the designer guessing a threshold.

### 13.6 Technical prerequisites

- A working simulation + Solver API (contract rules 1, 3)
- A build with a **load-arbitrary-level entry point** and a **bot control entry
  point** in dev builds
- MCP attached to the **right project** — check `Application.dataPath` every session
- Screenshots carry a **timestamp** matching the move index in `PlayResult`;
  without it images cannot be paired with decisions

### 13.7 Decisions

#### M1 — When to capture: at most **5 images per loop run**

- **1 opening image**, captured on entering the level. **Cached by
  `hash(levelData)`** — if the level data has not changed, reuse it across every
  iteration rather than recapturing. This is the largest saving, because a tuning
  loop replays the same level many times.
- **At most 4 event images**, only at a **big move** or a **choke point**.

**The trigger condition must be a metric the simulation can compute BEFORE
capturing** — the model must never look at an image to decide whether the image
was worth taking (a circular loop):

| Event | Measured definition | Source |
|---|---|---|
| Big move | `Δ(legal move count)` strongly positive, or `Δ(reachable type count)` rising | `SimSnapshot` |
| Choke point | `Δ(legal move count)` suddenly negative, or `pressure` stepping up | `SimSnapshot` |

Thresholds for "strongly" / "suddenly": `UNDEFINED` — tune when real data exists.

> **Keep them separate**: *measuring* at many points (cheap, in code) ≠
> *capturing images* at few points (expensive). The cap of 5 applies only to
> capture; perceptual metrics are still taken at every decision point.

#### M2 — Sampling: **3 runs × 3 playstyles**

The three runs correspond to the three Tier A bots (Random / Greedy / Lookahead).
**The interface must clearly show which playstyle the current run uses** —
otherwise whoever reviews the images cannot tell whose behaviour they are seeing.

Run it in a **simulated playtest mode with accelerated time**.

> ⚠️ **An unresolved constraint — depends on the physics architecture decision.**
> If the View still uses **real physics**, raising `Time.timeScale` makes the
> physics diverge from what a player sees at 1×, yet comparing the simulation to
> the real build is the entire reason this loop exists. Accelerating wrongly
> means measuring a game nobody plays.
>
> - A **kinematic** View → accelerate freely
> - Keeping real physics → **limit to 4–8×**; beyond that `FixedUpdate` backs up,
>   the CPU becomes the bottleneck and the wall-clock benefit disappears anyway
>
> **The acceleration factor is settled after that architecture decision, not before.**

#### M3 — Designer hand-scoring *(→ this produced mode 3, see section 9.1)*

The original question was "how large a simulation divergence counts as a bug".
The designer answered in a different direction, and **that direction is more
valuable**: instead of hunting for a statistical threshold, use **a human as the
arbiter**.

1. The simulation has **some variance** when self-playing (this is the `Mistake`
   trait — a perfectly optimal bot resembles nobody). **The variance must come
   from a seed**, otherwise reproducibility under contract rule 1 is lost and
   nobody can debug a losing match.
2. Generate a sample level set and collect the bot numbers.
3. **The designer replays the generated levels** and scores them easy/hard.
4. The designer gives a direction: this level needs to be **easier** or **harder**
   → tune → repeat.

The automated divergence threshold remains `UNDEFINED` — left until real
comparison data exists, and not blocking the loop.

#### M4 — Image analysis by **hard rules**

Pixel counting / occlusion queries **inside the engine**, producing numbers directly.

**The good consequence**: images no longer enter a vision model — they are just
artefacts for a human to review. The loop's token cost drops to nearly zero, and
the perceptual metrics become **deterministic**, so they are repeatable across
tuning rounds.

**The cost**: perceptual measurement becomes per-game code, not reusable between
titles. That is consistent with the frame — factor 5 was always a "per-game slot"
(section 4).

A vision model is used only for **legibility** (is the board readable at all) —
something hard rules judge poorly, and which does not need running every round.
