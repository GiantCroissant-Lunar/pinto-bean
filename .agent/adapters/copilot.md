# Copilot Adapter (Version Sync)
Base-Version-Expected: 0.2.0

Canonical rules live in `.agents/base/`. The base canon prevails on conflict.

## Retrieval Emphasis
- Copilot prioritizes `.github/` paths; pointer file `.github/copilot-instructions.md` references this adapter.

## Behavioral Adjustments
- Summaries: Prefer concise diffs and risk notes (align with R-PRC-010/060).
- Ask frequency: If ambiguous spec, ask (reinforces R-CODE-050).
- Avoid large multi‑file rewrites unless user explicitly requests (R-PRC-020).
- Enforce R-DOC-030/040 when creating or moving documents (RFCs/ADRs naming + RFC frontmatter).
- Enforce R-GIT-010: commit body must come from a file via `-F`; never include `
` escapes in `-m`.

## PR Output Conventions
- PR description sections: Summary / Rationale / Risks / Tests / Rollout & Rollback.
- Cite rule IDs verbatim; do not paraphrase rule identifiers.
