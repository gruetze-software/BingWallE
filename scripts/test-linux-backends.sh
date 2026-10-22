#!/usr/bin/env bash
# Real native window creation using a disposable headless compositor and X server.
set -euo pipefail
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
LOGS="$ROOT/artifacts/linux-backends"
mkdir -p "$LOGS"
WORK="$(mktemp -d)"
WESTON_PID=
cleanup() {
  if [[ -n "$WESTON_PID" ]]; then kill "$WESTON_PID" 2>/dev/null || true; wait "$WESTON_PID" 2>/dev/null || true; fi
  rm -rf -- "$WORK"
}
trap cleanup EXIT
export XDG_RUNTIME_DIR="$WORK/runtime"
mkdir -m 700 "$XDG_RUNTIME_DIR"
export LIBGL_ALWAYS_SOFTWARE=1
# These variables are scoped to this test process and its children.
export XDG_CONFIG_HOME="$WORK/config" XDG_DATA_HOME="$WORK/data" XDG_CACHE_HOME="$WORK/cache"
dotnet build tools/DesktopSmoke/DesktopSmoke.csproj -c Release --warnaserror
SMOKE="$ROOT/tools/DesktopSmoke/bin/Release/net10.0/DesktopSmoke.dll"
weston --backend=headless-backend.so --use-gl --width=1280 --height=960   --socket=bingwalle-test --idle-time=0 --no-config >"$LOGS/weston.log" 2>&1 &
WESTON_PID=$!
for attempt in {1..100}; do
  if [[ -S "$XDG_RUNTIME_DIR/bingwalle-test" ]]; then break; fi
  if ! kill -0 "$WESTON_PID" 2>/dev/null; then cat "$LOGS/weston.log"; exit 1; fi
  sleep 0.1
done
test -S "$XDG_RUNTIME_DIR/bingwalle-test"
run_check() {
  local name="$1"; shift
  if ! timeout 30s "$@" >"$LOGS/$name.log" 2>&1; then cat "$LOGS/$name.log"; return 1; fi
  grep -F 'SMOKE PASS:' "$LOGS/$name.log"
}
run_check wayland-auto env -u DISPLAY BINGWALLE_LINUX_BACKEND=auto WAYLAND_DISPLAY=bingwalle-test dotnet "$SMOKE" Wayland
run_check wayland-forced env -u DISPLAY BINGWALLE_LINUX_BACKEND=wayland WAYLAND_DISPLAY=bingwalle-test dotnet "$SMOKE" Wayland
run_check x11-forced xvfb-run -a env BINGWALLE_LINUX_BACKEND=x11 WAYLAND_DISPLAY=bingwalle-test dotnet "$SMOKE" X11
run_check x11-fallback xvfb-run -a env BINGWALLE_LINUX_BACKEND=auto WAYLAND_DISPLAY=bingwalle-missing dotnet "$SMOKE" X11
