---
name: unity-yaml-reading-guide
model: claude-opus-5-5
effort: medium
description: >-
  How to READ Unity YAML-serialised assets (.asset ScriptableObjects, .mat materials, and the
  structure inside .prefab/.unity): headers, !u! class ids, fileID/guid references, scalar forms.
  Use when parsing a config or level asset from disk while the Editor bridge is off, when
  reviewing an asset diff before a commit, or when tracing which asset references which GUID.
  Never a licence to edit: serialised assets are changed in the Inspector or through the bridge.
user-invocable: false
license: Unlicense
metadata:
  author: Koji Hasegawa
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

> **Kit note.** Adapted from *nowsprinting/unity-coding-skills*, where it was an editing guide.
> Here it is **read-only**: hand-editing `.asset` / `.mat` / `.prefab` / `.unity` / `.meta` is
> forbidden (`anti-patterns.md` §5/§6) and refused by the `pre-edit` hook. To change a value,
> use the Inspector, or the bridge after `/use-mcp` (`set_serialized_field`, `batch`,
> `SerializedObject` in `eval` — `mcp_unity.md` §4).

## What the file looks like

- **Header.** Lines 1–2 are `%YAML 1.1` and `%TAG !u! tag:unity3d.com,2011:`. A file that does
  not start this way is Binary/Mixed serialisation and cannot be parsed as text at all.
- **Document marker.** `--- !u!<classID> &<fileID>`. A ScriptableObject is `--- !u!114 &11400000`
  (class 114 = `MonoBehaviour`), a Material `--- !u!21 &2100000`. Prefabs and scenes hold many
  documents, one per object/component, each with its own `&fileID`.
- **Preamble.** A ScriptableObject mapping starts `m_ObjectHideFlags` … `m_Script` → `m_Name` →
  `m_EditorClassIdentifier`; the game's own fields come after, under their C# field names.
- **`m_Script`** is `{fileID: 11500000, guid: <script .cs.meta guid>, type: 3}` — the way to find
  which class an asset is: grep that GUID in `*.cs.meta`.
- **`m_Name`** equals the file name without extension.
- **References.** Cross-file: `{fileID: N, guid: <32-hex>, type: 2}` (project asset) or `type: 3`
  (script). Same file: `{fileID: N}` matching an `&N` anchor. `{fileID: 0}` is null. A GUID that
  no `.meta` in the project declares is a **missing reference** — worth reporting.
- **Auto-property backing fields** appear as `<PropertyName>k__BackingField`.
- **Non-ASCII strings** are double-quoted with `\uXXXX` escapes; decode them before quoting.

## Reading a diff before a commit

- A field added/removed in the C# class makes Unity re-serialise every asset of that type: the
  diff looks huge but is harmless if the field name matches the `.cs` change.
- A float that changed by a tiny amount (`0.30000001`) is Unity rewriting, not a tweak.
- A GUID that changed in a reference means the asset now points at a different file — check it.

## Scalar quick-reference

| Type | Form | Example |
|---|---|---|
| bool | `0` / `1` | `m_Enabled: 1` |
| int / enum | bare integer | `<Cost>k__BackingField: 1` |
| float | plain decimal | `m_Glossiness: 0.5` |
| ASCII string | unquoted | `m_Name: DryPrinciple` |
| non-ASCII string | double-quoted, `\uXXXX` escapes | `"DRY原則"` |
| Vector3 | flow mapping | `{x: 0, y: 0, z: 0}` |
| Quaternion | flow mapping | `{x: 0, y: 0, z: 0, w: 1}` |
| Color | flow mapping | `{r: 1, g: 1, b: 1, a: 1}` |
| local ref | flow mapping | `{fileID: 11400000}` |
| cross-file ref | flow mapping | `{fileID: 11400000, guid: <32-hex>, type: 2}` |
| null ref | flow mapping | `{fileID: 0}` |
| list of refs | block sequence with `- ` bullets | one `- {fileID: ...}` per line |

## Resources

- Unfamiliar structure (class-typed contents, nested serialisable classes, `[SerializeReference]`
  `rid` blocks): read `${CLAUDE_SKILL_DIR}/resources/asset-yaml-format.md`. Its C# samples show
  how a field serialises, not how to write the class — `coding_convention.md` still governs code.
