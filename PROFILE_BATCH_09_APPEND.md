# profile.md addendum - Batch 09

Date: 2026-11-10 (Asia/Ho_Chi_Minh)
Base: TEST_SWT with patches 01-08. Do not replace existing `profile.md`; this is the new section to append after a confirmed apply.

- Latest confirmed completed test runs: `FRMS-ST-0027` PASS and `FRMS-ST-0028` PASS individually after Patch 08. The earlier entire 30-case run had 28 PASS and two unconfirmed failures. A full 30/30 runtime pass has not been demonstrated.
- Patch 09 adds 21 executable case IDs and changes only Playwright test code + `tests/system/manifest/cases.json`.
- Registry after applying Patch 09: 275 total / 51 implemented / 216 outstanding / 8 review.
- Windows launch command (CMD, one line):
  `powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode all -Workers 3 -Retries 1`
- Environment remains `Testing`; API 127.0.0.1:5164, Vite UI 127.0.0.1:4173; Playwright 3 workers for shared remote API load and 1 retry. The Patch 08 launcher discovers current ACTIVE facility + positive-capacity UnitType across the tested two-month window.
- Existing Customer credentials: phone `0901000001`, password `11111111`. Staff: `staff.f01@example.com`, password `11111111`. These are authorized demo values; account status must be revalidated at runtime.
- No new environment variables for Batch 09 if the Patch 08 launcher is used. For future state-changing batches, request **separate test database and sandbox payment integration**.
- Static validation: `git apply --check`, registry coverage, 51 unique executable IDs and TypeScript parsing passed. Runtime not executed; do not label the new 21 as Pass.
- Jira: zero confirmed software defects from the supplied prior logs. Only export independently confirmed product defects, not fixture or harness failures.
