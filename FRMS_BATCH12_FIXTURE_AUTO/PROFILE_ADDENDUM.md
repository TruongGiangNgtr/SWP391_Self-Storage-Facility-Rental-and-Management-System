# profile.md — Batch 12 fixture update (2026-11-11)

The environment audit reported 20 explicit missing fixture keys. The uploaded public Facility API snapshot identifies:

- `FRMS_TEST_HANOI_FACILITY_ID=f0000000-0000-0000-0000-000000000001`
- `FRMS_TEST_HANOI_FACILITY_NAME=FRMS Facility - Hà Nội`

These are verified reference records as of the snapshot, not proof of next-month capacity or live run status. The other 18 keys require authorized live API reads; no UUID is to be invented. A `FRMS_BATCH12_FIXTURE_AUTO` install package adds a hook to `run-frms-tests.ps1` that discovers resource IDs after the backend becomes healthy, confirms login roles via `/auth/me`, and runs `batch12-env-audit.mjs` in the same PowerShell process before Playwright. This hook performs only login POSTs plus GETs; login may create normal audit/login-history records. It does not change business-state records or enable destructive flags. Unresolvable fixtures remain SKIPPED/Blocked. `101/275` implemented does not imply all tests passed.
