# Required status checks applied to the default branch via branch protection
# Update this list if workflow job names change.
required_status_checks = [
  "pre-commit",
  "CodeQL",
  "Trivy FS / Config Scan",
  "Trivy Config (IaC) Scan"
]
