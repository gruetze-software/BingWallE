#!/usr/bin/env bash
# Real native window creation using a disposable headless compositor and X server.
set -euo pipefail
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
for desktop_tool in dotnet weston wayland-info xvfb-run timeout; do
  if ! command -v "$desktop_tool" >/dev/null; then
    echo "Missing desktop test dependency: $desktop_tool" >&2
    exit 1
  fi
done
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
ready=false
deadline=$((SECONDS + 60))
while (( SECONDS < deadline )); do
  if ! kill -0 "$WESTON_PID" 2>/dev/null; then cat "$LOGS/weston.log"; exit 1; fi
  # A cold software renderer can need more than 15 seconds on hosted runners.
  # Wait for an actual connection and the window protocols, not just the socket.
  if [[ -S "$XDG_RUNTIME_DIR/bingwalle-test" ]] \
    && WAYLAND_DISPLAY=bingwalle-test timeout 2s wayland-info >"$LOGS/wayland-globals.log" 2>&1 \
    && grep -Fq wl_compositor "$LOGS/wayland-globals.log" \
    && grep -Fq xdg_wm_base "$LOGS/wayland-globals.log"; then
    ready=true
    break
  fi
  sleep 0.2
done
if [[ "$ready" != true ]]; then
  echo 'Weston did not become ready within 60 seconds.' >&2
  cat "$LOGS/weston.log"
  if [[ -f "$LOGS/wayland-globals.log" ]]; then cat "$LOGS/wayland-globals.log"; fi
  exit 1
fi
run_check() {
  local name="$1"; shift
  if ! timeout 30s "$@" >"$LOGS/$name.log" 2>&1; then cat "$LOGS/$name.log"; return 1; fi
  grep -F 'SMOKE PASS:' "$LOGS/$name.log"
}
run_check wayland-forced env -u DISPLAY BINGWALLE_LINUX_BACKEND=wayland WAYLAND_DISPLAY=bingwalle-test dotnet "$SMOKE" Wayland
run_check wayland-auto env -u DISPLAY BINGWALLE_LINUX_BACKEND=auto WAYLAND_DISPLAY=bingwalle-test dotnet "$SMOKE" Wayland
run_check x11-forced xvfb-run -a env BINGWALLE_LINUX_BACKEND=x11 WAYLAND_DISPLAY=bingwalle-test dotnet "$SMOKE" X11
run_check x11-fallback xvfb-run -a env BINGWALLE_LINUX_BACKEND=auto WAYLAND_DISPLAY=bingwalle-missing dotnet "$SMOKE" X11
