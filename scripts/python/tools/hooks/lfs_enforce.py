#!/usr/bin/env python3
import pathlib
import subprocess
import sys

LFS_REQUIRED_EXTS = {
    ".psd",
    ".fbx",
    ".mp4",
    ".mov",
    ".avi",
    ".wav",
    ".aiff",
    ".aif",
    ".flac",
    ".png",
    ".jpg",
    ".jpeg",
    ".tga",
    ".exr",
    ".hdr",
    ".zip",
    ".7z",
    ".rar",
}


def is_lfs_tracked(path: str) -> bool:
    r = subprocess.run(["git", "check-attr", "filter", path], capture_output=True, text=True)
    return "lfs" in r.stdout.lower()


res = subprocess.run(["git", "diff", "--cached", "--name-only"], capture_output=True, text=True)
paths = [p.strip() for p in res.stdout.splitlines() if p.strip()]
violations = []
for p in paths:
    ext = pathlib.Path(p).suffix.lower()
    if ext in LFS_REQUIRED_EXTS and not is_lfs_tracked(p):
        violations.append(p)

if violations:
    print("❌ Files should be tracked by Git LFS but are not:")
    for v in violations:
        print(f" - {v}")
    print(
        "\nFix:\n 1) Add pattern to .gitattributes (e.g. *.psd filter=lfs diff=lfs merge=lfs -text)\n 2) git add .gitattributes\n 3) git add --renormalize <files>"
    )
    sys.exit(1)
