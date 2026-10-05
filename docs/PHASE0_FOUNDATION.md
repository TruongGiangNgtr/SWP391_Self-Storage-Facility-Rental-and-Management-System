# FRMS Phase 0 technical-foundation record

## Scope applied

This implementation covers only SRS V10 §6.1 Phase 0 and its exit gate. It establishes the repository layout, backend/frontend build baseline, dependency boundaries, 27-entity EF Core schema baseline, approved reference seed, authentication technical foundation, error/OpenAPI conventions, and test harnesses. It does not implement a Phase 1 business flow. **Status: Completed — verified 2026-10-05.** All mandatory checks passed, including real SQL Server tests and the committed branch whitespace check.

## Traceability

| Baseline requirement | Implementation evidence |
|---|---|
| Controller → service → repository; DTO/Command/Result separation | `Frms.Api` mapping, `Frms.Business` service contracts, `Frms.DataAccess` repository; `Frms.ArchitectureTests` |
| 27 persisted entities and migration | `FrmsDbContext`, `FrmsModelConfiguration`, `20261004145122_InitialPhaseZero`, `20261004191200_PhaseZeroBaselineConstraints` |
| Required Phase 0 reference seed | Five UserRoles, Policy v1 and six fixed DamageTypes in `FrmsModelConfiguration.ConfigureSeed`; ExtraFeeType rows are deferred beyond Phase 0 |
| Customer phone / employee email authentication | `AuthController`, `AuthenticationService`, `UserAccountRepository` |
| BCrypt/JWT, active-account recheck, errors/logging/OpenAPI | `Program.cs`, `Authentication`, `GlobalExceptionHandler`, API tests |
| UTC + business time zone | `SystemClock`, EF `datetime2(3)` configuration, frontend time utility |
| Test harnesses | NUnit projects, required real-SQL authentication/constraint fixtures, `tests/postman`, `tests/e2e/playwright` |

## Owner-approved Phase 0 seed clarification

On 2026-10-05, the owner clarified the seed scope in SRS §6.1.1/§9.13 and Data Dictionary §30: Phase 0 requires only five UserRoles, Policy v1 and six fixed DamageTypes. ExtraFeeType entity/table and its CHECK constraints remain valid baseline structure, with zero seeded rows. Its five categories require approved amounts and currency in a later deployment; no values are inferred here.

The SQL Server migration/authentication gate has been verified on a clean local database. The SQL tests require a disposable `Frms_Test_*` database and do not skip missing configuration. Fixture accounts are created through the Testing host only; no production-account seed is added.

All four Business abstractions resolve to Infrastructure placeholders that return `EXTERNAL_PROVIDER_NOT_CONFIGURED` (503). This satisfies the Phase 0 interface/configuration boundary. Actual provider configuration belongs to later provider-dependent workflows.

No UI, fixture or adapter supplies unapproved monetary defaults.

## Migration and redeploy reasoning

The initial migration was already on the shared remote branch, so its history is preserved. Corrective migration `20261004191200_PhaseZeroBaselineConstraints` adds deterministic status defaults (including Facility INACTIVE and StorageUnit AVAILABLE), Policy.CreatedAt UTC default, Policy start-day <= end-day validation, and the NotificationLog Pending/null versus Sent/non-null SentAt check. It adds no tables, financial values, or business workflows. Both migrations build the 27-table baseline on an empty database.

Before applying to existing data, review rows returned by these queries and obtain an approved correction path; the migration intentionally fails on invalid historical rows and never rewrites them automatically:

```sql
SELECT PolicyId FROM Policy WHERE ReservationVisitStartDay > ReservationVisitEndDay;
SELECT NotificationLogId FROM NotificationLog
WHERE (Status = 'PENDING' AND SentAt IS NOT NULL) OR (Status = 'SENT' AND SentAt IS NULL);
```

Deploy forward using the idempotent migration script documented in README. `Down` removes the new defaults/SentAt check and restores the earlier Policy check without deleting rows or seed data. Rolling back permits weaker invariants, so prefer correcting deployment prerequisites and redeploying forward. Review both directions before deployment; no rollback against a shared database was performed here.

## Validation and phase boundary

Phase 0 can close based on the actual mandatory validation results in `PHASE0_MERGE_VALIDATION.md`: 53 NUnit tests passed without skips, the required reference seed was verified on SQL Server, frontend/Postman/Playwright passed, and `git diff --check origin/main...HEAD` passed after commit `1acc349`. Release 1 business workflows, real provider integrations, and approved later ExtraFeeType deployment data remain outside this change. Phase 0 owner: Nguyễn Trần Trường Giang; architecture review owner: Bùi Đình Long.
