# Backend Test Cases — Milestone 2,000 → 2,500 (10/22/2026)

> **Basis:** current backend source code and user-reported test output, not the SRS. This file adds the latest 2,001–2,500 batch and its execution report to the existing `TEST_CASES_2000.md` history. Expected results describe the behavior under test; a failing assertion is a candidate defect or a test-contract mismatch to investigate, not proof by itself.
>
> **Documentation layout:** batch 5 contains five test files with 100 intended cases each. See section 1 for the ranges and section 3 for the latest run summary and findings.

## 1. Batch 5 — test case ranges (2,001–2,500)

| Range | Focus | Test file |
|---|---|---|
| 2001–2100 | Requested-period rule and month-range boundaries | `RequestedPeriodRuleMatrix2001Tests.cs` |
| 2101–2200 | Customer profile PATCH payload parsing | `CustomerProfilePatchParserMatrix2101Tests.cs` |
| 2201–2300 | Current-user identity and claims validation | `CurrentUserContextMatrix2201Tests.cs` |
| 2301–2400 | JWT token service behavior | `JwtTokenServiceMatrix2301Tests.cs` |
| 2401–2500 | Authorization middleware outcomes | `AuthorizationMiddlewareResultMatrix2401Tests.cs` |

The batch ZIP is `backend-batch5-500-testcases.zip`. Each file was intended to cover 100 cases. The run log shows that the requested-period case source itself failed during discovery because it generated 99 cases rather than 100; therefore, do not assume all 500 intended cases were successfully discovered and executed.

## 2. Latest test execution

Command, run from `backend/`:

```powershell
dotnet test Frms.slnx --logger "console;verbosity=normal" *> test-log.txt
```

**Result: FAIL — 2,483 passed / 11 failed / 7 skipped; the reported suite total is 2,501.** The test run did not finish cleanly. The supplied log contains a unit-test run with 1,425 discovered tests (1,416 passed, 2 failed, 7 skipped) and an API-test run with 1,049 discovered tests (1,040 passed, 9 failed). Other project output is interleaved in the log, so the totals below reflect the overall summary reported for this run.

| Scope | Discovered / reported | Result |
|---|---:|---|
| `Frms.UnitTests` | 1,425 | 1,416 passed, 2 failed, 7 skipped |
| `Frms.ApiTests` | 1,049 | 1,040 passed, 9 failed |
| **Overall reported total** | **2,501** | **2,483 passed, 11 failed, 7 skipped** |

The total count is 2,501 rather than exactly 2,500. The log also shows a case-source discovery error for one intended 100-case matrix, so reconcile the discovered-test count before treating the intended 2,001–2,500 range as fully executed.

## 3. New findings from this run

### F-004 — Requested-period test matrix generates 99 cases instead of 100

- **Failing test:** `Parse_RequestedMonthRange_HandlesBoundaryInput()` in `RequestedPeriodRuleMatrix2001Tests`.
- **Observed:** `InvalidOperationException: Expected 100 cases; generated 99.`
- **Location:** `RequestedPeriodRuleMatrix2001Tests.cs:line 41`.
- **Assessment:** test-generation/count issue. This failure does not establish a production-code defect; the case source or its expected count should be corrected so the intended matrix is discovered.
- **Status:** Open; no production-code change made.

### F-005 — Customer profile validator rejects an overlong address even when that field is not selected

- **Failing test:** `unselected_long_address_should_be_ignored` in `CustomerProfileAdvancedBoundaryMatrixTests`.
- **Input intent:** `updateAddress = false`, address contains 1,001 characters, `updateCccd = true`, and `cccd = "123"`.
- **Expected:** the unselected address should not cause validation failure.
- **Observed:** `CustomerProfileValidator.ValidateAndNormalize` throws `BusinessException: Customer profile fields are invalid` at `CustomerProfileValidator.cs:line 19`.
- **Assessment:** candidate PATCH/update-semantics issue. Confirm the intended contract with the owner. If unselected fields must be ignored, validation should be limited to selected fields.
- **Status:** Open; production code not changed.

### F-006 — Current-user context does not reject four generated invalid-subject inputs

- **Failing tests:** `TC_2256_invalid_subject_31`, `TC_2264_invalid_subject_39`, `TC_2265_invalid_subject_40`, and `TC_2275_invalid_subject_50`.
- **Test method:** `Context_ResolvesOnlyAvailableIdentityClaims` in `CurrentUserContextMatrix2201Tests.cs:line 52`.
- **Expected:** `BusinessException`.
- **Observed:** no exception was thrown (`caughtException` was `null`).
- **Assessment:** either the generated subject values are accepted by the current identity-claim contract, or the implementation does not reject values the tests classify as invalid. Inspect the exact case inputs and confirm the contract before changing production code or expectations.
- **Status:** Open; production code not changed.

### F-007 — PATCH parser accepts four payloads expected to be rejected

- **Failing tests:** `TC_2189_rejected_patch_payload_89`, `TC_2190_rejected_patch_payload_90`, `TC_2191_rejected_patch_payload_91`, and `TC_2192_rejected_patch_payload_92`.
- **Test method:** `Parse_OnlyAllowsSupportedPatchFields` in `CustomerProfilePatchParserMatrix2101Tests.cs:line 68`.
- **Expected:** `BusinessException` for each payload.
- **Observed:** no exception was thrown (`caughtException` was `null`).
- **Assessment:** candidate PATCH contract/parser gap. Review the exact JSON payloads in the test matrix to determine whether unsupported fields or shapes are being accepted unexpectedly.
- **Status:** Open; production code not changed.

### F-008 — Registration request validation accepts an email containing embedded whitespace

- **Failing test:** `request_email_invalid_9` in `RegistrationRequestValidationMatrixTests.cs:line 41`.
- **Input:** `a b@example.test`.
- **Expected:** invalid (`expectedValid = false`).
- **Observed:** `Validator.TryValidateObject` reports the request as valid.
- **Assessment:** candidate request-validation weakness, consistent with the existing F-001 email-validation concern recorded in `TEST_CASES_2000.md`. Check the API DTO and business service together so they enforce a consistent email contract.
- **Status:** Open; production code not changed.

## 4. Existing known skipped cases

The run reports **7 skipped tests**. The previous overview, `TEST_CASES_2000.md`, identifies the seven `invalid_email_known_defect_*` probes as known defect F-001, covering embedded whitespace or an extra address separated by `;`. These are skipped probes, not passing tests. Confirm the skipped names in the complete log before assuming the set is unchanged.

## 5. How to interpret this run

- The run is **not green**: the overall summary reports 11 failures and 7 skipped tests.
- The log gives details for 10 named failing tests plus one test-case source generation error; the four current-user failures and four PATCH-parser failures are individual cases within their respective matrices.
- Do not make the suite green by blindly changing assertions. First verify that each test expresses the intended contract; if it does, treat it as a candidate backend finding.
- F-004 is a test-data/discovery issue rather than evidence of a production defect.
- F-005 through F-008 need contract review and implementation investigation. No production fixes are represented as applied in this report.
- The suite's reported total of 2,501 does not match the nominal 2,500 milestone. Resolve the extra test and the 99-versus-100 generated-case discrepancy before claiming every intended case from 2,001 through 2,500 was executed.
- Earlier milestones and findings remain documented in `TEST_CASES_2000.md`; this file records the batch 5 run and should not overwrite that history.
