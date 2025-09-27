# Request for Comments (RFCs)

This directory contains architectural decision documents and proposals for the Yokan PintoBean platform.

## Active RFCs

| RFC | Title | Status | Summary |
|-----|-------|--------|---------|
| [RFC-0001](rfc-0001-service-platform-core.md) | Service Platform Core (4-Tier, Reverse-Mode Façade) | Draft | Cross-engine service platform with Tier-1 (contracts), Tier-2 (façades), Tier-3 (adapters), and Tier-4 (providers) |
| [RFC-0002](rfc-0002-codegen-and-consumption.md) | Code Generation & Consumption (Façades, Registry, Packaging) | Draft | Source generator attributes, outputs, diagnostics, and Unity/Godot/.NET consumption patterns |
| [RFC-0003](rfc-0003-selection-strategies.md) | Selection Strategies (PickOne, FanOut, Sharded) & Category Defaults | Draft | Standard selection strategies for provider selection with category defaults and override points |

## Quickstart Guides

| Guide | Description |
|-------|-------------|
| [Unity Integration Guide](../../UNITY.md) | Complete end-to-end Unity integration with DI bridge, strategy assets, profiles, and main-thread scheduler |
| [Samples Quickstart](../samples-quickstart.md) | Run instructions for each sample and how strategies affect routing behavior |
| [Selection Strategies Quickstart](../selection-strategies-quickstart.md) | Practical guide for implementing PickOne, FanOut, and Sharded strategies with DI configuration examples |
| [Hot-Swap Playbook](../hot-swap-playbook.md) | Operational guide for hot-swapping plugins in Unity and .NET environments |

## RFC Process

RFCs follow the standard process:
1. **Draft** - Initial proposal under discussion
2. **Accepted** - Approved for implementation
3. **Implemented** - Changes have been completed
4. **Superseded** - Replaced by a newer RFC

Each RFC includes:
- Unique ID and title
- Status, creation/update dates
- Authors and categories
- Dependencies and implementation links
- Detailed specification and rationale

## Contributing

To propose a new RFC:
1. Copy the RFC template
2. Assign the next available RFC number
3. Fill out all required sections
4. Submit as a pull request for review