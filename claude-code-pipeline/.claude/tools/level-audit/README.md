# Level audit runner — the black box `/gd-level-audit` runs

`/gd-level-audit` is used by the **game designer**, in `gd-mode`, where the Editor bridge and all
code are off limits. So the measuring is done by a program the **developer** builds once, and the
designer only runs one command and reads one file:

```
bash .claude/tools/level-audit/run-audit.sh --levels 21-30 --bot greedy --attempts 100
→ design/levels/audit-data/audit-<timestamp>.json
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
   CONFIG_JSON="Tools/LevelSim.Runner/config.export.json"
   CONFIG_SOURCE_DIR="Assets/_Project/Data"      # optional: enables the staleness check
   ```
4. A smoke check: `run-audit.sh --levels 1-3 --attempts 10` returns `OK`.

The runner command line (what `run-audit.sh` passes): `--levels-dir --config --levels --bot
--attempts --seed --misclick --out`. It must be **read-only** towards levels and assets, and
**deterministic**: the same arguments give the same JSON, whatever the core count.

## Output contract — `schemaVersion: 1`

```json
{
  "schemaVersion": 1,
  "run": { "bot": "greedy", "attempts": 100, "seed": 1, "misclick": 0.0, "cores": 8,
           "matchesPerSecond": 52000, "modelCommit": "<git hash>", "configExportedAt": "<iso>" },
  "levels": [
    { "level": 21, "winRate": 0.62, "winRateMargin": 0.09,
      "pressure": [20 numbers, 0..1, one per 5% of progress],
      "clearedOnLoss": 0.71, "lossType": "buffer-full | no-legal-move | …",
      "mechanics": ["Chain & Key"], "unscored": false,
      "colour": { "worstDeltaE": 14.2, "worstDeltaEColourBlind": 3.1,
                  "pair": ["Blue", "Cyan"], "windowPosition": 0.45 } }
  ]
}
```

`winRateMargin` is the half-width of the 95% interval. A level using a mechanic with no bot rule
branch has `"unscored": true` and no win rate.

## Exit codes of `run-audit.sh`

| Code | Meaning | What the designer does |
|---|---|---|
| 0 | OK, path printed | read the JSON |
| 2 | runner or config export not set up | ask the developer |
| 3 | `dotnet` missing | ask the developer |
| 4 | config export older than the config assets | ask the developer to re-export |
| 5 | runner failed or output invalid | ask the developer, quote the error |
| 6 | bad arguments | fix the command |

The script writes only under `design/levels/audit-data/`.
