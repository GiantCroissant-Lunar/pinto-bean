# Implement multi-layered secret scanning strategy

* Status: accepted
* Date: 2024-12-01 <!-- Estimated based on project structure -->
* Decision-makers: @GiantCroissant-Lunar

Technical Story: We need a comprehensive approach to prevent secrets from being committed to the repository, combining multiple scanning tools and enforcement points.

## Context and Problem Statement

The pinto-bean project handles sensitive infrastructure tokens and API keys. We need to ensure that:

1. No plaintext secrets are accidentally committed to the repository
2. High-entropy strings that might be secrets are detected early
3. Known secret patterns are identified and blocked
4. The solution works across different development environments (Windows/Linux/macOS)
5. Scanning integrates with our existing pre-commit hooks and CI/CD pipeline
6. False positives are minimized while maintaining security coverage

## Considered Options

* **Option A**: Single tool approach (gitleaks only)
* **Option B**: Single tool approach (detect-secrets only)
* **Option C**: Multi-layered approach with custom validation
* **Option D**: External service-based scanning (e.g., GitGuardian)
* **Option E**: Manual code review only

## Decision Outcome

Chosen **Option C: Multi-layered approach with custom validation**, implemented as:

1. **Custom Python validator** (`validate_secrets.py`) - Primary enforcement
2. **gitleaks** - Pattern-based secret detection
3. **detect-secrets** - Entropy-based detection with baseline management
4. **Pre-commit hooks** - Local enforcement
5. **CI validation** - Final verification gate

### Rationale

- **Defense in depth**: Multiple tools catch different types of secrets
- **Custom rules**: Tailored validation for project-specific requirements (age keys, JSON templates)
- **Developer experience**: Early feedback via pre-commit hooks
- **CI enforcement**: Prevents secrets from reaching main branch
- **Flexibility**: Can disable tools individually or run legacy full scans

### Positive Consequences

* High confidence that secrets won't be committed
* Multiple detection methods reduce false negatives
* SARIF output enables integration with GitHub Advanced Security
* Custom validator enforces project-specific rules (encrypted JSON naming, age key safety)
* Baseline management reduces false positives for detect-secrets

### Negative Consequences

* Multiple tools increase setup complexity
* Potential for tool conflicts or redundant alerts
* Requires maintenance of multiple configurations (`.gitleaks.toml`, `.secrets.baseline`)
* Longer pre-commit hook execution time

## Implementation Details

### Layer 1: Custom Python Validator
```python
# scripts/python/validate_secrets.py
- Structural checks (no plaintext .json in secret dirs)
- Age key safety (not tracked in git)
- High-entropy string detection
- Template validation
- Orchestrates other tools if LEGACY_FULL_SCAN=1
```

### Layer 2: Pre-commit Framework Integration
```yaml
# .pre-commit-config.yaml
- Custom validator hook
- gitleaks hook
- detect-secrets hook with baseline
```

### Layer 3: CI Integration
```yaml
# .github/workflows/*.yml
- Runs unified validator
- Generates SARIF reports for security dashboard
- Fails builds on any secret detection
```

### Configuration Files
- `.gitleaks.toml`: gitleaks configuration with allowlist
- `.secrets.baseline`: detect-secrets baseline for known false positives
- `scripts/python/validate_secrets.py`: Custom validation logic

### Enforcement Points
- **Pre-commit**: Local developer enforcement
- **CI/CD**: Branch protection via required status checks
- **Weekly scan**: Full repository scan for thoroughness

## Compliance and Reporting

The multi-layered approach generates:
- JSON reports for CI integration
- SARIF reports for GitHub Advanced Security
- Human-readable output for developers

Environment variables control report generation:
- `VALIDATOR_JSON=secret-scan.json`
- `VALIDATOR_SARIF=secret-scan.sarif`

## Links

* [gitleaks configuration](../../../.gitleaks.toml)
* [detect-secrets baseline](../../../.secrets.baseline)
* [Custom validator implementation](../../../scripts/python/validate_secrets.py)
* [Pre-commit configuration](../../../.pre-commit-config.yaml)
