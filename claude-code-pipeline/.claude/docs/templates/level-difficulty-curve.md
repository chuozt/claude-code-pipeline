# Level Difficulty Curves: [Game Title]

> **Status**: Draft | In Review | Approved
> **Owner**: [game designer]
> **Last Updated**: [Date]
> **Written by**: `/gd-level-intent` (designed curve) · `/gd-level-audit` (measured curve)
> **Project file**: `design/pipeline/level-curves.md`
> **Links To**: `design/pipeline/flow-map.md` (how `pressure(t)` is computed in this game),
> `design/pipeline/level-intent.md` (the designer's words per level)

One block per level: the curve the designer **wants** next to the curve the bots **measured**,
on the same scale, so "is this level shaped the way we meant?" is answered by looking.

---

## How To Read A Chart

- **Y axis — `pressure(t)`, 0.0 to 1.0**: how close the player is to losing
  (`resource-flow-difficulty-framework.md` §8). 0.0 = nothing threatens the player, 1.0 = the
  next wrong move loses. How it is computed in this game is in `flow-map.md`
  (e.g. `MID occupancy ÷ MID capacity`).
- **X axis — progress through the level**, 20 marks of 5% each: Start → Mid → Climax → Exit.
- **●** designed · **○** measured · **◉** both on the same cell. Values are rounded to 0.1 to
  plot; the numbers under the chart are the exact ones.
- A chart shows the **shape**; the tier's win-rate band says **how hard**. A level can hit
  its win rate with the wrong shape — that still fails review.

> `/gd-level-audit` reports "average free slots"; that is the opposite direction. Convert with
> the proxy from `flow-map.md` before plotting (with a MID: `pressure = 1 − free ÷ capacity`).

---

## Tolerances (set by the designer — `UNDEFINED` until they do)

| Check | Tolerance | Why it matters |
|---|---|---|
| Peak position (designed vs measured) | `UNDEFINED` (e.g. ± 10%) | The climax lands where the designer put it |
| Peak height | `UNDEFINED` (e.g. ± 0.1) | Tension is as sharp as intended |
| Peak count | must match | A sawtooth that measures flat is a different level |
| Largest gap at any mark (Δ max) | `UNDEFINED` (e.g. ≤ 0.2) | Catches a stretch that is much tighter or looser than meant |

---

## Reference Shapes (framework §8 — thresholds are calibrated per game)

### Normal — peak ≤ ~0.6, at most one peak, relaxed ending

```
0.6 |                            ●
0.5 |                      ●  ●     ●  ●
0.4 |                ●  ●                 ●  ●
0.3 |          ●  ●                             ●  ●
0.2 |    ●  ●                                         ●  ●
0.1 | ●                                                     ●  ●
0.0 |
    +------------------------------------------------------------
     5%          25%            50%            75%           100%
```

### Hard — sawtooth of 2–3 peaks, highest ~0.7–0.85 at ~70%

```
0.8 |                                           ●
0.7 |                                        ●
0.6 |                         ●           ●        ●
0.5 |          ●           ●     ●     ●
0.4 |       ●     ●     ●           ●                 ●
0.3 |    ●           ●                                   ●  ●
0.2 | ●                                                        ●
0.1 |
0.0 |
    +------------------------------------------------------------
     5%          25%            50%            75%           100%
```

### SuperHard — late peak ~0.85–0.95 at 80–90%, small safety margin

```
0.9 |                                                 ●
0.8 |                                           ●  ●     ●
0.7 |                                  ●  ●  ●
0.6 |                         ●  ●  ●                       ●
0.5 |                ●  ●  ●                                   ●
0.4 |          ●  ●
0.3 |    ●  ●
0.2 | ●
0.1 |
0.0 |
    +------------------------------------------------------------
     5%          25%            50%            75%           100%
```

A flat line is boring; a vertical spike at the start is frustrating — both fail review even
when the win rate is on target.

---

## Level [N] — [Tier] — "[designer's exact words]"

```
1.0 |
0.9 |
0.8 |
0.7 |
0.6 |
0.5 |
0.4 |
0.3 |
0.2 |
0.1 |
0.0 |
    +------------------------------------------------------------
     5%          25%            50%            75%           100%
```

**Rhythm**: [Where are the peaks, the valleys, the rest points? What should the player feel
at each? — in the designer's words.]

| | Values at 5%…100% (20) | Peaks (position → height) |
|---|---|---|
| Designed | [0.1 0.2 …] | [e.g. 65% → 0.8] |
| Measured | [from `/gd-level-audit` — run, bot, N attempts, date] | [..] |

**Δ max**: [value] at [mark] · **Verdict**: [OK / Revise — which check failed] · **Win rate**:
[measured] vs band [tier band]

---

## Example — filled in (illustrative numbers, not this project's data)

### Level 10 — Normal-Hard — "easy start, hard in the middle, easing off at the end"

```
1.0 |
0.9 |
0.8 |                                     ◉  ○  ○
0.7 |                               ●  ●     ●
0.6 |                         ●  ◉  ○  ○        ●  ○
0.5 |                   ●  ●                       ●
0.4 |       ○  ○  ◉  ◉  ○  ○  ○                       ◉  ○
0.3 |       ●  ●                                         ●
0.2 |    ◉                                                  ◉  ◉
0.1 | ●
0.0 | ○
    +------------------------------------------------------------
     5%          25%            50%            75%           100%
```

**Rhythm**: gentle first quarter to learn the board, pressure builds to one clear bottleneck
around two thirds in, then the board opens up so the win feels earned, not lucky.

| | Values at 5%…100% (20) | Peaks |
|---|---|---|
| Designed | 0.1 0.2 0.3 0.3 0.4 0.4 0.5 0.5 0.6 0.6 0.7 0.7 0.8 0.7 0.6 0.5 0.4 0.3 0.2 0.2 | 65% → 0.8 |
| Measured | 0.0 0.2 0.4 0.4 0.4 0.4 0.4 0.4 0.4 0.6 0.6 0.6 0.8 0.8 0.8 0.6 0.4 0.4 0.2 0.2 | 65–75% plateau → 0.8 |

**Δ max**: 0.2 at 75% · **Verdict**: Revise — the start is not easy (0.4 from 15% instead of
rising gently) and the peak is a 3-mark plateau instead of one bottleneck. Suspect resource
ordering in the 15–45% stretch (`/gd-level-audit` Phase 4, suspect 1).
