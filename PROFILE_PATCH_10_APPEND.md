## Automation Patch 10 - Authenticated route isolation (2026-11-10)

- Last received full result: `20261110T145651711Z`, `51` executed, `49` passed, `2` failed (`FRMS-ST-0147` and `FRMS-ST-0153`), `0` independently confirmed Jira defects.
- Backend was Healthy; Playwright used 4 workers with 1 retry. Failures are at protected Customer/Staff route transition, not catalog.
- Patch `10_FRMS_AUTH_ROUTE_SESSION_STABILITY.patch` updates only `auth-next.spec.ts` to authenticate through real API, verify ACTIVE CUSTOMER with `/auth/me`, initialize same-origin sessionStorage and then navigate directly. It does not bypass authorization or simulate server success.
- No new environment variables; existing Customer 1 phone/password, `FRMS_API_URL` and other launcher settings suffice.
- Still pending: apply patch in user repository and rerun cases 0147 and 0153, then all 51. Do not report PASS until new logs confirm it.
- Reminder: remote/shared Testing DB; login auditing can write, avoid mutating fixtures.
