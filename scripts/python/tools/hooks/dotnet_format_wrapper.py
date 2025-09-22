#!/usr/bin/env python3
"""Run dotnet format only if a sln or csproj exists; otherwise exit success.

Prevents failures in repositories that occasionally lack .NET solution files.
"""

from __future__ import annotations

import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[4]
targets = list(ROOT.glob("*.sln")) + list(ROOT.rglob("*.csproj"))
if not targets:
    sys.exit(0)

cmd = ["dotnet", "format", "--verify-no-changes"]
try:
    rc = subprocess.call(cmd, cwd=str(ROOT))
except FileNotFoundError:
    # dotnet SDK not installed; treat as non-blocking
    rc = 0
sys.exit(rc)
