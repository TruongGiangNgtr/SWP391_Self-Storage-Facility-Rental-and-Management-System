# FRMS / TEST_SWT — Patch 04: Windows process launch fix

## Scope

Updates only `tests/system/scripts/run.mjs` after Patches 01–03.

Fixes Windows failure `DEP0190` / `'C:\Program' is not recognized`: the runner previously used Node `spawn(..., { shell: true })`, which misparsed paths containing spaces. The new runner uses `shell: false` and executes `@playwright/test/cli.js` using `process.execPath` from the local Playwright workspace. The patch also reports missing CLI as a blocked runner condition instead of treating it as a product defect.

## Apply (PowerShell; repository root)

Unzip this archive **directly into the repository root** (branch `TEST_SWT`).

```powershell
git branch --show-current
git apply --check .\04_FRMS_WINDOWS_PLAYWRIGHT_RUNNER_FIX.patch
git apply .\04_FRMS_WINDOWS_PLAYWRIGHT_RUNNER_FIX.patch
pnpm run test:frms:coverage
pnpm run test:frms:case -- --id FRMS-ST-0002
```

Do not reapply Patches 01–03. Do not import old reports as verified defects. `FRMS-ST-0002` checks a public login form; it should not mutate the database. Ensure API /health and preflight are already ready and avoid running state-changing tests against a shared remote database.

If Playwright browser is missing, run `pnpm --dir tests/e2e/playwright run install:browsers` and retry.

## Verification

- `git apply --check`: passed against local workspace with 01–03 applied.
- `node --check`: passed.
- Mock-run with local fake health and fake Playwright CLI: 1 selected, 1 parsed, Jira 0 defects. **This was not a live Playwright/browser execution.**
