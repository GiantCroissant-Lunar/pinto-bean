# Terraform Cloud Variable & Run Scripts

Helpers for managing TFC workspace variables and triggering runs for the `pinto-bean` GitHub repo configuration without needing the local Terraform CLI.

## Scripts

### `Set-TfcVariable.ps1`
Upsert a single variable.

Parameters:
- `-Organization` (TFC org)
- `-Workspace` (workspace name)
- `-Name` (variable key)
- `-Value` (variable value)
- `-Category` `terraform|env` (default terraform)
- `-Hcl` (switch) treat value as HCL
- `-Sensitive` (switch) mark sensitive

Example:
```powershell
./Set-TfcVariable.ps1 -Organization my-org -Workspace pinto-github -Name github_owner -Value GiantCroissant-Lunar
```

### `BulkApply-TfcVariables.ps1`
Apply variables from a JSON descriptor (see `variables.sample.json`).

Example:
```powershell
./BulkApply-TfcVariables.ps1 -Organization my-org -Workspace pinto-github -File ./variables.json
```

### `Apply-SecretsJson.ps1`
Load key/value pairs from `../../.secrets/terraform_github_vars.json` (fallback: `secret.json`) and push them:
- `GITHUB_TOKEN` => env category, sensitive
- Keys ending in `_SECRET`, `_TOKEN`, `_KEY`, `_PASS`, `_PASSWORD` become sensitive terraform vars
- Booleans / numbers / list `[ ... ]` / map `{ ... }` forms auto-flagged as HCL
- Others => terraform string vars

Example:
```powershell
./Apply-SecretsJson.ps1 -Organization my-org -Workspace pinto-github
```

### `Queue-TfcRun.ps1`
Queue a new plan/apply run in Terraform Cloud.

Parameters:
- `-Organization` (org name)
- `-Workspace` (workspace name)
- `-Message` optional run message (default "Queued via API script")
- `-IsDestroy` switch to create a destroy plan

Returns the Run ID (GUID).

Example:
```powershell
$runId = ./Queue-TfcRun.ps1 -Organization giantcroissant-lunar -Workspace pinto-bean -Message "Initial config"
```

### `Get-TfcRunStatus.ps1`
Fetch current status of a run.

Parameters:
- `-RunId` run GUID

Outputs object with: RunId, Status, PlanId, ApplyId.

Example:
```powershell
./Get-TfcRunStatus.ps1 -RunId $runId
```

Common statuses: pending, planning, planned, cost_estimating, cost_estimated, applying, applied, discarded, errored, canceled.

### `Get-TfcOutputs.ps1`
Retrieve current state outputs for a workspace.

Parameters:
- `-Organization`
- `-Workspace`

Returns Name / Value / Sensitive for each output (sensitive values not redacted in raw API if not flagged sensitive in Terraform config—treat carefully).

Example:
```powershell
./Get-TfcOutputs.ps1 -Organization giantcroissant-lunar -Workspace pinto-bean
```

## Workflow Without Local Terraform CLI
1. Ensure variables are populated:
   ```powershell
   ./Apply-SecretsJson.ps1 -Organization giantcroissant-lunar -Workspace pinto-bean
   ```
2. Queue a run:
   ```powershell
   $runId = ./Queue-TfcRun.ps1 -Organization giantcroissant-lunar -Workspace pinto-bean -Message "GitHub repo mgmt initial"
   ```
3. Poll status (repeat until Status is planned, applied, or errored):
   ```powershell
   ./Get-TfcRunStatus.ps1 -RunId $runId
   ```
4. (Optional) If run requires confirmation (auto-apply disabled) approve it manually in UI, or add an approval API script (future enhancement).
5. After applied, fetch outputs:
   ```powershell
   ./Get-TfcOutputs.ps1 -Organization giantcroissant-lunar -Workspace pinto-bean
   ```

## Auth
Set `TFC_TOKEN` in your shell:
```powershell
$env:TFC_TOKEN = '<terraform_cloud_token>'
```

## Tips
- Sensitive vars always fully sent (no diff detection here)
- For complex maps/lists ensure valid HCL when using `-Hcl` or `"hcl": true`
- Use version control for JSON file but exclude actual secret values
- Queue multiple runs sequentially; TFC auto-cancels superseded runs if speculative.
- Use `-IsDestroy` to produce a destroy plan (won't apply unless approved if auto-apply off).

Add this to a `.gitignore` if you create a real variables file:
```
infra/terraform/scripts/variables.json
```

## Future Enhancements
- Diff mode (only show pending changes)
- Delete vars not in file (safe prune)
- Export script to dump current non-sensitive vars
- Approval script (`/runs/:id/actions/apply`)
- Auto polling helper that waits until terminal state
- Prune unused variables
- Export variables script

## SOPS-based Secret Encryption
Use [SOPS](https://getsops.io) with age to encrypt `infra/terraform/github/secrets/terraform-github-vars.json` into `infra/terraform/github/secrets/github-vars.encrypted.json` (or a `.sops.json` naming you choose). The config file is now at `infra/terraform/.sops.yaml`.

### Files (Current Layout)
- `infra/terraform/.sops.yaml` config (add your real age recipient)
- `infra/terraform/github/secrets/terraform-github-vars.json` plaintext (gitignored via secrets folder .gitignore)
- `infra/terraform/github/secrets/github-vars.encrypted.json` encrypted committed file
- (Optional) age key: e.g. `infra/terraform/github/secrets/age.key` (gitignored) or central `.secrets/age.key`

### Scripts
- `New-AgeKeyPair.ps1` (still outputs to legacy .secrets path by default; adjust OutFile param to new location if desired)
- `Encrypt-Secrets.ps1` (update Source/Target params if using new path pattern)
- `Apply-SecretsJson.ps1` auto-detect logic currently points to legacy `.secrets/terraform_github_vars(.sops).json`; update pending to also search `infra/terraform/github/secrets`.

### Setup (Adjusted Paths Example)
```powershell
# 1. Generate key (override output)
./infra/terraform/scripts/New-AgeKeyPair.ps1 -OutFile ./infra/terraform/github/secrets/age.key

# 2. Prepare plaintext secrets
notepad ./infra/terraform/github/secrets/terraform-github-vars.json

# 3. Encrypt (override paths)
./infra/terraform/scripts/Encrypt-Secrets.ps1 -Source ./infra/terraform/github/secrets/terraform-github-vars.json -Target ./infra/terraform/github/secrets/github-vars.encrypted.json -Force

# 4. Apply (explicit path until script updated)
./infra/terraform/scripts/Apply-SecretsJson.ps1 -Organization giantcroissant-lunar -Workspace pinto-bean -Path ./infra/terraform/github/secrets/terraform-github-vars.json
```

### Updated Secrets Layout
Secrets now reside in `infra/terraform/github/secrets/`:
- `terraform-github-vars.json` (plaintext)
- `github-vars.encrypted.json` (encrypted primary)
- `terraform.json` (additional sensitive config)
- `terraform.json.sops.json` (encrypted variant if created)

`Apply-SecretsJson.ps1` auto-detect order:
1. github-vars.encrypted.json
2. terraform-github-vars.json
3. terraform.json.sops.json
4. terraform.json
5. legacy .secrets/*.sops.json
6. legacy .secrets/*.json

### Encrypt terraform.json
```powershell
# Encrypt terraform.json specifically
./infra/terraform/scripts/Encrypt-Secrets.ps1 -TerraformJson -Force
# Apply (auto-detects terraform.json.sops.json afterward)
./infra/terraform/scripts/Apply-SecretsJson.ps1 -Organization giantcroissant-lunar -Workspace pinto-bean
```

### Notes
- Until auto-detect is expanded, supply `-Path` when using relocated secrets.
- Keep encrypted file naming consistent with `.sops.yaml` `path_regex` rules if you add them.

### Rotating / Updating
1. Edit plaintext JSON
2. Re-run encryption (add -Force to overwrite)
3. Commit updated encrypted file
4. Re-run Apply script

### Key Rotation
- Generate new key file
- Add new recipient to `sops.yaml`
- Re-encrypt with both old & new (SOPS supports multiple recipients)
- Distribute new key, then remove old recipient and re-encrypt

### Security Notes
- Never commit `.secrets/age.key` or plaintext JSON
- CI pipeline needs the age key injected (secret variable) for decrypting if using in build steps
- `Apply-SecretsJson.ps1` only decrypts locally where `sops` & key are present

## GitHub Actions CI (Plan + Apply)
Workflow file: `.github/workflows/terraform.yml`

Secrets required:
- `TFC_TOKEN` Terraform Cloud user/org token
- `AGE_KEY` contents of your age private key file

Process steps:
1. Checkout
2. Install Terraform & SOPS
3. Write age key to `infra/terraform/secrets/age.key`
4. Decrypt any `github-vars.encrypted.json` / `terraform.json.sops.json`
5. Run `Apply-SecretsJson.ps1` to sync TFC vars
6. `terraform init`, `validate`, `plan`, `apply` (apply only on main branch)

If you need plan-only on PRs, add a separate job with `pull_request` trigger and drop the apply step.
