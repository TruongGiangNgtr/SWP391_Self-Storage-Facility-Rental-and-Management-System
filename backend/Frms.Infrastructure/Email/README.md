# FRMS Email Service — SMTP (ADM-005 / ADM-012 / Notification delivery)

> **2026-10-29 — Notifications-only review, NOT approved for implementation.** [REV-2026-10-29-INAPP-NOTIFICATIONS](../../../docs/FRMS_InApp_Notifications_Review.md). The pending OD-06 proposal removes SMTP/SENT as the visibility gate for eligible in-app business alerts. Current mail adapters and account-email flows remain source evidence and are not changed now. Worker routing must exclude in-app-only messages after approval. Account registration, email verification, Employee provisioning and role/Facility rules are unchanged.

## Source and security

- Uses the existing `IEmailService` and `INotificationSender` Business abstractions.
- Implements `SmtpInitialCredentialEmailService`, `SmtpNotificationSender`, and shared `SmtpEmailTransport` in `Frms.Infrastructure` only.
- `Email:Provider` is opt-in (`Smtp` or `Unconfigured`). An unset value preserves the existing 503/unconfigured behavior. Unsupported provider names fail on app registration. SMTP options are validated when starting the application.
- **No hard-coded secrets.** The `EmailProvider.cs` sample uploaded during implementation included a live-looking Gmail App Password. **Revoke/rotate that password** before enabling SMTP. Never commit `.env`, passwords, mail content, or customer addresses to logging.
- SMTP credentials must be injected through environment variables or a secrets manager. Do not put real values in `appsettings.json`, source, patch or documentation.

Example environment variable names (values intentionally omitted):

```text
Email__Provider=Smtp
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__FromEmail=<approved-sender-address>
Email__Smtp__Username=<approved-smtp-login>
Email__Smtp__Password=<NEW-secret-from-secret-manager>
Email__Smtp__TimeoutSeconds=30
BackgroundJobs__Notifications__Enabled=false
```

Port 465 (implicit TLS/SMTPS) is explicitly rejected. The transport requires STARTTLS via `System.Net.Mail.SmtpClient.EnableSsl = true` (submission port 587). SMTP server must support STARTTLS and authenticate the configured account. Set the `FromEmail` to an address authorized by that account; for Gmail use an appropriately scoped app password or approved organizational auth, after revoking any exposed credential. This implementation uses username/password authentication and does **not** implement OAuth2.

## Existing ADM flow (no Controller/Service mutation)

1. `POST /api/v1/admin/employees`: Business persists a newly created **INACTIVE** Employee account and BCrypt hash, then `IEmailService.SendInitialCredentialAsync` submits the initial password, then activates the account only on successful submission.
2. `POST /api/v1/admin/employees/{employeeId}/resend-initial-credential`: Business allows only `INACTIVE` accounts, rotates the hash before SMTP send and activates only after successful submission.
3. Failed SMTP submission throws a sanitized error; the Business flow returns `503 EXTERNAL_PROVIDER_UNAVAILABLE`. Passwords are never exposed in API responses or logs.
4. SMTP acceptance is **not proof of delivery to the inbox**. Production smoke testing is required. The current Release 1 SRS excludes a self-service password-change/reset workflow; the email must not promise one.

## Notifications

When `Email:Provider=Smtp`, `INotificationSender` uses the same SMTP transport. The existing background notification job stays **off by default** (`BackgroundJobs:Notifications:Enabled=false`), even when SMTP credentials are configured. Only enable it after approval and production integration tests of the existing PostgreSQL queue/lease and `SENT` transition.

SMTP cannot guarantee exactly-once delivery across crashes. Notifications may be resent if SMTP accepted a message but the process crashed before marking `NotificationLog.SENT`. Do not claim provider-side deduplication by `NotificationLogId`. No new API or table is introduced.

## Validation before release

- `dotnet build backend/Frms.slnx`
- `dotnet test backend/tests/Frms.UnitTests/Frms.UnitTests.csproj`
- Tests with sandbox SMTP (approved account): credential delivery, resending, Unicode, bad credentials, STARTTLS failure, timeouts, cancelled requests; no secret logging.
- API + DB integration: ADM-005/ADM-012 account stays `INACTIVE` if SMTP submission fails and becomes `ACTIVE` only after success.
- Notification tests on PostgreSQL: `PENDING` on failure, `SENT` after send success, retry and cross-process concurrency and crash/re-delivery semantics.

No .NET SDK, PostgreSQL instance or SMTP credentials are included in the offline patch generation environment. Do not mark E2E or production SMTP as PASS without running them.
