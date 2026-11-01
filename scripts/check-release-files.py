#!/usr/bin/env python3
"""Check app payloads and ZIP/tar archives for packaging-only artifacts."""
import argparse
from pathlib import Path, PurePosixPath
import sys
import tarfile
import zipfile


def unwanted(name, platform):
    path = PurePosixPath(name.replace("\\", "/"))
    parts = [part.lower() for part in path.parts]
    leaf = path.name.lower()
    if path.suffix.lower() in {".pdb", ".dbg", ".debug", ".dwarf", ".mdb", ".lib", ".exp", ".a"}:
        return "debug symbols or link-time files"
    if any(part.endswith(".dsym") for part in parts):
        return "Apple debug symbols"
    if "runtimes" in parts:
        return "unfiltered platform assets"
    if leaf.endswith((".aar", ".xcframework.zip")):
        return "mobile platform assets"
    if platform != "macos" and leaf.endswith(".dylib"):
        return "macOS native library"
    if platform != "linux" and (leaf.endswith(".so") or ".so." in leaf):
        return "Linux native library"
    # Managed .NET DLLs are required on every platform; only reject known native
    # Windows libraries and Windows executables on Unix targets.
    if platform != "windows" and (leaf.endswith(".exe") or
            (leaf.endswith(".dll") and leaf.startswith(("libskia", "libharfbuzz", "onnxruntime", "av_lib")))):
        return "Windows native binary"
    return None


def entries(source):
    if source.is_dir():
        return [str(p.relative_to(source)) for p in source.rglob("*") if p.is_file()]
    if zipfile.is_zipfile(source):
        with zipfile.ZipFile(source) as archive:
            return [item.filename for item in archive.infolist() if not item.is_dir()]
    if tarfile.is_tarfile(source):
        with tarfile.open(source) as archive:
            return [item.name for item in archive.getmembers() if item.isfile()]
    raise ValueError(f"Unsupported payload: {source}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("platform", choices=("windows", "linux", "macos"))
    parser.add_argument("payload", nargs="+", type=Path)
    args = parser.parse_args()
    failed = False
    for source in args.payload:
        names = entries(source)
        if not names:
            raise ValueError(f"Empty payload: {source}")
        problems = [(name, reason) for name in names if (reason := unwanted(name, args.platform))]
        for name, reason in problems:
            print(f"{source}: {reason}: {name}", file=sys.stderr)
        failed |= bool(problems)
        if not problems:
            print(f"Packaging check passed: {source} ({len(names)} files)")
    return int(failed)


if __name__ == "__main__":
    sys.exit(main())
