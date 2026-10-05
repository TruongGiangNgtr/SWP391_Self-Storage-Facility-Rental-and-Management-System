# FRMS Phase 0 technical-foundation record

## Scope applied

This implementation covers only SRS V10 §6.1 Phase 0 and its exit gate. It establishes the repository layout, backend/frontend build baseline, dependency boundaries, 27-entity EF Core schema baseline, approved reference seed, authentication technical foundation, error/OpenAPI conventions, and test harnesses. It does not implement a Phase 1 business flow. **Status: In Progress** until the blockers below are resolved and verified.

## Traceability

| Baseline requirement | Implementation evidence |
|---|---|
| Controller → service → repository; DTO/Command/Result separation | `Frms.Api` mapping, `Frms.Business` service contracts, `Frms.DataAccess` repository; `Frms.ArchitectureTests` |
| 27 persisted entities and migration | `FrmsDbContext`, `FrmsModelConfiguration`, `20261004145122_InitialPhaseZero`, `20261004191200_PhaseZeroBaselineConstraints` |
| Seed reference data | Role, Policy v1, and DamageType data in `FrmsModelConfiguration.ConfigureSeed`; ExtraFeeType is blocked pending approved amounts |
| Customer phone / employee email authentication | `AuthController`, `AuthenticationService`, `UserAccountRepository` |
| BCrypt/JWT, active-account recheck, errors/logging/OpenAPI | `Program.cs`, `Authentication`, `GlobalExceptionHandler`, API tests |
| UTC + business time zone | `SystemClock`, EF `datetime2(3)` configuration, frontend time utility |
| Test harnesses | NUnit projects, required real-SQL authentication/constraint fixtures, `tests/postman`, `tests/e2e/playwright` |

## Explicit decisions still required

The source material is insufficient to create the following values without inventing business/operational semantics:

1. **ExtraFeeType default amounts.** The allowed fee-type names are specified but their amounts are not. Supply the five approved amounts and currency basis so the required deployment seed can be added without inventing money values.
The SQL Server migration/authentication gate has been verified on a clean local database. The SQL tests require a disposable `Frms_Test_*` database and do not skip missing configuration. Fixture accounts are created through the Testing host only; no production-account seed is added.

All four Business abstractions resolve to Infrastructure placeholders that return `EXTERNAL_PROVIDER_NOT_CONFIGURED` (503). This satisfies the Phase 0 interface/configuration boundary. Actual provider configuration belongs to later provider-dependent workflows and is not an additional Phase 0 blocker.

These items are intentionally boundaries, not omissions that the UI, a fixture, or a default monetary value may fill in.

## Migration and redeploy reasoning

The initial migration was already on the shared remote branch, so its history is preserved. Corrective migration `20261004191200_PhaseZeroBaselineConstraints` adds deterministic status defaults (including Facility INACTIVE and StorageUnit AVAILABLE), Policy.CreatedAt UTC default, Policy start-day <= end-day validation, and the NotificationLog Pending/null versus Sent/non-null SentAt check. It adds no tables, financial values, or business workflows. Both migrations build the 27-table baseline on an empty database.

Before applying to existing data, review rows returned by these queries and obtain an approved correction path; the migration intentionally fails on invalid historical rows and never rewrites them automatically:

```sql
SELECT PolicyId FROM Policy WHERE ReservationVisitStartDay > ReservationVisitEndDay;
SELECT NotificationLogId FROM NotificationLog
WHERE (Status = 'PENDING' AND SentAt IS NOT NULL) OR (Status = 'SENT' AND SentAt IS NULL);
```

Deploy forward using the idempotent migration script documented in README. `Down` removes the new defaults/SentAt check and restores the earlier Policy check without deleting rows or seed data. Rolling back permits weaker invariants, so prefer correcting deployment prerequisites and redeploying forward. Review both directions before deployment; no rollback against a shared database was performed here.

## Remaining exit-gate input

Phase 0 owner Nguyễn Trần Trường Giang must supply the approved non-negative decimal amount for each of `KEY_REPLACEMENT`, `ACCESS_CARD_REPLACEMENT`, `LOCK_REPLACEMENT`, `CLEANING_FEE`, `OTHER`, plus the currency basis. Architecture review owner Bùi Đình Long can review the ensuing seed migration. No amount (including zero) is inferred. After approval, add a forward seed migration and verify its five rows on SQL Server before changing status to Completed.
