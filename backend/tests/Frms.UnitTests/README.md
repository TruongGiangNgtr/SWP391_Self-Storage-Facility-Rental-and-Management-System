# Frms.UnitTests

Unit tests exercise behavior implemented in Business services and validators using in-memory fakes where practical. Current coverage includes authentication success/failure branches, role partitioning, account activity checks, current-account projection, customer registration/profile validation, payment logic, provider options, policy validation, and time conventions.

These tests do not require a live database. They verify the current implementation and should not be expanded to assert workflows that exist only in an SRS or route catalogue.
