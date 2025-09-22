#!/usr/bin/env python3
"""Block compiled binaries from being accidentally staged."""

import re
import subprocess
import sys


def main() -> int:
    """Check for compiled binaries in staged files."""
    try:
        out = subprocess.check_output(["git", "diff", "--cached", "--name-only"], text=True)
        pat = re.compile(r".*\.(dll|exe|pdb|so|dylib)$", re.IGNORECASE)
        bad = [line for line in out.splitlines() if pat.match(line)]
        if bad:
            print(
                "Compiled binaries are blocked. Use Packages or artifacts store.", file=sys.stderr
            )
            return 1
        return 0
    except subprocess.CalledProcessError:
        return 1


if __name__ == "__main__":
    sys.exit(main())
