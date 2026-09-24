# Path-Specific Rules

Rules in `.claude/rules/` load automatically when Claude works on a file matching the `paths:`
in their frontmatter. Keep this table in step with that frontmatter; `rules.md` §6 has the
short version.

| Rule file | Paths | Enforces |
|---|---|---|
| `gameplay-code.md` | `**/Scripts/**/Gameplay/**`, `**/Scripts/**/Mechanics/**` | Pure-C# model: no MonoBehaviour, no `deltaTime`, data-driven values, no UI references |
| `ui-code.md` | `**/Scripts/**/UI/**`, `**/Screens/**` | No game-state ownership, localisation, never blocks, subscribe/unsubscribe pairs |
| `editor-tool-code.md` | `**/Editor/**` | Undo support, no silent overwrites, headless-runnable business logic |
| `data-config.md` | `**/Resources/**`, `**/*.asset`, `**/Data/**` | One config per system, `[CreateAssetMenu]`, migrations |
| `test-code.md` | `**/Tests/**`, `**/*Tests.cs` | Naming, deterministic, independent, regression test per fixed bug |
| `design-docs.md` | `**/docs/features/**`, `**/design/**` | All 8 sections, formulas with units and ranges, edge cases |

The gameplay rule forbids MonoBehaviour for everything under `Gameplay/`, so views must live
outside it (`project_setup.md` §5 puts `View/` next to `Gameplay/`, not inside). A project with
a different folder tree edits the `paths:` of these files, not the rules themselves.
