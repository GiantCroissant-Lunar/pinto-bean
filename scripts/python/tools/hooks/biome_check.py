#!/usr/bin/env python3
"""Run Biome via pnpm for the staged files."""

from __future__ import annotations

import pathlib
import shutil
import subprocess
import sys
from collections.abc import Iterable

ROOT = pathlib.Path(__file__).resolve().parents[4]
WORKSPACE = ROOT / "scripts" / "nodejs"

PNPM_NAMES: tuple[str, ...] = ("pnpm", "pnpm.cmd", "pnpm.exe")
SUPPORTED_SUFFIXES = {".js", ".cjs", ".mjs", ".ts", ".cts", ".mts", ".jsx", ".tsx", ".json"}


def find_pnpm() -> pathlib.Path | None:
    for candidate in PNPM_NAMES:
        path = shutil.which(candidate)
        if path:
            return pathlib.Path(path)
    return None


def normalize_targets(args: Iterable[str]) -> list[pathlib.Path]:
    raw_paths = [ROOT / pathlib.Path(arg) for arg in args]
    targets: list[pathlib.Path] = []
    for candidate in raw_paths:
        if not candidate.exists() or candidate.suffix.lower() not in SUPPORTED_SUFFIXES:
            continue
        try:
            candidate.relative_to(WORKSPACE)
        except ValueError:
            continue
        targets.append(candidate.resolve())
    return targets


def _to_workspace_path(path: pathlib.Path) -> str:
    try:
        return path.relative_to(WORKSPACE).as_posix()
    except ValueError:
        return path.relative_to(ROOT).as_posix()


def main() -> int:
    pnpm_path = find_pnpm()
    if pnpm_path is None:
        print("pnpm not found; skipping Biome check.")
        return 0

    targets = normalize_targets(sys.argv[1:])
    if not targets:
        return 0

    command = [
        str(pnpm_path),
        "--dir",
        str(WORKSPACE),
        "exec",
        "biome",
        "check",
        "--write",
    ]
    command.extend(_to_workspace_path(p) for p in targets)
    try:
        result = subprocess.run(command, cwd=str(ROOT), check=False)
    except OSError as exc:
        print(f"Failed to launch Biome: {exc}")
        return 1
    return result.returncode


if __name__ == "__main__":
    sys.exit(main())
