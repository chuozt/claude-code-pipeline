# Level Player — how the designer plays the prototype

The simulation is developer tooling, and the designer never opens its code. But the designer must
be able to **play** it: try a level by hand, watch a bot play it, and see what a thousand bots do
to it. The **Level Player** is the one door for that: an Editor-only window the developer builds
once, that the designer uses like any other Unity tool. Claude cannot open it (no bridge in
`gd-mode`); it only reads the files the Level Player writes.

It plays the **real game**: the real View over the same Model the simulation uses. One copy of the
rules, so what the designer feels is what the bots measure.

```
Assets/_Tools/LevelPlayer/    Editor-only asmdef · never in a player build · developer-owned
                              references: the game's Model, its View, and the Sim tools
menu: Tools ▸ Level Player
```

## What it does (the five functions the designer asked for)

| Function | Behaviour |
|---|---|
| **Play** | pick a level (list or number) and play it by hand with the game's own View. Win/lose as in the game |
| **Watch a bot** | pick a bot (`random` / `greedy` / `lookahead`), a seed and a speed; the real View shows the bot's moves one by one. Pause, step one move, restart with the same seed |
| **Fast-forward** | speed 1× · 2× · 5× · 10× · 50× · 100× (`Time.timeScale` plus the View's animation speed). **Only the picture is faster:** the Model has no notion of time, so the outcome of a match never depends on speed. At very high speed the View may skip animations; say so on screen |
| **Many bots at once on one level** | two modes. **Live grid**: 1–16 matches of the same level side by side in the real View, each with its own bot and seed, all moving together. **Mass run**: hundreds or thousands of matches in parallel through `BatchRunner`, headless, with a live panel: matches done, matches per second, running win rate ± margin, outcome histogram (by loss type), mean pressure curve against the designed one |
| **Record my plays** | every hand-played match is saved as a play record (below). Nothing else is written |

## Play record — `design/levels/play-records/play-<level>-<yyyymmdd-hhmmss>-<name>.json`

```json
{
  "schemaVersion": 1,
  "level": 21, "player": "<name from Editor prefs>", "date": "<iso>",
  "modelCommit": "<git hash>", "attempt": 1,
  "result": "win | lose | quit",
  "moves": 37, "invalidTaps": 3, "restarts": 0, "seconds": 142,
  "pressure": [20 numbers, 0..1, one per 5% of progress],
  "lossType": "buffer-full | no-legal-move | … | null",
  "moveLog": [ { "n": 1, "action": "tap Bus@r3c2" } ]
}
```

`pressure` and `lossType` use the same definitions as the audit JSON
(`.claude/tools/level-audit/README.md`), so a human play can be set next to the bots'.
`attempt` counts how many times this designer has played this level: a replay is not a first
impression (see the caveats in `/gd-level-audit`).

## Rules the developer must keep

1. **Read-only towards the game.** The Level Player never changes a level, a config, a scene or a
   prefab. It writes only play records, and only under `design/levels/play-records/`.
2. **No second copy of the rules.** It uses the game's Model; bots come from the Sim tools. The
   same `(level, bot, seed)` gives the same match here and in `run-audit.sh`.
3. **Editor-only.** `"includePlatforms": ["Editor"]`, `autoReferenced: false`; nothing in the game
   references it. Verify a player build does not contain it.
4. **The designer never needs the code.** Everything is a window with buttons; opening a script is
   never part of using it. On `gd`, `gd-*` and `gd/*` branches `validate-commit.sh` refuses commits
   that touch `Assets/_Tools/LevelPlayer/`.
5. **Parallel runs stay parallel.** Mass run uses `BatchRunner`; the window stays responsive and
   reports matches per second measured, not assumed.

## Checks the developer runs before handing it over

1. Play level 1 by hand to a win and to a loss: both end correctly, a play record appears.
2. Watch `greedy` with seed 1 twice: identical move lists. Change the seed: different.
3. Fast-forward 100×: same result as 1× for the same seed.
4. Live grid with 16 matches: no shared state between boards (kill one, the others carry on).
5. Mass run of 10,000 matches: one thread vs all cores give identical aggregates; matches per
   second is shown.
6. The Mass-run win rate for a level matches `run-audit.sh` for the same arguments.
7. A player build contains no `LevelPlayer` or `Sim` assembly.
