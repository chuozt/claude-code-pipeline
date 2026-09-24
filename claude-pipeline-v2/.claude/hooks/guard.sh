#!/bin/bash
# Runs unity_guard.py with the first Python that actually works on this machine.
#
# Why a launcher: a bare `python` in settings.json is the Microsoft Store stub on many Windows
# machines (it prints "Python was not found" and exits 49), and macOS usually has only
# `python3`. Claude Code treats any exit code other than 2 as "carry on", so the guard silently
# did nothing. This probes each candidate for real before handing over stdin.
#
#   bash guard.sh <mode>   pre-edit | post-edit | pre-bash | pre-eval  (stdin = hook JSON)
#   bash guard.sh --probe  exit 0 if a Python was found, 1 if not (session-start.sh uses it)
#
# No Python → exit 0 (fail open): a missing interpreter must not stop every edit. The
# SessionStart hook reports it once per session so the gap is visible instead of silent.

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

for PY in python3 python py; do
    if command -v "$PY" >/dev/null 2>&1 && "$PY" -c "import sys" >/dev/null 2>&1 </dev/null; then
        [ "$1" = "--probe" ] && exit 0
        exec "$PY" "$DIR/unity_guard.py" "$@"
    fi
done

[ "$1" = "--probe" ] && exit 1
exit 0
