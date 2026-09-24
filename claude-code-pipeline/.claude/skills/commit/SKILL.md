---
name: commit
model: claude-opus-5-5
effort: medium
description: Commit the current changes in this Unity repo, including a review pass for unexpected changes and a commit message written in the repo's established style. Use when the developer types /commit or asks to "commit the code", "commit what is staged".
---

> **Coding rule — mandatory.** Every line of C# this skill writes, reviews, or proposes must follow `.claude/coding_convention.md` (Allman braces, §9 script layout, field order and naming, `GameDebug` instead of `Debug.Log`, no `{ get; private set; }`). Where any sample or advice below disagrees with that file, the convention wins.

# /commit — commit changes

A Unity project where several people push to `develop` / `art` / `gd` / `feature/*`, and where
the developer often runs git in another tool at the same time. The review step therefore
matters more than the commit itself.

## 1. Establish the scope

```bash
git rev-parse --abbrev-ref HEAD
git status --short
```

- On the default branch (`main`) → **stop and ask the developer**, never commit straight to it.
- "Commit what is staged" → commit exactly the staged set, **do not** `git add` anything more.
- "Commit everything" → `git add -A`, but still go through step 2 before committing.
- Nothing to commit → say so, do not create an empty commit.

## 2. Review — the most commonly skipped step

Read the diff before committing. Never skim file names and commit.

```bash
git diff --cached --stat
```

For **every file you did not edit yourself**, open the diff and **tell the developer about it**
in your reply (still commit if they said commit everything — but name it, do not bundle it
silently). Common in a Unity repo:

- **Unity re-serialising an asset** after a ScriptableObject field was added/removed/commented
  out — the diff looks like mass deletion or addition but is harmless. Verify by grepping the
  field name in the matching `.cs` file to see whether it still exists.
- **Values the developer tweaked by hand in the Inspector** slipping in (e.g. one float in a
  config changing). Compare against `git show HEAD:<file>` and against the default in the `.cs`
  to tell a hand edit from a Unity rewrite.
- **TMP font atlases** (`*SDF.asset`) growing after a play session — harmless, one line is enough.
- **Scene / ProjectSettings changes caused by Unity being open** during a branch switch.
  `ProjectSettings/TagManager.asset` is especially dangerous: losing a game-specific layer name
  makes a scene camera's culling mask display wrongly (the Inspector cannot draw an unnamed
  layer) even though the stored value is still correct.

If any `.cs` file changed, confirm the project compiles cleanly before committing: run the
`dotnet build` gate (`CLAUDE.md` §2.1), or — only when the bridge is enabled — `recompile`
then `recompile_status` with `failed: false`. Otherwise say plainly that compilation was not verified.

## 3. New assets must include their `.meta`

Adding a new asset (prefab, material, shader, texture…) **requires** committing its `.meta`
file too. Without it, another machine imports the asset with a fresh GUID and every reference
to it breaks.

```bash
git status --porcelain --untracked-files=all | grep -v '\.meta$'
```

Cross-check whether any added asset has a forgotten `.meta`.

Deleting an asset means deleting both the `.mat`/`.prefab` and its `.meta`, and **checking
references first** — scan with `AssetDatabase.GetDependencies` across every
prefab/scene/ScriptableObject, never rely on file names. `git rm` errors on a file with
uncommitted modifications; only use `-f` after reading the diff and confirming those
modifications can be discarded.

## 4. Write the commit message

Match the language and tone of the repo's recent commits (`git log -10 --format=%s`).

- Short subject line describing the **result**, not the action taken.
- The body explains **why** and **the trap that was hit**, without re-listing files (the diff has that).
- If there are measurements, put the numbers in (before/after, explicit units). If something
  could not be verified, say so plainly.
- If unrelated changes are bundled in, add an "Also included: …" paragraph at the end.

**Syntax warning:** the Bash tool here is Git Bash, **not PowerShell**. Never use a
PowerShell here-string like `-m @'...'@` — it puts literal `@` characters at both ends of the
message. Write the message to a file with a heredoc and use `-F`:

```bash
cat > /tmp/msg.txt <<'EOF'
Short subject line

Body explaining why, with measurements where they exist.

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>
EOF
git commit -F /tmp/msg.txt
```

Always end the message with the `Co-Authored-By` line above.

## 5. After committing

```bash
git log -1 --format='%h %s'
git status --short
```

Report back: the commit hash, the file count, and **what is still outstanding**.

**Never `git push`**, even when asked — pushing is the developer's job (`CLAUDE.md` §4, §10);
say the commit is ready to push. Never `--amend` a pushed commit.
