# FRMS TEST_SWT — Targeted reruns after 1547d67

Date: 2026-11-11. Evidence: USER_REPORTED_RUNTIME_CONSOLE. The user showed a fast-forward pull from 8826f5e to 1547d67 before execution, but individual report JSON and local per-run SHA were not supplied.

| Case | Run ID | Result | Exit codes |
| --- | --- | --- | --- |
| FRMS-ST-0160 | 20261111T095951885Z | PASS (1/1, 11.3 s) | 0 / 0 |
| FRMS-ST-0085 | 20261111T100054156Z | PASS (1/1, 8.7 s) | 0 / 0 |
| FRMS-ST-0141 | 20261111T100152311Z | FAIL (automation locator) | 1 / 1 |
| FRMS-ST-0184 | 20261111T100258194Z | PASS (1/1, 9.6 s) | 0 / 0 |

All targeted runs used one worker and zero retries, reported healthy existing Testing API, disabled state changes and zero independently confirmed Jira defects. An authorized SCHEDULED reservation Visit was still not found by fixture discovery.

## ST-0141 diagnosis

The form in frontend/src/components/admin/EmployeeAccountFormDialog.tsx wraps the Facility select in a label containing both the text 'Facility' and supplementary text 'Required for Facility Staff and Facility Manager.' Therefore getByLabel('Facility',{exact:true}) did not match. The case failed before completing its role-toggle checks; it does not establish a product defect.

The new test-only selector is scoped to the Create employee account dialog and selects the Facility combobox by the beginning of its accessible name. It explicitly checks that the Facility select appears for FACILITY_MANAGER and disappears for BUSINESS_OPERATIONS_MANAGER. Assertions that CUSTOMER is not offered and that global employee roles show 'Not applicable for global employee roles' remain unchanged. No employee is created.

The fix is SOURCE_PATCH_ONLY, pending new runtime evidence for FRMS-ST-0141. The four-case historical result remains 3 PASS, 1 FAIL. Previously reported first batch has four distinct targeted PASS. Across both batches, seven distinct repaired cases have an individual PASS, but they do not revise the historical full-run count.

Historical full run 20261111T085924993Z: 95 Pass, 7 Flaky, 18 Blocked, 18 UnconfirmedFailure, 8 Review, 129 Not Run. Registry: 138 IMPLEMENTED / 275 (50.18%), 129 NOT_IMPLEMENTED, 8 REVIEW. No new full-run result.

Next safe command (clean worktree, already healthy backend, shared Supabase read-only):

    git status --short
    git pull --ff-only origin TEST_SWT
    .\RUN_FRMS_TESTS.cmd -Mode case -CaseId FRMS-ST-0141 -Workers 1 -Retries 0 -SkipCatalogDiscovery

Inspect the new run summary.md/results.json before claiming PASS. No application, database, main branch, or Jira changes in this patch.
