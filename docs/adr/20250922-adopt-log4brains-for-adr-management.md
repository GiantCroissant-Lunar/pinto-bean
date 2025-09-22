# Adopt log4brains for ADR management and knowledge base

* Status: accepted
* Date: 2025-09-22
* Decision-makers: @GiantCroissant-Lunar

Technical Story: Following the evaluation in our GitHub issue discussion, we need to implement a system for documenting architectural decisions in a maintainable and accessible way.

## Context and Problem Statement

The pinto-bean project has made several significant architectural decisions that would benefit from formal documentation:

1. **SOPS with age for secret encryption** - Complex decision with multiple alternatives considered
2. **Multi-layered security scanning strategy** - Sophisticated approach requiring explanation
3. **Terraform Cloud vs local execution** - Infrastructure choice with ongoing implications
4. **Comprehensive CI/CD pipeline design** - Security-first approach that needs documentation

We need a solution that:
- Integrates well with our existing GitHub-based workflow
- Provides an accessible web interface for browsing decisions
- Supports markdown-based documentation (familiar to the team)
- Can be automated through our existing CI/CD pipelines
- Preserves decision history and context over time

## Considered Options

* **Option A**: Simple markdown files in docs/ directory
* **Option B**: GitHub Wiki for decision documentation
* **Option C**: log4brains for structured ADR management
* **Option D**: Custom documentation site with Jekyll/Hugo
* **Option E**: External documentation platform (Notion, Confluence)

## Decision Outcome

Chosen **Option C: log4brains for structured ADR management**, because:

- **Structured approach**: Enforces consistent ADR template (MADR)
- **Web interface**: Provides searchable, navigable knowledge base
- **Git integration**: Works with existing pull request workflow
- **Static site generation**: Can be published to GitHub Pages
- **Hot reload**: Live preview during development
- **Immutable records**: ADRs can only change status, preserving history

### Implementation Details

**Setup Structure:**
```
scripts/
  nodejs/               # Node.js dependencies isolated here
    package.json       # log4brains dependency
  adr.ps1              # PowerShell wrapper script
docs/adr/              # ADR markdown files
.log4brains.yml        # Configuration
.github/workflows/
  adr-publish.yml      # Automated publication
```

**Workflow Integration:**
- ADRs created via `.\scripts\adr.ps1 new "Title"` or `log4brains adr new`
- Preview available via `.\scripts\adr.ps1 preview`
- Automatic publication to GitHub Pages on main branch changes
- Badge in README linking to published knowledge base

**Initial ADRs Created:**
1. Use SOPS with age for secret encryption
2. Implement multi-layered secret scanning strategy  
3. Use Terraform Cloud for infrastructure execution
4. Adopt comprehensive CI/CD pipeline with security-first design
5. Adopt log4brains for ADR management (this ADR)

### Positive Consequences

* **Onboarding improvement**: New team members can understand architectural context
* **Decision traceability**: Clear record of why decisions were made
* **Consistent format**: Standardized template ensures complete information
* **Accessible browsing**: Web interface makes decisions easy to find
* **Integration**: Works seamlessly with existing GitHub workflow
* **Automation**: Publishing is fully automated via GitHub Actions

### Negative Consequences

* **Node.js dependency**: Adds JavaScript tooling to primarily PowerShell/Python project
* **Tool learning curve**: Team needs to learn log4brains commands and workflow
* **Maintenance overhead**: Another tool to keep updated and maintained
* **Publishing complexity**: Additional CI workflow to maintain

## Migration Plan

**Phase 1: Setup and Initial ADRs** ✅
- Install log4brains and configure project
- Create ADR management script for convenience
- Document existing major architectural decisions
- Set up automated publishing workflow

**Phase 2: Process Integration** (Next)
- Update contributing guidelines to include ADR process
- Train team on when and how to create ADRs
- Establish review process for architectural decisions

**Phase 3: Continuous Improvement** (Ongoing)
- Regular review of ADR process effectiveness
- Updates to templates or tooling as needed
- Periodic audit of decision outcomes vs. predicted consequences

## Alternatives Considered Details

**Option A (Simple Markdown)**: Rejected due to lack of structure and discoverability
**Option B (GitHub Wiki)**: Rejected due to separate workflow from code changes  
**Option D (Custom Site)**: Rejected due to higher maintenance burden
**Option E (External Platform)**: Rejected due to desire to keep documentation with code

## Links

* [log4brains repository](https://github.com/thomvaill/log4brains)
* [MADR template documentation](https://adr.github.io/madr/)
* [Michael Nygard's ADR article](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions.html)
* [Published ADR knowledge base](https://giantcroissant-lunar.github.io/pinto-bean/)
* [ADR management script](../../scripts/adr.ps1)
