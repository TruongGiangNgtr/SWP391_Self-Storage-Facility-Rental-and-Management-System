# FRMS Batch 12 — Verified negative-login fixture preflight

## Problem

`FRMS_TEST_ASSIGNABLE_RESERVATION_ID` is genuinely absent from the supplied 2026-11-11 database snapshot (no matching CONFIRMED + CHECKED_IN + AVAILABLE unit tuple for Facility 01). **Do not set it to an unrelated Reservation.** Leave FRMS-ST-0065 Blocked/Skipped until valid isolated test data are provisioned and verified.

The other two missing keys are negative login inputs and should be verified absent from the *entire* Admin user directory before use:

- `FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE` — needed by FRMS-ST-0199.
- `FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL` — needed by FRMS-ST-0203.

The sanitized DB snapshot excluded phone/email data, so its lack of those fields does not prove absence. Initial wrapper environment audit runs before the original launcher discovery, causing misleading `MISSING` entries.

## Install (CMD, repo root, branch TEST_SWT)

Extract `FRMS_NEGATIVE_FIXTURE_PREFLIGHT` into the root beside `RUN_FRMS_TESTS.ps1`, then:

```bat
node ".\FRMS_NEGATIVE_FIXTURE_PREFLIGHT\install.mjs"
.\RUN_FRMS_TESTS.cmd -AuditOnly
.\RUN_FRMS_TESTS.cmd -Mode all -Workers 4 -Retries 1
```

The wrapper runs a Node helper before audit using the configured Admin demo account. It authenticates with POST `/api/v1/auth/employee/login` (this may generate normal LoginHistory), verifies `SYSTEM_ADMINISTRATOR` ACTIVE via GET `/api/v1/auth/me`, then enumerates GET `/api/v1/admin/users?page=...&pageSize=100` exhaustively. It selects a syntactically valid phone and email only if absent from the full directory. It prints **only** verified negative candidates and the account count, never existing accounts, JWT or connection strings. Its values live in the PowerShell process and flow to the child launcher/Playwright; no `.env` is modified.

The helper does not create any account or perform business-domain writes. If Admin login, permissions, paging or API health fail, both keys remain missing and test cases are Blocked—not PASS. Preflight validates absence at inspection time, not indefinitely; a future signup could invalidate a chosen identifier.

To avoid even a login-history write, do not run the preflight against a shared DB; run in an authorized isolated Testing environment or leave negative tests Blocked. The wrapper will still run other read-only/validation tests with the existing no-state-change flags. Existing backend processes and jobs are not controlled by these flags.

## Files and rollback

- `batch12-verify-unregistered.mjs` is copied to `tests/system/scripts/`.
- `RUN_FRMS_TESTS.ps1` gets one idempotent pre-audit hook; a backup is saved as `RUN_FRMS_TESTS.ps1.negative-fixtures.bak`.
- No production source, auth data, or `.env` changes.
- To roll back, review `git diff`, then restore the `.negative-fixtures.bak` backup if no additional intended runner changes were made.
