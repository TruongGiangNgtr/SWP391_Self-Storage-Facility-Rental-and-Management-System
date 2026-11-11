# FRMS test tools — TEST_SWT

These four tools were relocated from repository root to **`test/tools/`** on 2026-11-11. The move is organizational: it **does not** install a patch, alter the active Playwright tests, or change the database. The actual test suite remains under `tests/` (plural). AI handoff information is in [`../handoff/`](../handoff/README.md).

## Tool index

| Directory | Purpose | Status |
| --- | --- | --- |
| `FRMS_DB_PROFILE_EXPORT/` | Read-only, sanitized PostgreSQL metadata export for test-fixture profiling | Utility: may be run with authorized DB credentials |
| `FRMS_BATCH12_FIXTURE_AUTO/` | Original Batch 12 fixture auto-discovery installer and source | Historical patch; **do not reapply** onto newer canonical discovery |
| `FRMS_BATCH12_FIXTURE_REFINEMENT/` | Refinement of missing fixture discovery and diagnostic messages | Historical patch; **do not downgrade** a newer discovery script |
| `FRMS_BATCH12_FINAL2_FIX/` | Later staff scope/discovery patch with SHA256 installer guards | Optional patch candidate; check installed code and fixture preconditions first |

Each package's README describes its intended behavior and historical limitations. **Being in this directory does not mean an installer has been run.** Package source and checksums are intentionally kept unchanged during relocation, except for documented path updates in the READMEs.

## Working directory requirements

Run commands **from the repository root**, on branch `TEST_SWT`. The Node installers resolve `process.cwd()` to locate the active launcher and Playwright sources. Do not run the installer after `cd test\tools`.

```bat
git branch --show-current
git status --short
```

## Read-only PostgreSQL profile exporter

```bat
.\test\tools\FRMS_DB_PROFILE_EXPORT\RUN_EXPORT.cmd
```

The exporter walks parent directories to find `backend/Frms.Api/.env`, so the new nested location works without editing the Python database connection logic. Its default output is in the Git-ignored repository directory `test/reports/db-profile/` (ZIP and summary JSON). Review the sanitized output before sharing; identifiers still merit restricted access.

**Python dependency handling:** `RUN_EXPORT.cmd` automatically creates/reuses `%TEMP%\frms-db-profile-export-venv`, installs `psycopg[binary]` there when missing, and runs the exporter from that venv. The broken system Python 3.14 pip is not used. No manual global pip installation is needed. This setup needs working `py -3 -m venv` and network access to install the package the first time.

## Historical patch entrypoints — reference only

Do **not** run these in sequence and do **not** run them by default. They mutate installed launcher/test script files on the local worktree (not the business DB), with version-specific checksum or source-layout checks. Review current canonical script versions first:

```bat
rem ORIGINAL discovery hook:
node .\test\tools\FRMS_BATCH12_FIXTURE_AUTO\install.mjs

rem Subsequent refinement:
node .\test\tools\FRMS_BATCH12_FIXTURE_REFINEMENT\install.mjs

rem Later scoped discovery and staff-test fix:
node .\test\tools\FRMS_BATCH12_FINAL2_FIX\install.mjs
```

If the installer reports a checksum mismatch, stop and review `git diff` and `tests/system/scripts/batch12-discover.mjs`. Do not force-overwrite an unknown version. The latest standalone F32 patch discussed outside this repo is not automatically available by moving these folders.

## Canonical test runner (unchanged)

```bat
.\RUN_FRMS_TESTS.cmd -AuditOnly
.\RUN_FRMS_TESTS.cmd -Mode coverage
.\RUN_FRMS_TESTS.cmd -Mode all -Workers 4 -Retries 1
```

See [`tests/system/README.md`](../../tests/system/README.md) for full context. The shared Supabase database is **not isolated** by setting `Testing`. Preserve Blocked status for `FRMS-ST-0065` without a verified eligible reservation/unit tuple and never use real state writes to manufacture one.
