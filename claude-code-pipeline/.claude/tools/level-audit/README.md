# Level audit runner — the black box `/gd-level-audit` runs

`/gd-level-audit` is used by the **game designer**, in `gd-mode`, where the Editor bridge and all
code are off limits. So the measuring is done by a program the **developer** builds once, and the
designer only runs one command and reads one file per level:

```
bash .claude/tools/level-audit/run-audit.sh --levels 21-30 --bot greedy --attempts 100
→ design/levels/audit-data/audit-level-<N>.json      one per level measured (latest numbers)
→ design/levels/audit-data/runs/audit-<timestamp>.json   this run's log (history)
```

No Unity Editor, no `/use-mcp`. The designer (and Claude in `gd-mode`) never opens, edits or
rebuilds what is behind `run-audit.sh`; the script builds the runner itself when needed.

## What the developer sets up (once, then after config changes)

1. **A runner project** (plain .NET console, no Unity) that includes the game's Model source and
   the Sim tools source (`Assets/_Tools/LevelSim/`) with linked `<Compile Include="…" />` entries,
   so there is still **one copy of the rules** (`/gd-prototype-sim`). It runs matches in parallel
   through `BatchRunner`.
2. **A config export**: the game's config assets as one JSON file (an Editor menu item the
   developer owns). Re-export after any config change.
3. **`runner.conf`** next to this file (not committed by designers; developer-owned):

   ```
   RUNNER_PROJECT="Tools/LevelSim.Runner/LevelSim.Runner.csproj"
   LEVELS_DIR="Assets/Resources/Levels"
   CANDIDATES_DIR="design/levels/candidates"      # optional: lets --levels find candidate files too
   CONFIG_JSON="Tools/LevelSim.Runner/config.export.json"
   CONFIG_SOURCE_DIR="Assets/_Project/Data"      # optional: enables the staleness check
   ```
4. A smoke check: `run-audit.sh --levels 1-3 --attempts 10` returns `OK`.

The runner command line (what `run-audit.sh` passes): `--levels-dir [--candidates-dir] --config
--levels --bot --attempts --seed --misclick --out <run log> --per-level-dir <dir>`. It must be
**read-only** towards levels and assets, and **deterministic**: the same arguments give the same
numbers, whatever the core count.

## The two curves — one definition, used everywhere

Both are 20 numbers, one per 5 % of progress, each in 0..1, averaged over the attempts.

| Curve | Field | Meaning |
|---|---|---|
| **Difficulty curve** | `pressure` | share of BUFFER capacity held by resources that **cannot progress** towards the GOAL at that moment (blocked: nothing they need is reachable). A resource that is progressing, or done and leaving, was the right pick and counts 0 — so a full BUFFER of right picks reads low. **1 = the loss state** (BUFFER full, nothing progresses). |
| **Stuck curve** | `stuckRate` | share of attempts in which the player is **stuck for a way** at that mark: something is still left to do, but no legal action helps now and nothing in the BUFFER is progressing. 0 = nobody stuck (an open end of level reads 0); a rise shows where players hit a dead end. |

Read them side by side, never merged: a high `pressure` with a low `stuckRate` is tight but
solvable; a rising `stuckRate` is a dead end whatever the pressure says. The designer's own
pressure proxy in `design/gdd/gd-flow-map.md` may be defined differently (for example
`occupancy ÷ capacity`); a comparison says which definition it uses. `/gd-level-gen` §3 filters
on the same two fields.

## Output contract

### Per-level file — `audit-level-<N>.json`, `schemaVersion: 2`

Written (created or updated) by the runner for every level it measured: one entry per bot, the
**latest** run only. A candidate file is its own level: `Level_5_c2.json` → `audit-level-5_c2.json`.

```json
{
  "schemaVersion": 2, "level": 21, "source": "Assets/Resources/Levels/Level_21.json",
  "entries": {
    "greedy": {
      "sourceHash": "<sha1 of the level file measured>", "measuredAt": "<iso>", "runLog": "runs/audit-<timestamp>.json",
      "attempts": 100, "seed": 1, "misclick": 0.0, "modelCommit": "<git hash>", "configExportedAt": "<iso>",
      "winRate": 0.62, "winRateMargin": 0.09,
      "pressure":  [20 numbers, 0..1],
      "stuckRate": [20 numbers, 0..1],
      "clearedOnLoss": 0.71, "lossType": "buffer-full | no-legal-move | …",
      "mechanics": ["Chain & Key"], "unscored": false,
      "colour": { "worstDeltaE": 14.2, "worstDeltaEColourBlind": 3.1,
                  "pair": ["Blue", "Cyan"], "windowPosition": 0.45 }
    }
  }
}
```

**`sourceHash` is what makes an entry valid.** The reader (`/gd-level-audit`) hashes the level
file as it is now; an entry whose `sourceHash` differs was measured on an older file — the level
was regenerated or edited since — and is read as **`unmeasured`**, never quoted. That is how
overwriting a candidate (`/gd-level-gen` §4c) voids its numbers without anybody editing this file.

`winRateMargin` is the half-width of the 95 % interval. A level using a mechanic with no bot
rule branch has `"unscored": true` and no win rate.

### Run log — `runs/audit-<timestamp>.json`, `schemaVersion: 1`

The whole run as it happened, never updated afterwards: `run` (bot, attempts, seed, misclick,
cores, `matchesPerSecond`, `modelCommit`, `configExportedAt`) and `levels[]` with the same
fields as an entry above (plus `level` and `sourceHash`). History for `/gd-calibrate`;
`run-audit.sh` validates this file.

## Exit codes of `run-audit.sh`

| Code | Meaning | What the designer does |
|---|---|---|
| 0 | OK, paths printed | read the per-level files |
| 2 | runner or config export not set up | ask the developer |
| 3 | `dotnet` missing | ask the developer |
| 4 | config export older than the config assets | ask the developer to re-export |
| 5 | runner failed or output invalid | ask the developer, quote the error |
| 6 | bad arguments | fix the command |

The script writes only under `design/levels/audit-data/`.
