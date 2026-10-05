# Frms.IntegrationTests

Contains host/registration checks and real SQL Server authentication and schema tests. Set `FRMS_TEST_CONNECTION_STRING` to a disposable database named `Frms_Test_*` before the root solution test command. Missing configuration fails the gate. Fixtures migrate that database; schema fixtures roll back and authentication fixture accounts remain only in that test database.

`INT_AUTH_001` uses the real API/repository/BCrypt/JWT path for customer phone login, employee email login, current-account identity/facility scope, inactive login rejection, and rejection of an already-issued JWT after deactivation. `DBT_PHASE0_001..003` exercise SQL default/UTC timestamp behavior, valid/equal/reversed Policy day ranges, and all Pending/Sent + null/non-null SentAt combinations.

These Phase 0 checks do not claim completion of Release 1 `DBT-01..25`, concurrency suites, or business journeys; those require their authoritative procedures/workflows in later phases.
