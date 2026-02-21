<#
.SYNOPSIS
    Publishes a new ProgramManager release to GitHub.

.PARAMETER Version
    The version number for this release (e.g. 1.0.1).

.PARAMETER Message
    The git commit message for this release.

.EXAMPLE
    .\Release.ps1 -Version 1.0.1 -Message "Fix startup crash on Windows 11"
#>

param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$Message
)

$ErrorActionPreference = "Stop"

# Refresh PATH so tools installed in the current session (e.g. gh) are found
$env:Path = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')

$ProjectRoot  = $PSScriptRoot
$ProjectFile  = "$ProjectRoot\ProgramManager.vbproj"
$PublishDir   = "$ProjectRoot\bin\Release\net8.0-windows\publish"
$ZipPath      = "$ProjectRoot\ProgramManager-v$Version.zip"
$Tag          = "v$Version"
$Exclude      = @("ProgramManagerLayout.xml", "ProgramManagerSettings.xml", "BACKUP-XML")

function Step($label) { Write-Host "`n==> $label" -ForegroundColor Cyan }
function Ok($label)   { Write-Host "    OK: $label" -ForegroundColor Green }
function Fail($label) { Write-Host "`n    ERROR: $label" -ForegroundColor Red; exit 1 }

# ── 0. Preflight ────────────────────────────────────────────────────────────
Step "Checking prerequisites"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { Fail "gh CLI not found. Run: winget install GitHub.cli" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Fail "dotnet CLI not found." }

$ghAuth = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) { Fail "Not logged in to GitHub. Run: gh auth login" }
Ok "All prerequisites met"

# ── 1. Build & Publish ───────────────────────────────────────────────────────
Step "Building and publishing project"
dotnet publish $ProjectFile -c Release -o $PublishDir
if ($LASTEXITCODE -ne 0) { Fail "dotnet publish failed" }
Ok "Publish complete"

# ── 2. Create zip ────────────────────────────────────────────────────────────
Step "Creating release zip: ProgramManager-v$Version.zip"
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
$files = Get-ChildItem -Path $PublishDir | Where-Object { $Exclude -notcontains $_.Name }
Compress-Archive -Path $files.FullName -DestinationPath $ZipPath
$sizeMB = [math]::Round((Get-Item $ZipPath).Length / 1MB, 2)
Ok "Zip created ($sizeMB MB)"

# ── 3. Commit & push source ──────────────────────────────────────────────────
Step "Committing and pushing source changes"
Set-Location $ProjectRoot
git add --all
$status = git status --porcelain
if ($status) {
    git commit -m $Message
    if ($LASTEXITCODE -ne 0) { Fail "git commit failed" }
    Ok "Committed: $Message"
} else {
    Ok "No source changes to commit"
}
git push
if ($LASTEXITCODE -ne 0) { Fail "git push failed" }
Ok "Pushed to GitHub"

# ── 4. Create GitHub Release ─────────────────────────────────────────────────
Step "Creating GitHub Release $Tag"

$Notes = @"
## Program Manager $Tag

$Message

## Features

- **Tabbed grid layout** - 8 tabs with 4x4 grids (128 shortcut slots) plus an All Links management tab
- **Drag and drop** - Drop files and folders from Explorer onto any grid cell
- **Icon rearrangement** - Drag icons between cells to swap positions
- **File and folder support** - Shortcuts to both files and folders with proper Windows icons
- **Launch on double-click** - Open any shortcut directly from the grid or the All Links list
- **Icon notes** - Attach a free-text note to any shortcut via right-click; notes are saved to XML and visible in the All Links tab
- **Tab renaming** - Customize all 8 tab names from the management tab with live preview
- **Hide/unhide desktop folders** - Toggle the Windows Hidden attribute on desktop folders via right-click
- **Dark / light mode** - Switch themes from the management tab; preference is persisted
- **Roll-up** - Double-click the title bar to collapse the window to just the title bar
- **Automatic XML backups** - On every launch, both data files are backed up to a BACKUP-XML subfolder; the last 10 backups per file are retained
- **Portable** - All settings stored next to the executable; copy the folder for independent instances

## Requirements

- Windows 10 / 11
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Installation

1. Download **ProgramManager-$Tag.zip** below
2. Extract the folder anywhere you like
3. Run **ProgramManager.exe**

No installer required. All settings are stored next to the executable.

## Getting Started

1. Drag files or folders from Windows Explorer onto any cell in Tabs 1-8
2. Double-click an icon to launch it
3. Drag icons between cells to rearrange
4. Right-click an icon for options (Open Containing Folder, Edit Note, Hide Folder, Remove)
5. Use the **All Links** tab to rename tabs, toggle dark mode, and view all shortcuts with their notes
"@

gh release create $Tag $ZipPath `
    --title "ProgramManager $Tag" `
    --notes $Notes
if ($LASTEXITCODE -ne 0) { Fail "gh release create failed" }
Ok "Release published"

# ── 5. Cleanup local zip ─────────────────────────────────────────────────────
Remove-Item $ZipPath -Force
Ok "Local zip cleaned up"

Write-Host "`nDone! Release $Tag is live at: https://github.com/warmac57/ProgramManager/releases/tag/$Tag`n" -ForegroundColor Green
