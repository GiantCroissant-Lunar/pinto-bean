# Agent Instruction Base
Version: 0.2.0
Source of Truth for all automated assistant behavior.

## Composition
- 10-principles.md: Core philosophy
- 20-rules.md: Normative, enforceable rules (ID-based)
- 30-workflows.md: Task patterns (informative unless citing rule IDs)
- 40-glossary.md: Domain terms

Adapters (in ../adapters) must reference **rule IDs** instead of copying rule text.

## Adapter Sync & Versioning
- Adapters MUST declare `Base-Version-Expected:`. If it doesn’t match this file’s `Version`, adapters should **fail closed** (ask for upgrade or pin to a branch/tag).
- Pointer files (e.g., CLAUDE.md) should redirect agents to this canon and the agent‑specific adapter.

All adapters must enforce documentation conventions (R-DOC-030/040).


## Naming Conventions (Documents)
- **RFCs**
  - Single global track in `rfcs/` (lowercase, plural).
  - Filename: `rfc-xxxx-title.md` (4‑digit zero‑padded number, lowercase slug).
  - Frontmatter REQUIRED (see R-DOC-040): `id`, `title`, `status`, `category` (one or more of: gameplay, infra, web, publishing, docs, tooling), `labels` (optional).
  - Cite in text as “RFC 0042”. IDs are immutable once published.
- **ADRs**
  - Folder: `adr/` (lowercase, singular).
  - Filename: `0001-title.md` (4‑digit zero‑padded number, lowercase slug). Cite as “ADR 0001”.
- **PRDs** (optional)
  - Folder: `prd/` (lowercase, singular).
  - Filename: `YYYY-MM-DD-title.md`.

Rules:
- Lowercase on disk for portability; use uppercase acronym (RFC/ADR/PRD) in prose.
- Once published, numbers/IDs are immutable; create a new document to supersede.

## Change Policy
- **Add rule**: append with a new unique ID; never repurpose IDs.
- **Deprecate rule**: mark “DEPRECATED” but keep the ID (do not delete).
- **Major version bump** if any backward‑incompatible change (removal or semantics shift). Minor bump for additive rules or clarifications.
