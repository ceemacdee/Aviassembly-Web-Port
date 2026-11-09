#!/usr/bin/env bash
# Aviassembly - build the WebGL version (first run only) and play it on http://localhost:8000
# Needs Unity 6000.0.69f1 with WebGL Build Support (via Unity Hub, signed in) and python3.
# Override the Unity location with UNITY_PATH=/path/to/Unity. Pass "rebuild" to force a new build.
set -euo pipefail
cd "$(dirname "$0")"
PROJECT="$(pwd)"
BUILD="$PROJECT/Builds/WebGL"
PORT="${PORT:-8000}"

if [[ ! -f "$BUILD/index.html" || "${1:-}" == "rebuild" ]]; then
  if [[ -z "${UNITY_PATH:-}" ]]; then
    for candidate in \
      "/Applications/Unity/Hub/Editor/6000.0.69f1/Unity.app/Contents/MacOS/Unity" \
      "$HOME/Unity/Hub/Editor/6000.0.69f1/Editor/Unity"; do
      [[ -x "$candidate" ]] && UNITY_PATH="$candidate" && break
    done
  fi
  if [[ -z "${UNITY_PATH:-}" || ! -x "$UNITY_PATH" ]]; then
    echo "Could not find Unity 6000.0.69f1. Install it with WebGL Build Support from Unity Hub, or set UNITY_PATH."
    exit 1
  fi
  echo "Building the WebGL version (first build can take 15-40 minutes, log: build.log)..."
  "$UNITY_PATH" -batchmode -quit -buildTarget WebGL -projectPath "$PROJECT" \
    -executeMethod AviassemblyWebGLBuild.BuildFromCommandLine -buildPath Builds/WebGL -logFile "$PROJECT/build.log" || true
  if [[ ! -f "$BUILD/index.html" ]]; then
    echo "The build failed. Look for 'error' in build.log."
    exit 1
  fi
fi

echo "Playing at http://localhost:$PORT/  (Ctrl+C to stop)"
( sleep 1; (command -v open >/dev/null && open "http://localhost:$PORT/") || (command -v xdg-open >/dev/null && xdg-open "http://localhost:$PORT/") || true ) &
cd "$BUILD"
exec python3 -m http.server "$PORT" --bind 127.0.0.1
