#!/bin/bash
# UserPromptSubmit hook — records the turn's start time; the Stop hook reports it.
#
# Prints nothing: stdout here is handed to Claude as context. (The first version relayed
# the previous turn's duration from here, one turn late; the Stop hook now shows it
# itself through `systemMessage`.)
#
# NO history is kept. The start marker below is the handoff between the two hooks —
# they are separate processes, so a file is unavoidable — but it lives OUTSIDE the
# project, in the OS temp directory, keyed by session_id, and the Stop hook deletes it.

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

# A result left behind by the old one-turn-late version would otherwise sit there forever.
rm -f "$TRACK_DIR/last"

exit 0
