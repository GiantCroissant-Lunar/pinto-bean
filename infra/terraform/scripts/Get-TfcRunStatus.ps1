[CmdletBinding()]
param(
  [Parameter(Mandatory)][string]$RunId
)
if (-not $env:TFC_TOKEN) { throw 'TFC_TOKEN not set.' }
$base = 'https://app.terraform.io/api/v2'
$run = Invoke-RestMethod -Method GET -Uri "$base/runs/$RunId" -Headers @{ Authorization = "Bearer $($env:TFC_TOKEN)" }
$status = $run.data.attributes.status
$planId = $run.data.relationships.plan.data.id
$applyId = $run.data.relationships.apply.data.id 2>$null
[pscustomobject]@{ RunId=$RunId; Status=$status; PlanId=$planId; ApplyId=$applyId }
