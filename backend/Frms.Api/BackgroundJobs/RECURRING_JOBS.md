# FRMS Release 1 recurring jobs (V11)

Patch baseline: GitHub `main` commit `8ca520300fae904ec86e282aa977a591062b6f6c`.

## Operations and schedule

| Hosted service | Job task(s) | Cadence | Flag |
|---|---|---|---|
| Existing `ExpirePendingReservationsJob` | `usp_ExpirePendingReservations` | Every 10 minutes | Existing behavior, unchanged |
| `BillingMaintenanceJob` | 1. `usp_GenerateMonthlyInvoices`; 2. `usp_MarkOverdueInvoices`; 3. `usp_RecalculateOpenLateFees` | Startup and every 24 hours | `BackgroundJobs:Billing:Enabled` |
| `ProcessReservationNoShowJob` | `usp_ProcessReservationNoShow` | Startup and every 24 hours | `BackgroundJobs:NoShow:Enabled` |
| `RetryNotificationsJob` | Claim one pending notification, send through `INotificationSender`, then `usp_MarkNotificationSent` | Startup and every 5 minutes | `BackgroundJobs:Notifications:Enabled` |

Default behavior if the settings are absent: **Billing=true**, **NoShow=false**, **Notifications=false**. Billing starts processing immediately when the API starts and then every 24 hours; ensure Pg007/Pg001-Pg013 are deployed, validate the financial routines, and check no external scheduler already duplicates the work before deployment. An explicit `false` still disables Billing.

```sh
# Explicit overrides; omitting Billing flag has the same enabled result.
BackgroundJobs__Billing__Enabled=true
BackgroundJobs__NoShow__Enabled=false
# DO NOT enable notifications until a real INotificationSender adapter
# is configured and tested; DI still registers UnconfiguredNotificationSender.
BackgroundJobs__Notifications__Enabled=false
```

For emergency pause use `BackgroundJobs__Billing__Enabled=false` and restart the API.

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
2. PostgreSQL integration: Pg001-Pg013 migration deployed; verify the five new routine invocations. Avoid disposable database assumptions that might affect production data.
3. Billing: first month offline, no invoice; generate only active contracts within StartMonth/EndMonth, honor renewal price and captured Discount, create one `RENTAL_FEE` invoice per month on retries and concurrent runners.
4. Overdue + LateFee: use each Contract's captured Policy and GMT+7 thresholds; retry-safe current LateFee only.
5. No-show: do not close completed contracts or valid visits; verify policy window and concurrency.
6. Notification: fake sender success/failure tests; `PENDING` preserved on provider failure; `SENT` only after acknowledgement; two replicas must not send same row concurrently; crash may redeliver, requiring provider idempotency.
7. Explicitly approve and monitor flag activation; verify production scheduler is not already calling these functions to avoid duplicate work.
