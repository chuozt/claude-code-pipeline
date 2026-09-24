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

## The duration appears ONE TURN LATE — a limitation, not a bug

Claude Code **discards** the `Stop` hook's stdout (debug log only, shown to neither the
developer nor Claude). So there is no way to print the duration at the moment that turn ends.

The workaround: `Stop` leaves the result in the scratch file → the **next** turn's
`UserPromptSubmit` reads it and hands it to Claude.

**Consequence**: the last turn of a session is never reported — there is no following turn to
report it, and no history to look back at. Accepting that is the price of keeping no files.

## What Claude does when it sees a ⏱ line

At the start of a turn, if a line like `⏱ Previous turn took ...` appears in context:

**Reprint that exact line at the start of the answer, then work normally.** One line, no
commentary, no judgements about fast/slow unless the developer asks.

No such line → say nothing about timing. **Never invent a duration** — if the hook did not
report one, Claude has no way to know it.

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
  printf '%s' "$P" | bash .claude/hooks/track-prompt-start.sh
  sleep 2
  printf '%s' "$P" | bash .claude/hooks/track-prompt-stop.sh
  printf '%s' '{"prompt_id":"p2","session_id":"s1"}' | bash .claude/hooks/track-prompt-start.sh
  # → must print: ⏱ Previous turn took 2.0s.
  ```
