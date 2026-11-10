param(
    [Parameter(Mandatory = $true)]
    [string]$Repo
)

$ErrorActionPreference = 'Stop'
$patchDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$patches = @(
    '01_FRMS_CORE_REGISTRY_REPORTER.patch',
    '02_FRMS_AUTH_UI_TESTS.patch',
    '03_FRMS_CATALOG_AND_API_TESTS.patch'
)

$repoPath = (Resolve-Path -LiteralPath $Repo).Path
Push-Location -LiteralPath $repoPath
try {
    git rev-parse --show-toplevel | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Repo must be a Git worktree.' }

    $branch = (git branch --show-current).Trim()
    if ($LASTEXITCODE -ne 0 -or $branch -ne 'TEST_SWT') {
        throw "Expected branch TEST_SWT, got '$branch'. Run git switch TEST_SWT first."
    }

    $changes = @(git status --porcelain)
    if ($LASTEXITCODE -ne 0) { throw 'Could not query Git status.' }
    if ($changes.Count -gt 0) { throw 'Working tree is not clean. Commit or stash changes first.' }

    $expected = '45e94710477f9a9085ca22dbc69523c914de4d62'
    $head = (git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Could not query HEAD.' }
    if ($head -ne $expected) {
        Write-Warning "Current TEST_SWT commit $head differs from inspected branch baseline $expected. Checking patch compatibility."
    }

    # Verify all patches before mutating the worktree.
    foreach ($file in $patches) {
        $patch = Join-Path $patchDir $file
        if (-not (Test-Path -LiteralPath $patch)) { throw "Missing patch: $patch" }
        git apply --check -- $patch
        if ($LASTEXITCODE -ne 0) { throw "Cannot apply $file. No changes have been applied." }
    }
    foreach ($file in $patches) {
        $patch = Join-Path $patchDir $file
        git apply -- $patch
        if ($LASTEXITCODE -ne 0) { throw "Failed to apply $file. Inspect Git status before retrying." }
        Write-Host "Applied $file to TEST_SWT" -ForegroundColor Green
    }
    Write-Host 'All 3 patches applied on TEST_SWT. Next: pnpm run test:frms:coverage' -ForegroundColor Green
} finally {
    Pop-Location
}
