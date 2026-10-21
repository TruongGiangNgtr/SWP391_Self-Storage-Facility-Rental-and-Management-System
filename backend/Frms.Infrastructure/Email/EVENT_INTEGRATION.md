# FRMS Email + transactional notification integration (Pg014)

Scope: `EMAIL_SERVICE` only. Source: GitHub `main` commit `0219f30734b00b3e5527e3453b19840f08f63639`.
This combined patch includes the earlier standalone SMTP patch **and** Pg014 notification hooks. Apply only the combined patch to an unmodified baseline. It does not replace the SRS or introduce any API endpoints or persisted entity.

## Covered business events

| Source | Event(s) | Queue type |
|---|---|---|
| ADM-005 / ADM-012 (existing) | Employee initial credential provision / resend | Direct `IEmailService` only; **never** `NotificationLog` |
| Reservation | `CONFIRMED`, `CANCELLED` | `RESERVATION_CONFIRMED`, `RESERVATION_CANCELLED` |
| Invoice | DEPOSIT or RENTAL_FEE created; status `PAID`, `OVERDUE`, `CANCELLED` | `DEPOSIT_INVOICE_CREATED`, `RENTAL_INVOICE_CREATED`, `PAYMENT_CONFIRMED`, `INVOICE_OVERDUE`, `INVOICE_CANCELLED` |
| Visit | scheduled / checked-in / checked-out / cancelled / rescheduled | `VISIT_*` |
| Contract | created / renewed / completed or terminated | `CONTRACT_*` |
| Support Ticket | created / status changed | `SUPPORT_TICKET_*` |
| Deposit Settlement | finalized | `DEPOSIT_SETTLEMENT_FINALIZED` |

Only generic content is queued; no initial credential, CCCD, financial amount, address, token, link or password is written into `NotificationLog`. Payment notice requires an authoritative `Invoice.Status -> PAID` transition; late payment success on a CANCELLED invoice does not invent a paid invoice or notice. LateFee daily re-calculation does not create repeated emails; `INVOICE_OVERDUE` triggers only on first status transition. First-month offline rental does not create invoices/notifications.

## Safe deployment — two gates

**Gate A: database enqueue (off until explicitly enabled).** Pg014 installs SQL hooks, with `frms.email_events_enabled` default OFF on absent settings. After reviewing event semantics and running DB tests, enable for the FRMS application database role (replace placeholders, never run against unreviewed production):

```sql
ALTER ROLE app_db_role IN DATABASE frms_db SET frms.email_events_enabled = 'on';
-- Restart API / reset pooled connections; verify using application-role session:
SELECT current_setting('frms.email_events_enabled', true);
-- Emergency stop: ALTER ROLE app_db_role IN DATABASE frms_db RESET frms.email_events_enabled;
```

This flag is evaluated per PostgreSQL **session**. Do not enable through `SET LOCAL` on an unrelated DBA session and expect application connections to inherit it. An app-role/user-supplied `SET frms.email_events_enabled = 'on'` is possible if DB role is granted SQL access, so this flag is an **operational gate, not a security boundary**; DB authorization remains mandatory.

**Gate B: SMTP delivery (off until tested).** Configure from secret manager/environment, then enable existing worker:

```text
Email__Provider=Smtp
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__FromEmail=<authorized address>
Email__Smtp__Username=<authorized user>
Email__Smtp__Password=<ROTATED SECRET>
Email__Smtp__TimeoutSeconds=30
BackgroundJobs__Notifications__Enabled=true
BackgroundJobs__Notifications__BatchSize=20
```

NOTE: the uploaded example contained a plaintext Gmail credential. **Revoke and rotate** it before any real send. Never commit or log it. SMTP sender only proves provider acceptance, not inbox delivery. Retry after a crash may double-deliver; SMTP does not offer `NotificationLogId` idempotency. Worker now drains a **configurable bounded batch** every 5 minutes: `BackgroundJobs__Notifications__BatchSize=20` by default (range 1..100). Production backlog throughput and SMTP rate limits still require explicit sizing before enablement. Do not enable in production until this capacity and at-least-once semantics are accepted.

## Deploy order / tests

1. Apply the single combined patch to commit `0219f307` on a local working branch; do not apply previous SMTP patch separately.
2. `dotnet build backend/Frms.slnx`, `dotnet test backend/Frms.slnx`.
3. Apply Pg014 in an isolated PostgreSQL test database. Use application role with Gate A OFF: test updates/inserts in all covered tables queue **zero** rows. Set gate ON in a transaction/session and verify each event queues exactly **one** row on a real transition, none on a no-op update or rollback; test access/RBAC, first-month-offline and duplicate webhook.
4. Unit/integration test ADM-005/ADM-012 email success/failure, notification provider missing/failed/sent, provider password revocation, SMTP timeout/cancellation, restart, multi-instance retry and at-least-once re-delivery.
5. Compare queue growth per day to worker capacity: at BatchSize=20 the theoretical maximum is 240/hour or 5,760/day (lower in practice due to SMTP latency, errors, contention and API replicas). Use a conservative BatchSize for Gmail/provider quotas.
6. Approve event subscription/privacy copy and monitor PENDING/SENT. Revisit email templates and notification opt-outs only if separately approved, not as part of this patch.

No GitHub writes, DB tests, SMTP sends or .NET build were executed during patch construction.
