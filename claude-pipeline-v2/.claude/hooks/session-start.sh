#!/bin/bash
# SessionStart hook — print session context at the start of every session.
# Runs on Git Bash (Windows), macOS and Linux. Never exits non-zero.

echo "=== Session context ==="

BRANCH=$(git rev-parse --abbrev-ref HEAD 2>/dev/null)
if [ -n "$BRANCH" ]; then
    echo "Branch: $BRANCH"
    if [ "$BRANCH" = "main" ] || [ "$BRANCH" = "master" ]; then
        echo "!! ON THE DEFAULT BRANCH — do not commit directly here."
    fi
    echo "Recent commits:"
    git log --oneline -5 2>/dev/null | sed 's/^/  /'
fi

# The safety guard needs a real Python; without one it lets everything through. Say so once.
if ! bash "$(dirname "${BASH_SOURCE[0]}")/guard.sh" --probe; then
    echo "!! No working Python (python3 / python / py) found - unity_guard is INACTIVE:"
    echo "   .meta / .prefab / .unity edits and destructive commands are NOT blocked this session."
fi

# New assets missing their .meta. Only Assets/ and Packages/ carry .meta files; cut -c4-
# keeps paths with spaces whole (awk '{print $2}' split them).
MISSING=$(git status --porcelain --untracked-files=all 2>/dev/null \
    | grep -E '^(\?\?|A ) ' | cut -c4- | sed 's/^"//; s/"$//' \
    | grep -E '^(Assets|Packages)/' | grep -vE '\.meta$|/$' \
    | while IFS= read -r f; do [ -e "${f}.meta" ] || echo "$f"; done | head -10)
if [ -n "$MISSING" ]; then
    echo ""
    echo "!! New assets with NO .meta:"
    echo "$MISSING" | sed 's/^/  /'
fi

# Previous session state
STATE="docs/_session/active.md"
if [ -f "$STATE" ]; then
    echo ""
    echo "=== PREVIOUS SESSION STATE FOUND ==="
    echo "Read this file to resume: $STATE"
    head -15 "$STATE" 2>/dev/null | sed 's/^/  /'
fi

echo "======================"
exit 0
