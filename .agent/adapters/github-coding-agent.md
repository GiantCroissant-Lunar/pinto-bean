# GitHub Coding Agent Adapter (Version Sync)
Base-Version-Expected: 0.2.0

References base canon in `.agents/base/`. The base canon prevails on conflict.

## Retrieval & Scoping
- GitHub Coding Agent now reads **AGENTS.md** and honors **scoped files** (closest AGENTS.md to the working directory wins).
- This adapter provides behavior deltas; pointers in AGENTS.md should redirect here and to the base canon.

## Behavioral Adjustments
- Before large edits, surface R-PRC-020 thresholds and request confirmation.
- Treat `.github/workflows/**` edits as CI‑sensitive (R-PRC-060): include risk + rollback in PR text.
- Block new dependencies unless approved (R-PRC-030) or in pre‑approved list.
- Prefer minimal, reviewable PRs (R-PRC-010); avoid style‑only churn (R-CODE-030).
- If spec is ambiguous, ask (R-CODE-050) rather than guessing file contents.
- Enforce R-DOC-030/040 for RFC/ADR naming and RFC frontmatter.
- Enforce R-GIT-010: commit body must come from a file via `-F`; never include `
` escapes in `-m`.

## PR Output Conventions
- PR description must include: Summary / Rationale / Risks / Tests / Rollout & Rollback.
- Cite rule IDs verbatim; do not paraphrase rule identifiers.
