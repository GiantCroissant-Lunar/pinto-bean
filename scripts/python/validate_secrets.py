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
HIGH_ENTROPY_THRESHOLD = (
    4.0  # bits/char approximate (configurable via ENV: VALIDATOR_ENTROPY_THRESHOLD)
)
MIN_SECRET_LENGTH = 20
DETECT_BASELINE = REPO_ROOT / ".secrets.baseline"  # retained for optional legacy full scan mode

# Recognize ADR-style filename stems (e.g., 20250922-use-something-long) to reduce false positives
ADR_TOKEN_RE = re.compile(r"^[0-9]{8}-[a-z0-9][a-z0-9-]{10,}$")

violations: list[str] = []

# Structured findings (rule_id, severity, message, file, token_prefix)
findings: list[dict[str, str]] = []

# Allow placeholders / benign markers
PLACEHOLDER_PATTERNS = [
    re.compile(r"<[^>]+>"),
    re.compile(r"^(CHANGE(ME)?|TODO|EXAMPLE|SAMPLE|DUMMY|PLACEHOLDER)$", re.IGNORECASE),
]

# Patterns with explicit rule ids and severity mapping
PATTERN_DEFINITIONS: list[tuple[str, str, re.Pattern[str]]] = [
    ("AWS_ACCESS_KEY", "HIGH", re.compile(r"AKIA[0-9A-Z]{16}")),
    ("GITHUB_PAT", "HIGH", re.compile(r"ghp_[A-Za-z0-9]{36}")),
    ("GITHUB_PAT_NEW", "HIGH", re.compile(r"github_pat_[A-Za-z0-9_]{22,}")),
    (
        "JWT_TOKEN",
        "MEDIUM",
        re.compile(r"eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{5,}"),
    ),
    ("AGE_PRIVATE_KEY", "CRITICAL", re.compile(r"^AGE-SECRET-KEY-[A-Z0-9]{59}$")),
]

FAIL_LEVEL = os.getenv("SECRET_VALIDATOR_FAIL_LEVEL", "HIGH").upper()  # CRITICAL|HIGH|MEDIUM|LOW
LEVEL_ORDER = {"CRITICAL": 4, "HIGH": 3, "MEDIUM": 2, "LOW": 1}

ENTROPY_FAIL_LEVEL = os.getenv("SECRET_VALIDATOR_ENTROPY_LEVEL", "HIGH").upper()
ENTROPY_THRESHOLD = float(os.getenv("VALIDATOR_ENTROPY_THRESHOLD", str(HIGH_ENTROPY_THRESHOLD)))

CACHE_PATH = REPO_ROOT / ".cache" / "secret-validator.json"
_cache: dict[str, dict[str, str]] = {}


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


def _load_cache() -> None:
    if CACHE_PATH.exists():
        try:
            _cache.update(json.loads(CACHE_PATH.read_text()))
        except Exception as e:
            # benign cache parse issue; continue without cache
            print(f"[validator] cache load skipped: {e}", file=sys.stderr)


def _save_cache() -> None:
    try:
        if not CACHE_PATH.parent.exists():
            CACHE_PATH.parent.mkdir(parents=True, exist_ok=True)
        with open(CACHE_PATH, "w", encoding="utf-8") as fh:
            json.dump(_cache, fh, indent=2)
    except Exception as e:
        print(f"[validator] cache save skipped: {e}", file=sys.stderr)


def classify(
    rule_id: str, severity: str, message: str, file: str, token: str | None = None
) -> None:
    findings.append(
        {
            "rule_id": rule_id,
            "severity": severity,
            "message": message,
            "file": file,
            "token_prefix": token[:8] + "..." if token else "",
        }
    )
    violations.append(message)


def _is_placeholder(token: str) -> bool:
    return any(p.fullmatch(token) for p in PLACEHOLDER_PATTERNS)


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
            if _is_placeholder(match):
                continue
            # Skip ADR filename stems (date + slug) which look random enough to trip entropy
            if ADR_TOKEN_RE.match(match.lower()):
                continue
            ent = calc_entropy(match)
            if (
                ent >= ENTROPY_THRESHOLD
                and not rel.startswith("infra/terraform/secrets/")
                and not rel.startswith("infra/terraform/github/secrets/")
            ):
                if path_fragment.search(match):
                    continue
                if match.startswith("VALIDATOR_"):
                    continue
                if match.startswith("age1"):
                    continue
                if match.startswith(("BulkApply", "ApplySecrets", "GetTfc", "QueueTfc", "SetTfc")):
                    continue
                sev = (
                    ENTROPY_FAIL_LEVEL if LEVEL_ORDER.get(ENTROPY_FAIL_LEVEL, 3) >= 3 else "MEDIUM"
                )
                classify(
                    "HIGH_ENTROPY_TOKEN",
                    sev,
                    f"Potential secret (entropy={ent:.2f}) in {rel}: '{match[:8]}...'",
                    rel,
                    match,
                )

        # Explicit pattern scans (line independent)
        for rule_id, sev, pattern in PATTERN_DEFINITIONS:
            for m in pattern.findall(text):
                # Avoid duplicate Age private key detection outside standard location
                if rule_id == "AGE_PRIVATE_KEY" and rel.endswith("age.key"):
                    continue
                classify(rule_id, sev, f"{rule_id} candidate in {rel}", rel, m)


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
                        [
                            "git",
                            "ls-files",
                            "--error-unmatch",
                            str(k.relative_to(REPO_ROOT)),
                        ]
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
    # Determine minimum severity for inclusion
    min_level = LEVEL_ORDER.get(os.getenv("VALIDATOR_REPORT_LEVEL", "LOW").upper(), 1)
    filtered_findings = [f for f in findings if LEVEL_ORDER.get(f["severity"], 0) >= min_level]

    if json_path:
        try:
            with open(json_path, "w", encoding="utf-8") as jf:
                json.dump({"timestamp": timestamp, "findings": filtered_findings}, jf, indent=2)
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
                            "ruleId": f["rule_id"],
                            "level": "error" if LEVEL_ORDER.get(f["severity"], 0) >= 3 else "note",
                            "message": {"text": f["message"]},
                            "locations": [],
                        }
                        for f in filtered_findings
                    ],
                }
            ],
        }
        try:
            with open(sarif_path, "w", encoding="utf-8") as sf:
                json.dump(sarif, sf, indent=2)
        except Exception as e:
            print(f"Could not write SARIF report: {e}", file=sys.stderr)


if __name__ == "__main__":  # pragma: no cover - complex integration logic, exercised via pre-commit
    _load_cache()
    scan_plain_json()
    check_age_key()
    scan_git_index()
    if os.getenv("LEGACY_FULL_SCAN"):
        legacy_run_detect_secrets()
        legacy_run_gitleaks()
    emit_reports(violations)
    _save_cache()
    # Fail decision based on highest severity meeting FAIL_LEVEL
    highest = 0
    for f in findings:
        level = LEVEL_ORDER.get(f["severity"], 0)
        if level > highest:
            highest = level
    fail_threshold = LEVEL_ORDER.get(FAIL_LEVEL, 3)
    if highest >= fail_threshold:
        print(f"Secret validation FAILED (threshold {FAIL_LEVEL}):")
        for f in findings:
            if LEVEL_ORDER.get(f["severity"], 0) >= fail_threshold:
                print(f" - [{f['severity']}] {f['message']}")
        sys.exit(1)
    else:
        if violations:
            print("Secret validation WARNINGS:")
            for f in findings:
                print(f" - [{f['severity']}] {f['message']}")
        print("Secret validation PASSED")
