#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
command -v snapcraft >/dev/null
bash scripts/prepare-snap.sh
VERSION="$(cat artifacts/snap-payload/opt/bingwalle/VERSION)"
# Default Snapcraft uses an isolated build environment. CI passes --destructive-mode
# only on its disposable Ubuntu 24.04 runner.
snapcraft pack "$@" --output "$ROOT/artifacts/bingwalle_${VERSION}_amd64.snap"
