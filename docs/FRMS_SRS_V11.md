# FRMS — Software Requirements Specification (SRS)

> **2026-10-29 — Notifications-only review, NOT approved for implementation.** [REV-2026-10-29-INAPP-NOTIFICATIONS](FRMS_InApp_Notifications_Review.md). OD-06 proposes in-app notifications independent of SMTP/SENT/SentAt. No FE/BE/DB/SQL/configuration or test-code edits are authorized before documentation and routing/contracts are approved. Account registration, email verification, Employee provisioning and role/Facility rules are unchanged.

**Project:** Self-Storage Facility Rental and Management System (FRMS)
**Document:** Software Requirements Specification + Architecture + Testing Baseline (API contracts maintained separately)
**Version:** V11.0 REVIEW CANDIDATE
**Baseline Date:** 2026-10-02
**Architecture Revision Date:** 2026-10-04
**Business Revision Date:** 2026-10-07 — REV-2026-10-07-FM-OFFLINE
**Operational Consistency Revision Date:** 2026-10-07 — REV-2026-10-07-OP-READ
**Payment Gateway Revision Date:** 2026-10-09 — REV-2026-10-09-PAYOS
**Payment Idempotency Clarification Date:** 2026-10-10 — REV-2026-10-10-PAY-IDEMPOTENCY
**Public Guest Requirements Revision Date:** 2026-10-13 — REV-2026-10-13-GUEST
**Public Guest Requirements Approval:** Team approved on 2026-10-13; confirmed by the project owner
**Customer Profile Revision Date:** 2026-10-20 — `REV-2026-10-20-CUSTOMER-NO-ADDRESS` (historical V10 decision, superseded for optional address collection by V11 §0.0.9)
**V11 Owner Decision Date:** 2026-10-20 — `REV-2026-10-20-V11-OWNER-DECISIONS` (partial normative approvals; release certification pending)
**V11 Reconciliation Date:** 2026-10-20 — `REV-2026-10-20-V11-CODE-ALIGNMENT` (draft), augmented by owner-approved decisions in §0.0.9
**V11 Recurring Jobs Documentation Update:** 2026-10-21 — `REV-2026-10-21-BACKGROUNDJOBS` (source `main` `0219f30734b0`; code registered, runtime/test acceptance pending)
**V11 Payment Documentation Cleanup:** 2026-10-21 — superseded provider-specific clauses removed; payOS-only active requirements and immutable historic payment evidence preserved
**Customer Sent Notification Read Approval:** `REV-2026-10-22-CUSTOMER-NOTIFICATIONS-READ` — owner-approved read-only Notifications page and Overview preview, limited to §0.0.9 OD-06; no release-test certification
**Customer Transaction History Approval:** `REV-2026-10-22-CUSTOMER-TRANSACTION-HISTORY-READ` — owner-approved own Payment history requirements in §0.0.9 OD-07; requirement approval only, implementation and test evidence remain separate
**CCCD Format Approval:** 2026-10-26 — `REV-2026-10-26-CCCD-12-DIGITS` (OD-10); owner-approved requirements; FE implementation and backend/database acceptance tracked separately.
**Customer CCCD / Operation Continuation Approval:** 2026-10-25 — `REV-2026-10-25-CUSTOMER-CCCD-CONTINUATION`, team approval confirmed by owner; §0.0.9 OD-09 and §14.15 define the approved target, not implementation/release acceptance
**V11 API Specification:** [`FRMS_API_V11.md`](FRMS_API_V11.md) (HTTP contract companion)  
**V11 Data Dictionary:** [`FRMS_Data_Dictionary_V11.md`](FRMS_Data_Dictionary_V11.md) (entity, PostgreSQL schema, routines and tests companion)
**Status:** V11 REVIEW CANDIDATE — owner-approved decisions in §0.0.9 effective for V11 target; full document ratification, technical-architecture sign-off, API-code parity and release tests pending
**Manager/BOM reporting alignment (2026-10-27):** Source-reviewed implementation-documentation proposal for MWP-06/BWP-07 and REP-RD-01..03; does not assert new owner approval, deployed-code parity or release acceptance.
**Architecture:** 3 Logical Layers + Repository Pattern + Infrastructure Adapters
**Backend:** ASP.NET Core Web API / C#
**Frontend:** React + TypeScript
**Database:** PostgreSQL / Npgsql / EF Core **observed on `main`; V11 architecture approval pending**
**ORM:** Entity Framework Core / Npgsql
**Unit Testing:** NUnit
**API Testing:** Postman
**UI / E2E Testing:** Playwright
**AI-assisted Development Tools:** ChatGPT / Claude / Gemini

---

## Version History

| Version | Status | Purpose |
|---|---|---|
| V1 | Skeleton Baseline | Established the SRS structure, authority order, architecture, API contract format, testing groups, phases, and locked implementation decisions. |
| V2 | Business / Data / Lifecycle Baseline | Populated approved scope, actors, flows, feature catalogue, data/lifecycle rules, calculations, procedures/jobs, critical errors, pseudocode, database tests, and phase acceptance gates. |
| V3 | API Contract / Frontend Integration Baseline | Locked the REST endpoint inventory, reusable JSON schemas, request/response templates, pagination/error conventions, FE API-module mapping, and feature-to-API traceability. |
| V4 | Final Implementation Baseline | Completed authorization, retry/idempotency, NFRs, release gates, deferred-decision handling, final traceability and acceptance rules. |
| V5 | Logic & Wording Review | Reconciled internal contradictions, strengthened normative wording, and explicitly identified source gaps. |
| V6 | Support Category Lock | Resolved DD-09 and locked the SupportTicket category catalogue as a fixed enum for Release 1. |
| V7 | Contract Policy Capture Lock | Resolved DD-12: Contract inherits Reservation.PolicyId at Complete Handover. |
| V8 | Low-impact Deferred Decision Lock | Resolved low-impact technical/operational defaults while preserving high-impact decisions as Deferred. |
| **V9 FINAL** | Remaining Deferred Decisions Lock | Resolves DD-01, DD-02, DD-13 and DD-15; completes Employee credential email provisioning, Manager Damage decision, first-month Discount rule, and production DamageType seed. |
| **V9.1 EDITORIAL** | Version-reference consistency correction | Replaces stale current-baseline references to V5/V6/V7 with V9.1/current-SRS wording. No actor, scope, business rule, lifecycle, API contract, data model, security rule, calculation, or test requirement is changed. Historical version references remain unchanged where they describe actual document history or decision provenance. |
| **V10 FINAL** | Backend Structure and Dependency Boundary Lock | Preserves V9 business semantics while locking API DTO mapping, Business Command/Result models, provider abstractions and adapters, dependency injection composition, background-job hosts, stored-procedure access organization, and architecture tests. Aligns the database authority reference to Data Dictionary V2.1. |
| **V10 FINAL — 2026-10-05 clarification** | Owner-approved Phase 0 seed scope | Phase 0 requires UserRole, Policy v1 and the fixed DamageType catalogue only. ExtraFeeType schema/constraints remain; its five rows are deferred beyond Phase 0 until amounts and currency are approved. No rental/payment rule changes. |
| **V10 FINAL — 2026-10-07 business revision** | Owner-approved offline first-month collection | REV-2026-10-07-FM-OFFLINE replaces first-month online Payment/Invoice with Staff's offline-receipt acknowledgment through OPS-004 and Contract-based settlement. PAY-002 is retired; billing starts after Contract.StartMonth; first-month revenue uses Reservation.LockedRentalPrice in Contract.StartMonth. |
| **V10 FINAL — 2026-10-07 operational consistency revision** | Owner-approved operational reads, return release and reporting | REV-2026-10-07-OP-READ aligns Staff/Manager/BOM reads and operational catalogues; locks Unit release at Finalize Return, usageRate over all scoped Units, Visit work-item navigation and resolved DamageType wording. No new entity or first-month payment change. |
| **V10 FINAL — 2026-10-09 payOS revision** | Owner-approved payOS gateway (code/mock baseline) | REV-2026-10-09-PAYOS; protect historical payment records, retain numeric order-code sequence and signed JSON webhook semantics; release acceptance pending. |
| **V10 FINAL — 2026-10-08 payment timestamp clarification** | Owner-approved persisted Invoice payment timestamp | Invoice.PaidAt is nullable UTC, set from the verified provider timestamp only on the first UNPAID/OVERDUE -> PAID transition. Duplicate webhook, subsequent successful attempts and cancelled invoices preserve the Invoice timestamp. Existing historical timestamps are not fabricated. |
| **V10 FINAL — 2026-10-10 payment idempotency clarification** | Owner-requested alignment with the approved PAY-001 contract | REV-2026-10-10-PAY-IDEMPOTENCY makes the frontend UUID, required persisted uniqueidentifier, global uniqueness, lifetime retention, retry/concurrency and non-disclosure rules explicit in the SRS and Data Dictionary. No new Payment status, recovery workflow or live-provider authorization. |
| **V10 FINAL — 2026-10-13 public Guest requirements revision** | Team-approved shared implementation baseline; approval confirmed by project owner | REV-2026-10-13-GUEST defines common public catalog, requested-period availability, sizing/rental-process guidance and public help/policies; permits optional anonymous AI recommendations; defines authentication continuation. Visual design and page/component implementation are excluded. Five business roles, seven flows, 27 entities and existing private-resource/financial lifecycles remain unchanged. Approval does not imply implementation acceptance. |
| **V10 FINAL — 2026-10-20 Customer profile revision** | Owner-approved profile simplification | `REV-2026-10-20-CUSTOMER-NO-ADDRESS` prohibits Customer physical address, updates AUTH-001/004/005 and defers backend/schema migration. |
| **V11.0 REVIEW CANDIDATE — 2026-10-20 owner decision update** | **PARTIAL BUSINESS APPROVAL / NOT RELEASE ACCEPTANCE** | Approves optional `Customer.Address` input/persistence; Manager-owned persisted handover assignment and its four-state lifecycle; read-only SYSTEM_ADMINISTRATOR StorageUnit timeline. Records admin deactivation-eligibility as HTTP 501 scaffold and public UnitType `imageUrl`/`videoUrl` as implemented. Other provisional behaviors, PostgreSQL architecture sign-off and testing remain gated. |
| **V11.0 REVIEW CANDIDATE — 2026-10-21 payment-documentation cleanup** | Editorial only — no business/API/code change | Removes superseded provider-specific historical text and configuration from this SRS. payOS remains the sole active gateway; historical payment preservation and existing release gates remain unchanged. |
| **V11.0 — 2026-10-22 Customer Transaction History decision** | **TRANSACTION HISTORY REQUIREMENTS APPROVED / IMPLEMENTATION AND TESTS PENDING** | `REV-2026-10-22-CUSTOMER-TRANSACTION-HISTORY-READ` approves ACTIVE Customer own Payment history under OD-07: actual Deposit/subsequent Rental Fee attempts, all three Payment statuses, pagination and View Invoice; no offline-first-month Payment, fabricated deduction/refund/wallet balance, or approval of other pending V11 decisions. |
| **V11.0 — 2026-10-25 Customer CCCD and continuation decision** | **TEAM-APPROVED REQUIREMENTS / IMPLEMENTATION AND TESTS SEPARATE** | OD-09 keeps CCCD optional at registration and nullable in Customer, requires it for new RES-001, and preserves the pending operation through authentication/profile completion. No LegalName, verification status, new table or endpoint; missing CCCD uses existing HTTP 400 VALIDATION_ERROR with errors.cccd. |
| **V11.0 — 2026-10-26 CCCD format decision** | **OWNER-APPROVED REQUIREMENTS / BE IMPLEMENTATION PENDING** | OD-10 requires exactly 12 ASCII digits for supplied CCCD and new RES-001; preserves optional registration, nullable varchar(20), leading zeroes, legacy rows and existing continuation. |

## V5 Review Summary

V5 performs a consistency review rather than adding a new business flow.

### Logic corrections applied

| Review ID | Issue found in V4 | V5 treatment |
|---|---|---|
| `REV-L01` | `LoginHistory.UserAccountId` is non-null, but V4 said every failed login attempt is recorded even when no account can be resolved. | LoginHistory is written only when a UserAccount is resolvable; unknown identifiers are recorded only in technical/security logging unless the schema is changed later. |
| `REV-L02` | Return finalization could silently ignore `DamageRecord(PENDING)` even though settlement uses approved Damage only. | Finalization is blocked while any DamageRecord remains `PENDING`; stable code `DAMAGE_DECISION_PENDING` is added. |
| `REV-L03` | V4 implied Facility creation defaults to `ACTIVE` without source support. | Initial Facility status becomes an explicit Deferred Decision; no default is invented. |
| `REV-L04` | V4 left first-month pre-handover Payment disconnected from Contract Discount timing/snapshot. | Marked as a Release 1 blocking Deferred Decision; PAY-002/OPS-004 cannot silently choose a discount interpretation. |
| `REV-L05` | Contract Policy capture at handover was not explicitly resolved. | Marked as a Release 1 blocking Deferred Decision. |
| `REV-L06` | Updating a Discount used by an active Contract could change future invoice behavior despite “fixed Contract Discount” wording. | Mutation of percentage/effective fields on referenced Discounts is prohibited until the rule is explicitly approved. |
| `REV-L07` | Damage recording requires DamageType, but no production DamageType seed/management source exists. | Marked as a Release 1 blocking master-data decision. |
| `REV-L08` | StorageUnit state matrix does not include direct `AVAILABLE -> MAINTENANCE`, while Manager is described as managing operational status. | Kept source-faithful and exposed as a Deferred Decision; V9 does not invent the transition. |
| `REV-L09` | SupportTicket Category values were only “suggested” in the Data Dictionary but examples looked normative. | Category catalogue is explicitly Deferred; examples are examples only. |
| `REV-L10` | V4 still contained stale version-specific wording such as `DESIGN-LOCKED — V3`, “V3 uses…”, and “V4 does not…”. | Normative wording is rewritten to be version-consistent with V5. |

### Wording rules applied

- Use **FRMS Release 1** for the product release and **SRS V1–V5 / Data Dictionary V1 / Scope V8** only for document versions.
- Use `MUST` for required Release 1 behavior.
- Use `MAY` only for genuinely optional implementation behavior.
- Replace ambiguous “recommended” wording with either a mandatory rule, an explicitly equivalent alternative, or a Deferred Decision.
- Distinguish source-derived rule, SRS technical decision, and unresolved source gap.

> V11 REVIEW CANDIDATE (not yet adopted) retains V10 FINAL; after approval, including the team-approved REV-2026-10-13-GUEST is the current shared implementation baseline. Its normative Guest requirements and contracts are governing requirements; approval does not claim that Guest APIs or functions are implemented or tested. payOS is the sole active online payment gateway. Offline first-month and operational-read semantics remain unchanged.

---

# 0. Document Authority and Source of Truth

## 0.0 Approved Change Record — REV-2026-10-07-FM-OFFLINE

**Approval:** The project owner confirms team approval of this model on 2026-10-07. This approved change is incorporated into the governing SRS, not an implementation-only ADR.

**Approved model:**

- Customer pays the full first rental month offline at the Facility.
- The authorized Staff's OPS-004 command acknowledges that full first-month rent has been received and handover is complete.
- Successful Contract creation is FRMS's business record of first-month settlement. No first-month Invoice, Payment, receipt entity, paid flag or new Contract attribute is created.
- First-month amount = Reservation.LockedRentalPrice, without Contract Discount.
- Deposit retains the existing Invoice/Payment flow through payOS Payment. Online Rental Fee invoices start strictly after Contract.StartMonth.
- First-month recognized revenue is derived once per Contract from Reservation.LockedRentalPrice and attributed to Contract.StartMonth; this is not a physical cash-receipt timestamp.
- PAY-002 is retired. OPS-004 retains VisitId, StorageUnitId and nullable DiscountId but removes FirstMonthPaymentId.

**Affected requirements:** Flow 2; BR-HO-02/03; BR-DIS-05; BR-BIL-06; CALC-FIRST-01; CALC-REP-02; DD-13; OPS-004; CWP-08/FWP-03/SSP-06/08; monthly billing, reporting, payment-result scope, DBT-24, E2E-F02/F04 and release gates.

**Authority and scope:** This revision overrides conflicting first-month rules in Data Dictionary V2.1, earlier proposals, DTOs, Swagger, SQL objects and code. Only this SRS is updated in the present task; alignment of those artifacts is a follow-up implementation task. Do not claim the new API/DB behavior is already implemented or synchronized. Architecture, five roles, 27 entities, Deposit flow, subsequent online payments, captured Policy and other approved flows remain unchanged.

**Historical-data protection:** Do not delete, rewrite or fabricate links for existing first-month Invoice/Payment or unlinked legacy payment rows. Preserve historical evidence; new handovers must not create those records. Require an approved migration/deployment review before changing constraints or replacing old SQL/API behavior. Revenue must not count a historical first-month Invoice/Payment in addition to the Contract-derived amount.

## 0.0.1 Retired Payment-Gateway Material — Superseded

Earlier provider-specific contracts, test URLs, credentials, callback conventions, and routes are intentionally excluded from the V11 normative SRS. The active payment provider and its API/security requirements are governed exclusively by §0.0.3 (payOS) and the companion API specification. This document cleanup does not authorize rewriting or deleting historical `Payment`, `Invoice`, or audit evidence, and does not change offline first-month handling.

## 0.0.2 Approved Change Record — REV-2026-10-07-OP-READ

**Approval:** The project owner confirms team approval of operational read and consistency corrections on 2026-10-07, including the explicit follow-up choices for return release, usageRate and reuse of existing read endpoints.

**Approved scope:**

- FACILITY_STAFF may read Reservation, Contract and Visit detail only within the actor's assigned Facility through RES-003, CON-002 and VIS-004. Customer ownership rules remain unchanged.
- FACILITY_MANAGER may list/read Reservation, Contract and Visit through RES-002/003, CON-001/002 and VIS-003/004 only in the assigned Facility; BUSINESS_OPERATIONS_MANAGER may use these reads system-wide. Staff gains no Reservation/Contract/Visit list permission through this revision.
- BOM-010 allows Customer own-Discount read and same-Facility Staff/Manager Customer-Discount read for Customers with a Reservation/Contract in that Facility. BOM-013 supplies active ExtraFeeTypes to Staff/Manager; existing BOM management access remains. Catalogue write roles do not change.
- BIL-001/002, PAY-003 and CON-004/005 allow Customer own reads, same-Facility Staff/Manager operational reads and BOM system-wide reads. No payment initiation, manual PAID mutation or other command permission is granted to employees.
- OPS-001 Visit work items identify the Visit, its referenced Reservation/Contract and the minimal Customer name/phone needed for daily reception.
- Staff obtains first-month rent from Reservation.LockedRentalPrice and the related RESERVATION Visit from ReservationDetail/VIS-004. ContractDetail already contains ReservationId; no duplicate field or derived financial/Visit field is added to the Contract entity or schema.
- After successful handover, the existing OPS-004 contractId permits CON-002 navigation. An authorized Contract read can follow reservationId to RES-003 for immutable rental-price and handover-Visit data.
- DamageType provisioning is resolved by DD-15; obsolete unresolved wording in section 9.13 is corrected without changing the fixed seed catalogue.
- Complete Inspection leaves StorageUnit in INSPECTION. Only atomic Finalize Return releases it to AVAILABLE/MAINTENANCE together with settlement and terminal Contract state. Move the non-persisted storageUnitStatus command parameter from INS-007 to INS-008; do not add an intermediate entity/status/field.
- usageRate = scoped IN_USE StorageUnits / all scoped StorageUnits, including INSPECTION/MAINTENANCE in the denominator; an empty denominator returns 0. This report metric does not change rentable-capacity eligibility.

**Affected requirements:** actor scopes, Flow 2/6, procedure catalogue, DD-15, authorization, endpoint registry and detail/catalogue/return/report contracts, pseudocode, FE requirements, tests, traceability, acceptance and lifecycle summaries; FWP-01..07, MWP-01/03/04, BWP-06, SSP-12/14/15/17.

**Implementation boundary:** This task updates the SRS only. Backend role gates, authoritative Facility/ownership checks, DTOs/Swagger, return stored procedures and frontend types/UI must be aligned and verified by their owners before the affected feature is declared complete. No new entity, persisted attribute, role, state, first-month Invoice/Payment or write-role permission is introduced. The existing offline-first-month revision remains in force. The approved Finalize Return release rule supersedes conflicting Complete Inspection release wording in the lower-level Data Dictionary; its SQL deployment alignment requires the existing migration/redeploy review process, not a new database field.

**Later supersession (2026-10-25, OD-08):** The historical 2026-10-07 restriction on Staff Contract **list** access is superseded for `CON-001` only. Staff still cannot list Reservations (`RES-002`) or Visits (`VIS-003`).

## 0.0.3 Approved Change Record — REV-2026-10-09-PAYOS

**Approval:** Owner approved payOS as the active gateway and clarified webhook acknowledgement, UTC+7 provider time and description on 2026-10-09.
payOS is the sole active EPS-01 gateway; pre-existing historical payment records and migration evidence remain preserved and are not rewritten.
payOS supports personal/household-business accounts; no separate sandbox/staging exists. Official production API: https://api-merchant.payos.vn.
The original 2026-10-09 scope was code, migrations and mock/fixture automated tests only. The limited owner-approved live E2E exception is recorded in section 0.0.5; it does not grant general production-payment authority.
Limited live payOS financial-path evidence is recorded in section 0.0.5; EPS-01 release acceptance additionally requires the implementation gates, and does not imply acceptance of unrelated FRMS workflows.
First month remains offline: no first-month Invoice/Payment, PAY-002 remains retired. Deposit/later Rental Fee eligibility, authorization, idempotency, three Payment statuses, audit and authoritative SQL effects are unchanged.

**Historical approved payment invariant (2026-10-09):** ProviderOrderCode BIGINT NULL; the former SQL Server design used `dbo.ProviderOrderCodeSequence` starting at 1000, increment 1, NO CYCLE. Allocate atomically for new PAYOS attempts; enforce uniqueness; preserve null order codes in pre-existing historical payment rows. Do not generate codes from timestamps or UUID conversions, and do not roll back verified PAYOS/payment evidence.

**V11 PostgreSQL provider mapping (observed, pending technical-baseline approval):** Implement the same order-code allocation/uniqueness/idempotency invariants using the deployed PostgreSQL sequence and unique index. The SQL Server sequence name above is a historical design record, **not** a current executable PostgreSQL object. Do not assume an existing live merchant can safely restart at 1000; verify sequence high-water mark and migration/test evidence.
PaymentUrl stores checkoutUrl (2048 bounded characters); PaymentUrlExpiresAt UTC stores provider expiry when supplied or explicit request expiry; TransactionCode stores successful webhook reference, not paymentLinkId/orderCode.
Missing config does not fail startup; PAY-001 raises EXTERNAL_PROVIDER_NOT_CONFIGURED before creation. Secrets are local only; .env ignored and excluded from build/publish.
PAY-001 is POST /api/v1/invoices/{invoiceId}/payments/payos; PAY-003 unchanged; PAY-004 is anonymous POST /api/v1/payments/payos/webhook. No superseded gateway route is part of the active V11 API contract.
Browser return/cancel never update financial state; refresh PAY-003/BIL-002.
Signed unknown/sample webhook -> HTTP200 without mutation; reserve sample code 123 by starting sequence at1000.
Provider transactionDateTime is interpreted as UTC+7 by owner decision then persisted UTC; description is FRMS, trace identity uses ProviderOrderCode.

**Only active configuration:** Payment:PayOS
```text
Payment__PayOS__ClientId=<configured locally>
Payment__PayOS__ApiKey=<secret/configured locally>
Payment__PayOS__ChecksumKey=<secret/configured locally>
Payment__PayOS__ApiBaseUrl=https://api-merchant.payos.vn
Payment__PayOS__ReturnUrl=http://localhost:5173/customer/payments/result
Payment__PayOS__CancelUrl=http://localhost:5173/customer/payments/result
Payment__PayOS__WebhookUrl=https://lying-ladder-showroom.ngrok-free.dev/api/v1/payments/payos/webhook
Payment__PayOS__ExpiryMinutes=15
```

[x] Approved live payOS link creation
[x] Approved real payment / verified webhook / UTC Invoice.PaidAt evidence
[ ] EPS-01 full release acceptance (live later Rental Fee scenario not yet demonstrated)

No editable task-tracker PDF source has been supplied; do not recreate PDF. This record supersedes historical provider requirements, not unrelated business semantics.

## 0.0.4 Approved Clarification — REV-2026-10-10-PAY-IDEMPOTENCY

**Approval:** On 2026-10-10 the owner requests explicit IdempotencyKey requirements in both the SRS and Data Dictionary, and Payment-related Data Dictionary alignment with the already-approved payOS and Invoice.PaidAt revisions. This incorporates the previously approved PAY-001 idempotency contract; it does not introduce a new payment obligation or gateway workflow.

`Payment.IdempotencyKey` MUST be a frontend-generated UUID, globally unique across all Payment rows and retained for the lifetime of its Payment row. In historical SQL Server it maps to `uniqueidentifier NOT NULL`; in the current PostgreSQL/Npgsql runtime it maps to `uuid NOT NULL`. Wire format stays a UUID string. This mapping correction does not change the approved PAY-001 retry/idempotency semantics. The authoritative attempt-creation/retry contract is section 12.7.1; PAY-001 and the Data Dictionary MUST follow that contract.

This clarification does not authorize fabricated keys or Invoice links for historical data, repair of stranded attempts, refund/reconciliation, additional statuses, code/schema changes, deployment or live payOS calls. Historical-data review remains in force; the separate limited live approval is recorded in section 0.0.5.

## 0.0.5 Approved Execution Boundary — REV-2026-10-10-PAYOS-LIVE-E2E

**Approval:** On 2026-10-10 the owner explicitly approved registering/confirming the configured payOS webhook, creating one real payment link from an eligible test Invoice, and personally paying that Invoice. The owner approved a minimal development fixture with rental price/deposit of 2,000 VND and requested a shareable seed file. This is an execution approval, not successful live-test evidence.

Payment must still use the persisted Invoice.AmountDue through PAY-001; do not hardcode an adapter/service amount, bypass authentication, manually mark Payment/Invoice paid or fabricate a webhook. Only a verified provider webhook may write the financial result. The agent must not initiate a bank transfer, payout or refund.

The owner permits checking the existing FRMS baseline and applying forward EPS-01 migrations without dropping tables/database or deleting historical data. If the baseline differs, stop and obtain approval for a specific adoption or new-database approach; do not fabricate migration history. Disposable Frms_Test_* integration databases remain separate from the live fixture. EPS-01 acceptance remains pending until actual build/test and verified live payment/Invoice.PaidAt evidence are recorded.

**Limited live evidence (2026-10-10):** The owner approved a new attempt after the earlier link expired and personally paid 2,000 VND. SQL inspection of FRMS_EPS01_Dev confirmed payOS ProviderOrderCode 1008 / Payment 46661ece-7df4-4179-ad82-f4c0daf3f18b as SUCCESS, linked Invoice e5010000-0000-0000-0000-000000000007 as PAID, both PaidAt values 2026-10-10T10:15:00Z (17:15:00 GMT+7), and exactly one PAYMENT_RESULT audit. The old attempts remain preserved. This is limited live financial-path evidence, not proof that unrelated FRMS workflows or all release gates are complete. No provider secret, signed payload, PaymentUrl or bank account is recorded here.

**Implementation verification (2026-10-10, integrated with origin/main d7cc1a5):** Backend Release build passed with zero errors/warnings; frontend production build passed with only a bundle-size warning. Unit tests: 142 passed; API tests: 64 passed; architecture tests: 13 passed; real SQL Server PaymentSql/PaymentMigration/PaymentServiceSql/PayOsSql tests: 65 passed. All four test runs had zero failures and zero skips. The SQL suites include empty-database migration, payment constraints, authorization, idempotency and concurrent result processing. Limited live evidence covers the Deposit payment above; a separate live later Rental Fee payment and the seven unrelated FRMS browser journeys are not claimed as verified.

## 0.0.6 Approved Change Record — REV-2026-10-13-GUEST

**Approval:** On 2026-10-13 the project owner confirms that the team has approved REV-2026-10-13-GUEST and requests its adoption into the shared governing SRS. The revision is approved and effective as an implementation baseline. It contains common project requirements only; the owner's separate visual design and page/component specification remain outside the SRS.

**Change history:** The requirements draft was published for team review in commit `9eaec86`. This approval record supersedes its pending-review status while preserving the reviewed functional/API scope. Requirement approval is separate from implementation completion and test/release acceptance.

**Approved scope:**

- Guest/Anonymous is an unauthenticated access mode, not a sixth business role or persisted account.
- Anonymous and Customer share Facility/UnitType browsing, current public prices, server-authoritative capacity for a requested month range, Size Guide, How It Works, Help Center and current public policies. Optional AI recommendation accepts user input without using personal rental/account history and remains non-blocking/non-authoritative.
- A Facility matches a complete search only when its location matches and at least one offered UnitType satisfies all supplied mode/type/size filters and has positive capacity in every month from StartMonth through EndMonth inclusive. Only matching UnitTypes are shown in that search result; filtering precedes pagination/counting.
- CAT-001..004 gain a public projection; CAT-005 supplies the shared UnitType list for Size Guide. PUB-POL-001/002 supply read-only public policy content/current-policy projection without a new persisted content entity or CMS workflow.
- Anonymous cannot create/read Reservation, Contract, Visit, Invoice, Payment, Discount, SupportTicket or return/settlement business data, or read an account's profile/personal notification/history. Existing AUTH-001 self-registration remains permitted without login. PAY-004 remains a separately validated provider webhook, not Guest access.
- Ordinary Customer login enters the authenticated Customer area. Authentication prompted by a protected action resumes its validated pending operation and non-sensitive selection. Login alone does not require identity verification, create/confirm Reservation, schedule Visit or initiate Payment.
- Customer retains all public capabilities available to Anonymous; public content/data contracts remain independent of account identity. Page structure, component reuse, navigation placement and visual style are outside this requirements revision.

**Affected requirements:** sections 2/3, Flow 1, GWP-01..05, CWP-01/02, BR-PUB-01..07, sections 10/13/14/15, catalog/policy schemas and endpoint index, public API/security tests, E2E-F01, traceability, Release 1 gates and acceptance.

**Implementation boundary:** This approval update changes only this SRS. Backend authorization/query contracts, Swagger, frontend API models, approved public article copy and automated functional/security tests require follow-up alignment with this approved baseline. Existing code must not be represented as already supporting the revised behavior. Visual style, layout, header/footer/sidebar, component design, specific public-page URLs and UI prototypes belong to the owner's separate frontend design work and are not defined by this revision. Public policy content is a deployment-owned read model, not a new Policy entity, approval workflow or financial rule. Operational Facility scope and private ownership checks remain enforced; public projection never grants operational reads or commands. No new role, business flow, persisted entity/attribute/status, stored procedure, personal-data publication, reservation hold at browsing time or payment obligation is introduced. Email OTP, password reset/change and Google login remain outside current Release 1.

## 0.0.7 Historical Approved Change Record (partly superseded by §0.0.9) — REV-2026-10-20-CUSTOMER-NO-ADDRESS

**Historical approval (verified on main):** On 2026-10-20 the owner originally confirmed that FRMS would not collect Customer physical addresses. **SUPERSEDED IN PART by §0.0.9:** optional `Customer.Address` entry and persistence are now approved for potential future use. The historical prohibition below is preserved for decision traceability, not as the current V11 target rule.

- Customer physical `address` is excluded from the target Customer profile, AUTH-001 registration, AUTH-005 update and authenticated own-profile GET (AUTH-004). `Facility.Address` is unchanged.
- AUTH-001 retains `fullName`, `phoneNumber`, `email`, `password` and optional unique `cccd`; AUTH-005 allows limited `fullName`/`cccd` changes only. Customer PhoneNumber login and account Email remain immutable.
- AUTH-004 Customer `data.profile.cccd: string | null` is private to the logged-in Customer. The FE-first implementation must treat missing/malformed fields as unavailable and disable CCCD editing until verified BE support exists.
- This does not add identity verification or a new role. Existing Customer.Address persistence cannot be dropped or overwritten without an independently reviewed migration and deployment/backfill/data-retention plan.

**Source:** `docs/FRMS_SRS_V10.md` §0.0.7 at anchored GitHub HEAD. Code on `main` accepts and stores the optional Address; this now accords with the §0.0.9 owner reversal, subject to data-security and regression checks. The missing AUTH-004 CCCD response was an implementation gap at that historical review; source at `2945788` now contains the owner-only nullable projection (OD-09), without certifying deployed/runtime behavior.

## 0.0.8 Draft Change Register — REV-2026-10-20-V11-CODE-ALIGNMENT

**Status:** REVIEW CANDIDATE. The explicit owner decisions in §0.0.9 are **APPROVED V11 TARGET CHANGES** (not automatically deployed/release-accepted); other new behavior remains proposed until separately authorized. This full document supersedes V10 as a released governing baseline **only after final V11 ratification**. Where the old V10 address prohibition conflicts with the later owner decision, §0.0.9 controls the V11 target.

**Base and reproducibility:** This review candidate carries forward the locally supplied `FRMS_SRS_V10.2.md` body, incorporates the owner-approved 2026-10-20 Customer profile revision in the `main` SRS, and records implementation evidence from GitHub `main` commit `90a6c5982d42` (2026-10-20). **It is not a certified byte-for-byte rebase of all `docs/FRMS_SRS_V10.md` content**; a complete main-to-V11 diff is a merge prerequisite. The companion API spec has the same source boundary and review status.

### V11 change register / impact and acceptance

| ID | Code observation / issue | V11 documentation change | Status / gate |
|---|---|---|---|
| V11-01 | Runtime `UseNpgsql` / independent PostgreSQL migrations | Align runtime persistence, SQL dialect, tooling and background execution references with PostgreSQL | IMPLEMENTED IN CODE; owner architecture approval / migration test outstanding |
| V11-02 | `Pg001..Pg013`, PostgreSQL functions/triggers | Add migration inventory and PostgreSQL transaction/concurrency verification | DOCUMENTED; deploy chain/rollback verification outstanding |
| V11-03 | `Pg013` `HandoverUnitAssignment` + `ACTIVE/REPLACED/CONSUMED/RELEASED` | Add persisted Manager assignment/reassignment/release/consumption lifecycle without modifying Reservation hold | **OWNER APPROVED — §0.0.9; implementation/SQL E2E not verified** |
| V11-04 | `Pg010` StorageUnit timeline via `AuditLog` | Read-model, partial history, Manager own-Facility / BOM system / Administrator system-wide read-only permission | **ADMIN READ APPROVED — §0.0.9; security tests pending** |
| V11-05 | `Pg012` optional UnitType `ImageUrl/VideoUrl` | Record actual CAT-003/004/005 public response `imageUrl`/`videoUrl` exposure; nullable length 2048 | **OBSERVED + OWNER ACKNOWLEDGED; media governance/Swagger tests pending** |
| V11-06 | Customer Activity Controller Notifications/Transactions | Read-only private history endpoints without a new table | **Notifications IMPL-CUSACT-01 APPROVED — OD-06; Transaction History IMPL-CUSACT-02 REQUIREMENTS APPROVED — OD-07. Implementation/integration/security acceptance remains separate.** |
| V11-07 | Admin/Manager/Staff/BOM supplemental lookup and directory GETs | Provisional API appendix and Facility/ownership scope test matrix | CODE EXISTS; role/privacy review pending |
| V11-08 | V10 profile prohibition subsequently revised by owner | Preserve optional Customer.Address and private AUTH-004 CCCD; OD-09 adds conditional CCCD requirement for new RES-001, not a NOT NULL column | **APPROVED — OD-01/09; AUTH-004 projection observed in source at 2945788; new reservation gate/continuation and runtime acceptance pending** |
| V11-09 | Complete API §13 embedded in V10 | Extract into `FRMS_API_V11.md` preserving endpoint IDs/contracts | DOCUMENT STRUCTURE DRAFT; Swagger parity not tested |
| V11-10 | Existing historical payOS and first-month offline | Preserve payment semantics and historical evidence, no gateway switch | INVARIANT, regression acceptance pending |
| V11-11 | Dev-only override for out-of-date Visit check-in | Document non-production opt-in and boundary | CODE EXISTS; verify Production cannot enable |
| V11-12 | README/TECH_BASELINE/AGENTS refer to older SQL Server paths/stack | Create follow-up alignment tasks rather than silently changing unrelated docs | DOCUMENT DRIFT; not edited here |
| V11-13 | Preliminary V11 draft mixed historical SQL Server names/types with current PostgreSQL mappings | Clarify provider mapping for payment order-code sequence and `Payment.IdempotencyKey`; preserve historic approval/evidence | **EDITORIAL / TRACEABILITY — no financial semantics changed** |
| V11-14 | Original draft repeated the entire endpoint registry in SRS Appendix D | Keep the normative endpoint inventory only in companion API Spec §13.11 and candidate routes in §13A | **DOCUMENT SPLIT — no route approval** |
| V11-15 | Original draft had illustrative assignment JSON and stale private notification field label | Correct provisional JSON models from pinned DTO source; add exact schema-source references and acceptance flags | **CODE-OBSERVED; runtime/OpenAPI verification outstanding** |
| V11-16 | V11 candidate's release checklist used 27-table wording | Maintain V10 historical 27-core provenance; approve `HandoverUnitAssignment` as 28th Release-1 persisted business table and test mapping | **OWNER APPROVED — §0.0.9; V11-G02 tests and V11-G09 final sign-off pending** |
| V11-17 | Data Dictionary V2.1 retained MoMo, first-month online Payment, SQL Server and 27-table physical assumptions | Split V11 Data Dictionary, align PostgreSQL schema, payment, optional Address, media and approved 28th entity | **DOCUMENT SYNC; owner V11 full ratification and runtime DB tests PENDING** |
| V11-18 | `main` `0219f307` contains the newly added recurring jobs, default-on billing, DI and registration-only NUnit test | Record actual hosted services, five new tasks, configuration/ordering, startup intervals and acceptance gaps across SRS/API/Data Dictionary | **CODE REGISTERED; PRODUCTION EXECUTION/INTEGRATION NOT VERIFIED** |

### Implementation source-of-truth distinction

`Observed in code` does NOT mean `approved business behavior`. V11 distinguishes (1) **owner-approved decision** (specifically §0.0.9), (2) **current implementation observation**, (3) **proposal requiring separate approval**, and (4) **tested/accepted release evidence**. V11-03, Administrator timeline access in V11-04, sent-notification history in OD-06 and own Payment history in OD-07 are approved; supplemental directories in V11-07 remain provisional. Media exposure in V11-05 is **verified + acknowledged**, not a blanket approval of future uploads, arbitrary URLs or content management. No sixth role, eighth business flow, first-month Invoice/Payment, new payment method or second gateway is introduced.

### Scope/release gate

The **initial V11 reconciliation package** was SRS + API + Data Dictionary documentation drafting only, not a production migration, backend refactor or release certification. The separately owner-approved OD-06 task authorizes the narrow Customer Notifications/Overview frontend read and its SRS/API synchronization. The subsequent OD-07 task approves Transaction History requirements and their SRS/API synchronization only; frontend implementation remains a separate task. Neither decision authorizes other provisional features, backend/database changes, provider activation or GitHub publication without a separate request.

## 0.0.9 Approved Owner Decisions — REV-2026-10-20-V11-OWNER-DECISIONS

**Decision authority:** Explicit project-owner instructions in the current review session, 2026-10-20. These are *approved requirements for the V11 target*, not evidence that the whole V11 document, code, migration deployment, Swagger, tests or Release 1 DoD have passed. They supersede only conflicting earlier requirements identified here.

### OD-01 — Optional Customer physical address retained

- `Customer.Address` remains an **optional, nullable** customer profile field, maximum 1,000 characters under the current backend request validator and persistence model. `AUTH-001` registration **MAY** accept `address`; `AUTH-005` own-profile PATCH **MAY** supply `address` or explicitly `null` to clear it; omission leaves the saved value unchanged. `fullName`, `phoneNumber`, `email`, `password` and optional unique `cccd` retain the established AUTH-001 semantics.
- Collection is **optional**, not a condition for account creation, reservation, visit, payment or any current workflow. Its potential future business uses are *not authorized or defined* by retaining the field. Do not add geolocation, ID verification, public listing or cross-role exposure by inference.
- Customer login `PhoneNumber` and account `Email` remain non-editable through AUTH-005. `Facility.Address` is unrelated and remains mandatory. No Address data may be deleted automatically as part of this decision.
- Current frontend Account Settings need **not** add an Address editor; any future UI that does must follow the optional-field API, validation and privacy rules. In particular, retaining the field does **not** independently approve its inclusion in AUTH-004 own-profile responses or other directory/public projections.
- **Overrides:** The 2026-10-20 `REV-2026-10-20-CUSTOMER-NO-ADDRESS` requirement prohibiting collection/update in AUTH-001/AUTH-005 and the target entity is **superseded**. The earlier decision for `AUTH-004` Customer own-profile `cccd: string | null`, private to the owner, **continues to apply**. That projection is present in source at `2945788`; runtime/Swagger acceptance remains separate. OD-09 adds a new-Reservation precondition without changing optional Address or adding identity verification.

### OD-02 — Manager handover assignment approved

- Approve `HandoverUnitAssignment` as the **28th Release-1 persisted business entity**, in addition to the existing V10 27-entity catalogue. Its persisted status values are exactly `ACTIVE`, `REPLACED`, `RELEASED`, `CONSUMED`; the four-state lifecycle, FK/index/immutability rules and approval boundary appear in §9.2A and API Spec §13A.1.
- Manager in the Reservation's Facility **MAY** assign or replace an AVAILABLE StorageUnit of the matching Facility/UnitType **only after** the linked RESERVATION Visit is CHECKED_IN and before Contract creation. Only one active assignment per Reservation and per Unit; duplicate same-unit PUT returns the existing assignment. Manager may release it; Staff may read the active assignment but **may not** change it.
- At Complete Handover, the Staff's atomic Contract operation **MUST** verify the assigned unit equals the Contract unit and consume the active assignment in the same transaction; failure rolls back Contract/assignment state. Preassignment is not Reservation capacity allocation or early StorageUnit occupancy. The Reservation remains a Facility+UnitType+month-range capacity hold.
- Approved endpoints: `PUT/GET/DELETE /api/v1/reservations/{reservationId}/handover-unit-assignment` with RBAC per API Spec §13A.1. Implementation presence is confirmed; PostgreSQL concurrency, race, rollback, negative authorization and E2E acceptance remain unverified.

### OD-03 — Administrator StorageUnit timeline read approved

- `SYSTEM_ADMINISTRATOR` **MAY** call the three **read-only** timeline GETs in API Spec §13A.2 for Facilities across the system. `FACILITY_MANAGER` remains limited to the Manager's own Facility; `BUSINESS_OPERATIONS_MANAGER` retains system-wide read access.
- The timeline is derived from `AuditLog` evidence beginning with Pg010; it must not fabricate history before capture or expose tenant/contract/payment/private Customer details. This exception does **not** grant Administrator StorageUnit mutation, Reservation/Contract/Invoice business reads, BOM commands, or general operational data access. Controller and service authorization plus 401/403/negative-scope tests remain mandatory.

### OD-04 — Admin deactivation-eligibility endpoint is NOT IMPLEMENTED

- `GET /api/v1/admin/customers/{customerId}/deactivation-eligibility` exists as an Admin-scoped **HTTP 501** scaffold (`ENDPOINT_NOT_IMPLEMENTED`, `UX-RBAC-11`). It is **not** an accepted working endpoint, not an implicit prerequisite to `ADM-004`, and must not be presented to the frontend as available. Actual Customer deactivation guard remains in the existing `ADM-004` service. Future implementation requires a separate feature task and passing tests.

### OD-05 — Public UnitType media URLs acknowledged as code-observed

- Public `CAT-003`, `CAT-004` and `CAT-005` response mappings/controllers already contain nullable `imageUrl` and `videoUrl` fields corresponding to Pg012 `UnitType.ImageUrl` / `UnitType.VideoUrl` (`varchar(2048)` nullable). **Record this as implementation fact**, not a speculative future DTO.
- This acknowledgement does not authorize uploading media, external fetch of untrusted URLs, new media CMS, private asset access or unreviewed link content. URL validation, null serialization, cache/public disclosure and generated Swagger/FE parity remain verification gates.

### OD-06 — Customer in-app notifications — replacement REVIEW DRAFT

**Review record:** REV-2026-10-29-INAPP-NOTIFICATIONS, documentation only. The owner narrowed this task to Notifications; authentication/account onboarding and OD-07 Transaction History are unchanged. The old approved revision REV-2026-10-22-CUSTOMER-NOTIFICATIONS-READ described SENT-only email history; source still implements it. The replacement below needs approval before any code or DB change.

- `BR-NOT-APP-01`: an eligible business event and its Customer notice commit/rollback together. After commit the notice is readable independently of SMTP/email delivery state. No-op/failed/rolled-back operations produce no successful-event notification; a GET/Refresh never creates one.
- `BR-NOT-APP-02`: retain IMPL-CUSACT-01 GET /api/v1/customers/me/notifications for ACTIVE CUSTOMER, own UserAccount only, with scope before count/page. Pagination 1/20, max 100; order createdAt DESC, notificationId DESC. Overview reads the first three from the same API. Proposed DTO: notificationId, notificationType, content, createdAt; sentAt is no longer required for display or eligibility.
- `BR-NOT-APP-03`: retain the existing Reservation/Invoice/Visit/Contract/SupportTicket/finalized-settlement event catalogue in the review record. Actual VisitDate change produces VISIT_RESCHEDULED, same-date save does not. Payment/refund wording must not invent financial outcomes. No near-date reminder or new business transition is added.
- `BR-NOT-APP-04`: expose approved safe in-app events only, with authoritative recipient/provenance. Do not publish all PENDING/internal data, credentials, verification proof or CCCD; do not fabricate SENT/SentAt. NotificationLog reuse is an option; exact physical routing/schema/constraints and old-row eligibility require NOT-D01/02 approval. No new table/column/status is selected here.
- `BR-NOT-APP-05`: these business alerts are in-app only. The legacy sender must not email them, while email requirements of other account workflows remain unchanged. Reading Notifications is not SMTP delivery, account verification, state mutation or a payment command.

Preserve plain text, generic rendering for safe unknown types, UTC/GMT+7, loading/empty/error/forbidden/session-switch states and read retry. No unread badge/count, mark-as-read, resource/action URL inferred from content, realtime/polling or historical backfill is added. Migrate FE types/validator/copy and BE/OpenAPI together after approval; current required sentAt/SENT filters are implementation gaps against this proposed replacement, not permission to edit source now.

**Traceability:** IMPL-CUSACT-01 + Overview -> OD-06 / BR-NOT-APP-01..05 -> CustomerActivityService.NotificationsAsync -> CustomerActivityRepository + eligible event recording -> approved NotificationLog reuse/routing -> API-NOTAPP-001, SEC-NOTAPP-001, DBT-NOTAPP-001..002, E2E-NOTAPP-001 -> V11-G16. These are planned acceptance obligations, not implemented/PASS evidence. [Review, decisions and checklist](FRMS_InApp_Notifications_Review.md).

### OD-07 — Customer Transaction History requirements approved

**Change record:** `REV-2026-10-22-CUSTOMER-TRANSACTION-HISTORY-READ`. On 2026-10-22 the project owner expressly approved the six Transaction History requirements below. `IMPL-CUSACT-02` is **APPROVED**, not provisional. This is a read-only requirement approval and SRS/API update; it does not claim completed frontend implementation or passing backend/database/security/E2E tests. No entity, persisted state, financial formula, payment obligation, business flow or write permission is added.

- **Owner-only access:** approve `GET /api/v1/customers/me/transactions` for an authenticated **ACTIVE CUSTOMER**. Derive the Customer from the authenticated account and enforce Payment -> Invoice -> owned Reservation/Contract scope on the server before pagination. No caller-supplied Customer/account ID and no Guest or Employee access to this Customer endpoint.
- **Actual Payment attempts and statuses:** return existing Payment records for owned Deposit invoices and post-first-month Rental Fee invoices. Include `PENDING`, `SUCCESS` and `FAILED` with distinct, explicit labels. `PENDING` means awaiting a result, not money received. A row represents one Payment attempt, so multiple attempts for one Invoice remain separate; FE must not treat their sum as money collected or infer an Invoice state from this list. An Invoice without a Payment does not create a history row.
- **Displayed evidence:** show payment purpose/type, amount, method, status, creation time and nullable paid time from the API, with UTC instants displayed in GMT+7. Each row offers **View Invoice**, using its returned `invoiceId` and the existing protected Invoice Detail route/API; normal ownership checks still apply. IDs are internal keys/links, not primary Customer-facing labels. FE must not fabricate a paid timestamp or replace historical payment evidence with current provider settings.
- **Pagination and read-only presentation:** reuse API §13A.3 collection/error conventions, default `page=1&pageSize=20`, maximum `pageSize=100`, ordered `createdAt DESC`, then `paymentId DESC`. Transaction History is the second tab of the shared Notifications & History area. Opening, paging, refreshing and following View Invoice must not initiate payment/retry/refund commands or change business state. FE displays server amounts/statuses without calculating financial totals or transitions; loading, empty, validation, authorization, unexpected-error and safe read-retry states must remain distinct.
- **Offline first month excluded:** first-month rent collected offline at handover has no first-month Invoice/Payment under the existing approved rule. Do not fabricate a Transaction History row from Contract creation or offline-receipt acknowledgment.
- **No invented deductions, refunds or wallet:** do not turn Damage, ExtraFee, DepositSettlement amounts or Figma sample content into payment-history rows, refund-transfer confirmations or wallet/deposit balances without the corresponding approved transaction evidence and API contract. Those display extensions are outside OD-07; the existing Fees & Return Tracking read remains separate.
- **Scope and acceptance:** this decision leaves supplemental directories/RBAC, billing backfill, scheduling, technical-architecture decisions and all other pending matters unchanged for later team discussion. Required verification includes exact DTO/envelope parity; cross-Customer/Guest/Employee/inactive-account denial; pagination/order and empty pages; all three statuses and multiple attempts per Invoice; nullable paid time/GMT+7 display; correct View Invoice targets; no fabricated offline-first-month/refund/balance records; safe loading/errors, account switching and no write side effects. Test results must be recorded separately from this approved requirement status.
- **Traceability:** Customer Transaction History tab -> OD-07 -> `IMPL-CUSACT-02` (API §13A.3) -> `CustomerActivityService.PaymentsAsync` -> `CustomerActivityRepository.ListPaymentsAsync` -> existing `Payment`/`Invoice` with `Reservation`/`Contract` ownership -> the contract, authorization and UI checks above. View Invoice reuses `BIL-002`; no new persisted model or payment endpoint is required.

**Unchanged decisions:** payOS only for eligible Deposit and post-first-month Rental Fee invoices, offline first-month acknowledgment, policy/price snapshot rules, five roles, seven core flows, ownership guards, and the separate limited live-payment evidence scope. **Pending:** owner architecture ratification of PostgreSQL, supplemental directory proposals, billing backfill/scheduling decisions, AUTH-004 runtime/Swagger parity, OD-09 implementation and release-gate test evidence. Transaction History requirements are approved; implementation/testing is tracked separately.

### OD-08 — Facility Staff read-only Contract list approved (2026-10-25)

**Change record:** `REV-2026-10-25-STAFF-CONTRACT-LIST`. The owner authorizes authenticated ACTIVE `FACILITY_STAFF` to call the existing `CON-001 GET /api/v1/contracts?page=1&pageSize=20` for a **read-only, server-filtered list of Contracts in that Staff member's assigned Facility**. The current `CON-002` detail access is unchanged. This decision expressly supersedes only the historical Staff/CON-001 denial in §0.0.2; Staff remains excluded from `RES-002` and `VIS-003` lists.

- Resolve the current Employee/Facility assignment from the authenticated account; never accept a caller-supplied FacilityId or CustomerId to widen scope. Apply `Contract.FacilityId == assignedFacilityId` to both count and page query **before** pagination. Staff may view Contracts belonging to different Customers only within that Facility. Unassigned Staff -> 403; missing/inactive authentication -> 401/403 per existing authentication rules. No cross-Facility list data may be returned.
- Reuse the existing `CON-001` route, paginated `ContractListResponse` and `ContractSummary` projection; `page` starts at 1, `pageSize` is 1..100 (default 20). Existing `CUSTOMER` owner, `FACILITY_MANAGER` Facility and `BUSINESS_OPERATIONS_MANAGER` system-wide list scopes remain unchanged. No new Contract/Customer/Visit field, SQL table, stored procedure, command permission or status is introduced.
- Backend implementation only in this scope. Frontend Staff Contract List navigation/presentation will be completed by the FE owner independently. Required acceptance: Staff own-Facility pages and counts; no other-Facility rows; missing Facility 403; Customer/Manager/BOM behavior unchanged; Administrator rejected; GET has no writes; API authorization/OpenAPI parity; build, unit and integration tests recorded separately.

### OD-09 — CCCD before new Reservation and interrupted-operation continuation (2026-10-25)

**Change record:** `REV-2026-10-25-CUSTOMER-CCCD-CONTINUATION`. The project owner confirms team approval of the CCCD-only proposal and the in-flow/return-navigation additions. The owner also approves the existing **HTTP 400 `VALIDATION_ERROR` with `errors.cccd`** for missing CCCD. These requirements are effective for this V11 target; this documentation task does not implement or certify them.

- **Conditional data, not account activation:** retain the existing Customer name field and nullable/unique `Customer.CCCD` (physical capacity 20 characters; accepted values now governed by OD-10). Do not add `LegalName`, a separate real-name field, a verification status, a table or an identity-approval workflow. AUTH-001 CCCD is optional; registration, login and public browsing do not require it. A supplied CCCD is not proof of verified identity; OD-10 now expressly requires the 12-digit format; no eKYC or identity-verification rule is implied.
- **New Reservation precondition:** an authenticated ACTIVE Customer must have a saved CCCD matching the OD-10 format and existing uniqueness constraints before RES-001 creates any Reservation, capacity hold or Deposit Invoice (`BR-RES-09`). Resolve the profile from the authenticated account, not client CustomerId or a claimed FE completion flag. Missing or invalid saved CCCD returns the approved validation envelope. Account/role/ownership checks remain mandatory. Apply the new check only to new Reservation creation after implementation deployment; do not cancel/rewrite existing Reservation/Contract/payment history or add this prerequisite to unrelated commands.
- **In-flow completion:** when the Customer chooses to reserve and CCCD is missing, offer a focused entry step in the current reservation flow, preferably a popup; if reservation review is already a dialog, switch its content rather than stack dialogs. Explain "Bổ sung số CCCD để tiếp tục đặt kho." Save through AUTH-005, refresh own authoritative profile through AUTH-004, then restore the reservation review; do not make the Customer manually find the abandoned page. "Lưu và tiếp tục" saves the profile only; the Customer separately confirms RES-001. "Để sau" closes completion without creating a Reservation. Account Settings remains an ordinary own-profile editor and a valid fallback only with a safe return destination when opened as a prerequisite.
- **One Customer flow after authentication:** Guest public browsing remains available. On requesting a protected reservation operation, preserve the valid non-sensitive selection, authenticate/register first, then use the same ACTIVE Customer profile check and reservation flow. No duplicate Guest reservation workflow or unconditional post-login identity gate is introduced.
- **General continuation principle:** system-requested intermediate steps preserve the originating operation, safe internal destination/next valid step and approved non-sensitive context; success returns there rather than defaulting to Overview. This applies to the defined authentication and CCCD-completion steps, not arbitrary navigation or automatic replay of business commands. Ordinary Account Settings/menu visits do not create a forced return. Section 14.15 defines cancellation, revalidation, account-switch and privacy requirements.
- **Authority and implementation boundary:** reuse AUTH-001/002/004/005 and RES-001; no new route or database field is required. Customer.CCCD remains nullable; do not fabricate/backfill IDs or copy names into identity data. Do not expose CCCD to Staff/Manager/BOM/Admin or public projections by inference. Optional Address and immutable login Email/PhoneNumber remain unchanged. The canonical API error and continuation contract is in API §13.14 RES-001 / §13.12 AUTH-004/005; persistence implications are in Data Dictionary §§4.3/12.4/22.
- **Evidence boundary:** source at `2945788` includes own-Customer CCCD in `CurrentCustomerProfileResponse` / `AuthenticationMapping` and corresponding test sources. This removes the old static DTO gap, not the need for runtime tests. The new RES-001 precondition, CCCD completion UI and extended continuation are implementation work; they are not certified by this SRS update. Acceptance/traceability is in §14.15.3 and V11-G14.

### OD-10 — CCCD is exactly 12 ASCII digits (2026-10-26)

**Change record:** `REV-2026-10-26-CCCD-12-DIGITS`. **OWNER-APPROVED REQUIREMENT CHANGE:** the repository owner authorized this source-of-truth and frontend update in the current task. This supersedes OD-09's earlier maximum-20-only / no-12-digit-format wording; its continuation, privacy, nullable-registration and no-auto-submit rules remain effective. This approval does not certify backend/database implementation or final V11 release acceptance.

- **BR-ACC-04 — Format:** this field accepts a Vietnamese CCCD/citizen personal identification number as a string of exactly 12 ASCII digits (`[0-9]{12}`, whole-string match). Normalize surrounding whitespace on newly supplied input, then validate; preserve leading zeroes. Do not coerce to a number, remove internal characters, pad, truncate or infer geographic/birth/identity-verification rules. Format validity does not establish identity authenticity.
- **AUTH-001/005:** registration remains optional. An omitted CCCD or blank-after-trim registration input is stored as null. Profile PATCH omission preserves the saved value; explicit null/blank clears it. Every supplied nonblank replacement must satisfy BR-ACC-04 and existing uniqueness. A PATCH omitting CCCD may still update other approved fields without rewriting or rejecting an unchanged legacy CCCD.
- **AUTH-004 / legacy compatibility:** continue returning the owner's stored string or explicit null. Existing noncompliant strings remain readable only under the existing own-profile privacy scope, with no automatic repair, deletion, padding, truncation or backfill. FE allows their correction; absent/non-string/malformed response shapes remain unavailable rather than a confirmed null. A legacy string that does not itself match BR-ACC-04 cannot satisfy a new Reservation prerequisite, even when it is nonblank. Existing Reservations/Contracts/Payments remain unaffected.
- **RES-001 / BR-RES-09:** before any Reservation, capacity hold or Deposit Invoice write, BE must resolve the authenticated ACTIVE Customer and require the saved CCCD to match BR-ACC-04 at the existing atomic creation boundary, including concurrent profile clear/update handling. Missing or invalid saved CCCD returns HTTP 400 `VALIDATION_ERROR` with `errors.cccd` and traceId, without booking writes. Keep the four-field RES-001 request unchanged; no client CCCD or completion flag is authoritative.
- **Errors / FE:** AUTH-001/005 format/duplicate rejection also uses existing HTTP 400 `VALIDATION_ERROR` + `errors.cccd`; no new error code. Apply the same format rule in registration, ordinary Account Settings and the in-flow completion step. Keep CCCD out of URLs, continuation/browser persistence, logs and other-role projections. Save/read/review never auto-submits a Reservation or Payment.
- **Persistence:** retain nullable PostgreSQL `varchar(20)` and the existing unique index; 20 is physical storage capacity, not permission to accept a new nonblank value of another format. No table/column/type migration, database CHECK deployment or stored-routine edit is performed by this task. BE must implement service and authoritative creation validation; any later routine/constraint deployment requires its own reviewed migration and legacy-data handling.

**Backend handoff — implementation pending:** align RegisterCustomerRequest/CustomerRegistrationService; CustomerProfilePatchParser/CustomerProfileValidator/CustomerService; stable field-error mapping; ReservationService/repository/authoritative PostgreSQL creation routine; generated OpenAPI. Use the existing layers, preserve nullable/omit/clear and unique semantics, validate only a supplied CCCD on profile PATCH, and do not log values. Backend code, migrations and live data are deliberately unchanged in this FE/documentation task.

**Acceptance / traceability:** AUTH-001/005 + RES-001 -> BR-ACC-04 / BR-RES-09 -> existing Business/repository/PostgreSQL boundaries -> `API-CCCD-004`, `DBT-CCCD-003`, `REG-CONT-002` -> V11-G15. Test null/omitted/blank handling, valid leading-zero 12-digit strings, 11/13 digits, letters, internal whitespace, punctuation and non-ASCII digits; duplicate attempts; legacy correction and unrelated PATCH omission; concurrent clear/update; no writes on rejection; unchanged old rentals; own-profile privacy and continuation without auto-submit. Backend/PostgreSQL/Swagger acceptance remains pending until separately executed.

## 0.0.10 Three-Document V11 Synchronization — 2026-10-20

Three V11 Markdown specifications are maintained as a single **review candidate**: this SRS for approved business/domain requirements; [`FRMS_API_V11.md`](FRMS_API_V11.md) for the full HTTP contract and observed extensions; [`FRMS_Data_Dictionary_V11.md`](FRMS_Data_Dictionary_V11.md) for the 28-entity target dictionary, PostgreSQL types/routines/migrations and DB test matrix. The historical V2.1 Data Dictionary and V10 API §13 are preserved as source provenance, not independently governing V11 changes that contradict later owner approvals.

Document synchronization alone does not approve PostgreSQL architecture, provisional directory APIs, unrelated feature development, production migrations or live bank operations. Customer activity reads remain limited to OD-06/07; OD-09 separately approves conditional CCCD and continuation, not wider personal-data reads. AUTH-004 owner-only CCCD is now observed in source at `2945788`; OD-09's new reservation gate/continuation still require implementation and acceptance. Optional Customer.Address remains approved. No build, NUnit, PostgreSQL E2E, compiled Swagger, Postman or Playwright results are claimed by this documentation update.

## 0.0.11 Approved Change Record — REV-2026-10-29-PUBLIC-POLICY-CONSOLIDATION

**Owner approval:** On 2026-10-29 the owner approved reducing the public Help Center catalogue from six overlapping articles to four focused articles. This changes GWP-04 presentation content and the PUB-POL-001/002 topic allowlist, not the underlying Policy entity, captured PolicyId, financial rules, lifecycle, access rights or database schema.

The four topics, in display order, are `reservation-cancellation` (Reservation & Account Requirements, including CCCD and own-account data access), `deposit-payment-refund` (Deposit, Invoices & Refunds), `storage-access-handover-contract` (Storage Rental & Return), and `facility-rules-prohibited-items` (Facility Rules & Prohibited Items). The former `identity-verification` and `account-privacy` articles are consolidated into the first topic; they are no longer separate PUB-POL keys. Existing frontend links to those former Help articles may redirect to the consolidated topic. This does not introduce an identity-verification process or a complete legal privacy notice.

The Facility Rules item catalogue still has `DRAFT_REQUIRES_LEGAL_AND_BUSINESS_APPROVAL` status and no effective date. The owner's approval of this four-topic structure and local review copy does not mark that source APPROVED or certify production publication. Team review of public copy and applicable GWP-04 acceptance tests remain separate. Article version wording must distinguish the current persisted Policy parameters from the independently edited explanatory copy; configurable numerical thresholds may not be invented or hard-coded into article text.

## 0.1 Authority Order

For implementation, review, testing, and conflict resolution:

1. **Approved governing SRS (currently V10 FINAL; V11 after owner approval)**
2. **Companion `FRMS_API_V11.md` for API wire contracts only (after approval)**
3. **Companion `FRMS_Data_Dictionary_V11.md` for entity/DB rules, within explicit approved SRS limits (after approval)**
4. **FRMS Scope V8**
5. Implementation detail (current code is evidence, not authorization)

Conflict resolution:

```text
IF an approved SRS explicitly defines or overrides a business rule
    FOLLOW the approved SRS (V10 until V11 ratification)

ELSE IF a published API-only wire contract applies without business conflict
    FOLLOW the approved companion API specification

ELSE IF the approved Data Dictionary V11 defines or overrides a database rule
    FOLLOW Data Dictionary V11

ELSE
    FOLLOW Scope V8
```

No implementation component may silently create another interpretation.

## 0.2 Source Roles

### Approved SRS V10 FINAL / V11 target upon owner approval

Highest implementation authority for:

- consolidated business requirements;
- approved architecture;
- API contract;
- coding conventions;
- authentication/authorization;
- frontend/backend integration;
- development phases;
- testing;
- quality gates;
- Definition of Done;
- explicit overrides and reviewed clarifications.

### Data Dictionary V11 (V2.1 historical baseline)

Primary lower-level authority for:

- confirmed ERD decisions;
- persisted entities;
- relationships;
- nullability;
- statuses;
- constraints/indexes;
- stored procedures;
- triggers;
- scheduled jobs;
- transaction/concurrency requirements;
- idempotency;
- database integration tests.

### Scope V8

Primary lower-level authority for:

- business goals;
- five actors;
- seven business flows;
- actor responsibilities;
- scope boundaries;
- business intent;
- Future Work not superseded later.

## 0.3 Requirement Classification

The following four terms classify **approved or source-derived requirements**. Two additional *non-normative* tags (`IMPLEMENTATION-OBSERVED`, `PROPOSED`) identify mismatches and requests for approval; these tags never grant new permissions or entity/status authority.

| Label | Meaning |
|---|---|
| `SOURCE-LOCKED` | Directly supported by Data Dictionary V2.1 / Scope V8 and not superseded. |
| `OVERRIDE-LOCKED` | Explicit later override of older source wording. |
| `DESIGN-LOCKED` | Technical/design decision explicitly locked by this SRS. |
| `DEFERRED` | Current sources do not define enough to safely implement the behavior; implementation MUST NOT guess. |
| `IMPLEMENTATION-OBSERVED` | Factual code/schema/route observation as of pinned commit; **not** an approved requirement. |
| `PROPOSED` | Candidate requirement/role/entity under review; **not** approved until change control succeeds. |

## 0.4 No Silent Invention Rule

Implementation MUST NOT silently invent:

- a new business flow;
- a new entity/table;
- a new stateful workflow;
- a new actor permission;
- a new financial formula;
- a new lifecycle status;
- a new approval process;
- a new payment obligation;
- a resolution to any Section 23 Deferred Decision.

Technical detail MAY be refined behind interfaces/configuration boundaries only when it does not change approved business semantics.

## 0.5 Final Version Change Control

`V10-APPROVED INHERITED BASELINE — V11 RATIFICATION PENDING`

The approved V10 business requirements inherited into this V11 candidate remain the governing baseline until V11 is formally ratified. It preserves the approved V9 business semantics except for the explicit owner-approved revisions recorded in section 0.0 and its subsections.

After approval:

- code contradicting this SRS is a defect unless accompanied by an approved SRS revision;
- Data Dictionary or Scope wording does not override an explicit rule in this SRS;
- a Deferred Decision MUST remain unimplemented, guarded, or configurable as specified until separately approved;
- no team member may resolve a Deferred Decision only in code, database migration, Swagger, UI, or test fixture;
- any semantic change requires a new SRS revision and traceability update.

An unapproved future change does not replace this implementation baseline until its approval/adoption is recorded. REV-2026-10-13-GUEST has been approved under section 0.0.6 and is part of this baseline; requirement approval never substitutes for implementation or test evidence.

# 1. Normative Keywords


- **MUST / SHALL** — mandatory Release 1 behavior.
- **MUST NOT / SHALL NOT** — prohibited behavior.
- **SHOULD** — permitted only for non-semantic engineering guidance where an equivalent implementation may be justified.
- **MAY** — genuinely optional behavior.
- **SOURCE-LOCKED / OVERRIDE-LOCKED / DESIGN-LOCKED / DEFERRED** — classifications defined in Section 0.3.

When a statement is both normative and classified, the normative keyword controls implementation obligation while the classification explains authority/origin.

# 2. Purpose and Product Scope


## 2.1 Purpose

FRMS is a web-based Self-Storage Facility Rental and Management System.

This SRS provides one implementation-oriented baseline for:

- business behavior;
- architecture;
- API contracts;
- data boundaries;
- coding conventions;
- authentication and authorization;
- testing;
- development phases;
- acceptance.

## 2.2 System Goals

`SOURCE-LOCKED`

The system targets seven end-to-end flows:

1. Storage Unit Reservation.
2. Storage Check-in and Handover.
3. Rented Storage Unit Management.
4. Business Rules, Fee Management, and Revenue Monitoring.
5. Facility Storage and Staff Management.
6. Storage Renewal and Overdue Handling.
7. Support Request and Issue Handling.

The design remains month-based: Reservation holds capacity by `Facility + UnitType + month range`. Under owner-approved V11 §0.0.9, the Manager selects a concrete `StorageUnit` **after RESERVATION Visit CHECKED_IN** in a persisted pre-handover assignment; the unit becomes contractually allocated/occupied **only at atomic Complete Handover**.

## 2.3 In-Scope

Current FRMS Release 1 business scope includes:

- customer self-registration and account use;
- shared Anonymous/Customer public Facility and Unit Type browsing and filtered storage search;
- available-capacity display for a requested month range;
- public Size Guide, How It Works, Help Center/current public policies and Guest-to-Customer login continuation;
- Reservation creation using future `StartMonth` / `EndMonth`;
- Deposit invoice/payment through payOS Payment;
- Reservation Visit scheduling in the captured Policy window;
- Staff check-in;
- Manager physical StorageUnit selection;
- offline first-month collection acknowledged by Staff at Complete Handover, without a first-month Invoice/Payment;
- atomic Complete Handover;
- Contract lifecycle and multiple active rentals per Customer where business rules allow;
- ACCESS Visits;
- monthly Rental Fee invoice generation strictly after Contract.StartMonth;
- payment-result processing;
- overdue detection and LateFee;
- Contract renewal using append-only `ContractExtension`;
- RETURN Visit and `ActualReturnDate`;
- Inspection claim and completion;
- DamageRecord, InspectionEvidence, ExtraFee;
- DepositSettlement calculation;
- Facility and StorageUnit operation;
- UnitType current pricing;
- versioned Policy;
- Customer-owned Discount;
- Facility/system reporting;
- SupportTicket processing;
- Customer account status and Employee administration;
- RBAC, facility scope, login history and audit;
- Notification queue/retry support;
- optional AI Size Guide / Unit Type recommendation.

payOS payment integration is limited to Deposit and Rental Fee invoices from the second rental month onward. The first month is collected offline and represented by Contract creation, not an Invoice/Payment.

## 2.4 Out-of-Scope

The current core scope does **not** implement:

- Item Inventory.
- Staff Schedule.
- Shift Management.
- Day-off Management.
- Staff availability/conflict engine.
- separate Task Management entity.
- separate Appointment entity/workflow.
- Physical Unit allocation at Reservation time.
- `ASSIGNED` / `RESERVED` StorageUnit status.
- complex StorageUnit priority algorithm.
- live production payment validation until separately approved; only payOS code/mock integration is authorized.
- actual refund transfer.
- actual debt/compensation collection.
- legal dispute management.
- legal enforcement.
- detailed external property recovery workflow.
- Fee Waiver or Fee Waiver approval.
- complex Discount approval workflow.
- Rental Price Range / MinPrice / MaxPrice.
- separate Price History entity.
- separate stateful Renewal entity/workflow.
- separate Renewal Fee entity.
- complex generic rule engine.
- detailed physical repair workflow.
- detailed stored-item handling/inventory.
- actual collection of Late/Extra/Damage/AdditionalAmountDue through a dedicated gateway.

## 2.5 Core Demo Scope

`SOURCE-LOCKED`

Core Demo must prove all seven approved business flows end-to-end. Supporting capability may be simplified where it does not alter the core lifecycle.

## 2.6 Release 1 Scope Boundary

`DESIGN-LOCKED`

Release 1 is the completed, hardened, tested implementation of the approved FRMS Release 1 business scope.

Future Work does **not** automatically become Release 1 scope.

The optional AI Size Guide is not a Release 1 blocker unless explicitly promoted later.

# 3. Actors and Access Scope


The system uses exactly five business roles:

```text
CUSTOMER
FACILITY_STAFF
FACILITY_MANAGER
BUSINESS_OPERATIONS_MANAGER
SYSTEM_ADMINISTRATOR
```

## 3.1 CUSTOMER

Customer may:

- use all public Guest capabilities in section 3.8 under the same public data/access contracts;
- browse active Facilities;
- view Unit Types, size/mode, current price and requested-period capacity;
- create Reservation;
- pay Deposit via payOS, pay the first rental month offline before handover, and pay subsequent Rental Fee invoices via payOS;
- schedule/reschedule valid RESERVATION Visit;
- receive a StorageUnit through the handover flow;
- manage multiple Contracts;
- create ACCESS Visit for an ACTIVE Contract;
- renew Contract;
- view invoices/payment/overdue/return progress;
- create and track Contract-related Support Tickets;
- create RETURN Visit.

Authentication identifier:

```text
PhoneNumber
```

Customer self-registration assigns role `CUSTOMER`.

## 3.2 FACILITY_STAFF

Facility Staff belongs to exactly one Facility and may:

- process Reservation check-in at that Facility;
- read Reservation/Contract detail and related Visit detail within that Facility for authorized operational handling;
- verify system payment state without manually marking Invoice `PAID`;
- acknowledge full offline first-month receipt by invoking Complete Handover; this receipt creates no Invoice/Payment;
- complete handover according to authorized workflow;
- process ACCESS and RETURN Visits;
- confirm actual physical return;
- claim and perform Inspections;
- record Damage, ExtraFee and evidence;
- process assigned Support Tickets;
- view a daily work list derived from Visits, Inspections and Support Tickets.

Authentication identifier:

```text
Email
```

## 3.3 FACILITY_MANAGER

Facility Manager belongs to exactly one Facility and may:

- manage physical `StorageUnit` records for the Facility;
- assign existing UnitType to StorageUnit;
- maintain unit location/operational status;
- view Unit Type and current price without changing global master pricing;
- select an appropriate StorageUnit during handover;
- monitor Reservations, Visits, Contracts, payments, overdue, returns and Inspections;
- approve or reject PENDING DamageRecord items for Inspections in the assigned Facility;
- assign Facility Staff to Support Tickets;
- view Facility-level operational reports.

Authentication identifier:

```text
Email
```

## 3.4 BUSINESS_OPERATIONS_MANAGER

Business Operations Manager has system-wide business-operation scope and may:

- create/view/update/activate/deactivate Facilities;
- manage UnitType current RentalPrice;
- create new Policy versions;
- manage Customer Discounts;
- manage ExtraFeeType configuration;
- monitor multi-facility performance;
- view/export business reports.

Authentication identifier:

```text
Email
```

## 3.5 SYSTEM_ADMINISTRATOR

System Administrator manages access/system administration and may:

- view user accounts and basic profiles;
- activate/deactivate Customer accounts subject to lifecycle restrictions;
- create/update Employee accounts;
- activate/deactivate Employee accounts;
- assign Employee roles;
- assign Facility to Facility Staff/Manager;
- view LoginHistory;
- view AuditLog/activity.

Administrator does not gain business-data mutation authority merely by being Administrator.

Authentication identifier:

```text
Email
```

## 3.6 SYSTEM

Internal system responsibilities include:

- capacity protection;
- reservation expiration;
- no-show processing;
- monthly invoice generation;
- overdue marking;
- LateFee calculation;
- payment result application;
- notification retry;
- audit/logging support;
- transaction and idempotency enforcement.

## 3.7 External Providers

- **payOS Payment** — Deposit and second-month-onward Rental Fee Invoice payment processing.
- **Runtime AI service** — optional Size Guide recommendation.
- **Future Google Identity Provider** — Employee external login only; Future Work.

## 3.8 Anonymous Visitor / Guest — Public Access Mode

`TEAM-APPROVED — REV-2026-10-13-GUEST`

Anonymous means no authenticated FRMS account context. It is not a UserRole, UserAccount, Customer entity or an additional business actor/flow.

Anonymous may:

- browse/search ACTIVE Facilities by location and offered UnitTypes;
- read public Facility information, UnitType mode/size/description/current RentalPrice and requested-period availableCapacity;
- read Private/Public Size Guide and choose a UnitType for subsequent search;
- read How It Works, Help Center/current public policy articles and public contact information;
- request optional AI Size Guide recommendations from input supplied for that request;
- keep a non-sensitive Facility + UnitType + month-range selection and enter existing Customer registration/login before a protected action.

For the same query and authoritative data snapshot, public catalog/policy content is the same for Anonymous and Customer; it MUST NOT depend on CustomerId, owned Contracts, personal Discount or account history. Capacity/price/policy can change over time and filters can produce different results. AI suggestions depend on submitted description/mode, not account identity, and are not guaranteed to be byte-identical across provider calls.

Anonymous MUST NOT access personal Reservation/Contract/Visit lists or detail, physical-unit assignment/access credentials, Invoice/Payment/settlement, owned Discount, SupportTicket, account/profile, personal notifications/history or administrative/operational data. Public contact information does not authorize creation of a SupportTicket without the existing authenticated Contract relationship.

# 4. Business Flow Overview


## 4.1 Flow 1 — Storage Unit Reservation

```text
Browse/search public active Facility / UnitType (Anonymous or Customer)
-> choose StartMonth + EndMonth
-> calculate capacity for every requested month
-> authenticate as Customer if the user is Anonymous, then resume the selection
-> read own profile; if CCCD is missing or does not match OD-10, save it through the in-flow completion step
-> resume the same Customer reservation review and re-read current data
-> validate the current selection and required account/Reservation data
-> Customer explicitly submits the Reservation
-> create Reservation(PENDING_DEPOSIT)
-> create Deposit Invoice
-> payOS payment
-> confirm Reservation
-> create RESERVATION Visit(SCHEDULED)
```

Rules:

- Browsing, recommendation and the sign-in gate are read/selection steps only; they do not hold capacity or create Reservation/Invoice/Visit/Payment.
- Ordinary Customer login enters the authenticated Customer area; only an interrupted protected action carries a pending operation. Required identity/account fields are handled at the relevant business step, never as an unconditional post-login workflow.
- Under OD-09 / BR-RES-09, new RES-001 requires saved CCCD matching OD-10 on the authenticated Customer's profile. Missing or invalid saved CCCD is HTTP 400 VALIDATION_ERROR with errors.cccd and no Reservation/hold/Invoice writes. Profile completion returns to reservation review, not automatic submission; previously created rentals remain unchanged.
- `StartMonth > CurrentMonth`.
- `EndMonth >= StartMonth`.
- Reservation holds capacity, not a concrete StorageUnit.
- Deposit is a snapshot of UnitType current RentalPrice.
- Deposit does not use Discount.
- capacity check + Reservation creation is atomic.
- Deposit timeout comes from captured Policy.
- RESERVATION Visit date must fall inside captured Policy day window.

## 4.2 Flow 2 — Storage Check-in and Handover

```text
Reservation CONFIRMED
-> RESERVATION Visit SCHEDULED
-> Staff CHECKED_IN
-> Manager selects AVAILABLE matching StorageUnit
-> Staff receives full first-month rent offline
-> Staff Complete Handover (OPS-004 acknowledges offline receipt)
-> Contract ACTIVE
-> StorageUnit IN_USE
-> Reservation COMPLETED
-> Visit CHECKED_OUT
```

Complete Handover is an atomic multi-table transaction without a first-month Invoice/Payment. The authorized Staff command acknowledges offline receipt; FRMS does not verify cash through payOS. Contract existence records initial settlement, including after its status becomes COMPLETED or TERMINATED.

## 4.3 Flow 3 — Rented Storage Unit Management

```text
Contract ACTIVE
-> Customer creates ACCESS Visit
-> Staff CHECKED_IN
-> physical access/handling
-> Staff CHECKED_OUT
-> Contract remains ACTIVE
```

Rules:

- Contract must be `ACTIVE`.
- pending RETURN Visit blocks new ACCESS Visit.
- visit handling does not change Contract ownership or rental period.

## 4.4 Flow 4 — Business Rules, Fee Management, and Revenue Monitoring

Includes:

- current UnitType pricing;
- versioned Policy rows;
- Customer-owned Discount;
- ExtraFeeType configuration;
- monthly Rental Fee invoice generation strictly after Contract.StartMonth;
- overdue and LateFee calculation;
- reporting and revenue monitoring.

Historical Reservation/Contract/Invoice snapshots must not be rewritten by later master-data changes.

## 4.5 Flow 5 — Facility Storage and Staff Management

Includes:

- StorageUnit creation/update/operational status;
- facility-scoped Staff/Manager access;
- actual actor tracking;
- derived daily work list from Visits, Inspections and Support Tickets;
- Facility monitoring/reporting.

No separate Task table is required.

## 4.6 Flow 6 — Storage Renewal and Overdue Handling

Renewal:

```text
ACTIVE Contract
-> no pending RETURN Visit
-> contiguous extension
-> future capacity check for every extension month
-> snapshot current UnitType price
-> append ContractExtension
-> update Contract.EndMonth
```

Return:

```text
ACTIVE Contract
-> RETURN Visit
-> ActualReturnDate
-> StorageUnit INSPECTION
-> Inspection PENDING
-> claim Inspection / record Damage and ExtraFee
-> complete Inspection (Unit remains INSPECTION)
-> resolve any pending Damage decisions before Finalize Return
-> Staff Finalize Return selects AVAILABLE or MAINTENANCE
-> atomic DepositSettlement + Contract COMPLETED or TERMINATED + Unit AVAILABLE or MAINTENANCE
```

`ActualReturnDate` determines Normal Return vs Early Return.

Inspection completion alone never releases the Unit. Applicable LateFee and recorded approved Damage/ExtraFee contribute to Finalize Return; a pending Damage decision blocks that transaction. Manager may decide recorded Damage before or after Inspection completion, but every pending decision must be resolved before Finalize Return; this does not add an Inspection-completion approval gate.

## 4.7 Flow 7 — Support Request and Issue Handling

```text
Customer creates SupportTicket(OPEN)
-> Facility Manager assigns Facility Staff
-> IN_PROGRESS
-> Staff records result
-> COMPLETED
```

`OPEN` or `IN_PROGRESS` may become `CANCELLED` according to the approved lifecycle.

SupportTicket is Contract-related and does not directly mutate Contract/Payment/StorageUnit lifecycle.

# 5. Technology Stack

| Area | Tool / Technology | Use |
|---|---|---|
| Requirement & Design | Draw.io | Use Case, Activity, Sequence, Class, ERD, architecture diagrams |
| UI/UX | Figma | Customer, Staff, Manager, BOM, Admin prototypes |
| Backend Coding | Visual Studio | ASP.NET Core Web API development |
| Frontend Coding | Visual Studio Code | React frontend development |
| Frontend Framework | React | Web interface |
| Frontend Language | TypeScript | Components, frontend logic, typed API models |
| Backend Framework | ASP.NET Core Web API | REST API and backend |
| Backend Language | C# | Backend services, data access, tests |
| Database | PostgreSQL | Main relational database |
| Database Tool | psql / pgAdmin / approved PostgreSQL deployment tooling | Schema, queries, procedures, administration |
| ORM | Entity Framework Core | Persistence and mapping |
| API Testing | Postman | REST API testing |
| Unit Testing | NUnit | Backend unit and service tests |
| UI / E2E Testing | Playwright | Browser-based end-to-end testing |
| AI-assisted Development | ChatGPT / Claude / Gemini | Development support |

## 5.1 Frontend Clarification

```text
React + TypeScript
```

C# / Razor / Blazor are not part of the frontend baseline.

## 5.2 Runtime AI Clarification

Current optional product AI capability:

```text
AI Size Guide / Unit Type Recommendation
Priority: Optional
Release blocker: No
```

Runtime provider is intentionally provider-agnostic and selected by configuration behind `IAiRecommendationProvider`.

---

# 6. Development Phases and Ownership


## 6.1 Phase 0 — Structure & Technical Foundation

**Primary Implementer / Owner:** Nguyễn Trần Trường Giang
**Architecture / Cross-module Review:** Bùi Đình Long

Phase 0 establishes:

- backend solution structure;
- frontend project structure;
- three logical layer boundaries plus the Infrastructure adapter assembly;
- Controller / DTO baseline;
- API DTO validation and DTO-to-Business mapping baseline;
- Business Command / Result model baseline;
- Service / interface baseline;
- Repository / EF Core / DbContext baseline;
- external-provider abstraction and adapter baseline;
- dependency-injection composition root;
- background-job host baseline;
- migration baseline and approved Phase 0 seed: five UserRoles, Policy v1 and fixed DamageTypes;
- Customer phone login;
- Employee email login;
- JWT + BCrypt;
- Global Exception Middleware;
- `ILogger<T>`;
- UTC persistence / GMT+7 business-display;
- Swagger/OpenAPI;
- common API envelopes;
- NUnit / Postman / Playwright skeletons;
- architecture-boundary test skeleton;
- Development / Testing / Production configuration.

### 6.1.1 Phase 0 Exit Gate

```text
[ ] Backend solution builds
[ ] Frontend solution builds
[ ] API DTOs do not cross into Business service contracts
[ ] Business Commands/Results do not expose EF entities
[ ] Controller -> Service -> Repository boundary tests pass
[ ] External-provider implementations are resolved through Business interfaces
[ ] Background workers call Business services and do not access Repository/DbContext directly
[ ] Empty database migrates successfully
[ ] Phase 0 seed loads successfully: five UserRoles, Policy v1 and six fixed DamageTypes
[ ] Customer phone login baseline works
[ ] Employee email login baseline works using seeded/test Employee account(s); this does not depend on the deferred production Employee credential-provisioning workflow
[ ] JWT authorization baseline works
[ ] BCrypt baseline works
[ ] Global Exception Middleware works
[ ] ILogger<T> logging works
[ ] Swagger/OpenAPI is available
[ ] API JSON convention is fixed
[ ] UTC/GMT+7 convention is implemented
[ ] NUnit project runs
[ ] Postman environment runs
[ ] Playwright project runs
```

ExtraFeeType entity/table and its CHECK constraints remain part of the Phase 0 database structure. Phase 0 MUST NOT seed its five categories or invent DefaultAmount/currency. Those rows are deferred beyond Phase 0 until deployment values are approved; they are not part of this exit gate.

## 6.2 Phase 1 — Core Demo

Core Demo proves the seven flows end-to-end.

Mandatory Core Demo concerns:

- correct lifecycle;
- transaction correctness;
- role/facility/resource authorization;
- payOS Payment Deposit and second-month-onward Rental Fee flow; offline first-month handover confirmation;
- critical concurrency protection;
- reproducible migration/seed;
- seven E2E journeys.

### 6.2.1 Core Demo Exit Gate

```text
[ ] Flow 1 Reservation works end-to-end
[ ] Flow 2 Check-in/Handover works end-to-end
[ ] Flow 3 ACCESS lifecycle works
[ ] Flow 4 Pricing/Policy/Billing/Overdue baseline works
[ ] Flow 5 Facility/StorageUnit operation works
[ ] Flow 6 Renewal/Return/Settlement works
[ ] Flow 7 Support lifecycle works
[ ] All five roles can perform required demo actions
[x] approved live payOS Deposit works (limited evidence in section 0.0.5)
[ ] Offline first-month receipt is acknowledged by OPS-004 without Invoice/Payment
[ ] Second-month-onward Rental Fee payment works
[ ] Reservation overbooking race is protected
[ ] Handover same-unit race is protected
[ ] Renewal capacity race is protected
[ ] Inspection double-claim is protected
[ ] Duplicate payOS webhook is idempotent
[ ] Duplicate return finalization is protected
[ ] Wrong-facility access is rejected
[ ] Wrong-customer ownership access is rejected
[ ] Critical database integration tests pass
[ ] Seven Playwright E2E journeys pass
```

## 6.3 Phase 2 — Full Release 1.0

Release 1 hardens approved FRMS Release 1 business scope.

Includes:

- complete negative/error/empty states;
- pagination/filtering where required;
- background jobs;
- full RBAC/facility/resource authorization;
- complete API catalogue;
- Swagger synchronization;
- full testing groups;
- CI release gate;
- operational logging/audit;
- documentation;
- regression and acceptance.

### 6.3.1 Release 1 Exit Gate

```text
[ ] All mandatory FRMS Release 1 features implemented
[ ] All mandatory business rules enforced
[ ] API contracts documented and synchronized with Swagger
[ ] Full database migration verified from empty database
[ ] Scheduled jobs configured and tested
[ ] Authentication/authorization security suite passes
[ ] DBT baseline passes
[ ] Critical concurrency/idempotency suite passes
[ ] Postman regression suite passes
[ ] Playwright E2E suite passes
[ ] Requirement -> API -> code -> test traceability complete
[ ] No committed secrets
[ ] Logging/audit requirements satisfied
[ ] Release 1 Definition of Done satisfied
```

## 6.4 Feature → Phase Mapping

All business features are completed by Release 1. Core Demo priority is the end-to-end path for all seven flows; optional enhancements are not blockers.

| Feature family | Core Demo | Release 1 |
|---|---:|---:|
| Public Guest discovery / login continuation | Entry to Flow 1 | Complete; AI remains optional |
| Customer lifecycle | Required | Complete |
| Staff operational lifecycle | Required | Complete |
| Manager facility operation | Required | Complete |
| BOM pricing/policy/report baseline | Required | Complete |
| Admin account/RBAC baseline | Required | Complete |
| System transaction/jobs | Critical subset required | Complete |
| AI Size Guide | Optional | Optional unless promoted |

## 6.5 Ownership Matrix

Current implementation responsibility baseline:

| Member | Main Responsibility |
|---|---|
| Bùi Đình Long | Project architecture, shared backend platform, Admin, Facility, Pricing/Policy, integration review |
| Trần Đăng Khoa | Reservation, Check-in, Rental lifecycle |
| Nguyễn Trần Trường Giang | Phase 0 foundation; Payment, Return, Fee, Support, Reporting, AI |
| Phạm Tuấn Triển | Customer Portal + Staff Portal frontend |
| Nguyễn Sỹ Minh Mẫn | Manager + BOM + Admin Portal frontend; frontend structure/component leadership |

# 7. System Architecture

## 7.1 High-Level Architecture

FRMS has exactly three **logical application layers**. `Frms.Infrastructure` is a supporting adapter assembly; it does not introduce an additional business layer.

```text
React Frontend
      ↓ HTTP / JSON
Frms.Api (Presentation)
      ↓ Business Commands / Results
Frms.Business (Business)
      ↓ Repository interfaces
Frms.DataAccess (Data Access)
      ↓ EF Core / database routines (functions/procedures)
PostgreSQL

Supporting assembly:
Frms.Infrastructure -> implements Business provider interfaces

Scheduled execution:
Frms.Api/BackgroundJobs -> Business services
approved PostgreSQL job scheduler (if separately deployed)        -> authoritative PostgreSQL business routines
```

## 7.2 Dependency Rules

Allowed runtime call paths:

```text
Controller -> Business Service
API Background Job -> Business Service
Business Service -> Repository Interface
Repository Implementation -> EF Core / DbContext
Repository Implementation -> Stored Procedure access
Infrastructure Adapter -> Business provider interface contract
```

The `Frms.Api` composition root MAY reference Business, DataAccess and Infrastructure registration extensions solely to assemble dependency injection. This permission does not allow Controllers, middleware or authorization handlers to call repositories, DbContext or provider implementations directly.

Not allowed:

```text
Controller -> Repository / DbContext / Stored Procedure
Controller -> external-provider implementation
API Background Job -> Repository / DbContext / Stored Procedure
Business -> Api
Business -> Infrastructure implementation
Service -> DbContext
DataAccess -> Api
Repository -> Controller
Frontend -> Database
```

Repository interfaces remain in `Frms.DataAccess` for the approved classical 3-layer dependency `Business -> DataAccess`. Moving them into Business would be a Clean Architecture revision and requires an explicit SRS architecture change.

## 7.3 Layer and Supporting-Assembly Responsibilities

### 7.3.1 Presentation Layer — `Frms.Api`

Responsible for:

- HTTP transport and API version routing;
- request/response DTOs;
- primitive DTO validation;
- DTO-to-Command and Result-to-DTO mapping;
- authentication middleware and authorization policies;
- Global Exception Middleware and HTTP status mapping;
- dependency-injection composition root;
- in-process background-job registration.

Must not contain SQL, direct DbContext access, authoritative business calculations or lifecycle orchestration.

### 7.3.2 Business Layer — `Frms.Business`

Responsible for:

- service interfaces and orchestration;
- Commands, Results and other Business models;
- business, actor, facility, ownership and lifecycle validation;
- application-side calculations and rules;
- security/time/external-provider abstractions;
- delegates/internal events and exceptions;
- `IClock`.

Business service contracts MUST NOT accept API Request DTOs, return API Response DTOs, or expose EF entities.

### 7.3.3 Data Access Layer — `Frms.DataAccess`

Responsible for:

- repository interfaces and implementations;
- EF Core, DbContext, entities and entity configuration;
- stored-procedure command/result wrappers and SQL scripts;
- migrations and PostgreSQL persistence;
- database dependency registration.

Repositories MUST NOT decide actor permissions or business state transitions. They execute persistence behavior requested by Business services and authoritative PostgreSQL business routines.

### 7.3.4 Supporting Infrastructure Adapter Assembly

`Frms.Infrastructure` implements `IPaymentGateway`, `IAiRecommendationProvider`, `IEmailService`, `INotificationSender` and future external-provider interfaces declared by Business. Provider wire DTOs remain inside their adapter.

Release 1 does not define a separate Worker project. Application-scheduled execution is hosted in `Frms.Api/BackgroundJobs` and calls Business services. Where available, approved PostgreSQL job scheduler (if separately deployed) MAY call the same authoritative PostgreSQL business routines directly.

## 7.4 Interface and Model Architecture

Baseline interface examples:

```text
IAuthenticationService  IPasswordHasher  ITokenService
ICurrentUserContext     IClock

IReservationService     IVisitService       IContractService
IPaymentService         IRenewalService     IReturnService
IInspectionService      ISupportTicketService
IReportingService       IPolicyService

IReservationRepository  IContractRepository IInvoiceRepository
IPaymentRepository      IStorageUnitRepository
IInspectionRepository   ISupportTicketRepository

IPaymentGateway         IAiRecommendationProvider
IEmailService           INotificationSender
```

Boundary model flow:

```text
API Request DTO
  -> API Mapping
    -> Business Command
      -> Business Service
        -> Business Result
          -> API Mapping
            -> API Response DTO
```

Exact interface/model count is implementation detail; these dependency and type boundaries are mandatory.

## 7.5 Delegate / Internal Event Rules

Delegates/events MAY be used for audit hooks, notification hooks, logging, cache invalidation and post-transaction events. They MUST NOT hide critical business transactions.

## 7.6 Transaction Ownership

```text
Simple CRUD/query:
Service -> Repository -> EF Core / DbContext

Critical multi-table lifecycle:
Service -> Repository -> authoritative PL/pgSQL routine -> PostgreSQL transaction
```

The Stored Procedure owns the authoritative database transaction for critical multi-table lifecycle operations.

## 7.7 External Integration and Background Execution Boundaries

- Business declares provider interfaces.
- Infrastructure contains provider implementations and provider-specific wire DTOs.
- API composition registers implementations through dependency injection.
- Payment, email, notification or AI implementations MUST NOT be instantiated inside Controllers or Business services.
- API BackgroundJobs call Business services and reuse the same authoritative PostgreSQL business routines and idempotency rules as HTTP-triggered flows.
- approved PostgreSQL job scheduler (if separately deployed) jobs MAY call authoritative PostgreSQL business routines directly and remain subject to the same idempotency/locking rules.

## 7.8 Baseline Solution Structure

```text
backend/
├── Frms.Api/
│   ├── Controllers/
│   ├── DTOs/Requests/
│   ├── DTOs/Responses/
│   ├── Mapping/
│   ├── Validation/
│   ├── Middleware/
│   ├── Authentication/
│   ├── Authorization/
│   ├── BackgroundJobs/
│   ├── DependencyInjection/
│   └── Program.cs
├── Frms.Business/
│   ├── Services/Interfaces/
│   ├── Services/Implementations/
│   ├── Models/Commands/
│   ├── Models/Results/
│   ├── Models/Common/
│   ├── Abstractions/Security/
│   ├── Abstractions/External/
│   ├── Abstractions/Time/
│   ├── Validators/
│   ├── Rules/
│   ├── Calculations/
│   ├── Mapping/
│   ├── Events/
│   └── Exceptions/
├── Frms.DataAccess/
│   ├── Repositories/Interfaces/
│   ├── Repositories/Implementations/
│   ├── Persistence/FrmsDbContext.cs
│   ├── Persistence/Entities/
│   ├── Persistence/Configurations/
│   ├── StoredProcedures/Commands/
│   ├── StoredProcedures/Models/
│   ├── StoredProcedures/Sql/
│   ├── Migrations/
│   └── DependencyInjection/
├── Frms.Infrastructure/
│   ├── Payments/
│   ├── Email/
│   ├── Ai/
│   ├── Notifications/
│   └── DependencyInjection/
└── tests/
    ├── Frms.UnitTests/
    ├── Frms.ApiTests/
    ├── Frms.IntegrationTests/
    └── Frms.ArchitectureTests/

frontend/src/
├── app/
├── routes/
├── layouts/
├── pages/
├── components/
├── features/
├── api/
├── hooks/
├── models/
├── auth/
└── utils/

tests/
├── postman/
└── e2e/playwright/
```

`Frms.Infrastructure` is a supporting adapter assembly. Its presence does not change the three logical layer model. Release 1 has no separate Worker project.

---

# 8. Engineering Conventions

## 8.1 Backend Coding Rules

- Controllers MUST remain thin.
- Controllers MUST call services.
- Services MUST NOT access `DbContext` directly.
- Repositories MUST not decide business state transitions.
- API MUST NOT expose EF entities directly.
- Currency MUST use `decimal`.
- Backend I/O MUST use async APIs where the underlying operation is asynchronous.
- For normal HTTP request work, `CancellationToken` MUST propagate Controller → Service → Repository/provider where supported.

## 8.2 C# Naming Convention

| Element | Convention | Example |
|---|---|---|
| Class | PascalCase | `ReservationService` |
| Interface | `I` + PascalCase | `IReservationService` |
| Method | PascalCase | `CreateReservationAsync` |
| Async method | `Async` suffix | `GetContractAsync` |
| Property | PascalCase | `ReservationId` |
| Request DTO | PascalCase + `Request` | `CreateReservationRequest` |
| Response DTO | PascalCase + `Response` | `ReservationResponse` |
| Enum type | PascalCase singular | `ReservationStatus` |
| Parameter | camelCase | `reservationId` |
| Local variable | camelCase | `activePolicy` |
| Private field | `_camelCase` | `_reservationRepository` |
| Constant | PascalCase | `DefaultPageSize` |
| Delegate | PascalCase | `BusinessEventHandler` |

## 8.3 React / TypeScript Naming Convention

| Element | Convention | Example |
|---|---|---|
| Component | PascalCase | `ReservationCard` |
| Component file | PascalCase.tsx | `ReservationCard.tsx` |
| Page | PascalCase | `ReservationDetailPage` |
| Hook | `use...` | `useReservation` |
| Function | camelCase | `createReservation` |
| Variable | camelCase | `reservationStatus` |
| Type | PascalCase | `Reservation` |
| Props type | `ComponentNameProps` | `ReservationCardProps` |
| API module | camelCase | `reservationApi.ts` |
| Utility | camelCase | `formatCurrency.ts` |
| Constant | UPPER_SNAKE_CASE | `DEFAULT_PAGE_SIZE` |
| Feature folder | kebab-case | `return-management/` |
| Route | kebab-case | `/my-contracts` |

## 8.4 DTO / Entity / Mapping Rules

```text
Request DTO
!= Response DTO
!= Business Command
!= Business Result
!= Database Entity
```

Rules:

- API Request DTOs are validated for transport/primitive concerns and mapped to Business Commands in `Frms.Api`.
- Business service interfaces accept Business Commands or explicit scalar inputs, not API DTOs.
- Business services return Business Results, not API Response DTOs or EF entities.
- `Frms.Api` maps Business Results to Response DTOs.
- Database entities MUST NOT be serialized directly as API responses.
- Business code MUST NOT reference `Frms.Api` DTOs or `Frms.Infrastructure` implementations.
- Provider-specific wire DTOs remain inside their Infrastructure adapter.
- Stored-procedure parameter/result models remain inside DataAccess and do not become public API contracts.

## 8.4.1 Dependency Injection and Composition

- `Frms.Api/Program.cs` is the primary composition root.
- Each implementation assembly SHOULD expose focused registration extensions such as `AddBusiness`, `AddDataAccess` and `AddInfrastructure`.
- Controllers and Business services MUST receive dependencies through constructor injection.
- Service-locator access and ad-hoc provider construction inside Controllers/Services are prohibited.
- Background jobs hosted by `Frms.Api` use the primary composition root and call Business services.

## 8.5 Environment and Configuration

Required environments:

```text
Development
Testing
Production
```

Secrets such as JWT signing key, SQL connection, payOS ApiKey/ChecksumKey, AI keys, and notification credentials MUST NOT be hard-coded or committed.

## 8.6 Validation Standard

```text
DTO validation
    -> required / format / primitive range

Service validation
    -> business rule / ownership / actor / lifecycle precondition

Database validation
    -> integrity / uniqueness / transaction / concurrency invariant
```

## 8.7 Date and Time Standard

Storage:

```text
UTC / GMT+0
```

Business and display timezone:

```text
GMT+7
Asia/Ho_Chi_Minh
```

Rules:

- timestamps are persisted in UTC;
- API timestamps use UTC;
- FE converts timestamps to GMT+7 for display;
- business calendar-day rules are evaluated in GMT+7;
- month-only fields are not timezone-converted like timestamps;
- use `IClock` / UTC rather than scattered `DateTime.Now`.

---

# 9. Data Architecture


## 9.1 Data Model Authority

Data Dictionary V11 ([`FRMS_Data_Dictionary_V11.md`](FRMS_Data_Dictionary_V11.md)) aligns the approved V10 logical baseline with owner-approved §0.0.9 changes and verified PostgreSQL/Npgsql code observations. The 27 original persisted business entities remain historically traceable; V11 adds the approved 28th `HandoverUnitAssignment`. Provider-specific constraints, PL/pgSQL routines and Pg010–Pg013 extensions require V11 release gates and actual database verification.

## 9.2 Persisted Entity Catalogue (approved core vs observed extension)

V10's *historically approved core* catalogue has 27 persisted entities. **Owner-approved V11 §0.0.9 adds `HandoverUnitAssignment` as business entity #28**, represented by migration `Pg013`. The current code includes the 28th table, while its actual deployment must be checked independently in each target database. The original 27 entities are:

1. `UserRole`
2. `Policy`
3. `Facility`
4. `UnitType`
5. `UserAccount`
6. `Customer`
7. `Employee`
8. `Discount`
9. `StorageUnit`
10. `Reservation`
11. `Visit`
12. `Contract`
13. `ContractExtension`
14. `Invoice`
15. `Payment`
16. `LateFee`
17. `SupportTicket`
18. `Inspection`
19. `DamageType`
20. `DamageRecord`
21. `InspectionEvidence`
22. `ExtraFeeType`
23. `ExtraFee`
24. `DepositSettlement`
25. `LoginHistory`
26. `AuditLog`
27. `NotificationLog`

### 9.2A Approved V11 extension: HandoverUnitAssignment

**Source:** `backend/sql/postgresql/HandoverAssignments.sql` and migration `Pg013`.

| Field | PostgreSQL type / constraint | Description |
|---|---|---|
| `AssignmentId` | uuid PK, generated | Assignment history identity |
| `ReservationId` | uuid NOT NULL FK -> Reservation | Existing CONFIRMED Reservation |
| `StorageUnitId` | uuid NOT NULL FK -> StorageUnit | Actual unit selected at check-in, not during Reservation creation |
| `AssignedByEmployeeId` | uuid NOT NULL FK -> Employee | Authenticated Manager business profile |
| `AssignedAt` | timestamptz(3) NOT NULL | UTC assignment time |
| `Status` | varchar(12) CHECK | ACTIVE / REPLACED / CONSUMED / RELEASED |
| `ClosedAt` | timestamptz(3) NULL | Required for closed statuses, null while ACTIVE |

Current implementation has filtered UNIQUE indexes for one ACTIVE row per Reservation and per StorageUnit. History guards prohibit DELETE and edits to closed records. All assignment/replacement/release actions use PostgreSQL routines and checked role/Facility ownership. Selection only occurs after the RESERVATION Visit becomes CHECKED_IN; Contract handover consumes a matching selection atomically. It does not modify the Reservation's `Facility+UnitType+month` capacity hold or change StorageUnit status to `IN_USE` prior to Contract activation.

**Owner approval:** §0.0.9 approves the 28th entity, state lifecycle, Manager/Staff role contract and post-check-in handover precondition as V11 requirements. **Acceptance remains pending** for applied migration history, FK/unique/immutability tests, assignment conflicts, role/Facility boundaries, atomicity/rollback, and E2E; approval does not certify the existing implementation.

Concepts intentionally not persisted as separate FRMS Release 1 tables include:

```text
Booking
FacilityAssignment
MonthlyRentalFee
ReturnProcess
Renewal
Refund
DamageFee
DailyTask
Report
DiscountRedemption
separate Maintenance
```

## 9.3 Explicit SRS Data Overrides

`OVERRIDE-LOCKED`

```text
UserAccount.Email       -> UNIQUE
UserAccount.PhoneNumber -> UNIQUE
```

Application validation and database uniqueness constraints MUST both enforce these.

Stable conflict codes:

```text
EMAIL_ALREADY_EXISTS
PHONE_NUMBER_ALREADY_EXISTS
```

## 9.4 Core Relationships

Key confirmed relationships:

- `UserRole 1:N UserAccount`.
- `UserAccount 1:0..1 Customer`.
- `UserAccount 1:0..1 Employee`.
- `Facility 1:N Employee` for facility-scoped roles.
- `Facility 1:N StorageUnit`.
- `UnitType 1:N StorageUnit`.
- `Customer 1:N Reservation`.
- `Customer 1:N Discount`.
- `Reservation 1:0..1 Contract`.
- `Contract 0..1 Discount`.
- `Contract 1:N ContractExtension`.
- `Contract 1:N SupportTicket`.
- `Contract 1:N Inspection` historically where return/recovery records require it.
- `Inspection 1:N DamageRecord`.
- `Inspection 1:N InspectionEvidence`.
- `Inspection 1:N ExtraFee`.
- `Invoice 1:N Payment`.
- `Invoice 1:0..1 LateFee`.
- `Contract 1:0..1 DepositSettlement`.

Polymorphic references:

```text
Visit.EntityId:
    RESERVATION -> Reservation
    ACCESS      -> Contract
    RETURN      -> Contract

Invoice.EntityId:
    DEPOSIT     -> Reservation
    RENTAL_FEE  -> Contract
```

## 9.5 Status Enumerations

### UserAccount

```text
ACTIVE
INACTIVE
```

### Facility

```text
ACTIVE
INACTIVE
```

### StorageUnit

```text
AVAILABLE
IN_USE
INSPECTION
MAINTENANCE
```

### Reservation

```text
PENDING_DEPOSIT
CONFIRMED
COMPLETED
CANCELLED
```

### VisitType

```text
RESERVATION
ACCESS
RETURN
```

### Visit

```text
SCHEDULED
CHECKED_IN
CHECKED_OUT
CANCELLED
```

### Contract

```text
ACTIVE
COMPLETED
TERMINATED
```

### InvoiceType

```text
DEPOSIT
RENTAL_FEE
```

### Invoice

```text
UNPAID
PAID
OVERDUE
CANCELLED
```

### Payment

```text
PENDING
SUCCESS
FAILED
```

### Discount

```text
ACTIVE
INACTIVE
```

### Policy

```text
ACTIVE
INACTIVE
```

### SupportTicket

```text
OPEN
IN_PROGRESS
COMPLETED
CANCELLED
```

### Inspection

```text
PENDING
IN_PROGRESS
COMPLETED
```

### DamageRecord

```text
PENDING
APPROVED
REJECTED
```

### ExtraFeeType / DamageType

```text
ACTIVE
INACTIVE
```

### DepositSettlement

```text
PENDING
FINALIZED
```

### NotificationLog

```text
PENDING
SENT
```

### LoginHistory

```text
SUCCESS
FAILED
```

## 9.6 Important Nullability / Conditional Rules

- **V11-OD-01 overrides historical REV-2026-10-20-CUSTOMER-NO-ADDRESS for collection:** `Customer.Address` stays optional/nullable (maximum 1,000 characters in observed API validation); Customer may provide it at AUTH-001 registration and change/clear it via AUTH-005. No required use or public/employee/BOM projection is introduced. Existing values must not be silently destroyed. `Facility.Address` is unchanged.
- AUTH-004 Customer own-profile `cccd` is private, `string | null`; the owner-only projection is observed in source at `2945788`, with running API/Swagger/FE integration still to be verified. No employee/admin/public projection inherits CCCD by this revision.
- `Customer.CCCD` retains nullable varchar(20) storage and uniqueness; OD-10 requires each newly supplied nonblank value and the saved value used for new RES-001 to be exactly 12 ASCII digits. Legacy values are preserved until explicitly corrected. This is a conditional creation rule, not a global NOT NULL column or verification status. Existing FullName and Employee name semantics are unchanged; no LegalName is introduced.
- `Employee.FacilityId` required for Facility Staff/Manager; not required for global employee roles.
- `Visit.EmployeeId` nullable before actual Staff handling.
- `Visit.ActualReturnDate` only meaningful for RETURN.
- `Contract.DiscountId` nullable.
- `Invoice.BillingMonth` null for DEPOSIT; required for RENTAL_FEE.
- `Invoice.DiscountId` null for DEPOSIT.
- `Invoice.PaidAt` is a nullable persisted UTC timestamp. Set it from the verified provider success timestamp only when the Invoice first transitions from UNPAID/OVERDUE to PAID. Already-PAID and CANCELLED invoices retain their timestamp; duplicate webhook never overwrites it. Historical rows without timestamp evidence remain null (owner-approved 2026-10-08 clarification).
- Every new Payment MUST reference an existing Invoice through Payment.InvoiceId. No first-month Payment is allowed. Historical null InvoiceId rows from the superseded model must be preserved until an approved migration/deployment review; do not silently link or delete them.
- `Payment.IdempotencyKey` is a required frontend UUID stored as `uuid NOT NULL`; it has no fabricated server-generated backfill, is globally unique and is retained unchanged for the lifetime of the Payment row. See section 12.7.1.
- `Payment.TransactionCode` nullable before gateway reference exists; unique when present.
- `Payment.PaidAt` required for `SUCCESS`.
- `Policy.EffectiveTo` nullable for current active version.
- `SupportTicket.AssignedEmployeeId` nullable while OPEN.
- `Inspection.VisitId` nullable for external recovery.
- `Inspection.EmployeeId` nullable before claim.
- `AuditLog.UserAccountId` nullable for system actions.
- `NotificationLog.SentAt` null while PENDING.

## 9.7 Historical Snapshot / Immutability Rules

The following MUST not be rewritten because a master value changes later:

- `Reservation.LockedRentalPrice`;
- `Reservation.DepositAmount`;
- issued Invoice monetary values;
- `ContractExtension.AppliedMonthlyPrice`;
- historical Policy parameter values;
- append-only `ContractExtension`;
- append-only `LoginHistory`;
- append-only `AuditLog`.

Reservation Facility/UnitType/StartMonth/EndMonth are immutable after creation.

## 9.8 Constraints and Indexes

Minimum design:

- UNIQUE `UserAccount.Email`.
- UNIQUE `UserAccount.PhoneNumber` — SRS override.
- UNIQUE `Customer.UserAccountId`.
- UNIQUE `Employee.UserAccountId`.
- filtered UNIQUE `Customer.CCCD` when present.
- `StorageUnit.UnitCode` uniqueness within Facility remains source-recommended rather than source-locked; implementation MUST NOT claim it as a final invariant unless explicitly approved. If implemented, use UNIQUE `(FacilityId, UnitCode)`.
- UNIQUE `Contract.ReservationId`.
- filtered UNIQUE one ACTIVE Contract per StorageUnit.
- one DEPOSIT Invoice per Reservation.
- one RENTAL_FEE Invoice per Contract + BillingMonth.
- UNIQUE `LateFee.InvoiceId`.
- UNIQUE `DepositSettlement.ContractId`.
- filtered UNIQUE `Payment.TransactionCode` when present.
- globally UNIQUE `Payment.IdempotencyKey` (`uuid NOT NULL`); a database unique constraint/index is the final protection against concurrent same-key attempt creation.
- one active Policy row.
- CHECK non-negative monetary values.
- CHECK Discount percentage 0..100.
- CHECK month ranges.
- CHECK legal status/type values.

## 9.9 Authoritative Database Routine Catalogue (PostgreSQL; historical names retained)

| Procedure | Primary responsibility | Atomic |
|---|---|---:|
| `usp_CreateReservation` | OD-09/10 saved-Customer valid 12-digit CCCD precondition + capacity check + Reservation + Deposit Invoice; reject missing CCCD without writes | Yes |
| `usp_ConfirmReservation` | validate paid Deposit + create RESERVATION Visit + confirm | Yes |
| `usp_CancelReservation` | cancel eligible Reservation/Visit and release hold | Yes |
| `usp_CheckInVisit` | facility/date/role validation + CHECKED_IN | Yes |
| `usp_CancelVisit` | valid SCHEDULED cancellation | Yes |
| `usp_CompleteHandover` | Staff offline-receipt acknowledgment + Contract + Unit IN_USE + lifecycle completion; no first-month Invoice/Payment | Mandatory |
| `usp_CreateAccessVisit` | create valid ACCESS Visit | Yes |
| `usp_CreateReturnVisit` | create RETURN Visit and block conflicting actions | Yes |
| `usp_ConfirmActualReturn` | ActualReturnDate + unit INSPECTION + Inspection creation | Mandatory |
| `usp_ClaimInspection` | atomic claim PENDING Inspection | Mandatory |
| `usp_RecordDamage` | record DamageRecord as PENDING | Yes |
| `usp_DecideDamage` | Facility Manager atomically APPROVE/REJECT a PENDING DamageRecord | Yes |
| `usp_RecordExtraFee` | record ExtraFee | Yes |
| `usp_AddInspectionEvidence` | store binary evidence | Yes |
| `usp_CompleteInspection` | complete Inspection; keep unit INSPECTION until Finalize Return | Yes |
| `usp_FinalizeReturn` | calculate settlement + terminal Contract state + unit AVAILABLE/MAINTENANCE | Mandatory |
| `usp_RenewContract` | contiguous capacity-safe renewal + ContractExtension | Mandatory |
| `usp_CreateMonthlyInvoice` | generate one logical Rental Fee invoice only for Contract.StartMonth < BillingMonth <= Contract.EndMonth | Yes |
| `usp_ApplyPaymentResult` | idempotent gateway result processing | Yes |
| `usp_CalculateLateFee` | calculate/update LateFee | Yes |
| `usp_MarkOverdueInvoices` | mark eligible invoices OVERDUE | Yes |
| `usp_CreatePolicyVersion` | insert new active Policy, inactivate prior version | Yes |
| `usp_AssignSupportTicket` | facility-safe staff assignment | Yes |
| `usp_CompleteSupportTicket` | record result and complete | Yes |
| `usp_CancelSupportTicket` | valid cancellation | Yes |
| `usp_SetUserAccountStatus` | activate/deactivate with lifecycle guard | Yes |
| `usp_QueueNotification` | queue PENDING notification | Yes |
| `usp_RetryPendingNotification` | retry pending notification | short/application background job |

## 9.10 Trigger Responsibilities

Triggers protect:

- polymorphic `Visit.EntityId`;
- polymorphic `Invoice.EntityId`;
- legal Reservation transitions;
- legal Visit transitions;
- legal Contract transitions;
- legal Inspection transitions;
- legal StorageUnit transitions;
- legal SupportTicket transitions;
- historical Policy;
- append-only ContractExtension/LoginHistory/AuditLog;
- Discount ownership;
- Employee Facility requirement;
- audit capture.

Triggers MUST NOT orchestrate Complete Handover or Return finalization.

## 9.11 Scheduled Jobs — V11 current-source reconciliation (`REV-2026-10-21-BACKGROUNDJOBS`)

**Notifications-only review boundary (2026-10-29):** the pending OD-06 replacement makes in-app notices visible after commit without waiting for this legacy mail pipeline. Worker routing/old-queue treatment require NOT-D01/02 approval; other jobs and account-email behavior are unchanged. No schedule/configuration/SQL is changed by this draft.

All six **business scheduled tasks** are in the V11 scope. The current `main` implementation hosts them in **four .NET background services**, because the three billing tasks share one ordered pipeline. A DI registration or SQL function is not proof of a successful production run.

| Business task / SRS ID | Current hosted service | PostgreSQL routine | Source-observed cadence | Default state in current code |
|---|---|---|---|---|
| `job_ExpirePendingReservations` | `ExpirePendingReservationsJob` | `usp_ExpirePendingReservations` | Immediately at startup, then every **10 minutes** | Always registered/runs while API host is running (existing behavior) |
| `job_GenerateMonthlyInvoices` | `BillingMaintenanceJob`, stage 1 | `usp_GenerateMonthlyInvoices` → `usp_CreateMonthlyInvoice` | Startup, then every **24 hours** | `BackgroundJobs:Billing:Enabled=true` by default |
| `job_MarkOverdueInvoices` | `BillingMaintenanceJob`, stage 2 | `usp_MarkOverdueInvoices` | Same run, **after successful stage 1** | Same Billing flag |
| `job_RecalculateOpenLateFees` | `BillingMaintenanceJob`, stage 3 | `usp_RecalculateOpenLateFees` → `usp_CalculateLateFee` | Same run, **after successful stage 2** | Same Billing flag |
| `job_ProcessReservationNoShow` | `ProcessReservationNoShowJob` | `usp_ProcessReservationNoShow` | Startup, then every **24 hours** when enabled | `BackgroundJobs:NoShow:Enabled=false` by default |
| `job_RetryNotifications` | `RetryNotificationsJob` | `usp_RetryPendingNotification` → sender → `usp_MarkNotificationSent` | Startup, then every **5 minutes** when enabled; currently **one queued item/run** | `BackgroundJobs:Notifications:Enabled=false` by default; sender unconfigured |

### 9.11.1 Runtime and ownership boundary

- `Program.cs` registers `AddFrmsBackgroundJobs()`. The four `IHostedService` registrations are in `BackgroundJobRegistration.cs`; existing expiration behavior is preserved. The patch requested by the owner has **already appeared on `main`** as of `0219f307`. No new HTTP endpoint, EF entity, migration or role is introduced by these jobs.
- Execution boundary MUST remain `Api BackgroundJob → Business Service → Repository → PostgreSQL function`. Never inject `DbContext`, SQL or Repository directly into the API job. The job does not impersonate a Customer/Manager identity.
- Billing MUST run in the ordered sequence `GenerateMonthlyInvoices → MarkOverdueInvoices → RecalculateOpenLateFees`, and a failed step MUST prevent subsequent stages in that tick. Exceptions are logged; retry occurs on next run/restart. `BillingMaintenanceJob` is **enabled when the setting is absent**; `BackgroundJobs__Billing__Enabled=false` disables it on restart.
- Scheduling uses `PeriodicTimer` **relative to API startup**, *not* midnight cron. Billing and NoShow intervals are 24 hours; billing date calculation uses `Asia/Ho_Chi_Minh`, while persisted instants are UTC. Do not describe a guaranteed 00:00 business-day execution.
- Multi-instance deployment and external PostgreSQL scheduler coexistence MUST be reviewed. Current SQL routines use transactional advisory locks and invoice uniqueness; repeated runs MUST NOT duplicate Invoice, LateFee or financial effects. Global delivery locking is distinct from billing locks.
- Notification retry MUST call a configured real `INotificationSender`, and MUST mark `NotificationLog.SENT/SentAt` **only after acknowledged delivery**. SQL `usp_RetryPendingNotification` only selects `PENDING`; it does not send. `UnconfiguredNotificationSender` is registered on `main` and deliberately fails. On send failure, retain `PENDING`. Provider should deduplicate on `NotificationLogId`; delivery-before-mark crashes may resend (at-least-once). Do not log recipient addresses, content, secrets or payment URLs.

### 9.11.2 Release-1 financial and lifecycle invariants

1. `usp_GenerateMonthlyInvoices` processes the **current business month only** and eligible `ACTIVE` Contracts where `StartMonth < BillingMonth <= EndMonth`. No Invoice/Payment is generated for the offline first month. Existing same-Contract/same-month invoice is reused; historical invoices are not repriced.
2. `usp_CreateMonthlyInvoice` captures the Reservation locked price or the applicable `ContractExtension` snapshot, the Contract selected Discount, the Contract-captured Policy due day and an initially `UNPAID` invoice.
3. `usp_MarkOverdueInvoices` applies the captured Policy `OverdueStartDay`; `usp_RecalculateOpenLateFees` calls `usp_CalculateLateFee` for eligible `OVERDUE` rental invoices, **inserting or updating one LateFee row per invoice** rather than creating duplicates.
4. `usp_ProcessReservationNoShow` is intended to close expired handover windows without cancelling already-completed business; the current SQL includes a checked-in Visit branch. Validate exact Visit/Reservation behavior against approved SRS and concurrency before enabling NoShow.
5. `ExpirePendingReservationsJob` cancels only expired unpaid Deposit Reservations using the existing procedure. No extra scheduled first-month charge, payment provider poller, automatic settlement/refund or new approval job is authorized.

### 9.11.3 Missing execution evidence and decisions (blocking release acceptance)

| Gap ID | Gap requiring verification or separate decision | Status |
|---|---|---|
| `JOB-GAP-01` | Run `dotnet build/test`, real PostgreSQL migration/routine integration and time-boundary E2E. `RecurringJobRegistrationTests` verifies **service registrations only**. | NOT VERIFIED |
| `JOB-GAP-02` | Confirm deployment scheduler ownership, multi-replica safety, startup side effects, monitoring/error alerts and safe pause/rollback (Billing defaults **true**). | NOT VERIFIED |
| `JOB-GAP-03` | Decide whether automatic **multi-month backfill** is required after a prolonged API outage. Current SQL generator chooses the current month only, so it does **not** fill older missed months. Do not silently introduce retroactive charges. | DECISION PENDING |
| `JOB-GAP-04` | Confirm run-at-midnight precision/SLA (current timer is relative to startup), business calendar boundary and whether once every 24 hours is sufficient for invoice due dates/LateFee freshness. | ACCEPTANCE PENDING |
| `JOB-GAP-05` | Verify NoShow SQL effects on `SCHEDULED` vs `CHECKED_IN` Visits and Facility/Policy ownership before enabling; default remains false. | TESTS / ACTIVATION PENDING |
| `JOB-GAP-06` | Implement/configure a real Notification sender; test no false `SENT`, retries, at-least-once deduplication, cross-instance lock and one-item-per-run backlog throughput. | PROVIDER NOT IMPLEMENTED |

No separate Worker project is required. A separately approved PostgreSQL/external executor MAY be substituted, but MUST follow the same SRS business rules, advisory locking and delivery safety contract. Its production existence has **not** been verified from repository source. Full acceptance gates: §27 V11-G10–G13 and Data Dictionary §27.

## 9.12 Migration Strategy

- schema is migration-based;
- empty DB must be recreatable;
- migrations should be repeatable/idempotent where practical;
- production historical snapshots must not be silently rewritten.

## 9.13 Seed / Test Data Strategy

Required production baseline seed:

For Phase 0, the required subset is the five UserRole rows, Policy v1 and six fixed DamageType rows below. ExtraFeeType data belongs to a later approved deployment; the database entity/table/constraints are retained in Phase 0 without seed rows.

```text
UserRole:
CUSTOMER
FACILITY_STAFF
FACILITY_MANAGER
BUSINESS_OPERATIONS_MANAGER
SYSTEM_ADMINISTRATOR
```

Initial Policy:

| Field | Initial value |
|---|---:|
| Version | 1 |
| Status | ACTIVE |
| DepositTimeoutHours | 1 |
| ReservationVisitStartDay | 1 |
| ReservationVisitEndDay | 5 |
| MonthlyPaymentDueDay | 5 |
| OverdueStartDay | 6 |
| LateFeeDivisorDays | 31 |
| EarlyReturnWaiveFeeUntilDay | 5 |

ExtraFeeType catalogue for later deployment (not Phase 0 seed):

Do not seed these five rows in Phase 0. DefaultAmount and currency require approved deployment data; no zero or other amount may be inferred.

```text
KEY_REPLACEMENT
ACCESS_CARD_REPLACEMENT
LOCK_REPLACEMENT
CLEANING_FEE
OTHER
```

`DESIGN-LOCKED — V9 FINAL`

Production `DamageType` seed:

```text
LOCK_DAMAGE
DOOR_DAMAGE
WALL_DAMAGE
FLOOR_DAMAGE
WATER_DAMAGE
OTHER
```

All seeded rows start `ACTIVE`. `DefaultAmount` is `NULL` unless approved deployment data supplies a value. Release 1 has no DamageType CRUD workflow.

### Release 1 operational master-data prerequisite

The application requires existing `UnitType` rows before Facility Manager can create/assign StorageUnits. Current sources do not define UnitType CRUD ownership beyond BOM price management.

Therefore:

- Release/demo deployment MUST provide required UnitType master rows through approved deployment seed/data preparation.
- This does not create a new UnitType CRUD workflow.
- DamageType provisioning is resolved by DD-15: the six fixed ACTIVE seed rows above and INS-010 provide the Release 1 production catalogue; no DamageType CRUD workflow is introduced.

# 10. Authentication and Authorization


## 10.1 Authentication Architecture

`DESIGN-LOCKED — FINAL`

```text
ASP.NET Core Authentication
JWT Bearer
ASP.NET Core Authorization
Custom UserAccount / UserRole
ASP.NET Core Identity: NOT USED
```

Authentication and authorization are separate concerns:

The following pipeline applies to authenticated private/operational access and business commands. The public allowlist in section 10.6.1 is intentionally accessible without account authentication and does not grant access to this protected pipeline.

```text
Authenticate identity
-> validate UserAccount ACTIVE
-> issue FRMS JWT
-> authorize role
-> authorize Facility/resource ownership
-> execute business rule
```

## 10.2 Customer Login

```text
Identifier: PhoneNumber
Credential: Password
Password verification: BCrypt
Endpoint: POST /api/v1/auth/customer/login
```

Rules:

- `PhoneNumber` is globally UNIQUE.
- resolved account MUST have role `CUSTOMER`.
- account MUST be `ACTIVE`.
- failed credential check returns `AUTH_INVALID_CREDENTIALS`.
- if the identifier resolves to a UserAccount, the authentication outcome is recorded in `LoginHistory`; an unknown identifier MUST NOT fabricate a UserAccountId and is recorded only in technical/security logging.

## 10.3 Employee Login

Applies to:

```text
FACILITY_STAFF
FACILITY_MANAGER
BUSINESS_OPERATIONS_MANAGER
SYSTEM_ADMINISTRATOR
```

```text
Identifier: Email
Credential: Password
Password verification: BCrypt
Endpoint: POST /api/v1/auth/employee/login
```

Rules:

- `Email` is globally UNIQUE.
- resolved role MUST be one of the four employee roles.
- account MUST be `ACTIVE`.
- if the identifier resolves to a UserAccount, the authentication outcome is recorded in `LoginHistory`; an unknown identifier MUST NOT fabricate a UserAccountId and is recorded only in technical/security logging.

## 10.4 Password Policy and BCrypt

Release 1 password validation:

```text
Length: 8..64 characters
Composition: no mandatory character classes
Encoded BCrypt input: <= 72 bytes
```

Hashing:

```text
Algorithm: BCrypt
Work Factor: 12
Configuration: configurable
```

Rules:

- plaintext password MUST never be persisted;
- `PasswordHash` MUST never be exposed through API;
- `PasswordHash` MUST never be written to `ILogger`, `AuditLog`, or exception payload;
- hashing/verification MUST be isolated behind `IPasswordHasher` or equivalent;
- work factor MUST be configurable so it can be benchmarked/changed without domain changes.

## 10.5 JWT Bearer Contract

`DESIGN-LOCKED — FINAL`

Release 1 uses FRMS-issued JWT Bearer authentication.

Minimum claims:

```text
sub / userAccountId
role
```

Optional convenience claims MAY include:

```text
customerId
employeeId
facilityId
```

but backend authorization MUST re-check authoritative server data when resource/facility state matters.

Requirements:

- JWT signing key is external configuration/secret;
- token expiration MUST be enabled;
- token lifetime MUST be configurable per environment;
- the SRS does not prescribe one numeric token lifetime;
- Release 1 does not require a refresh-token workflow;
- implementation MUST NOT add a persistent refresh-token subsystem without an explicit future SRS revision;
- invalid/expired token -> `401 UNAUTHORIZED`.

This resolves token behavior without inventing an unsupported numeric SLA.

## 10.6 Role Authorization Matrix

The following is the API-level authorization baseline for the five business roles. Anonymous is a separate public access mode, specified below, not a sixth matrix role. Endpoint-specific access in API Spec §13.11 controls operational reads.

| Capability | Customer | Facility Staff | Facility Manager | BOM | Administrator |
|---|---:|---:|---:|---:|---:|
| Own account/profile | Limited | Limited | Limited | Limited | View all basic accounts |
| Browse active Facility/UnitType | Yes | Read | Read | Read/manage | Read as needed |
| Reservation create/cancel | Own | No | No | No | No |
| Reservation read/list | Own | Same Facility detail; OPS-001 work list | Same Facility list/detail | System list/detail | No |
| Visit create | Own where applicable | No customer creation | Monitor | Read | No |
| Visit read/list | Own | Same Facility detail; OPS-001 work list | Same Facility list/detail | System list/detail | No |
| Visit check-in/out | No | Same Facility | Monitor | No | No |
| Complete Handover | No | Same Facility; command acknowledges offline first-month receipt | Preassign/reassign/release StorageUnit after CHECKED_IN; monitor; cannot execute Staff handover | No | No |
| StorageUnit management | No | Operational handling only | Same Facility | Read/monitor | No mutation; only the three approved read-only status timeline endpoints |
| Contract read/list | Own | Same Facility list/detail (CON-001/002; read-only) | Same Facility list/detail | System list/detail | No |
| Contract renewal | Own request | No | Monitor | Monitor | No |
| Invoice/Payment | Own view/pay | Same Facility read/verify only | Same Facility read/monitor | System read/report | No manual PAID |
| Policy | Current public articles/projection | Read | Read | Create version | Public projection only; no business mutation |
| UnitType price/master | Read | Read | Read | Manage | No |
| Discount | Own read/eligible usage | Read for Customer with same-Facility Reservation/Contract | Same-Facility Customer read | Manage | No |
| ExtraFeeType catalogue | No | ACTIVE types read | ACTIVE types read | Read/manage | No |
| Inspection | Own return progress | Claim/perform same Facility | Monitor same Facility | Read | No |
| Damage/ExtraFee | Own settlement read | Record during authorized Inspection | Monitor | Manage type masters | No |
| SupportTicket | Own create/view/cancel | Process assigned | Assign/monitor same Facility | Read/monitor | No |
| Employee/account administration | No | No | No | No | Yes |
| LoginHistory/AuditLog | No requirement | No | No | No | View; StorageUnit timeline projection only as V11-OD-03 |

### 10.6.1 Public Access Allowlist

| Capability | Anonymous access | Boundary |
|---|---|---|
| CAT-001..005 catalog | Yes | Public ACTIVE-Facility/offered-UnitType projection; current price and requested-period capacity only |
| PUB-POL-001/002 | Yes | Current public article/policy projection; no management/history/customer view |
| How It Works / Help / manual Size Guide | Yes | Approved public content; no dedicated API required for static explanatory content |
| AI-001 | Yes, when optional AI is implemented | Description/mode supplied in that request; no account-derived personalization |
| AUTH-001..003 | Existing anonymous auth entry points | Existing registration/login contract; no new identity-verification workflow |
| Reservation/Contract/Visit/Invoice/Payment/Discount/SupportTicket/profile/history/settlement | No | Missing authentication -> 401 UNAUTHORIZED; authenticated private access retains role/ownership/state checks |
| PAY-004 | Not a Guest capability | Provider authenticity and webhook validation remain mandatory |

Public access grants only the documented projection. A logged-in employee may consume that same projection as any visitor, but extra operational data still requires the endpoint's original role/current ACTIVE account/Facility checks. An inactive account can view public information available without an account; it gains no authenticated private access or mutation permission. Do not silently downgrade a denied operational request to a wider public result.

## 10.7 Facility Authorization

Facility Staff and Facility Manager MUST be scoped to exactly their assigned Facility.

For every protected facility-scoped operational read or command (not the identity-independent public projection):

```text
actor.Employee.FacilityId
MUST equal
targetResource.FacilityId
```

Failure -> `403 FORBIDDEN`.

Facility scope MUST be checked server-side even if FE routes hide other Facilities.

For Reservation detail, the resource Facility is Reservation.FacilityId. For Contract detail, it is Contract.FacilityId. For Visit detail and Visit work items, resolve Visit.EntityId by VisitType: RESERVATION -> Reservation.FacilityId; ACCESS/RETURN -> Contract.FacilityId. Do not treat every EntityId as a ReservationId or trust a client-supplied Facility identifier.

RES-003, CON-001, CON-002 and VIS-004 require an authenticated ACTIVE actor. Staff access additionally requires a current Employee assignment to the resource Facility. Staff may read Contracts belonging to different Customers within their assigned Facility via CON-001; server-side filtering must constrain both COUNT and page query before pagination, without accepting a client-owned FacilityId. Staff may read different Customers' authorized details within that Facility and is not evaluated under Customer-own-resource semantics. Cross-Facility detail access returns 403 FORBIDDEN; a missing Staff Facility assignment also returns 403. A nonexistent detail resource returns 404 RESOURCE_NOT_FOUND. Read permission does not authorize a state transition or bypass command-specific lifecycle checks.

## 10.8 Resource Ownership Authorization

Customer-owned resources include, where applicable:

- Reservation;
- Contract;
- Visit;
- Invoice;
- Payment;
- SupportTicket;
- return/settlement views.

Backend derives Customer identity from authenticated `UserAccount`; client-supplied `customerId` MUST NOT override actor identity.

Unauthorized cross-customer access returns `403` or non-disclosing `404` according to endpoint security design, but behavior MUST be consistent inside each resource family.

For shared operational-read endpoints, authorization MUST branch on the authoritative role: Customer -> own-resource check; Staff/Manager -> same-Facility operational-read check; BOM -> authorized system-wide read. Merely widening a Controller role attribute while retaining a Customer-only service query does not implement operational access. Never accept CustomerId or FacilityId from the UI as a substitute for authoritative authorization.

## 10.9 Inactive Account

`BR-ACC-01`

Inactive account:

- cannot log in;
- cannot initiate new business actions;
- historical data remains;
- existing business records are not deleted.

Customer deactivation additionally obeys `BR-ACC-02`.

## 10.10 Authorization Enforcement Boundary

Authorization is enforced at multiple levels:

```text
Controller / policy
-> role authentication gate

Service
-> Facility / resource ownership / business authorization

Stored Procedure
-> validates critical owner/facility identifiers where required to protect invariant
```

Security MUST NOT rely on FE control visibility.

## 10.11 Future External Authentication Extension

Architecture remains extensible through:

```text
IEmployeeExternalAuthenticationProvider
```

Current Release 1 does not require an implementation.

## 10.12 Future Google Login

Future Employee login only:

```text
Google OAuth 2.0 / OpenID Connect
-> GoogleAuthenticationProvider
-> map external identity to existing UserAccount
-> validate ACTIVE / role / Facility
-> issue FRMS JWT
```

Future model may introduce:

```text
ExternalLogin
-------------
ExternalLoginId
UserAccountId
Provider
ProviderSubjectId
CreatedAt

UNIQUE(Provider, ProviderSubjectId)
```

`ExternalLogin` is not part of the current 27-entity Release 1 schema.

# 11. Functional Requirements


## 11.1 Feature Requirement Format

Each detailed feature specification uses:

```text
Feature ID:
Feature Name:
Actor:
Target Phase:
Owner:
Release Blocker:
Description:
Preconditions:
Main Flow:
Alternative / Error Flow:
Business Rule IDs:
API IDs:
Entities:
database routines (functions/procedures):
Authorization:
Test Groups:
Acceptance Criteria:
```

## 11.2 Customer Features

| ID | Feature | Primary flow |
|---|---|---|
| `CWP-01` | Facility & Unit Type Browsing | Flow 1 |
| `CWP-02` | AI Size Guide Recommendation | Flow 1 / Optional |
| `CWP-03` | Storage Reservation | Flow 1 |
| `CWP-04` | Reservation & Deposit Management | Flow 1 |
| `CWP-05` | Reservation Visit Management | Flow 1 -> 2 |
| `CWP-06` | Rental Management | Flow 3 / 6 |
| `CWP-07` | Access Visit Management | Flow 3 |
| `CWP-08` | Rental Payment | Flow 3 / 6 — second month onward |
| `CWP-09` | Contract Renewal | Flow 6 |
| `CWP-10` | Storage Return Visit | Flow 6 |
| `CWP-11` | Fee & Return Tracking | Flow 6 |
| `CWP-12` | Customer Support | Flow 7 |

`CWP-01` shares its public catalog with GWP-01/02. `CWP-02` shares optional AI with Anonymous and remains optional, not a core release blocker unless explicitly promoted.

## 11.3 Facility Staff Features

| ID | Feature | Primary flow |
|---|---|---|
| `FWP-01` | Daily Work List | Flow 5 |
| `FWP-02` | Reservation Check-in | Flow 2 |
| `FWP-03` | Handover Processing | Flow 2 |
| `FWP-04` | Access Visit Processing | Flow 3 |
| `FWP-05` | Return Confirmation | Flow 6 |
| `FWP-06` | Return Inspection | Flow 6 |
| `FWP-07` | Return Fee Recording | Flow 6 |
| `FWP-08` | Support Request Processing | Flow 7 |

## 11.4 Facility Manager Features

| ID | Feature | Primary flow |
|---|---|---|
| `MWP-01` | Handover Unit Selection | Flow 2 |
| `MWP-02` | Physical Unit Management | Flow 5 |
| `MWP-03` | Facility Operations Monitoring | Flow 5 / 6 |
| `MWP-04` | Return, Inspection & Damage Decision | Flow 5 / 6 |
| `MWP-05` | Support Staff Assignment | Flow 7 |
| `MWP-06` | Facility Reporting | Flow 4 / 5 |

## 11.5 Business Operations Manager Features

| ID | Feature | Primary flow |
|---|---|---|
| `BWP-01` | Facility Management | Flow 5 |
| `BWP-02` | Unit Type Pricing | Flow 4 |
| `BWP-03` | Policy Version Management | Flow 4 |
| `BWP-04` | Discount Management | Flow 4 |
| `BWP-05` | Extra Fee Management | Flow 4 |
| `BWP-06` | Multi-Facility Monitoring | Flow 4 / 5 |
| `BWP-07` | Business Reporting & Export | Flow 4 |

### 11.5.1 Manager/BOM report read-model alignment — implementation review, not new approval

The existing FE layouts are the integration target for MWP-06 (Manager Unit/Revenue tabs) and BWP-07 (BOM Facility/UnitType/Revenue/Rental Status tabs and CSV export). The following **REP-RD-01..03 identifiers are provisional names for already-present backend routes**, not a renumbering of the approved REP-001..004 catalogue. See API V11 §13.22 for response fields and query options.

- **Manager, assigned Facility only:** REP-001 supplies the *current* four-state physical inventory/usage/overdue summary; REP-002 supplies recognized rental revenue for a selected business month (`yyyy-MM`); REP-RD-01 supplies current unit-type utilization, paginated current inventory and **evidence-based** daily status series for a selected month. The selected month MUST NOT cause current UnitType/location/Contract/customer fields to be represented as a historical snapshot. If AuditLog coverage is incomplete, `dailyTrend[].usageRate` is nullable and `unknownUnits` MUST remain visible; historical inventory support remains false.
- **BOM, system-wide read:** REP-RD-02 groups current physical state and selected-period recognized revenue by `facility`, `unitType` or Facility × UnitType (`status` view), with optional Facility/UnitType filters and paginated rows. REP-RD-03 supplies 12 monthly revenue rows for a selected business year, or three for a selected quarter. Year/quarter affect revenue, **not** the physical snapshot.
- **BOM export:** REP-004 remains UTF-8 CSV; omitted `view` preserves the existing monthly `revenue` file, while proposed `view=facility|unitType|status` exports the complete filtered grouped report, not just the current UI page. For every view, exported figures and filters MUST agree with the corresponding on-screen report; any text cells MUST be protected from spreadsheet formula injection. No persisted Report table or new financial state is introduced.

**Implementation/release gates (still open):** The FE pages on `main` do not yet consume these additional report routes; the Manager historical inventory cannot be truthfully reconstructed from the current schema. Grouped export in the reviewed backend patch pages its HTTP output but `BusinessBreakdownAsync` computes/materializes all groups before `Skip/Take`, so SRS §18.6 bounded-memory/DB-side-pagination performance acceptance is **NOT MET/NOT DEMONSTRATED** for large reports. Resolve and test that implementation before sign-off; Swagger, RBAC, CSV parity/security, daily AuditLog completeness and real PostgreSQL/API/E2E acceptance are pending.

## 11.6 System Administrator Features

| ID | Feature |
|---|---|
| `AWP-01` | User Account Monitoring |
| `AWP-02` | Customer Account Status Management |
| `AWP-03` | Employee Account Management |
| `AWP-04` | Role & Facility Assignment |
| `AWP-05` | Access Management |
| `AWP-06` | Login History |
| `AWP-07` | Activity Log Management |

## 11.7 System Services

| ID | Feature |
|---|---|
| `SSP-01` | Unit Type Catalog |
| `SSP-02` | Capacity Management |
| `SSP-03` | Deposit Calculation |
| `SSP-04` | Reservation Deposit Expiration |
| `SSP-05` | Handover Window Expiration |
| `SSP-06` | Rental Fee Calculation |
| `SSP-07` | Payment Result Processing |
| `SSP-08` | Atomic Complete Handover |
| `SSP-09` | Monthly Billing Management |
| `SSP-10` | Overdue & Late Fee Calculation |
| `SSP-11` | Renewal Processing |
| `SSP-12` | Return Processing |
| `SSP-13` | Inspection Claim Control |
| `SSP-14` | Deposit Settlement Calculation |
| `SSP-15` | Reporting Calculation |
| `SSP-16` | Policy Version Application |
| `SSP-17` | RBAC & Facility Authorization |
| `SSP-18` | Actor & Activity Audit |

Notification queue/retry is an internal supporting requirement from the Data Dictionary and is not introduced as a separate user-facing business flow.

## 11.8 External Service

```text
EPS-01 — payOS Payment Processing
EPS-02 — Employee Initial Credential Email Delivery
```

Used for:

- Deposit;
- Rental Fee invoices from the second month onward; offline first-month receipt does not use payOS.

It is not the settlement mechanism for LateFee, ExtraFee, Damage, RefundAmount or AdditionalAmountDue in FRMS Release 1.

## 11.9 Public Guest Features

GWP denotes a public functional capability, not a business role or new end-to-end flow. These features are also available to Customer; catalog/booking entry remains part of Flow 1. The feature names describe information and behavior, not required page layouts, component boundaries or navigation labels. Their acceptance obligations are effective under the team approval recorded in section 0.0.6.

| ID | Feature | Normative behavior / acceptance |
|---|---|---|
| `GWP-01` | Public Facility & UnitType Search | Anonymous/Customer search location + mode/type/size + inclusive month range; return matching ACTIVE Facilities and only matching UnitTypes with capacity > 0 in every requested month; identical public projection without personal data or holds |
| `GWP-02` | Storage Sizing Guidance (Size Guide) | Read offered Private/Public UnitTypes, existing size/description/current price; use the selected type/size in subsequent search; optional AI uses AI-001 and failure leaves manual selection available |
| `GWP-03` | How It Works | Explain Reservation, Deposit, scheduled handover/offline first-month receipt, Contract, visits and return using approved semantics; static content introduces no new lifecycle/API |
| `GWP-04` | Public Help / Policies / Contact | Read the four current public policy articles in §0.0.11 and contact information; no raw BOM policy/history, ticket creation or personal settlement; numerical policy text agrees with authoritative current Policy |
| `GWP-05` | Authentication Continuation | Ordinary Customer login enters the authenticated Customer area; interrupted action resumes the validated pending operation with preserved non-sensitive selection; re-read current data; no automatic business submission or global identity gate |

GWP-01..05 are required supporting Release 1 capabilities. The optional AI portion of GWP-02/CWP-02 is not promoted to a blocker by this revision. New ownership assignments are not inferred; delivery ownership is allocated by the existing project process.

## 11A. PostgreSQL Runtime / Deployment Reconciliation — V11 Candidate

**Observed current runtime:** `Frms.DataAccess/DependencyInjection/DataAccessServiceCollectionExtensions.cs` registers `Npgsql` / `UseNpgsql`; migrations are owned by `Frms.Migrations.PostgreSql`, not ordinary runtime EF migrations. The standalone migration chain in source is Pg001..Pg013. `Frms.Api` composes DataAccess via DI, with `Controller -> Business Service -> Repository -> EF Core / PL/pgSQL -> PostgreSQL`.

**Migration inventory relevant to V11:**

| Migration | Relevance | Business scope |
|---|---|---|
| Pg001–Pg009 | Baseline PostgreSQL tables/routines/seeds, payment constraints, full migration/runtime alignment and text-search helper | Port of existing business behavior; independent SQL/PG regression required |
| Pg010 | StorageUnit status AuditLog trigger, temporal timeline read; no backfill | Operational reporting/read model |
| Pg011 | Development/demo Visit check-in compatibility only | Not a production rule; isolated flag required |
| Pg012 | UnitType `ImageUrl`/`VideoUrl` nullable URL fields, length ≤2048 | Optional master-data extension; no invented default media |
| Pg013 | Manager-assigned unit selection table, four statuses, atomic handover guard | **Owner-approved in §0.0.9; deployment/test gates pending** |

PostgreSQL conventions: UUID (`uuid`), money `numeric`/decimal, UTC `timestamptz`, database-side transactional functions / PL/pgSQL, row locks and unique indexes, `pg_advisory_xact_lock` where used, app-level PostgreSQL exception mapping. Do not assume a SQL Server schema/constraint/transaction feature maps mechanically; each mapped invariant requires real PostgreSQL integration and concurrency tests. No automatic migration is run by API startup. Do not rewrite historical SQL Server migration records or legacy payOS Payment/Invoice evidence.

**Runtime auditing:** `PostgreSqlAuditInterceptor` propagates the authenticated actor into session scope for AuditLog source evidence; Pg010 captures events from application and database-triggered StorageUnit changes. Timelines can only claim history captured since the migration; they must not fabricate pre-migration intervals.

**Production scope note:** Repo includes PostgreSQL 17 compatibility and an approved Supabase database-only deployment record in migration README; that record is not proof that every Pg010–Pg013 migration has been applied in every environment. Independently check target migration history.

**Configuration:** `ConnectionStrings:Frms` or `ConnectionStrings:FrmsDb`, non-secret local environment configuration, direct/session-affine connection for audited operations; reject unsupported multiplexing. `Demo:AllowOutOfDateVisitCheckIn` is development-only opt-in and MUST NOT relax production date validation.

**Owner-approved RBAC exception (V11-OD-03):** `SYSTEM_ADMINISTRATOR` may use the three read-only StorageUnit timeline routes across Facilities; Manager remains own-Facility only. This exception does not grant Administrator other operational or financial reads or writes. Authorization/security tests remain mandatory.

---

# 12. Business Rules and Lifecycle Specification


## 12.1 Business Rule Catalogue

The following rule IDs are canonical for FRMS Release 1:

### Account / Employee / Facility

- `BR-ACC-01` Inactive account cannot log in or initiate authenticated business actions; identity-independent public access follows section 10.6.1 and grants no private/operational authority.
- `BR-ACC-02` Customer with CONFIRMED Reservation or ACTIVE Contract cannot be deactivated.
- `BR-ACC-03` (**V11-OD-01**) Customer Address is OPTIONAL at registration/profile PATCH, nullable/max 1000 characters; it is not a condition for reservation/payment/visit. No new role/Guest exposure or automatic downstream processing is authorized.
- `BR-ACC-04` (**OD-10**) Supplied nonblank CCCD must be exactly 12 ASCII digits after surrounding-whitespace normalization; preserve string/leading zeroes, optional registration, nullable/unique storage and PATCH omission/clear semantics. New RES-001 requires a valid saved value; legacy evidence is not rewritten.
- `BR-EMP-01` Facility Staff/Manager require one Facility; global employee roles do not.
- `BR-FAC-01` INACTIVE Facility cannot accept new Reservation.

### Public Discovery / Continuation

- `BR-PUB-01` Guest is anonymous access, not a persisted role/account/entity; shared public output is independent of account identity.
- `BR-PUB-02` Public discovery exposes ACTIVE Facilities and UnitTypes offered at ACTIVE Facilities, current public prices and aggregate capacity only. Private/operational/history data is excluded.
- `BR-PUB-03` A Facility matches location and has at least one offered UnitType matching all supplied mode/size/type filters and positive available capacity for every inclusive requested month. The same UnitType must satisfy the full range; capacity from different UnitTypes/months MUST NOT be combined. Filter before pagination/counting.
- `BR-PUB-04` Public browsing/AI/draft selection/login never holds capacity, assigns a physical unit or creates a Reservation, Invoice, Payment, Contract or Visit. Existing atomic RES-001 remains authoritative.
- `BR-PUB-05` Authentication and OD-09 profile-completion continuation preserve only a validated pending operation, safe destination/next valid step and non-sensitive selection; authoritative data/role/state are rechecked, and the Customer explicitly performs the next protected action. No forced return for ordinary menu navigation. Ordinary Customer login without a pending operation enters the authenticated Customer area.
- `BR-PUB-06` Public policy articles describe the current Policy, not a Customer's captured terms. Existing Reservation/Contract continues using captured PolicyId. Publishing public copy does not change Policy or financial rules.
- `BR-PUB-07` Anonymous may not access any account-specific/private read or command. Shared public endpoints do not waive protected role/Facility/ownership checks or provider-webhook validation.

### Reservation

- `BR-RES-01` StartMonth must be future and EndMonth >= StartMonth.
- `BR-RES-02` Reservation holds capacity by Facility + UnitType + month range, not StorageUnit.
- `BR-RES-03` capacity check + Reservation creation is atomic.
- `BR-RES-04` Deposit is UnitType price snapshot.
- `BR-RES-05` Deposit cannot use Discount.
- `BR-RES-06` Reservation confirms only after Deposit payment succeeds.
- `BR-RES-07` RESERVATION Visit date must satisfy captured Policy.
- `BR-RES-08` Facility/UnitType/StartMonth/EndMonth are immutable after creation.
- `BR-RES-09` (**OD-09**) Before new RES-001 writes, the server must verify the authenticated ACTIVE Customer's saved CCCD matches BR-ACC-04 / OD-10. Missing or invalid CCCD -> HTTP 400 VALIDATION_ERROR with errors.cccd; no Reservation, capacity hold or Deposit Invoice. CCCD remains optional at registration/nullable in Customer; this does not retroactively invalidate rentals or add a new prerequisite to their existing commands.

### Visit / Handover

- `BR-VIS-01` only SCHEDULED Visit may be cancelled.
- `BR-VIS-02` RESERVATION Visit references Reservation; ACCESS/RETURN reference Contract.
- `BR-VIS-03` ACCESS Visit requires ACTIVE Contract.
- `BR-VIS-04` pending RETURN Visit blocks ACCESS and Renewal.
- `BR-HO-01` selected StorageUnit must be AVAILABLE and match Facility + UnitType.
- `BR-HO-02` Staff may invoke Complete Handover only after receiving the full first-month rent offline. The authorized command acknowledges receipt; Contract creation records settlement without an Invoice/Payment or payment reference.
- `BR-HO-03` Complete Handover atomically creates Contract and updates Unit/Reservation/Visit state; it must not create or link a first-month Invoice/Payment. Retry must not create another Contract or duplicate initial recognized revenue.

### Contract / Discount / Billing

- `BR-CON-01` one Reservation creates at most one Contract.
- `BR-CON-02` one StorageUnit has at most one ACTIVE Contract.
- `BR-DIS-01` Customer owns many Discounts; each Discount belongs to one Customer.
- `BR-DIS-02` Contract uses zero or one Discount and it must belong to the Contract Customer.
- `BR-DIS-03` Contract-selected Discount is fixed for the Contract lifecycle in FRMS Release 1.
- `BR-DIS-04` issued rental Invoice snapshots Discount reference/amount.
- `BR-DIS-05` first-month offline rent does not use Contract Discount. Captured Contract Discount may apply to eligible invoices from Contract.StartMonth + 1 month onward, including the first generated Rental Fee invoice.
- `BR-BIL-01` exactly one logical Deposit Invoice per Reservation.
- `BR-BIL-02` at most one Rental Fee Invoice per Contract + BillingMonth.
- `BR-BIL-03` initial-period BaseAmount uses Reservation.LockedRentalPrice.
- `BR-BIL-04` extension-period BaseAmount uses ContractExtension.AppliedMonthlyPrice.
- `BR-BIL-05` unpaid Rental Invoice becomes OVERDUE by captured Policy threshold.
- `BR-BIL-06` the first month is settled offline for Reservation.LockedRentalPrice and has no Invoice/Payment. Rental Fee invoice generation, overdue detection and rental LateFee must exclude Contract.StartMonth.

### Payment / Late Fee

- `BR-PAY-01` payOS webhook processing is idempotent.
- `BR-PAY-02` Staff/Manager cannot manually mark an Invoice paid or create a successful gateway Payment. Staff's OPS-004 first-month receipt acknowledgment does not create or mark an Invoice/Payment.
- `BR-LATE-01` LateFee exists only for overdue RENTAL_FEE Invoice.
- `BR-LATE-02` LateFee uses Contract-captured Policy divisor.

### Renewal

- `BR-REN-01` renewal requires ACTIVE Contract and no pending RETURN Visit.
- `BR-REN-02` renewal is contiguous.
- `BR-REN-03` capacity is checked for every extension month atomically.
- `BR-REN-04` successful renewal appends ContractExtension and updates EndMonth atomically.

### Return / Inspection / Settlement

- `BR-RET-01` ActualReturnDate determines Normal vs Early Return.
- `BR-RET-02` confirmed return moves unit to INSPECTION and creates PENDING Inspection atomically.
- `BR-RET-03` Inspection claim is atomic.
- `BR-RET-04` early return terminates Contract after finalization.
- `BR-RET-05` normal return completes only after physical return/recovery + Inspection + settlement.
- `BR-RET-06` early return inside Policy waive window may cancel/waive unpaid current-month Invoice and must not create LateFee for that month.
- `BR-RET-07` Complete Inspection keeps Unit INSPECTION. Finalize Return atomically creates the settlement, terminates/completes Contract and changes Unit to AVAILABLE/MAINTENANCE. No Unit is released for a new handover while the prior return is unfinalized.
- `BR-DMG-01` DamageRecord belongs to Inspection and active DamageType.
- `BR-DMG-02` Staff records Damage as PENDING; same-Facility Manager atomically decides PENDING -> APPROVED or PENDING -> REJECTED. APPROVED/REJECTED are terminal.
- `BR-DMG-03` Settlement includes APPROVED Damage only and Finalize Return is blocked while any Damage remains PENDING.
- `BR-EXT-01` ExtraFee belongs to Inspection and snapshots configured/actual amount.
- `BR-SET-01` settlement uses applicable LateFee + ExtraFee + approved Damage.
- `BR-SET-02` early return forfeits Deposit; RefundAmount = 0.

### Policy / Support / Notification / Audit

- `BR-POL-01` Policy change creates a new version; old values are not overwritten.
- `BR-POL-02` existing Reservation/Contract uses captured PolicyId.
- `BR-SUP-01` SupportTicket is Contract-related and does not mutate core lifecycle directly.
- `BR-NOT-01` historical email-delivery failure remains PENDING for retry; the pending OD-06 replacement separates in-app eligibility from that email state. See BR-NOT-APP-01..05; other account-email flows are unchanged.
- `BR-AUD-01` ContractExtension, LoginHistory and AuditLog are append-only.

## 12.2 State Transition Matrix

### Reservation

```text
PENDING_DEPOSIT -> CONFIRMED
PENDING_DEPOSIT -> CANCELLED
CONFIRMED       -> COMPLETED
CONFIRMED       -> CANCELLED
```

All other transitions are rejected.

### Visit

```text
SCHEDULED -> CHECKED_IN -> CHECKED_OUT
SCHEDULED -> CANCELLED
```

Cancellation after CHECKED_IN is forbidden.

### Contract

```text
ACTIVE -> COMPLETED
ACTIVE -> TERMINATED
```

Terminal states cannot return to ACTIVE.

### StorageUnit

```text
AVAILABLE   -> IN_USE
AVAILABLE   -> MAINTENANCE
IN_USE      -> INSPECTION
INSPECTION  -> AVAILABLE
INSPECTION  -> MAINTENANCE
MAINTENANCE -> AVAILABLE
```

`AVAILABLE -> MAINTENANCE` is a V8 design override for preventive maintenance by the same-Facility Manager. MAINTENANCE units are excluded from capacity.

During a return, INSPECTION -> AVAILABLE/MAINTENANCE occurs only in usp_FinalizeReturn (INS-008), after the completed Inspection and settlement prerequisites. INS-007 leaves the Unit INSPECTION; UNIT-005 cannot perform an alternative return release.

### Invoice

```text
UNPAID  -> PAID
UNPAID  -> OVERDUE
OVERDUE -> PAID
UNPAID  -> CANCELLED   // only explicit valid business cases
```

Deposit Invoice does not become OVERDUE; unpaid timeout cancels Reservation.

### Payment

```text
PENDING -> SUCCESS
PENDING -> FAILED
```

A retry may create another Payment attempt rather than mutate FAILED back to PENDING.

### Inspection

```text
PENDING -> IN_PROGRESS -> COMPLETED
```

### DamageRecord

```text
PENDING -> APPROVED
PENDING -> REJECTED
```

Only same-Facility `FACILITY_MANAGER` may perform the decision. `APPROVED` and `REJECTED` are terminal.

### SupportTicket

```text
OPEN        -> IN_PROGRESS
OPEN        -> CANCELLED
IN_PROGRESS -> COMPLETED
IN_PROGRESS -> CANCELLED
```

### NotificationLog

Legacy email-delivery transitions below do not gate in-app visibility under the pending OD-06 replacement. No new status or read/unread lifecycle is selected.

```text
PENDING -> SENT
PENDING -> PENDING // legacy email failure, retry later
```

## 12.3 Capacity Rules

For every requested month:

```text
AvailableCapacity
=
eligible physical-unit capacity
- Reservation holds
- Contract occupancy
```

Invariant:

> Reservation holds + Contract occupancy MUST NOT exceed eligible StorageUnit capacity for Facility + UnitType in any month.

Reservation creation and Renewal capacity checks require concurrency protection.

For public requested-period display/search, `availableCapacity` is the minimum of the existing authoritative per-month AvailableCapacity across all months from StartMonth through EndMonth inclusive. A UnitType is available for that requested range only when this minimum is greater than zero. This projection reuses the existing capacity algorithm and eligible-unit rules; it does not redefine eligibility or allocate/hold a Unit. Browse results are a point-in-time indication and MUST be checked again by atomic Reservation creation.

## 12.4 Financial Calculations

| ID | Formula / source |
|---|---|
| `CALC-DEP-01` | `DepositAmount = UnitType.RentalPrice` snapshot |
| `CALC-INV-01` | Initial BaseAmount = `Reservation.LockedRentalPrice` |
| `CALC-INV-02` | Renewal BaseAmount = `ContractExtension.AppliedMonthlyPrice` |
| `CALC-FIRST-01` | First-month offline amount and Contract-derived recognized revenue = Reservation.LockedRentalPrice, without Contract Discount or first-month Invoice/Payment |
| `CALC-DIS-01` | For subsequent eligible Rental Fee invoices: `BaseAmount * Percentage / 100` |
| `CALC-INV-03` | `MAX(BaseAmount - DiscountAmount, 0)` |
| `CALC-LATE-01` | OverdueDays from captured Policy threshold to cutoff |
| `CALC-LATE-02` | `(RentalFeeAmount / Policy.LateFeeDivisorDays) * OverdueDays` |
| `CALC-SET-01` | LateFee + ExtraFee + approved Damage |
| `CALC-SET-02` | Normal Refund = `MAX(DepositPaidAmount - TotalDeduction, 0)`; Early = 0 |
| `CALC-SET-03` | `MAX(TotalDeduction - DepositPaidAmount, 0)` |
| `CALC-CAP-01` | capacity formula above |
| `CALC-REP-01` | Scoped IN_USE StorageUnits / all scoped StorageUnits (AVAILABLE + IN_USE + INSPECTION + MAINTENANCE); denominator 0 -> usageRate 0 |
| `CALC-REP-02` | Contract-derived first-month revenue attributed to Contract.StartMonth + successfully paid second-month-onward Rental Fee Invoice amounts; excludes Deposit/Late/Extra/Damage and double counting |

Money calculations use decimal arithmetic; final persisted monetary values use consistent rounding.

CALC-REP-01 is a physical-unit usage metric, not CALC-CAP-01 available rental capacity. Count all persisted StorageUnits in the authorized reporting Facility scope regardless of Unit status; only IN_USE enters the numerator. For a system-wide report, divide total IN_USE across the scoped Facilities by total Units across those Facilities rather than averaging Facility percentages. Do not exclude INSPECTION/MAINTENANCE from this denominator or infer a Facility ACTIVE filter that the report query does not specify.

**Reporting time/snapshot distinction:** `month=yyyy-MM` selects the Manager revenue month and the interval for AuditLog-based daily status evidence; it does not convert current inventory into historical inventory. On BOM grouped views, `year`/`quarter` select recognized revenue only, while all four physical statuses and usageRate remain current. Null/unknown historical readings must never be replaced with zero or current readings. These are read-model clarifications, not new persisted data requirements.

### 12.4.1 First-month settlement, billing and revenue boundaries

First-month offline rent equals the immutable Reservation.LockedRentalPrice snapshot. Contract existence records Staff's acknowledgment of full receipt during successful handover. Do not add a firstMonthPaid attribute, cash Payment, initial Invoice or receipt table.

Rental Fee billing MUST enforce:

```text
Contract.StartMonth < BillingMonth <= Contract.EndMonth
```

Use the original Contract.StartMonth even after renewal; do not skip the first month of each ContractExtension. A one-month Contract has no Rental Fee Invoice. The first generated invoice, if any, represents at least the second rental month and may apply the valid captured Contract Discount.

For an authorized reporting period and Facility scope:

```text
FirstMonthRecognizedRevenue
= SUM(Reservation.LockedRentalPrice once per Contract
      whose Contract.StartMonth falls in the reporting period)

SubsequentPaidRentalRevenue
= SUM(AmountDue once per successfully paid RENTAL_FEE Invoice
      with BillingMonth > its Contract.StartMonth
      and authoritative PaidAt in the reporting period)

RentalRevenue
= FirstMonthRecognizedRevenue + SubsequentPaidRentalRevenue
```

Include Contract history in ACTIVE, COMPLETED and TERMINATED states. Do not lose initial recognized revenue when rental ends. Use the immutable Reservation price, not a current UnitType/extension price. Deduplicate by Contract for initial recognition and by Invoice for subsequent paid revenue; joins, retries, webhooks and multiple Payment attempts must not multiply either amount.

Contract.StartMonth is the offline recognition month, not an actual cash-receipt timestamp. Online PaidAt reporting boundaries retain the GMT+7 business-calendar interpretation of UTC timestamps. Reporting is server-owned; the FE must not calculate the total.

Legacy first-month Invoice/Payment rows remain historical evidence but are excluded from the paid-invoice component, avoiding double counting with Contract-derived revenue. During a first-month return, no Rental Fee Invoice exists to waive or mark overdue and no rental LateFee is created for that month; existing Deposit/ExtraFee/Damage/settlement rules still apply.

## 12.5 Policy Version Rules

Each Policy row is a complete immutable business-parameter version.

Update means:

```text
current ACTIVE -> INACTIVE
insert new row with incremented Version -> ACTIVE
```

Existing Reservation/Contract keeps captured `PolicyId`.

## 12.6 Transaction / Concurrency Rules

| Operation | Required protection |
|---|---|
| Create Reservation | transaction + capacity re-check + serializable/equivalent range lock |
| Complete Handover | transaction + update lock selected StorageUnit |
| Renew Contract | same future-capacity locking strategy as Reservation |
| Claim Inspection | conditional update PENDING -> IN_PROGRESS; exactly one row |
| Decide Damage | conditional update PENDING -> APPROVED/REJECTED; exactly one manager decision wins |
| payOS webhook | unique `orderCode`/`reference` + idempotent procedure |
| Monthly invoice generation | logical uniqueness + insert-if-absent transaction |
| Deposit invoice creation | logical uniqueness |
| Create Policy version | lock active version / sequence |
| Finalize Return | unique settlement + terminal-state guard |
| Return Unit release | same authoritative Finalize Return transaction; no release at Inspection completion |
| Retry Notification | claim/update or background-job lock |

## 12.7 Idempotency Rules

Repeat behavior MUST preserve logical single-effect semantics for:

- payOS webhook;
- Complete Handover;
- Confirm Reservation;
- monthly Invoice generation;
- LateFee calculation;
- Renewal;
- Confirm Actual Return;
- Finalize Return;
- notification queueing where event semantics require one message.

### 12.7.1 PAY-001 Payment-Attempt Idempotency Contract

1. `POST /api/v1/invoices/{invoiceId}/payments/payos` requires an `Idempotency-Key` header containing a UUID generated by the frontend. The frontend generates one new UUID for each deliberate Customer payment action.
2. Automatic HTTP retries of the same action reuse the same key. A new deliberate retry, including a new attempt after failure, uses a new key; new-attempt eligibility is still validated.
3. Store the key in `Payment.IdempotencyKey` as `uuid NOT NULL`. It is globally unique across all Invoices and Customers, not only within an Invoice or Customer, and remains stored unchanged for the lifetime of the Payment row.
4. Same key and same Invoice resolves to the existing attempt: do not create another Payment, allocate another provider order code for that attempt or create another provider session. Return/reuse its persisted session information when available; terminal Payment state remains terminal.
5. Same key with a different Invoice returns a controlled conflict, HTTP 409 with stable code `PAYMENT_IDEMPOTENCY_CONFLICT`, without disclosing the existing Payment or its session.
6. A key associated with another Customer MUST NOT disclose that Customer's Payment. Authentication, authoritative Invoice ownership and existing-attempt ownership checks precede any attempt/session disclosure. Repositories supply ownership data; Business decides actor authorization.
7. Concurrent same-key requests MUST produce exactly one Payment row. The atomic repository operation and database uniqueness enforce this invariant; the unique-key race resolves by reading the winning row and then applying same-Invoice/conflict/non-disclosure handling, not by exposing an uncontrolled SQL exception.
8. Only the atomic `Created` outcome may call the provider create operation. A concurrent loser resolving to `Existing` must not create another session. Do not hold a database transaction across gateway calls.
9. Resolve authorized existing attempts/conflicts before gateway configuration checks. For a new attempt, configuration validation must succeed before atomic creation. Configuration failure or cancellation must propagate without creating Payment/session, consuming the key or writing a Payment result.
10. Persist `PaymentUrl` (nullable until a session exists, maximum 2048 characters) and nullable `PaymentUrlExpiresAt` in UTC so an authorized HTTP retry can reuse the same stored session. PAY-003 must not expose the URL or idempotency key; logs must not contain the complete PaymentUrl.
11. If a stored session is known to be expired, resolve the stored attempt through a controlled expired/conflict outcome; never create another attempt or session silently with that key. A new deliberate Customer action requires a new key. Do not invent an expiry when no provider/request expiry is available.
12. An existing PENDING attempt without a session remains the existing attempt; the same-key path must not bypass the Created-only rule or perform recovery. Unknown provider outcome/timeout remains PENDING, as specified in PAY-001. A definitive failure does not reset the key or make a terminal attempt reusable as a new attempt.
13. Webhook result idempotency uses the verified gateway transaction/reference. ProviderOrderCode resolves the Payment; neither the frontend Idempotency-Key nor a display description is authoritative webhook identity.
14. No migration may manufacture missing IdempotencyKeys or Invoice links. Unexpected rows incompatible with the required key must block deployment with a clear diagnostic and remain preserved pending approved deployment review; do not silently delete or rewrite them.

Required verification includes SQL rejection of null/duplicate keys, same-key reuse, different-Invoice/cross-Customer non-disclosure, concurrent same-key creation, different-key new attempts, retained terminal states, expired-session handling and configuration failure/cancellation without key consumption. Database uniqueness and concurrency must be proved using real PostgreSQL, not EF InMemory; listing these requirements does not claim the tests have run.

## 12.8 Stable Error Codes

Canonical Data Dictionary codes:

```text
ACCOUNT_INACTIVE
FACILITY_INACTIVE
INVALID_MONTH_RANGE
CAPACITY_NOT_AVAILABLE
RESERVATION_INVALID_STATUS
DEPOSIT_NOT_PAID
VISIT_DATE_OUT_OF_POLICY
VISIT_INVALID_STATUS
VISIT_ENTITY_MISMATCH
CONTRACT_NOT_ACTIVE
RETURN_VISIT_PENDING
UNIT_NOT_AVAILABLE
UNIT_FACILITY_TYPE_MISMATCH
DISCOUNT_NOT_OWNED_BY_CUSTOMER
DISCOUNT_NOT_VALID
INVOICE_ALREADY_EXISTS
PAYMENT_CALLBACK_DUPLICATE
PAYMENT_IDEMPOTENCY_CONFLICT
INSPECTION_ALREADY_CLAIMED
INSPECTION_INVALID_STATUS
RENEWAL_CAPACITY_NOT_AVAILABLE
RENEWAL_NOT_CONTIGUOUS
POLICY_VERSION_CONFLICT
SUPPORT_TICKET_INVALID_STATUS
RETURN_ALREADY_FINALIZED
```

SRS additional account-uniqueness codes:

```text
EMAIL_ALREADY_EXISTS
PHONE_NUMBER_ALREADY_EXISTS
```

# 13. API Contract Catalogue — External Reference (V11)

All executable API definitions are maintained in the **separate companion document**
[`FRMS_API_V11.md`](FRMS_API_V11.md).

It contains the formerly embedded full §13.1–13.27 catalogue: HTTP routes,
roles, request/response JSON schemas, error envelopes, paging, time formats,
Swagger rules, security constraints, and the V11 implementation-delta registry.
The API document is authoritative **only for API wire contracts after owner
approval** and may not override the business/domain rules of this SRS.

Legacy references in this SRS to `§13.x` or `section 13.x` mean
`FRMS_API_V11.md §13.x`; the API annex's `§13A` contains
**provisional** implementation-derived endpoints and does not grant permissions
until expressly approved.

For this release, changes to endpoints and DTOs MUST preserve the usual
`Controller -> Service -> Repository -> EF Core/PostgreSQL routines` boundary;
user/Facility authorization is evaluated server-side and not inferred from frontend
visibility. All role changes need a revised SRS authorization matrix plus security
tests and OpenAPI synchronization.

---

# 14. Frontend Requirements

**V11 Account Settings synchronization note (OD-01/09):** Customer Address remains optional; Account Settings need not add an Address editor/read projection. Email/PhoneNumber remain unchanged through AUTH-005. CCCD is optional for registration/ordinary profile use, but required for new RES-001 under BR-RES-09. Reuse AUTH-004/005 for own CCCD editing and in-flow completion; AUTH-004 serialization is now present in source, not runtime-certified here. A missing/malformed field is unavailable, not confirmed null; do not enable destructive edits or silently bypass the reservation prerequisite. No separate Identity Verification page, LegalName or verified-identity claim is required. Ordinary Settings save stays on that page; prerequisite completion returns to its safe originating operation. UI mock tests are not live API acceptance.



## 14.1 React Architecture

`DESIGN-LOCKED — V10 FINAL`

Frontend baseline structure:

```text
src/
├── app/
│   ├── App.tsx
│   ├── router.tsx
│   └── providers/
├── auth/
│   ├── AuthProvider.tsx
│   ├── RequireAuth.tsx
│   ├── RequireRole.tsx
│   └── auth.types.ts
├── api/
│   ├── httpClient.ts
│   ├── authApi.ts
│   ├── catalogApi.ts
│   ├── reservationApi.ts
│   ├── billingApi.ts
│   ├── contractApi.ts
│   ├── visitApi.ts
│   ├── inspectionApi.ts
│   ├── supportTicketApi.ts
│   ├── storageUnitApi.ts
│   ├── businessApi.ts
│   ├── reportApi.ts
│   └── adminApi.ts
├── features/
├── layouts/
├── pages/
├── components/
├── hooks/
├── models/
└── utils/
```

## 14.2 Feature Folder Convention

```text
features/reservation/
├── api/
├── components/
├── hooks/
├── models/
└── pages/
```

Feature code MUST use the shared HTTP client abstraction rather than creating ad-hoc clients, except isolated provider-specific code explicitly outside normal FE API access.

## 14.3 HTTP Client Responsibilities

For protected/API requests, the common client MUST:

- attach JWT Bearer token for authenticated requests; public allowlisted browsing must also work without a token/account;
- serialize query parameters consistently;
- deserialize common envelopes;
- expose `traceId` on errors;
- route `401` to authentication recovery/logout handling;
- preserve `403` and `409` as business-visible errors;
- not interpret provider redirect as payment success.

## 14.4 Frontend API Error Type

TypeScript error model:

```typescript
export interface ApiError {
  code: string;
  message: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
```

Frontend logic uses `code`, not parsing `message`.

## 14.5 Authentication State

Minimum FE auth state:

```typescript
type UserRole =
  | "CUSTOMER"
  | "FACILITY_STAFF"
  | "FACILITY_MANAGER"
  | "BUSINESS_OPERATIONS_MANAGER"
  | "SYSTEM_ADMINISTRATOR";

interface AuthUser {
  userAccountId: string;
  role: UserRole;
  status: "ACTIVE" | "INACTIVE";
  customerId?: string;
  employeeId?: string;
  facilityId?: string | null;
}
```

JWT content is not the only authorization authority; backend checks account/resource state on every protected operation.

The absence of AuthUser selects Anonymous public access, not an extra UserRole value. Public capability must not require a login/session solely to obtain identity-independent public data. Account-specific operations remain guarded.

## 14.6 Route Groups

Route-group baseline:

```text
/customer/*
/staff/*
/manager/*
/business/*
/admin/*
```

Role-aware routes are UX boundaries only.

The public functional capabilities in section 11.9 are available without authenticated Customer membership; their exact frontend URLs and presentation are defined by the frontend owner. Authentication/authorization requirements are governed by sections 10 and 13, independent of route visibility or naming. Existing protected route groups remain in force.

## 14.7 Canonical FE Domain Types

Example:

```typescript
export type ReservationStatus =
  | "PENDING_DEPOSIT"
  | "CONFIRMED"
  | "COMPLETED"
  | "CANCELLED";

export type VisitStatus =
  | "SCHEDULED"
  | "CHECKED_IN"
  | "CHECKED_OUT"
  | "CANCELLED";

export type ContractStatus =
  | "ACTIVE"
  | "COMPLETED"
  | "TERMINATED";
```

Enums MUST mirror API string values.

## 14.8 Date / Time Handling

FE receives UTC timestamps and converts them to GMT+7 for display.

Date/time conversion boundary:

```text
API timestamp string
-> parsing utility
-> GMT+7 display formatter
```

Do not apply timezone conversion to month-only strings.

## 14.9 Money Handling

FE displays server-returned monetary values.

FE MUST NOT recompute authoritative:

- Deposit;
- Discount amount;
- Rental Fee;
- LateFee;
- settlement;
- revenue.

Formatting is allowed; business calculation is not.

## 14.10 Payment UI Rules

This provider flow applies only to Deposit and second-month-onward Rental Fee Invoice payments:

```text
Start payment
-> redirect/open provider
-> user returns to FE
-> FE queries Payment/Invoice status
-> UI reflects authoritative server status
```

Navigation to/from payOS is never proof of payment success.

For the first month, Customer receives offline-payment instructions and Staff acknowledges full receipt by submitting Complete Handover. Do not show a first-month payOS payment button or create an initial Invoice/Payment history row. Contract creation records settlement; absence of an initial invoice is intentional, not an unpaid charge.

Staff sees the server-owned Reservation.LockedRentalPrice and must acknowledge full receipt before submitting OPS-004. The authorized request itself is the acknowledgment; no separately persisted first-month-paid flag is required. UI checks do not replace server authorization and lifecycle validation.

Staff loads Visit/Reservation context through OPS-001 -> VIS-004/RES-003. Staff may independently browse same-Facility Contracts through CON-001 (read-only). After OPS-004 succeeds, use its contractId -> CON-002; ContractDetail.reservationId -> RES-003 supplies the immutable first-month amount and handover Visit without adding financial/Visit attributes to Contract. Staff must not call Customer-only list/write endpoints or synthesize missing operational data.

## 14.11 Loading / Empty / Error States

Every Release 1 data screen MUST handle:

- loading;
- empty;
- validation error;
- 401;
- 403;
- 404;
- 409;
- unexpected error.

For a `409`, FE MUST surface the business conflict and MUST refresh authoritative resource state when the conflict may have made displayed state stale.

## 14.12 API-to-Module Mapping

| API family | FE module |
|---|---|
| AUTH-* | `authApi.ts` |
| CAT-* / AI-* | `catalogApi.ts` / AI feature API |
| PUB-POL-* | Public Help/policy read module; no reuse of privileged BOM policy-history DTO |
| RES-* | `reservationApi.ts` |
| BIL-* / PAY-* | `billingApi.ts` |
| CON-* | `contractApi.ts` |
| VIS-* / OPS-* | `visitApi.ts` |
| INS-* | `inspectionApi.ts` |
| SUP-* | `supportTicketApi.ts` |
| UNIT-* | `storageUnitApi.ts` |
| BOM-* | `businessApi.ts` |
| REP-* | Existing `facilityReportApi.ts` (Manager REP-001), `businessMonitoringApi.ts` (BOM REP-003) and `businessApi.ts` (report filter catalogues); dedicated typed report reads/CSV download remain FE integration tasks; `reportApi.ts` is the earlier intended generic module, not evidence the live Report tabs are wired. |
| ADM-* | `adminApi.ts` |

## 14.13 Mock-First FE Development

FE MAY implement against local mock objects that exactly match the current SRS schemas before BE endpoints are complete.

Mock behavior MUST NOT invent:

- statuses;
- fields;
- financial formulas;
- permissions;
- lifecycle transitions.

When BE becomes available, the mock is replaced without changing feature semantics.

## 14.14 Swagger Synchronization

Before a feature is marked Done:

```text
SRS JSON example
≈ Swagger schema
≈ TypeScript interface
```

Any mismatch must be resolved before Release 1 acceptance.

## 14.15 Public Capabilities and Authentication Continuation

`TEAM-APPROVED — REV-2026-10-13-GUEST`

This section specifies functional behavior and security boundaries shared by the project. The 2026-10-13 Guest requirements are extended by **TEAM-APPROVED OD-09 — REV-2026-10-25-CUSTOMER-CCCD-CONTINUATION**. The functional in-flow completion/return behavior below is approved; visual styling and component implementation remain frontend-owned.

- Customer retains every public capability available to Anonymous under section 11.9. Public Facility/UnitType/price/capacity/policy output is independent of account identity for the same filters/data snapshot.
- Search inputs are location/Facility, Private/Public mode, size/UnitType and Check-in/Check-out Month under API Spec §13.13; access mode does not change their meaning or the server's matching rules.
- Public policy/help/contact information does not grant access to personal records or create SupportTickets. SUP-001 continues requiring authenticated Customer and an authorized Contract relationship.
- Public content does not introduce email OTP, password reset/change, Google login, physical-unit preassignment or financial rules excluded by this SRS.

**Ordinary Customer login:** Without a valid pending protected operation, successful login enters the authenticated Customer area. The specific landing page is a frontend design decision. Customer uses PhoneNumber under AUTH-002; Employee login retains the existing role-specific access rules and cannot assume Customer continuation.

**Authentication prompted by a protected operation:** Preserve the pending operation and non-sensitive booking selection, including FacilityId, UnitTypeId and StartMonth/EndMonth when applicable, through the existing registration/login process. This selection is not a Reservation, capacity hold or Payment. AUTH-001/002 contracts remain unchanged; no new verification stage is introduced.

After successful Customer authentication:

```text
Validate the intended operation and authenticated role
-> restore the valid non-sensitive selection
-> re-read authoritative public data and required account/resource context
-> validate current Facility/UnitType/months, ownership and business preconditions
-> allow the Customer to continue the applicable operation
-> require explicit submission of the next business command
```

Missing identity/account data is required only when the current business operation requires it. Login does not unconditionally require Identity Verification, Reservation confirmation, Deposit or Visit scheduling, and MUST NOT automatically replay RES-001/RES-004/PAY-001 or another mutation. A stale selection/current-state conflict must be explained and current valid options re-evaluated before submission.

### 14.15.1 CCCD completion within reservation review (OD-09)

Guest starts a protected reservation operation -> preserve valid selection -> AUTH-001/002 as needed -> enter the same authenticated Customer flow -> AUTH-004 own-profile read -> if CCCD is missing or does not match OD-10, offer in-flow entry -> AUTH-005 profile save -> AUTH-004 read/revalidation -> restore reservation review -> explicit RES-001 confirmation. A Customer already signed in starts at the own-profile check; there is no second Guest-specific booking flow.

The completion step should be a focused popup within the reservation flow, or an internal step when reservation review is already a dialog; avoid stacked dialogs and forced manual navigation to/from Settings. Explain "Bổ sung số CCCD để tiếp tục đặt kho." "Lưu và tiếp tục" saves CCCD and resumes review; it does not submit RES-001. "Để sau" closes the completion step with no booking writes, leaving the current review/selection available until abandoned. Show loading, field validation (including existing duplicate/length handling), API unavailable/error and safe retry states; do not show an unknown/malformed profile read as confirmed missing CCCD or allow reservation creation on that basis. A failed/uncertain save must be re-read safely, not falsely reported as successful.

Account Settings remains available for ordinary editing. If a notification/link opens it specifically to complete a pending operation, preserve a safe return context and return automatically to that operation after success. Saving Settings opened from the normal menu does not force a return to Reservation. After completion, re-read price, full-period availability and account/resource preconditions; explain stale selections and require review before any command. No capacity or price is guaranteed merely by retaining a selection.

### 14.15.2 Interrupted-operation return contract (OD-09)

- Preserve the operation kind, approved non-sensitive selection/resource references and safe internal destination or next valid step across system-requested intermediate steps (currently Customer login/registration and CCCD completion). A URL alone is not sufficient to restore the business context; this does not introduce a server draft/session entity or grant a new protected operation.
- Successful completion resumes the interrupted operation, not an unrelated Overview/default page. Restoring a page/review is navigation only; never automatically replay a Reservation, confirmation, Payment, Visit or other mutation. Resume only after role, ownership and current state are checked again by their authoritative APIs.
- Do not treat all navigation as a detour: ordinary menu/Settings visits have no forced return. Cancellation of a required intermediate step creates no business record. Explicit abandonment, logout, account switch, completion, or invalid return context clears/invalidates continuation as appropriate; retain no private context for another account. Closing the CCCD step with "Để sau" does not itself submit or discard the still-open reservation review.
- Accept only registered, authorized internal destinations and allowlisted non-sensitive parameters; reject external URLs, privileged/unknown routes and client scope overrides. Do not persist authoritative amounts, capacity, Customer profiles or CCCD in continuation. Do not expand external payment-return behavior or define arbitrary nested workflow stacks by inference.

Continuation MUST NOT accept an external/privileged destination, treat a client CustomerId or stored price/capacity as authority, or expose another account's information. Resource-reference paths/IDs already defined by the API and protected route groups remain permitted, but server role/ownership/state checks are mandatory; a reference is not proof of access.

Password/JWT/CCCD/document/payment secrets and raw personal-resource payloads MUST NOT be placed in a continuation URL or selection record. Selection/intent retention must not survive completion, abandonment, logout or account change in a way that causes cross-account reuse. Its storage and presentation mechanism is owned by frontend implementation; no server session/draft table, persisted Booking entity or new auth response field is required.

### 14.15.3 OD-09 acceptance and traceability (required; not test results)

| Test ID | Required result |
|---|---|
| `API-CCCD-001` | Registration/login/public browsing without CCCD succeed under existing rules; own AUTH-004 includes cccd null/string; Employee/public responses do not acquire it. |
| `API-CCCD-002` | Direct authenticated RES-001 with missing/blank saved CCCD -> 400 VALIDATION_ERROR + errors.cccd, traceId, no Reservation/hold/Invoice; valid saved CCCD still requires all other reservation checks. |
| `API-CCCD-003` | CCCD-only AUTH-005 preserves omitted name/Address fields; OD-10 exact-12-digit input and existing unique/clear/omit semantics apply; varchar(20) remains physical storage capacity. Save + own read exposes the accepted value; no automatic Reservation. |
| `SEC-CCCD-001` | Guest/wrong-role/inactive/cross-account calls remain denied; no CCCD in continuation/URL/logs/public/Employee projections; account switching and invalid/external return paths cannot reuse another account's context. |
| `DBT-CCCD-001` | Rejected creation leaves business tables/capacity unchanged; CCCD remains nullable/unique; no LegalName, fabricated backfill or rewrite/cancellation of old rentals. |
| `DBT-CCCD-002` | Concurrent profile clear/update and RES-001 cannot bypass the authoritative creation precondition or leave partial Reservation/Invoice state; existing capacity concurrency guarantees remain. |
| `API-CCCD-004` | OD-10 AUTH-001/005/RES-001 format and field-error parity: optional null versus exactly 12 ASCII digits; invalid/duplicate -> 400 VALIDATION_ERROR + errors.cccd; legacy and leading-zero behavior. |
| `DBT-CCCD-003` | OD-10 new-Reservation format guard is atomic with capacity/Invoice creation; invalid/legacy IDs leave no writes; uniqueness and historical rows preserved; BE/DB tests pending. |
| `REG-CONT-002` | OD-10 shared FE validation across register/Settings/completion; invalid stored value requests correction, no auto-submit or private browser persistence. |
| `REG-CONT-001` | Guest login/registration -> same Customer review -> optional CCCD completion -> correct return; signed-in flow also works. Late/failed/unavailable saves, cancel/back/refresh, normal Settings, abandonment/logout/account switch and stale availability preserve safe behavior without auto-submit. |
| `E2E-F01` | Demonstrate CCCD missing first booking and already-saved subsequent booking, saved non-sensitive selection, explicit confirmation and unchanged Deposit/handover behavior. |

Traceability: CWP-03/GWP-05 + OD-09 -> BR-RES-09 / BR-PUB-05 -> AUTH-001/002/004/005 + RES-001 -> own-profile/authentication and Reservation Business Services -> repositories + existing atomic usp_CreateReservation / Customer.CCCD -> the tests above -> V11-G14. Implementation changes follow Controller -> Service -> Repository -> EF/routine; the FE is not an authority for profile completeness or capacity.

## 14.16 Public Discovery Acceptance

- For the same filters/data snapshot, Anonymous and Customer receive the same public Facility/UnitType/price/capacity/policy fields; authentication does not add personal fields to the public DTO.
- Public discovery, selection and requesting authentication produce zero Reservation/Invoice/Payment/Visit/Contract writes and no capacity hold. Atomic RES-001 rechecks capacity after the Customer submits.
- A complete search returns only Facilities with a single matching UnitType available in every inclusive requested month; non-matching UnitTypes are excluded from search-result expansions.
- Size Guide can lead to filtered search. AI failure does not block manual selection; optional AI has no Release 1 completion claim unless implemented/tested.
- Ordinary Customer login enters the authenticated Customer area; authentication during a pending operation preserves and revalidates the selection before continuation, without exposing another account's data or submitting a command automatically.
- OD-09 CCCD completion returns to the interrupted review/next valid step, with no forced return for ordinary Settings navigation; missing CCCD on new RES-001 is rejected server-side using 400 VALIDATION_ERROR + errors.cccd. See §14.15.3 for required positive, negative, persistence/concurrency and continuation coverage.
- Personal lists/detail/payments/support remain server-protected even when their URL or API is called directly by Anonymous.

# 15. External Integration and System Services


## 15.1 payOS Payment Processing

Deposit and Rental Fee invoices strictly after Contract.StartMonth only. No first-month Invoice/Payment.
payOS supports individual/household-business onboarding and has no sandbox/staging; official API is production.
This phase uses mocks/fixtures only: no real links, /confirm-webhook registration or transfers; live E2E requires separate owner approval.
Staff/Manager cannot manually mark Invoice paid.

## 15.2 payOS Webhook Processing

See PAY-004 for signed JSON validation, UTC+7 normalization and acknowledgement semantics.
Use ProviderOrderCode to resolve the Payment, reference for gateway-result idempotency, verified amount and timestamp, and authoritative usp_ApplyPaymentResult.
Late success on CANCELLED Invoice records Payment SUCCESS with LATE_SUCCESS_ON_CANCELLED_INVOICE audit, leaves Invoice CANCELLED, and never revives Reservation/Contract or executes refund.
Duplicate delivery, second legitimate success and terminal/reference conflicts preserve existing atomic SQL behavior and Invoice.PaidAt first-transition semantics.

## 15.3 Notification Processing — pending OD-06 replacement

Proposed in-app path: actual successful business event + eligible Customer notice commit together -> own-Customer GET/Overview -> render createdAt. No SMTP call, delivery worker, SENT state or SentAt timestamp gates this path. Read/Refresh never queues, sends, marks read or changes business state.

Existing PENDING -> SENT and retry-on-failure describe legacy email delivery only. Preserve those historical facts; do not fake send success. NOT-D01/02 must resolve eligible data, legacy visibility and worker exclusion before implementation. Email behavior of registration/Employee provisioning remains unchanged by this Notifications-only revision.

## 15.4 Scheduled Workers / Jobs

Required logical jobs:

- expire unpaid Deposit Reservation;
- process reservation handover no-show;
- generate recurring Rental Fee Invoice;
- mark overdue Invoice;
- recalculate open LateFee;
- retry pending notification.

Time-based rules use captured Policy where applicable.

## 15.5 AI Size Guide

Optional shared Anonymous/Customer capability in Size Guide. It remains non-authoritative and not a Release 1 blocker.

AI receives only public offered UnitType metadata and the description/mode submitted for the request, not Customer profile/Contract/Visit/payment history, and may return:

- recommended UnitType;
- short reason;
- alternatives.

AI MUST NOT:

- create or confirm Reservation;
- decide authoritative capacity;
- calculate authoritative price/Deposit/Discount/Rental Fee/LateFee;
- bypass deterministic Reservation validation.

AI service failure MUST NOT block manual browsing/selection.

## 15.6 AI Failure Isolation

AI integration is non-authoritative and must be isolated behind `IAiRecommendationProvider` or equivalent interface.

Failure behavior:

```text
AI unavailable
-> return controlled recommendation-unavailable result
-> Anonymous/Customer may continue normal UnitType selection
```

## 15.7 Employee Initial-Credential Email

`IEmailService` delivers Employee initial credentials.

Requirements:

- email provider credentials are secure configuration;
- credential email body MUST NOT be persisted in NotificationLog;
- credential email body MUST NOT be logged;
- provider result is reduced to provisioning result (`SENT` / `FAILED`);
- retry generates a new initial password and invalidates the previous hash;
- credential email is separate from ordinary retryable notification content because it contains sensitive authentication material.

# 16. Cross-Cutting Concerns


## 16.1 Global Exception Middleware

All unhandled HTTP-request exceptions MUST flow through one Global Exception Middleware.

Controllers MUST NOT duplicate generic global `try/catch` behavior. Localized exception handling is permitted only when the Controller can meaningfully transform a transport-specific condition without owning business logic.

Middleware responsibilities:

```text
catch exception
-> obtain traceId
-> classify known business/application exception
-> log appropriate level
-> map stable code + HTTP status
-> return sanitized API error
```

## 16.2 ILogger<T>

`ILogger<T>` is the technical/application logging abstraction.

Use cases:

- unexpected exception;
- provider/infrastructure failure;
- scheduled-job failure;
- business conflict diagnostics where useful;
- transaction rollback diagnostics;
- security diagnostics for login identifiers that do not resolve to a UserAccount;
- operational diagnostics.

Logging severity baseline:

```text
Information -> normal significant operation
Warning     -> expected business conflict / retry / recoverable anomaly
Error       -> unexpected failure / provider/infrastructure failure
Critical    -> catastrophic application-level failure only
```

## 16.3 Exception Response Safety

API MUST NOT expose:

- stack trace;
- raw SQL exception;
- database connection string;
- JWT signing key;
- provider secret;
- plaintext password or password hash;
- internal implementation details that reveal sensitive information.

Unexpected failures return:

```json
{
  "code": "INTERNAL_SERVER_ERROR",
  "message": "An unexpected error occurred.",
  "traceId": "..."
}
```

## 16.4 Correlation / TraceId

Every API error response MUST expose `traceId`.

Technical logs for the same request MUST include the correlatable trace identifier when the logging infrastructure supports request scope.

Correlation goal:

```text
FE error
-> API request
-> Controller
-> Service
-> Repository
-> DB/provider failure
```

## 16.5 AuditLog

`AuditLog` records business/security actions, not generic technical diagnostics.

Minimum audited event families include:

```text
CREATE / CONFIRM / CANCEL RESERVATION
CHECK_IN / CHECK_OUT / CANCEL VISIT
COMPLETE_HANDOVER
CREATE_RENTAL_INVOICE
PAYMENT_RESULT
MARK_INVOICE_OVERDUE
RENEW_CONTRACT
CONFIRM_ACTUAL_RETURN
CLAIM / COMPLETE INSPECTION
RECORD_DAMAGE
RECORD_EXTRA_FEE
FINALIZE_RETURN
CREATE_POLICY_VERSION
UPDATE_UNIT_PRICE
CREATE_OR_UPDATE_DISCOUNT
ASSIGN / COMPLETE SUPPORT_TICKET
CHANGE_ACCOUNT_STATUS
CHANGE_EMPLOYEE_ROLE_FACILITY
```

Audit payload MUST exclude secrets/binary evidence and SHOULD include only business-relevant changed fields.

## 16.6 LoginHistory

`LoginHistory.UserAccountId` is non-null in the current Data Dictionary.

Therefore:

```text
IF login identifier resolves to a UserAccount
    append LoginHistory(SUCCESS or FAILED)

ELSE
    do NOT fabricate a UserAccountId
    write only a minimized technical/security event (for example masked/hashed identifier context), not a fabricated UserAccountId
```

LoginHistory remains append-only.

Any future requirement to persist unknown-account login attempts in LoginHistory requires a schema revision (for example nullable UserAccountId or a separate attempted-identifier field) and is outside the current Release 1 baseline.

## 16.7 Sensitive Logging Rules

Never log:

```text
Password
Employee initial plaintext password / credential email body
PasswordHash
JWT access token
JWT signing key
refresh token (if introduced in future)
database credential
payOS ApiKey/ChecksumKey
AI/provider secret
InspectionEvidence.FileData
```

These values MUST NOT appear in AuditLog old/new snapshots either.

## 16.8 Retry / Idempotency

`SOURCE-LOCKED`

| Operation | Idempotency/invariant | Repeat behavior |
|---|---|---|
| payOS webhook | `orderCode` / `reference` | no duplicate Payment/Invoice effect |
| Confirm Reservation | one Reservation + one RESERVATION Visit | no second Visit |
| Complete Handover | unique `Contract.ReservationId` + terminal state | return existing outcome or controlled already-completed conflict |
| Monthly Invoice | unique Contract + BillingMonth | reuse/reject duplicate logical Invoice |
| LateFee calculation | unique LateFee per Invoice | recalculate/update same row |
| Renewal | current EndMonth + extension invariant | no duplicate ContractExtension |
| Confirm Actual Return | return state + Inspection invariant | no duplicate Inspection |
| Finalize Return | unique DepositSettlement per Contract | one terminal transition |
| Return Unit release | terminal Contract/settlement + locked Unit state | same finalization outcome; retry cannot retarget a released Unit |
| Notification retry | PENDING/SENT terminal semantics | SENT is not sent again by the retry job |

Retry handlers MUST distinguish:

```text
safe transport retry
vs
new business action
```

A retried request MUST NOT create a second logical transaction merely because HTTP transport repeated.

## 16.9 Configuration

Configuration MUST support:

```text
Development
Testing
Production
```

Environment/configuration includes:

- PostgreSQL connection;
- JWT signing settings;
- BCrypt work factor;
- payOS settings;
- AI provider settings if enabled;
- notification settings;
- email provider / `IEmailService` settings;
- scheduler settings;
- logging levels.

Secrets MUST be injected through secure configuration/environment mechanisms and MUST NOT be committed to source control.

## 16.10 IClock / Deterministic Time

Application business logic MUST obtain current time through `IClock` or an equivalent centralized abstraction.

Database procedures MAY accept test-controlled `@NowUtc` where needed.

Production source of time is UTC.

Business calendar conversion:

```text
UTC
-> Asia/Ho_Chi_Minh (GMT+7)
-> evaluate day/month business rule
```

# 17. Core Pseudocode Reference


## 17.1 Pseudocode Rule

Business algorithms use pseudocode, not implementation C#.

## 17.2 Customer Authentication

```text
FUNCTION CustomerLogin(phoneNumber, password):

    account = FindUserAccountByPhoneNumber(phoneNumber)

    IF account does not exist
        LogSecurityFailureWithoutUserAccountId(MaskOrHash(phoneNumber))
        FAIL AUTH_INVALID_CREDENTIALS

    IF account.Role != CUSTOMER OR account.Status != ACTIVE
        Append LoginHistory(FAILED)
        FAIL appropriate authentication/account error

    IF NOT PasswordHasher.Verify(password, account.PasswordHash)
        Append LoginHistory(FAILED)
        FAIL AUTH_INVALID_CREDENTIALS

    Append LoginHistory(SUCCESS)
    RETURN TokenService.Generate(account)
```

## 17.3 Employee Authentication

```text
FUNCTION EmployeeLogin(email, password):

    account = FindUserAccountByEmail(email)

    IF account does not exist
        LogSecurityFailureWithoutUserAccountId(MaskOrHash(email))
        FAIL AUTH_INVALID_CREDENTIALS

    IF account.Role NOT IN employee roles OR account.Status != ACTIVE
        Append LoginHistory(FAILED)
        FAIL appropriate authentication/account error

    IF NOT PasswordHasher.Verify(password, account.PasswordHash)
        Append LoginHistory(FAILED)
        FAIL AUTH_INVALID_CREDENTIALS

    Append LoginHistory(SUCCESS)
    RETURN TokenService.Generate(account)
```

## 17.4 Create Reservation

```text
FUNCTION CreateReservation(customer, facilityId, unitTypeId, startMonth, endMonth):

    REQUIRE customer account ACTIVE
    REQUIRE facility ACTIVE
    REQUIRE startMonth > CurrentBusinessMonth
    REQUIRE endMonth >= startMonth

    BEGIN AUTHORITATIVE DB TRANSACTION

        policy = current ACTIVE Policy
        price = current UnitType.RentalPrice

        capacity = CheckCapacityForEveryMonth(
            facilityId,
            unitTypeId,
            startMonth,
            endMonth,
            with concurrency protection
        )

        REQUIRE capacity available for ALL months

        create Reservation(
            PolicyId = policy.Id,
            LockedRentalPrice = price,
            DepositAmount = price,
            Status = PENDING_DEPOSIT
        )

        create DEPOSIT Invoice(
            BaseAmount = price,
            DiscountAmount = 0,
            AmountDue = price
        )

    COMMIT

    RETURN Reservation
```

## 17.5 Confirm Reservation

```text
FUNCTION ConfirmReservation(reservationId, reservationVisitDate):

    load Reservation + captured Policy
    REQUIRE Reservation.Status == PENDING_DEPOSIT
    REQUIRE Deposit Invoice == PAID
    REQUIRE visit date inside captured Policy window

    BEGIN TRANSACTION
        create one RESERVATION Visit(SCHEDULED)
        Reservation.Status = CONFIRMED
    COMMIT
```

## 17.6 Complete Handover

```text
FUNCTION CompleteHandover(reservationId, visitId, storageUnitId, discountId):

    REQUIRE authenticated ACTIVE Facility Staff
    // This Staff command acknowledges full offline first-month receipt
    // of Reservation.LockedRentalPrice and completion of handover.
    // It does not request a Payment/Invoice or verify physical cash.

    BEGIN AUTHORITATIVE DB TRANSACTION

        load Reservation / Visit / StorageUnit with required locks

        REQUIRE Staff belongs to Reservation.FacilityId
        REQUIRE Reservation.Status == CONFIRMED
        REQUIRE Visit.VisitType == RESERVATION
        REQUIRE Visit.EntityId == Reservation.ReservationId
        REQUIRE Visit.Status == CHECKED_IN
        REQUIRE StorageUnit.Status == AVAILABLE
        REQUIRE StorageUnit.FacilityId == Reservation.FacilityId
        REQUIRE StorageUnit.UnitTypeId == Reservation.UnitTypeId
        REQUIRE one Contract does not already exist for Reservation

        validate Contract Discount if present

        create Contract(
            Status = ACTIVE,
            PolicyId = Reservation.PolicyId,
            DiscountId = validated discountId or NULL
        )
        // No first-month Invoice, Payment, paid flag or receipt entity.

        StorageUnit.Status = IN_USE
        Reservation.Status = COMPLETED
        Visit.Status = CHECKED_OUT

    COMMIT

    record existing handover audit according to audit requirements
    RETURN canonical OPS-004 outcome
```

Apply section 16.8 repeat behavior: return an authorized existing outcome or a controlled already-completed conflict. Do not create another Contract, first-month charge or recognized offline amount on retry.

## 17.7 Monthly Billing

```text
FUNCTION CreateMonthlyInvoice(contractId, billingMonth):

    REQUIRE Contract ACTIVE
    REQUIRE Contract.StartMonth < billingMonth <= Contract.EndMonth
    REQUIRE logical Invoice does not already exist

    IF billingMonth inside original reservation period
        baseAmount = Reservation.LockedRentalPrice
    ELSE
        baseAmount = ContractExtension.AppliedMonthlyPrice
            for extension covering billingMonth

    discountId = Contract.DiscountId when eligible
    discountAmount = CalculateContractDiscount(baseAmount)
        using captured Discount semantics
    amountDue = MAX(baseAmount - discountAmount, 0)

    create RENTAL_FEE Invoice
```

Skip Contract.StartMonth rather than creating a paid, zero-value or overdue initial Invoice. The first generated invoice may apply Contract Discount because it represents a later month. Renewal does not reset this exclusion.

## 17.8 Apply Payment Result

```text
FUNCTION ApplyPaymentResult(gatewayResult):

    REQUIRE gateway reference valid
    REQUIRE current-model Payment references an existing Invoice
    // Offline first-month collection has no Payment to process.

    IF same gateway transaction already applied
        RETURN existing logical result

    update Payment status

    IF SUCCESS and linked Invoice.Status in (UNPAID, OVERDUE)
        Invoice.Status = PAID
        Invoice.PaidAt = verified provider success timestamp normalized to UTC
    // Already PAID/CANCELLED: preserve Invoice status and PaidAt.
    // Payment.PaidAt records each legitimate successful attempt separately.
```

## 17.9 Renewal

```text
FUNCTION RenewContract(contractId, newEndMonth):

    BEGIN AUTHORITATIVE DB TRANSACTION

        contract = load ACTIVE Contract
        REQUIRE no pending RETURN Visit
        REQUIRE new period is contiguous

        check capacity for EVERY extension month
        REQUIRE capacity available

        currentPrice = current UnitType.RentalPrice

        append ContractExtension(
            OldEndMonth,
            NewEndMonth,
            AppliedMonthlyPrice = currentPrice
        )

        Contract.EndMonth = newEndMonth

    COMMIT
```

## 17.10 Confirm Actual Return

```text
FUNCTION ConfirmActualReturn(returnVisitId, actualReturnDate):

    BEGIN AUTHORITATIVE DB TRANSACTION

        visit = load RETURN Visit
        contract = load ACTIVE Contract
        unit = load occupied StorageUnit

        REQUIRE visit belongs to contract
        REQUIRE return not already confirmed

        Visit.ActualReturnDate = actualReturnDate
        classify normal vs early return

        apply valid early-return current-month rule

        StorageUnit.Status = INSPECTION
        create Inspection(PENDING)

    COMMIT
```

## 17.11 Inspection Claim

```text
FUNCTION ClaimInspection(inspectionId, staffId):

    UPDATE Inspection
    SET Status = IN_PROGRESS,
        EmployeeId = staffId
    WHERE InspectionId = inspectionId
      AND Status = PENDING

    REQUIRE affectedRows == 1
```

### 17.11.1 Complete Inspection

```text
FUNCTION CompleteInspection(inspectionId, conditionNote):

    BEGIN AUTHORITATIVE DB TRANSACTION
        REQUIRE authenticated ACTIVE same-Facility Staff
        load claimed IN_PROGRESS Inspection / related Contract / Unit
        validate existing Inspection completion prerequisites
        Inspection.ConditionNote = conditionNote
        Inspection.Status = COMPLETED
        Inspection.CompletedAt = nowUtc
        // Unit remains INSPECTION; Contract remains ACTIVE.
        // No storageUnitStatus / NextUnitStatus is stored here.
    COMMIT
```

## 17.12 Finalize Return

```text
FUNCTION FinalizeReturn(contractId, storageUnitStatus):

    BEGIN AUTHORITATIVE DB TRANSACTION

        REQUIRE authenticated ACTIVE same-Facility Staff
        validate storageUnitStatus is AVAILABLE or MAINTENANCE
        load related Contract / Inspection / Unit with transaction locks
        REQUIRE correct Contract/Inspection/Unit return linkage
        REQUIRE Contract ACTIVE and Unit INSPECTION
        REQUIRE Inspection COMPLETED
        REQUIRE return not already finalized
        REQUIRE no DamageRecord remains PENDING

        totalDeduction =
            applicable LateFee
          + applicable ExtraFee
          + approved DamageAmount

        IF EarlyReturn
            refundAmount = 0
            terminalStatus = TERMINATED
        ELSE
            refundAmount = MAX(depositPaid - totalDeduction, 0)
            terminalStatus = COMPLETED

        additionalAmountDue =
            MAX(totalDeduction - depositPaid, 0)

        create one DepositSettlement(FINALIZED)
        Contract.Status = terminalStatus
        StorageUnit.Status = storageUnitStatus
        // Settlement, terminal Contract and Unit release commit together.

    COMMIT
```

## 17.13 Policy Versioning

```text
FUNCTION CreatePolicyVersion(newValues):

    BEGIN TRANSACTION

        lock current active Policy/version sequence

        currentPolicy.Status = INACTIVE
        currentPolicy.EffectiveTo = now

        insert new Policy(
            Version = current.Version + 1,
            Status = ACTIVE,
            all required parameter values
        )

    COMMIT
```

## 17.14 Support Ticket

```text
OPEN
-> Facility Manager assigns Facility Staff
-> IN_PROGRESS
-> Staff records ResultNote
-> COMPLETED

OPEN / IN_PROGRESS
-> CANCELLED
```

Support lifecycle does not directly mutate Contract/Payment/StorageUnit state.

# 18. Non-Functional Requirements


## 18.1 Security

Mandatory baseline:

- ASP.NET Core Authentication;
- JWT Bearer;
- BCrypt;
- no ASP.NET Identity;
- server-side RBAC;
- facility isolation;
- resource ownership checks;
- inactive-account enforcement;
- global exception sanitization;
- secret/config protection;
- sensitive-log exclusion;
- stable business error codes;
- database constraints/procedures as defense-in-depth for critical invariants.

Security acceptance is primarily behavioral and test-driven, not based on UI hiding.

## 18.2 Reliability

FRMS MUST preserve business-state correctness under:

- repeated HTTP requests;
- duplicate payOS webhooks;
- scheduled-job retries;
- concurrent Reservation/Renewal/Handover/Inspection actions;
- external-provider timeout/failure;
- application exception during multi-table mutation.

Requirements:

```text
Critical mutation
-> all required changes commit
OR
-> all required changes rollback
```

Partial lifecycle state is not an acceptable success result.

Historical snapshot data MUST survive master-data changes.

Scheduled jobs MUST be safely rerunnable according to Section 16.8.

## 18.3 Transaction Integrity

Critical lifecycle transactions MUST be atomic.

Mandatory DB transaction owners include at minimum:

- `usp_CreateReservation`;
- `usp_CompleteHandover`;
- `usp_ConfirmActualReturn`;
- `usp_ClaimInspection`;
- `usp_RenewContract`;
- `usp_FinalizeReturn`;
- `usp_CreatePolicyVersion`;
- idempotent payment-result processing.

## 18.4 Concurrency Safety

The following invariants MUST hold under concurrency:

- no overbooking;
- one active Contract per StorageUnit;
- one Contract per Reservation;
- one claim winner per Inspection;
- one logical monthly Invoice per Contract/BillingMonth;
- one logical Deposit Invoice per Reservation;
- one settlement per Contract;
- one active Policy version;
- no duplicate webhook financial effect.

Concurrency correctness MUST be proven with real PostgreSQL integration tests.

## 18.5 Idempotency

Retry-sensitive operations MUST satisfy Section 16.8.

An idempotent repeat may:

- return the already-created/resulting resource; or
- return a stable controlled conflict/already-completed result;

but MUST NOT create duplicate logical effects.

## 18.6 Performance

No source-approved numeric latency SLA exists; therefore the Final SRS does not invent one.

Mandatory engineering performance requirements:

- list endpoints MUST be paginated when result sets can grow;
- default page size is 20 and maximum is 100;
- data-access queries MUST avoid known N+1 query patterns in Release 1 endpoints;
- capacity, overdue, work-list and reporting queries MUST be implemented against the Data Dictionary index strategy or an explicitly equivalent measured index strategy;
- database triggers MUST remain bounded and MUST NOT perform network calls;
- binary evidence MUST NOT be included in normal list responses;
- reporting/export operations MUST NOT load unbounded result sets into API memory;
- FE payment/status polling MUST use a bounded interval/backoff strategy and MUST stop when a terminal state is reached.

Any numeric SLA introduced later must be added as a new SRS revision or explicitly marked environment-specific.

## 18.7 Maintainability

Mandatory maintainability requirements:

- three logical layer boundaries are preserved with the Infrastructure supporting assembly;
- HTTP dependencies flow Controller -> Service -> Repository;
- application-scheduled dependencies flow Api BackgroundJob -> Service -> Repository;
- API DTOs, Business Commands/Results and persistence Entities remain separate;
- API mapping owns DTO boundary conversion;
- provider implementations remain in Infrastructure behind Business interfaces;
- architecture tests reject forbidden Controller/BackgroundJob/Service dependencies;
- external providers are behind interfaces;
- critical business state is not hidden in controllers/delegates;
- stable error codes are centralized;
- SRS/Swagger/TypeScript contracts remain synchronized;
- migrations are version-controlled;
- business constants that belong to Policy/configuration are not duplicated as magic numbers;
- C# and React/TypeScript naming conventions are followed;
- pseudocode and traceability are updated when authoritative workflow changes.

## 18.8 Auditability

The system MUST be able to determine for critical actions:

```text
who
performed what action
on which business resource
when
with what resulting state
```

Requirements:

- critical business actions listed in Section 16.5 are audited;
- system/background action may have null actor and must still identify action/resource/time;
- `AuditLog`, `LoginHistory`, `ContractExtension` are append-only;
- logs use UTC timestamps;
- technical `ILogger` and business `AuditLog` remain separate;
- secrets/binary evidence are excluded from audit snapshots;
- Administrator can query LoginHistory and AuditLog through approved APIs.

## 18.9 Availability and Graceful Degradation

No numeric uptime SLA is source-approved.

Release 1 availability behavior:

- AI Size Guide failure MUST NOT block manual browsing/reservation;
- notification delivery failure leaves NotificationLog `PENDING` for retry;
- payOS provider failure MUST NOT mark Invoice paid;
- external-provider error is returned as controlled `502/503` behavior where applicable;
- failed critical transaction MUST leave the prior consistent state;
- scheduled jobs can resume/retry without duplicate logical effects;
- application restart MUST not require manual repair of successful committed business transactions.

## 18.10 Usability

Release 1 UI MUST provide:

- loading state;
- empty state;
- validation feedback;
- clear unauthorized/forbidden result;
- not-found result;
- business-conflict result;
- unexpected-error result with trace reference where useful;
- role-appropriate navigation;
- clear current lifecycle/status display;
- disabled/unavailable action explanation where practical;
- GMT+7 display for timestamps;
- server-authoritative payment/status refresh after payOS browser return.

Frontend MUST NOT present local calculations as authoritative financial/capacity results.

## 18.11 Data Integrity

- no normal hard delete for historical core business data;
- master records use inactive status when historically referenced;
- FK delete/update defaults to NO ACTION;
- historical Policy/price/Invoice snapshots are immutable as defined;
- unique/check/filter indexes protect business invariants;
- polymorphic Visit/Invoice references are protected by SP/trigger/application validation.

## 18.12 Portability / Environment Consistency

All environments MUST be creatable from:

```text
source code
+ migrations
+ required seed
+ environment configuration
```

A manually modified developer database is not an authoritative deployment artifact.

# 19. Testing and Quality Assurance


Testing is a first-class release requirement.

## 19.1 Testing Tools

```text
NUnit      -> backend unit/integration tests
Postman    -> API functional/integration tests
Playwright -> browser E2E tests
PostgreSQL -> real database integration/concurrency tests
```

## 19.2 Testing Groups

| Group | Prefix | Target |
|---|---|---|
| TG-01 Business Unit Testing | `UT-*` | Service, validator, calculator, business rules |
| TG-02 Controller & DTO Contract Testing | `CTL-*` | Controller, request/response mapping |
| TG-03 Repository / EF Core Integration Testing | `DAL-*` | Repository, EF Core, DbContext, SP integration |
| TG-04 Database Integrity & Lifecycle Testing | `DBT-*` | Constraints, triggers, SPs, transitions |
| TG-05 Transaction / Concurrency / Idempotency Testing | `CON-*` | Races, rollback, duplicate actions |
| TG-06 Authentication / Authorization / Security Testing | `SEC-*` | BCrypt, JWT, RBAC, facility/resource scope |
| TG-07 API / Postman Testing | `API-*` | REST contracts and business responses |
| TG-08 External Integration / Background Job Testing | `INT-*`, `JOB-*` | payOS, AI, notification, scheduler |
| TG-09 Playwright End-to-End Testing | `E2E-*` | Seven business flows |
| TG-10 Regression / Acceptance Testing | `REG-*`, `ACC-*` | Whole-system release confidence |

## 19.3 Database Test Environment

Real PostgreSQL behavior is required to prove:

- PostgreSQL business routines;
- filtered unique indexes;
- PostgreSQL locking;
- triggers;
- transaction isolation;
- concurrency.

EF Core InMemory is not sufficient evidence for those behaviors.

## 19.4 Minimum Database Integration Test Matrix

| Test ID | Scenario | Expected |
|---|---|---|
| `DBT-01` | Two concurrent Reservations compete for final capacity | exactly one succeeds |
| `DBT-02` | Reservation on INACTIVE Facility | rejected |
| `DBT-03` | Confirm Reservation without paid Deposit | rejected |
| `DBT-04` | Create second RESERVATION Visit | rejected or returns existing idempotently |
| `DBT-05` | Cancel Visit after CHECKED_IN | rejected |
| `DBT-06` | Two handovers allocate same StorageUnit | exactly one succeeds |
| `DBT-07` | Contract uses another Customer's Discount | rejected |
| `DBT-08` | Duplicate Rental Invoice for Contract/BillingMonth | rejected/prevented |
| `DBT-09` | Same payOS webhook twice | no duplicate effect |
| `DBT-10` | Two Staff claim same Inspection | exactly one succeeds |
| `DBT-11` | Renewal and Reservation compete for last future capacity | no overbooking |
| `DBT-12` | Existing Contract after new Policy activation | old captured Policy remains |
| `DBT-13` | Early Return inside waive window with unpaid current Invoice | waive rule applied; no LateFee |
| `DBT-14` | Normal Return with Late + Extra + approved Damage | settlement formula correct; Inspection completion keeps Unit INSPECTION; finalization atomically completes Contract and releases Unit |
| `DBT-15` | Early Return with deductions | RefundAmount = 0; termination/settlement/Unit release atomic; failed finalization leaves Unit INSPECTION |
| `DBT-16` | Repeat FinalizeReturn | no duplicate settlement/state corruption; retry cannot change previously committed Unit target |
| `DBT-17` | UPDATE/DELETE append-only history | rejected |
| `DBT-18` | Notification send failure | remains PENDING |
| `DBT-19` | External recovery with Inspection.VisitId null | allowed when valid |
| `DBT-20` | Repeat actual-return confirmation | no duplicate Inspection; classification stable |
| `DBT-21` | Manager decides PENDING Damage | only same-Facility Manager succeeds; terminal status stored |
| `DBT-22` | Concurrent Damage decisions | exactly one terminal decision wins |
| `DBT-23` | Finalize with REJECTED Damage | rejected Damage contributes zero |
| `DBT-24` | Offline first-month handover with Contract Discount | no initial Invoice/Payment; billing skips StartMonth; first eligible later invoice applies Discount; Contract-derived first-month revenue is counted once |
| `DBT-25` | Employee credential email failure | account remains INACTIVE; no plaintext persisted/logged |

## 19.5 Critical Concurrency Scenarios

```text
CON-RES-*  Last-capacity Reservation race
CON-HO-*   Same StorageUnit handover race
CON-REN-*  Renewal vs Reservation capacity race
CON-INS-*  Inspection claim race
CON-DMG-*  Concurrent Damage approval/rejection decision
CON-PAY-*  Duplicate payOS webhook
CON-INV-*  Duplicate invoice generation
CON-RET-*  Duplicate return finalization
CON-RET-*  Handover cannot reuse a Unit before return finalization commits
```

## 19.6 Authentication / Authorization Critical Scenarios

```text
Customer phone login
Employee email login
Email uniqueness
Phone uniqueness
Wrong password
Inactive account
Invalid JWT
Wrong role
Wrong Facility
Wrong Customer ownership
Same-Facility Staff can read different Customers' Reservation/Contract/Visit details
Cross-Facility Staff detail/work-item access denied, including modified URL IDs
Customer remains unable to read another Customer's operational details
Inactive Staff or changed Facility assignment cannot use stale JWT scope
OPS-001 Visit IDs/type mapping and minimal Customer identification match authoritative data
Reservation locked rent remains unchanged after UnitType price changes
Successful OPS-004 Contract can be read through CON-002 without initial Invoice/Payment
Same-Facility Manager lists/details and BOM system-wide operational reads are authorized
Wrong-Facility Manager and cross-Customer catalogue/detail requests are denied
Collection filtering/counting precedes pagination and never leaks other Facilities
Staff/Manager Invoice/Payment GET access cannot initiate payOS or manually mark PAID
Customer/Staff/Manager Discount reads preserve ownership/Facility and immutable captured semantics
Staff/Manager ExtraFeeType reads expose ACTIVE deployment rows only; write roles unchanged
Staff cannot approve/reject Damage
Wrong-Facility Manager cannot decide Damage
Employee initial password not returned/logged
Credential email failure keeps Employee INACTIVE
PasswordHash not exposed
Sensitive data not logged
```

## 19.7 E2E Business Journeys

```text
E2E-F01 Reservation
E2E-F02 Check-in & Handover
E2E-F03 Rented Storage / Access Visit
E2E-F04 Business Rules / Fee / Revenue
E2E-F05 Facility / Staff Management
E2E-F06 Renewal / Overdue / Return
E2E-F07 Support
```

E2E-F01 MUST cover Guest discovery -> protected action -> existing registration/login -> resumed Customer Reservation/Deposit/Visit flow, plus ordinary login with no automatic booking/payment action. These are variants of Flow 1, not E2E-F08 or an eighth business flow; specific pages and visual arrangements are not acceptance requirements of this revision.

## 19.8 Test Data

Test data MUST support:

- all five roles;
- at least two Facilities for scope/isolation tests;
- multiple UnitTypes;
- anonymous sessions and two Customer accounts with different private resources but the same public projection;
- matching/non-matching location/mode/size/UnitType combinations, zero capacity in an intermediate month and positive capacity in every requested month;
- a UnitType offered at multiple ACTIVE Facilities, and an unoffered/inactive-Facility-only type for public non-disclosure;
- capacity = 1 scenario for race tests;
- ACTIVE/INACTIVE Facility;
- active and historical Policy versions;
- paid/unpaid/overdue invoices;
- active Contract;
- pending RETURN Visit;
- PENDING Inspection;
- valid/invalid Discount ownership;
- production-seeded DamageType values: LOCK_DAMAGE, DOOR_DAMAGE, WALL_DAMAGE, FLOOR_DAMAGE, WATER_DAMAGE, OTHER;
- fixed DD-09 SupportTicket categories;
- normal and early return cases.

## 19.9 Test Naming Convention

Baseline:

```text
<Prefix>-<Domain>-<Sequence>
```

Examples:

```text
UT-RES-001
CTL-AUTH-001
DAL-INV-001
DBT-09
CON-HO-001
SEC-FAC-001
API-RES-001
JOB-LATE-001
E2E-F06
REG-CORE-001
```

## 19.10 Testing Definition of Done

A mandatory requirement is not complete until its required automated tests pass.

Critical transaction/concurrency rules require integration/concurrency proof, not only unit tests.

## 19.11 Public Guest / Continuation Required Scenarios

These are required tests for the revised implementation, not evidence that they have already run. Existing protected-path and SQL/concurrency gates remain unchanged.

| Test ID | Scenario | Expected |
|---|---|---|
| `API-PUB-001` | CAT-001..005 without JWT | Public allowlist succeeds with documented envelopes; public detail hides INACTIVE/unoffered resources |
| `API-PUB-002` | Full search location + mode + size/type + month range | Facility matches only if the same UnitType satisfies every filter and every month; result expansion excludes non-matches |
| `API-PUB-003` | Intermediate month has zero capacity; alternatives cover different months/types | Exclude unavailable UnitType; do not sum/swap different types/months to fabricate one valid result |
| `API-PUB-004` | Filtered pagination/count/sort; repeated multi-Facility UnitType | Filter before page/count; stable order; CAT-005 deduplicates; valid no-match -> empty list |
| `API-PUB-005` | Invalid/missing paired months, mode/UUID/sort/page or availableOnly | Documented 400 stable error/traceId; no business writes |
| `API-PUB-006` | Four current public policy topics in approved order; detail, removed/unknown/unpublished topic, no ACTIVE Policy | Current metadata and approved content; missing -> documented 404; no history/customer/BOM fields |
| `SEC-PUB-001` | Same public request by Anonymous, Customer A and Customer B | Equal identity-independent fields for same data snapshot; no Discount/private personalization |
| `SEC-PUB-002` | Anonymous private lists/detail/create/payment/support/profile, including modified URLs/IDs | 401 and no personal data/counts/business mutation; webhook still requires provider validation |
| `SEC-PUB-003` | Anonymous operational view; wrong-role/wrong-Facility employee operational view | 401/403 as applicable; no public fallback or role/Facility bypass; default public returns only public fields |
| `SEC-PUB-004` | Continuation URL tampering, other role, logout/account change | No external/privileged redirect, no cross-account data/draft reuse and no replayed mutation |
| `DAL-PUB-001` | Multi-month query, public offering and matching Facility pagination against real PostgreSQL | Same authoritative capacity/eligibility as reservation logic; no N+1 per Facility/month and no capacity hold |
| `REG-PUB-001` | Normal vs interrupted login; register/login/back/refresh; stale capacity/price | Correct destination/selection; explicit submission and server revalidation; no unconditional identity/payment gate |
| `ACC-PUB-001` | Public capability/access equivalence and sizing/process/help information | Anonymous and Customer can obtain the same public information and use the same search inputs; explanations match SRS semantics; private actions remain protected |
| `INT-AI-PUB-001` | Optional anonymous AI success/failure/hidden UnitType suggestion | Public offered types only; no personal/provider secrets; controlled failure keeps manual search usable |

UT/CTL filter/DTO tests supplement API-PUB/DAL-PUB. E2E-F01 covers the above booking-entry variants; rental-process/sizing/help information and anonymous isolation also receive functional acceptance/regression coverage. Optional AI tests are required when that capability is delivered, without promoting it to a blocker. Visual comparisons and implementation-specific page/component tests belong to the separate frontend design work, not this Guest requirements acceptance catalogue.

# 20. Requirements Traceability


## 20.1 Traceability Chain

```text
Feature
-> Business Rule / Calculation
-> API or background execution surface
-> Service
-> Repository / Stored Procedure
-> Test Case
-> Release Gate
```

## 20.2 Final Feature Traceability Matrix

Ownership notation is `FE / BE` where both apply; `System / BE` for internal services.

| Feature | Name | Owner | API / Execution | Main Rule(s) | Required Test Families | R1 Blocker |
|---|---|---|---|---|---|---|
| `GWP-01` | Shared Public Facility / UnitType Search | Delivery owner assigned by project; shares CWP-01 platform | CAT-001..004 | BR-PUB-01..04/07; CALC-CAP-01 | API-PUB-001..005; SEC-PUB-001..003; DAL-PUB-001; E2E-F01 | Yes |
| `GWP-02` | Storage Sizing Guidance | Delivery owner assigned by project; shares CWP-01/02 platform | CAT-004..005; optional AI-001 | BR-PUB-01/02/04; AI non-authoritative rules | API-PUB; SEC-PUB; ACC-PUB-001; optional INT-AI-PUB-001 | Yes for manual guide; AI optional |
| `GWP-03` | Public How It Works | Delivery owner assigned by project | Approved static content; no new API | BR-PUB-04; existing seven-flow/financial semantics | ACC-PUB-001; browser content/regression | Yes |
| `GWP-04` | Public Help / Policy / Contact | Delivery owner assigned by project | PUB-POL-001..002; approved static contact | BR-PUB-06/07; BR-POL-01..02; BR-SUP-01 | API-PUB-006; SEC-PUB-001..002; ACC-PUB-001 | Yes |
| `GWP-05` | Guest-to-Customer / prerequisite continuation | Delivery owner assigned by project; shares AUTH/CWP-03 | AUTH-001/002/004/005; safe return to existing protected destination (OD-09) | BR-PUB-04/05/07; BR-RES-03/09 | SEC-PUB-004; REG-PUB-001; REG-CONT-001; SEC-CCCD-001; E2E-F01 | Yes |
| `CWP-01` | Facility & Unit Type Browsing | Triển / Long | CAT-001..005; common public data contracts | BR-FAC-01; BR-PUB-01..04; CALC-CAP-01 | API-CAT/PUB, SEC-PUB, DAL-PUB, E2E-F01 | Yes |
| `CWP-02` | Shared Optional AI Size Guide Recommendation | Triển / Giang | AI-001 | Public-only inputs/types; AI non-authoritative rules | INT-AI, API-AI, INT-AI-PUB-001 | No |
| `CWP-03` | Storage Reservation | Triển / Khoa | RES-001 + AUTH-004/005 own CCCD completion (OD-09) | BR-RES-01..05/09; BR-PUB-05 | UT-RES, DBT-01/02, CON-RES, API-RES, API-CCCD-001..003, DBT-CCCD-001/002, SEC-CCCD-001, REG-CONT-001, E2E-F01 | Yes |
| `CWP-04` | Reservation & Deposit Management | Triển / Khoa+Giang | RES-002..005, BIL-001..002, PAY-001, PAY-003 | BR-RES-06..08; BR-PAY-01 | UT/API/DBT/E2E-F01 | Yes |
| `CWP-05` | Reservation Visit Management | Triển / Khoa | RES-004, VIS-003..006 | BR-RES-07; BR-VIS-01..02 | UT-VIS, API-VIS, E2E-F01/F02 | Yes |
| `CWP-06` | Rental Management | Triển / Khoa | CON-001..002, CON-004..005 | BR-CON-* | API-CON, SEC, E2E-F03/F06 | Yes |
| `CWP-07` | Access Visit Management | Triển / Khoa | VIS-001, VIS-003..006 | BR-VIS-01..04 | UT/API/E2E-F03 | Yes |
| `CWP-08` | Rental Payment (second month onward) | Triển / Giang | BIL-*, PAY-001, PAY-003, CON-004 | BR-BIL-*; BR-PAY-* | UT-PAY, DBT-08/09, CON-PAY, API-PAY | Yes |
| `CWP-09` | Contract Renewal | Triển / Khoa | CON-003 | BR-REN-01..04 | UT-REN, DBT-11, CON-REN, API-REN, E2E-F06 | Yes |
| `CWP-10` | Storage Return Visit | Triển / Giang | VIS-002..006, CON-005 | BR-RET-01..02 | UT-RET, API-RET, E2E-F06 | Yes |
| `CWP-11` | Fee & Return Tracking | Triển / Giang | CON-004..005, BIL-* | BR-LATE-*; BR-SET-* | UT/API/E2E-F06 | Yes |
| `CWP-12` | Customer Support | Triển / Giang | SUP-001..004 | BR-SUP-01 | UT-SUP, API-SUP, E2E-F07 | Yes |
| `FWP-01` | Daily Work List | Triển / Long | OPS-001; VIS-004/RES-003/CON-001/CON-002 navigation | Derived Visits/Inspections/Tickets; same-Facility Contract list/detail read only (OD-08) | DAL/API/SEC | Yes |
| `FWP-02` | Reservation Check-in | Triển / Khoa | OPS-001, RES-003, VIS-004, OPS-002 | BR-VIS-*; same-Facility read | UT/API/SEC/E2E-F02 | Yes |
| `FWP-03` | Offline-receipt acknowledgment & Handover Processing | Triển / Khoa | RES-003, VIS-004, BOM-010, OPS-004, CON-002 | BR-HO-01..03; BR-BIL-06; CALC-FIRST-01; immutable rental read | DBT-06/24, CON-HO, API-HO, SEC, E2E-F02 | Yes |
| `FWP-04` | Access Visit Processing | Triển / Khoa | OPS-002..003 | BR-VIS-* | UT/API/E2E-F03 | Yes |
| `FWP-05` | Return Confirmation | Triển / Giang | OPS-002, OPS-005 | BR-RET-01..02,06 | DBT-20, CON-RET, API-RET, E2E-F06 | Yes |
| `FWP-06` | Return Inspection | Triển / Giang | INS-001..003,006..007 | BR-RET-03/07; BR-DMG-01; Unit remains INSPECTION | DBT-10/14, CON-INS, API-INS, E2E-F06 | Yes |
| `FWP-07` | Return Fee Recording & Atomic Unit Release | Triển / Giang | INS-004..005, INS-008, INS-010, BOM-013; BIL-001..002, PAY-003, CON-004..005 reads | BR-DMG-01..03; BR-EXT-01; BR-SET-*; BR-RET-07 | DBT-14..16, DBT-21..23, CON-RET, API-INS, SEC, E2E-F06 | Yes |
| `FWP-08` | Support Request Processing | Triển / Giang | SUP-002..003,006 | BR-SUP-01 | UT/API/E2E-F07 | Yes |
| `MWP-01` | Handover Unit Selection | Mẫn / Khoa | UNIT-001,003; RES-002..003, VIS-003..004, BOM-010 reads; **IMPL-HOA-01..03 in API Spec §13A.1**; Staff OPS-004 | BR-HO-01; Manager same-Facility assignment only after checked-in Visit; Staff read-only active assignment; atomic consume at handover | PG constraint/concurrency, API/SEC/E2E-F02 | Yes |
| `MWP-02` | Physical Unit Management | Mẫn / Long | UNIT-001..005 | StorageUnit state rules | UT/DAL/API/SEC | Yes |
| `MWP-03` | Facility Operations Monitoring | Mẫn / Long | UNIT-001, REP-001..002; RES-002..003, CON-001..002/004..005, VIS-003..004, BIL-001..002, PAY-003 | CALC-REP-*; same-Facility reads | DAL/API/SEC | Yes |
| `MWP-04` | Return, Inspection & Damage Decision | Mẫn / Giang | INS-001..002, INS-009..010, REP-001 | BR-RET-*; BR-DMG-02..03 | API/SEC/CON-DMG/E2E-F06 | Yes |
| `MWP-05` | Support Staff Assignment | Mẫn / Giang | SUP-002..005 | BR-SUP-01 | UT/API/SEC/E2E-F07 | Yes |
| `MWP-06` | Facility Reporting | Mẫn / Giang | REP-001..002; REP-RD-01 (provisional implementation route) | CALC-REP-01/02; same-Facility read and honest current-vs-history scope | DAL/API/SEC/ACC; daily AuditLog evidence (pending) | Yes |
| `BWP-01` | Facility Management | Mẫn / Long | BOM-001..005 | BR-FAC-01 | UT/API/SEC | Yes |
| `BWP-02` | Unit Type Pricing | Mẫn / Long | BOM-006..007 | snapshot rules | UT/API/REG | Yes |
| `BWP-03` | Policy Version Management | Mẫn / Long | BOM-008..009 | BR-POL-01..02 | DBT-12, CON-POL, API | Yes |
| `BWP-04` | Discount Management | Mẫn / Long | BOM-010..012 | BR-DIS-01..04 | DBT-07, API | Yes |
| `BWP-05` | Extra Fee Management | Mẫn / Giang | BOM-013..014 | BR-EXT-01 | API/REG | Yes |
| `BWP-06` | Multi-Facility Monitoring | Mẫn / Giang | BOM-001, REP-003; RES-002..003, CON-001..002/004..005, VIS-003..004, BIL-001..002, PAY-003 | CALC-REP-*; authorized system-wide reads | DAL/API/SEC | Yes |
| `BWP-07` | Business Reporting & Export | Mẫn / Giang | REP-003..004; REP-RD-02..03 (provisional implementation routes) | CALC-REP-01/02; all tabs and filtered UTF-8 CSV parity | DAL/API/SEC/ACC; large-export bounded-memory verification (pending) | Yes |
| `AWP-01` | User Account Monitoring | Mẫn / Long | ADM-001..002 | BR-ACC-* | API/SEC | Yes |
| `AWP-02` | Customer Account Status Management | Mẫn / Long | ADM-003..004 | BR-ACC-02 | UT/API/DBT | Yes |
| `AWP-03` | Employee Account Management | Mẫn / Long | ADM-005..008, ADM-012 | BR-EMP-01; credential provisioning | UT/API/SEC/INT-EMAIL | Yes |
| `AWP-04` | Role & Facility Assignment | Mẫn / Long | ADM-009 | BR-EMP-01 | UT/API/SEC | Yes |
| `AWP-05` | Access Management | Mẫn / Long | Authorization policies | SSP-17 | SEC-* | Yes |
| `AWP-06` | Login History | Mẫn / Long | ADM-010 | append-only rules | DAL/API/SEC | Yes |
| `AWP-07` | Activity Log Management | Mẫn / Long | ADM-011 | BR-AUD-01 | DAL/API/SEC | Yes |
| `SSP-01` | Unit Type Catalog | System / Long | CAT-003..005, BOM-006 | public offered-catalog vs protected management rules | UT/DAL/API/SEC-PUB | Yes |
| `SSP-02` | Capacity Management | System / Khoa | CAT-001/003, RES-001, CON-003 | BR-PUB-03/04; BR-RES-02..03; BR-REN-03 | DAL-PUB-001, DBT-01/11, CON-RES/REN | Yes |
| `SSP-03` | Deposit Calculation | System / Khoa | RES-001 | BR-RES-04..05; CALC-DEP-01 | UT/DBT/API | Yes |
| `SSP-04` | Reservation Deposit Expiration | System / Khoa | job_ExpirePendingReservations | Policy timeout | JOB/DBT | Yes |
| `SSP-05` | Handover Window Expiration | System / Khoa | job_ProcessReservationNoShow | Policy visit window | JOB/DBT | Yes |
| `SSP-06` | Rental Fee Calculation | System / Giang | Billing APIs/jobs | CALC-INV-*; CALC-DIS-* | UT/DBT/API | Yes |
| `SSP-07` | Payment Result Processing | System / Giang | PAY-004 | BR-PAY-01 | DBT-09, CON-PAY, INT-PAYOS | Yes |
| `SSP-08` | Atomic Complete Handover without first-month Invoice/Payment | System / Khoa | OPS-004 | BR-HO-02..03; BR-BIL-06 | DBT-06/24, CON-HO | Yes |
| `SSP-09` | Monthly Billing Management | System / Giang | billing jobs | BR-BIL-* | DBT-08, CON-INV, JOB | Yes |
| `SSP-10` | Overdue & Late Fee Calculation | System / Giang | CON-004 + jobs | BR-BIL-05; BR-LATE-* | UT/JOB/DBT | Yes |
| `SSP-11` | Renewal Processing | System / Khoa | CON-003 | BR-REN-* | DBT-11, CON-REN | Yes |
| `SSP-12` | Return Processing | System / Giang | VIS-002, OPS-005, INS-007, INS-008 | BR-RET-* including finalization-only Unit release | DBT-13..16,20; CON-RET | Yes |
| `SSP-13` | Inspection Claim Control | System / Giang | INS-003 | BR-RET-03 | DBT-10, CON-INS | Yes |
| `SSP-14` | Deposit Settlement Calculation | System / Giang | INS-008 | BR-SET-*; CALC-SET-* | DBT-14..16 | Yes |
| `SSP-15` | Reporting Calculation | System / Giang | REP-001..004 | CALC-REP-* | UT/DAL/API | Yes |
| `SSP-16` | Policy Version Application | System / Long | BOM-008..009 | BR-POL-* | DBT-12, CON-POL | Yes |
| `SSP-17` | RBAC & Facility Authorization | System / Long | all protected endpoints | actor matrix | SEC-* | Yes |
| `SSP-18` | Actor & Activity Audit | System / Long | audit paths | BR-AUD-01 | DAL/API/REG | Yes |
| `EPS-01` | payOS Payment Processing (Deposit and later Rental Invoices) | External / Giang | PAY-001, PAY-003, PAY-004 | BR-PAY-01 | INT-PAYOS, API-PAY | Yes |
| `EPS-02` | Employee Initial Credential Email Delivery | External / Long | ADM-005, ADM-012 | DD-01 resolved workflow | INT-EMAIL, SEC, API-ADM | Yes |

## 20.3 API → Service / Stored Procedure Mapping

Critical command baseline:

| API / execution | Business service | Authoritative DB operation |
|---|---|---|
| `RES-001` | ReservationService | `usp_CreateReservation` |
| `RES-004` | ReservationService | `usp_ConfirmReservation` |
| `RES-005` | ReservationService | `usp_CancelReservation` |
| `OPS-002` | VisitService | `usp_CheckInVisit` |
| `VIS-006` | VisitService | `usp_CancelVisit` |
| `OPS-004` | HandoverService | `usp_CompleteHandover` |
| `VIS-001` | VisitService | `usp_CreateAccessVisit` |
| `VIS-002` | ReturnService | `usp_CreateReturnVisit` |
| `OPS-005` | ReturnService | `usp_ConfirmActualReturn` |
| `INS-003` | InspectionService | `usp_ClaimInspection` |
| `INS-004` | InspectionService | `usp_RecordDamage` |
| `INS-009` | InspectionService | `usp_DecideDamage` |
| `INS-005` | InspectionService | `usp_RecordExtraFee` |
| `INS-006` | InspectionService | `usp_AddInspectionEvidence` |
| `INS-007` | InspectionService | `usp_CompleteInspection` |
| `INS-008` | ReturnService | `usp_FinalizeReturn` |
| `CON-003` | RenewalService | `usp_RenewContract` |
| `PAY-004` | PaymentService | `usp_ApplyPaymentResult` |
| monthly billing job | BillingService/BackgroundJob | `usp_CreateMonthlyInvoice` / generator wrapper |
| late fee job | BillingService/BackgroundJob | `usp_CalculateLateFee` / recalculation wrapper |
| overdue job | BillingService/BackgroundJob | `usp_MarkOverdueInvoices` |
| `BOM-009` | PolicyService | `usp_CreatePolicyVersion` |
| `SUP-005` | SupportTicketService | `usp_AssignSupportTicket` |
| `SUP-006` | SupportTicketService | `usp_CompleteSupportTicket` |
| `SUP-004` | SupportTicketService | `usp_CancelSupportTicket` |
| account status commands | AccountService | `usp_SetUserAccountStatus` |
| notification background job | NotificationService/BackgroundJob | `usp_QueueNotification` / `usp_RetryPendingNotification` |

Simple read/query endpoints may use repository/EF Core without a PostgreSQL business routine.

## 20.4 API → Test Group Rule

Every API has applicable:

```text
CTL-*  request/response contract
API-*  HTTP behavior
SEC-*  if protected or public projection/continuation has a non-disclosure boundary
DAL-*  if persistence/query behavior matters
DBT-*  when DB state machine/invariant matters
CON-*  when concurrency/idempotency matters
E2E-*  when part of a core journey
```

## 20.5 Coverage Rule

Every Release 1 blocker feature MUST have:

1. at least one positive automated test;
2. at least one relevant negative/authorization test;
3. DB integration/concurrency test when it owns a critical invariant;
4. E2E coverage when part of one of the seven core flows.

A feature cannot be marked Done only because its UI is implemented.

# 21. CI / Quality Gate


## 21.1 Pull Request / Merge Gate

Before merge to the shared integration branch:

```text
[ ] Backend build passes
[ ] Frontend build passes
[ ] Changed-module unit tests pass
[ ] Required integration tests for changed critical path pass
[ ] Database migration applies successfully when schema changed
[ ] Migration rollback/redeploy strategy reviewed when applicable
[ ] Swagger updated for API change
[ ] FE TypeScript contract updated for API change
[ ] No secret committed
[ ] No SRS authority conflict introduced
```

## 21.2 Critical Path Gate

Any change touching:

```text
capacity
payment
handover
renewal
inspection claim
return finalization
policy version
account authorization
```

MUST run the corresponding DBT/CON/SEC tests before merge.

## 21.3 Core Demo Gate

Core Demo requires:

```text
[ ] Seven E2E flows pass in demo environment
[ ] Critical DBT/CON tests pass
[ ] Role/Facility isolation tests pass
[ ] Seed/demo data reset process works
[x] approved live payOS Deposit scenario works (limited evidence in section 0.0.5)
[ ] No known blocker corrupts lifecycle state
```

## 21.4 Release 1 Gate

```text
[ ] Backend build passes
[ ] Frontend build passes
[ ] Unit suite passes
[ ] Controller/DTO contract suite passes
[ ] Repository/SQL integration suite passes
[ ] DBT-01..25 pass
[ ] Critical CON suite passes
[ ] SEC suite passes
[ ] Postman regression passes
[ ] Playwright E2E-F01..F07 pass
[ ] Scheduled-job tests pass
[ ] Migration from empty DB succeeds
[ ] Production-like seed succeeds
[ ] SRS / Swagger / FE contract synchronization verified
[ ] GWP-01..05 public discovery, privacy and login-continuation acceptance passes (AI remains optional)
[ ] No unresolved Release 1 blocker defect
[ ] Deferred Decisions are not silently implemented
```

## 21.5 Secret / Configuration Gate

Repository scan/review MUST ensure absence of committed:

- database password;
- JWT signing key;
- payOS ApiKey/ChecksumKey;
- AI provider key;
- notification credentials;
- plaintext production password.

## 21.6 Final Release Evidence

Release package MUST retain:

- test result summary;
- migration version;
- Postman collection/environment template;
- Playwright result summary;
- SRS version identifier;
- Swagger/OpenAPI snapshot;
- known deferred-decision list.

# 22. Definition of Done


## 22.1 Feature Definition of Done

A Release 1 feature is Done only when:

```text
[ ] Functional implementation complete
[ ] Correct layer boundaries preserved
[ ] Request/response DTO contract complete
[ ] API JSON example updated
[ ] Swagger synchronized
[ ] FE TypeScript model synchronized
[ ] Authorization verified
[ ] Validation exists at correct layers
[ ] Stable error handling implemented
[ ] Logging/Audit implemented where required
[ ] Required automated tests pass
[ ] Migration/seed updated if data model changed
[ ] Traceability row updated
[ ] No unapproved semantic change introduced
```

## 22.2 Phase 0 DoD

Phase 0 is Done when all Section 6.1.1 exit-gate items pass.

## 22.3 Core Demo DoD

```text
[ ] Seven flows demonstrated end-to-end
[ ] Five roles demonstrate required actions
[ ] approved live payOS Deposit + Rental Fee flows work (Deposit verified; live later Rental Fee not yet demonstrated)
[ ] Critical lifecycle transactions are atomic
[ ] Critical concurrency scenarios pass
[ ] Critical RBAC/facility/resource tests pass
[ ] Seven E2E demo journeys pass
[ ] Database can be recreated from migration + seed
[ ] No known blocker corrupts business state
```

## 22.4 Release 1 DoD

```text
[ ] All Release 1 blocker features complete
[ ] All mandatory BR/CALC rules enforced
[ ] Endpoint registry implemented or explicitly excluded by approved change
[ ] Swagger matches SRS
[ ] FE types match normative JSON
[ ] Full required test groups executed
[ ] DBT-01..25 pass
[ ] Critical concurrency/idempotency suite passes
[ ] Postman regression passes
[ ] Playwright E2E-F01..F07 pass
[ ] Scheduled jobs verified
[ ] Authentication/authorization suite passes
[ ] Logging/audit verified
[ ] CI/release gates pass
[ ] Traceability matrix complete
[ ] Documentation complete
[ ] Deferred Decisions remain explicit and non-silent
```

## 22.5 Documentation DoD

Final project documentation includes:

- this SRS;
- Data Dictionary;
- Scope;
- ERD;
- architecture diagrams;
- Swagger/OpenAPI;
- Postman collection;
- test catalogue/results;
- migration/seed guide;
- deployment/configuration guide;
- deferred-decision list.

# 23. Known Gaps and Clarifications


Section 23 records the reviewed design decisions. In V9, all currently identified Deferred Decisions DD-01..DD-18 are resolved and normative.

Any future ambiguity discovered after V9 MUST be recorded as a new SRS decision rather than resolved silently in code.

## 23.1 DD-01 — Employee Initial Credential Provisioning — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

Administrator creates the Employee account through `ADM-005`; FRMS delivers the initial password by email using `IEmailService`.

```text
Create account INACTIVE
-> generate random 16-character initial password
-> persist BCrypt hash only
-> send plaintext password by email
-> accepted send => ACTIVE
-> failed send   => remain INACTIVE
```

The plaintext initial password MUST NOT appear in API responses, application logs, AuditLog, NotificationLog, or database plaintext.

`ADM-012` retries initial provisioning for an INACTIVE Employee by generating a new password/hash and emailing the new credential.

Password reset/change remains outside Release 1; the successfully emailed initial password is the Employee's current password.

This is an explicit Release 1 security tradeoff approved by the project.

## 23.2 DD-02 — Damage Approval Actor / Workflow — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

Damage decision actor is `FACILITY_MANAGER`.

```text
FACILITY_STAFF records Damage -> PENDING
same-Facility FACILITY_MANAGER -> APPROVED or REJECTED
```

Only PENDING may transition. APPROVED/REJECTED are terminal. Decision is atomic and audited.

APPROVED DamageAmount is included in settlement; REJECTED contributes zero. Finalize Return remains blocked while any DamageRecord is PENDING.

API: `INS-009`
Procedure: `usp_DecideDamage`.

## 23.3 DD-03 — Report Export Format — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

`REP-004` exports business reports as:

```text
CSV
Content-Type: text/csv
File extension: .csv
Encoding: UTF-8
```

Rationale:

- CSV is sufficient for Release 1 tabular export.
- It introduces no spreadsheet-library dependency.
- It can be opened by Excel/Google Sheets and imported by reporting tools.
- It does not change business calculations or persisted data.

The export MUST use the same filters/calculations as the corresponding on-screen report. The reviewed implementation-alignment proposal keeps legacy `view=revenue` CSV header/row contract and adds `view=facility|unitType|status` grouped CSV using the matching BOM analysis filters. This documents the existing backend patch target **without certifying** that large exports already satisfy §18.6 (bounded-memory), concurrent-data consistency, or acceptance testing. Formula-like text cells require CSV injection defenses.

Changing Release 1 export to XLSX or another format requires only an API/UI contract revision and does not change domain semantics.

## 23.4 DD-04 — Numeric Performance / Availability SLA — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

FRMS Release 1 has **no mandatory numeric latency, throughput, concurrency-volume, or uptime SLA**.

The qualitative performance, reliability, pagination, concurrency, and graceful-degradation requirements in Section 18 remain mandatory.

Numeric performance measurements MAY be collected during testing, but they are diagnostic unless a later SRS revision explicitly promotes a value to a release criterion.

This avoids inventing unsupported contractual targets while keeping engineering performance requirements enforceable.

## 23.5 DD-05 — Exact payOS Wire Payload — RESOLVED

`DESIGN-LOCKED — V10 FINAL / REV-2026-10-09-PAYOS`
Technical sources: https://payos.vn/docs/api/, https://payos.vn/docs/du-lieu-tra-ve/webhook/, https://payos.vn/docs/tich-hop-webhook/kiem-tra-du-lieu-voi-signature/, https://payos.vn/docs/sdks/back-end/net/ (checked 2026-10-09).
Wire DTOs and signatures remain Infrastructure/API transport concerns, never Business/Repository domain contracts.
Create request signature contains exactly alphabet-sorted amount, cancelUrl, description, orderCode, returnUrl; expiredAt is sent but excluded from this signature fieldset.
Description is the owner-approved ASCII literal FRMS (4 characters; within the documented 9-character restricted-account limit). Traceability uses ProviderOrderCode -> PaymentId.
Amount/orderCode use signed 64-bit integers, matching official .NET long models and SQL BIGINT. expiredAt must fit the documented Int32 Unix timestamp range.
Validate successful create envelope/signature, orderCode/amount/currency, initial PENDING status, HTTPS checkoutUrl and usable expiry before persisting.
If response omits expiry, persist the explicitly requested server expiry; never invent a provider expiry interval.
No automatic HTTP retries of create requests; only the Created request invokes the provider.
Only verified webhook success may set financial state; SDK/provider status enums do not add persisted Payment statuses.

## 23.6 DD-06 — Password Complexity Policy — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

Release 1 uses one centralized password validation policy:

```text
Minimum length: 8 characters
Maximum length: 64 characters
Additional composition requirement: none
BCrypt input safety: encoded password MUST NOT exceed 72 bytes
```

Rules:

- FE may provide immediate validation feedback.
- BE is authoritative and MUST enforce the same policy.
- Password MUST NOT be silently trimmed.
- Password MUST NOT be normalized into a different value after the user submits it.
- PasswordHash is BCrypt with configured work factor baseline 12.

Rationale: length-based validation is simple, consistent, and avoids duplicating arbitrary composition rules across FE and BE.

## 23.7 DD-07 — Login Identifier Change Workflow — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

Release 1 does **not** provide a workflow to change authentication identifiers after account creation.

```text
Customer login identifier = PhoneNumber
Employee login identifier = Email
```

Therefore:

- `AUTH-005` MUST NOT modify Customer `PhoneNumber` or account `Email`;
- Admin employee profile update MUST NOT silently change Employee login Email;
- a login-identifier change workflow is Future Work and requires explicit verification/security rules before introduction.

This restriction does not prevent editing non-authentication profile fields.

## 23.8 DD-08 — Runtime AI Provider — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

FRMS Release 1 does not lock a specific runtime AI vendor.

Runtime AI Size Guide MUST remain behind:

```text
IAiRecommendationProvider
```

The concrete provider is selected through environment/configuration.

Requirements:

- provider choice MUST NOT change API/business semantics;
- AI remains optional and non-authoritative;
- provider failure MUST degrade to manual UnitType selection;
- provider credentials MUST remain secure configuration;
- development use of ChatGPT / Claude / Gemini does not imply runtime vendor selection.

This is considered resolved because provider-agnostic architecture is the intended Release 1 contract.

## 23.9 DD-09 — SupportTicket Category Catalogue — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

FRMS Release 1 uses a fixed SupportTicket category enum.

Allowed values:

```text
UNIT_ISSUE
LOCK_KEY_ISSUE
ACCESS_CARD_CODE_ISSUE
PAYMENT_ISSUE
STORED_ITEM_ISSUE
DAMAGE_ISSUE
OTHER
```

Rules:

- `SupportTicket.Category` MUST contain exactly one of the values above.
- `OTHER` is allowed for issues that do not fit an existing category.
- When `Category = OTHER`, `Description` MUST contain sufficient issue detail.
- The category catalogue is fixed in Release 1 and is not a configurable master-data table.
- Adding, renaming, or removing a category requires an explicit SRS revision.
- FE TypeScript enum, API validation, Swagger/OpenAPI, database CHECK constraint, Postman tests, and Playwright test data MUST use the same values.

Database requirement:

```text
CHECK SupportTicket.Category IN (
    'UNIT_ISSUE',
    'LOCK_KEY_ISSUE',
    'ACCESS_CARD_CODE_ISSUE',
    'PAYMENT_ISSUE',
    'STORED_ITEM_ISSUE',
    'DAMAGE_ISSUE',
    'OTHER'
)
```

## 23.10 DD-10 — Initial Facility Status on BOM Create — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

A newly created Facility starts as:

```text
Status = INACTIVE
```

Activation is explicit:

```text
BOM-002 Create Facility -> INACTIVE
BOM-004 Activate Facility -> ACTIVE
BOM-005 Deactivate Facility -> INACTIVE
```

Rationale:

- an incomplete Facility must not accidentally accept Reservation;
- Scope already provides explicit activate/deactivate capability;
- Facility configuration/StorageUnit preparation can occur before activation.

`BR-FAC-01` continues to block new Reservation on an INACTIVE Facility.

## 23.11 DD-11 — Direct StorageUnit Maintenance Transition — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

Facility Manager may place an unused StorageUnit into preventive maintenance.

The legal StorageUnit transition matrix is:

```text
AVAILABLE   -> IN_USE
AVAILABLE   -> MAINTENANCE
IN_USE      -> INSPECTION
INSPECTION  -> AVAILABLE
INSPECTION  -> MAINTENANCE
MAINTENANCE -> AVAILABLE
```

Rules for direct `AVAILABLE -> MAINTENANCE`:

- actor MUST be Facility Manager for the same Facility;
- StorageUnit MUST have no ACTIVE Contract;
- the unit is excluded from rentable/available capacity while `MAINTENANCE`;
- no separate Maintenance entity/workflow is created;
- returning to service uses `MAINTENANCE -> AVAILABLE`.

This resolves preventive-maintenance handling without adding a new business flow.

## 23.12 DD-12 — Contract Policy Capture at Handover — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

When `usp_CompleteHandover` creates a Contract:

```text
Contract.PolicyId = Reservation.PolicyId
```

The Contract MUST NOT capture the currently ACTIVE Policy at handover time.

Rationale:

- Reservation already captured the applicable Policy version at Reservation creation.
- Handover is the continuation of the same approved rental lifecycle.
- Copying `Reservation.PolicyId` prevents policy drift between Reservation and Contract when a new Policy version becomes active before handover.
- Historical behavior remains deterministic and consistent with `BR-POL-02`.

Normative rule:

```text
Reservation.PolicyId
        |
        v
Complete Handover
        |
        v
Contract.PolicyId = Reservation.PolicyId
```

After Contract creation, all Contract-level lifecycle procedures MUST continue using `Contract.PolicyId`.

Affected components:

- `usp_CompleteHandover`
- Contract creation mapping
- Contract API response
- Policy/lifecycle tests
- DBT-12 historical-policy test
- E2E-F02 / E2E-F06 where Policy behavior is relevant

Required test:

```text
Given:
    Reservation captured Policy V1
    Policy V2 becomes ACTIVE before handover

When:
    Complete Handover succeeds

Then:
    Contract.PolicyId == Reservation.PolicyId == Policy V1
    Contract does NOT use Policy V2
```

## 23.13 DD-13 — Offline First-Month Settlement and Contract Discount — RESOLVED

`OWNER-APPROVED — REV-2026-10-07-FM-OFFLINE`

The approved 2026-10-07 revision supersedes V9's online first-month Payment/Invoice linkage. The initial amount remains undiscounted; its representation is offline receipt acknowledged by Staff when creating Contract.

```text
First-month offline amount = Reservation.LockedRentalPrice
First-month settlement record = successful Contract creation
First-month Invoice = none
First-month Payment = none
Rental Invoice BillingMonth > Contract.StartMonth
```

Contract Discount may apply to eligible later invoices, including the first generated invoice because it represents at least the second rental month.

No pre-handover Payment, cash-payment record, paid flag, receipt entity or new Contract attribute is introduced. Authorized OPS-004 records Staff's acknowledgment through Contract creation, without gateway confirmation or another approval workflow.

Recognize the first-month amount once per Contract in Contract.StartMonth using the immutable Reservation price (section 12.4.1). Preserve historical Invoice/Payment evidence without reproducing the retired charge model or counting it twice. External commercial accommodation does not change the official snapshot-derived amount or create a FRMS adjustment/refund workflow.

## 23.14 DD-14 — Mutation of a Discount Referenced by a Contract — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

Once a Discount is referenced by any Contract, its financial/effective semantics become immutable.

The following fields MUST NOT be modified in place:

```text
Percentage
EffectiveFrom
EffectiveTo
CustomerId
```

`Status` behavior:

- `INACTIVE` prevents selection by a **new** Contract;
- changing Status to INACTIVE MUST NOT invalidate a Discount already captured by an existing Contract;
- existing Contract invoices continue using the captured Discount reference and immutable Percentage according to `BR-DIS-03/04`.

If business needs a changed percentage/effective period, create a new Discount row rather than mutate the referenced row.

Issued Invoice snapshots remain immutable.

## 23.15 DD-15 — Production DamageType Provisioning — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

Release 1 uses fixed production seed data and no DamageType CRUD workflow.

```text
LOCK_DAMAGE
DOOR_DAMAGE
WALL_DAMAGE
FLOOR_DAMAGE
WATER_DAMAGE
OTHER
```

All rows start ACTIVE. `DefaultAmount = NULL` unless approved deployment data supplies a value.

Staff selects active DamageType through `INS-010`; actual charge is stored in `DamageRecord.DamageAmount`.

Changing this catalogue requires an explicit SRS/seed revision.

## 23.16 DD-16 — Email Verification — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

FRMS Release 1 does not implement email verification / email OTP.

Rules:

```text
EmailVerifiedAt may remain NULL
EmailVerifiedAt is NOT a login precondition
```

Customer self-registration still creates an ACTIVE Customer account under the current Release 1 model.

A future email-verification workflow requires an explicit SRS revision.

## 23.17 DD-17 — Password Reset / Password Change — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

Password reset/change workflow is **outside FRMS Release 1 scope**.

Release 1 includes:

- registration password creation;
- password login verification.

Release 1 does not include:

- forgot-password OTP;
- reset token/link;
- self-service password change;
- Administrator password reset.

Adding any of these requires a later SRS revision covering identity verification, token expiry, audit, rate limiting, and API contracts.

## 23.18 DD-18 — Initial StorageUnit Status — RESOLVED

`DESIGN-LOCKED — V9 FINAL`

A newly created StorageUnit starts as:

```text
Status = AVAILABLE
```

Preconditions for `UNIT-002`:

- actor is Facility Manager of the same Facility;
- Facility exists;
- UnitType exists;
- UnitCode and other required fields satisfy approved constraints.

Behavior:

- if Facility is `INACTIVE`, an `AVAILABLE` StorageUnit still does not permit new Reservation because Facility activation is independently required;
- when Facility is ACTIVE, the new AVAILABLE unit contributes to eligible capacity;
- preventive maintenance can immediately use the V8-approved `AVAILABLE -> MAINTENANCE` transition.

No additional `DRAFT`, `NEW`, or `INACTIVE` StorageUnit status is introduced.

## 23.19 Resolved Review Items


V8 preserves the V5 review corrections and additionally resolves the low-impact decisions listed above.

- unknown-account LoginHistory contradiction;
- unresolved Damage settlement safety;
- stale V3/V4 normative labels;
- invoice due-day wording;
- Manager unit-selection persistence limitation;
- UnitType reassignment guard;
- stronger TraceId/CancellationToken/shared-client wording;
- FRMS Release 1 vs document-version terminology.

# 24. Future Work


## 24.1 Employee Google Login

Future Employee external authentication:

```text
Google OAuth 2.0 + OpenID Connect
```

Google login maps to an existing FRMS `UserAccount` and still results in an FRMS JWT.

Not a Release 1 blocker unless explicitly promoted.

## 24.2 Future ExternalLogin Entity

Future-only proposal:

```text
ExternalLogin
-------------
ExternalLoginId
UserAccountId
Provider
ProviderSubjectId
CreatedAt
```

```text
UNIQUE(Provider, ProviderSubjectId)
```

Not part of current 27-entity FRMS Release 1 schema.

## 24.3 Advanced Authentication Security

Potential future work:

- account lock/unlock;
- automatic lockout;
- additional external identity providers.

## 24.4 Additional Business Future Work

Source-listed future directions include:

- arbitrary Start Date + rental duration;
- separate Renewal Fee/payment flow;
- Staff Schedule / Shift Management;
- Staff availability/conflict handling;
- Day-off Management;
- optional Staff pre-assignment;
- complex staff assignment optimization;
- expanded Manager approval/operation for Renewal/Overdue;
- Facility-level UnitType/size/pricing delegation;
- expanded configurable rule/policy management;
- configurable Late/Overdue formula;
- generalized role reclassification including Customer;
- Rental Price Range / facility-specific pricing;
- Fee Waiver;
- full payment flow for extra charges;
- detailed refund transaction;
- detailed compensation/debt collection;
- legal dispute/enforcement;
- detailed property recovery;
- additional compensation when Facility cannot provide reserved unit;
- business flows outside the seven core flows.

Future Work is not automatically Release 1 scope.

# 25. Approved Baseline Summary and V11 Pending Overrides

Historical V10 FINAL summary (superseded by the explicit V11 candidate technical deltas on owner approval) consolidates the following implementation baseline, including the approved offline-first-month, operational-consistency, payOS and team-approved Guest revisions; V9 decisions not explicitly superseded remain in force. Items 110–115 summarize the approved Guest requirements adopted under section 0.0.6:
1. Historical V10 authority order was `SRS V10 FINAL > Data Dictionary V2.1 > Scope V8`; V11 draft/approval rules are in §0.1.
2. This SRS is the FRMS Release 1 implementation source of truth.
3. Five roles are fixed: CUSTOMER, FACILITY_STAFF, FACILITY_MANAGER, BUSINESS_OPERATIONS_MANAGER, SYSTEM_ADMINISTRATOR.
4. Seven business flows are the core lifecycle scope.
5. React + TypeScript is the frontend baseline.
6. ASP.NET Core Web API + C# is the backend baseline.
7. PostgreSQL + EF Core is the persistence baseline.
8. Architecture has three logical layers: Presentation / Business / Data Access; Infrastructure is a supporting adapter assembly, not an additional business layer.
9. HTTP dependency flow is Controller -> Service -> Repository -> EF Core/Stored Procedure; application-scheduled flow is Api BackgroundJob -> Service -> Repository, while approved PostgreSQL job scheduler (if separately deployed) may call authoritative procedures directly.
10. No standalone DAO layer exists in the required architecture.
11. Request DTO, Response DTO, Business Command, Business Result and persistence Entity are separate concerns.
12. Critical multi-table lifecycle is owned by authoritative database routines (functions/procedures)/DB transactions.
13. ASP.NET Core Authentication is used.
14. ASP.NET Core Identity is NOT used.
15. Customer login identifier is PhoneNumber.
16. Employee login identifier is Email.
17. `UserAccount.PhoneNumber` is UNIQUE.
18. `UserAccount.Email` is UNIQUE.
19. BCrypt is the password hash algorithm.
20. BCrypt work factor starts at 12 and is configurable.
21. FRMS Release 1 uses FRMS-issued JWT Bearer access tokens.
22. JWT expiry is mandatory and environment-configurable; a refresh-token subsystem is not required.
23. Customer self-registration creates role CUSTOMER with ACTIVE account status under the current no-verification Release 1 model.
24. Authorization is role + Facility + resource ownership + business state.
25. Administrator privilege does not grant general business-data mutation.
26. Future Employee Google OAuth 2.0/OIDC remains Future Work.
27. `ILogger<T>` is used for technical logging.
28. Global Exception Middleware is mandatory.
29. Every API error response includes TraceId.
30. AuditLog is distinct from technical logging.
31. Unknown login identifiers are not inserted into LoginHistory because UserAccountId is non-null.
32. Sensitive credentials/tokens/hashes/binary evidence are never logged.
33. Persisted timestamps use UTC/GMT+0.
34. Business calendar/day calculations and UI display use GMT+7 / Asia/Ho_Chi_Minh.
35. `IClock` or equivalent is mandatory for application time-dependent business logic.
36. Data Dictionary V2.1 entity/lifecycle model is used unless this SRS explicitly overrides it.
37. **Approved V10 core** includes 27 persisted business entities. **Owner-approved V11-OD-02** adds `HandoverUnitAssignment` (Pg013) as the 28th business entity in the Release-1 target; migration application and tests remain V11-G02 acceptance obligations.
38. Email and PhoneNumber uniqueness are explicit SRS overrides.
39. Reservation is month-based and holds capacity, not a concrete StorageUnit.
40. Invoice is used for Deposit and Rental Fee; no separate MonthlyRentalFee entity.
41. Renewal uses append-only ContractExtension; no Renewal workflow entity.
42. Return uses RETURN Visit + ActualReturnDate + Inspection + Settlement; no ReturnProcess table.
43. Policy uses immutable versioned rows.
44. Historical price/policy/invoice snapshots are not rewritten.
45. Direct normal hard-delete of historical core business data is prohibited.
46. `/api/v1` is the REST base path.
47. JSON properties use camelCase.
48. Enum values serialize as strings.
49. API timestamps serialize as UTC ISO-8601.
50. Month values serialize as `YYYY-MM`.
51. List APIs use default `page=1`, `pageSize=20`, maximum `pageSize=100`.
52. FE uses stable `error.code`, not message parsing.
53. Companion API Spec §13 JSON schemas, Swagger/OpenAPI and FE TypeScript models must remain aligned; the SRS contains business/traceability references, not a second normative endpoint inventory.
54. Binary InspectionEvidence upload uses multipart/form-data.
55. Report export is a UTF-8 CSV file response.
56. payOS wire payload remains behind the payment adapter.
57. Business algorithms in the SRS are expressed in pseudocode.
58. Ten testing groups are mandatory delivery structure.
59. Real PostgreSQL integration tests are required for PostgreSQL business routine/trigger/index/locking proof.
60. DBT-01..25 is the minimum database integration baseline.
61. Critical concurrency/idempotency tests are FRMS Release 1 blockers.
62. Seven Playwright E2E journeys correspond to the seven business flows.
63. Migration-based schema and seed are mandatory.
64. Development / Testing / Production configuration separation is mandatory.
65. CI/quality gates are mandatory.
66. Feature/phase/release Definition of Done is mandatory.
67. Phase 0 primary implementer/owner is Nguyễn Trần Trường Giang.
68. AI Size Guide is optional and non-authoritative.
69. Runtime AI provider is isolated behind an interface.
70. Return finalization is blocked while any related DamageRecord remains PENDING.
71. UnitType reassignment is rejected while a StorageUnit is IN_USE/INSPECTION or otherwise occupied by an ACTIVE Contract.
72. **V11-OD-02 supersedes V10:** Manager selects, replaces or releases a persisted `HandoverUnitAssignment` after RESERVATION Visit CHECKED_IN; handover atomically consumes the matching selection.
73. UnitType master rows are deployment master data unless a future explicit CRUD capability is approved.
74. Section 23 Deferred Decisions MUST NOT be silently resolved in code.
75. All previously identified Deferred Decisions DD-01..DD-18 are resolved in V9.
76. Any post-V9 semantic change requires an explicit SRS revision/change record.
77. Release 1 report export format is UTF-8 CSV.
78. Release 1 has no mandatory numeric performance/uptime SLA beyond qualitative NFRs.
79. payOS wire DTOs remain adapter/provider-specific and outside domain contracts.
80. Password validation is 8–64 characters with no mandatory composition classes and BCrypt input <=72 bytes.
81. Login identifiers cannot be changed in Release 1.
82. Runtime AI provider is provider-agnostic/configuration-selected.
83. New Facility status is INACTIVE.
84. StorageUnit may transition AVAILABLE -> MAINTENANCE for preventive maintenance.
85. A Discount referenced by a Contract has immutable customer/percentage/effective-period semantics.
86. Email verification is not part of Release 1 and EmailVerifiedAt is not a login precondition.
87. Password reset/change is outside Release 1 scope.
88. New StorageUnit status is AVAILABLE.
89. Employee initial password is generated by FRMS and delivered through IEmailService; plaintext is never stored/logged.
90. Employee account remains INACTIVE until initial credential email is accepted; ADM-012 retries provisioning with a new password.
91. Facility Manager is the DamageRecord approval/rejection actor.
92. DamageRecord lifecycle is PENDING -> APPROVED or PENDING -> REJECTED; terminal decisions are audited.
93. First-month rent is collected offline for Reservation.LockedRentalPrice without Contract Discount; Contract creation records settlement and no initial Invoice/Payment is created.
94. Contract Discount may apply to eligible invoices strictly after Contract.StartMonth; first-month recognized revenue is Contract-derived and must not be counted again through legacy Invoice/Payment data.
95. Production DamageType seed is LOCK_DAMAGE, DOOR_DAMAGE, WALL_DAMAGE, FLOOR_DAMAGE, WATER_DAMAGE, OTHER with no Release 1 CRUD.
96. All DD-01..DD-18 decisions identified through V9 are resolved.
97. API Request/Response DTOs, Business Commands/Results and persistence Entities are separate boundary types.
98. DTO-to-Command and Result-to-DTO mapping belongs to `Frms.Api`; Business service contracts do not reference API DTOs.
99. External-provider implementations belong to `Frms.Infrastructure` and implement interfaces owned by Business.
100. Release 1 has no separate Worker project. Application-scheduled execution runs in `Frms.Api/BackgroundJobs` and calls Business services; approved PostgreSQL job scheduler (if separately deployed) may call authoritative PostgreSQL business routines directly.
101. `Frms.ArchitectureTests` is required to enforce the locked dependency boundaries.
102. RES-003, CON-002 and VIS-004 allow Customer own detail, Staff/Manager same-Facility detail and BOM system-wide detail. OD-08 additionally grants Staff same-Facility read-only CON-001 list access, not RES-002/VIS-003 list access.
103. RES-002 and VIS-003 remain Customer-own, Manager-Facility and BOM-system lists; CON-001 additionally allows Staff's assigned-Facility Contract list. All filtering and counting precede pagination; no Staff-controlled FacilityId expands scope.
104. OPS-001 Visit work items expose authoritative VisitId/EntityId and minimal Customer identification; non-Visit reference IDs are not interpreted as Visit IDs.
105. ContractDetail already contains ReservationId; operational locked-price/RESERVATION Visit reads follow RES-003/VIS-004 without new Contract attributes or duplicate fields.
106. BOM-010 supports own Customer and authorized same-Facility operational Discount reads; BOM-013 supports active operational ExtraFeeType reads. Catalogue mutation roles are unchanged.
107. BIL-001/002, PAY-003 and CON-004/005 support authorized operational verification/monitoring, not employee payment initiation or manual PAID/SUCCESS mutation.
108. INS-007 completes Inspection but keeps Unit INSPECTION. Required storageUnitStatus moves to INS-008, whose transaction atomically finalizes settlement/Contract and releases Unit AVAILABLE/MAINTENANCE.
109. usageRate = IN_USE / all StorageUnits in authorized report scope, including INSPECTION/MAINTENANCE; zero denominator -> 0. Rental capacity remains a separate calculation.
110. Anonymous Guest is public access mode, not a sixth role or persisted account; public output is independent of account identity.
111. Public catalog/policies and optional AI use an explicit allowlist; private reads/commands and operational views remain authenticated/scoped.
112. A searched Facility needs a matching offered UnitType with positive capacity in every inclusive requested month; public search/selection does not hold capacity.
113. Ordinary Customer login enters the authenticated Customer area; authentication during a pending operation resumes the validated selection/operation without automatic identity/payment/lifecycle submission.
114. Customer retains all Anonymous public capabilities under common data/access contracts; visual design, frontend page URLs/layout and components are specified separately by the frontend owner.
115. CAT-005 and PUB-POL-001/002 are read-only projections without a new entity/table/CMS or personal-policy history; implementation acceptance remains separate from this SRS revision.

# 26. Whole-System Acceptance Criteria


FRMS Release 1 is accepted only when all applicable conditions below are true.

## 26.1 Business Acceptance

```text
[ ] Seven business flows operate according to the current SRS lifecycle
[ ] Five-role responsibilities are respected
[ ] Scope exclusions are not silently implemented as conflicting workflows
[ ] Data Dictionary overrides incorporated by SRS are respected
[ ] Explicit overrides in the current SRS are respected
[ ] GWP-01..05 operate as shared public entry/support capabilities within the existing flows, with AI still optional
```

## 26.2 Data / Lifecycle Acceptance

```text
[ ] Original 27 core tables AND owner-approved V11 28th `HandoverUnitAssignment` target match the governing logical model and PostgreSQL migration metadata
[ ] Pg013 `HandoverUnitAssignment` table, four states and matching-Contract SQL guard are deployed and independently tested; never silently omit history or existing data
[ ] PK/FK/CHECK/UNIQUE/filtered indexes implemented as approved
[ ] Email UNIQUE
[ ] PhoneNumber UNIQUE
[ ] Historical snapshot rules enforced
[ ] Contract.PolicyId equals Reservation.PolicyId after Complete Handover
[ ] State transitions protected
[ ] Append-only tables protected
[ ] Critical database routines (functions/procedures) transactional
[ ] Jobs safely retryable
[ ] Return finalization cannot ignore PENDING DamageRecord
[ ] Complete Inspection leaves Unit INSPECTION and does not persist an intermediate target-status field
[ ] Finalize Return atomically commits settlement, terminal Contract and Unit release; failure/retry cannot release or retarget Unit independently
```

## 26.3 Architecture Acceptance

```text
[ ] Controller -> Service -> Repository dependency flow
[ ] No Controller -> DbContext
[ ] No Service -> DbContext
[ ] No Api BackgroundJob -> Repository/DbContext
[ ] API DTOs are mapped to Business Commands/Results at the Presentation boundary
[ ] Business service contracts expose neither API DTOs nor EF entities
[ ] Infrastructure provider implementations are resolved through Business interfaces
[ ] Architecture-boundary tests pass
[ ] External providers behind interfaces
[ ] Global Exception Middleware active
[ ] ILogger<T> used for technical logging
[ ] AuditLog used for business/security audit
[ ] UTC persistence / GMT+7 business-display behavior
[ ] IClock used for application business time
```

## 26.4 API / FE Contract Acceptance

```text
[ ] Endpoint registry implemented for applicable Release 1 features
[ ] Swagger contains implemented endpoints
[ ] Request/response schemas match SRS
[ ] FE TypeScript models match API enums/fields
[ ] Stable error.code handling implemented
[ ] Pagination contract implemented
[ ] Protected endpoints enforce server-side authorization
[ ] Retired first-month online-payment route/payment reference are absent from current API/FE contracts
[ ] OPS-004 follows the approved offline DD-13 model without initial Invoice/Payment
[ ] Staff CON-001 same-Facility read-only Contract list (count/page scoped before pagination) and detail read RES-003/CON-002/VIS-004; OPS-001 Visit navigation match the approved operational contract
[ ] ContractDetail remains unchanged; locked rent and handover Visit are read through its ReservationId
[ ] Manager/BOM operational reads and BOM-010/BOM-013 catalogue reads match the role/Facility contracts
[ ] INS-007 has no storageUnitStatus; INS-008 requires AVAILABLE/MAINTENANCE and reports committed Unit state
[ ] usageRate includes every scoped Unit and returns 0 for an empty denominator
[ ] CAT-001..005 public search/guide and PUB-POL-001/002 match the approved public schemas, filters and access allowlist
[ ] Anonymous/Customer public data and search semantics agree; ordinary login vs pending-operation continuation follows section 14.15
```

## 26.5 Security Acceptance

```text
[ ] Customer login by PhoneNumber
[ ] Employee login by Email
[ ] BCrypt verification
[ ] JWT expiry enabled
[ ] Account ACTIVE enforced for authenticated private/operational access and business commands
[ ] RBAC enforced
[ ] Facility scope enforced
[ ] Customer ownership enforced
[ ] Staff same-Facility operational detail and work-item reads pass positive/cross-Facility tests
[ ] Operational read access does not grant Customer mutation or expose unrelated profiles
[ ] Manager/BOM and financial/catalogue GET scopes pass role, Facility, ownership and pagination-leak tests
[ ] Anonymous catalog/policy reads expose only common public fields; private/operational endpoints reject unauthenticated access
[ ] Protected operational view cannot fall back to public after authorization denial; continuation resists redirect/role/account tampering
[ ] Unknown identifier does not fabricate LoginHistory.UserAccountId
[ ] PasswordHash/token/secret not exposed/logged
```

## 26.6 Testing Acceptance

```text
[ ] Required unit tests pass
[ ] Controller/DTO contract tests pass
[ ] Repository/SQL integration tests pass
[ ] DBT-01..25 pass
[ ] Critical CON suite passes
[ ] SEC suite passes
[ ] Postman regression passes
[ ] JOB/INT tests pass for enabled integrations/jobs
[ ] Playwright E2E-F01..F07 pass for implementable approved scenarios
[ ] Regression/Acceptance suite passes
```

## 26.7 Decision-Resolution Acceptance

All known DD-01..DD-18 decisions are resolved in V9.

```text
[ ] Employee initial credential email flow verified
[ ] Facility Manager Damage decision flow verified
[ ] Offline first-month amount and Staff acknowledgment verified
[ ] No Invoice/Payment is generated for Contract.StartMonth by billing/return/webhook paths
[ ] Later-invoice Discount behavior and Contract-derived first-month revenue verified
[ ] Production DamageType seed verified
[ ] No implementation introduces an unrecorded new business decision
```

## 26.8 Release Evidence

```text
[ ] Migration version recorded
[ ] Seed process recorded
[ ] Swagger/OpenAPI snapshot available
[ ] Postman collection available
[ ] Test result summary available
[ ] Applicable approved SRS version and standalone API specification referenced by release
[ ] Deferred Decision resolutions/change records attached where applicable
```

A build that fails any applicable Release Blocker criterion is not aligned with the applicable approved SRS; this V11 candidate requires its own owner sign-off and verification.

# Appendix A — Core Lifecycle Summary


Core lifecycle summary:

```text
Browse/search public Facility / UnitType (Anonymous or Customer)
-> select valid future month range
-> Customer authentication when needed; resume validated selection
-> Customer explicitly creates Reservation
-> Reservation(PENDING_DEPOSIT)
-> Deposit Invoice
-> Deposit Payment SUCCESS
-> Reservation CONFIRMED
-> RESERVATION Visit
-> CHECKED_IN
-> select AVAILABLE StorageUnit
-> Staff receives full first-month rent offline
-> Staff Complete Handover (OPS-004 acknowledges offline receipt)
-> Contract ACTIVE / Unit IN_USE
```

During rental:

```text
ACTIVE Contract
-> ACCESS Visit(s)
-> monthly Invoice(s) strictly after Contract.StartMonth
-> payOS Payment / Overdue / LateFee for those invoices
-> optional Renewal -> ContractExtension
```

Return:

```text
RETURN Visit
-> ActualReturnDate
-> Unit INSPECTION
-> claim Inspection / record fees and Damage
-> Inspection COMPLETED (Unit stays INSPECTION)
-> resolve any pending Manager Damage decisions
-> INS-008(storageUnitStatus = AVAILABLE or MAINTENANCE)
-> atomic DepositSettlement + Contract COMPLETED or TERMINATED + Unit AVAILABLE or MAINTENANCE
```

# Appendix B — Glossary


| Term | Meaning |
|---|---|
| Anonymous / Guest | Unauthenticated public access mode; not a UserRole or Customer/account entity |
| Public projection | Shared booking/help information independent of account identity; excludes private and operational data |
| Login continuation | Resume a validated internal selection/step after Customer login; no hold or automatic mutation |
| Facility | Physical storage service location |
| UnitType | Global storage-type master with current RentalPrice |
| StorageUnit | Physical rentable unit/position |
| Reservation | Future capacity hold by Facility + UnitType + month range |
| Visit | RESERVATION, ACCESS or RETURN visit record |
| Contract | Rental agreement, occupancy source and first-month offline settlement record after successful handover |
| ContractExtension | Append-only successful renewal history |
| Invoice | DEPOSIT or second-month-onward RENTAL_FEE billing record; none for the initial month |
| Payment | Invoice-backed payOS payment attempt/result; no first-month offline Payment |
| Policy | Complete immutable versioned operational-parameter row |
| Inspection | Official return/recovery inspection record |
| DamageRecord | Damage found during Inspection |
| ExtraFee | Operational extra fee recorded from Inspection |
| DepositSettlement | Final recorded deduction/refund/additional obligation |
| SOURCE-LOCKED | Directly supported by authoritative lower-level source |
| OVERRIDE-LOCKED | Explicit later override of older wording |
| DESIGN-LOCKED | Approved technical design requirement |
| DEFERRED | Explicit unresolved decision that must not be silently invented |
| Core Demo | Phase 1 end-to-end demonstration of seven flows |
| Release 1 | Hardened complete implementation of the approved Release 1 business scope |
| Release Blocker | Requirement/test whose failure prevents Release 1 acceptance |
| Authoritative transaction | Transaction boundary responsible for atomic business-state mutation |
| Idempotent | Repetition produces no duplicate logical business effect |

# Appendix C — Error Code Catalogue


## Business / Lifecycle Codes

```text
ACCOUNT_INACTIVE
FACILITY_INACTIVE
INVALID_MONTH_RANGE
CAPACITY_NOT_AVAILABLE
RESERVATION_INVALID_STATUS
DEPOSIT_NOT_PAID
VISIT_DATE_OUT_OF_POLICY
VISIT_INVALID_STATUS
VISIT_ENTITY_MISMATCH
CONTRACT_NOT_ACTIVE
RETURN_VISIT_PENDING
UNIT_NOT_AVAILABLE
UNIT_FACILITY_TYPE_MISMATCH
DISCOUNT_NOT_OWNED_BY_CUSTOMER
DISCOUNT_NOT_VALID
INVOICE_ALREADY_EXISTS
PAYMENT_CALLBACK_DUPLICATE
INSPECTION_ALREADY_CLAIMED
INSPECTION_INVALID_STATUS
RENEWAL_CAPACITY_NOT_AVAILABLE
RENEWAL_NOT_CONTIGUOUS
POLICY_VERSION_CONFLICT
SUPPORT_TICKET_INVALID_STATUS
RETURN_ALREADY_FINALIZED
DAMAGE_DECISION_PENDING
DAMAGE_INVALID_STATUS
CREDENTIAL_PROVISIONING_INVALID_STATUS
EMAIL_ALREADY_EXISTS
PHONE_NUMBER_ALREADY_EXISTS
```

## API / Technical Codes

```text
AUTH_INVALID_CREDENTIALS
UNAUTHORIZED
FORBIDDEN
RESOURCE_NOT_FOUND
VALIDATION_ERROR
EXTERNAL_PROVIDER_UNAVAILABLE
EXTERNAL_PROVIDER_ERROR
INTERNAL_SERVER_ERROR
```

## Mapping Principles

```text
validation / malformed request      -> 400
invalid/missing authentication      -> 401
authenticated but not authorized    -> 403
accessible resource not found       -> 404
state/capacity/uniqueness conflict  -> 409
provider invalid response           -> 502
provider temporarily unavailable    -> 503
unexpected internal failure         -> 500
```

Exact SQL numeric error numbers are implementation detail; stable string codes are the API contract.

# Appendix D — API Endpoint Index (separate companion)

The **only normative HTTP endpoint inventory** for this V11 candidate is
[`FRMS_API_V11.md` §13.11](FRMS_API_V11.md#1311-endpoint-registry).  
The **non-approved implementation delta** is maintained in the same file's
[§13A](FRMS_API_V11.md#13a-v11-implementation-delta--provisional-api-additions).

This SRS deliberately does not duplicate method/route/role/DTO definitions. Approved core
IDs (`AUTH`, `CAT`, `PUB-POL`, `AI`, `RES`, `BIL`, `PAY`, `CON`, `VIS`, `OPS`, `INS`,
`SUP`, `UNIT`, `BOM`, `REP`, `ADM`) remain unchanged; provisional `IMPL-*` identifiers
have **no** automatic role or release authority. Any API inventory/JSON/Swagger/FE mismatch
is a tracked contract drift under V11-G07.

# Appendix E — Test Case Index


## Test Prefixes

```text
UT-*       Business unit
CTL-*      Controller / DTO
DAL-*      Repository / EF / SQL integration
DBT-*      Database lifecycle/invariant
CON-*      Concurrency / idempotency
SEC-*      Authentication / authorization / security
API-*      Postman / HTTP
INT-*      External integration
JOB-*      Scheduled/background job
E2E-*      Playwright end-to-end
REG-*      Regression
ACC-*      Acceptance
```

## Minimum Database Integration Baseline

```text
DBT-01  final-capacity Reservation race
DBT-02  Reservation on INACTIVE Facility
DBT-03  confirm without paid Deposit
DBT-04  duplicate RESERVATION Visit
DBT-05  cancel after CHECKED_IN
DBT-06  same-unit concurrent Handover
DBT-07  cross-customer Discount
DBT-08  duplicate monthly Rental Invoice
DBT-09  duplicate payOS webhook
DBT-10  concurrent Inspection claim
DBT-11  Renewal vs Reservation capacity race
DBT-12  historical Contract retains old Policy
DBT-13  early-return waive-window behavior
DBT-14  normal settlement with Late/Extra/Damage
DBT-15  early-return RefundAmount = 0
DBT-16  duplicate FinalizeReturn
DBT-17  append-only mutation rejection
DBT-18  failed notification remains PENDING
DBT-19  external recovery with null VisitId
DBT-20  repeat ActualReturn confirmation
```

## Core E2E Baseline

```text
E2E-F01  Reservation
E2E-F02  Check-in & Handover
E2E-F03  Rented Storage / ACCESS
E2E-F04  Business Rules / Fee / Revenue
E2E-F05  Facility / Staff Management
E2E-F06  Renewal / Overdue / Return
E2E-F07  Support
```

# Appendix F — Pull Request / Implementation Checklist


```text
[ ] Requirement/feature ID identified
[ ] Correct current SRS section referenced
[ ] Correct logical-layer and supporting-assembly dependency
[ ] Controller remains thin
[ ] API DTO -> Business Command/Result mapping remains in Frms.Api
[ ] Business service contract exposes no API DTO or EF Entity
[ ] Service owns business orchestration
[ ] Repository owns persistence/query access
[ ] No direct Controller/Service -> DbContext violation
[ ] No Api BackgroundJob -> Repository/DbContext violation
[ ] External adapter implements a Business-owned interface
[ ] Composition-root-only Api reference to DataAccess/Infrastructure preserved
[ ] Architecture test updated when a dependency boundary changes
[ ] Request/Response DTO contract updated
[ ] Swagger updated
[ ] FE TypeScript model updated
[ ] Role authorization verified
[ ] Facility/resource ownership verified
[ ] Validation placed at correct layer
[ ] Stable error code used
[ ] Global exception behavior preserved
[ ] ILogger does not contain secrets
[ ] Audit event added when required
[ ] UTC persistence respected
[ ] GMT+7 business/display rule respected
[ ] Critical transaction uses correct authoritative boundary
[ ] Idempotency/concurrency invariant preserved
[ ] Migration included if schema changed
[ ] Seed updated if required
[ ] Unit test updated
[ ] Integration/DBT/CON test updated when invariant changed
[ ] Postman contract test updated
[ ] Playwright E2E updated if user journey changed
[ ] Traceability matrix updated
[ ] No Deferred Decision silently resolved
[ ] No unapproved business entity/status/workflow added
```

---

# 27. V11 Documentation / Code Alignment Gate (DRAFT)

## 27.1 Evidence sources (read-only)

- GitHub `main` current review HEAD: `0219f30734b00b3e5527e3453b19840f08f63639`, verified 2026-10-21 (includes recurring BackgroundJobs). Earlier `90a6c5982d4237a61eab9299f2ca434efd624fcc` is preserved as the initial V11 audit provenance.
- Governing V10 main source: `docs/FRMS_SRS_V10.md` (contains 2026-10-20 Customer profile revision).
- Locally available prior canonical body: `FRMS_SRS_V10.2.md` (used as content base in this candidate; latest main revision explicitly reconciled).
- Technical evidence: `backend/Frms.DataAccess/DependencyInjection/DataAccessServiceCollectionExtensions.cs`; `backend/Frms.Migrations.PostgreSql/Migrations/Pg*`; `backend/sql/postgresql/HandoverAssignments.sql`; controller sources for the §13A implementation-delta API appendix.
- Historical lower authority: `docs/03-FRMS_Data_Dictionary_V2_1.md`; synchronized V11 target: [`FRMS_Data_Dictionary_V11.md`](FRMS_Data_Dictionary_V11.md). `Customer.Address` is **retained optional** under V11-OD-01; PostgreSQL implementation metadata and the approved new handover entity/statuses are documented but require actual migration/test evidence.

## 27.2 Items expressly retained

Five roles and seven core flows; Reservation holds Facility+UnitType+month capacity, no Physical Unit ID on Reservation; signed/full offline first-month settlement at atomic handover; later invoice/payOS flow; Payment idempotency and verified webhook; captured Policy and price snapshots; same-Facility authorization; Inspection claim and approved Damage/Settlement; active Contract occupancy; AuditLog immutability and historical payment evidence; public Guest projection/access separation.

## 27.3 Pending owner approvals / blocked DoD

| Gate | Requirement | Current status | Required evidence |
|---|---|---|---|
| V11-G01 | Approve PostgreSQL as replacement technical baseline | PENDING | Owner ADR/SRS approval + complete schema/SQL migration assurance |
| V11-G02 | Owner approval for 28th entity, states and Manager handover workflow | **APPROVED — TESTS PENDING** | Verify migration application, index/trigger guards, SQL concurrency/rollback, full handover E2E |
| V11-G03 | Approve three read-only SYSTEM_ADMINISTRATOR timeline GETs | **APPROVED — SECURITY TESTS PENDING** | Restrict exception to timeline; confirm own-Facility Manager and cross-Facility Admin/BOM reads; 401/403/404 |
| V11-G04 | Approve Customer activity and supplemental directory APIs | **PARTIAL: IMPL-CUSACT-01/02 REQUIREMENTS APPROVED (OD-06/07); implementation/test acceptance pending; directories still provisional** | Owner-only SENT/Payment history, pagination/API errors and security/integration/UI tests; no unread/derived notification links or fabricated financial records; separate approval for directory reads |
| V11-G05 | Acknowledge public CAT-003/004/005 media URL response fields | **CODE VERIFIED / OWNER ACKNOWLEDGED — TESTS PENDING** | Media governance/URL validation and generated Swagger/FE null/shape parity; future upload/CMS not approved |
| V11-G06 | Retain optional Customer.Address and align private AUTH-004 CCCD | **APPROVED; OWNER-ONLY CCCD SERIALIZATION NOW OBSERVED AT 2945788; RUNTIME TESTS PENDING** | Regression for AUTH-001/005 optional Address; AUTH-004 null/string own-Customer-only projection and no widened disclosure; generated Swagger/FE tests |
| V11-G07 | Confirm exact endpoint registry vs running OpenAPI and FE | NOT VERIFIED | Diff generated Swagger against companion API spec |
| V11-G08 | Test full migration chain Pg001..Pg013 and cross-feature regression | NOT RUN HERE | Restore/build/NUnit/PostgreSQL integration/concurrency/Playwright |
| V11-G09 | Final SRS/API release-document ratification | **PARTIAL OWNER DECISIONS APPROVED; FINAL PENDING** | Complete main-to-V11 full-text diff, other pending decisions, API/FE parity and controlled repository publication |
| V11-G10 | Monthly billing / overdue / LateFee recurring pipeline (`JOB-GAP-01/03/04`) | **CODE REGISTERED; BILLING DEFAULT TRUE; TESTS PENDING** | PostgreSQL DBT: no first month Invoice; correct month/price/Discount/Policy; idempotent Invoice and LateFee; outage/backfill decision, UTC/GMT+7 boundary, interval SLA |
| V11-G11 | No-show cleanup (`JOB-GAP-05`) | **CODE REGISTERED; DEFAULT FALSE** | Prove `SCHEDULED`/`CHECKED_IN` Visit effects and no accidental valid handover cancellation, test concurrency, approve activation |
| V11-G12 | Notification retry (`JOB-GAP-06`) | **CODE REGISTERED; PROVIDER UNCONFIGURED; DEFAULT FALSE** | Real sender, `PENDING→SENT` after delivery, idempotent provider, crash/retry and multi-instance lock tests, queue throughput |
| V11-G13 | Job deploy/operability (`JOB-GAP-01/02`) | **NOT VERIFIED** | Build/NUnit, current PostgreSQL Pg007 functions, deployed scheduler ownership, logs/alerting, safety flags, multi-replica tests, canary and rollback |
| V11-G14 | OD-09 CCCD before new Reservation and interrupted-operation continuation | **TEAM APPROVED; NEW GATE/CONTINUATION IMPLEMENTATION AND TESTS PENDING** | §14.15.3 API-CCCD/SEC-CCCD/DBT-CCCD/REG-CONT + E2E-F01; 400 errors.cccd; no writes on rejection, atomic/current-profile guard, safe return and no auto-submit, deployed API/Swagger/FE parity |
| V11-G15 | OD-10 exact 12-digit CCCD | **OWNER APPROVED; FE/DOC UPDATE; BE/DB/OPENAPI ACCEPTANCE PENDING** | API-CCCD-004, DBT-CCCD-003, REG-CONT-002; authenticated atomic format guard, optional/legacy semantics and generated Swagger parity |
| V11-G16 | OD-06 replacement: SMTP-independent in-app Notifications / Overview | **REVIEW DRAFT; CODE CHANGES NOT AUTHORIZED** | NOT-D01/02 and API/FE DTO approval; API-NOTAPP, SEC-NOTAPP, DBT-NOTAPP, E2E-NOTAPP; SMTP off/failure, correct recipient, atomic events, preserved history and worker exclusion |

**Job-specific DoD:** All six tasks are present in source but only three Billing operations and Deposit expiration are default-active by current source. NoShow and Notifications require separate activation; the notification sender does not exist as an operational adapter. Do not infer successful execution from hosted-service registration. The 24-hour interval is not a fixed local-midnight cron; the current-month SQL generator does not backfill previous missed months.

**Definition of Done:** V11 cannot be called `FINAL`, `RELEASE COMPLETE`, or `API CONTRACT VERIFIED` until all applicable gates are positively evidenced. Historical V10's 27-entity scope and the **owner-approved V11 28th entity** are both traceable; approval is not proof Pg013 is deployed or safe. `SYSTEM_ADMINISTRATOR` timeline-only read is explicitly approved (V11-OD-03), but all other role expansion remains prohibited without a change record. A controller, migration or page is not proof of passing integration or security gates.

## 27.4 Controlled rollout sequence (documentation only)

1. Record accepted V11-OD-01..07 decisions; implement/verify approved Customer activity reads separately and resolve remaining pending architecture/directory/billing-backfill/media-governance decisions in the appropriate owning tasks.
2. Produce approved SRS/API V11 release candidate; reconcile full `main`-to-V11 diff.
3. Review the synchronized companion Data Dictionary V11 (`FRMS_Data_Dictionary_V11.md`); verify its physical schema/routine notes against actual PostgreSQL deployment in the owning implementation tasks only.
4. Verify actual PostgreSQL migration history, SQL routine behavior, Swagger/TypeScript parity, auth/PII and negative tests, then E2E.
5. Merge artifacts only through an authorized process; **this output does not modify GitHub**.


## V11 PR Publication Note — 2026-10-21

This document is prepared for controlled GitHub review with companion `FRMS_API_V11.md` and `FRMS_Data_Dictionary_V11.md` at the pinned read-only `main` revision `0219f30734b00b3e5527e3453b19840f08f63639`. The Data Dictionary entity catalogue has been statically normalized to **28 classes / 202 fields** as currently modeled; this does not reauthorize historical or provisional scope. Payment gateway for **new** transactions is payOS only; SRS contains no retired-gateway contract.

**PR acceptance gates remain open:** V11-G02/G03/G06–G13, Swagger generated DTO/route parity, PostgreSQL migrations on target database, real Notification sender, scheduling/outage/backfill policy, tests and owner full-document ratification. Document publication is permitted as a **V11 review candidate** but must not be represented as `V11 FINAL`, production validation or completed Definition of Done. No repository or runtime change is part of this documentation package.
