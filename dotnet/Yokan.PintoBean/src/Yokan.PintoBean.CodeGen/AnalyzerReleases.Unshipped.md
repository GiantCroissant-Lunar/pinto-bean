; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SG0001 | PintoBean.Tier | Error | RealizeServiceAttribute not allowed in Tier-1
SG0002 | PintoBean.Configuration | Error | RealizeService without contracts
SG0003 | PintoBean.Configuration | Error | Missing GenerateRegistry for realized contract
SG0004 | PintoBean.Contract | Error | Façade method signature mismatch with contract
SG0005 | PintoBean.Design | Warning | Multi-contract façade spans categories