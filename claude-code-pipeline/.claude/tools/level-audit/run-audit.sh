#!/bin/bash
# Sanctioned entry point for /gd-level-audit. Owned by the developer.
# A designer (and Claude in gd-mode) RUNS this and READS its JSON; nobody opens, edits or rebuilds
# what is behind it. No Unity Editor and no MCP bridge are needed: the runner is a plain .NET
# program over the game's pure-C# Model and the Sim tools (see README.md).
#
#   bash .claude/tools/level-audit/run-audit.sh --levels 21-30 --bot greedy --attempts 100 \
#        [--seed 1] [--misclick 0.25] [--allow-stale]
#
# Pure bash, no jq. Writes ONLY under design/levels/audit-data/. Never touches Assets/ or levels.
# Exit codes: 0 ok · 2 runner not set up · 3 dotnet missing · 4 config export stale
#             5 runner failed or produced invalid output · 6 bad arguments

set -u

LEVELS=all; BOT=greedy; ATTEMPTS=100; SEED=1; MISCLICK=0; STALE_OK=0
while [ $# -gt 0 ]; do
    case "$1" in
        --levels)   LEVELS=${2:-}; shift 2 ;;
        --bot)      BOT=${2:-}; shift 2 ;;
        --attempts) ATTEMPTS=${2:-}; shift 2 ;;
        --seed)     SEED=${2:-}; shift 2 ;;
        --misclick) MISCLICK=${2:-}; shift 2 ;;
        --allow-stale) STALE_OK=1; shift ;;
        *) echo "unknown argument: $1" >&2; exit 6 ;;
    esac
done

case "$BOT" in greedy|random|lookahead) ;; *) echo "--bot must be greedy, random or lookahead" >&2; exit 6 ;; esac
[[ $LEVELS =~ ^(all|[0-9]+(-[0-9]+)?)$ ]] || { echo "--levels must be all, N or N-M" >&2; exit 6; }
[[ $ATTEMPTS =~ ^[0-9]+$ && $SEED =~ ^[0-9]+$ ]] || { echo "--attempts and --seed must be whole numbers" >&2; exit 6; }
[[ $MISCLICK =~ ^[0-9]*\.?[0-9]+$ ]] || { echo "--misclick must be a number between 0 and 1" >&2; exit 6; }

ROOT=$(git rev-parse --show-toplevel 2>/dev/null) || ROOT=$PWD
CONF="$ROOT/.claude/tools/level-audit/runner.conf"
if [ ! -f "$CONF" ]; then
    echo "AUDIT RUNNER NOT SET UP: .claude/tools/level-audit/runner.conf is missing." >&2
    echo "Ask the developer to set it up (README.md in this folder); nothing was measured." >&2
    exit 2
fi

RUNNER_PROJECT=""; LEVELS_DIR=""; CONFIG_JSON=""; CONFIG_SOURCE_DIR=""
# shellcheck disable=SC1090
. "$CONF"
if [ -z "$RUNNER_PROJECT" ] || [ -z "$LEVELS_DIR" ] || [ -z "$CONFIG_JSON" ]; then
    echo "runner.conf must define RUNNER_PROJECT, LEVELS_DIR and CONFIG_JSON (see README.md)." >&2
    exit 2
fi

command -v dotnet >/dev/null 2>&1 || { echo "dotnet is not on PATH; the developer must install the .NET SDK." >&2; exit 3; }
[ -f "$ROOT/$CONFIG_JSON" ] || { echo "Config export not found: $CONFIG_JSON. Ask the developer to export it." >&2; exit 2; }

# The runner reads an EXPORT of the game's config assets. If an asset changed after the export,
# every number would describe an old game: refuse instead of measuring the wrong thing.
if [ -n "$CONFIG_SOURCE_DIR" ] && [ "$STALE_OK" = 0 ]; then
    NEWER=$(find "$ROOT/$CONFIG_SOURCE_DIR" -type f ! -name '*.meta' -newer "$ROOT/$CONFIG_JSON" 2>/dev/null | head -3)
    if [ -n "$NEWER" ]; then
        printf 'CONFIG EXPORT IS STALE: these assets changed after %s was exported:\n%s\n' "$CONFIG_JSON" "$(printf '%s\n' "$NEWER" | sed 's/^/  /')" >&2
        echo "Ask the developer to re-export. (--allow-stale measures anyway and the report must say so.)" >&2
        exit 4
    fi
fi

OUT_DIR="$ROOT/design/levels/audit-data"
mkdir -p "$OUT_DIR"
OUT="$OUT_DIR/audit-$(date +%Y%m%d-%H%M%S).json"

if ! dotnet run -c Release --project "$ROOT/$RUNNER_PROJECT" -- \
        --levels-dir "$ROOT/$LEVELS_DIR" --config "$ROOT/$CONFIG_JSON" \
        --levels "$LEVELS" --bot "$BOT" --attempts "$ATTEMPTS" --seed "$SEED" --misclick "$MISCLICK" \
        --out "$OUT" >/dev/null 2>"$OUT.err"; then
    echo "RUNNER FAILED (nothing was changed). Last lines:" >&2
    tail -5 "$OUT.err" >&2
    rm -f "$OUT" "$OUT.err"
    exit 5
fi
rm -f "$OUT.err"

if ! grep -q '"schemaVersion"' "$OUT" 2>/dev/null; then
    echo "Runner output is not a valid audit file (no schemaVersion): $OUT" >&2
    exit 5
fi

echo "OK $OUT"
echo "levels measured: $(grep -o '"level"[[:space:]]*:' "$OUT" | wc -l)"
grep -o '"matchesPerSecond"[[:space:]]*:[[:space:]]*[0-9.]*' "$OUT" | head -1
[ "$STALE_OK" = 1 ] && echo "WARNING: measured with --allow-stale; say so in the report."
exit 0
