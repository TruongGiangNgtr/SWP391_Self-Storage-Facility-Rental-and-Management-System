# FRMS — All API implementation batch V6 / static handoff

SESSION-ID: FRMS-ALL-API-20261014
ACTIVE FEATURE: All APIs
PHASE: Implementation / Final Release
CURRENT STEP: Static source patch; local compile and SQL integration pending
ROLE: Backend implementation
TEST STATUS: Git patch check PASS (synthetic source fixture); .NET build/NUnit/SQL E2E NOT RUN

## Source priority and branch

- Branch: `be/PATCH/1410`; base audit commit `bcc4f3917042045302d7550c2529d4a9d3a2bf87`.
- SRS: `/mnt/data/FRMS_SRS_V10.2.md`; Dictionary: `/mnt/data/03-FRMS_Data_Dictionary_V2_1.md`; Scope: `/mnt/data/FRMS_scope_0110_V8_revised.md`.
- Required `FRMS_IMPLEMENTATION_HANDOFF.md` not found on accessible branch or local Project files. The patch is not final-release-certified.
- No modifications to GitHub; only local .patch artifacts.

## Implemented code replacing HTTP 501 — 19 points

### Customer (5 — prior V3 combined)

- `AUTH-005`: PATCH `/api/v1/customers/me/profile` — verified allowlist, same-user, active status, CCCD uniqueness.
- `UX-RBAC-12-LIST/DETAIL`: GET Facility Customers — assigned-facility and related Reservation/Contract filtering, before pagination.
- `UX-RBAC-14-LIST/DETAIL`: GET BOM Customers — minimal read-only DTO.

### Admin (5)

- `ADM-003/004`: POST Customer activate/deactivate — uses **existing dbo.usp_SetUserAccountStatus** with parameters `@UserAccountId`, `@NewStatus`; SQL error 51149 -> controlled 409.
- `ADM-010/011`: GET login-history and audit-logs — paginated server-side filtered read.
- `UX-RBAC-01`: GET Admin Facility lookup — minimal DTO; originally PROPOSED; owner authorization/route confirmation still required.

### BOM/Staff/Manager (7)

- `UX-RBAC-16-LIST/DETAIL`: GET BOM Employees — minimal directory DTO without credentials/PII.
- `BOM-011/012`: Create/update Customer Discount — BOM-only, frozen financial fields for Contract-referenced Discounts; serializable transaction guard on update.
- `BOM-013/014`: Read ExtraFeeType (Staff/Manager ACTIVE-only with assigned Facility; BOM all), BOM update default amount/status.
- `UX-RBAC-09`: Manager Staff lookup — same-Facility, ACTIVE Staff only; route originally PROPOSED, owner approval still required.

### Public (2)

- `CAT-005`: Public UnitType list — DISTINCT offered UnitTypes linked to ACTIVE Facilities, filters, sort and pagination before return.
- `CAT-004-PUBLIC`: Public UnitType detail — ACTIVE Facility offering required; explicit operational view retains authentication.

## Corrected 7 disallowed BOM branches (not implemented as BOM reads)

The SRS V10.2 section 13.11 explicitly does NOT grant BOM access to these operations. Prior scaffolds granted a role attribute then returned 501. Patch removes BOM from gates and removes now unreachable 501 checks:

- `INS-001/002-BOM`: inspections list/detail.
- `UNIT-001/003-BOM`: storage-unit list/detail.
- `SUP-002/003-BOM`: support tickets list/detail.
- `REP-001-BOM`: Facility operations report.

Additionally, `CON-001` list disallows Staff per SRS section 13.11.1; Staff still has authorized Contract detail, billing and return-summary operations where explicitly allowed. `REP-002` Facility revenue report also becomes Manager-only per the same canonical table.

## Remaining 9 points / BLOCKERS

- `CAT-001-SEARCH`: public Facility full filtering including inclusive future-month capacity. Requires server-side period eligibility before count/page, not a paginated FE filter.
- `CAT-003-PUBLIC`: full public UnitType availability/period calculation, `availableOnly` and server-side filters. Existing `CapacityService` requires authenticated operational context; cannot expose it without safe public projection and authoritative full-period algorithm.
- `PUB-POL-001/002`: approved published article content not provided. Must not invent content, thresholds or policy text. Current ACTIVE Policy snapshot alone is insufficient to represent published articles.
- `AI-001`: optional external AI provider/adapter and public rate-abuse protections are not established in this patch; provider outputs must be checked against public offering.
- `REP-002/003/004`: exact CALC-REP-02 first-month Contract recognition plus paid later RENTAL_FEE invoices, scoped Facility/system aggregation and UTF-8 CSV export require a full financial integration test; no Invoice-only shortcut.
- `UX-RBAC-11`: Admin deactivation eligibility is a PROPOSED supplementary route/DTO. SQL already guarantees actual `ADM-004` deactivation protection; don't silently invent response fields.

These nine are NOT implemented; total 19 implemented + 7 corrected unauthorized branches + 9 pending = 35 original 501 points.

## Static checks

- Combined patch: `git apply --check` PASS on synthetic fixture built from exact retrieved base source excerpts, including Customer Controller, DI registrations, impacted Controller snippets, existing tests.
- `git apply` PASS on the same fixture; `git diff --check` PASS.
- Test fix removes references to deleted `*ScaffoldController` types. It also removes obsolete assertion that Staff can call `CON-001`.
- The synthetic fixture is NOT the user's Windows working tree. No `dotnet` executable exists in this environment; build and NUnit are NOT verified. Database/provider tests are NOT verified.
- Changes still require technical review for behavior, security, EF query translation, SQL concurrency, migration and deployment compatibility.
- No GitHub write/commit/push executed.

## Applying the patch

Use exactly ONE based on working tree status:

**Clean branch matching source (no Customer V3, no previous test hotfix):**

```powershell
git apply --check FRMS_ALL_API_IMPLEMENTATION_PARTIAL_V6_CLEAN_BRANCH.patch
git apply FRMS_ALL_API_IMPLEMENTATION_PARTIAL_V6_CLEAN_BRANCH.patch
```

**Customer V3 applied, but no previous test hotfix:**

```powershell
git apply --check FRMS_ALL_API_IMPLEMENTATION_PARTIAL_V6_AFTER_CUSTOMER.patch
git apply FRMS_ALL_API_IMPLEMENTATION_PARTIAL_V6_AFTER_CUSTOMER.patch
```

**Customer V3 and previous `FRMS_FIX_API_TESTS_AFTER_CONTROLLER_CONSOLIDATION.patch` already applied:**

```powershell
git apply --check FRMS_ALL_API_IMPLEMENTATION_V6_AFTER_CUSTOMER_AND_TESTFIX.patch
git apply FRMS_ALL_API_IMPLEMENTATION_V6_AFTER_CUSTOMER_AND_TESTFIX.patch
```

Do NOT stack variants; do NOT force, `--reject`, or reapply already present Customer files. Stop if `--check` fails and inspect `git diff` / local source.

Then:

```powershell
cd backend
dotnet build
dotnet test
```

Manual checkpoints: Customer Admin activate/deactivate SQL 409, Discount Contract-reference freeze, filters before pagination, role-403 matrix, Guest allowlist, UnitType ACTIVE offering, and error envelopes.

Frontend integration GAP: Admin Facility assignment still uses `/api/v1/business/facilities` (BOM-only). It must switch to the new Admin lookup after route/DTO approval; patch does NOT change FE.

## Final release

**FAIL: full all-API implementation/DoD not complete.** This deliverable is a partial implementation batch with explicit known gaps.
