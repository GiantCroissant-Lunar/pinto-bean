#!/usr/bin/env python3
"""Invoke JetBrains cleanupcode if available for changed files."""

from __future__ import annotations

import pathlib
import shutil
import subprocess
import sys
from collections.abc import Iterable

ROOT = pathlib.Path(__file__).resolve().parents[4]
LOCAL_TOOL_DIR = ROOT / ".dotnet-tools"
CLI_CANDIDATES: tuple[tuple[str, tuple[str, ...]], ...] = (
    ("jb", ("cleanupcode",)),
    ("jb.exe", ("cleanupcode",)),
    ("cleanupcode", ()),
    ("cleanupcode.exe", ()),
)
SUPPORTED_SUFFIXES = {".cs", ".csproj", ".sln"}


def find_cli() -> list[str] | None:
    for name, extra in CLI_CANDIDATES:
        exe = shutil.which(name)
        if exe:
            return [exe, *extra]
        local_candidate = (LOCAL_TOOL_DIR / name).resolve()
        if local_candidate.exists():
            return [str(local_candidate), *extra]
    return None


def nearest_solution(path: pathlib.Path) -> pathlib.Path | None:
    current = path.parent
    while ROOT in current.parents or current == ROOT:
        slns = sorted(current.glob("*.sln"))
        if slns:
            return slns[0]
        if current == ROOT:
            break
        current = current.parent
    return None


def normalize_targets(args: Iterable[str]) -> list[pathlib.Path]:
    candidates = [ROOT / pathlib.Path(arg) for arg in args]
    return [p.resolve() for p in candidates if p.suffix.lower() in SUPPORTED_SUFFIXES]


cli = find_cli()
if cli is None:
    print("ReSharper CLI (cleanupcode) not found; skipping.")
    sys.exit(0)

filtered = normalize_targets(sys.argv[1:])
if not filtered:
    sys.exit(0)

grouped: dict[pathlib.Path, set[pathlib.Path]] = {}
for file_path in filtered:
    solution = nearest_solution(file_path)
    if solution is None:
        continue
    grouped.setdefault(solution, set()).add(file_path)

if not grouped:
    sys.exit(0)

exit_code = 0
for solution, files in grouped.items():
    args = cli.copy()
    args.append(str(solution))
    for file_path in sorted(files):
        rel = file_path.relative_to(ROOT)
        args.append(f"--include={rel.as_posix()}")
    try:
        completed = subprocess.run(args, cwd=str(ROOT), check=False)
    except OSError as exc:
        print(f"Failed to run {' '.join(args)}: {exc}")
        exit_code = exit_code or 1
        continue
    if completed.returncode != 0:
        exit_code = exit_code or completed.returncode

sys.exit(exit_code)
