#!/bin/bash
# UserPromptSubmit hook — records the turn's start time, and reports the PREVIOUS turn.
#
# Why it reports the previous turn: Claude Code discards the `Stop` hook's stdout
# (it only reaches the debug log), so there is no way to show a duration at the
# moment that turn ends. `UserPromptSubmit` stdout IS handed to Claude. So the Stop
# hook writes its result to a scratch file and this hook reads it on the next turn.
#
# Visible consequence: the duration appears at the START of the following turn,
# exactly one turn late.
#
# NO history is kept. Measure and display within the running session only. The two
# files below are the handoff between the two hooks — they are separate processes, so
# a file is unavoidable — but they live OUTSIDE the project, in the OS temp directory,
# keyed by session_id, and are deleted immediately after being read.

# Milliseconds since the epoch. BSD date (macOS) has no %N and prints it literally;
# fall back to whole seconds there.
now_ms() { local t; t=$(date +%s%3N); case "$t" in *[!0-9]*) echo $(( $(date +%s) * 1000 )) ;; *) echo "$t" ;; esac; }

set -u

INPUT=$(cat)

# No jq on this machine — extract strings with grep -E (grep -P is often absent on Windows).
field() {
    printf '%s' "$INPUT" \
        | grep -oE "\"$1\"[[:space:]]*:[[:space:]]*\"[^\"]*\"" \
        | head -1 | sed 's/.*"\([^"]*\)"$/\1/'
}

PROMPT_ID=$(field prompt_id)
[ -z "$PROMPT_ID" ] && PROMPT_ID="unknown"

SESSION_ID=$(field session_id)
[ -z "$SESSION_ID" ] && SESSION_ID="nosession"
# Keep only characters that are safe in a directory name.
SESSION_ID=$(printf '%s' "$SESSION_ID" | tr -c 'A-Za-z0-9._-' '_')

TRACK_DIR="${TMPDIR:-/tmp}/claude-prompt-track/$SESSION_ID"
mkdir -p "$TRACK_DIR" 2>/dev/null || exit 0

printf '%s %s\n' "$PROMPT_ID" "$(now_ms)" > "$TRACK_DIR/current"

# Hand the previous turn's result to Claude, then delete it so it is not reported twice.
if [ -f "$TRACK_DIR/last" ]; then
    cat "$TRACK_DIR/last"
    rm -f "$TRACK_DIR/last"
fi

exit 0
