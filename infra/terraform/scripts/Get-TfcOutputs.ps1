[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$Organization,
  [Parameter(Mandatory)][string]$Workspace
)
if (-not $env:TFC_TOKEN) { throw 'TFC_TOKEN not set.' }
$base = 'https://app.terraform.io/api/v2'
$ws = Invoke-RestMethod -Method GET -Uri "$base/organizations/$Organization/workspaces/$Workspace" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)" }
$stateVerId = $ws.data.relationships['current-state-version'].data.id
if (-not $stateVerId) { Write-Host 'No state yet.' -ForegroundColor Yellow; return }
$state = Invoke-RestMethod -Method GET -Uri "$base/state-versions/$stateVerId" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)" }
$outputs = $state.data.attributes.outputs
$result = @()
foreach ($k in $outputs.Keys) { $o = $outputs[$k]; $result += [pscustomobject]@{ Name=$k; Value=$o.value; Sensitive=$o.sensitive } }
$result
