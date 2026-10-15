# Frms.DataAccess

Owns repository interfaces/implementations and the 27-table PostgreSQL runtime EF model. Schema, seeds, migrations, triggers and authoritative functions are deployed only by `Frms.Migrations.PostgreSql`. Ordinary queries use EF; critical routines remain atomic database operations. No Service-to-DbContext or Controller-to-Repository dependency is introduced.

`../Frms.Migrations.PostgreSql/Migrations` is the independent chain and snapshot. The runtime source-links that project's canonical model configuration; it compiles no migration classes and discovers no migrations. No circular project reference is introduced. The old SQL Server migration source is preserved verbatim under `../sql/archive/sqlserver-migrations`, outside assembly discovery. PostgreSQL bootstrap uses migrations only, without legacy exports or fabricated migration history.

`../sql/postgresql` SQL is embedded only by the independent migration project and installed by its owning migration. Treat applied migration resources as immutable; add a new resource and forward migration for changes. Functions keep the approved `usp_*` business names in `public` and return named result rows. `PostgreSqlRoutine` binds typed inputs with named `p_snake_case` arguments and explicit SELECT calls; it never uses Npgsql's CALL behavior for SQL Server-style CommandType.StoredProcedure.

Native constraints use SQLSTATE. Custom `Pxxxx` values map to the corresponding legacy 50000+xxxx number at the repository compatibility boundary, preserving public business codes. Stable machine-code messages are compared exactly where one old number represented several codes. No prose substring parsing or secret diagnostics are introduced.

Tables/columns retain quoted PascalCase names. UUIDs, numeric money, bytea, native dates and UTC timestamptz(3) replace vendor-specific SQL Server types. ICU `frms_ci_as` preserves the source case-insensitive/accent-sensitive intent. PostgreSQL owns Policy v1 deployment timestamps; snapshots contain no build-time clock values.

Payment result processing serializes attempt/result reference writes with advisory key70439101 and locks Invoice then Payment. Capacity/lifecycle writers use transaction advisory key70439102 before row locks, including statement triggers for direct EF writes; Policy version allocation uses70439103 and legacy account-creation functions70439104. These conservative locks prioritize correctness; throughput tuning needs separate measured concurrency evidence.

Legacy demo check-in and the obsolete non-idempotent CreatePaymentAttempt signature are excluded. Check-in uses the approved exact server business day; current PaymentRepository owns lifetime idempotent creation. The old CompleteInspection procedure remains excluded because it released Units too early; the existing EF workflow completes Inspection while leaving the Unit in INSPECTION until Finalize Return.


The API composition root supplies the authenticated actor through `AddDatabaseAuditActor`. `PostgreSqlAuditInterceptor` sets `frms.user_account_id` with a bound parameter on every connection open, including an explicit empty value for anonymous/job access. Tests verify attribution and clearing on the same pooled physical connection. Direct/session affinity is required; Npgsql multiplexing is rejected and transaction pooling remains unsupported by this implementation.

Policy creation, renewal and inspection child writes consume the UUID returned by the database function and read that exact row. They do not infer the result from the latest active version, a stale contract snapshot or a before/after row list. `ReturnSqlExecutor` preserves any current EF transaction. Existing function locks, financial formulas, ownership checks and stable business error codes remain unchanged.

See the [runtime report](../../docs/POSTGRESQL_RUNTIME_MIGRATION_REPORT.md) for the repository/function inventory, PostgreSQL 18.6 test evidence and deployment limits.


Pg009 adds the literal ICU substring helper needed by PostgreSQL17. `PostgreSqlStringSearch` maps it into both models and rewrites single-argument `string.Contains` query expressions through a stateless EF query interceptor. Original ICU equality/uniqueness constraints remain unchanged; `%`, `_` and backslash remain literal substring characters. Null operands produce a non-match, and empty fragments match non-null values. Both runtime and deployment use the same metadata source. Real tests cover accented Vietnamese, NFC/NFD, ligatures, width equivalence, column operands and negation, with PostgreSQL18 native LIKE as an independent oracle. PostgreSQL17.11 and18.6 are verified after Pg009. The fallback can inspect quadratically many substrings; benchmark before adding wide/unbounded production searches.
