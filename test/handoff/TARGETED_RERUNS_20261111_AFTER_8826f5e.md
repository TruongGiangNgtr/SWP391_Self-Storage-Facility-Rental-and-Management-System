# Targeted post-patch runtime evidence — 2026-11-11

**Project:** FRMS / SWP391 • Branch `TEST_SWT` • Source repair commit `8826f5e305f1e19c141550dcde3c3975de1b4d3a`.

**Provenance:** `USER_REPORTED_RUNTIME_CONSOLE`: the user provided an unedited-by-assistant CMD transcript of the runner's output; no `summary.md`, `results.json`, trace archive, or `git rev-parse HEAD` from individual runs was independently retrieved. The transcript shows a clean `git status --short` before `git pull --ff-only origin TEST_SWT`; successful fast-forward `94efe60..8826f5e`. Tests were run after pull, so source revision is **inferred** from the preceding Git output, not independently verified from per-run metadata.

## Commands and results

```bat
git status --short
git pull --ff-only origin TEST_SWT
.\RUN_FRMS_TESTS.cmd -Mode coverage
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0081 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0156 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0235 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0183-B -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

| Case | Run ID | Selected | Executed | Passed | Runner/wrapper exit | Wall duration as reported |
| --- | --- | ---: | ---: | ---: | --- | ---: |
| `FRMS-ST-0081` | `20261111T094923472Z` | 1 | 1 | 1 | 0 / 0 | 14.1s |
| `FRMS-ST-0156` | `20261111T095025709Z` | 1 | 1 | 1 | 0 / 0 | 12.1s |
| `FRMS-ST-0235` | `20261111T095126418Z` | 1 | 1 | 1 | 0 / 0 | 11.5s |
| `FRMS-ST-0183-B` | `20261111T095244321Z` | 1 | 1 | 1 | 0 / 0 | 8.7s |

**Outcome:** 4 separate targeted runs, each 1/1 **PASS** (no retries). The runner printed `Jira CSV: 0 independently confirmed defect(s), 275 registered cases` in each. The launcher `-Mode coverage` returned 275 registered/unique, 138 implemented, 8 review, 129 outstanding, exit code 0. Coverage **138/275 = 50.18%**.

Environment preflight: Testing API `http://127.0.0.1:5164` healthy during the reported runs; UI started through Vite at `http://127.0.0.1:4173`. Each transcript explicitly reports `FRMS_TEST_DB_ISOLATED=false`, `FRMS_ALLOW_STATE_CHANGES=NO`, AI disabled, and warns that the database appears remote/shared. It does not prove DB isolation. Fixture discovery still warned **no authorized SCHEDULED reservation visit for the signed-in Staff**; preserve this Blocked prerequisite.

## Integrity and counting rules

Historical full run `20261111T085924993Z` remains **95 Pass / 7 Flaky / 18 Blocked / 18 UnconfirmedFailure / 8 Review / 129 Not Run**, out of 275 selected and 138 implemented mappings. **Do not rewrite the historical run or aggregate these four separate targeted passes into 99/138 PASS for the full suite.** `FRMS-ST-0183-B` is still *Flaky in the historical run*, although it now has a clean 1/1 targeted PASS; a single clean rerun does not prove long-term stability.

These four cases are all within the 17 case IDs addressed by the source repair. **Remaining 13 repaired IDs have no new runtime evidence in this transcript**, as do unresolved 5xx/timeouts and genuine fixture gaps.

Next, run the remaining repaired cases with `-Workers 1 -Retries 0` only after health preflight. Investigate `GET /api/v1/facilities` HTTP 500 and health 503 separately. After the environment stabilizes, perform a new full run with `-Workers 1 -Retries 0` and compare per-ID results. Do not enable writes, seed shared Supabase data, create Jira defects from unconfirmed failures, or infer a new full-run PASS count.

**Change scope:** documentation/context only; no tests or application modified for this evidence batch.
