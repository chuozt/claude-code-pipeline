---
name: mcp-check
model: claude-opus-5-5
effort: medium
description: Check whether the Unity CLI / MCP bridge is attached to the right project and whether the Editor is in a state where files can be edited. Useful when Claude reports a compile error for a type that certainly exists. Only when the developer types /mcp-check or says "check MCP", "check the Unity bridge", "Claude says it cannot find a type".
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

# /mcp-check — check the Unity bridge

The bridge (`unity` CLI + `com.unity.pipeline`, exposed to Claude as
`mcp__unity-editor-mcp__*`) follows the **running Editor**, not the chat's working
directory. With the wrong project open, every command fails silently — the classic symptom
is `CS0246: type or namespace not found` for types that definitely exist in the project.

This skill only reads state. `unity status`, `unity pipeline list` and `unity mcp configure
--list` are always allowed. The bridge status calls (`editor_status`, `recompile_status`) are
allowed **only because the developer typed `/mcp-check`**, for this prompt only (`CLAUDE.md`
§2.1). Never run this skill on your own initiative — not at session start, not "to be sure";
suggest `/mcp-check` to the developer instead.

## 1. Check the connected Editor

```bash
unity status --no-banner
```

Expected: exactly one line with state `ready`, `project` matching this project's own root
(the directory this session is working in, not a value copied from another project), and
`version` matching the Unity version pinned in `CLAUDE.md` §1. Then
`mcp__unity-editor-mcp__editor_status` (or `unity command editor_status`) for `compiling`,
`domainReloadInProgress`, `playMode`.

**Project path mismatch → STOP, tell the developer, do nothing else.**
Two Editors listed → every later call must pass `--project-path <this project's path>`.

## 2. Check the version

`unityVersion` must match `CLAUDE.md` §1 exactly. Even a minor version difference must be
reported — the APIs can differ.

## 3. Check the Editor state

Files must not be edited while: in Play mode · compiling · a modal popup is open.
`recompile_status` should be `completed` / `up_to_date` with `failed: false`.

## 4. No `ready` instance although Unity is open → Safe Mode?

```bash
unity pipeline list
```

`Editor is in Safe Mode - Pipeline server disabled` means compile errors keep the Pipeline
package from loading. Read `Logs/Editor.log` filtered on `error CS`, report the errors, and
ask the developer to fix/restart. Do not "edit blindly" (`mcp_unity.md` §1).

## 5. Check the MCP registration

`unity mcp configure --list` shows which clients are configured. For Claude Code the entry
lives in the user's `~/.claude.json` (`unity-editor-mcp` → `unity mcp`); if the
`mcp__unity-editor-mcp__*` tools are absent from the session, that registration is missing
on this machine — tell the developer to run `unity mcp configure claude-code`.

## Result format

```
✅ Bridge OK — <game name>, Unity <version>, branch <git branch>, Editor idle (port <port>)
```
or state exactly which check failed and what the developer needs to do.
