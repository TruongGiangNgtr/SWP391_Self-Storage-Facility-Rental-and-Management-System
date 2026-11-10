# FRMS Patch 08 - Fix catalog fixture discovery and wait (TEST_SWT)

## Root cause and scope

The V8 launcher picked the first facility from an UNFILTERED facility listing; FRMS API filters availability by requested month range BEFORE pagination. Therefore that fixture may not be present in the actual search results. Also FRMS-ST-0027/0028 used Playwright's 5-second default to assert results, while other catalogue cases already wait up to 45 seconds.

Patch 08:
- Selects an ACTIVE facility from the filtered two-month `/facilities` result page 1 and checks the same unit remains available for the one-month check. No fake DB seed.
- Sets `FRMS_TEST_ACTIVE_FACILITY_ID` and verified fixture fields. When unable to verify, case 0027 becomes Blocked instead of a fabricated product defect.
- Makes ST-0027 query that facility ID, while ST-0028 still tests the PUBLIC broad search with real month selections.
- Waits for catalogue API results or an explicit API failure, then expands the correct facility and validates unit name, mode, size, rent, capacity, and optional description.
- Includes actionable diagnostics if an expected facility is absent after the API returned results.

## Installation

Apply **after** Patches 01-07. Extract ZIP into repository root, overwriting `run-frms-tests.ps1` with the supplied V9 launcher. From Windows CMD:

```bat
 git switch TEST_SWT
 git apply --check .\08_FRMS_CATALOG_FIXTURE_AND_WAIT.patch
 git apply .\08_FRMS_CATALOG_FIXTURE_AND_WAIT.patch
 powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode case -CaseId FRMS-ST-0027 -Workers 3 -Retries 1
 powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode case -CaseId FRMS-ST-0028 -Workers 3 -Retries 1
```

Afterwards run the full suite if the two cases pass:

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ".\run-frms-tests.ps1" -Mode all -Workers 3 -Retries 1
```

Your last run logged 3 Playwright workers, despite an earlier request for 4. This patch does NOT override the chosen workers. Specify `-Workers 4` if desired, noting the remote API may saturate under higher concurrency.

## Verification and limitations

Verified `git apply --check` / `git apply` on a local baseline equivalent to Patch 06 + Runner V8; exact local Windows run and remote DB conditions remain unverified. This patch does not mutate database content or mark unconfirmed failures as Jira defects. If it prints `FRMS_FIXTURE_MISSING`, capture its diagnostic output and the associated Playwright trace. Recheck test-specific outputs before declaring PASS.
