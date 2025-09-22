#!/usr/bin/env python3
"""Fail if plaintext secrets are present or sensitive keys unencrypted.
Rules:
 1. In infra/terraform/secrets and infra/terraform/github/secrets no *.json except *.json.encrypted
 2. Block age private key being staged outside ignore.
 3. Simple entropy / pattern scan on tracked files (optional lightweight) unless excluded.
Exit non-zero on violation; print reasons.
"""

from __future__ import annotations

import datetime
import json
import math
import os
import pathlib
import re
import shutil
import subprocess
import sys

REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
SECRET_DIRS = [
    REPO_ROOT / "infra" / "terraform" / "secrets",
    REPO_ROOT / "infra" / "terraform" / "github" / "secrets",
]
ALLOWED_JSON_SUFFIX = ".json.encrypted"
PLAIN_JSON_BLOCKLIST = re.compile(r".+\.json$")  # not currently used but kept for future refinement
SENSITIVE_KEY_PATTERN = re.compile(r"(API|SECRET|TOKEN|KEY|PASSWORD|PASS)$", re.IGNORECASE)
HIGH_ENTROPY_THRESHOLD = 4.0  # bits/char approximate
MIN_SECRET_LENGTH = 20
DETECT_BASELINE = REPO_ROOT / ".secrets.baseline"  # retained for optional legacy full scan mode

violations: list[str] = []


def sh(cmd: list[str]) -> str:
    # Trusted internal commands (git). noqa for security lint.
    return subprocess.check_output(cmd, text=True, cwd=REPO_ROOT).strip()  # noqa: S603


def calc_entropy(s: str) -> float:
    from collections import Counter

    if not s:
        return 0.0
    counts = Counter(s)
    n = len(s)
    return -sum((c / n) * math.log2(c / n) for c in counts.values())


WHITELIST_PLAINTEXT_TEMPLATES = {
    "infra/terraform/secrets/terraform.json",
    "infra/terraform/github/secrets/github-vars.json",
}


def scan_plain_json() -> None:
    for d in SECRET_DIRS:
        if not d.exists():
            continue
        for p in d.glob("*.json"):
            if str(p).endswith(ALLOWED_JSON_SUFFIX):
                continue
            rel = str(p.relative_to(REPO_ROOT)).replace("\\", "/")
            if rel in WHITELIST_PLAINTEXT_TEMPLATES:
                # treat as safe template; no further content inspection required
                continue
            violations.append(f"Plain JSON secret present: {rel}")


def scan_git_index() -> None:
    # list staged or committed files (tracked) to evaluate suspicious high entropy strings
    try:
        tracked = sh(["git", "ls-files"]).splitlines()
    except subprocess.CalledProcessError:  # specific failure retrieving git files
        return
    candidate_ext = {"tf", "ps1", "py", "yml", "yaml", "json", "txt", "md"}
    secret_like = re.compile(r"([A-Za-z0-9+/=_-]{20,})")
    path_fragment = re.compile(r"[/\\]")
    for rel in tracked:
        p = REPO_ROOT / rel
        if not p.is_file():
            continue
        if p.name.endswith(ALLOWED_JSON_SUFFIX):
            # Skip known config where age public key appears
            if rel.endswith(".sops.yaml"):
                continue
            continue
        # Skip entropy scanning in test modules entirely
        if rel.startswith("scripts/python/tests/"):
            continue
        if p.suffix.lstrip(".") not in candidate_ext:
            continue
        try:
            text = p.read_text(errors="ignore")
        except OSError:  # file read or encoding issue
            continue
        for match in secret_like.findall(text):
            if len(match) < MIN_SECRET_LENGTH:
                continue
            ent = calc_entropy(match)
            if (
                ent >= HIGH_ENTROPY_THRESHOLD
                and not rel.startswith("infra/terraform/secrets/")
                and not rel.startswith("infra/terraform/github/secrets/")
            ):
                # Skip if token looks like a path or terraform directory fragment (common false positives)
                if path_fragment.search(match):
                    continue
                # Skip explicit validator related env var examples
                if match.startswith("VALIDATOR_"):
                    continue
                # age public key recipient (harmless and expected)
                if match.startswith("age1"):
                    continue
                # Skip script/tool names that look entropy-ish
                if (
                    match.startswith("BulkApply")
                    or match.startswith("ApplySecrets")
                    or match.startswith("GetTfc")
                    or match.startswith("QueueTfc")
                    or match.startswith("SetTfc")
                ):
                    continue
                violations.append(
                    f"Potential secret (high entropy) in {rel}: '{match[:8]}...' entropy={ent:.2f}"
                )


def check_age_key() -> None:
    for d in SECRET_DIRS:
        k = d / "age.key"
        if k.exists():
            try:
                content_first = k.read_text().splitlines()[0]
            except Exception:
                content_first = ""
            if "SECRET-KEY" in content_first:
                # ensure it's ignored by git
                try:
                    status = sh(
                        ["git", "ls-files", "--error-unmatch", str(k.relative_to(REPO_ROOT))]
                    )
                    if status:
                        violations.append(f"age.key appears tracked: {k.relative_to(REPO_ROOT)}")
                except subprocess.CalledProcessError:
                    pass


def legacy_run_detect_secrets() -> None:
    """Optional legacy execution if LEGACY_FULL_SCAN set.
    Intentionally not run by default since pre-commit handles detect-secrets now.
    """
    if not shutil.which("detect-secrets"):
        return
    if not DETECT_BASELINE.exists():
        violations.append("detect-secrets baseline missing: .secrets.baseline")
        return
    try:
        hook = shutil.which("detect-secrets-hook") or "detect-secrets-hook"
        hook_rc = subprocess.call(  # noqa: S603 (resolved executable; args static)
            [hook, "--baseline", str(DETECT_BASELINE)], cwd=REPO_ROOT
        )
        if hook_rc != 0:
            violations.append(
                "detect-secrets found new potential secrets (update or audit baseline)"
            )
    except (OSError, subprocess.CalledProcessError) as e:
        violations.append(f"detect-secrets execution error: {e}")


def legacy_run_gitleaks() -> None:
    """Optional legacy execution if LEGACY_FULL_SCAN set. Pre-commit owns gitleaks now."""
    if not shutil.which("gitleaks"):
        return
    import json as _json
    import tempfile
    import time

    for attempt in range(2):
        report_path = None
        try:
            with tempfile.NamedTemporaryFile(
                prefix=f"gitleaks_{os.getpid()}_", suffix=".json", delete=False
            ) as tf:
                report_path = tf.name
            cmd = [
                "gitleaks",
                "detect",
                "--no-git",
                "--report-format",
                "json",
                "--report-path",
                report_path,
            ]
            if (REPO_ROOT / ".gitleaks.toml").exists():
                cmd[3:3] = ["--config", ".gitleaks.toml"]
            rc = subprocess.call(cmd, cwd=REPO_ROOT)  # noqa: S603
            if rc != 0:
                try:
                    with open(report_path, encoding="utf-8", errors="ignore") as fh:
                        data = _json.load(fh)
                    for finding in data:
                        desc = finding.get("Description", "?")
                        file = finding.get("File", "?")
                        violations.append(f"gitleaks: {file}: {desc}")
                except Exception:
                    violations.append("gitleaks reported issues (could not parse report)")
            break
        except Exception as e:
            if attempt == 0:
                time.sleep(0.5)
                continue
            violations.append(f"gitleaks execution error: {e}")
        finally:
            if report_path and os.path.exists(report_path):
                try:
                    os.remove(report_path)
                except OSError:
                    pass


def emit_reports(violations: list[str]) -> None:
    sarif_path = os.getenv("VALIDATOR_SARIF")
    json_path = os.getenv("VALIDATOR_JSON")
    if not (sarif_path or json_path):
        return
    timestamp = datetime.datetime.utcnow().isoformat() + "Z"
    if json_path:
        try:
            with open(json_path, "w", encoding="utf-8") as jf:
                json.dump({"timestamp": timestamp, "violations": violations}, jf, indent=2)
        except Exception as e:
            print(f"Could not write JSON report: {e}", file=sys.stderr)
    if sarif_path:
        sarif = {
            "$schema": "https://schemastore.azurewebsites.net/schemas/json/sarif-2.1.0.json",
            "version": "2.1.0",
            "runs": [
                {
                    "tool": {"driver": {"name": "pinto-bean-secret-validator", "version": "1.0.0"}},
                    "results": [
                        {
                            "ruleId": "SECRET-CHECK",
                            "level": "error",
                            "message": {"text": v},
                            "locations": [],
                        }
                        for v in violations
                    ],
                }
            ],
        }
        try:
            with open(sarif_path, "w", encoding="utf-8") as sf:
                json.dump(sarif, sf, indent=2)
        except Exception as e:
            print(f"Could not write SARIF report: {e}", file=sys.stderr)


if __name__ == "__main__":
    scan_plain_json()
    check_age_key()
    scan_git_index()
    if os.getenv("LEGACY_FULL_SCAN"):
        legacy_run_detect_secrets()
        legacy_run_gitleaks()
    emit_reports(violations)
    if violations:
        print("Secret validation FAILED:\n" + "\n".join(f" - {v}" for v in violations))
        sys.exit(1)
    print("Secret validation PASSED")
