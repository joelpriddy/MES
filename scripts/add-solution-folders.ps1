<#
.SYNOPSIS
  Organize PA.MES.sln with solution folders and add all projects
  under their appropriate folders.

.DESCRIPTION
  - Ensures a solution exists (creates PA.MES.sln if missing).
  - Removes existing project entries (if they were added without folders).
  - Re-adds projects with --solution-folder so the solution is neatly grouped.

.PARAMETER SolutionPath
  Path to the solution file (default: PA.MES.sln)

.PARAMETER RepoRoot
  Repository root (default: current directory)

.EXAMPLE
  .\scripts\add-solution-folders.ps1
.EXAMPLE
  .\scripts\add-solution-folders.ps1 -SolutionPath "PA.MES.sln" -RepoRoot "."
#>

Param(
  [string]$SolutionPath = "PA.MES.sln",
  [string]$RepoRoot = "."
)

$ErrorActionPreference = "Stop"

function Assert-Command([string]$name) {
  if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
    throw "Required command '$name' not found. Please ensure it is installed and on PATH."
  }
}

function New-SolutionIfMissing([string]$slnPath) {
  if (-not (Test-Path $slnPath)) {
    Write-Host "Solution '$slnPath' not found. Creating..." -ForegroundColor Yellow
    dotnet new sln -n ([System.IO.Path]::GetFileNameWithoutExtension($slnPath)) | Out-Null
  }
}

function Get-SolutionProjects([string]$slnPath) {
  # Returns lines like: "Project(s)" -> we just need the list
  $lines = & dotnet sln $slnPath list
  $projLines = @()
  $capture = $false
  foreach ($l in $lines) {
    if ($l -match "Project\(s\)") { $capture = $true; continue }
    if ($capture -and -not [string]::IsNullOrWhiteSpace($l)) { $projLines += $l.Trim() }
  }
  return $projLines
}

function Remove-IfPresent([string]$slnPath, [string]$projPath) {
  $projPathNorm = (Resolve-Path $projPath).Path
  $inSol = Get-SolutionProjects $slnPath | Where-Object {
    # Normalize to absolute to compare reliably
    try {
      ((Resolve-Path $_).Path) -eq $projPathNorm
    } catch { $false }
  }
  if ($inSol) {
    Write-Host " - Removing existing entry: $projPath" -ForegroundColor DarkYellow
    dotnet sln $slnPath remove $projPath | Out-Null
  }
}

function Add-WithFolder([string]$slnPath, [string]$projPath, [string]$folder) {
  Write-Host " + Adding: $projPath  ->  $folder" -ForegroundColor Green
  dotnet sln $slnPath add $projPath --solution-folder $folder | Out-Null
}

# ---- MAIN ----
Push-Location $RepoRoot
try {
  Assert-Command "dotnet"
  New-SolutionIfMissing $SolutionPath

  # Map of projects -> solution folder
  $items = @()

  # Contracts
  $items += @{
    Path = "src/PA.Contracts.Abstractions/PA.Contracts.Abstractions.csproj"
    Folder = "Contracts"
  }
  $items += @{
    Path = "src/PA.Contracts.Models/PA.Contracts.Models.csproj"
    Folder = "Contracts"
  }
  $items += @{
    Path = "src/PA.Contracts.Grpc/PA.Contracts.Grpc.csproj"
    Folder = "Contracts"
  }

  # Platform
  $items += @{ Path = "src/PA.Platform.Common/PA.Platform.Common.csproj"; Folder = "Platform" }
  $items += @{ Path = "src/PA.Platform.Data/PA.Platform.Data.csproj";       Folder = "Platform" }
  $items += @{ Path = "src/PA.Platform.Messaging/PA.Platform.Messaging.csproj"; Folder = "Platform" }
  $items += @{ Path = "src/PA.Barcode/PA.Barcode.csproj";                   Folder = "Platform" }

  # Services
  $services = @('Catalog','Inventory','Manufacturing','Orders','Payments','Shipping','Customers')
  foreach ($s in $services) {
    $folder = "Services\$s"
    $items += @{ Path = "src/Services/$s/PA.$s.Domain/PA.$s.Domain.csproj"; Folder = $folder }
    $items += @{ Path = "src/Services/$s/PA.$s.Data/PA.$s.Data.csproj";     Folder = $folder }
    $items += @{ Path = "src/Services/$s/PA.$s.Api/PA.$s.Api.csproj";       Folder = $folder }
    $items += @{ Path = "src/Services/$s/PA.$s.Tests/PA.$s.Tests.csproj";   Folder = $folder }
  }

  # Gateway
  $items += @{ Path = "src/Services/Gateway/PA.Gateway.Api/PA.Gateway.Api.csproj"; Folder = "Services\Gateway" }

  # Tests
  $items += @{ Path = "tests/PA.EndToEnd.Tests/PA.EndToEnd.Tests.csproj"; Folder = "Tests" }

  # Validate files exist, remove if already in solution, then add with folder
  foreach ($it in $items) {
    $proj = $it.Path
    $folder = $it.Folder

    if (-not (Test-Path $proj)) {
      Write-Host " ! Skipping (not found): $proj" -ForegroundColor Red
      continue
    }

    Remove-IfPresent -slnPath $SolutionPath -projPath $proj
    Add-WithFolder -slnPath $SolutionPath -projPath $proj -folder $folder
  }

  Write-Host "`nDone. Open '$SolutionPath' in Visual Studio and verify the folder grouping." -ForegroundColor Cyan
}
finally {
  Pop-Location
}
