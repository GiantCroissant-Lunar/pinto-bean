#!/usr/bin/env python3
"""Block Unity generated folders from being committed."""

import subprocess
import sys


def main() -> int:
    """Check for Unity generated folders in staged files."""
    try:
        out = subprocess.check_output(["git", "diff", "--cached", "--name-only"], text=True)
        bad = [
            line
            for line in out.splitlines()
            if line.startswith(("Library/", "Temp/", "Obj/", "Logs/", "Build/"))
        ]
        if bad:
            print("Do not commit Unity generated folders.", file=sys.stderr)
            return 1
        return 0
    except subprocess.CalledProcessError:
        return 1


if __name__ == "__main__":
    sys.exit(main())
