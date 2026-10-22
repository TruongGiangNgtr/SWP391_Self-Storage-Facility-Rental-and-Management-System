# Backend Test Cases — Milestone 2,500 → 3,000 (batch 6, first run 10/22/2026)

> **Basis:** source of `CustomerRegistrationService` and the user-reported run output (`probe-log.txt`). The 500 cases are *probes*: each states what a sound registration contract would do. A failing probe is a candidate defect or a missing rule, not proof by itself; policy-type probes (password rules, character-set rules) only count as defects if the SRS requires them.

## 1. Batch 6 — test case ranges (2,501–3,000)

| Range | Focus | Test file | Passed | Failed |
|---|---|---|---:|---:|
| 2501–2600 | Phone number and CCCD format | `RegistrationPhoneCccdProbeMatrix2501Tests.cs` | 20 | 80 |
| 2601–2700 | Email format | `RegistrationEmailProbeMatrix2601Tests.cs` | 27 | 73 |
| 2701–2800 | Full name and address content | `RegistrationNameAddressProbeMatrix2701Tests.cs` | 37 | 63 |
| 2801–2900 | Password policy | `RegistrationPasswordProbeMatrix2801Tests.cs` | 21 | 79 |
| 2901–3000 | Registration behaviour | `RegistrationBehaviorProbeMatrix2901Tests.cs` | 94 | 6 |

Shared helpers (not test cases): `RegistrationProbeHarness.cs`. All fixtures use `Category("RegistrationProbe")`.

## 2. Latest test execution

Command, run from `backend/`:

```powershell
dotnet test Frms.slnx --filter "TestCategory=RegistrationProbe" --logger "console;verbosity=normal" *> probe-log.txt
```

**Result: FAIL (expected for a probe batch) — 199 passed / 301 failed / 0 skipped; 500 discovered and executed (5.46 s).** All 500 intended cases were discovered; no harness error (`Probe harness: …`) appeared, so the reflection-based property lookup worked.

| Failure type | Cases | Meaning |
|---|---:|---|
| Expected `BusinessException`, none thrown | 280 | Service accepted input the probe expects to be rejected |
| Stored value differs from expected | 15 | Service stores text as typed (Unicode form, inner whitespace, invisible-only address) |
| Expected an exception, none thrown (hasher returned no hash) | 3 | Empty/blank/null hash is persisted |
| Expected `OperationCanceledException`, none thrown | 3 | Pre-cancelled token does not stop registration |
| **Total** | **301** | |

Note: the log file was written by PowerShell `*>` as UTF-16. Use `... 2>&1 | Out-File -Encoding utf8 probe-log.txt` for a UTF-8 log.

Prediction check: I predicted about 279 failures; 301 occurred. Email failed more than predicted (73 of 100) because `EmailAddressAttribute` accepts all dot/hyphen/character/domain cases tested; the 7 "several addresses" cases passed (rejected) because each contains two `@`.

## 3. Results by group


### `RegistrationPhoneCccdProbeMatrix2501Tests.cs`

| IDs | Group | Pass | Fail | Expected by the test |
|---|---|---:|---:|---|
| 2501–2514 | Phone: non-digit characters | 0 | 14 | Reject (VALIDATION_ERROR) |
| 2515–2523 | Phone: too few digits (1-9) | 0 | 9 | Reject (VALIDATION_ERROR) |
| 2524–2538 | Phone: more than 15 digits (E.164 maximum), 16-30 | 0 | 15 | Reject (VALIDATION_ERROR) |
| 2539–2548 | Phone: valid values and trimming | 10 | 0 | Accept; stored value checked |
| 2549–2554 | Phone: separators / multiple numbers | 0 | 6 | Reject (VALIDATION_ERROR) |
| 2555–2566 | CCCD: non-digit characters | 0 | 12 | Reject (VALIDATION_ERROR) |
| 2567–2574 | CCCD: 1-8 digits (shorter than legacy 9-digit CMND) | 0 | 8 | Reject (VALIDATION_ERROR) |
| 2575–2584 | CCCD: 10, 11 and 13-20 digits (not 9 or 12) | 0 | 10 | Reject (VALIDATION_ERROR) |
| 2585–2594 | CCCD: valid / optional values | 10 | 0 | Accept; stored value checked |
| 2595–2600 | CCCD: 12 digits with invalid province prefix | 0 | 6 | Reject (VALIDATION_ERROR) |

### `RegistrationEmailProbeMatrix2601Tests.cs`

| IDs | Group | Pass | Fail | Expected by the test |
|---|---|---:|---:|---|
| 2601–2612 | Email: dot and hyphen placement | 0 | 12 | Reject (VALIDATION_ERROR) |
| 2613–2625 | Email: characters illegal in an unquoted local part | 0 | 13 | Reject (VALIDATION_ERROR) |
| 2626–2631 | Email: display-name / URI / comment forms | 0 | 6 | Reject (VALIDATION_ERROR) |
| 2632–2643 | Email: domain structure | 0 | 12 | Reject (VALIDATION_ERROR) |
| 2644–2649 | Email: local part / label length limits | 0 | 6 | Reject (VALIDATION_ERROR) |
| 2650–2655 | Email: non-ASCII characters | 1 | 5 | Reject (VALIDATION_ERROR) |
| 2656–2660 | Email: invisible / control characters | 0 | 5 | Reject (VALIDATION_ERROR) |
| 2661–2675 | Email: valid values, case folding and boundaries | 15 | 0 | Accept; stored (lower-case, trimmed) value checked |
| 2676–2679 | Email: total length over 254 | 4 | 0 | Reject (VALIDATION_ERROR) |
| 2680–2693 | Email: characters illegal in the domain | 0 | 14 | Reject (VALIDATION_ERROR) |
| 2694–2700 | Email: several addresses in one field | 7 | 0 | Reject (VALIDATION_ERROR) |

### `RegistrationNameAddressProbeMatrix2701Tests.cs`

| IDs | Group | Pass | Fail | Expected by the test |
|---|---|---:|---:|---|
| 2701–2708 | Name: invisible-only values (not removed by Trim) | 0 | 8 | Reject (VALIDATION_ERROR) |
| 2709–2715 | Name: control characters | 0 | 7 | Reject (VALIDATION_ERROR) |
| 2716–2721 | Name: punctuation / digits only | 0 | 6 | Reject (VALIDATION_ERROR) |
| 2722–2730 | Name: markup / injection payloads | 0 | 9 | Reject (VALIDATION_ERROR) |
| 2731–2739 | Name: Unicode / whitespace normalisation | 0 | 9 | Accept; stored value must be NFC and single-spaced |
| 2740–2751 | Name: valid values and boundaries | 12 | 0 | Accept; stored (trimmed) value checked |
| 2752–2753 | Name: multi-byte length boundary | 2 | 0 | Accept at 150 chars; reject at 151 |
| 2754–2756 | Name: emoji | 0 | 3 | Reject (VALIDATION_ERROR) |
| 2757–2759 | Name: repeated punctuation, padded and off-by-one lengths | 2 | 1 | Mixed (see names) |
| 2760–2764 | Name / Address: bidi override and replacement characters | 0 | 5 | Reject (VALIDATION_ERROR) |
| 2765–2768 | Address: control characters | 0 | 4 | Reject (VALIDATION_ERROR) |
| 2769–2773 | Address: markup / injection payloads | 0 | 5 | Reject (VALIDATION_ERROR) |
| 2774–2776 | Address: invisible-only values | 0 | 3 | Accept as blank; stored value must be null |
| 2777–2782 | Address: whitespace-only values | 6 | 0 | Accept as blank; stored value must be null |
| 2783–2791 | Address: length boundaries | 9 | 0 | Accept <= 1000 after trim; reject above |
| 2792–2797 | Address: valid values | 6 | 0 | Accept; stored (trimmed) value checked |
| 2798–2800 | Address: decomposed (NFD) Unicode | 0 | 3 | Accept; stored value must be NFC |

### `RegistrationPasswordProbeMatrix2801Tests.cs`

| IDs | Group | Pass | Fail | Expected by the test |
|---|---|---:|---:|---|
| 2801–2805 | Password: whitespace-only | 0 | 5 | Reject (VALIDATION_ERROR) |
| 2806–2827 | Password: common / breached passwords (policy probe) | 0 | 22 | Reject (VALIDATION_ERROR) |
| 2828–2832 | Password: equal to another field | 0 | 5 | Reject (VALIDATION_ERROR) |
| 2833–2838 | Password: control characters (bcrypt truncates at NUL) | 0 | 6 | Reject (VALIDATION_ERROR) |
| 2839–2846 | Password: valid values (hasher must receive the exact text) | 8 | 0 | Accept; hasher input checked |
| 2847–2852 | Password: byte/char boundaries | 6 | 0 | Reject over limit; accept at limit |
| 2853–2860 | Password: single character class | 0 | 8 | Reject (VALIDATION_ERROR) |
| 2861–2868 | Password: keyboard / sequence patterns | 0 | 8 | Reject (VALIDATION_ERROR) |
| 2869–2876 | Password: season/year and word+year patterns | 0 | 8 | Reject (VALIDATION_ERROR) |
| 2877–2882 | Password: contains "password"/"admin" style words | 0 | 6 | Reject (VALIDATION_ERROR) |
| 2883–2889 | Password: common Vietnamese passwords | 0 | 7 | Reject (VALIDATION_ERROR) |
| 2890–2893 | Password: repeated blocks | 0 | 4 | Reject (VALIDATION_ERROR) |
| 2894–2900 | Password: strong random-looking values | 7 | 0 | Accept; hasher input checked |

### `RegistrationBehaviorProbeMatrix2901Tests.cs`

| IDs | Group | Pass | Fail | Expected by the test |
|---|---|---:|---:|---|
| 2901–2905 | Hasher returns no hash / hasher throws | 2 | 3 | No hash: exception and nothing persisted. Throw: same exception propagates |
| 2906–2915 | Repository throws an unexpected exception | 10 | 0 | Same exception instance propagates (not wrapped, not swallowed) |
| 2916–2921 | Cancellation token handling | 3 | 3 | Token reaches repository; a pre-cancelled token must not persist |
| 2922–2931 | Generated account / customer ids | 10 | 0 | Ids are non-empty and unique across N registrations |
| 2932–2941 | Created-at timestamp | 10 | 0 | Stored timestamp equals clock.UtcNow in UTC |
| 2942–2946 | Hash stored instead of plain password | 5 | 0 | Stored hash equals hasher output; plain password absent |
| 2947–2962 | Validation failure side effects and multi-field failures | 16 | 0 | BusinessException; hasher and repository never called |
| 2963–2970 | Null command fields | 8 | 0 | Required fields rejected; optional fields stored as null |
| 2971–2975 | Returned result matches stored ids | 5 | 0 | Result ids equal the ids sent to the repository |
| 2976–2980 | Parallel registrations | 5 | 0 | All succeed, hasher called once each, unique ids |
| 2981–2990 | Hasher / repository called exactly once | 10 | 0 | One hash call and one persistence call per valid registration |
| 2991–2996 | Huge (1,000,000-character) inputs | 6 | 0 | Rejected fast; hasher and repository not called |
| 2997–3000 | Culture-invariant email lower-casing | 4 | 0 | Stored email is lower-case regardless of current culture |

**Defences confirmed working (probes that passed):** valid phone/CCCD/email/name/address/password values are accepted and trimmed correctly; email is lower-cased independent of culture (tr-TR etc.); email over 254 characters, a missing/duplicated `@` and addresses containing two `@` are rejected; invalid input never reaches the hasher or repository; 1,000,000-character inputs are rejected in under 2 s; ids are unique (also under 50 parallel registrations); `CreatedAt` follows the clock in UTC; repository exceptions propagate unchanged; the caller token reaches the repository.

## 4. Findings

Numbering continues after F-008 in `TEST_CASES_2500.md`. All entries: status **Open**, production code **not changed**. "Cases" lists the failing probes.

### F-009 — Phone number has no format rule (44 failing cases: TC_2501–2538, 2549–2554)

- **Observed:** Service only checks length 1–30 after trim. Accepted: letters (`abcdefghij`), symbols/separators, inner spaces, full-width and Arabic-Indic digits, NUL/zero-width suffixes, `;`/`/` lists, 1–9 digit numbers, 16–30 digit numbers. A single-digit phone such as `0` is stored.
- **Assessment / suggestion:** Probable real gap — phone is used as the customer login identifier (AUTH-002). Confirm the intended format (e.g. 10-digit VN mobile or E.164, max 15 digits) and add a digit/length check after normalisation.
- **Status:** Open; production code not changed.

### F-010 — CCCD has no format rule (36 failing cases: TC_2555–2584, 2595–2600)

- **Observed:** Service only checks length ≤ 20. Accepted: letters, symbols, 1–8 digits, 10/11/13–20 digits, and 12-digit values with invalid province prefixes (`000…`, `097…`, `999999999999`).
- **Assessment / suggestion:** Confirm with the SRS whether CCCD must be 12 digits (and whether 9-digit CMND is allowed). Digits-only is the minimum sensible rule.
- **Status:** Open; production code not changed.

### F-011 — Email validation limited to EmailAddressAttribute (73 failing cases: TC_2601–2654, 2656–2660, 2680–2693)

- **Observed:** Accepted after trim/lower-case: `a..b@…`, leading/trailing dots, `user@-example.test`, `user@example.test.`, characters `( ) , : ; < > [ ] \ "` in the local part, display-name and `mailto:` forms, `user@localhost`, `user@example`, IP literals, port/path/query suffixes, local part > 64 chars, DNS label > 63 chars, non-ASCII local/domain, zero-width/NUL/BEL characters, and `! # $ % ^ & * ( ) = + { } |` in the domain. Only strings with 0 or 2+ `@`, over-254 length, or CR/LF are rejected. Extends F-001 (7 ignored cases) and F-008.
- **Assessment / suggestion:** Probable real gap. Recommend a stricter validator (ASCII-only dot-atom rules, label/length limits) applied identically in the API DTO and the business service.
- **Status:** Open; production code not changed.

### F-012 — Name/address content is not sanitised (51 failing cases excluding normalisation: TC_2701–2730, 2754–2757, 2760–2776)

- **Observed:** Accepted as typed: invisible-only names (`U+200B`, `U+FEFF`, …), control characters (NUL, BEL, ESC, DEL, inner tab/CR/LF), punctuation/digit-only names, `<script>` / SQL / template / CSV-formula payloads, emoji, bidi override (`U+202E`) and `U+FFFD`; the same for address (control, markup, bidi). Invisible-only address is stored instead of being treated as blank (3 cases).
- **Assessment / suggestion:** Decide the content policy. At minimum reject control characters and treat invisible-only text as empty; output encoding must still be handled by the frontend/API consumers.
- **Status:** Open; production code not changed.

### F-013 — No Unicode / whitespace normalisation (12 failing cases: TC_2731–2739, 2798–2800)

- **Observed:** Names and addresses typed in decomposed form (NFD, common on macOS) are stored as typed; repeated inner spaces and inner NBSP are not collapsed. This can create visually identical but different strings.
- **Assessment / suggestion:** Normalise to NFC and collapse inner whitespace before validation/storage, if consistent with the SRS.
- **Status:** Open; production code not changed.

### F-014 — Password rules are length/byte-count only (79 failing cases: TC_2801–2838, 2853–2893)

- **Observed:** Accepted: whitespace-only (8–64 spaces/tabs/NBSP), strings containing NUL/BEL/ESC/DEL (bcrypt truncates at NUL), password equal to email/phone/CCCD/name, and common or patterned passwords (`password`, `12345678`, `Summer2024`, `qwerty123`, `matkhau123`, repeated blocks, single-class strings).
- **Assessment / suggestion:** Split by severity: (a) whitespace-only, control characters and NUL — likely real defects; (b) equal-to-own-data — recommended; (c) deny-list/complexity — policy, count as defects only if the SRS requires them.
- **Status:** Open; production code not changed.

### F-015 — Empty/blank/null password hash is persisted (3 failing cases: TC_2901–2903)

- **Observed:** If `IPasswordHasher.Hash` returns `""`, `" "` or `null`, the service still builds the registration and calls the repository. A faulty hasher would create accounts that cannot log in.
- **Assessment / suggestion:** Add a defensive check (non-empty hash, e.g. BCrypt length) before persisting.
- **Status:** Open; production code not changed.

### F-016 — Pre-cancelled token does not stop registration (3 failing cases: TC_2919–2921)

- **Observed:** The service never calls `cancellationToken.ThrowIfCancellationRequested()`; with a repository that ignores the token, the registration is hashed and persisted. With a repository that honours the token (TC_2916–2918 pass) cancellation works.
- **Assessment / suggestion:** Low severity; optional early `ThrowIfCancellationRequested()` before hashing.
- **Status:** Open; production code not changed.

## 5. How to interpret this run

- The run is red by design: 280 of the 301 failures mean "service accepted something the probe says should be rejected". Treat F-009, F-010, F-011, F-012 (control characters), F-014 (a), F-015 as the strongest candidates; F-013, F-014 (b/c) and the content-policy parts of F-012 need an SRS decision before changing code or deleting probes.
- Do not make the batch green by editing assertions blindly. If a rule is out of scope, mark the probe group `Ignore("…")` with the finding id, as done for F-001.
- Not covered: `CustomerRegistrationConflictException` mapping, and all classes other than `CustomerRegistrationService` (source unavailable). Earlier milestones remain in `TEST_CASES_2500.md` / `TEST_CASES_2000.md`.
