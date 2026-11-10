# FRMS Test Runner V8 - Fix for PowerShell preflight false failures

## Root cause

`Run-Pnpm` originally invoked `pnpm.cmd` directly. Because the caller assigns
`$preflight = Run-Pnpm ...`, PowerShell captures all native stdout in addition
to the numeric exit code. The resulting array is not equal to numeric zero,
so the launcher throws `FRMS preflight failed` even when `preflight.mjs` succeeds.
The log's `API /health: Healthy` is consistent with this explanation.

## Fix

Redirect native stdout to the host inside the helper:

```powershell
& $pnpmExe @Arguments | Out-Host
$result = $LASTEXITCODE
```

`Run-Pnpm` now emits exactly one value, the numeric exit status. All test
output remains visible. This is a launcher fix; the backend, Playwright tests,
and coverage registry are unchanged.

## Installation

1. Extract `run-frms-tests.ps1` into the root of your `TEST_SWT` repository,
   **replacing** the V7 script. There is no need to reapply Patch 07.
2. From **CMD** in the repository root, execute the one-line command:

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode all -Workers 4 -Retries 1
```

## Expected check

After `API /health: Healthy`, you should see both:

```text
PREFLIGHT OK: Testing API http://127.0.0.1:5164 is healthy; web UI will start via Playwright.
```

and subsequently:

```text
Running 30 tests using 4 workers
```

The latter depends on the existing implementation count; additional test
failures/blocked fixtures are possible and are unrelated to this launcher bug.

## Caveats

Do not run more than one launcher concurrently against the same DB.
The backend connection appears to point to a remote/shared database: `Testing`
does not isolate it, and reservation-expiry background processing may still
be active. Do not run destructive scenarios without an isolated test database.
