#!/usr/bin/env python3
import subprocess
import sys


def staged_contains(path: str) -> bool:
    r = subprocess.run(
        ["git", "diff", "--cached", "--name-only", "--", path], capture_output=True, text=True
    )
    return any(p.strip() == path for p in r.stdout.splitlines())


manifest_changed = staged_contains("Packages/manifest.json")
lock_changed = staged_contains("Packages/packages-lock.json")

if manifest_changed and not lock_changed:
    print("❌ Packages/manifest.json changed but Packages/packages-lock.json not updated.")
    print("Run Unity once or refresh Package Manager to update the lockfile, then stage it.")
    sys.exit(1)
