# FRMS TEST_SWT - Patch 11: fix Playwright trace scope

## Cause

Patch 10 placed `test.use({ trace: 'off' })` inside `test.describe` in `tests/e2e/playwright/tests/frms/auth-next.spec.ts`. Playwright rejects the worker-scoped `trace` setting inside a describe group. This causes 0 executed tests even though 51 were selected.

## Fix

Move `test.use({ trace: 'off' })` to the file's top level, after `test.setTimeout(90_000)`. Remove the nested directive. Do not alter the two functional test bodies or environment settings. All tests in this specific file will have traces disabled, protecting the signed JWT and demo login credentials; tests in other files retain the project trace configuration (`retain-on-failure`).

## CMD instructions

Extract the ZIP into the repository root. Patch 10 must already be applied.

```bat
git apply --check .\11_FRMS_TRACE_SCOPE_FIX.patch
git apply .\11_FRMS_TRACE_SCOPE_FIX.patch
```

Run two cases individually:

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode case -CaseId FRMS-ST-0147 -Workers 3 -Retries 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode case -CaseId FRMS-ST-0153 -Workers 3 -Retries 1
```

Then, after the cases pass:

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode all -Workers 4 -Retries 1
```

No new environment variables. Expected: Playwright now starts executing tests. No guarantee that 0147 and 0153 pass; if they still fail inspect actual route/HTTP behavior. After all implemented tests pass, `Mode all` may return exit 3 for incomplete registry coverage.

## Validation

Validated patch applies to the exact `auth-next.spec.ts` from Patch 10, and that the resulting file has a single top-level trace setting with none inside `test.describe`. Browser execution is not available in this environment.
