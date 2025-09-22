#!/usr/bin/env python3
import os
import subprocess
import sys

res = subprocess.run(["git", "diff", "--cached", "--name-only"], capture_output=True, text=True)
paths = [p.strip() for p in res.stdout.splitlines() if p.strip()]
missing = []
for p in paths:
    if p.endswith(".meta"):
        continue
    if not os.path.exists(p):
        continue
    meta = p + ".meta"
    meta_exists = os.path.exists(meta)
    if not meta_exists:
        rc = subprocess.run(
            ["git", "ls-files", "--error-unmatch", meta],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
        meta_exists = rc.returncode == 0
    if not meta_exists:
        missing.append(p)

if missing:
    print("❌ Missing .meta files for:")
    for m in missing:
        print(f" - {m} (expected: {m}.meta)")
    print("\nTip: Reimport in Unity so .meta files regenerate, then stage both.")
    sys.exit(1)
