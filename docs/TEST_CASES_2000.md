# Backend Test Cases — Milestone 1,500 → 2,000 (10/22/2026)

> **Basis:** current backend source code and user-reported test output, not the SRS. This file preserves the 1,000 → 1,500 milestone notes and adds the latest 1,501 → 2,000 execution report. Expected results describe the behavior under test; a failing assertion is a candidate defect or a test-contract mismatch to investigate, not proof by itself.
>
> **Documentation layout:** test cases 1,501–2,000 are documented in five separate Markdown files, 100 cases per file, under `backend/test-case-docs/`. See section 5 for the run summary and links/filenames.

## 1. Customer registration matrix (unit tests)

Source: `tests/Frms.UnitTests/CustomerRegistrationServiceMatrixTests.cs`
Service under test: `Frms.Business.Services.Implementations.CustomerRegistrationService`

Service rules the cases are based on (after `Trim()`; email also `ToLowerInvariant()`): name 1–150 chars; phone 1–30; email 1–254 and `EmailAddressAttribute`; password 8–64 chars and ≤ 72 UTF-8 bytes; address ≤ 1000 (optional); CCCD ≤ 20 (optional).

### 1.1 Invalid-field matrix — rejected before persistence (138 cases)

Test: `Register_InvalidFieldMatrixRejectsBeforePersistence`. Every case in this table expects `BusinessException` with code `VALIDATION_ERROR` (HTTP 400) and `repository.Registration == null`.

| ID | Test case | Input / condition | Cases | Expected result | Type |
|---|---|---|---:|---|---|
| REG-MX-001 | Empty full name | `FullName = ""` | 1 | `VALIDATION_ERROR`; nothing persisted | Added (corrected) |
| REG-MX-002 | Whitespace-only full name | `FullName = "   "` (trims to empty) | 1 | `VALIDATION_ERROR`; nothing persisted | Added (corrected) |
| REG-MX-003 | Full name too long | `N` repeated 151–178 times | 28 | `VALIDATION_ERROR`; nothing persisted | Added (corrected) |
| REG-MX-004 | Phone number too long | `9` repeated 31–50 times | 20 | `VALIDATION_ERROR`; nothing persisted | Added |
| REG-MX-005 | Blank email | `""`, `" "`, tab, newline (trim to empty) | 4 | `VALIDATION_ERROR`; nothing persisted | Added |
| REG-MX-006 | Email missing a required part | `not-an-email`, `missing-at.test`, `@example.test`, `user@`, `@` | 5 | `VALIDATION_ERROR`; nothing persisted | Added |
| REG-MX-007 | Email with repeated `@` | `user@@example.test`, `user@example@@test` | 2 | `VALIDATION_ERROR`; nothing persisted | Added |
| REG-MX-008 | Email with line break in local part | `user\n@example.test`, `user\r@example.test` | 2 | `VALIDATION_ERROR`; nothing persisted | Added |
| REG-MX-009 | Password too short | `a` repeated 0–7 times | 8 | `VALIDATION_ERROR`; nothing persisted | Added |
| REG-MX-010 | Password too long (characters) | `a` repeated 65–86 times | 22 | `VALIDATION_ERROR`; nothing persisted | Added |
| REG-MX-011 | Password over 72 UTF-8 bytes | `ế` (3 bytes) repeated 25–44 times; 25 chars = 75 bytes | 20 | `VALIDATION_ERROR`; nothing persisted | Added |
| REG-MX-012 | Address too long | `A` repeated 1001–1015 times | 15 | `VALIDATION_ERROR`; nothing persisted | Added |
| REG-MX-013 | CCCD too long | `7` repeated 21–30 times | 10 | `VALIDATION_ERROR`; nothing persisted | Added |
| | **Subtotal** | | **138** | | |

### 1.2 Email normalization — accepted after trimming (5 cases)

Test: `Register_EmailWithSurroundingWhitespaceIsTrimmedAndAccepted`. The service trims the email before validating (same normalization as AUTH-002/003), so these are valid registrations.

| ID | Test case | Input / condition | Expected result | Type |
|---|---|---|---|---|
| REG-MX-014 | Trailing tab | `user@example.test\t` | No exception; repository receives a registration | Added (corrected) |
| REG-MX-015 | Trailing newline | `user@example.test\n` | No exception; repository receives a registration | Added (corrected) |
| REG-MX-016 | Leading space | ` user@example.test` | No exception; repository receives a registration | Added (corrected) |
| REG-MX-017 | Trailing space | `user@example.test ` | No exception; repository receives a registration | Added (corrected) |
| REG-MX-018 | Trailing CR+LF | `user@example.test\r\n` | No exception; repository receives a registration | Added (corrected) |

### 1.3 Known defect — expected rejection, currently ignored (7 cases)

Source: `KnownDefectEmailCases` (same test method as 1.1). Marked `Ignore("F-001 …")`, reported as **skipped**. They document the intended behavior; see finding **F-001** in section 4.

| ID | Test case | Input / condition | Intended result | Current result | Type |
|---|---|---|---|---|---|
| REG-MX-019 | Space in local part | `user example@test.test` | `VALIDATION_ERROR` | Accepted by service | Added (ignored) |
| REG-MX-020 | Space in domain | `user@exam ple.test` | `VALIDATION_ERROR` | Accepted by service | Added (ignored) |
| REG-MX-021 | Space in domain label | `user@example test` | `VALIDATION_ERROR` | Accepted by service | Added (ignored) |
| REG-MX-022 | Tab in local part | `user\t@example.test` | `VALIDATION_ERROR` | Accepted by service | Added (ignored) |
| REG-MX-023 | Space before `@` | `user @example.test` | `VALIDATION_ERROR` | Accepted by service | Added (ignored) |
| REG-MX-024 | Space after `@` | `user@ example.test` | `VALIDATION_ERROR` | Accepted by service | Added (ignored) |
| REG-MX-025 | Extra address after `;` | `user@example.test; user` | `VALIDATION_ERROR` | Accepted by service | Added (ignored) |

**Section 1 total: 138 + 5 + 7 = 150 cases (143 executed and passing, 7 skipped).**

## 2. Remaining new test cases — not yet documented

| Item | Cases | Status |
|---|---:|---|
| Documented in section 1 | 150 | Done |
| Other new `Frms.UnitTests` files | 250 | Pending — file names/content needed |
| New `Frms.ApiTests` files (849 → 449 total ApiTests; +100 vs. previous run) | 100 | Pending — file names/content needed |
| **Total new cases in batch 2** | **500** | 150 documented / 350 pending |

Counts come from the project totals: `Frms.UnitTests` 624 → 1,024 (+400, of which 150 are section 1) and `Frms.ApiTests` 349 → 449 (+100).

## 3. Historical execution status at 1,500 cases

**Historical run: PASS — 1,493 passed / 0 failed / 7 skipped / 1,500 total.** This is the earlier 1,500-case run, not the latest 2,000-case result documented in section 5.

Reported command (from `backend/`):

```powershell
dotnet test Frms.slnx
```

Reported summary:

```text
Test summary: total: 1500, failed: 0, succeeded: 1493, skipped: 7, duration: 20.8s
Build succeeded in 32.5s.
```

Project-level discovered counts in that run (console output was interleaved, so the mapping to projects follows the previous run's counts):

| Test project | Discovered | Result |
|---|---:|---|
| `Frms.UnitTests` | 1,024 | Passed (7 skipped) |
| `Frms.ArchitectureTests` | 13 | Passed |
| `Frms.IntegrationTests` | 14 | Passed |
| `Frms.ApiTests` | 449 | Passed |
| **Total** | **1,500** | **0 failed** |

The 7 skipped cases are exactly the `invalid_email_known_defect_*` cases listed in 1.3.

Correction applied between the first 1,500-test run (41 failed) and this run:

| Failing cases | Count | Root cause | Fix |
|---|---:|---|---|
| `name_length_1` … `name_length_29` | 29 | Test generated 0–29 character names as invalid, but the service accepts 1–150 characters | Test rewritten (REG-MX-001–003) |
| `invalid_email_15, 17, 18, 19, 22` | 5 | Service trims surrounding whitespace, so these emails are valid | Moved to accepted cases (REG-MX-014–018) |
| `invalid_email_11, 12, 13, 14, 20, 21, 24` | 7 | Service behavior (F-001) | Kept as ignored cases (REG-MX-019–025) |

This result confirms the discovered tests passed in the reported environment. It does **not** prove full business-flow coverage, and the legacy SQL Server integration source files listed in `TEST_CASES_1000.md` section 5 remain excluded from compilation.

## 4. Findings outside test code

### F-001 — Email validation accepts embedded whitespace and `;`

- **Where:** `backend/Frms.Business/Services/Implementations/CustomerRegistrationService.cs`, check `!new EmailAddressAttribute().IsValid(email)`.
- **Observed:** after `Trim().ToLowerInvariant()`, emails with spaces, tabs or `;` inside are accepted and sent to the repository.
- **Expected:** rejected with `VALIDATION_ERROR` (HTTP 400) before persistence.
- **Suggested fix (not applied):**

```csharp
private static bool IsValidEmail(string email) =>
    !email.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c == ';' || c == ',') &&
    new EmailAddressAttribute().IsValid(email);
// replace !new EmailAddressAttribute().IsValid(email) with !IsValidEmail(email)
```

- **Status:** Open — the service file is shared; confirm with its owner before changing.
- **After the fix:** remove `.Ignore(...)` in `KnownDefectEmailCases`; the 7 skipped cases should then pass (expected 1,500 passed / 0 skipped).

### Notes

- `TrimmedEmailCases` only assert that registration succeeds and the repository receives a registration; they do not assert the stored email value.
- Reproduce from `backend/`: `dotnet test Frms.slnx`.


## 5. Batch 4 — cases 1,501–2,000 and latest execution

### 5.1 Documentation files (100 cases each)

| Range | Focus | Markdown file |
|---|---|---|
| 1501–1600 | Policy version validator boundaries | `TEST_CASES_1501_1600.md` |
| 1601–1700 | Customer profile validation boundaries and update-field behavior | `TEST_CASES_1601_1700.md` |
| 1701–1800 | Customer registration accepted boundaries | `TEST_CASES_1701_1800.md` |
| 1801–1900 | Registration API request Data Annotations | `TEST_CASES_1801_1900.md` |
| 1901–2000 | Employee login, role/status/password matrix | `TEST_CASES_1901_2000.md` |

The files are included in `backend-batch4-500-testcases.zip` under `backend/test-case-docs/`. These are the five 100-case documents; this overview file is not intended to replace them.

### 5.2 Latest user-reported run

Command, run from `backend/`:

```powershell
dotnet test Frms.slnx
```

**Result: FAIL — 1,991 passed / 2 failed / 7 skipped / 2,000 total.** All projects compiled, but `dotnet test` ended with test errors; do not describe this run as a successful test run.

| Test project | Discovered | Result from the supplied log |
|---|---:|---|
| `Frms.UnitTests` | 1,424 | 1,416 passed, 1 failed, 7 skipped |
| `Frms.ArchitectureTests` | 13 | 13 passed |
| `Frms.IntegrationTests` | 14 | 14 passed |
| `Frms.ApiTests` | 549 | 548 passed, 1 failed |
| **Total** | **2,000** | **1,991 passed, 2 failed, 7 skipped** |

Reported duration: 37.8 seconds for test execution; build/test command ended with `Build failed with 2 error(s) in 71.0s` because of the two test failures. The seven skipped tests remain the known email cases `invalid_email_known_defect_*` associated with F-001.

### 5.3 New failures to investigate

#### F-002 — Customer profile validator validates an unselected address

- **Failing test:** `unselected_long_address_should_be_ignored` in `CustomerProfileAdvancedBoundaryMatrixTests`.
- **Input intent:** `updateAddress = false`, `address` contains 1,001 characters, `updateCccd = true`, `cccd = "123"`.
- **Test expectation:** valid, because the address field is not selected for update.
- **Observed:** `CustomerProfileValidator.ValidateAndNormalize` throws `BusinessException: Customer profile fields are invalid` at `CustomerProfileValidator.cs:19`.
- **Assessment:** candidate patch/update semantics defect. Confirm the intended contract with the owner before changing production code or the assertion. If unselected fields must be ignored, validation should apply only to fields whose update flags are true.
- **Status:** Open; production code not changed.

#### F-003 — Registration request accepts an email containing embedded whitespace

- **Failing test:** `request_email_invalid_9` in `RegistrationRequestValidationMatrixTests`.
- **Input:** `a b@example.test`.
- **Test expectation:** invalid (`expectedValid = false`).
- **Observed:** `Validator.TryValidateObject` returns `true`; the request DTO's current Data Annotations accept the value.
- **Assessment:** candidate request-validation weakness, consistent with the broader email-validation concern in F-001. Check the DTO attributes and service validation together so the API and business layer enforce a consistent email contract.
- **Status:** Open; production code not changed.

### 5.4 How to interpret this run

- The 2 failures are not to be made green by blindly changing expectations. First decide whether each test expresses the intended contract; if it does, treat it as a backend finding and fix the implementation only with approval.
- The 7 skipped F-001 cases are known defect probes, not passing tests.
- This run establishes that the 2,000-case suite was discovered in the reported environment. It does **not** establish a clean test run because two tests failed.
- Earlier section 3 records the historical 1,500-case run. Its pass result is historical and is superseded as the latest run status by this section; it should not be rewritten as though it described the 2,000-case run.
