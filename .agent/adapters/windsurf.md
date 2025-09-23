# Windsurf Adapter (Version Sync)
Base-Version-Expected: 0.2.0

References base canon in `.agents/base/`. The base canon prevails on conflict.

## Retrieval & Scoping
- Windsurf (latest) resolves **scoped rules** from `.windsurf/` folders closest to the working directory.
- This adapter assumes pointers will live under `.windsurf/` at the appropriate scope and will redirect back to this adapter and the base canon.

## Behavioral Adjustments
- Prefer **plan-first** execution for multi-step edits; surface R-PRC-020 threshold checks inline.
- When editing multiple files, auto-summarize per-path risk and call out cross-cutting changes (R-PRC-020/050).
- Treat any changes under `.github/workflows/**` as CI-sensitive and include risk/rollback in PR output (R-PRC-060).
- If dependency changes are detected, halt and request approval citing R-PRC-030 unless on the pre-approved list.
- Enforce R-DOC-030/040 for RFC/ADR naming and RFC frontmatter.
- Enforce R-GIT-010 for commit message formation: use `git commit -F <file>`; no `
` in `-m`.

## Output Conventions
- When generating PRs or patches, include sections: Summary / Rationale / Risks / Tests / Rollout & Rollback.
- Cite rule IDs verbatim (do not paraphrase). If unsure about a rule, ask (R-CODE-050).
