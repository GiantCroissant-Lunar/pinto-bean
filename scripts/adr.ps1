#!/usr/bin/env pwsh
<#
.SYNOPSIS
    ADR (Architecture Decision Record) management script for pinto-bean project

.DESCRIPTION
    This script provides convenient commands to manage Architecture Decision Records
    using log4brains, including creating new ADRs, previewing the knowledge base,
    and building the static site.

.PARAMETER Command
    The command to execute: preview, build, new, list

.PARAMETER Title
    Title for new ADR (required when Command is 'new')

.EXAMPLE
    ./adr.ps1 preview
    Starts the log4brains preview server

.EXAMPLE
    ./adr.ps1 new "Use Redis for caching"
    Creates a new ADR with the given title

.EXAMPLE
    ./adr.ps1 build
    Builds the static ADR site
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("preview", "build", "new", "list", "help")]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$Title,

    [Parameter()]
    [int]$Port = 4004
)

$ErrorActionPreference = 'Stop'
$RepoRoot = (Get-Item -Path (Resolve-Path .)).FullName

# Ensure we operate from repository root (script may be invoked from subdirectory)
try {
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    # If script is inside repo root/scripts, move to repo root only if not already there
    if (-not (Test-Path (Join-Path $PWD ".git"))) {
        if (Test-Path (Join-Path $scriptDir ".." ".." ".git")) {
            Set-Location (Resolve-Path (Join-Path $scriptDir ".." ".."))
            $RepoRoot = (Get-Item -Path (Resolve-Path .)).FullName
        }
    }
}
catch {
    Write-Host "⚠️  Failed to normalize working directory: $_" -ForegroundColor Yellow
}

function Write-Info($Message) {
    Write-Host "🔷 $Message" -ForegroundColor Cyan
}

function Write-Success($Message) {
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error-Custom($Message) {
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Test-Log4brains {
    try {
        $null = Get-Command log4brains -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Show-Help {
    Write-Host @"
🏗️  ADR Management for pinto-bean

Available commands:
  preview    Start the log4brains preview server
  build      Build the static ADR knowledge base
  new        Create a new ADR (requires title)
  list       List all existing ADRs
  help       Show this help message

Examples:
  .\adr.ps1 preview
  .\adr.ps1 new "Use Redis for caching"
  .\adr.ps1 build
  .\adr.ps1 list

Requirements:
  - Node.js and npm installed
  - log4brains installed globally: npm install -g log4brains

Current ADR directory: docs/adr/
Knowledge base URL (when running): http://localhost:$Port
"@
}

function Start-Preview {
    Write-Info "Starting log4brains preview server on port $Port..."
    Write-Info "Access the knowledge base at: http://localhost:$Port"
    Write-Info "Press Ctrl+C to stop the server"

    try {
        & log4brains preview --port $Port
    }
    catch {
        Write-Error-Custom "Failed to start preview server: $_"
        Write-Info "Make sure log4brains is installed: npm install -g log4brains"
    }
}

function Build-Site {
    Write-Info "Building static ADR knowledge base..."

    try {
        # Correct command for building the static site
        & log4brains build
        if (-not $?) { throw "log4brains build returned a non-zero exit code" }
        $distDir = Join-Path $RepoRoot ".log4brains" "dist"
        if (Test-Path $distDir) {
            Write-Success "Static site built successfully in $distDir"
        }
        else {
            Write-Info "Build finished but expected dist directory not found (looked in $distDir)."
        }
    }
    catch {
        Write-Error-Custom "Failed to build site: $_"
    }
}

function New-ADR {
    if (-not $Title) {
         Write-Error-Custom "Title is required for new ADR. Use: .\adr.ps1 new 'Your ADR Title'"
         return
    }

    Write-Info "Creating new ADR: $Title"

    try {
        # Pass the title through to log4brains (supports non-interactive creation)
        & log4brains adr new --title "$Title"
        if (-not $?) { throw "log4brains returned a non-zero exit code" }
        Write-Success "New ADR created successfully!"
    }
    catch {
        Write-Error-Custom "Failed to create new ADR: $_"
    }
}

function List-ADRs {
    Write-Info "Architecture Decision Records in docs/adr/:"
    Write-Host ""

    $adrDir = Join-Path $RepoRoot "docs/adr"
    if (-not (Test-Path $adrDir)) {
        Write-Error-Custom "ADR directory does not exist: $adrDir"
        return
    }

    $adrFiles = Get-ChildItem -Path (Join-Path $adrDir "*.md") -File |
        Where-Object { $_.Name -notin @("index.md", "README.md", "template.md") }

    if ($adrFiles) {
        foreach ($file in $adrFiles | Sort-Object Name) {
            $titleLine = Get-Content $file.FullName -First 15 | Where-Object { $_ -match "^#\s+(.+)" } | Select-Object -First 1
            $title = if ($titleLine) { $titleLine -replace "^#\s+", "" } else { "(No title found)" }
            Write-Host "📋 $($file.Name) - $title"
        }
    }
    else {
        Write-Host "No ADR files found."
    }
}

# Main execution
if (-not (Test-Log4brains)) {
    Write-Error-Custom "log4brains is not installed or not in PATH"
    Write-Info "Install it with: npm install -g log4brains"
    exit 1
}

switch ($Command) {
    "preview" { Start-Preview }
    "build" { Build-Site }
    "new" { New-ADR }
    "list" { List-ADRs }
    "help" { Show-Help }
    default { Show-Help }
}
