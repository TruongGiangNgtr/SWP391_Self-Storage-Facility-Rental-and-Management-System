# FRMS Release 1 recurring jobs (V11)
> **Source evidence update 2026-11-03:** Pg018 (`InAppNotifications.sql`) excludes `IsInApp` notices and legacy allowlisted business-alert rows from retry-email selection. The 2026-10-29 review warning below is historical; the deployed migration, actual sender configuration and runtime verification remain unverified. Current source defaults the worker ON, but no successful delivery is asserted.

> **2026-11-04 alignment:** OD-06 in-app routing through Pg018 is the approved current design. The legacy email retry job is separate from in-app visibility. Its sender remains unconfigured; runtime/deployment tests are pending. Account registration, email verification, Employee provisioning and role/Facility rules are outside OD-06.

Source baseline: GitHub `main` commit `29a688b`.

## Operations and schedule

| Hosted service | Job task(s) | Cadence | Flag |
|---|---|---|---|
| Existing `ExpirePendingReservationsJob` | `usp_ExpirePendingReservations` | Every 10 minutes | Existing behavior, unchanged |
| `BillingMaintenanceJob` | 1. `usp_GenerateMonthlyInvoices`; 2. `usp_MarkOverdueInvoices`; 3. `usp_RecalculateOpenLateFees` | Startup and every 24 hours | `BackgroundJobs:Billing:Enabled` |
| `ProcessReservationNoShowJob` | `usp_ProcessReservationNoShow` | Startup and every 24 hours | `BackgroundJobs:NoShow:Enabled` |
| `RetryNotificationsJob` | Claim one pending notification, send through `INotificationSender`, then `usp_MarkNotificationSent` | Startup and every 5 minutes | `BackgroundJobs:Notifications:Enabled` |

Default behavior if the settings are absent: **Billing=true**, **NoShow=true**, **Notifications=true**, as set by the three `EnabledByDefault` overrides. The owner chose automatic NoShow processing under SRS OD-11. Startup executes each enabled job immediately, followed by its configured interval. Notification retry cannot deliver through the unconfigured sender. Check deployed migrations and external schedulers before claiming successful execution; explicit `false` disables the corresponding job after restart.

```sh
# Optional explicit overrides; all three jobs default to enabled.
BackgroundJobs__Billing__Enabled=true
BackgroundJobs__NoShow__Enabled=true
BackgroundJobs__Notifications__Enabled=true
```

To pause a specific job, set its `BackgroundJobs__...__Enabled=false` override and restart the API. A real `INotificationSender` adapter remains required for delivery.

The cadence is interval-from-start, not a fixed midnight cron. `usp_GenerateMonthlyInvoices()` derives the current month in `Asia/Ho_Chi_Minh` at invocation time, so a restart or later run repairs missing current-month invoices. SQL prevents billing Contract.StartMonth or out-of-range months and deduplicates invoices.

## Safety and deployment gates

- Application pipeline: BackgroundJob -> Business Service -> Repository -> PostgreSQL function. No Controller or DbContext in jobs.
- The billing group is **ordered**: create invoices before overdue transition; overdue before recalculating fees. If any step fails, later steps do not run on that tick; it retries next day or on application restart.
- PostgreSQL routines use transaction-level advisory locking and database uniqueness checks, including the one-Invoice-per-month invariant. Test parallel API replicas and external scheduler overlap prior to production rollout.
- Notification delivery is **not operational out of the box**. An explicit real `INotificationSender` adapter must be implemented/configured. `UnconfiguredNotificationSender` fails without mutating `NotificationLog`. Do not assume a provider exists or count notification retry as PASS.
- For notifications, the repository uses a global PostgreSQL session advisory lock while sending, **without a database transaction spanning the provider call**. Mark `SENT` only after `SendAsync` success. A crash after delivery but before mark may cause an at-least-once redelivery. Providers should deduplicate using `NotificationLogId`. External direct database schedulers must participate in the same delivery lock if they send notifications.
- Never log notification content, recipient email, provider credentials, payOS URLs, or customer PII.
- Existing SQL functions come from Pg007; this patch adds no schema migration or mutation of immutable migration files.

## Required validation (not performed by patch generation)

1. `dotnet build backend/Frms.slnx` and `dotnet test backend/Frms.slnx`.
2. PostgreSQL integration: verify the full forward migration chain through Pg019 is applied; verify the job routine invocations. Avoid disposable database assumptions that might affect production data.
3. Billing: first month offline, no invoice; generate only active contracts within StartMonth/EndMonth, honor renewal price and captured Discount, create one `RENTAL_FEE` invoice per month on retries and concurrent runners.
4. Overdue + LateFee: use each Contract's captured Policy and GMT+7 thresholds; retry-safe current LateFee only.
5. No-show: do not close completed contracts or valid visits; verify policy window and concurrency.
6. Notification: fake sender success/failure tests; `PENDING` preserved on provider failure; `SENT` only after acknowledgement; two replicas must not send same row concurrently; crash may redeliver, requiring provider idempotency.
7. Monitor the default-enabled flags and verify a production scheduler is not already calling these functions to avoid duplicate work.
