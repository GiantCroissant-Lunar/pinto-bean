# Adopt comprehensive CI/CD pipeline with security-first design

* Status: accepted
* Date: 2024-11-30 <!-- Estimated based on project structure -->
* Decision-makers: @GiantCroissant-Lunar

Technical Story: We need a robust CI/CD pipeline that ensures code quality, security, and reliable infrastructure deployments while supporting our security-first development approach.

## Context and Problem Statement

The pinto-bean project requires a CI/CD strategy that addresses:

1. **Security scanning**: Multiple types of vulnerabilities (secrets, dependencies, IaC misconfigurations, SAST)
2. **Code quality**: Consistent formatting, linting, and pre-commit validation
3. **Infrastructure deployment**: Automated Terraform Cloud integration
4. **Branch protection**: Enforced quality gates before merge
5. **Cross-platform compatibility**: Works on developer machines and CI runners
6. **Compliance reporting**: SARIF output for security dashboards

## Considered Options

* **Option A**: Single workflow with all checks
* **Option B**: Separate workflows for different concern areas
* **Option C**: Matrix builds for multiple environments
* **Option D**: External CI system (Jenkins, CircleCI, etc.)
* **Option E**: Minimal CI with manual processes

## Decision Outcome

Chosen **Option B: Separate workflows for different concern areas**, implemented as:

1. **Pre-commit workflow**: Code quality and basic validation
2. **CodeQL workflow**: Static Application Security Testing (SAST)
3. **Trivy workflow**: Dependency and IaC security scanning
4. **Terraform workflow**: Infrastructure deployment
5. **Weekly secret scan**: Comprehensive secret detection
6. **Review response workflow**: Automated PR management

### Rationale

- **Separation of concerns**: Each workflow focuses on specific quality aspects
- **Parallel execution**: Faster feedback through concurrent runs
- **Granular control**: Can enable/disable specific checks independently
- **Clear failure attribution**: Easy to identify which quality gate failed
- **Flexible scheduling**: Different workflows can have different triggers

### Positive Consequences

* Comprehensive security coverage across multiple vectors
* Fast feedback loops for developers
* Clear quality gates enforced via branch protection
* SARIF integration enables GitHub Advanced Security features
* Automated dependency management via Renovate integration
* Badge-driven visibility of project health status

### Negative Consequences

* Multiple workflows increase CI complexity
* Higher resource usage due to parallel execution
* Potential for workflow interdependency issues
* More configuration files to maintain

## Implementation Details

### Workflow Architecture

#### 1. Pre-commit Workflow (`.github/workflows/pre-commit.yml`)
```yaml
Triggers: push, pull_request
Purpose: Code quality, formatting, basic validation
Tools: pre-commit hooks, custom validators
Badge: Pre-Commit status
```

#### 2. CodeQL Workflow (`.github/workflows/codeql.yml`)
```yaml
Triggers: push, pull_request, schedule (weekly)
Purpose: Static Application Security Testing
Languages: Python, PowerShell, JavaScript
Badge: CodeQL status
```

#### 3. Trivy Workflows (`.github/workflows/trivy.yml`)
```yaml
Triggers: push, pull_request
Purpose: 
  - Trivy FS/Config Scan: Dependencies and filesystem
  - Trivy Config (IaC) Scan: Terraform misconfigurations
SARIF: Uploaded to GitHub Security tab
Badge: Trivy Scan status
```

#### 4. Terraform Workflow (`.github/workflows/terraform.yml`)
```yaml
Triggers: push, pull_request
Purpose: Infrastructure validation and deployment
Features:
  - SOPS secret decryption
  - Terraform Cloud API integration
  - Plan on PR, Apply on main branch
  - Secret validation enforcement
```

#### 5. Weekly Secret Scan (`.github/workflows/weekly-secret-scan.yml`)
```yaml
Triggers: schedule (weekly)
Purpose: Comprehensive repository-wide secret detection
Tools: Full validator run with legacy scanning enabled
SARIF: Security findings uploaded
Badge: Weekly Secret Scan status
```

#### 6. Review Response (`.github/workflows/review-response.yml`)
```yaml
Triggers: pull_request_review
Purpose: Automated PR management and AI-assisted responses
Features: GitHub API integration for PR automation
```

### Branch Protection Integration

Required status checks configured via Terraform:
```hcl
required_status_checks = [
  "pre-commit",
  "CodeQL",
  "Trivy FS / Config Scan", 
  "Trivy Config (IaC) Scan"
]
```

This ensures all security and quality gates pass before merge.

### Badge Integration

README.md displays real-time status:
```markdown
[![Pre-Commit](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/pre-commit.yml/badge.svg)](...)
[![CodeQL](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/codeql.yml/badge.svg)](...)
[![Trivy Scan](https://github.com/GiantCroissant-Lunar/pinto-bean/actions/workflows/trivy.yml/badge.svg)](...)
```

### Security Features

1. **Secret Management**: 
   - Age key stored as `AGE_KEY` repository secret
   - Automatic SOPS decryption in workflows
   - TFC_TOKEN loaded from encrypted `terraform.json`

2. **Security Scanning**:
   - SARIF upload to GitHub Advanced Security
   - Multi-tool secret detection
   - Dependency vulnerability scanning
   - IaC misconfiguration detection

3. **Compliance**:
   - Audit trail through GitHub Actions logs
   - Security findings tracked in Security tab
   - Branch protection prevents bypassing checks

## Performance Considerations

- Workflows run in parallel where possible
- Caching used for dependencies and tool installations
- Selective triggering (not all workflows on every change)
- Resource limits managed through workflow design

## Future Enhancements

- [ ] Matrix builds for multi-environment testing
- [ ] Performance benchmarking workflows
- [ ] Automated security baseline updates
- [ ] Integration with external compliance tools

## Links

* [GitHub Actions Workflows](../../../.github/workflows/)
* [Branch Protection Configuration](../../../infra/terraform/github/branch_protection.tf)
* [Pre-commit Configuration](../../../.pre-commit-config.yaml)
