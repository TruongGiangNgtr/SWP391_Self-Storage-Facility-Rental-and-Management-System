#requires -version 5.1
<#
  FRMS TEST_SWT read-only/validation automation runner.
  Uses the existing run-frms-tests.ps1 launcher. DOES NOT modify source/DB.
  Snapshot fixture baseline: user-provided 2026-11-11T06:30:28Z PostgreSQL export.

  Examples:
    .\RUN_FRMS_TESTS.cmd
    .\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0165 -Workers 1 -Retries 0
    .\RUN_FRMS_TESTS.cmd -AuditOnly
    .\RUN_FRMS_TESTS.cmd -LiveDiscoveryOnly
    .\RUN_FRMS_TESTS.cmd -StartBackend -Mode all

  IMPORTANT: -StartBackend may launch scheduled background jobs, including
  reservation expiration, and must be authorized for the configured DB.
#>
[CmdletBinding()]
param(
    [ValidateSet('all','case','area','coverage','preflight','catalog-probe')]
    [string]$Mode = 'all',
    [string]$CaseId,
    [string]$Area,
    [ValidateRange(1,16)][int]$Workers = 4,
    [ValidateRange(0,3)][int]$Retries = 1,
    [switch]$AuditOnly,
    [switch]$LiveDiscoveryOnly,
    [switch]$StartBackend,
    [switch]$InstallBrowser,
    [switch]$SkipCatalogDiscovery
)

$ErrorActionPreference = 'Stop'
$Repo = Split-Path -Parent $MyInvocation.MyCommand.Path
$Launcher = Join-Path $Repo 'run-frms-tests.ps1'
$Audit = Join-Path $Repo 'tests\system\scripts\batch12-env-audit.mjs'
$Coverage = Join-Path $Repo 'tests\system\scripts\validate-coverage.mjs'
$Discovery = Join-Path $Repo 'tests\system\scripts\batch12-discover.mjs'
$Reports = Join-Path $Repo 'tests\system\reports'

function Set-DefaultEnv([string]$Name, [string]$Value) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($Name,'Process'))) {
        [Environment]::SetEnvironmentVariable($Name,$Value,'Process')
    }
}

function Require-File([string]$File, [string]$Description) {
    if (-not (Test-Path -LiteralPath $File -PathType Leaf)) {
        throw "$Description missing: $File"
    }
}

try {
    Set-Location -LiteralPath $Repo
    Require-File $Launcher 'Main FRMS launcher'
    # Fail early if the child launcher contains malformed PowerShell syntax.
    # This also runs in -AuditOnly mode; it never starts the backend.
    $parseTokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile(
        $Launcher, [ref]$parseTokens, [ref]$parseErrors) | Out-Null
    if ($parseErrors.Count -gt 0) {
        $first = $parseErrors[0]
        throw ("Main FRMS launcher syntax error at line " +
            $first.Extent.StartLineNumber + ": " + $first.Message)
    }
    Write-Host 'PowerShell launcher syntax: OK.'
    Require-File $Audit 'Batch 12 environment audit'
    Require-File $Coverage 'Coverage validator'
    if (-not (Get-Command node -ErrorAction SilentlyContinue)) { throw 'Node.js is not available in PATH.' }
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Git is not available in PATH.' }
    $branch = (& git -C $Repo branch --show-current 2>$null)
    if ($LASTEXITCODE -ne 0 -or ($branch | Out-String).Trim() -ne 'TEST_SWT') {
        throw "Expected TEST_SWT branch, found: $branch. Never switch branches automatically."
    }
    if ($Mode -eq 'case' -and [string]::IsNullOrWhiteSpace($CaseId)) { throw 'Specify -CaseId for -Mode case.' }
    if ($Mode -eq 'area' -and [string]::IsNullOrWhiteSpace($Area)) { throw 'Specify -Area for -Mode area.' }

    # Process-local Testing and Playwright environment (nothing is persisted to Windows).
    $env:ASPNETCORE_ENVIRONMENT = 'Testing'
    $env:DOTNET_ENVIRONMENT = 'Testing'
    $env:ASPNETCORE_URLS = 'http://127.0.0.1:5164'
    $env:FRMS_ENVIRONMENT = 'Testing'
    $env:FRMS_API_URL = 'http://127.0.0.1:5164'
    $env:FRMS_UI_URL = 'http://127.0.0.1:4173'
    $env:VITE_API_PROXY_TARGET = 'http://127.0.0.1:5164'
    $env:CI = 'true'
    $env:FRMS_PLAYWRIGHT_WORKERS = [string]$Workers
    $env:FRMS_PLAYWRIGHT_RETRIES = [string]$Retries
    if ($env:NODE_ENV -eq 'production') { Remove-Item Env:NODE_ENV -ErrorAction SilentlyContinue }

    # Explicitly disable application-state mutation tests. This does not prove DB isolation.
    $env:FRMS_TEST_DB_ISOLATED = 'false'
    $env:FRMS_ALLOW_STATE_CHANGES = 'NO'
    $env:FRMS_ALLOW_AI_CALLS = 'NO'
    $env:BackgroundJobs__Billing__Enabled = 'false'
    $env:BackgroundJobs__NoShow__Enabled = 'false'
    $env:BackgroundJobs__Notifications__Enabled = 'false'

    # Demo-only accounts. Customer login uses phone; employee login uses email.
    # Locally supplied overrides have priority, including passwords.
    Set-DefaultEnv 'FRMS_TEST_DEMO_PASSWORD' '11111111'
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_1_PHONE' '0901000001'
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_2_PHONE' '0901000002'
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_3_PHONE' '0901000003'
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_1_PASSWORD' $env:FRMS_TEST_DEMO_PASSWORD
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_2_PASSWORD' $env:FRMS_TEST_DEMO_PASSWORD
    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_3_PASSWORD' $env:FRMS_TEST_DEMO_PASSWORD
    Set-DefaultEnv 'FRMS_TEST_STAFF_01_EMAIL' 'staff.f01@example.com'
    Set-DefaultEnv 'FRMS_TEST_STAFF_PASSWORD' $env:FRMS_TEST_DEMO_PASSWORD
    Set-DefaultEnv 'FRMS_TEST_FOREIGN_STAFF_EMAIL' 'staff.f32@example.com'
    Set-DefaultEnv 'FRMS_TEST_FOREIGN_STAFF_PASSWORD' $env:FRMS_TEST_STAFF_PASSWORD
    Set-DefaultEnv 'FRMS_TEST_MANAGER_01_EMAIL' 'manager.f01@example.com'
    Set-DefaultEnv 'FRMS_TEST_MANAGER_PASSWORD' $env:FRMS_TEST_DEMO_PASSWORD
    Set-DefaultEnv 'FRMS_TEST_BOM_EMAIL' 'bom@example.com'
    Set-DefaultEnv 'FRMS_TEST_BOM_PASSWORD' $env:FRMS_TEST_DEMO_PASSWORD
    Set-DefaultEnv 'FRMS_TEST_ADMIN_EMAIL' 'admin@example.com'
    Set-DefaultEnv 'FRMS_TEST_ADMIN_PASSWORD' $env:FRMS_TEST_DEMO_PASSWORD

    # Inherited catalogue metadata. The launcher re-checks real facility capacity.
    Set-DefaultEnv 'FRMS_TEST_AVAILABLE_UNIT_TYPE_ID' 'b1000000-0000-0000-0000-000000000003'
    Set-DefaultEnv 'FRMS_TEST_AVAILABLE_UNIT_NAME' 'Shared Public Locker (5x5)'
    Set-DefaultEnv 'FRMS_TEST_AVAILABLE_UNIT_MODE' 'PUBLIC'
    Set-DefaultEnv 'FRMS_TEST_AVAILABLE_UNIT_SIZE' '3 m² equivalent (Locker)'
    Set-DefaultEnv 'FRMS_TEST_AVAILABLE_UNIT_RENT_VND' '3500'

    if (-not $LiveDiscoveryOnly) {
        # Snapshot-backed candidate fixtures from Nov 11 06:30:28Z DB export.
        # FRMS-ST-0065 is strictly read-only: an ACTIVE assignment already exists.
        # Defaults are not current-runtime verification; discovery/test assertions must
        # validate ownership, facility, type, status and authorization again.
        $snapshotFixtures = [ordered]@{
            'FRMS_TEST_ACTIVE_CONTRACT_ID' = '91000000-0000-0000-0000-000000000001'
            'FRMS_TEST_ASSIGNABLE_RESERVATION_ID' = 'f1112fee-e99f-4094-879a-563ddd1f8f81'
            'FRMS_TEST_AVAILABLE_PHYSICAL_UNIT_ID' = 'fa72ca32-22d6-496c-aca2-20e64c040de7'
            'FRMS_TEST_CUSTOMER_2_INVOICE_ID' = '83000000-0000-0000-0000-000000000003'
            'FRMS_TEST_CUSTOMER_2_RESERVATION_ID' = '81000000-0000-0000-0000-000000000002'
            'FRMS_TEST_CUSTOMER_2_SUPPORT_TICKET_ID' = 'ffcfa55e-9512-4f62-9f83-34ba8c15b98d'
            'FRMS_TEST_HANOI_FACILITY_ID' = 'f0000000-0000-0000-0000-000000000001'
            'FRMS_TEST_HANOI_FACILITY_NAME' = 'FRMS Facility - Hà Nội'
            'FRMS_TEST_OTHER_FACILITY_REFERENCE_ID' = '33a922ce-416f-4757-87f5-8f85aba047ef'
            'FRMS_TEST_OWN_DEPOSIT_INVOICE_ID' = '2383e8b2-2f51-43c7-88cb-def8a359fe2b'
            'FRMS_TEST_OWN_PAID_INVOICE_ID' = '83000000-0000-0000-0000-000000000001'
            'FRMS_TEST_OWN_RESERVATION_ID' = 'b370c765-adb7-4e16-aa19-54caf7d5b788'
            'FRMS_TEST_PRICING_UNIT_TYPE_NAME' = 'Medium Private (5x10)'
            'FRMS_TEST_STAFF_HANDOVER_RESERVATION_ID' = 'f1112fee-e99f-4094-879a-563ddd1f8f81'
            'FRMS_TEST_STAFF_HANDOVER_VISIT_ID' = '6855fba5-942c-40a4-bf22-5658ca3be00c'
            'FRMS_TEST_STAFF_IN_PROGRESS_INSPECTION_ID' = '8284175e-729e-4070-931e-3d44deb21ec0'
            'FRMS_TEST_STAFF_OWN_REFERENCE_ID' = '0c5eb20c-ec50-4e2f-a633-e0cea07eeadf'
            'FRMS_TEST_STAFF_SCOPE_DATE' = '2026-12-01'
        }
        foreach ($name in $snapshotFixtures.Keys) { Set-DefaultEnv $name $snapshotFixtures[$name] }
        Set-DefaultEnv 'FRMS_TEST_OTHER_FACILITY_SCOPE_DATE' '2026-12-05'
        # This Contract is owned by Customer 1 and TERMINATED in the snapshot.
        Set-DefaultEnv 'FRMS_TEST_TERMINATED_CONTRACT_ID' 'b2447e82-3493-44e7-8678-cd9255ea8d72'
    }

    # Reject known-ineligible candidates; the newly eligible READ-ONLY ST-0065 fixture
    # is intentionally not rejected. It has an existing ACTIVE assignment.
    # Other custom values are left untouched but MUST be runtime verified.
    $knownIneligible = @(
        'b370c765-adb7-4e16-aa19-54caf7d5b788',
        'bc9b2c4c-9d74-4f16-b0d6-73156a42fe85'
    )
    if ($env:FRMS_TEST_ASSIGNABLE_RESERVATION_ID -in $knownIneligible) {
        Remove-Item Env:FRMS_TEST_ASSIGNABLE_RESERVATION_ID -ErrorAction SilentlyContinue
        Write-Warning 'Cleared a known-ineligible assignable reservation ID. Case FRMS-ST-0065 will be Blocked.'
    }

    # Intentionally NOT invented from the snapshot:
    # - FRMS_TEST_ASSIGNABLE_RESERVATION_ID: snapshot has an F01 matching tuple,
    #   but it is already linked by an ACTIVE assignment; read-only checks ONLY.
    # - FRMS_TEST_STAFF_HANDOVER_VISIT_ID: currently CHECKED_IN, not SCHEDULED;
    #   FRMS-ST-0067 cannot use it for a pre-check-in fixture.
    # - FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE, FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL:
    #   excluded PII; existing API discovery must verify they do not exist.
    # - FRMS_TEST_NO_CCCD_PHONE/PASSWORD, COMPLETED_CONTRACT_ID: not established.

    Write-Host 'FRMS TEST_SWT runner: process-local Testing environment prepared.'
    Write-Host "Mode=$Mode; workers=$Workers; retries=$Retries; snapshotDefaults=$(-not $LiveDiscoveryOnly)."
    Write-Host 'Demo actor identities: Customer1/2/3, Staff F01/F32, Manager F01, BOM, Admin.'
    Write-Host 'Writes disabled: FRMS_TEST_DB_ISOLATED=false; FRMS_ALLOW_STATE_CHANGES=NO; AI calls disabled.'
    if (-not $LiveDiscoveryOnly) {
        Write-Warning 'Fixture defaults are from Nov 11, 2026. They MUST match current API/DB state to be valid.'
    }
    if (-not $env:FRMS_TEST_ASSIGNABLE_RESERVATION_ID) {
        Write-Warning 'FRMS-ST-0065 fixture not set. Set a verified F01 matching reservation for read-only checks; never fabricate an ID.'
    } else {
        Write-Warning 'FRMS-ST-0065 matching Reservation candidate configured; verify Manager F01 GET, CHECKED_IN Visit, matching AVAILABLE Unit and ACTIVE assignment before PASS. No save/release.'
    }
    if (-not $StartBackend -and $Mode -notin @('coverage')) {
        Write-Host 'Safety default: the FRMS API must already be healthy at http://127.0.0.1:5164/health.'
        Write-Host 'To explicitly allow launcher backend startup, pass -StartBackend (review scheduled jobs and database target first).'
    }
    if (-not (Test-Path -LiteralPath $Discovery)) {
        Write-Warning 'Batch 12 discovery script not installed. Snapshot fallbacks are used; unregistered account fixture discovery may remain unavailable.'
    }

    if ($AuditOnly) {
        Write-Host '--- Pre-discovery environment audit (NO Admin directory check) ---'
        & node $Audit | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Batch 12 core environment audit failed (exit $LASTEXITCODE)." }
        Write-Host 'AuditOnly is a static key inventory. Unregistered phone/email require a live, authenticated Admin preflight.'
        Write-Host 'AuditOnly completed: no backend, Admin API verification or Playwright tests started.'
        exit 0
    }
    Write-Host 'Final fixture audit will run after backend preflight, discovery and verified Admin directory scan.'

    $launcherArgs = @('-NoProfile','-ExecutionPolicy','Bypass','-File',$Launcher,
        '-Mode',$Mode,'-Workers',[string]$Workers,'-Retries',[string]$Retries)
    if ($Mode -eq 'case') { $launcherArgs += @('-CaseId',$CaseId) }
    if ($Mode -eq 'area') { $launcherArgs += @('-Area',$Area) }
    if (-not $StartBackend) { $launcherArgs += '-SkipBackend' }
    if ($InstallBrowser) { $launcherArgs += '-InstallBrowser' }
    if ($SkipCatalogDiscovery) { $launcherArgs += '-SkipCatalogDiscovery' }
    Write-Host '--- Running existing FRMS launcher ---'
    & powershell.exe @launcherArgs
    $code = [int]$LASTEXITCODE
    Write-Host "Result directory: $Reports"
    Write-Host "FRMS wrapper exit code: $code"
    if ($code -eq 3) {
        Write-Warning 'Exit code 3 may reflect incomplete case registry/Blocked rows. Inspect latest summary.md and results.json; do not infer a Playwright failure.'
    }
    exit $code
} catch {
    [Console]::Error.WriteLine('FRMS wrapper error: ' + $_.Exception.Message)
    exit 2
}
