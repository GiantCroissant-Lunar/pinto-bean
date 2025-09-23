# Pinto Bean - Development Workspace

PNPM workspace for managing documentation, ADRs, and development tools.

## 🏗️ Workspace Structure

```
scripts/nodejs/
├── packages/
│   ├── dev-tools/          # PM2 orchestration + Log4brains
│   └── docs-site/          # Docusaurus documentation site
├── apps/
│   └── main-site/          # Next.js main website (future)
└── shared/                 # Shared configs and utilities
```

## 🚀 Quick Start

```bash
cd scripts/nodejs

# Install all dependencies
pnpm install

# Start all development servers (triple hot-reload)
pnpm dev

# Individual servers
pnpm dev:docs     # Docusaurus docs (localhost:3000)
pnpm adr:preview  # Log4brains ADR preview (localhost:4004)
pnpm dev:main     # Next.js main site (localhost:3001)
```

## 🔧 Available Commands

### Workspace Management
- `pnpm install` - Install all workspace dependencies
- `pnpm build:all` - Build all packages

### ADR Management
- `pnpm adr:new` - Create new ADR
- `pnpm adr:preview` - Preview ADRs (localhost:4004)
- `pnpm adr:build` - Build ADR static site

### Documentation
- `pnpm dev:docs` - Start Docusaurus dev server
- `pnpm --filter docs-site build` - Build docs for production

### Main Site (Future)
- `pnpm dev:main` - Start Next.js dev server
- `pnpm --filter main-site build` - Build main site

## 📁 Development Ports

| Service | Port | URL | Purpose |
|---------|------|-----|---------|
| Docusaurus | 3000 | http://localhost:3000 | Documentation site |
| Next.js | 3001 | http://localhost:3001 | Main website |
| Log4brains | 4004 | http://localhost:4004 | ADR authoring & preview |

## 🔄 Hot Reload Setup

The `pnpm dev` command starts PM2 with all services watching relevant files:

- **ADR Preview**: Watches `docs/adr/` for ADR changes
- **Docs Site**: Watches `docs/`, `src/`, and `docs/adr/` for content/code changes
- **Main Site**: Watches `src/`, `pages/` for code changes

## 📝 ADR Workflow

1. **Create ADR**: `pnpm adr:new`
2. **Edit in IDE**: Files are in `../../../docs/adr/`
3. **Preview**: Visit http://localhost:4004 (auto-refreshes)
4. **View in Docs**: Visit http://localhost:3000/architecture/decisions (when integrated)

## 🎯 Integration Status

- ✅ PNPM workspace configured
- ✅ Log4brains working in dev-tools package
- ✅ Docusaurus initialized
- ✅ PM2 orchestration configured
- 🔄 ADR integration into Docusaurus (next phase)
- 🔄 Custom Log4brains plugin (next phase)
- 🔄 Next.js main site setup (future)

## 🚀 Next Steps

1. **ADR Integration**: Create custom Docusaurus plugin to import ADRs with full Log4brains feature parity
2. **GitHub Pages**: Configure deployment for docs-site
3. **Main Site**: Set up Next.js structure when ready

## 🛠️ Troubleshooting

- **PM2 not starting**: Run `pnpm --filter dev-tools dev:stop` then try again
- **Port conflicts**: Check if ports 3000, 3001, 4004 are available
- **Dependency issues**: Try `pnpm install --force` from workspace root
