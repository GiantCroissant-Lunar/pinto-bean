# Use Terraform Cloud for infrastructure execution

* Status: accepted
* Date: 2024-10-20 <!-- Estimated based on project structure -->
* Decision-makers: @GiantCroissant-Lunar

Technical Story: We need to choose between local Terraform execution and cloud-based execution for managing GitHub repository infrastructure.

## Context and Problem Statement

The pinto-bean project uses Terraform to manage GitHub repository configuration (branch protection, secrets, variables, environments). We need to decide on the execution model that best supports:

1. Consistent state management across team members
2. Secure handling of sensitive tokens (GitHub PAT, Terraform variables)
3. Integration with existing CI/CD workflows
4. Auditability and compliance
5. Ease of setup for new team members
6. Cost considerations

## Considered Options

* **Option A**: Local Terraform execution only
* **Option B**: Terraform Cloud with remote state and execution
* **Option C**: Self-hosted Terraform Enterprise
* **Option D**: Terraform Cloud for state, local execution
* **Option E**: HashiCorp Consul for state backend with local execution

## Decision Outcome

Chosen **Option B: Terraform Cloud with remote state and execution**, because:

- **State consistency**: Eliminates state file conflicts and corruption risks
- **Secret management**: Secure variable storage without local credential files
- **Audit trail**: Full execution logs and state history in Terraform Cloud
- **Team collaboration**: No need to coordinate state file access
- **CI/CD integration**: GitHub Actions can trigger runs via API
- **Cost effective**: Free tier sufficient for single workspace usage

### Positive Consequences

* No local state file management required
* Sensitive variables stored securely in Terraform Cloud workspace
* Complete audit trail of all infrastructure changes
* GitHub Actions integration via TFC API enables automated deployments
* Team members don't need local Terraform credentials
* Consistent execution environment regardless of developer machine

### Negative Consequences

* Dependency on Terraform Cloud availability
* Network connectivity required for all operations
* Additional API complexity for programmatic access
* Learning curve for team members unfamiliar with TFC

## Implementation Details

### Workspace Configuration
```hcl
# infra/terraform/main.tf
terraform {
  cloud {
    organization = "giantcroissant-lunar"
    workspaces {
      name = "pinto-bean"
    }
  }
}
```

### Variable Management
- Sensitive variables (tokens) stored in TFC workspace
- Non-sensitive variables can be set via `.tfvars` files or API
- PowerShell scripts automate variable synchronization

### API Integration Scripts
```powershell
# infra/terraform/scripts/
Apply-SecretsJson.ps1     # Sync variables from encrypted JSON
Queue-TfcRun.ps1         # Trigger Terraform runs
Get-TfcRunStatus.ps1     # Monitor run progress
Get-TfcOutputs.ps1       # Retrieve output values
```

### CI/CD Workflow
1. GitHub Actions decrypts secrets using SOPS/age
2. `Apply-SecretsJson.ps1` pushes variables to TFC workspace
3. `terraform init/validate/plan` run via API
4. Apply runs on main branch only, with approval gates

### Backup Strategy
- Terraform Cloud maintains state history automatically
- Configuration code is version-controlled in Git
- Variable values can be extracted via API for disaster recovery

## Alternative Usage Patterns

For development or emergency scenarios, local execution is still supported:
1. Extract variables from TFC workspace
2. Create local `terraform.tfvars` file
3. Run `terraform init/plan/apply` locally

However, this is discouraged for regular operations to maintain consistency.

## Cost Analysis

- **Terraform Cloud Free**: Suitable for single workspace, up to 5 users
- **Cost**: $0/month for current usage pattern
- **Scaling**: Can upgrade to paid plans if team or workspace needs grow

## Links

* [Terraform Cloud Documentation](https://www.terraform.io/cloud-docs)
* [TFC API Scripts](../../../infra/terraform/scripts/)
* [GitHub Actions Integration](../../../.github/workflows/terraform.yml)
