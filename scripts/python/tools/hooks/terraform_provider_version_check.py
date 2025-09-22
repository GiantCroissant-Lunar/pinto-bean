#!/usr/bin/env python3
"""Pre-commit hook: ensure every provider in required_providers has a version constraint.

Scans *.tf files (limited set) and flags any block like:
  required_providers {
    aws = { source = "hashicorp/aws" }
  }
Where the nested map lacks a 'version'.

Limitations: naive HCL parsing via regex; sufficient for gating obvious omissions.
"""
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[4]
TERRAFORM_DIR = ROOT / "infra" / "terraform"

RE_REQUIRED_PROVIDERS_START = re.compile(r"^\s*required_providers\s*{\s*$")
RE_PROVIDER_ENTRY = re.compile(r"^\s*([A-Za-z0-9_]+)\s*=\s*{(.*)$")
RE_VERSION = re.compile(r"version\s*=")

def scan_file(path: pathlib.Path, problems: list[str]) -> None:
    try:
        lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
    except OSError:
        return
    in_block = False
    current_provider = None
    provider_has_version = False
    for line in lines:
        if not in_block:
            if RE_REQUIRED_PROVIDERS_START.match(line):
                in_block = True
            continue
        # Inside required_providers block
        if line.strip().startswith("}"):
            in_block = False
            current_provider = None
            provider_has_version = False
            continue
        m = RE_PROVIDER_ENTRY.match(line)
        if m:
            # finalize previous provider
            if current_provider and not provider_has_version:
                problems.append(f"{path}: provider '{current_provider}' missing version constraint")
            current_provider = m.group(1)
            provider_has_version = RE_VERSION.search(line) is not None
            continue
        if current_provider and RE_VERSION.search(line):
            provider_has_version = True
        if current_provider and line.strip().startswith('}'):  # end of provider inline map
            if not provider_has_version:
                problems.append(f"{path}: provider '{current_provider}' missing version constraint")
            current_provider = None
            provider_has_version = False

    # End of file: finalize
    if current_provider and not provider_has_version:
        problems.append(f"{path}: provider '{current_provider}' missing version constraint")


def main(args: list[str]) -> int:
    problems: list[str] = []
    # If filenames passed by pre-commit, filter to .tf; else scan terraform dir
    tf_files = [pathlib.Path(a) for a in args if a.endswith('.tf')] if args else list(TERRAFORM_DIR.rglob('*.tf'))
    for f in tf_files:
        scan_file(f, problems)
    if problems:
        print("Terraform provider version check FAILED:")
        for p in problems:
            print(f" - {p}")
        return 1
    return 0

if __name__ == "__main__":  # pragma: no cover (simple CLI)
    sys.exit(main(sys.argv[1:]))
