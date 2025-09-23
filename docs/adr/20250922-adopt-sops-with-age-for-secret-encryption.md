# Use SOPS with age for secret encryption

* Status: accepted
* Date: 2024-11-15 <!-- Estimated based on project structure -->
* Decision-makers: @GiantCroissant-Lunar

Technical Story: We need a secure and maintainable approach to manage sensitive configuration data like Terraform Cloud tokens and GitHub tokens within our infrastructure-as-code setup.

## Context and Problem Statement

The pinto-bean project manages GitHub repository configuration via Terraform Cloud. This requires storing sensitive tokens and API keys that:

1. Must be encrypted at rest in the Git repository
2. Need to be accessible to CI/CD pipelines
3. Should support key rotation without significant operational overhead
4. Must integrate well with existing PowerShell and Python tooling
5. Should provide cross-platform compatibility (Windows/Linux/macOS)

## Considered Options

* **Option A**: Store secrets as GitHub repository secrets only
* **Option B**: Use HashiCorp Vault for secret management
* **Option C**: Use SOPS (Secrets OPerationS) with age encryption
* **Option D**: Use git-crypt for repository-level encryption
* **Option E**: Use sealed-secrets with Kubernetes (not applicable for this use case)

## Decision Outcome

Chosen **Option C: SOPS with age encryption**, because:

- **Lightweight**: No external infrastructure required (unlike Vault)
- **Developer-friendly**: Integrates directly with existing file-based workflows
- **Flexible**: Supports multiple encryption backends, chose age for simplicity
- **CI/CD friendly**: Easy to decrypt in GitHub Actions with `AGE_KEY` secret
- **Cross-platform**: Works on Windows (PowerShell), Linux, and macOS
- **Granular**: Can encrypt specific JSON values while keeping structure readable
- **Auditable**: Changes to encrypted files are tracked in Git history

### Positive Consequences

* Sensitive tokens are encrypted at rest in the repository
* Developers can manage secrets using familiar file-based workflows
* CI/CD pipeline can decrypt secrets automatically using stored `AGE_KEY`
* Key rotation is straightforward with SOPS recipient management
* Integration with existing PowerShell scripts (`Encrypt-Secrets.ps1`, `Apply-SecretsJson.ps1`)

### Negative Consequences

* Requires SOPS and age tools to be installed in development environments
* Private age key must be securely distributed to developers
* Additional complexity compared to simple repository secrets approach

## Implementation Details

### File Structure
```
infra/terraform/
├── .sops.yaml                    # SOPS configuration
├── secrets/
│   ├── terraform.json            # Template (gitignored)
│   └── terraform.json.encrypted  # Encrypted secrets (committed)
└── github/secrets/
    ├── terraform-github-vars.json        # Template (gitignored)
    └── github-vars.encrypted.json        # Encrypted secrets (committed)
```

### Key Management
- Age key pair generation: `New-AgeKeyPair.ps1`
- Private key stored as `AGE_KEY` repository secret for CI
- Public key configured in `.sops.yaml`

### Workflow Integration
- `Encrypt-Secrets.ps1`: Encrypts plaintext JSON files
- `Apply-SecretsJson.ps1`: Decrypts and applies to Terraform Cloud variables
- CI workflow automatically decrypts using `AGE_KEY` secret

## Links

* [SOPS GitHub Repository](https://github.com/mozilla/sops)
* [age encryption tool](https://github.com/FiloSottile/age)
* [Project's encryption scripts](../../../infra/terraform/scripts/)
