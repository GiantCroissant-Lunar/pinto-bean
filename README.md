# pinto-bean

Infrastructure automation for GitHub repository management via Terraform Cloud.

## Secrets & Tokens

Terraform Cloud token (TFC_TOKEN) is sourced exclusively from an encrypted `terraform.json.encrypted` (legacy `terraform.json.sops.json` and `terraform.json.encrypted.json` still supported) in `infra/terraform/secrets/`.

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

Pre-commit and CI use a unified Python validator (`scripts/python/validate_secrets.py`) that orchestrates:
1. Plaintext / structural checks.
2. detect-secrets baseline diff (`.secrets.baseline`).
3. gitleaks scan with `.gitleaks.toml` allowlist (parsed into same report).

Enable hooks locally (cross‑platform launcher auto-selects pwsh/python):
```
git config core.hooksPath .githooks
chmod +x .githooks/pre-commit
```

Refresh detect-secrets baseline after intentional changes:
```
detect-secrets scan > .secrets.baseline
git add .secrets.baseline
```

## Next Steps

1. Populate & encrypt `terraform.json` (ensure TFC_TOKEN present) and commit only the encrypted file.
2. Remove any repository secret named `TFC_TOKEN` (no longer used).
3. Rotate tokens periodically; re-encrypt after edits (script re-runs are idempotent).