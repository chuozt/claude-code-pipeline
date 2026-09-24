#!/bin/bash
# Stop hook — Claude has just finished a turn. Compute the duration and show it NOW.
#
# Plain stdout from a Stop hook only reaches the debug log, but a JSON object with
# `systemMessage` is displayed in the transcript (code.claude.com/docs/en/hooks, Stop
# decision control). So the duration is printed as that JSON at the end of the turn it
# measures, instead of being relayed to the next turn as the first version did.
#
# NO history is written to any file: measure, show it in the session, done. The start
# marker lives in the OS temp directory, keyed by session_id, and is deleted here.
#
# Never exit non-zero: exit 2 would BLOCK Claude from ending the turn, causing a loop.

# Milliseconds since the epoch. BSD date (macOS) has no %N and prints it literally;
# fall back to whole seconds there.
now_ms() { local t; t=$(date +%s%3N); case "$t" in *[!0-9]*) echo $(( $(date +%s) * 1000 )) ;; *) echo "$t" ;; esac; }

set -u

INPUT=$(cat)
END_MS=$(now_ms)

field() {
    printf '%s' "$INPUT" \
        | grep -oE "\"$1\"[[:space:]]*:[[:space:]]*\"[^\"]*\"" \
        | head -1 | sed 's/.*"\([^"]*\)"$/\1/'
}

PROMPT_ID=$(field prompt_id)
[ -z "$PROMPT_ID" ] && PROMPT_ID="unknown"

SESSION_ID=$(field session_id)
[ -z "$SESSION_ID" ] && SESSION_ID="nosession"
SESSION_ID=$(printf '%s' "$SESSION_ID" | tr -c 'A-Za-z0-9._-' '_')

TRACK_DIR="${TMPDIR:-/tmp}/claude-prompt-track/$SESSION_ID"

[ -f "$TRACK_DIR/current" ] || exit 0
read -r START_ID START_MS < "$TRACK_DIR/current"

# Mismatched prompt_id = the start marker belongs to a different turn (the hook was
# enabled mid-session, or the turn was cancelled). A wrong report is worse than none,
# so skip this turn.
if [ "$START_ID" != "$PROMPT_ID" ]; then
    rm -f "$TRACK_DIR/current"
    exit 0
fi

case "$START_MS" in ''|*[!0-9]*) exit 0 ;; esac

ELAPSED=$(( END_MS - START_MS ))
[ "$ELAPSED" -lt 0 ] && exit 0

TOTAL_S=$(( ELAPSED / 1000 ))
TENTHS=$(( (ELAPSED % 1000) / 100 ))
MIN=$(( TOTAL_S / 60 ))
SEC=$(( TOTAL_S % 60 ))

if [ "$MIN" -gt 0 ]; then
    HUMAN="${MIN}m ${SEC}s"
else
    HUMAN="${SEC}.${TENTHS}s"
fi

rm -f "$TRACK_DIR/current"

# HUMAN holds only digits, spaces, '.', 'm' and 's', so it needs no JSON escaping.
printf '{"systemMessage":"⏱ This turn took %s."}\n' "$HUMAN"
exit 0
