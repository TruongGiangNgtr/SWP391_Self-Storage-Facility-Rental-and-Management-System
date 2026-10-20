# Frms.IntegrationTests

The current DataAccess implementation registers Entity Framework Core with PostgreSQL/Npgsql (`UseNpgsql`) and the schema configuration lives in `Frms.Migrations.PostgreSql`. Active integration-project tests therefore focus on PostgreSQL provider registration, design-time connection configuration, and the EF model without requiring a live database.

Several historical integration fixtures in this folder still target SQL Server (`Microsoft.Data.SqlClient`, `UseSqlServer`, `dbo.*`, `sys.tables`, and T-SQL). They are intentionally excluded from compilation by `Frms.IntegrationTests.csproj` until they are ported to PostgreSQL. They are retained as source reference only and must not be reported as active or passing tests. The previous `FRMS_TEST_CONNECTION_STRING`/`Frms_Test_*` instructions applied to those SQL Server fixtures and are no longer the setup contract for the active test suite.

No SRS-only workflow is assumed to be implemented. Each test should be added to the active suite only after its target behavior is confirmed in the current code and its database assumptions match the provider used by the application.
