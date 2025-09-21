# Terraform: GitHub Repository Management

(Moved from parent directory.)

This configuration manages GitHub repository settings for `pinto-bean` via Terraform Cloud.

## Features
- Repository data lookup (no creation/destruction of repo)
- Branch protection for main branch
- Repository-level Actions secrets & variables (maps)
- Environment creation + per-environment secrets & variables
- Configurable status checks & PR review requirements
- Signed commits, admin enforcement, force-push / deletion flags

## Layout
| Path | Purpose |
|------|---------|
| `versions.tf` | Required versions & providers |
| `main.tf` | Provider config & repository data source |
| `variables.tf` | Input variables |
| `secrets_variables.tf` | Secrets, variables, environments resources |
| `branch_protection.tf` | Branch protection rule |
| `outputs.tf` | Outputs |

## Inputs Summary
Key variables (set these in the Terraform Cloud workspace):

- `github_owner` (string, required)
- `repository_name` (default `pinto-bean`)
- `default_branch` (default `main`)
- `repository_secrets` (map(string), sensitive)
- `repository_variables` (map(string))
- `environments` (map(object)) shape:
```hcl
environments = {
  prod = {
    secrets = { FOO = "bar" }
    variables = { FEATURE_FLAG = "true" }
  }
  staging = {
    secrets = {}
    variables = { LOG_LEVEL = "debug" }
  }
}
```
- `required_status_checks` (list(string))
- review / protection booleans (see `variables.tf`)

## Setting Secrets Securely
In Terraform Cloud set `repository_secrets` and any environment secrets as **HCL** variable (sensitive). Example:
```hcl
repository_secrets = {
  GH_PAT = var.some_external_sensitive
  NPM_TOKEN = "s3cr3t"
}
```
Or set directly as sensitive variable values.

## Importing Existing Items
Existing secrets can't be read back from GitHub (write-only). If a secret already exists with same name, Terraform will update / overwrite it.

If branch protection already exists, import it before first apply to avoid replacement:
```
terraform import github_branch_protection.main <repo>:<branch>
# example: owner/repo:main
```

## Required Workspace Environment Variable
Set a Terraform Cloud workspace environment variable:
- `GITHUB_TOKEN` with repo admin permissions (repo, admin:repo_hook recommended)

## Example Variable Set (HCL)
```hcl
github_owner = "GiantCroissant-Lunar"
repository_secrets = {
  EXAMPLE_SECRET = "value"
}
repository_variables = {
  RUNTIME = "dotnet"
}
required_status_checks = ["build", "test"]
required_approving_review_count = 1
environments = {
  prod = {
    secrets = { API_KEY = "xxx" }
    variables = { LOG_LEVEL = "info" }
  }
}
```

## Apply Flow
1. In Terraform Cloud workspace, set working directory to `infra/terraform/github` (or run locally by `cd infra/terraform/github`).
2. Configure variables & `GITHUB_TOKEN`.
3. (Optional) Import existing branch protection (see below).
4. Plan → review → Apply.

## Notes
- Deleting entries from maps will destroy corresponding secrets/variables.
- GitHub API does not return secret values; drift can't be detected unless name removed.
- Add more branch patterns by duplicating `github_branch_protection` resource.

## Scripts
Helper scripts under `../scripts` for Terraform Cloud variable management:
- `Set-TfcVariable.ps1`
- `BulkApply-TfcVariables.ps1`

## Future Enhancements
- Add CODEOWNERS management (file sync via local_file + github_repository_file)
- Add team permissions management
- Add issue labels management

---
