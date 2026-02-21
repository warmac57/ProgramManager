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
gh release create $Tag $ZipPath `
    --title "ProgramManager $Tag" `
    --notes "Release $Tag - $Message"
if ($LASTEXITCODE -ne 0) { Fail "gh release create failed" }
Ok "Release published"

# ── 5. Cleanup local zip ─────────────────────────────────────────────────────
Remove-Item $ZipPath -Force
Ok "Local zip cleaned up"

Write-Host "`nDone! Release $Tag is live at: https://github.com/warmac57/ProgramManager/releases/tag/$Tag`n" -ForegroundColor Green
