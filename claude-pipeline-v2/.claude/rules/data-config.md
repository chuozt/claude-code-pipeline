---
paths:
  - "**/Resources/**"
  - "**/*.asset"
  - "**/Data/**"
---

# Rules For Data & Configuration

- Every ScriptableObject has `[CreateAssetMenu]` with a `menuName` following the project's folder tree.
- **One system, one config asset.** No single giant "GameConfig" asset — everyone
  editing the same file means constant conflicts.
- Adding/removing a field in a ScriptableObject makes Unity re-serialise the asset, so the
  git diff looks huge but is usually harmless. Confirm by grepping the field name in `.cs`.
- Versioned data needs a **migration** path. Changing the schema without one loses player saves.
- File names and keys: consistent, no accents, no spaces.
- Level files are **only ever produced by a tool**, never hand-edited.
- JSON must parse — check before committing.

## Data invariants
Every data type needs an explicitly written list of **invariants**, and a validator that checks them.
For example: *"the total resource count of each colour must EXACTLY equal the number of cells needing that colour"*.
Off by one is corrupt data that no one can spot by eye.
