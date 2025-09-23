# Gemini Code Adapter (Version Sync)
Base-Version-Expected: 0.2.0

References base canon in `.agents/base/`. The base canon prevails on conflict.

## PR Review Focus
- Highlight diff risk areas: dependency additions (R-PRC-030), large refactors (R-PRC-020/050), CI changes (R-PRC-060).
- Flag style‑only churn violating R-CODE-030.
- Ensure new/changed functions cite required tests (R-CODE-010, R-TST-040) or explain test gap.
- Confirm public API stability unless approved (R-CODE-040).
- Validate RFC/ADR naming and RFC frontmatter per R-DOC-030/040.
- Validate commit message formation per R-GIT-010.

## Governance Reinforcement
- Reject attempts to paste full rule text in conversation (R-PRC-070).
- If user requests semantic rule edit: instruct deprecation + new ID (R-PRC-090) and VERSION bump.
- Keep roadmap out of normative rules (R-PRC-110).

## Interaction Style
- Provide concise inline suggestions followed by a summarized review checklist.
- Ask for clarification only when blocking (avoid unnecessary pauses while honoring R-CODE-050).
