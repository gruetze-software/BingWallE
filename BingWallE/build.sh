#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
# Compatibility entry point; platform scripts live in scripts/.
case "$(uname -s)" in
  Darwin) exec bash "$ROOT/scripts/build-macos.sh" "${1:-arm64}" ;;
  Linux) exec bash "$ROOT/scripts/build-linux.sh" "${1:-x64}" ;;
  *) echo "On Windows, run: pwsh -File scripts/build-windows.ps1" >&2; exit 1 ;;
esac
