# FRMS TEST_SWT - Batch 09 (21 new automated system test cases)

Date: 2026-11-10 (Asia/Ho_Chi_Minh)
Baseline: TEST_SWT with patches 01-08 already applied.
Source of configuration: `profile.md` plus the most recent confirmed Patch 08 runs of FRMS-ST-0027 and FRMS-ST-0028.

## 1. Scope and evidence

- New executable tests: 21
- Previous implemented cases: 30
- After patch: 51 implemented, 216 not implemented, 8 review, 275 unique registry IDs
- Assertion sources checked: the real TEST_SWT frontend (CustomerRegisterPage, CustomerLoginPage, EmployeeLoginPage, CustomerAccountSettingsPage, RequireAuth/RequireRole, PublicFacilityDetailPage, PublicStorageSearchPage, PublicUnitTypeList, CustomerSignInGate, FlowDialog), the backend public catalog filtering code, and the original registry expectedUI fields.
- Validation performed locally: git apply --check PASS, manifest/mapping checker PASS, 51 unique executable IDs PASS, TypeScript syntax parsing PASS.
- NOT performed: actual browser/runtime execution or full TypeScript module/type resolution against the complete source tree. Do not claim 51/51 PASS.

## 2. New case IDs

**Authentication & Security (14):**
FRMS-ST-0006, FRMS-ST-0009, FRMS-ST-0012, FRMS-ST-0014, FRMS-ST-0017, FRMS-ST-0018, FRMS-ST-0019, FRMS-ST-0147, FRMS-ST-0148, FRMS-ST-0150, FRMS-ST-0153, FRMS-ST-0154, FRMS-ST-0198, FRMS-ST-0202.

**Storage Search & AI Size Guide (5):**
FRMS-ST-0029, FRMS-ST-0039, FRMS-ST-0208, FRMS-ST-0209, FRMS-ST-0210.

**Reservation - Guest (2):**
FRMS-ST-0056, FRMS-ST-0211.

## 3. Apply the patch in Windows CMD

Extract this ZIP into your repo root on branch TEST_SWT; DO NOT overwrite your working `profile.md` or `run-frms-tests.ps1`.

```bat
git branch --show-current
git apply --check .\09_FRMS_NEXT_21_AUTOMATED_CASES.patch
git apply .\09_FRMS_NEXT_21_AUTOMATED_CASES.patch
pnpm run test:frms:coverage
```

Expected registry line after successful application:

```
Registry: 275 cases / 275 unique / 51 implemented / 8 review / 216 outstanding
```

Run an individual new case through your tested Patch 08 PowerShell launcher (from CMD):

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode case -CaseId FRMS-ST-0208 -Workers 3 -Retries 1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode case -CaseId FRMS-ST-0056 -Workers 3 -Retries 1
```

Run all executable cases:

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode all -Workers 3 -Retries 1
```

The `-Mode all` command should select 275 registry entries but execute only the 51 mapped automated tests. Inspect `tests/system/reports/<run-id>/results.json` and `summary.md`, not only the console's final exit code.

## 4. Required environment variables

**No NEW environment variables are required for Batch 09**, provided the existing Patch 08 runner is used and its catalog fixture discovery succeeds.

The following existing variables must be populated:

| Variable | Where it comes from | Use |
| --- | --- | --- |
| `ConnectionStrings__FrmsDb`, `Jwt__SigningKey` | backend `Frms.Api/.env` | Running real Testing backend |
| `FRMS_API_URL`, `FRMS_UI_URL`, `VITE_API_PROXY_TARGET`, `FRMS_ENVIRONMENT` | Launcher | API/UI/proxy and Testing mode |
| `FRMS_TEST_CUSTOMER_1_PHONE` | Profile default `0901000001` | Customer sign in |
| `FRMS_TEST_CUSTOMER_1_PASSWORD` | Profile default `11111111` | Auth/role/JWT tests |
| `FRMS_TEST_STAFF_01_EMAIL` | Profile default `staff.f01@example.com` | Staff sign in |
| `FRMS_TEST_STAFF_PASSWORD` | Profile default `11111111` | Auth/role tests |
| `FRMS_TEST_ACTIVE_FACILITY_ID`, `FRMS_TEST_ACTIVE_FACILITY_NAME` | Live catalog discovery | Facility/detail/guest tests |
| `FRMS_TEST_AVAILABLE_UNIT_TYPE_ID`, `FRMS_TEST_AVAILABLE_UNIT_NAME`, `FRMS_TEST_AVAILABLE_UNIT_SIZE`, `FRMS_TEST_AVAILABLE_UNIT_MODE`, `FRMS_TEST_AVAILABLE_UNIT_RENT_VND` | Catalog discovery (preference for PUBLIC locker) | Filtered catalog and guest tests |
| `FRMS_PLAYWRIGHT_WORKERS`, `FRMS_PLAYWRIGHT_RETRIES` | Launcher arguments | Parallelism/retries |

Verified previous catalog fixture: `FRMS Facility - An Giang` / `Shared Public Locker (5x5)` / 1 slot / 2026-12 to 2027-01. These are NOT fixed test expectations: the launcher must reverify them for the current business months before running.

Never place actual database/JWT/PayOS/SMTP credentials in a patch, test source, profile, or Jira file.

## 5. Safety and assertion limitations

- Positive registration and reservation-creation cases are **not included** in this batch because the current backend points to remote/shared PostgreSQL.
- Registration format cases deliberately invalidate an unrelated required field. This verifies frontend validation only and ensures no account-creation POST is sent. It is NOT proof of successful backend registration.
- `FRMS-ST-0019` reflects the actual HTML `maxLength=64`; normal typing of character 65 is prevented without requiring a submit-time validation message.
- Customer wrong-password feedback uses the visible customer copy (`The phone number or password is incorrect...`), not a raw error code.
- The three invalid-login tests can still create authentication/audit entries and might trigger rate limits or lockouts. Only run against authorized demo/test accounts.
- The guest reservation tests assert that clicking `Create Reservation` opens the sign-in gate and does not generate a state-changing API request. They do not create a reservation or payment.
- `FRMS-ST-0150` checks tampered JWT signature against a protected API using a real test-account token, without printing it.
- The source registry's `oracleApproval` remains `PENDING_SRS_REVIEW` for all new cases. An unconfirmed test failure is not automatically a Jira defect.

## 6. Fixtures to request for later write-capable batches

The next wave, covering successful Registration, Reservation, Payment, Handover, Return, and Renewal, requires a dedicated **isolated PostgreSQL Testing database** with disposable seeded accounts/inventory and controlled background jobs, and for payment flows a payOS sandbox/test integration. Do NOT substitute the current shared Supabase database or auto-reset it.

When an isolated environment is available, provide its configuration locally via `.env` (without posting secrets to chat) and identify which account/facility/slot data are disposable. This is NOT a prerequisite for applying Batch 09.
