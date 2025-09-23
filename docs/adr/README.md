# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) for the pinto-bean project using [log4brains](https://github.com/thomvaill/log4brains).

ADRs are automatically published to our Log4brains architecture knowledge base:

🔗 **<https://giantcroissant-lunar.github.io/pinto-bean/>**

Please use this link to browse them.

## Quick Start

### Prerequisites
- Node.js and npm installed
- log4brains installed globally: `npm install -g log4brains`

### Using the ADR Management Script

From the project root, use the PowerShell script:

```powershell
# List all ADRs
.\scripts\adr.ps1 list

# Preview the ADR knowledge base (opens web interface)
.\scripts\adr.ps1 preview

# Build static site
.\scripts\adr.ps1 build

# Create new ADR
.\scripts\adr.ps1 new "Your decision title"

# Show help
.\scripts\adr.ps1 help
```

## Current ADRs

1. **Use Markdown Architectural Decision Records** - Initial decision to adopt ADR methodology
2. **Use Log4brains to manage the ADRs** - Tool selection for ADR management  
3. **Use SOPS with age for secret encryption** - Secret management architecture
4. **Implement multi-layered secret scanning strategy** - Security tooling approach
5. **Use Terraform Cloud for infrastructure execution** - Infrastructure deployment strategy
6. **Adopt comprehensive CI/CD pipeline with security-first design** - CI/CD architecture

## ADR Process

1. **Identify Decision**: When facing an architecturally significant decision
2. **Research Options**: Evaluate alternatives and their trade-offs  
3. **Create ADR**: Use `.\scripts\adr.ps1 new "Decision Title"` or `log4brains adr new`
4. **Document**: Fill in the context, options considered, decision outcome, and consequences
5. **Review**: Submit as pull request for team review
6. **Accept**: Merge the ADR with status "accepted"
7. **Evolve**: If decision changes, create new ADR and mark old one as "superseded"

## Development

If not already done, install Log4brains:

```bash
npm install -g log4brains
```

To preview the knowledge base locally, run:

```bash
log4brains preview
```

In preview mode, the Hot Reload feature is enabled: any change you make to a markdown file is applied live in the UI.

To create a new ADR interactively, run:

```bash
log4brains adr new
```

## More information

- [Log4brains documentation](https://github.com/thomvaill/log4brains/tree/develop#readme)
- [What is an ADR and why should you use them](https://github.com/thomvaill/log4brains/tree/develop#-what-is-an-adr-and-why-should-you-use-them)
- [ADR GitHub organization](https://adr.github.io/)
