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

# Unity CLI bridge readiness on THIS machine. The project's rules and com.unity.pipeline come
# with git; the CLI binary and the MCP registration do not — a teammate who pulled a project
# someone else set up has neither. Prints only when something is missing.
if [ -f Packages/manifest.json ] && grep -q '"com.unity.pipeline"' Packages/manifest.json; then
    if ! command -v unity >/dev/null 2>&1         && [ ! -x "${LOCALAPPDATA:-/nonexistent}/Unity/bin/unity.exe" ]         && [ ! -x "$HOME/.local/bin/unity" ]; then
        echo ""
        echo "!! Unity CLI not installed on this machine — the Editor bridge is unavailable"
        echo "   (read-only console/status commands too). The project already uses it:"
        echo "   suggest /init-unity-cli to the developer; until then ask for pasted logs."
    fi
elif [ -f Packages/manifest.json ] && grep -q '/init-unity-cli' CLAUDE.md 2>/dev/null; then
    echo ""
    echo "!! CLAUDE.md expects the Unity CLI bridge but com.unity.pipeline is not in"
    echo "   Packages/manifest.json — suggest /init-unity-cli to the developer."
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
