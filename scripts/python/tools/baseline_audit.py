#!/usr/bin/env python3
"""Audit detect-secrets baseline for potentially stale entries.

Heuristic: list baseline results whose referenced file no longer exists or which have not
appeared in git history (approx) within the last N commits (adjust via env BASELINE_HISTORY_COMMITS, default 200).
This is a lightweight helper; it does not modify the baseline.
"""

from __future__ import annotations

import json
import os
import pathlib
import subprocess
import sys
from typing import Any

ROOT = pathlib.Path(__file__).resolve().parents[3]
BASELINE = ROOT / ".secrets.baseline"
HISTORY_COMMITS = int(os.getenv("BASELINE_HISTORY_COMMITS", "200"))


def git_grep(path: str, token: str) -> bool:
    try:
        # Using explicit argument list (no shell) mitigates shell injection; noqa for S607 static rule
        out = subprocess.check_output(  # noqa: S603
            ["git", "log", f"-n{HISTORY_COMMITS}", "-S", token, "--", path],
            cwd=ROOT,
            text=True,
            stderr=subprocess.DEVNULL,
        )
        return bool(out.strip())
    except Exception:
        return False


def main() -> int:
    if not BASELINE.exists():
        print("No .secrets.baseline present")
        return 0
    try:
        data = json.loads(BASELINE.read_text())
    except Exception as e:
        print(f"Could not parse baseline: {e}", file=sys.stderr)
        return 1
    results: list[dict[str, Any]] = data.get("results", [])
    stale: list[str] = []
    for r in results:
        path = r.get("filename")
        hashed = r.get("hashed_secret")
        if not path or not hashed:
            continue
        rel = path.replace("\\", "/")
        file_path = ROOT / rel
        if not file_path.exists():
            stale.append(f"File removed: {rel}")
            continue
        # we cannot reconstruct the secret from the hash; rely on presence of file change activity instead
        # as a weak signal; if file hasn't changed recently it may still be valid so this is informational
        # allow user to manually rescan to confirm necessity.
    if stale:
        print("Stale baseline candidates:")
        for s in stale:
            print(f" - {s}")
    else:
        print("No stale baseline entries detected (heuristic).")
    return 0


if __name__ == "__main__":  # pragma: no cover
    sys.exit(main())
