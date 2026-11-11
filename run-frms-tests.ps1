#requires -version 5.1
<#
.SYNOPSIS
  Run FRMS automated tests on Windows with complete Testing environment.
.EXAMPLE
  .\run-frms-tests.ps1 -Mode all
.EXAMPLE
  .\run-frms-tests.ps1 -Mode case -CaseId FRMS-ST-0005
.EXAMPLE
  .\run-frms-tests.ps1 -Mode catalog-probe
.EXAMPLE
  .\run-frms-tests.ps1 -Mode all -Workers 4 -Retries 1 -SkipBackend
#>
[CmdletBinding()]
param(
    [ValidateSet('all','case','area','coverage','preflight','catalog-probe')]
    [string]$Mode = 'all',
    [string]$CaseId,
    [string]$Area,
    [ValidateRange(1,16)][int]$Workers = 4,
    [ValidateRange(0,3)][int]$Retries = 1,
    [ValidateRange(10,240)][int]$StartupTimeoutSec = 90,
    [switch]$SkipBackend,
    [switch]$SkipCatalogDiscovery,
    [switch]$InstallBrowser
)

$ErrorActionPreference = 'Stop'
$Repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$EnvFile = Join-Path $Repo 'backend\Frms.Api\.env'
$Reports = Join-Path $Repo 'tests\system\reports'
$ApiUrl = 'http://127.0.0.1:5164'
$UiUrl = 'http://127.0.0.1:4173'
$backendProcess = $null
$exitCode = 0

function Set-DefaultEnv([string]$Name, [string]$Value) {
    $existing = [Environment]::GetEnvironmentVariable($Name, 'Process')
    if ([string]::IsNullOrWhiteSpace($existing)) {
        [Environment]::SetEnvironmentVariable($Name, $Value, 'Process')
    }
}

function Load-DotEnv([string]$File) {
    if (-not (Test-Path -LiteralPath $File)) {
        throw "Missing backend environment file: $File"
    }
    $count = 0
    foreach ($line in Get-Content -LiteralPath $File -Encoding UTF8) {
        if ($line -notmatch '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)$') { continue }
        $key = $Matches[1]
        $value = $Matches[2].Trim()
        if ($value.Length -ge 2) {
            if (($value.StartsWith('"') -and $value.EndsWith('"')) -or
                ($value.StartsWith("'") -and $value.EndsWith("'"))) {
                $value = $value.Substring(1, $value.Length - 2)
            }
        }
        [Environment]::SetEnvironmentVariable($key, $value, 'Process')
        $count++
    }
    Write-Host "Loaded $count backend settings from .env (values hidden)."
}

function Assert-Prerequisites {
    if (-not (Test-Path (Join-Path $Repo 'tests\system\scripts\run.mjs'))) {
        throw 'Expected FRMS repository layout not found. Place this script in the repository root.'
    }
    foreach ($tool in @('dotnet','node','git','pnpm.cmd')) {
        if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
            throw "Missing required executable: $tool"
        }
    }
    $branch = (& git -C $Repo branch --show-current 2>$null)
    if ($LASTEXITCODE -ne 0 -or $branch.Trim() -ne 'TEST_SWT') {
        throw "This launcher is for TEST_SWT; current branch: $branch"
    }
}

function Assert-RunnerSupportsWorkers {
    $runnerFile = Join-Path $Repo 'tests\system\scripts\run.mjs'
    $runner = [IO.File]::ReadAllText($runnerFile)
    if (-not $runner.Contains('FRMS_PLAYWRIGHT_WORKERS')) {
        throw 'Runner still hardcodes 1 worker. Apply 07_FRMS_CONFIGURABLE_WORKERS.patch first.'
    }
}

function Test-ApiHealthy {
    try {
        $r = Invoke-WebRequest -Uri "$ApiUrl/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        return ($r.StatusCode -eq 200 -and $r.Content.Trim() -eq 'Healthy')
    } catch { return $false }
}

function Start-BackendIfNeeded {
    if (Test-ApiHealthy) {
        Write-Host "Existing API is healthy at $ApiUrl (ensure it is configured for Testing)."
        return
    }
    if ($SkipBackend) {
        throw "No healthy API at $ApiUrl and -SkipBackend was specified."
    }
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $stdout = Join-Path $Reports "launcher_backend_${stamp}.stdout.log"
    $stderr = Join-Path $Reports "launcher_backend_${stamp}.stderr.log"
    $dotnetExe = (Get-Command dotnet).Source
    $script:backendProcess = Start-Process -FilePath $dotnetExe -ArgumentList @(
        'run', '--project', '.\backend\Frms.Api', '--no-launch-profile'
    ) -WorkingDirectory $Repo -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
    Write-Host "Started backend process PID=$($script:backendProcess.Id). Logs: $Reports"
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if (Test-ApiHealthy) { Write-Host 'API /health: Healthy'; return }
        $script:backendProcess.Refresh()
        if ($script:backendProcess.HasExited) {
            throw "Backend exited before API became healthy. Inspect $stdout and $stderr (may contain secrets)."
        }
        Start-Sleep -Seconds 2
    }
    throw "Backend did not become healthy within $StartupTimeoutSec seconds. Inspect $stdout and $stderr."
}

function Run-Pnpm([string[]]$Arguments) {
    $pnpmExe = (Get-Command pnpm.cmd).Source
    Write-Host ("Executing: pnpm " + ($Arguments -join ' '))
    # Send native stdout to the console, not to PowerShell's success pipeline.
    # Otherwise `$preflight = Run-Pnpm ...` captures script output + the exit code
    # as an array, causing `-ne 0` to report failure even when pnpm exits 0.
    & $pnpmExe @Arguments | Out-Host
    $result = $LASTEXITCODE
    if ($null -eq $result) { $result = 2 }
    return [int]$result
}

function Setup-TestEnvironment {
    Load-DotEnv $EnvFile
    # Never replace real database/JWT values with placeholders; they come from .env.
    foreach ($required in @('ConnectionStrings__FrmsDb','Jwt__SigningKey')) {
        if (-not [Environment]::GetEnvironmentVariable($required, 'Process')) {
            throw "Missing backend variable: $required"
        }
    }
    $cert = Join-Path $Repo 'backend\Frms.Api\prod-ca-2021.crt'
    if (-not (Test-Path -LiteralPath $cert)) {
        Write-Warning 'Backend TLS certificate prod-ca-2021.crt was not found. Check your connection string.'
    }
    $env:ASPNETCORE_ENVIRONMENT = 'Testing'
    $env:DOTNET_ENVIRONMENT = 'Testing'
    $env:ASPNETCORE_URLS = $ApiUrl
    $env:FRMS_ENVIRONMENT = 'Testing'
    $env:FRMS_API_URL = $ApiUrl
    $env:FRMS_UI_URL = $UiUrl
    $env:VITE_API_PROXY_TARGET = $ApiUrl
    $env:CI = 'true'
    $env:FRMS_PLAYWRIGHT_WORKERS = [string]$Workers
    $env:FRMS_PLAYWRIGHT_RETRIES = [string]$Retries
    if ($env:NODE_ENV -eq 'production') { Remove-Item Env:NODE_ENV }
    # These switches do NOT disable the reservation-expiry job.
    $env:BackgroundJobs__Billing__Enabled = 'false'
    $env:BackgroundJobs__NoShow__Enabled = 'false'
    $env:BackgroundJobs__Notifications__Enabled = 'false'

    # Customer authentication is PHONE-based; staff authentication is EMAIL-based.
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_1_PHONE' '0901000001'
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_2_PHONE' '0901000002'
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_3_PHONE' '0901000003'
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_1_PASSWORD' '11111111'
    Set-DefaultEnv 'FRMS_TEST_STAFF_01_EMAIL' 'staff.f01@example.com'
    Set-DefaultEnv 'FRMS_TEST_STAFF_PASSWORD' '11111111'
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_2_PASSWORD' '11111111'
    Set-DefaultEnv 'FRMS_TEST_MANAGER_01_EMAIL' 'manager.f01@example.com'
    Set-DefaultEnv 'FRMS_TEST_MANAGER_PASSWORD' '11111111'
    Set-DefaultEnv 'FRMS_TEST_BOM_EMAIL' 'bom@example.com'
    Set-DefaultEnv 'FRMS_TEST_BOM_PASSWORD' '11111111'
    Set-DefaultEnv 'FRMS_TEST_ADMIN_EMAIL' 'admin@example.com'
    Set-DefaultEnv 'FRMS_TEST_ADMIN_PASSWORD' '11111111'

    # Reference unit type. A valid ACTIVE facility and positive capacity must
    # still be established before the catalog fixture is considered complete.
    Set-DefaultEnv 'FRMS_TEST_AVAILABLE_UNIT_TYPE_ID' 'b1000000-0000-0000-0000-000000000003'
    Set-DefaultEnv 'FRMS_TEST_AVAILABLE_UNIT_NAME' 'Shared Public Locker (5x5)'
    Set-DefaultEnv 'FRMS_TEST_AVAILABLE_UNIT_SIZE' '3 m² equivalent (Locker)'
    Set-DefaultEnv 'FRMS_TEST_AVAILABLE_UNIT_MODE' 'PUBLIC'
    Set-DefaultEnv 'FRMS_TEST_AVAILABLE_UNIT_RENT_VND' '3500'

    if ($env:ConnectionStrings__Frms -and $env:ConnectionStrings__FrmsDb) {
        Write-Warning 'Both ConnectionStrings__Frms and ConnectionStrings__FrmsDb are set; confirm which database the backend uses.'
    }
    if ($env:ConnectionStrings__FrmsDb -match '(supabase|pooler\.supabase|amazonaws|azure|neon|render\.com)') {
        Write-Warning 'The connection appears remote/shared. Testing mode does NOT isolate the database; avoid destructive tests.'
    }
    Write-Host "Testing environment prepared: API=$ApiUrl; UI=$UiUrl; workers=$Workers; retries=$Retries; CI=true."
    Write-Host 'Backend settings: DB configured; JWT configured; secrets not printed.'
}

function Resolve-CatalogFixture {
    # FRMS backend filters ACTIVE facilities by positive whole-period capacity
    # BEFORE pagination. Discover from the same two-month catalogue as ST-0028,
    # rather than from a no-month list that can contain unavailable facilities.
    # ST-0027 scopes the one-month search to the verified facility ID.
    try {
        $nowUtc = [DateTime]::UtcNow
        $local = [TimeZoneInfo]::ConvertTimeBySystemTimeZoneId($nowUtc, 'SE Asia Standard Time')
        $start = (Get-Date -Year $local.Year -Month $local.Month -Day 1).AddMonths(1).ToString('yyyy-MM')
        $end = (Get-Date -Year $local.Year -Month $local.Month -Day 1).AddMonths(2).ToString('yyyy-MM')
        $base = "$ApiUrl/api/v1"
        $url = "$base/facilities?page=1&pageSize=20&sortBy=name&sortDirection=asc&startMonth=$start&endMonth=$end"
        $facilities = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 90
        $candidates = @($facilities.data | Where-Object { $_.status -eq 'ACTIVE' })
        if ($candidates.Count -eq 0) {
            Write-Warning "Catalog fixture: no ACTIVE facilities with capacity for $start to $end."
            return
        }
        foreach ($facility in $candidates) {
            $id = [uri]::EscapeDataString([string]$facility.facilityId)
            try {
                $unitUrl = "$base/facilities/$id/unit-types?view=public&page=1&pageSize=20&sortBy=name&sortDirection=asc&startMonth=$start&endMonth=$end&availableOnly=true"
                $units = Invoke-RestMethod -Uri $unitUrl -Method Get -TimeoutSec 45
                $eligible = @($units.data | Where-Object {
                    $_.availableCapacity -gt 0 -and
                    $_.requestedPeriod.startMonth -eq $start -and
                    $_.requestedPeriod.endMonth -eq $end
                })
                if ($eligible.Count -eq 0) { continue }
                $preferred = @($eligible | Where-Object { $_.unitTypeId -eq 'b1000000-0000-0000-0000-000000000003' } | Select-Object -First 1)
                $selected = if ($preferred.Count -gt 0) { $preferred[0] } else { $eligible[0] }

                # Independently confirm that this exact unit is available for
                # the one-month card check, not just the two-month search.
                $unitId = [uri]::EscapeDataString([string]$selected.unitTypeId)
                $oneMonth = Invoke-RestMethod -Uri "$base/facilities/$id/unit-types?view=public&page=1&pageSize=1&unitTypeId=$unitId&startMonth=$start&endMonth=$start&availableOnly=true" -Method Get -TimeoutSec 45
                if (@($oneMonth.data | Where-Object { $_.availableCapacity -gt 0 }).Count -eq 0) { continue }

                $env:FRMS_TEST_ACTIVE_FACILITY_ID = [string]$facility.facilityId
                $env:FRMS_TEST_ACTIVE_FACILITY_NAME = [string]$facility.name
                $env:FRMS_TEST_AVAILABLE_UNIT_TYPE_ID = [string]$selected.unitTypeId
                $env:FRMS_TEST_AVAILABLE_UNIT_NAME = [string]$selected.name
                $env:FRMS_TEST_AVAILABLE_UNIT_SIZE = [string]$selected.size
                $env:FRMS_TEST_AVAILABLE_UNIT_MODE = [string]$selected.mode
                $env:FRMS_TEST_AVAILABLE_UNIT_RENT_VND = [string]$selected.rentalPrice
                if ($selected.description) {
                    $env:FRMS_TEST_AVAILABLE_UNIT_DESCRIPTION = [string]$selected.description
                } else {
                    Remove-Item Env:FRMS_TEST_AVAILABLE_UNIT_DESCRIPTION -ErrorAction SilentlyContinue
                }
                Write-Host "Catalog fixture verified for BOTH month windows: $($facility.name) / $($selected.name) / $($selected.availableCapacity) slots / $start to $end."
                return
            } catch {
                Write-Warning "Catalog candidate $($facility.name) could not be verified: $($_.Exception.Message)"
            }
        }
        Write-Warning 'No eligible catalog fixture was verified in the filtered first page.'
    } catch {
        Write-Warning ("Catalog auto-discovery failed: " + $_.Exception.Message)
    }
    # Make missing/stale discovery explicitly Blocked, never a false application defect.
    Remove-Item Env:FRMS_TEST_ACTIVE_FACILITY_ID -ErrorAction SilentlyContinue
    Remove-Item Env:FRMS_TEST_ACTIVE_FACILITY_NAME -ErrorAction SilentlyContinue
}

function Resolve-VerifiedNegativeLoginFixtures {
    # Complete Admin directory enumeration, authenticated and read-only.
    $verifier = Join-Path $Repo 'tests\system\scripts\batch12-verify-unregistered.mjs'
    $negativeKeys = @('FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE', 'FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL')
    $verified = $false
    $reason = 'Verifier unavailable or did not return a valid result'
    if (Test-Path -LiteralPath $verifier -PathType Leaf) {
        try {
            $raw = (& node $verifier | Out-String).Trim()
            if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($raw)) {
                throw 'Verifier process failed'
            }
            $result = $raw | ConvertFrom-Json -ErrorAction Stop
            if ($result.verified -eq $true) {
                $phone = [string]$result.fixtures.FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE
                $email = [string]$result.fixtures.FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL
                if ($phone -notmatch '^0[0-9]{9}
    $scriptFile = Join-Path $Repo 'tests\system\scripts\batch12-discover.mjs'
    if (-not (Test-Path -LiteralPath $scriptFile)) {
        Write-Warning 'Batch 12 fixture discovery script not found; missing fixtures remain blocked.'
        return
    }
    try {
        $rawJson = & node $scriptFile
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($rawJson)) { throw 'Node discovery failed.' }
        $result = $rawJson | ConvertFrom-Json
        $count = 0
        foreach ($item in $result.fixtures.PSObject.Properties) {
            if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($item.Name, 'Process'))) {
                [Environment]::SetEnvironmentVariable($item.Name, [string]$item.Value, 'Process')
                $count++
            }
        }
        Write-Host "Batch 12 fixture discovery: $count verified value(s) supplied to this launcher process."
        foreach ($item in $result.roles.PSObject.Properties) {
            $state = if ($item.Value) { "verified" } else { "unavailable" }
            Write-Host "  $($item.Name): $state"
        }
        foreach ($note in @($result.notes)) { Write-Warning "Batch 12 fixture: $note" }
    } catch {
        Write-Warning ("Batch 12 fixture discovery unavailable: " + $_.Exception.Message)
        Write-Warning 'Unresolved fixtures remain SKIPPED/Blocked, not PASS.'
    }
}

try {
    Set-Location -LiteralPath $Repo
    Assert-Prerequisites
    if ($Mode -eq 'case' -and [string]::IsNullOrWhiteSpace($CaseId)) { throw 'Supply -CaseId for -Mode case.' }
    if ($Mode -eq 'area' -and [string]::IsNullOrWhiteSpace($Area)) { throw 'Supply -Area for -Mode area.' }
    if ($Mode -in @('all','case','area')) { Assert-RunnerSupportsWorkers }
    if ($Mode -eq 'case') {
        $env:FRMS_TARGET_CASE_ID = $CaseId
    } else {
        Remove-Item Env:FRMS_TARGET_CASE_ID -ErrorAction SilentlyContinue
    }
    if ($Mode -eq 'coverage') {
        $exitCode = Run-Pnpm @('run','test:frms:coverage')
    } else {
        Setup-TestEnvironment
        New-Item -ItemType Directory -Force -Path $Reports | Out-Null
        Start-BackendIfNeeded
        $preflight = Run-Pnpm @('run','test:frms:preflight')
        if ($preflight -ne 0) { throw 'FRMS preflight failed.' }
        if (-not $SkipCatalogDiscovery -and $Mode -in @('all','case','area')) {
            Resolve-CatalogFixture
        }
        if ($Mode -in @('all','case','area')) {
            Resolve-Batch12Fixtures
            # Only negative-login cases need this additional authenticated Admin scan.
            if ($Mode -ne 'case' -or $CaseId -in @('FRMS-ST-0199','FRMS-ST-0203')) {
                Resolve-VerifiedNegativeLoginFixtures
            }
            & node (Join-Path $Repo 'tests\system\scripts\batch12-env-audit.mjs') | Out-Host
            if ($LASTEXITCODE -ne 0) { throw 'Batch 12 mandatory environment audit failed.' }
        }
        if ($InstallBrowser) {
            $browser = Run-Pnpm @('--dir','tests/e2e/playwright','exec','playwright','install','chromium')
            if ($browser -ne 0) { throw 'Playwright Chromium installation failed.' }
        }
        switch ($Mode) {
            'preflight' { $exitCode = 0 }
            'catalog-probe' {
                if (-not (Test-Path 'tests\system\scripts\probe-catalog.mjs')) {
                    throw 'Catalog probe is missing. Apply Patch 06 before using -Mode catalog-probe.'
                }
                $exitCode = Run-Pnpm @('run','test:frms:catalog-probe')
            }
            'all' { $exitCode = Run-Pnpm @('run','test:frms:all') }
            'case' { $exitCode = Run-Pnpm @('run','test:frms:case','--','--id',$CaseId) }
            'area' { $exitCode = Run-Pnpm @('run','test:frms:all','--','--area',$Area) }
        }
    }
} catch {
    [Console]::Error.WriteLine("FRMS launcher error: " + $_.Exception.Message)
    $exitCode = 2
} finally {
    if ($null -ne $backendProcess) {
        $backendProcess.Refresh()
        if (-not $backendProcess.HasExited) {
            Write-Host "Stopping backend process tree PID=$($backendProcess.Id)..."
            & taskkill.exe /PID $backendProcess.Id /T /F 2>$null | Out-Null
        }
    }
    Write-Host "FRMS test launcher finished: exit code $exitCode."
}
exit $exitCode
 -or
                    $email -notmatch '^[^\s@]+@[^\s@]+\.[^\s@]+
    $scriptFile = Join-Path $Repo 'tests\system\scripts\batch12-discover.mjs'
    if (-not (Test-Path -LiteralPath $scriptFile)) {
        Write-Warning 'Batch 12 fixture discovery script not found; missing fixtures remain blocked.'
        return
    }
    try {
        $rawJson = & node $scriptFile
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($rawJson)) { throw 'Node discovery failed.' }
        $result = $rawJson | ConvertFrom-Json
        $count = 0
        foreach ($item in $result.fixtures.PSObject.Properties) {
            if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($item.Name, 'Process'))) {
                [Environment]::SetEnvironmentVariable($item.Name, [string]$item.Value, 'Process')
                $count++
            }
        }
        Write-Host "Batch 12 fixture discovery: $count verified value(s) supplied to this launcher process."
        foreach ($item in $result.roles.PSObject.Properties) {
            $state = if ($item.Value) { "verified" } else { "unavailable" }
            Write-Host "  $($item.Name): $state"
        }
        foreach ($note in @($result.notes)) { Write-Warning "Batch 12 fixture: $note" }
        & node (Join-Path $Repo 'tests\system\scripts\batch12-env-audit.mjs') | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Batch 12 mandatory environment audit failed.' }
    } catch {
        Write-Warning ("Batch 12 fixture discovery unavailable: " + $_.Exception.Message)
        Write-Warning 'Unresolved fixtures remain SKIPPED/Blocked, not PASS.'
    }
}

try {
    Set-Location -LiteralPath $Repo
    Assert-Prerequisites
    if ($Mode -eq 'case' -and [string]::IsNullOrWhiteSpace($CaseId)) { throw 'Supply -CaseId for -Mode case.' }
    if ($Mode -eq 'area' -and [string]::IsNullOrWhiteSpace($Area)) { throw 'Supply -Area for -Mode area.' }
    if ($Mode -in @('all','case','area')) { Assert-RunnerSupportsWorkers }
    if ($Mode -eq 'coverage') {
        $exitCode = Run-Pnpm @('run','test:frms:coverage')
    } else {
        Setup-TestEnvironment
        New-Item -ItemType Directory -Force -Path $Reports | Out-Null
        Start-BackendIfNeeded
        $preflight = Run-Pnpm @('run','test:frms:preflight')
        if ($preflight -ne 0) { throw 'FRMS preflight failed.' }
        if (-not $SkipCatalogDiscovery -and $Mode -in @('all','case','area')) {
            Resolve-CatalogFixture
        }
        if ($Mode -in @('all','case','area')) { Resolve-Batch12Fixtures }
        if ($InstallBrowser) {
            $browser = Run-Pnpm @('--dir','tests/e2e/playwright','exec','playwright','install','chromium')
            if ($browser -ne 0) { throw 'Playwright Chromium installation failed.' }
        }
        switch ($Mode) {
            'preflight' { $exitCode = 0 }
            'catalog-probe' {
                if (-not (Test-Path 'tests\system\scripts\probe-catalog.mjs')) {
                    throw 'Catalog probe is missing. Apply Patch 06 before using -Mode catalog-probe.'
                }
                $exitCode = Run-Pnpm @('run','test:frms:catalog-probe')
            }
            'all' { $exitCode = Run-Pnpm @('run','test:frms:all') }
            'case' { $exitCode = Run-Pnpm @('run','test:frms:case','--','--id',$CaseId) }
            'area' { $exitCode = Run-Pnpm @('run','test:frms:all','--','--area',$Area) }
        }
    }
} catch {
    [Console]::Error.WriteLine("FRMS launcher error: " + $_.Exception.Message)
    $exitCode = 2
} finally {
    if ($null -ne $backendProcess) {
        $backendProcess.Refresh()
        if (-not $backendProcess.HasExited) {
            Write-Host "Stopping backend process tree PID=$($backendProcess.Id)..."
            & taskkill.exe /PID $backendProcess.Id /T /F 2>$null | Out-Null
        }
    }
    Write-Host "FRMS test launcher finished: exit code $exitCode."
}
exit $exitCode
) {
                    throw 'Verified response has invalid identifier formats'
                }
                [Environment]::SetEnvironmentVariable($negativeKeys[0], $phone, 'Process')
                [Environment]::SetEnvironmentVariable($negativeKeys[1], $email, 'Process')
                $verified = $true
                Write-Host ("Verified negative login fixtures: phone and email absent from " +
                    $result.accountCount + " complete Admin account records; no values logged.")
            } else {
                $reason = [string]$result.reason
            }
        } catch {
            $reason = $_.Exception.Message
        }
    }
    if (-not $verified) {
        # A caller override must also be verified; otherwise SKIP both negative cases.
        foreach ($key in $negativeKeys) {
            [Environment]::SetEnvironmentVariable($key, $null, 'Process')
        }
        Write-Warning ("Unregistered login fixtures not verified (" + $reason +
            "). FRMS-ST-0199 and FRMS-ST-0203 remain Blocked.")
    }
}

function Resolve-Batch12Fixtures {
    $scriptFile = Join-Path $Repo 'tests\system\scripts\batch12-discover.mjs'
    if (-not (Test-Path -LiteralPath $scriptFile)) {
        Write-Warning 'Batch 12 fixture discovery script not found; missing fixtures remain blocked.'
        return
    }
    try {
        $rawJson = & node $scriptFile
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($rawJson)) { throw 'Node discovery failed.' }
        $result = $rawJson | ConvertFrom-Json
        $count = 0
        foreach ($item in $result.fixtures.PSObject.Properties) {
            if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($item.Name, 'Process'))) {
                [Environment]::SetEnvironmentVariable($item.Name, [string]$item.Value, 'Process')
                $count++
            }
        }
        Write-Host "Batch 12 fixture discovery: $count verified value(s) supplied to this launcher process."
        foreach ($item in $result.roles.PSObject.Properties) {
            $state = if ($item.Value) { "verified" } else { "unavailable" }
            Write-Host "  $($item.Name): $state"
        }
        foreach ($note in @($result.notes)) { Write-Warning "Batch 12 fixture: $note" }
        & node (Join-Path $Repo 'tests\system\scripts\batch12-env-audit.mjs') | Out-Host
        if ($LASTEXITCODE -ne 0) { throw 'Batch 12 mandatory environment audit failed.' }
    } catch {
        Write-Warning ("Batch 12 fixture discovery unavailable: " + $_.Exception.Message)
        Write-Warning 'Unresolved fixtures remain SKIPPED/Blocked, not PASS.'
    }
}

try {
    Set-Location -LiteralPath $Repo
    Assert-Prerequisites
    if ($Mode -eq 'case' -and [string]::IsNullOrWhiteSpace($CaseId)) { throw 'Supply -CaseId for -Mode case.' }
    if ($Mode -eq 'area' -and [string]::IsNullOrWhiteSpace($Area)) { throw 'Supply -Area for -Mode area.' }
    if ($Mode -in @('all','case','area')) { Assert-RunnerSupportsWorkers }
    if ($Mode -eq 'coverage') {
        $exitCode = Run-Pnpm @('run','test:frms:coverage')
    } else {
        Setup-TestEnvironment
        New-Item -ItemType Directory -Force -Path $Reports | Out-Null
        Start-BackendIfNeeded
        $preflight = Run-Pnpm @('run','test:frms:preflight')
        if ($preflight -ne 0) { throw 'FRMS preflight failed.' }
        if (-not $SkipCatalogDiscovery -and $Mode -in @('all','case','area')) {
            Resolve-CatalogFixture
        }
        if ($Mode -in @('all','case','area')) { Resolve-Batch12Fixtures }
        if ($InstallBrowser) {
            $browser = Run-Pnpm @('--dir','tests/e2e/playwright','exec','playwright','install','chromium')
            if ($browser -ne 0) { throw 'Playwright Chromium installation failed.' }
        }
        switch ($Mode) {
            'preflight' { $exitCode = 0 }
            'catalog-probe' {
                if (-not (Test-Path 'tests\system\scripts\probe-catalog.mjs')) {
                    throw 'Catalog probe is missing. Apply Patch 06 before using -Mode catalog-probe.'
                }
                $exitCode = Run-Pnpm @('run','test:frms:catalog-probe')
            }
            'all' { $exitCode = Run-Pnpm @('run','test:frms:all') }
            'case' { $exitCode = Run-Pnpm @('run','test:frms:case','--','--id',$CaseId) }
            'area' { $exitCode = Run-Pnpm @('run','test:frms:all','--','--area',$Area) }
        }
    }
} catch {
    [Console]::Error.WriteLine("FRMS launcher error: " + $_.Exception.Message)
    $exitCode = 2
} finally {
    if ($null -ne $backendProcess) {
        $backendProcess.Refresh()
        if (-not $backendProcess.HasExited) {
            Write-Host "Stopping backend process tree PID=$($backendProcess.Id)..."
            & taskkill.exe /PID $backendProcess.Id /T /F 2>$null | Out-Null
        }
    }
    Write-Host "FRMS test launcher finished: exit code $exitCode."
}
exit $exitCode
