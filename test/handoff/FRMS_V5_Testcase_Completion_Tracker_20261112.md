# FRMS v5 — System Test Case Completion Tracker

**Status:** WORKING DRAFT — not a signed-off or executed workbook.

## Authoritative inputs

- `tests/system/manifest/cases.json` on `TEST_SWT`: 281 unique IDs (275 baseline + 6 supplemental).
- `docs/FRMS_SRS_V11.md` on `main`: normative requirement source; reconcile individual acceptance criteria.
- `FRMS_System_Test_Case_Requirements_v5_0_Consolidated(1).md`: SWT301 case-writing and release rules.
- `FRMS_Journey_Accuracy_Reaudit_v5_20261112(1).md`: 79 documented journeys, 38 requiring documentation/coverage corrections; no UI execution confirmed.

## Source partition

| Source classification | Count | Disposition |
| --- | ---: | --- |
| UI | 263 | Rewrite and confirm actual browser path, accepted SRS oracle and fixture |
| API-only | 10 | Preserve linked technical tests; do not put API calls in the manual workbook |
| REVIEW | 8 | Hold outside the approved workbook until owner resolves scope/oracle |
| **Total** | **281** | **Preserve all IDs and source keys** |

## V5 case-writing rules

Write the UI procedure in short numbered English using verified clickable labels from actual frontend; start at Homepage and show role-specific sign-in steps. Never put URLs, SQL/API calls, hardcoded test accounts/passwords, unapproved numeric limits or exact date fixtures in the manual steps. Expected Output is requirement-backed and UI-visible. Dependence must contain only prerequisite `FRMS-ST-xxxx` IDs; move fixture/role/entity conditions to an external register. Only `Pass`, `Fail`, `Untested`, `N/A` belong in workbook Result, with new/revised cases defaulting to `Untested`. Test date and Note remain blank.

## Known journey gaps and corrections

- QA5-01: replace non-action procedure lines with exact Click/Enter/Select steps.
- QA5-02: remove inconsistent National ID/session prerequisites.
- QA5-03: reclassify normal transitions wrongly described as negative.
- QA5-04: replace generalized field names with actual UI controls.
- QA5-05: Contract renewal uses `Select New Check-out Month`.
- QA5-06: Facility Staff must click `Sign Out` before asserting a guest view.
- QA5-07: Facility create/update is separate from Activate/Deactivate lifecycle; newly created Facility starts INACTIVE.
- QA5-08: assert success UI for support completion and assignment.
- QA5-09: split independent invalid equivalence classes.
- QA5-10: static review is not an E2E test result.
- QA5-11: include standalone invalid Customer login, not only a continuation flow.
- QA5-12: include cross-owner/cross-role denial paths backed by actual frontend and approved fixtures.

## Release gates

G0 source inventory; G1 approved requirement; G2 real frontend path; G3 trace and technical transfer; G4 case quality; G5 authorized lecturer template adaptation; G6 independent review. No gate is marked passed by this tracker.

**Do not alter historical 95-Pass full-run evidence or export Jira product defects from static source differences.**