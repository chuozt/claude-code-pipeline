---
name: track-prompt-duration
description: "Enable/disable measuring how long each Claude turn takes, from the moment the developer sends a prompt until Claude finishes answering. Use when the developer types /track-prompt-duration on|off or says 'time each prompt', 'how long did that turn take', 'turn on time tracking'."
argument-hint: "on | off | (empty = show status)"
user-invocable: true
allowed-tools: Read, Write, Edit, Bash, Glob
---

Measures **one turn**: from the developer pressing send to Claude finishing its answer.

## Session-only — NO history is kept ⭐

Enabled means measure and **display within the running session**; no history file
accumulates on disk. Close the session and it is gone.

Exactly **one temporary file** remains, and it is unavoidable: `UserPromptSubmit` and `Stop`
are **two separate processes** that cannot pass variables to each other, so the timestamp has
to travel through disk. That file:

- lives **outside the project**, at `${TMPDIR:-/tmp}/claude-prompt-track/<session_id>/`
- is **per session**, so two Claude windows open at once do not overwrite each other
- is **deleted immediately after being read**, never appended to

⚠️ Do not reintroduce a `durations.tsv` or any other form of history — it existed once and
was removed deliberately.

## How it works — and why a plain skill cannot do it

Claude **cannot time itself**: it does not know when the developer pressed send, and it will
forget to run `date` if merely told to in prose. This has to be done by a **hook** — the
harness runs hooks, not Claude.

Two hooks `[VERIFIED: code.claude.com/docs/en/hooks]`:

| Hook | Fires when | Script |
|---|---|---|
| `UserPromptSubmit` | the developer sends a prompt, before Claude processes it | `.claude/hooks/track-prompt-start.sh` |
| `Stop` | Claude finishes **one turn** | `.claude/hooks/track-prompt-stop.sh` |

⚠️ **`Stop` is not the end of the session** — that is `SessionEnd`.
(`.claude/docs/hooks-reference/hook-input-schemas.md` gets this wrong.)

## The duration appears at the end of the same turn

A `Stop` hook's **plain** stdout goes to the debug log only, but a JSON object with
`systemMessage` is displayed in the transcript `[VERIFIED: code.claude.com/docs/en/hooks,
Stop decision control]`. So `track-prompt-stop.sh` prints

```json
{"systemMessage":"⏱ This turn took 42.3s."}
```

and the developer sees it right after the turn it measures, including the last turn of a
session.

⚠️ The first version relayed the duration through a scratch file to the **next** turn's
`UserPromptSubmit`, so every report arrived one prompt late. Do not bring that back: the
start hook now prints nothing (its stdout would be injected into Claude's context) and
deletes any leftover `last` file from the old version.

## What Claude does about timing

Nothing. The harness shows the ⏱ line itself; Claude does not reprint it, comment on it, or
judge fast/slow unless the developer asks. **Never invent a duration** — Claude has no way to
know one the hook did not report.

## `on` — enable

1. Read `.claude/settings.local.json` (create it if absent).
2. Merge the block below into `hooks`. **Keep** every existing hook — merge into the arrays,
   do not overwrite the file.

```json
{
  "hooks": {
    "UserPromptSubmit": [
      {
        "matcher": "*",
        "hooks": [
          {
            "type": "command",
            "command": "bash \"${CLAUDE_PROJECT_DIR}/.claude/hooks/track-prompt-start.sh\"",
            "timeout": 10
          }
        ]
      }
    ],
    "Stop": [
      {
        "matcher": "*",
        "hooks": [
          {
            "type": "command",
            "command": "bash \"${CLAUDE_PROJECT_DIR}/.claude/hooks/track-prompt-stop.sh\"",
            "timeout": 10
          }
        ]
      }
    ]
  }
}
```

3. Tell the developer: **hooks only take effect from the next session**, so Claude Code must
   be restarted.

No `.gitignore` change is needed: the scratch directory is outside the project, and
`.claude/settings.local.json` is already gitignored by this kit.

## `off` — disable

Remove exactly those two blocks from `.claude/settings.local.json`. **Leave** any other hook
in the file. If `hooks` ends up empty, drop the `hooks` key entirely.

Do not delete the scripts in `.claude/hooks/` unless the developer asks.

## No argument — show status

Read `.claude/settings.local.json` and report whether it is **on** or **off**. That is all.

**There are no statistics to report** — this skill deliberately keeps no history, so there is
no turn count, no average, no "last 5 turns". If the developer asks for statistics → say
plainly that nothing is stored, and **do not go digging through old files or estimating**.

## Traps already hit, do not repeat them

- **The `Stop` hook must never exit non-zero.** `exit 2` **blocks Claude from ending the
  turn** → infinite loop. The current script always exits 0; keep it that way.
- **`jq` may not exist.** Parse JSON with `grep -oE` (not `grep -P` — often absent on Windows).
- **Two parallel sessions**: handled by keying the scratch directory on `session_id`. The
  second safeguard stays too — compare `prompt_id`, and on a mismatch **skip that turn**
  rather than report a wrong number (this catches hooks enabled mid-session, and cancelled turns).
- **Never write anything inside the project.** The first version wrote
  `.claude/.track/durations.tsv` and had to be removed; the scratch location is now
  `${TMPDIR:-/tmp}/claude-prompt-track/<session_id>/`.
- After editing a hook script, **test with a fake payload** before trusting it — run the whole
  cycle, not just one script:
  ```bash
  P='{"prompt_id":"p1","session_id":"s1"}'
  printf '%s' "$P" | bash .claude/hooks/track-prompt-start.sh   # → must print nothing
  sleep 2
  printf '%s' "$P" | bash .claude/hooks/track-prompt-stop.sh
  # → must print valid JSON: {"systemMessage":"⏱ This turn took 2.0s."}
  printf '%s' '{"prompt_id":"pX","session_id":"s1"}' | bash .claude/hooks/track-prompt-stop.sh
  # → mismatched or missing start: must print nothing and exit 0
  ```
- **Stop's stdout must be one JSON object and nothing else.** A stray `echo` before it turns
  the whole output into plain text, which lands in the debug log and the ⏱ line disappears.
