# Batch 14 — first targeted run evidence and ST-0061 locator repair

**Date:** 2026-11-11. **Branch:** `TEST_SWT`. **Input:** user-provided Windows CMD console log (4 individual targeted runs, each `-Workers 1 -Retries 0 -SkipCatalogDiscovery`). **Execution SHA is not printed in the console**, so the report only identifies the branch and user's `git pull` output, not an independently checked per-run SHA.

| ID | Run ID | Observed result | Meaning |
| --- | --- | --- | --- |
| `FRMS-ST-0103` | `20261111T114719268Z` | **PASS**, 1 executed; 13.5 s | Source-aligned five-month renewal UI preview; no contract renewal submitted |
| `FRMS-ST-0104` | `20261111T114821972Z` | **PASS**, 1 executed; 12.3 s | Source-aligned six-month preview, without invented discount policy |
| `FRMS-ST-0194` | `20261111T114924460Z` | **PASS**, 1 executed; 13.4 s | Existing valid Facility selector / nonexistent ID excluded; no employee created |
| `FRMS-ST-0061` | `20261111T115025397Z` | **FAIL**, 1 executed | Playwright strict locator matched **two** legitimate notices containing the same offline-invoice text |

## Root cause and correction

The failing assertion used an unscoped `page.getByText('No separate first-month invoice or online payment is created.', {exact:false})`. The React `StaffHandoverPage` renders the same clause both in `FirstMonthHandoverInfo` and in a `p.muted` under the handover `form`. The test made it as far as validating the Manager's ACTIVE assignment, Staff's CHECKED_IN Visit, checkbox enablement and Complete Handover button enablement; it failed **only** at the ambiguous text locator.

The assertion is now narrowed to `page.locator('.handover-operation form > p.muted')` and checks the recorded note with `toContainText`. **No application or database change**, no click on Complete Handover, no status or logic weakening. This is a **test-locator defect**, not a confirmed product defect and not eligible for Jira export.

**Current ST-0061 status after source repair:** `PENDING_TARGETED_RERUN`, **not PASS**. The historical four-run result remains 3 PASS and 1 FAIL; this repair did not execute a new Playwright run.

## Environment observations

`TEST_SWT` worktree was clean, `git pull --ff-only` reported up-to-date; backend `http://127.0.0.1:5164/health` returned `Healthy`. Coverage registry validation returned `275 / 275 unique / 153 implemented / 8 review / 114 outstanding`, exit code 0. `-AuditOnly` reported two absent keys **only for unregistered negative-login tests**: `FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE` and `FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL`. Targeted fixture audit for the four selected cases reported `19 applicable keys / 0 missing`; no new credentials are needed. Shared Supabase remained explicitly read-only: `FRMS_TEST_DB_ISOLATED=false`, `FRMS_ALLOW_STATE_CHANGES=NO`, AI disabled.

### Next local commands

```bat
git status --short
git pull --ff-only origin TEST_SWT
.\RUN_FRMS_TESTS.cmd -Mode coverage
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0061 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0093 -Workers 1 -Retries 0 -SkipCatalogDiscovery
.\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0134 -Workers 1 -Retries 0 -SkipCatalogDiscovery
```

Do not use `-StartBackend` or any state-writing fixture setup on the shared database. The older four unresolved cases `0067`, `0071`, `0109`, `0188` remain deferred; see `BLOCKED_FIXTURES_0067_0071_0109_0188.md`.

## 2026-11-11 — ST-0061 targeted PASS after locator fix

User CMD log confirmed `FRMS-ST-0061` run `20261111T120242294Z`: **1/1 PASS, 24.3s**, workers=1, retries=0, exit codes 0. The local branch was fast-forwarded to `fb5f216` before execution. Earlier `20261111T115025397Z` strict-locator FAIL remains historical; **0061 now has post-fix targeted PASS evidence**. Batch 14 thus has **4/15 distinct targeted PASS**, 11 without runtime, no newly confirmed Jira defect from this run; DB remains read-only. Since this run, remote branch also added ST-0276 as NOT_IMPLEMENTED, increasing registry to **276 cases / 153 IMPLEMENTED / 115 NOT_IMPLEMENTED / 8 REVIEW (55.43%)**. The old full run (275 cases, 95 PASS) and 0061's own 275-case runner snapshot remain unchanged. See `test/handoff/BATCH14_ST0061_PASS_20261111T120242294Z.md`.

## Post-run evidence — 20261111T122312270Z and 20261111T122412231Z

`FRMS-ST-0093` 1/1 PASS in 11.9 s, `FRMS-ST-0134` 1/1 PASS in 13.5 s, both one worker / zero retries / exit 0 (USER_REPORTED_RUNTIME_CONSOLE). These two additional distinct passes bring Batch 14 to **6/15**. Current source manifest subsequently grew to **279** with 4 supplemental defect/UI cases. The previous coverage validator failed exit 3 at 276 cases because it hardcoded 275 and rejected `Return Inspection`; a source repair is now pending local coverage verification. See `BATCH14_COVERAGE_FIX_AND_RERUNS_20261111.md`.
