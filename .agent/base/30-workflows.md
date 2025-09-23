# Workflows (Informative)

> Use these patterns to propose small, reviewable steps. When a rule applies, reference its ID.

## Bug Fix Pattern
1. Reproduce / restate issue.
2. Identify impacted rule IDs (e.g., R-CODE-010 if missing tests).
3. Propose minimal patch plan.
4. Implement; add/adjust tests; update CHANGELOG if public‑facing (R-DOC-010).
5. Open PR with required sections (see PR template below).

## Feature Addition
1. Confirm acceptance criteria.
2. Check existing modules (R-CODE-020).
3. Draft plan referencing constraints (security, performance).
4. Implement incrementally (small PRs per R-PRC-010).
5. Ensure tests per R-CODE-010/R-TST-040; update docs if API changes.

## Large Refactor Candidate
- Stop if it triggers R-PRC-020; ask user with:
  - Scope
  - Risk
  - Benefit
  - Alternatives
  - Test strategy

## Required PR Sections (all PRs)
- **Summary** – What changed and why.
- **Rationale** – Link issues/RFCs.
- **Risks** – What might break + mitigations.
- **Tests** – What was added/updated; how to run.
- **Rollout/Rollback** – Deploy plan and safe rollback.
