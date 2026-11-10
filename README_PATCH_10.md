# FRMS TEST_SWT - Patch 10: two authenticated-route test failures

Baseline: Patch 09 already applied (`51` implemented of `275` registered).
Observed run `20261110T145651711Z`: **49 passed, 2 failed** (`FRMS-ST-0147`, `FRMS-ST-0153`); no confirmed Jira defect.

## Diagnosis

- `0147`: the Account Settings heading was not visible after logging into `/customer` and doing a full navigation to `/customer/account-settings`.
- `0153`: navigation to `/staff/work-list` ended at `/auth/employee/login`, not `/forbidden` as expected for an authenticated Customer.
- `authToken.ts` keeps JWT in **sessionStorage**; HTTP 401 from any authenticated request clears it and logs out. `CustomerPortalPage` starts multiple unrelated API requests immediately after login. The supplied test log does NOT identify which request (if any) cleared the session. These failures are **unconfirmed**, not proven application defects.

## Patch scope

Only `tests/e2e/playwright/tests/frms/auth-next.spec.ts` is changed. Two tests now create a signed Customer session through the **real** `/auth/customer/login` API, check `/auth/me` returns an ACTIVE CUSTOMER, set the token once in same-origin `sessionStorage`, and then navigate **directly** to the route under test. This avoids Customer Portal background requests while preserving the real backend and frontend authorization flow.

If a protected route still redirects incorrectly after this setup, inspect the resulting report before treating it as a product defect. The patch does not force a PASS, stub network responses, reset your database, or change production code.

## Apply in CMD

Extract the ZIP to the repository root (do not overwrite other files). Run:

```bat
git branch --show-current
git apply --check .\10_FRMS_AUTH_ROUTE_SESSION_STABILITY.patch
git apply .\10_FRMS_AUTH_ROUTE_SESSION_STABILITY.patch
```

### Verify the two cases

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode case -CaseId FRMS-ST-0147 -Workers 3 -Retries 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode case -CaseId FRMS-ST-0153 -Workers 3 -Retries 1
```

### Full regression

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode all -Workers 4 -Retries 1
```

If all 51 implemented cases pass, `-Mode all` may still exit **3** because 224 registry cases remain unexecuted (`216` outstanding + `8` review). Exit code 3 is coverage, not an assertion failure.

## Environment

No **new** environment variables. Continue using `profile.md` and the configured launcher:

- `FRMS_API_URL=http://127.0.0.1:5164`
- `FRMS_UI_URL=http://127.0.0.1:4173`
- `FRMS_TEST_CUSTOMER_1_PHONE=0901000001`
- `FRMS_TEST_CUSTOMER_1_PASSWORD=11111111`
- `FRMS_ENVIRONMENT=Testing`, API `/health` Healthy, Vite proxy configured

**Security:** Access tokens are not printed and trace recording is disabled for these two cases to reduce the risk of JWT exposure in reports. Login requests may still produce audit events. Database is still remote/shared; no DB mutations are performed by these cases beyond normal login auditing.

**Validation performed:** `git apply --check`, apply/compare resulting file, TypeScript syntax transpilation (if compiler present). **Browser execution not available here**. Two-case PASS remains to be established on the user's Testing runtime.
