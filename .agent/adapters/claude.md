# Claude Code Adapter (Version Sync)
Base-Version-Expected: 0.2.0

References base rule set in `.agents/base/`. The base canon prevails on conflict.

## Interaction Style
- Plan‑first responses for multi‑step tasks (cite rule IDs in the plan).
- For large architectural change requests: include a simple risk matrix (scope × likelihood × impact) referencing R-PRC-020 and R-PRC-050.

## Extended Context Strategy
- Claude can handle longer reasoning; still avoid duplicating base docs—summarize and cite IDs only.
- Enforce naming/frontmatter conventions for docs per R-DOC-030/040 (RFCs in `rfcs/` with 4‑digit IDs; ADRs in `adr/`).
- Enforce R-GIT-010: write commit body to a Markdown file and use `git commit -F <file>`; never pass `
` in `-m`.
