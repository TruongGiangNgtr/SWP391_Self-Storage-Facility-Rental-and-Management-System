# Independent PostgreSQL migrations

Owner-approved migration-only deliverable based on `9871565c9049e197d611e11bb54b238d5ef5838a`. This standalone EF tooling project creates a fresh PostgreSQL database without depending on or changing API/runtime repositories, tests or CI. It reuses the original 27 entity definitions and seed IDs through linked source files; its PostgreSQL model, factory, migrations and snapshot belong to this assembly. Historical SQL Server sources are preserved in the runtime working tree under `backend/sql/archive/sqlserver-migrations`; they cannot be discovered by either PostgreSQL context.

Requirements: .NET SDK 10.0.401, pinned EF tool 10.0.12, an empty UTF-8 PostgreSQL database with ICU support. The complete Pg001–Pg009 chain is verified on PostgreSQL 17.11 and 18.6. Provision the database/server and configure real connection values before applying migrations. No existing business data is imported.

## Connection and commands

From backend, restore the pinned tool and this project:

```powershell
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet restore Frms.Migrations.PostgreSql/Frms.Migrations.PostgreSql.csproj
```

The factory reads the existing ignored Frms.Api/.env in Development. Use one connection alias: ConnectionStrings__Frms, otherwise ConnectionStrings__FrmsDb. Explicit process configuration wins; FRMS_CONNECTION_STRING is the highest-priority EF-only override. Testing/Staging/Production ignore the local file. Never commit .env or credentials.

Create the full database contents:

```powershell
dotnet tool run dotnet-ef -- database update --project Frms.Migrations.PostgreSql --startup-project Frms.Migrations.PostgreSql --configuration Release -- --environment Development
```

Or export a standalone, idempotent SQL deployment file:

```powershell
dotnet tool run dotnet-ef -- migrations script 0 20261015210000_Pg009_PostgreSql17StringSearch --idempotent --output obj/postgresql-deploy/FRMS.PostgreSql.sql --project Frms.Migrations.PostgreSql --startup-project Frms.Migrations.PostgreSql --configuration Release -- --environment Development
```

Run the generated SQL against the already-created database using its real host/user/database values and prompted password:

```powershell
psql -X -v ON_ERROR_STOP=1 -h '<host>' -U '<user>' -d '<database>' -W -f './obj/postgresql-deploy/FRMS.PostgreSql.sql'
```

Do not use the API project as startup for this independent migration chain. Generated obj output is ignored and must be regenerated before deployment. History guards skip applied migration IDs; they do not reconcile independently edited schemas. Do not copy SQL Server exports/history to the new database.

## Schema, seed and redeploy

Pg001..Pg008 create 27 business tables, 40 business functions plus 8 helpers, 52 integrity/audit triggers, constraints/indexes and the payment order-code sequence. Only five roles, Policy v1 and six ACTIVE DamageTypes with NULL default amounts are seeded; no accounts, facilities, prices or ExtraFeeType amounts are invented. Owner selected database-creation timestamps for Policy v1, preserved on repeat deployment.

This project does not switch the application's runtime to PostgreSQL or certify unchanged SQL Server tests. Tests, CI, frontend and broader documentation changes are excluded from this push at the owner's request. Old SQL Server sources remain intact. Latest main changes after the frozen base are not merged here.

Keep migration files and embedded SQL resources immutable after review/deployment; use forward corrections. Protected Down paths refuse unsafe financial/history removal. Rollback is a reviewed source/configuration redeploy preserving database evidence, not automatic PostgreSQL-to-SQL Server data conversion. Do not reuse a fresh order-code sequence starting at 1000 with an already-used live merchant without an approved collision strategy. No production or live-payment call is performed by this source push.

## Verified migration-only commit scope

The exact staged source was restored and built in an isolated checkout: 0 warnings and 0 errors, with NuGet audit enabled. Its model and snapshot have no pending changes. EF database update created an empty temporary database through all 8 migrations; a second update was a no-op. Its exported SQL also applied to a separate empty database and reapplied successfully, preserving approved seeds and Policy timestamps. Catalogue counts were 27 business tables, 48 functions and 52 triggers. Only task-owned verification databases were removed.

No test changes are committed. Existing application runtime and its SQL Server dependency findings remain outside this standalone migration project; no whole-application build or E2E acceptance is claimed by this scoped commit.


## Subsequent runtime integration

The current local runtime task adds this project to `Frms.slnx` and compiles this project's canonical model configuration into DataAccess through a source link. It adds no assembly dependency/cycle and changes none of Pg001–Pg008, their snapshot, SQL resources or schema configuration. Runtime migration discovery is empty; schema deployment still uses this project as both `--project` and `--startup-project`. The earlier migration-only commit statements above describe that commit, not the later local runtime verification.

The full runtime now uses Npgsql. Clean bootstrap, repeat deployment and exact runtime/deployment model agreement are verified through the independent context in the PostgreSQL integration fixture. See the [runtime report](../../docs/POSTGRESQL_RUNTIME_MIGRATION_REPORT.md) for 421 passing .NET tests, real HTTP/Postman checks and Supabase prerequisites. No new commit or push is part of that verification.


## PostgreSQL 17 compatibility and approved Supabase deployment

Pg009 adds `public.frms_contains_ci_as(text,text)` without changing Pg001–Pg008, their SQL resources, the original snapshot or the canonical entity configuration. PostgreSQL17 rejects LIKE on nondeterministic collations. This literal substring helper uses the existing `frms_ci_as` equality, including different-length Unicode equivalents; it does not replace that collation, lowercase values, reinterpret wildcards or change persisted data. The runtime and independent contexts share its EF function metadata/query interceptor source. Single-argument LINQ `string.Contains` executes this parameterized function on both supported versions.

Current catalogue: 27 business tables, 40 business functions plus 9 helpers, 52 triggers, one provider order-code sequence and nine migration history rows. Pg009 is now applied and immutable; future changes require another reviewed forward migration. The helper can inspect quadratically many substrings on an unsuccessful search; no unbounded-search throughput claim is made.

The owner subsequently authorized database-only Supabase deployment and confirmed Data API was already disabled. The approved target was PostgreSQL17.11. Deployment through this independent project completed on 2026-10-15 and live catalogue/seed/literal-search checks passed. No application accounts, payment transactions or operational data were created. API hosting and Git push were not part of deployment. See the current deployment addendum in the [runtime report](../../docs/POSTGRESQL_RUNTIME_MIGRATION_REPORT.md).
