# BackgroundJobs boundary

Application-scheduled work belongs here and must call a `Frms.Business` service only:

```text
Api BackgroundJob -> Business Service -> Repository -> EF Core / Stored Procedure
```

No hosted service or scheduled-job logic is included in this scaffold. A background job must never depend directly on `FrmsDbContext`, repository interfaces, or stored-procedure access.
