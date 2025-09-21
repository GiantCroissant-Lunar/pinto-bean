[CmdletBinding()]
param(
  [string]$OutFile = (Join-Path $PSScriptRoot '..' '..' '..' '.secrets' 'age.key')
)
if (Test-Path $OutFile) { throw "File already exists: $OutFile" }
if (-not (Get-Command age-keygen -ErrorAction SilentlyContinue)) { throw 'age-keygen not found. Install from https://github.com/FiloSottile/age/releases' }
New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null
& age-keygen | Out-File -Encoding utf8 $OutFile
Write-Host "Wrote key file: $OutFile" -ForegroundColor Green
(Get-Content $OutFile | Select-String 'public key:').ToString()
