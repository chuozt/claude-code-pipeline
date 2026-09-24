# General Rules & Multi-Developer Operation

---

## 1. Unity Architecture

- **Namespaces** match the directory path exactly.
- **Design patterns:** prefer Observer (events) and State. Avoid static singletons for game state.
- **Assembly Definitions:** use `.asmdef` sensibly, **one-way** references, no cycles.
- **Interface first, implementation second.** Shared interfaces must be agreed and merged
  into the integration branch **before** work is split across people.

---

## 2. AI Operation — MCP Is OFF By Default

⛔ **All `mcp__unity-editor-mcp__*` tools, all `mcp__unity-mcp__Unity_*` tools and Bash
`unity command|eval|test|build|run|open` calls are forbidden** until the developer types
`/use-mcp` (`CLAUDE.md` §2.1 · `skills/use-mcp/SKILL.md`). Two bridges exist — Unity CLI +
`com.unity.pipeline` (preferred) and the in-Editor MCP — see `mcp_unity.md` §0. The offline
`mcp__unity-api__*` lookup is always allowed and should be used before quoting any Unity API.

- **Read the source docs first:** scan the feature's `docs/features/*.md` before editing.
- **Editing code:** `Edit` directly on disk. Do not rewrite a whole file for a few lines.
- **Compilation IS checkable while MCP is off.** With the Editor open,
  `dotnet build Assembly-CSharp.csproj --no-restore`, plus `Assembly-CSharp-Editor` and one
  `.csproj` per game `.asmdef` (the list and the baseline warning counts live in `CLAUDE.md`
  §2.1, filled in per project). The gate proves
  compilation only, so report *"compiles, not tested"*, never that behaviour was verified.

Once the developer enables it (details in `mcp_unity.md`): check `unity status` /
`editor_status` first (right project, right version); scripts are still edited **on disk**
with `Edit`, then `recompile` → `recompile_status`; verify the Editor is not in Play mode,
not compiling, and has no modal popup open.

---

## 3. Performance

- Measure with the Profiler **before** changing anything. Quote real numbers, never "slow".
- Frame budget: **16.6 ms** for 60fps · **33 ms** for 30fps. State how much is being spent.
- Object pooling is mandatory for VFX / audio / continuously spawned objects.
- The gameplay loop allocates ≈ **0 B/frame** of GC.
- LOD for 3D models. Drop effects by quality tier on weak devices.

---

## 4. Working With 2–3 Developers — File Ownership

Unity stores scenes/prefabs as YAML and auto-merge breaks them often.
**Agree the boundaries BEFORE writing code.**

| File type | Rule |
|---|---|
| Scene | **One scene, one owner.** Need a change in someone else's scene → message them, do not edit |
| UI prefab | One screen, one prefab |
| ScriptableObject config | One system, one asset. No single giant "GameConfig" asset |
| Script | Split by namespace/folder. Large file → `partial class` |
| `ProjectSettings/` | **Lead developer only**, and the whole team must be told |

### Reference Branch Layout

**Filled in by the developer: `<...>` developers, integration branch `<...>`.**

```
main                      ← released builds only, nobody codes directly here
 └── <integration branch> ← integration branch, owned by the lead developer
      ├── feature/<name>  ← one branch per developer, created when work is split
      ├── gd              ← designers push levels/data, NO .cs files (if the team has one)
      └── art             ← artists push assets, NO .cs files (if the team has one)
```

Adjust to the branches actually in use — this is a starting shape, not a requirement to
create branches nobody needs yet. Create `feature/*`, `gd` and `art` only when the work
actually splits, not before.

- `gd` and `art` branch off the integration branch and merge back **one-way** into it.
- `feature/*` is named after the feature, not the person — branches are short-lived, people change tasks.
- Merge into the integration branch **at least once a day**. Three days of drift turns conflicts into a disaster.
- Before starting a new feature: **pull the latest integration branch**.

### Using Claude With Several People

- **One Claude session per developer.** Never share one — one person's context pollutes the other's.
- Each developer states their branch in the base prompt. Claude verifies with
  `git rev-parse --abbrev-ref HEAD` before touching files.
- If Claude sees it must edit a file outside that developer's scope → **STOP and report**, do not edit.

---

## 5. Three Mandatory Git Rules In A Unity Project

1. **New assets must be committed with their `.meta`.** Quick check:
   ```bash
   git status --porcelain --untracked-files=all | grep -v '\.meta$'
   ```
2. **Scan references before deleting an asset** with `AssetDatabase.GetDependencies` across
   every prefab / scene / ScriptableObject. Do not rely on file names.
3. **Read the diff before committing**, do not skim file names. Unity repos commonly sneak in:
   - assets re-serialised after adding/removing a ScriptableObject field — the diff looks like
     mass deletion but is harmless (grep the field name in `.cs` to confirm)
   - values someone tweaked by hand in the Inspector
   - TMP font atlases (`*SDF.asset`) growing after a play session — harmless
   - scene / ProjectSettings changes caused by Unity being open during a branch switch

---

## 6. Path-Scoped Rules

`.claude/rules/` holds rules that apply to specific file paths. When editing a file matching
a pattern, the AI must also apply the corresponding rules:

| Rule file | Applies to | Main content |
|---|---|---|
| `rules/gameplay-code.md` | gameplay logic code | data-driven, deltaTime, no UI references |
| `rules/ui-code.md` | UI code | holds no state, localisation, never blocks |
| `rules/editor-tool-code.md` | editor/tool code | Undo, no silent overwrites, no runtime coupling |
| `rules/data-config.md` | ScriptableObject, JSON, levels | schema, naming, migration |
| `rules/test-code.md` | tests | naming, deterministic, independent |
| `rules/design-docs.md` | design documents | all 8 required sections |
