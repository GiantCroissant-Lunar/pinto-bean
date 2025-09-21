#!/usr/bin/env python3
"""Fail if plaintext secrets are present or sensitive keys unencrypted.
Rules:
 1. In infra/terraform/secrets and infra/terraform/github/secrets no *.json except *.json.encrypted
 2. Block age private key being staged outside ignore.
 3. Simple entropy / pattern scan on tracked files (optional lightweight) unless excluded.
Exit non-zero on violation; print reasons.
"""
from __future__ import annotations
import os, re, sys, json, math, pathlib, subprocess, shutil, datetime, uuid

REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]
SECRET_DIRS = [REPO_ROOT/"infra"/"terraform"/"secrets", REPO_ROOT/"infra"/"terraform"/"github"/"secrets"]
ALLOWED_JSON_SUFFIX = ".json.encrypted"
PLAIN_JSON_BLOCKLIST = re.compile(r".+\.json$")  # not currently used but kept for future refinement
SENSITIVE_KEY_PATTERN = re.compile(r"(API|SECRET|TOKEN|KEY|PASSWORD|PASS)$", re.IGNORECASE)
HIGH_ENTROPY_THRESHOLD = 4.0  # bits/char approximate
MIN_SECRET_LENGTH = 20
DETECT_BASELINE = REPO_ROOT/".secrets.baseline"

violations: list[str] = []

def sh(cmd: list[str]) -> str:
    return subprocess.check_output(cmd, text=True, cwd=REPO_ROOT).strip()

def calc_entropy(s: str) -> float:
    from collections import Counter
    if not s: return 0.0
    counts = Counter(s)
    n = len(s)
    return -sum((c/n)*math.log2(c/n) for c in counts.values())

WHITELIST_PLAINTEXT_TEMPLATES = {"infra/terraform/secrets/terraform.json", "infra/terraform/github/secrets/github-vars.json"}

def scan_plain_json():
    for d in SECRET_DIRS:
        if not d.exists():
            continue
        for p in d.glob("*.json"):
            if str(p).endswith(ALLOWED_JSON_SUFFIX):
                continue
            rel = str(p.relative_to(REPO_ROOT)).replace('\\','/')
            if rel in WHITELIST_PLAINTEXT_TEMPLATES:
                # treat as safe template; no further content inspection required
                continue
            violations.append(f"Plain JSON secret present: {rel}")

def scan_git_index():
    # list staged or committed files (tracked) to evaluate suspicious high entropy strings
    try:
        tracked = sh(["git", "ls-files"]).splitlines()
    except Exception:
        return
    candidate_ext = {"tf","ps1","py","yml","yaml","json","txt","md"}
    secret_like = re.compile(r"([A-Za-z0-9+/=_-]{20,})")
    path_fragment = re.compile(r"[/\\]")
    for rel in tracked:
        p = REPO_ROOT/rel
        if not p.is_file():
            continue
        if p.name.endswith(ALLOWED_JSON_SUFFIX):
            continue
        # Skip entropy scanning in test modules entirely
        if rel.startswith("scripts/python/tests/"):
            continue
        if p.suffix.lstrip('.') not in candidate_ext:
            continue
        try:
            text = p.read_text(errors='ignore')
        except Exception:
            continue
        for match in secret_like.findall(text):
            if len(match) < MIN_SECRET_LENGTH:
                continue
            ent = calc_entropy(match)
            if ent >= HIGH_ENTROPY_THRESHOLD \
               and not rel.startswith("infra/terraform/secrets/") \
               and not rel.startswith("infra/terraform/github/secrets/"):
                # Skip if token looks like a path or terraform directory fragment (common false positives)
                if path_fragment.search(match):
                    continue
                # Skip explicit validator related env var examples
                if match.startswith("VALIDATOR_"):
                    continue
                violations.append(f"Potential secret (high entropy) in {rel}: '{match[:8]}...' entropy={ent:.2f}")

def check_age_key():
    for d in SECRET_DIRS:
        k = d/"age.key"
        if k.exists():
            try:
                content_first = k.read_text().splitlines()[0]
            except Exception:
                content_first = ''
            if 'SECRET-KEY' in content_first:
                # ensure it's ignored by git
                try:
                    status = sh(["git", "ls-files", "--error-unmatch", str(k.relative_to(REPO_ROOT))])
                    if status:
                        violations.append(f"age.key appears tracked: {k.relative_to(REPO_ROOT)}")
                except subprocess.CalledProcessError:
                    pass

def run_detect_secrets():
    if not shutil.which("detect-secrets"):
        return
    if not DETECT_BASELINE.exists():
        violations.append("detect-secrets baseline missing: .secrets.baseline")
        return
    try:
        out = subprocess.check_output(["detect-secrets", "scan"], text=True, cwd=REPO_ROOT)
        # naive compare: if any high-entropy/keyword findings not in baseline -> fail
        # Use official audit tool: detect-secrets audit baseline; but we simulate quick diff.
        # Simpler: run hook which returns nonzero if new secrets vs baseline.
        hook_rc = subprocess.call(["detect-secrets-hook", "--baseline", str(DETECT_BASELINE)], cwd=REPO_ROOT)
        if hook_rc != 0:
            violations.append("detect-secrets found new potential secrets (update or audit baseline)")
    except Exception as e:
        violations.append(f"detect-secrets execution error: {e}")

def run_gitleaks():
    if not shutil.which("gitleaks"):
        return
    import tempfile, json as _json, time
    # Unique report path to avoid Windows file locking issues
    for attempt in range(2):
        report_fd = None
        try:
            with tempfile.NamedTemporaryFile(prefix=f"gitleaks_{os.getpid()}_", suffix=".json", delete=False) as tf:
                report_path = tf.name
            cmd = [
                "gitleaks", "detect", "--no-git", "--config", ".gitleaks.toml",
                "--report-format", "json", "--report-path", report_path
            ]
            rc = subprocess.call(cmd, cwd=REPO_ROOT)
            if rc != 0:
                try:
                    with open(report_path, 'r', encoding='utf-8', errors='ignore') as fh:
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
            if 'report_path' in locals() and os.path.exists(report_path):
                try:
                    os.remove(report_path)
                except OSError:
                    pass

def emit_reports(violations: list[str]):
    sarif_path = os.getenv("VALIDATOR_SARIF")
    json_path = os.getenv("VALIDATOR_JSON")
    if not (sarif_path or json_path):
        return
    timestamp = datetime.datetime.utcnow().isoformat() + "Z"
    if json_path:
        try:
            with open(json_path, 'w', encoding='utf-8') as jf:
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
                            "locations": []
                        } for v in violations
                    ]
                }
            ]
        }
        try:
            with open(sarif_path, 'w', encoding='utf-8') as sf:
                json.dump(sarif, sf, indent=2)
        except Exception as e:
            print(f"Could not write SARIF report: {e}", file=sys.stderr)

if __name__ == "__main__":
    scan_plain_json()
    check_age_key()
    scan_git_index()
    run_detect_secrets()
    run_gitleaks()
    emit_reports(violations)
    if violations:
        print("Secret validation FAILED:\n" + "\n".join(f" - {v}" for v in violations))
        sys.exit(1)
    print("Secret validation PASSED")
