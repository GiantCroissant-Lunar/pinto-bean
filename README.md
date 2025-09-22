# pinto-bean

[![Pre-Commit](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/pre-commit.yml/badge.svg)](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/pre-commit.yml)
[![CodeQL](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/codeql.yml/badge.svg)](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/codeql.yml)
[![Trivy Scan](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/trivy.yml/badge.svg)](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/trivy.yml)
[![Weekly Secret Scan](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/weekly-secret-scan.yml/badge.svg)](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/weekly-secret-scan.yml)

Infrastructure automation for GitHub repository management via Terraform Cloud.

> Secret scanning layers: Pre-commit hooks (gitleaks + detect-secrets) + custom validator + weekly full scan.

## Secrets & Tokens

Terraform Cloud token (TFC_TOKEN) is sourced exclusively from an encrypted `terraform.json.encrypted` (legacy `
terraform.json.sops.json` and `terraform.json.encrypted.json` still supported) in `infra/terraform/secrets/`.
Example template plaintext (committed scaffold) `infra/terraform/secrets/terraform.json` before encryption (contains only placeholders — safe to keep):
```
{
	"TFC_TOKEN": "<terraform-cloud-api-token>",
	"GITHUB_TOKEN": "<github-pat-or-gha-app-token>"
}
```
Encrypt it:
```
pwsh infra/terraform/scripts/Encrypt-Secrets.ps1 -TerraformJson
```
This creates `terraform.json.encrypted`; commit only the encrypted file (the placeholder template may remain for onboarding).

`Apply-SecretsJson.ps1` skips `TFC_TOKEN` (never stored as a Terraform workspace variable) but pushes `GITHUB_TOKEN` (as env) plus other keys. The GitHub Actions workflow decrypts and must find `TFC_TOKEN`; pipeline fails if absent.

## Age Key

Store the private age key in repository secret `AGE_KEY` for CI decryption. Generate with:
```
pwsh infra/terraform/scripts/New-AgeKeyPair.ps1
```

## Workflow

`.github/workflows/terraform.yml`:
1. Installs Terraform & SOPS.
2. Decrypts secrets if encrypted files exist.
3. Loads `TFC_TOKEN` from decrypted `terraform.json` (fails fast if missing).
4. Applies variables to Terraform Cloud via API.
5. Validates secret hygiene (Python validator + gitleaks).
6. Runs init/validate/plan/apply.

## Secret Scanning Tooling

Layers now:
1. Custom validator (`scripts/python/validate_secrets.py`): plaintext template enforcement, high-entropy heuristic scan, age key safety. By default it NO LONGER runs `detect-secrets` or `gitleaks` directly to avoid duplication.
2. `pre-commit` framework: owns `detect-secrets` (baseline guarded) and `gitleaks` signatures.
3. CI: runs the same `pre-commit` hooks plus the custom validator (and can still emit SARIF/JSON artifacts).

Legacy full scan mode: set `LEGACY_FULL_SCAN=1` to have the validator also invoke `detect-secrets` + `gitleaks` (useful for isolated debugging in CI).

Optional report outputs (CI friendly):
```
VALIDATOR_JSON=secret-scan.json
VALIDATOR_SARIF=secret-scan.sarif
```
SARIF can be uploaded to code scanning dashboards.

Install and activate hooks (dev tooling requirements file moved under scripts/python/):
```
pip install -r scripts/python/requirements-dev.txt
pre-commit install --hook-type pre-commit --hook-type commit-msg
git config core.hooksPath .githooks   # retains custom launcher chaining into pre-commit
```

Update baseline when secrets intentionally change:
```
detect-secrets scan > .secrets.baseline
git add .secrets.baseline
```

## Local Quality (QA) Runner

Use the consolidated PowerShell script (cross‑platform) to run all quality gates:

Run everything:
```
pwsh scripts/qa.ps1
```

Select specific steps:
```
pwsh scripts/qa.ps1 -Step precommit,ruff,mypy
```

Apply auto-fixes (Ruff format + Terraform fmt):
```
pwsh scripts/qa.ps1 -Fix
```

Legacy full secret scan with detect-secrets & gitleaks:
```
pwsh scripts/qa.ps1 -Step secrets -LegacyFullScan
```

Available step keys: `precommit, ruff, mypy, secrets, terraform, format, tests`.

The script internally:
- Runs pre-commit hooks across all files.
- Runs Ruff (lint then format check, or format with -Fix).
- Runs mypy against `scripts/python`.
- Runs custom secret validator (with optional legacy scan).
- Executes Terraform fmt/validate (only in `infra/terraform/github`).
- Discovers and runs unit tests under `scripts/python/tests`.

CI caches:
- Pre-commit environments (~/.cache/pre-commit)
- Tool caches: `.mypy_cache`, `.ruff_cache`, `.pytest_cache` for faster incremental runs.

## Next Steps

1. Populate & encrypt `terraform.json` (ensure TFC_TOKEN present) and commit only the encrypted file.
2. Remove any repository secret named `TFC_TOKEN` (no longer used).
3. Rotate tokens periodically; re-encrypt after edits (script re-runs are idempotent).

## Automated Dependency Updates

A `dependabot.yml` (or Renovate config) can keep Actions, Python, Terraform, and NuGet dependencies current with
batched weekly PRs. (Added in this branch.)
