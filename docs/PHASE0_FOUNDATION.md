# FRMS Phase 0 technical-foundation record

## Scope applied

This implementation covers only SRS V10 §6.1 Phase 0 and its exit gate. It establishes the repository layout, backend/frontend build baseline, dependency boundaries, 27-entity EF Core initial schema, approved reference seed, authentication technical foundation, error/OpenAPI conventions, and test harnesses. It does not implement a Phase 1 business flow.

## Traceability

| Baseline requirement | Implementation evidence |
|---|---|
| Controller → service → repository; DTO/Command/Result separation | `Frms.Api` mapping, `Frms.Business` service contracts, `Frms.DataAccess` repository; `Frms.ArchitectureTests` |
| 27 persisted entities and migration | `FrmsDbContext`, `FrmsModelConfiguration`, `20261004145122_InitialPhaseZero` |
| Seed reference data | Role, Policy v1, and DamageType data in `FrmsModelConfiguration.ConfigureSeed` |
| Customer phone / employee email authentication | `AuthController`, `AuthenticationService`, `UserAccountRepository` |
| BCrypt/JWT, active-account recheck, errors/logging/OpenAPI | `Program.cs`, `Authentication`, `GlobalExceptionHandler`, API tests |
| UTC + business time zone | `SystemClock`, EF `datetime2(3)` configuration, frontend time utility |
| Test harnesses | NUnit projects, `tests/postman`, `tests/e2e/playwright` |

## Explicit decisions still required

The source material is insufficient to create the following values without inventing business/operational semantics:

1. **Employee/customer test accounts.** No approved identity, phone/email, password, or BCrypt hash is provided. Supply approved non-production identities and a secure provisioning path, then seed or provision them outside committed secrets.
2. **ExtraFeeType default amounts.** The allowed fee-type names are specified but their amounts are not. Supply an approved deployment-data set with a currency/value basis before seeding them.
3. **Concrete provider adapters.** SRS V10 does not select/configure provider credentials for email, notifications, MoMo, or AI. The Business boundary and Infrastructure composition point exist; provider behavior belongs to an approved Phase 1/operational decision.
4. **Background jobs.** No Phase 0 job schedule or business semantics is specified. The API registration boundary is present and architecture-tested, but no job is registered.

These items are intentionally boundaries, not omissions that the UI, a fixture, or a default monetary value may fill in.
