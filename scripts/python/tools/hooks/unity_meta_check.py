#!/usr/bin/env python3
import os
import re
import subprocess
import sys

# Only check Unity asset-like files. When invoked by pre-commit with filenames,
# use them directly; otherwise, fall back to staged files.
UNITY_EXT_RE = re.compile(
    r"\.(cs|asmdef|unity|prefab|mat|asset|controller|anim|shader(?:Graph)?|guiskin|ttf|otf|wav|mp3|ogg|png|jpg|jpeg|tga|psd|fbx|obj|hdr|exr|cubemap)$",
    re.IGNORECASE,
)


ALLOWED_ROOT_HINTS = (
    os.path.join("projects", "").replace("\\", "/"),  # any under projects/
)


def _is_under_unity_root(path: str) -> bool:
    norm = path.replace("\\", "/")
    # Heuristic: limit to items under projects/ or any path containing /Assets/
    if "/Assets/" in norm:
        return True
    return any(norm.startswith(prefix) for prefix in ALLOWED_ROOT_HINTS)


def get_candidate_paths() -> list[str]:
    args = [a for a in sys.argv[1:] if a and a != "-"]
    if args:
        return [
            p
            for p in args
            if UNITY_EXT_RE.search(p) and not p.endswith(".meta") and _is_under_unity_root(p)
        ]
    # Fallback: get staged file list and filter by Unity extensions.
    res = subprocess.run(["git", "diff", "--cached", "--name-only"], capture_output=True, text=True)
    paths = [p.strip() for p in res.stdout.splitlines() if p.strip()]
    return [
        p
        for p in paths
        if UNITY_EXT_RE.search(p) and not p.endswith(".meta") and _is_under_unity_root(p)
    ]


def main() -> int:
    missing: list[str] = []
    for p in get_candidate_paths():
        if not os.path.exists(p):
            # If the file isn't in the working tree (deleted/renamed), skip
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
        # Avoid Unicode emoji to prevent encoding issues on some Windows consoles.
        print("[ERROR] Missing .meta files for:")
        for m in missing:
            print(f" - {m} (expected: {m}.meta)")
        print("\nTip: Reimport in Unity so .meta files regenerate, then stage both.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
