### Branch Protection & Required Status Checks

The file `infra/terraform/github/terraform.tfvars` defines **`required_status_checks`**, consumed by the Terraform-managed branch protection for the default branch. This makes required CI gates declarative, reviewable, and versioned.

#### Currently Enforced Checks
- **pre-commit** – Aggregated lint + hygiene hooks
- **CodeQL** – Static application security testing (SAST)
- **Trivy FS / Config Scan** – Dependency + filesystem & generic config scan
- **Trivy Config (IaC) Scan** – Terraform / IaC misconfiguration scan

#### Why Declarative?
1. **Deterministic governance** – Protection policy changes must go through PR review.
2. **Reduced drift** – Job renames immediately surface (failing protection) until tfvars is updated.
3. **Security posture** – Ensures both SAST and vuln/misconfig scanning pass before merge.

#### Updating the List
1. Add or rename a workflow job you want enforced.
2. Trigger it on a PR (so the exact status context appears in the Checks UI).
3. Copy the displayed check name into `required_status_checks` in `terraform.tfvars` (same PR).
4. Run/apply Terraform so GitHub updates branch protection prior to merge.

#### Example Snippet
```hcl
# infra/terraform/github/terraform.tfvars
required_status_checks = [
  "pre-commit",
  "CodeQL",
  "Trivy FS / Config Scan",
  "Trivy Config (IaC) Scan"
]
```

#### Future Enhancements (Optional)
<details>
<summary>Expand protection & automation ideas</summary>

- ✅ Add test/build matrix jobs once present.
- ✅ Parameterize required approving reviews / dismiss_stale_reviews.
- ✅ Introduce a protected `staging` branch with slimmer gates for faster iteration.
- 🔄 Add a CI guard script to diff live check names vs tfvars to catch drift automatically.
- 🔄 Auto-open PR when a new recommended scan (e.g. SBOM) is detected.

</details>

#### Automation Idea (Drift Check)
A lightweight script could call the GitHub API for combined status contexts on the latest PR commit and compare to the list in tfvars, failing CI if mismatch is detected.

```bash
# pseudo-shell
LIVE=$(gh api repos/:owner/:repo/commits/$SHA/status | jq -r '.statuses[].context' | sort -u)
TFVARS=$(grep 'required_status_checks' infra/terraform/github/terraform.tfvars | sed 's/.*= \[//;s/\]//;s/"//g' | tr ',' '\n' | sed 's/^ *//;s/ *$//' | sort -u)
# diff logic here
```

Let me know if you want the drift guard implemented.
