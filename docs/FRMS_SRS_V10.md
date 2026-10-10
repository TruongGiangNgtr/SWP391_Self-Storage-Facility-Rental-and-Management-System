# FRMS — Software Requirements Specification (SRS)

**Project:** Self-Storage Facility Rental and Management System (FRMS)
**Document:** Software Requirements Specification + Architecture + API Contract + Testing Baseline
**Version:** V10 FINAL
**Baseline Date:** 2026-10-02
**Architecture Revision Date:** 2026-10-04
**Business Revision Date:** 2026-10-07 — REV-2026-10-07-FM-OFFLINE
**Operational Consistency Revision Date:** 2026-10-07 — REV-2026-10-07-OP-READ
**Payment Gateway Revision Date:** 2026-10-09 — REV-2026-10-09-PAYOS
**Payment Idempotency Clarification Date:** 2026-10-10 — REV-2026-10-10-PAY-IDEMPOTENCY
**Status:** Final Implementation Baseline — Approved payOS Code/Mock Revision; Live Payment Acceptance Pending
**Architecture:** 3 Logical Layers + Repository Pattern + Infrastructure Adapters
**Backend:** ASP.NET Core Web API / C#
**Frontend:** React + TypeScript
**Database:** Microsoft SQL Server
**ORM:** Entity Framework Core
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
| **V10 FINAL — 2026-10-07 payment gateway revision** | Owner-approved VNPay Sandbox substitution | REV-2026-10-07-VNPAY replaces the MoMo Sandbox adapter and routes with VNPay Sandbox for Deposit and post-first-month Rental Fee invoices. Payment business semantics, offline first-month settlement, Invoice/Payment entities and idempotency rules remain unchanged. |
| **V10 FINAL — 2026-10-09 payOS revision** | Owner-approved provider replacement (code/mock only) | REV-2026-10-09-PAYOS; preserve MOMO/VNPAY history, numeric order-code sequence, signed JSON webhook; live acceptance pending. |
| **V10 FINAL — 2026-10-08 payment timestamp clarification** | Owner-approved persisted Invoice payment timestamp | Invoice.PaidAt is nullable UTC, set from the verified provider timestamp only on the first UNPAID/OVERDUE -> PAID transition. Duplicate webhook, subsequent successful attempts and cancelled invoices preserve the Invoice timestamp. Existing historical timestamps are not fabricated. |
| **V10 FINAL — 2026-10-10 payment idempotency clarification** | Owner-requested alignment with the approved PAY-001 contract | REV-2026-10-10-PAY-IDEMPOTENCY makes the frontend UUID, required persisted uniqueidentifier, global uniqueness, lifetime retention, retry/concurrency and non-disclosure rules explicit in the SRS and Data Dictionary. No new Payment status, recovery workflow or live-provider authorization. |

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

> V10 FINAL with REV-2026-10-09-PAYOS is the current implementation baseline. Earlier MoMo/VNPay revision entries are historical only; payOS is the sole active gateway. Offline first-month and operational-read semantics remain unchanged.

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

## 0.0.1 Historical Change Record (superseded by REV-2026-10-09-PAYOS) — REV-2026-10-07-VNPAY

**Approval:** The project owner selects VNPay Sandbox as the Release 1 online payment gateway on 2026-10-07 because the team is implementing an academic project and will use VNPay Sandbox credentials.

**Approved scope:**

- VNPay Sandbox replaces MoMo Sandbox for Deposit invoices and Rental Fee invoices whose BillingMonth is later than Contract.StartMonth.
- The first rental month remains offline under REV-2026-10-07-FM-OFFLINE. This revision does not restore PAY-002 or create a first-month Invoice/Payment.
- `Payment.PaymentMethod` for new Release 1 online attempts is `VNPAY`.
- PAY-001 becomes `POST /api/v1/invoices/{invoiceId}/payments/vnpay`.
- PAY-004 becomes the provider-facing `GET /api/v1/payments/vnpay/ipn`. VNPay sends IPN parameters in the query string; the endpoint is anonymous at the HTTP authentication layer but trusted only after adapter-level checksum, merchant, reference and amount validation.
- The customer browser return target is `/customer/payments/result`. Local development uses `http://localhost:5173/customer/payments/result`. A staging/production frontend origin has not been provided and MUST remain environment configuration rather than a fabricated domain.
- The team selects ngrok for Sandbox development. ngrok proxies the public HTTPS origin `https://lying-ladder-showroom.ngrok-free.dev` to the backend HTTP origin `http://localhost:5164`. The provider-facing IPN URL is `https://lying-ladder-showroom.ngrok-free.dev/api/v1/payments/vnpay/ipn`; localhost is not used as the server-to-server IPN target.
- Browser return data is not authoritative and MUST NOT update Payment/Invoice state. The frontend refreshes PAY-003/BIL-002; only a valid, idempotently applied IPN result may update financial state.

**Provider contract lock:** VNPay Sandbox API version `2.1.0` is used with payment URL `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`, VND amounts encoded as the FRMS amount multiplied by 100, and HMAC-SHA512 checksum validation. `vnp_TmnCode` is non-secret runtime configuration; `vnp_HashSecret` is a secret and MUST NOT be committed, logged or exposed to the frontend. Exact provider fields remain inside the VNPay adapter.

**Runtime configuration:**

```text
Payment__VnPay__TmnCode=<configured locally>
Payment__VnPay__HashSecret=<secret/configured locally>
Payment__VnPay__CredentialSetId=sandbox-v1
Payment__VnPay__PaymentUrl=https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
Payment__VnPay__ReturnUrl=http://localhost:5173/customer/payments/result
Payment__VnPay__IpnUrl=https://lying-ladder-showroom.ngrok-free.dev/api/v1/payments/vnpay/ipn
Payment__VnPay__Locale=vn
Payment__VnPay__OrderType=other
Payment__VnPay__ExpiryMinutes=15
```

The approved .NET configuration section is `Payment:VnPay`; do not introduce a parallel `Vnpay:*` section. `Locale=vn` selects the Vietnamese VNPay UI, `OrderType=other` classifies the self-storage rental service for the current Sandbox flow, and `ExpiryMinutes=15` is used to calculate the required `vnp_ExpireDate`. VNPay API version `2.1.0`, command `pay`, currency `VND` and HMAC-SHA512 remain adapter constants for this locked contract rather than runtime configuration.

Start the selected tunnel with `ngrok http 5164` and keep both the backend and ngrok process running during VNPay Sandbox tests. If ngrok later assigns a different public hostname, the environment value and VNPay Sandbox IPN registration MUST be updated together before testing. `CredentialSetId` is internal non-secret metadata and is not sent to VNPay.

**Affected requirements:** EPS-01; SSP-07; CWP-04/CWP-08; PAY-001/PAY-003/PAY-004; PaymentDetail; BR-PAY-01; external-integration, security, testing, traceability and release-gate references; Data Dictionary PaymentMethod and payment-result wording.

**Compatibility boundary:** Existing code, migrations, tests, branch names and historical records may still contain `MOMO` until their owning implementation plan migrates them. The supplied BE task-tracker PDF also still labels CWP-04/CWP-08/SSP-07/EPS-01 as MoMo and must be regenerated from its editable source; the Topic PDF is gateway-neutral and needs no change. This change record defines the target source of truth; it does not falsely claim that implementation artifacts have already been aligned. Existing historical `MOMO` rows, if any, MUST be preserved rather than rewritten.

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

## 0.0.3 Approved Change Record — REV-2026-10-09-PAYOS

**Approval:** Owner approved VNPay replacement and clarified webhook acknowledgement, UTC+7 provider time and description on 2026-10-09.
payOS is the sole active EPS-01 gateway; MOMO/VNPAY rows and old migrations remain historical-compatible and are never rewritten.
payOS supports personal/household-business accounts; no separate sandbox/staging exists. Official production API: https://api-merchant.payos.vn.
Current scope is code, migrations, mock/fixture automated tests only. No real payment link, transfer, /confirm-webhook or live API call is authorized.
No live payOS evidence exists; EPS-01 release acceptance remains NOT COMPLETE until separately approved live E2E succeeds.
First month remains offline: no first-month Invoice/Payment, PAY-002 remains retired. Deposit/later Rental Fee eligibility, authorization, idempotency, three Payment statuses, audit and authoritative SQL effects are unchanged.

**Approved persistence:** ProviderOrderCode BIGINT NULL; SQL Server dbo.ProviderOrderCodeSequence starts at 1000, increment 1, NO CYCLE. Allocate atomically with new PAYOS attempts, not timestamps/UUID conversions; filtered unique index. Existing MOMO/VNPAY codes remain null. Downgrade fails clearly when PAYOS/code evidence exists.
PaymentUrl stores checkoutUrl (2048 bounded characters); PaymentUrlExpiresAt UTC stores provider expiry when supplied or explicit request expiry; TransactionCode stores successful webhook reference, not paymentLinkId/orderCode.
Missing config does not fail startup; PAY-001 raises EXTERNAL_PROVIDER_NOT_CONFIGURED before creation. Secrets are local only; .env ignored and excluded from build/publish.
PAY-001 is POST /api/v1/invoices/{invoiceId}/payments/payos; PAY-003 unchanged; PAY-004 is anonymous POST /api/v1/payments/payos/webhook. Old MoMo/VNPay routes are inactive.
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

[ ] Approved live payOS link creation
[ ] Approved real payment / verified webhook / UTC Invoice.PaidAt evidence
[ ] EPS-01 release acceptance

No editable task-tracker PDF source has been supplied; do not recreate PDF. This record supersedes historical provider requirements, not unrelated business semantics.

## 0.0.4 Approved Clarification — REV-2026-10-10-PAY-IDEMPOTENCY

**Approval:** On 2026-10-10 the owner requests explicit IdempotencyKey requirements in both the SRS and Data Dictionary, and Payment-related Data Dictionary alignment with the already-approved payOS and Invoice.PaidAt revisions. This incorporates the previously approved PAY-001 idempotency contract; it does not introduce a new payment obligation or gateway workflow.

`Payment.IdempotencyKey` MUST be a frontend-generated UUID persisted as SQL Server `uniqueidentifier NOT NULL`, globally unique across all Payment rows and retained for the lifetime of its Payment row. The authoritative attempt-creation/retry contract is section 12.7.1; PAY-001 and the Data Dictionary MUST follow that contract.

This clarification does not authorize fabricated keys or Invoice links for historical data, repair of stranded attempts, refund/reconciliation, additional statuses, code/schema changes, deployment or live payOS calls. Historical-data review and the code/mock-only boundary remain in force.

## 0.1 Authority Order

For implementation, review, testing, and conflict resolution:

1. **FRMS SRS V10 FINAL**
2. **FRMS Data Dictionary V2.1**
3. **FRMS Scope V8**
4. Implementation detail

Conflict resolution:

```text
IF this SRS explicitly defines or overrides a rule
    FOLLOW this SRS

ELSE IF Data Dictionary V2.1 defines or overrides it
    FOLLOW Data Dictionary V2.1

ELSE
    FOLLOW Scope V8
```

No implementation component may silently create another interpretation.

## 0.2 Source Roles

### SRS V10 FINAL

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

### Data Dictionary V2.1

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

This SRS uses only the following implementation classifications:

| Label | Meaning |
|---|---|
| `SOURCE-LOCKED` | Directly supported by Data Dictionary V2.1 / Scope V8 and not superseded. |
| `OVERRIDE-LOCKED` | Explicit later override of older source wording. |
| `DESIGN-LOCKED` | Technical/design decision explicitly locked by this SRS. |
| `DEFERRED` | Current sources do not define enough to safely implement the behavior; implementation MUST NOT guess. |

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

`DESIGN-LOCKED — FINAL`

This V10 document is the FRMS Release 1 implementation baseline. It preserves the approved V9 business semantics except for the explicit owner-approved revisions recorded in section 0.0 and its subsections.

After approval:

- code contradicting this SRS is a defect unless accompanied by an approved SRS revision;
- Data Dictionary or Scope wording does not override an explicit rule in this SRS;
- a Deferred Decision MUST remain unimplemented, guarded, or configurable as specified until separately approved;
- no team member may resolve a Deferred Decision only in code, database migration, Swagger, UI, or test fixture;
- any semantic change requires a new SRS revision and traceability update.

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

The design remains month-based: Reservation holds capacity by `Facility + UnitType + month range`; a concrete `StorageUnit` is selected only at handover.

## 2.3 In-Scope

Current FRMS Release 1 business scope includes:

- customer self-registration and account use;
- Facility and Unit Type browsing;
- available-capacity display for a requested month range;
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

# 4. Business Flow Overview


## 4.1 Flow 1 — Storage Unit Reservation

```text
Browse active Facility / UnitType
-> choose StartMonth + EndMonth
-> calculate capacity for every requested month
-> create Reservation(PENDING_DEPOSIT)
-> create Deposit Invoice
-> payOS payment
-> confirm Reservation
-> create RESERVATION Visit(SCHEDULED)
```

Rules:

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
| Database | SQL Server | Main relational database |
| Database Tool | SQL Server Management Studio | Schema, queries, procedures, administration |
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
[ ] approved live payOS Deposit works (NOT RUN)
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
      ↓ EF Core / Stored Procedures
SQL Server

Supporting assembly:
Frms.Infrastructure -> implements Business provider interfaces

Scheduled execution:
Frms.Api/BackgroundJobs -> Business services
SQL Server Agent        -> authoritative stored procedures
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
- migrations and SQL Server persistence;
- database dependency registration.

Repositories MUST NOT decide actor permissions or business state transitions. They execute persistence behavior requested by Business services and authoritative stored procedures.

### 7.3.4 Supporting Infrastructure Adapter Assembly

`Frms.Infrastructure` implements `IPaymentGateway`, `IAiRecommendationProvider`, `IEmailService`, `INotificationSender` and future external-provider interfaces declared by Business. Provider wire DTOs remain inside their adapter.

Release 1 does not define a separate Worker project. Application-scheduled execution is hosted in `Frms.Api/BackgroundJobs` and calls Business services. Where available, SQL Server Agent MAY call the same authoritative stored procedures directly.

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
Service -> Repository -> Stored Procedure -> SQL Server transaction
```

The Stored Procedure owns the authoritative database transaction for critical multi-table lifecycle operations.

## 7.7 External Integration and Background Execution Boundaries

- Business declares provider interfaces.
- Infrastructure contains provider implementations and provider-specific wire DTOs.
- API composition registers implementations through dependency injection.
- Payment, email, notification or AI implementations MUST NOT be instantiated inside Controllers or Business services.
- API BackgroundJobs call Business services and reuse the same authoritative stored procedures and idempotency rules as HTTP-triggered flows.
- SQL Server Agent jobs MAY call authoritative stored procedures directly and remain subject to the same idempotency/locking rules.

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

Data Dictionary V2.1 defines the current database baseline unless this SRS explicitly overrides it.

## 9.2 Persisted Entity Catalogue

FRMS Release 1 has 27 persisted entities:

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

- `Customer.CCCD` nullable; unique when present.
- `Employee.FacilityId` required for Facility Staff/Manager; not required for global employee roles.
- `Visit.EmployeeId` nullable before actual Staff handling.
- `Visit.ActualReturnDate` only meaningful for RETURN.
- `Contract.DiscountId` nullable.
- `Invoice.BillingMonth` null for DEPOSIT; required for RENTAL_FEE.
- `Invoice.DiscountId` null for DEPOSIT.
- `Invoice.PaidAt` is a nullable persisted UTC timestamp. Set it from the verified provider success timestamp only when the Invoice first transitions from UNPAID/OVERDUE to PAID. Already-PAID and CANCELLED invoices retain their timestamp; duplicate webhook never overwrites it. Historical rows without timestamp evidence remain null (owner-approved 2026-10-08 clarification).
- Every new Payment MUST reference an existing Invoice through Payment.InvoiceId. No first-month Payment is allowed. Historical null InvoiceId rows from the superseded model must be preserved until an approved migration/deployment review; do not silently link or delete them.
- `Payment.IdempotencyKey` is a required frontend UUID stored as `uniqueidentifier NOT NULL`; it has no fabricated server-generated backfill, is globally unique and is retained unchanged for the lifetime of the Payment row. See section 12.7.1.
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
- globally UNIQUE `Payment.IdempotencyKey` (`uniqueidentifier NOT NULL`); a database unique constraint/index is the final protection against concurrent same-key attempt creation.
- one active Policy row.
- CHECK non-negative monetary values.
- CHECK Discount percentage 0..100.
- CHECK month ranges.
- CHECK legal status/type values.

## 9.9 Stored Procedure Catalogue

| Procedure | Primary responsibility | Atomic |
|---|---|---:|
| `usp_CreateReservation` | capacity check + Reservation + Deposit Invoice | Yes |
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

## 9.11 Scheduled Jobs

| Job | Purpose |
|---|---|
| `job_ExpirePendingReservations` | cancel unpaid expired Reservation |
| `job_ProcessReservationNoShow` | handle handover no-show/incomplete visit |
| `job_GenerateMonthlyInvoices` | create missing Rental Fee invoices strictly after Contract.StartMonth; never recreate a first-month charge |
| `job_MarkOverdueInvoices` | mark overdue using captured Policy |
| `job_RecalculateOpenLateFees` | refresh current LateFee |
| `job_RetryNotifications` | retry PENDING notifications |

SQL Server Agent may be used where available; otherwise the `Frms.Api` application scheduler invokes Business services that reach the same authoritative procedures.

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

The following is the API-level authorization baseline.

| Capability | Customer | Facility Staff | Facility Manager | BOM | Administrator |
|---|---:|---:|---:|---:|---:|
| Own account/profile | Limited | Limited | Limited | Limited | View all basic accounts |
| Browse active Facility/UnitType | Yes | Read | Read | Read/manage | Read as needed |
| Reservation create/cancel | Own | No | No | No | No |
| Reservation read/list | Own | Same Facility detail; OPS-001 work list | Same Facility list/detail | System list/detail | No |
| Visit create | Own where applicable | No customer creation | Monitor | Read | No |
| Visit read/list | Own | Same Facility detail; OPS-001 work list | Same Facility list/detail | System list/detail | No |
| Visit check-in/out | No | Same Facility | Monitor | No | No |
| Complete Handover | No | Same Facility; command acknowledges offline first-month receipt | Unit selection/monitor | No | No |
| StorageUnit management | No | Operational handling only | Same Facility | Read/monitor | No |
| Contract read/list | Own | Same Facility detail | Same Facility list/detail | System list/detail | No |
| Contract renewal | Own request | No | Monitor | Monitor | No |
| Invoice/Payment | Own view/pay | Same Facility read/verify only | Same Facility read/monitor | System read/report | No manual PAID |
| Policy | Read if exposed | Read | Read | Create version | No |
| UnitType price/master | Read | Read | Read | Manage | No |
| Discount | Own read/eligible usage | Read for Customer with same-Facility Reservation/Contract | Same-Facility Customer read | Manage | No |
| ExtraFeeType catalogue | No | ACTIVE types read | ACTIVE types read | Read/manage | No |
| Inspection | Own return progress | Claim/perform same Facility | Monitor same Facility | Read | No |
| Damage/ExtraFee | Own settlement read | Record during authorized Inspection | Monitor | Manage type masters | No |
| SupportTicket | Own create/view/cancel | Process assigned | Assign/monitor same Facility | Read/monitor | No |
| Employee/account administration | No | No | No | No | Yes |
| LoginHistory/AuditLog | No requirement | No | No | No | View |

## 10.7 Facility Authorization

Facility Staff and Facility Manager MUST be scoped to exactly their assigned Facility.

For every facility-scoped read or command:

```text
actor.Employee.FacilityId
MUST equal
targetResource.FacilityId
```

Failure -> `403 FORBIDDEN`.

Facility scope MUST be checked server-side even if FE routes hide other Facilities.

For Reservation detail, the resource Facility is Reservation.FacilityId. For Contract detail, it is Contract.FacilityId. For Visit detail and Visit work items, resolve Visit.EntityId by VisitType: RESERVATION -> Reservation.FacilityId; ACCESS/RETURN -> Contract.FacilityId. Do not treat every EntityId as a ReservationId or trust a client-supplied Facility identifier.

RES-003, CON-002 and VIS-004 require an authenticated ACTIVE actor. Staff access additionally requires a current Employee assignment to the resource Facility. Staff may read different Customers' resources within that Facility; Staff is not evaluated under Customer-own-resource semantics. Cross-Facility access returns 403 FORBIDDEN. A nonexistent detail resource returns 404 RESOURCE_NOT_FOUND. Read permission does not authorize a state transition or bypass command-specific lifecycle checks.

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
Stored Procedures:
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

`CWP-02` is optional and is not a core release blocker unless explicitly promoted.

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

# 12. Business Rules and Lifecycle Specification


## 12.1 Business Rule Catalogue

The following rule IDs are canonical for FRMS Release 1:

### Account / Employee / Facility

- `BR-ACC-01` Inactive account cannot log in or initiate new actions.
- `BR-ACC-02` Customer with CONFIRMED Reservation or ACTIVE Contract cannot be deactivated.
- `BR-EMP-01` Facility Staff/Manager require one Facility; global employee roles do not.
- `BR-FAC-01` INACTIVE Facility cannot accept new Reservation.

### Reservation

- `BR-RES-01` StartMonth must be future and EndMonth >= StartMonth.
- `BR-RES-02` Reservation holds capacity by Facility + UnitType + month range, not StorageUnit.
- `BR-RES-03` capacity check + Reservation creation is atomic.
- `BR-RES-04` Deposit is UnitType price snapshot.
- `BR-RES-05` Deposit cannot use Discount.
- `BR-RES-06` Reservation confirms only after Deposit payment succeeds.
- `BR-RES-07` RESERVATION Visit date must satisfy captured Policy.
- `BR-RES-08` Facility/UnitType/StartMonth/EndMonth are immutable after creation.

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
- `BR-NOT-01` failed notification remains PENDING and may retry.
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

```text
PENDING -> SENT
PENDING -> PENDING // delivery failure, retry later
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
3. Store the key in `Payment.IdempotencyKey` as `uniqueidentifier NOT NULL`. It is globally unique across all Invoices and Customers, not only within an Invoice or Customer, and remains stored unchanged for the lifetime of the Payment row.
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

Required verification includes SQL rejection of null/duplicate keys, same-key reuse, different-Invoice/cross-Customer non-disclosure, concurrent same-key creation, different-key new attempts, retained terminal states, expired-session handling and configuration failure/cancellation without key consumption. Database uniqueness and concurrency must be proved using real SQL Server, not EF InMemory; listing these requirements does not claim the tests have run.

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

# 13. API Contract Catalogue


## 13.1 API Design Status

`DESIGN-LOCKED — V10 FINAL`

This SRS preserves the approved API surface for BE–FE implementation. Endpoints wrap approved business capabilities; they do not introduce additional business lifecycles.

Base contract:

```text
Base path: /api/v1
Content-Type: application/json
JSON property naming: camelCase
Enum serialization: string
Timestamp: UTC ISO-8601
Calendar date: YYYY-MM-DD
Month value: YYYY-MM
Money: JSON number backed by decimal
```

## 13.2 Resource / Command Convention

Use resource endpoints for read/create/update and explicit command endpoints for state transitions.

Preferred:

```text
POST /reservations/{id}/confirm
POST /contracts/{id}/renew
POST /inspections/{id}/claim
```

Avoid generic mutation such as:

```text
PATCH /entity/status
```

when a business command has explicit semantics.

## 13.3 Common Success Envelope

```json
{
  "data": {},
  "message": "Operation completed successfully."
}
```

For commands returning no business payload, `data` MAY be `null`.

## 13.4 Common Collection Envelope

```json
{
  "data": [],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 125,
    "totalPages": 7
  }
}
```

## 13.5 Common Error Envelope

```json
{
  "code": "CAPACITY_NOT_AVAILABLE",
  "message": "Storage capacity is not available for the requested period.",
  "traceId": "00-..."
}
```

## 13.6 Validation Error Envelope

```json
{
  "code": "VALIDATION_ERROR",
  "message": "Request validation failed.",
  "errors": {
    "startMonth": [
      "Start month must be after the current business month."
    ]
  },
  "traceId": "00-..."
}
```

## 13.7 Pagination / Filtering / Sorting

`DESIGN-LOCKED — V10 FINAL`

Common list query:

```text
?page=1&pageSize=20
```

Rules:

- default `page = 1`;
- default `pageSize = 20`;
- maximum `pageSize = 100`;
- invalid pagination -> `400 VALIDATION_ERROR`;
- endpoint-specific filters are explicitly documented;
- `sortBy` may only reference endpoint-approved fields;
- `sortDirection` is `asc` or `desc`;
- unsupported sort field -> `400 VALIDATION_ERROR`.

## 13.8 HTTP Status Mapping

| HTTP | Use |
|---:|---|
| `200` | Successful read/command |
| `201` | Resource created |
| `204` | Successful command with no body where explicitly chosen |
| `400` | DTO/format/date/range validation |
| `401` | Missing/invalid authentication or invalid login credentials |
| `403` | Authenticated but account/role/facility/resource access denied |
| `404` | Accessible resource not found |
| `409` | Current-state, uniqueness, concurrency, capacity or duplicate logical-operation conflict |
| `500` | Unexpected internal error |
| `502` | External provider returned invalid/unusable response |
| `503` | Required external provider temporarily unavailable |

Canonical examples:

```text
INVALID_MONTH_RANGE              -> 400
VISIT_DATE_OUT_OF_POLICY         -> 400
ACCOUNT_INACTIVE                 -> 403
CAPACITY_NOT_AVAILABLE           -> 409
RESERVATION_INVALID_STATUS       -> 409
UNIT_NOT_AVAILABLE               -> 409
INSPECTION_ALREADY_CLAIMED       -> 409
EMAIL_ALREADY_EXISTS             -> 409
PHONE_NUMBER_ALREADY_EXISTS      -> 409
```

## 13.9 Standard API-Level Technical Codes

The SRS adds technical API codes that do not alter business semantics:

```text
AUTH_INVALID_CREDENTIALS
UNAUTHORIZED
FORBIDDEN
RESOURCE_NOT_FOUND
VALIDATION_ERROR
EXTERNAL_PROVIDER_UNAVAILABLE
EXTERNAL_PROVIDER_ERROR
```

Business codes from Section 12.8 remain authoritative for business failures.

## 13.10 Canonical JSON Schemas

### 13.10.1 AuthTokenResponse

```json
{
  "data": {
    "accessToken": "<jwt>",
    "tokenType": "Bearer",
    "user": {
      "userAccountId": "uuid",
      "role": "CUSTOMER",
      "status": "ACTIVE"
    }
  },
  "message": "Login successful."
}
```

`expiresAt` is intentionally not locked until token lifetime is decided.

### 13.10.2 FacilitySummary

```json
{
  "facilityId": "uuid",
  "name": "District 7 Facility",
  "address": "Ho Chi Minh City",
  "contactInfo": "string",
  "description": "string",
  "status": "ACTIVE"
}
```

### 13.10.3 UnitTypeAvailability

```json
{
  "unitTypeId": "uuid",
  "name": "Medium Private",
  "mode": "PRIVATE",
  "size": "string",
  "rentalPrice": 1500000.00,
  "description": "string",
  "requestedPeriod": {
    "startMonth": "2026-11",
    "endMonth": "2027-01"
  },
  "availableCapacity": 3
}
```

`availableCapacity` is server-authoritative.

### 13.10.4 ReservationDetail

```json
{
  "reservationId": "uuid",
  "facilityId": "uuid",
  "unitTypeId": "uuid",
  "policyId": "uuid",
  "startMonth": "2026-11",
  "endMonth": "2027-01",
  "lockedRentalPrice": 1500000.00,
  "depositAmount": 1500000.00,
  "status": "PENDING_DEPOSIT",
  "depositInvoice": {
    "invoiceId": "uuid",
    "status": "UNPAID",
    "amountDue": 1500000.00,
    "dueDate": "2026-10-02T15:30:00Z"
  },
  "reservationVisit": null,
  "createdAt": "2026-10-02T14:30:00Z"
}
```

### 13.10.5 VisitDetail

```json
{
  "visitId": "uuid",
  "entityId": "uuid",
  "visitType": "ACCESS",
  "visitDate": "2026-11-03",
  "actualReturnDate": null,
  "status": "SCHEDULED",
  "employeeId": null
}
```

### 13.10.6 ContractDetail

```json
{
  "contractId": "uuid",
  "reservationId": "uuid",
  "customerId": "uuid",
  "facilityId": "uuid",
  "storageUnitId": "uuid",
  "policyId": "uuid",
  "discountId": null,
  "startMonth": "2026-11",
  "endMonth": "2027-01",
  "status": "ACTIVE",
  "extensions": []
}
```

### 13.10.7 InvoiceDetail

```json
{
  "invoiceId": "uuid",
  "entityId": "uuid",
  "invoiceType": "RENTAL_FEE",
  "billingMonth": "2026-12",
  "baseAmount": 1500000.00,
  "discountId": null,
  "discountAmount": 0.00,
  "amountDue": 1500000.00,
  "dueDate": "2026-12-05T16:59:59Z",
  "status": "UNPAID",
  "paidAt": null
}
```

Business due-day interpretation is GMT+7 even though the timestamp is serialized in UTC.

For `RENTAL_FEE`, the configured `MonthlyPaymentDueDay` is the final non-overdue business day and `OverdueStartDay` begins at 00:00 GMT+7 on the configured overdue day. If `DueDate` is persisted/serialized as an instant, it MUST represent the end of the due business day consistently. For `DEPOSIT`, `DueDate` is the timeout instant derived from `DepositTimeoutHours`.

### 13.10.8 PaymentDetail

```json
{
  "paymentId": "uuid",
  "invoiceId": "uuid",
  "amount": 1500000.00,
  "paymentMethod": "PAYOS",
  "transactionCode": null,
  "status": "PENDING",
  "paidAt": null,
  "createdAt": "2026-10-02T14:31:00Z"
}
```

### 13.10.9 InspectionDetail

```json
{
  "inspectionId": "uuid",
  "contractId": "uuid",
  "storageUnitId": "uuid",
  "visitId": "uuid",
  "employeeId": null,
  "status": "PENDING",
  "conditionNote": null,
  "damages": [],
  "extraFees": [],
  "evidence": [],
  "completedAt": null
}
```

### 13.10.10 SupportTicketDetail

```json
{
  "supportTicketId": "uuid",
  "contractId": "uuid",
  "customerId": "uuid",
  "assignedEmployeeId": null,
  "category": "PAYMENT_ISSUE",
  "description": "string",
  "status": "OPEN",
  "resultNote": null,
  "createdAt": "2026-10-02T14:30:00Z",
  "completedAt": null
}
```

## 13.11 Endpoint Registry

| API ID | Method | Endpoint | Description | Allowed role(s) | Feature |
|---|---|---|---|---|---|
| `AUTH-001` | `POST` | `/api/v1/auth/customer/register` | Customer self-registration | Anonymous | Supporting / CWP |
| `AUTH-002` | `POST` | `/api/v1/auth/customer/login` | Customer login by PhoneNumber | Anonymous | Auth |
| `AUTH-003` | `POST` | `/api/v1/auth/employee/login` | Employee login by Email | Anonymous | Auth |
| `AUTH-004` | `GET` | `/api/v1/auth/me` | Get current authenticated account/profile | Authenticated | Auth |
| `AUTH-005` | `PATCH` | `/api/v1/customers/me/profile` | Update limited Customer profile fields | CUSTOMER | Supporting |
| `CAT-001` | `GET` | `/api/v1/facilities` | Browse active Facilities | CUSTOMER | CWP-01 |
| `CAT-002` | `GET` | `/api/v1/facilities/{facilityId}` | Facility detail | CUSTOMER | CWP-01 |
| `CAT-003` | `GET` | `/api/v1/facilities/{facilityId}/unit-types` | Unit Types and optional requested-period capacity | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER | CWP-01 / operational read |
| `CAT-004` | `GET` | `/api/v1/unit-types/{unitTypeId}` | Unit Type detail | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER | CWP-01 / operational read |
| `AI-001` | `POST` | `/api/v1/ai/unit-type-recommendations` | Optional AI Size Guide | CUSTOMER | CWP-02 |
| `RES-001` | `POST` | `/api/v1/reservations` | Create Reservation + Deposit Invoice | CUSTOMER | CWP-03 |
| `RES-002` | `GET` | `/api/v1/reservations` | Customer own / Manager Facility / BOM system Reservation list | CUSTOMER,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-04 / operational monitoring |
| `RES-003` | `GET` | `/api/v1/reservations/{reservationId}` | Authorized own / Facility / system Reservation detail | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-04 / FWP-02/03 / operational monitoring |
| `RES-004` | `POST` | `/api/v1/reservations/{reservationId}/confirm` | Confirm after paid Deposit and create RESERVATION Visit | CUSTOMER | CWP-04/CWP-05 |
| `RES-005` | `POST` | `/api/v1/reservations/{reservationId}/cancel` | Cancel eligible Reservation | CUSTOMER | CWP-04 |
| `BIL-001` | `GET` | `/api/v1/invoices` | Authorized own / Facility / system Invoice list | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-08/CWP-11 / operational monitoring |
| `BIL-002` | `GET` | `/api/v1/invoices/{invoiceId}` | Authorized own / Facility / system Invoice detail | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-08/CWP-11 / operational monitoring |
| `PAY-001` | `POST` | `/api/v1/invoices/{invoiceId}/payments/payos` | Start payOS payment for Deposit/Rental Invoice | CUSTOMER | CWP-04/CWP-08 |
| `PAY-003` | `GET` | `/api/v1/payments/{paymentId}` | Authorized own / Facility / system Payment status | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-08 / operational verification |
| `PAY-004` | `POST` | `/api/v1/payments/payos/webhook` | payOS provider webhook | EXTERNAL | EPS-01/SSP-07 |
| `CON-001` | `GET` | `/api/v1/contracts` | Customer own / Manager Facility / BOM system Contract list | CUSTOMER,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-06 / operational monitoring |
| `CON-002` | `GET` | `/api/v1/contracts/{contractId}` | Authorized own / Facility / system Contract detail | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-06 / operational read |
| `CON-003` | `POST` | `/api/v1/contracts/{contractId}/renew` | Renew active Contract | CUSTOMER | CWP-09 |
| `CON-004` | `GET` | `/api/v1/contracts/{contractId}/billing` | Authorized Contract billing/overdue summary | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-08/CWP-11 / operational monitoring |
| `CON-005` | `GET` | `/api/v1/contracts/{contractId}/return-summary` | Authorized return/inspection/settlement summary | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-10/CWP-11 / operational monitoring |
| `VIS-001` | `POST` | `/api/v1/contracts/{contractId}/access-visits` | Create ACCESS Visit | CUSTOMER | CWP-07 |
| `VIS-002` | `POST` | `/api/v1/contracts/{contractId}/return-visits` | Create RETURN Visit | CUSTOMER | CWP-10 |
| `VIS-003` | `GET` | `/api/v1/visits` | Customer own / Manager Facility / BOM system Visit list | CUSTOMER,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-05/CWP-07/CWP-10 / operational monitoring |
| `VIS-004` | `GET` | `/api/v1/visits/{visitId}` | Authorized own / Facility / system Visit detail | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-05/CWP-07/CWP-10 / FWP-02..05 |
| `VIS-005` | `PATCH` | `/api/v1/visits/{visitId}/schedule` | Reschedule eligible SCHEDULED Visit | CUSTOMER | CWP-05/CWP-07/CWP-10 |
| `VIS-006` | `POST` | `/api/v1/visits/{visitId}/cancel` | Cancel eligible SCHEDULED Visit | CUSTOMER | CWP-05/CWP-07/CWP-10 |
| `OPS-001` | `GET` | `/api/v1/staff/work-items` | Derived daily Staff work list | FACILITY_STAFF | FWP-01 |
| `OPS-002` | `POST` | `/api/v1/visits/{visitId}/check-in` | Check in Reservation/Access/Return Visit | FACILITY_STAFF | FWP-02/FWP-04/FWP-05 |
| `OPS-003` | `POST` | `/api/v1/visits/{visitId}/check-out` | Check out eligible non-handover Visit | FACILITY_STAFF | FWP-04 |
| `OPS-004` | `POST` | `/api/v1/reservations/{reservationId}/complete-handover` | Atomic Complete Handover | FACILITY_STAFF | FWP-03/SSP-08 |
| `OPS-005` | `POST` | `/api/v1/visits/{visitId}/confirm-return` | Confirm ActualReturnDate and create Inspection | FACILITY_STAFF | FWP-05/SSP-12 |
| `INS-001` | `GET` | `/api/v1/inspections` | List facility-scoped Inspections | FACILITY_STAFF,FACILITY_MANAGER | FWP-06/MWP-04 |
| `INS-002` | `GET` | `/api/v1/inspections/{inspectionId}` | Inspection detail | FACILITY_STAFF,FACILITY_MANAGER | FWP-06/MWP-04 |
| `INS-003` | `POST` | `/api/v1/inspections/{inspectionId}/claim` | Atomic Inspection claim | FACILITY_STAFF | FWP-06/SSP-13 |
| `INS-004` | `POST` | `/api/v1/inspections/{inspectionId}/damages` | Record DamageRecord | FACILITY_STAFF | FWP-07 |
| `INS-005` | `POST` | `/api/v1/inspections/{inspectionId}/extra-fees` | Record ExtraFee | FACILITY_STAFF | FWP-07 |
| `INS-006` | `POST` | `/api/v1/inspections/{inspectionId}/evidence` | Upload InspectionEvidence | FACILITY_STAFF | FWP-06 |
| `INS-007` | `POST` | `/api/v1/inspections/{inspectionId}/complete` | Complete Inspection; keep StorageUnit INSPECTION | FACILITY_STAFF | FWP-06 |
| `INS-008` | `POST` | `/api/v1/contracts/{contractId}/finalize-return` | Atomic settlement, terminal Contract and Unit release | FACILITY_STAFF | FWP-07/SSP-14 |
| `INS-009` | `POST` | `/api/v1/damage-records/{damageRecordId}/decision` | Approve or reject PENDING DamageRecord | FACILITY_MANAGER | MWP-04 |
| `INS-010` | `GET` | `/api/v1/damage-types` | List active seeded DamageTypes for inspection use | FACILITY_STAFF,FACILITY_MANAGER | FWP-07/MWP-04 |
| `SUP-001` | `POST` | `/api/v1/support-tickets` | Create Contract-related SupportTicket | CUSTOMER | CWP-12 |
| `SUP-002` | `GET` | `/api/v1/support-tickets` | List accessible SupportTickets | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER | CWP-12/FWP-08/MWP-05 |
| `SUP-003` | `GET` | `/api/v1/support-tickets/{ticketId}` | SupportTicket detail | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER | CWP-12/FWP-08/MWP-05 |
| `SUP-004` | `POST` | `/api/v1/support-tickets/{ticketId}/cancel` | Cancel eligible SupportTicket | CUSTOMER | CWP-12 |
| `SUP-005` | `POST` | `/api/v1/support-tickets/{ticketId}/assign` | Assign Facility Staff | FACILITY_MANAGER | MWP-05 |
| `SUP-006` | `POST` | `/api/v1/support-tickets/{ticketId}/complete` | Complete assigned SupportTicket | FACILITY_STAFF | FWP-08 |
| `UNIT-001` | `GET` | `/api/v1/facilities/{facilityId}/storage-units` | List facility StorageUnits | FACILITY_MANAGER | MWP-01/MWP-02 |
| `UNIT-002` | `POST` | `/api/v1/facilities/{facilityId}/storage-units` | Create StorageUnit | FACILITY_MANAGER | MWP-02 |
| `UNIT-003` | `GET` | `/api/v1/storage-units/{storageUnitId}` | StorageUnit detail | FACILITY_MANAGER | MWP-01/MWP-02 |
| `UNIT-004` | `PATCH` | `/api/v1/storage-units/{storageUnitId}` | Update UnitType/location fields allowed by Manager | FACILITY_MANAGER | MWP-02 |
| `UNIT-005` | `POST` | `/api/v1/storage-units/{storageUnitId}/status` | Operational status transition | FACILITY_MANAGER | MWP-02 |
| `BOM-001` | `GET` | `/api/v1/business/facilities` | List all Facilities for BOM | BUSINESS_OPERATIONS_MANAGER | BWP-01/BWP-06 |
| `BOM-002` | `POST` | `/api/v1/business/facilities` | Create Facility | BUSINESS_OPERATIONS_MANAGER | BWP-01 |
| `BOM-003` | `PATCH` | `/api/v1/business/facilities/{facilityId}` | Update Facility basic information | BUSINESS_OPERATIONS_MANAGER | BWP-01 |
| `BOM-004` | `POST` | `/api/v1/business/facilities/{facilityId}/activate` | Activate Facility | BUSINESS_OPERATIONS_MANAGER | BWP-01 |
| `BOM-005` | `POST` | `/api/v1/business/facilities/{facilityId}/deactivate` | Deactivate Facility | BUSINESS_OPERATIONS_MANAGER | BWP-01 |
| `BOM-006` | `GET` | `/api/v1/business/unit-types` | List global UnitTypes | BUSINESS_OPERATIONS_MANAGER | BWP-02 |
| `BOM-007` | `PATCH` | `/api/v1/business/unit-types/{unitTypeId}/price` | Update current UnitType RentalPrice | BUSINESS_OPERATIONS_MANAGER | BWP-02 |
| `BOM-008` | `GET` | `/api/v1/business/policies` | List Policy versions | BUSINESS_OPERATIONS_MANAGER | BWP-03 |
| `BOM-009` | `POST` | `/api/v1/business/policies` | Create new Policy version | BUSINESS_OPERATIONS_MANAGER | BWP-03 |
| `BOM-010` | `GET` | `/api/v1/business/customers/{customerId}/discounts` | Own / Facility operational / BOM management Customer Discount list | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | BWP-04 / FWP-03 / MWP-01 |
| `BOM-011` | `POST` | `/api/v1/business/customers/{customerId}/discounts` | Create Customer Discount | BUSINESS_OPERATIONS_MANAGER | BWP-04 |
| `BOM-012` | `PATCH` | `/api/v1/business/discounts/{discountId}` | Update allowed Discount master fields | BUSINESS_OPERATIONS_MANAGER | BWP-04 |
| `BOM-013` | `GET` | `/api/v1/business/extra-fee-types` | BOM management / Staff-Manager active operational ExtraFeeType list | FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | BWP-05 / FWP-07 |
| `BOM-014` | `PATCH` | `/api/v1/business/extra-fee-types/{extraFeeTypeId}` | Update ExtraFeeType default amount/status | BUSINESS_OPERATIONS_MANAGER | BWP-05 |
| `REP-001` | `GET` | `/api/v1/reports/facilities/{facilityId}/operations` | Facility operations report | FACILITY_MANAGER | MWP-06 |
| `REP-002` | `GET` | `/api/v1/reports/facilities/{facilityId}/revenue` | Facility revenue report | FACILITY_MANAGER | MWP-06 |
| `REP-003` | `GET` | `/api/v1/reports/business/overview` | System-wide business overview | BUSINESS_OPERATIONS_MANAGER | BWP-06/BWP-07 |
| `REP-004` | `GET` | `/api/v1/reports/business/export` | Export system-wide report | BUSINESS_OPERATIONS_MANAGER | BWP-07 |
| `ADM-001` | `GET` | `/api/v1/admin/users` | List user accounts | SYSTEM_ADMINISTRATOR | AWP-01 |
| `ADM-002` | `GET` | `/api/v1/admin/users/{userAccountId}` | User/account profile detail | SYSTEM_ADMINISTRATOR | AWP-01 |
| `ADM-003` | `POST` | `/api/v1/admin/customers/{customerId}/activate` | Activate Customer account | SYSTEM_ADMINISTRATOR | AWP-02 |
| `ADM-004` | `POST` | `/api/v1/admin/customers/{customerId}/deactivate` | Deactivate Customer with lifecycle guard | SYSTEM_ADMINISTRATOR | AWP-02 |
| `ADM-005` | `POST` | `/api/v1/admin/employees` | Create Employee account/profile | SYSTEM_ADMINISTRATOR | AWP-03 |
| `ADM-006` | `PATCH` | `/api/v1/admin/employees/{employeeId}` | Update Employee basic profile | SYSTEM_ADMINISTRATOR | AWP-03 |
| `ADM-007` | `POST` | `/api/v1/admin/employees/{employeeId}/activate` | Activate Employee | SYSTEM_ADMINISTRATOR | AWP-03 |
| `ADM-008` | `POST` | `/api/v1/admin/employees/{employeeId}/deactivate` | Deactivate Employee | SYSTEM_ADMINISTRATOR | AWP-03 |
| `ADM-009` | `PUT` | `/api/v1/admin/employees/{employeeId}/assignment` | Assign role and Facility | SYSTEM_ADMINISTRATOR | AWP-04/AWP-05 |
| `ADM-010` | `GET` | `/api/v1/admin/login-history` | Search LoginHistory | SYSTEM_ADMINISTRATOR | AWP-06 |
| `ADM-011` | `GET` | `/api/v1/admin/audit-logs` | Search AuditLog | SYSTEM_ADMINISTRATOR | AWP-07 |
| `ADM-012` | `POST` | `/api/v1/admin/employees/{employeeId}/resend-initial-credential` | Regenerate and resend initial password for INACTIVE Employee | SYSTEM_ADMINISTRATOR | AWP-03 |

### 13.11.1 Shared Operational Read Authorization

The role sets in section 13.11 are authoritative for each endpoint; a broad role in the actor matrix does not grant access to an unlisted endpoint. In particular, Staff cannot call RES-002, CON-001 or VIS-003; OPS-001 supplies the Staff work list. Manager and BOM can use those lists and their detail endpoints. SYSTEM_ADMINISTRATOR receives no business-read access through this revision.

For the revised reads:

| Read family | Customer scope | Staff/Manager scope | BOM scope |
|---|---|---|---|
| Reservation / Contract detail | Own resource | Assigned Facility; endpoint role required | All Facilities |
| Reservation / Contract / Visit lists | Own resources | Manager assigned Facility; Staff not allowed | All Facilities |
| Visit detail | Owned referenced Reservation/Contract | Facility of referenced Reservation/Contract | All Facilities |
| Invoice list/detail | Owned referenced Reservation/Contract | Facility of referenced Reservation/Contract | All Facilities |
| Payment detail | Owned linked Invoice resource | Facility of linked Invoice resource | All Facilities |
| Contract billing/return summary | Own Contract | Contract in assigned Facility | All Facilities |
| Customer Discounts (BOM-010) | Path Customer must be actor's Customer | Path Customer must have an existing Reservation or Contract in assigned Facility | All Customers |
| ExtraFeeType catalogue (BOM-013) | Not allowed | ACTIVE-account Staff/Manager with valid Facility assignment; ACTIVE types only | Full management catalogue |

Collection filtering MUST occur server-side before pagination/counting so neither rows nor totalItems leak another Customer/Facility. Any query filter may narrow the actor's authorized set, never widen it. Facility/Customer IDs in paths or queries are resource identifiers, not authorization evidence.

Invoice Facility/ownership is resolved by InvoiceType: DEPOSIT -> Invoice.EntityId -> Reservation; RENTAL_FEE -> Invoice.EntityId -> Contract. Payment read resolves Payment.InvoiceId -> Invoice -> the same resource scope. A legacy unlinked Payment must not be granted Customer/Facility access by guessing an association; preservation/deployment handling remains governed by section 0.0.

All reads require ACTIVE account and authoritative current role/assignment. Wrong Facility or endpoint role -> 403 FORBIDDEN; nonexistent detail resource -> 404 RESOURCE_NOT_FOUND. Customer ownership denial retains section 10.8's consistent security design. Employees do not inherit Customer create/cancel/reschedule/renew/payment commands from GET access. Immutable money/status remains server-owned. Listing Discount or ExtraFeeType rows does not grant their write endpoints.

## 13.12 Authentication API Contracts

### AUTH-001 — Customer Registration

```text
POST /api/v1/auth/customer/register
Authentication: none
```

Request:

```json
{
  "fullName": "Nguyen Van A",
  "phoneNumber": "0900000000",
  "email": "customer@example.com",
  "password": "<plaintext only in request transport>",
  "address": "Ho Chi Minh City",
  "cccd": null
}
```

Field rules:

| Field | Required | Rule |
|---|---:|---|
| `fullName` | Yes | Customer profile name |
| `phoneNumber` | Yes | UNIQUE login identifier |
| `email` | Yes | UNIQUE account email |
| `password` | Yes | hashed with BCrypt before persistence |
| `address` | No | profile field |
| `cccd` | No | unique when present |

Success: `201`, returns account/customer identifiers without `PasswordHash`.

`DESIGN-LOCKED — V9 FINAL`: Customer self-registration creates:

```text
Role   = CUSTOMER
Status = ACTIVE
```

Rationale: current approved scope has Customer self-registration but no approval/email-verification activation workflow. If a future approval/verification workflow is introduced, this rule must be revised explicitly.

Errors:

```text
EMAIL_ALREADY_EXISTS        -> 409
PHONE_NUMBER_ALREADY_EXISTS -> 409
VALIDATION_ERROR            -> 400
```

### AUTH-002 — Customer Login

Request:

```json
{
  "phoneNumber": "0900000000",
  "password": "..."
}
```

Success: `AuthTokenResponse`.

Errors:

```text
AUTH_INVALID_CREDENTIALS -> 401
ACCOUNT_INACTIVE         -> 403
```

### AUTH-003 — Employee Login

Request:

```json
{
  "email": "staff@example.com",
  "password": "..."
}
```

Success: `AuthTokenResponse`.

The resolved role MUST be one of the four employee roles.

### AUTH-004 — Current Account

Response example:

```json
{
  "data": {
    "userAccountId": "uuid",
    "role": "FACILITY_STAFF",
    "status": "ACTIVE",
    "email": "staff@example.com",
    "phoneNumber": "0900000001",
    "profile": {
      "employeeId": "uuid",
      "fullName": "Staff A",
      "facilityId": "uuid"
    }
  }
}
```

### AUTH-005 — Update Customer Profile

Request:

```json
{
  "fullName": "Updated Name",
  "address": "Updated address",
  "cccd": "optional"
}
```

V9 does not allow this endpoint to change login `PhoneNumber` or account `Email`; credential-identifier change is governed by `DD-07`.

## 13.13 Catalog / AI API Contracts

### CAT-001 — Browse Facilities

```text
GET /api/v1/facilities?page=1&pageSize=20
```

Response: collection of `FacilitySummary`. Only `ACTIVE` Facilities are exposed to Customer browsing.

### CAT-003 — Unit Types and Capacity

```text
GET /api/v1/facilities/{facilityId}/unit-types
    ?startMonth=2026-11
    &endMonth=2027-01
    &page=1
    &pageSize=20
```

When both month parameters are supplied, each item includes server-authoritative `availableCapacity`.

Authorization:

- Customer may read active Facility/UnitType catalogue.
- Facility Staff/Manager may use this endpoint for operational UnitType read; Facility scope MUST be enforced for the path Facility.
- BOM uses the business catalogue endpoint (`BOM-006`) for global management views.

Errors:

```text
INVALID_MONTH_RANGE -> 400
FACILITY_INACTIVE   -> 409 when operation requires active Facility
```

### AI-001 — AI Size Guide

Request:

```json
{
  "storageDescription": "Boxes, books, two small shelves",
  "preferredMode": "PRIVATE"
}
```

Response:

```json
{
  "data": {
    "recommendedUnitTypeId": "uuid",
    "reason": "Short recommendation reason.",
    "alternatives": [
      {
        "unitTypeId": "uuid",
        "reason": "Alternative reason."
      }
    ]
  }
}
```

AI response MUST be validated against existing UnitTypes. If provider fails:

```text
EXTERNAL_PROVIDER_UNAVAILABLE -> 503
```

Customer can continue manual selection.

## 13.14 Reservation API Contracts

### RES-001 — Create Reservation

Request:

```json
{
  "facilityId": "uuid",
  "unitTypeId": "uuid",
  "startMonth": "2026-11",
  "endMonth": "2027-01"
}
```

Success: `201` using `ReservationDetail`.

Server-owned fields MUST NOT be accepted from FE:

```text
customerId
policyId
lockedRentalPrice
depositAmount
status
createdAt
```

Errors:

```text
FACILITY_INACTIVE       -> 409
INVALID_MONTH_RANGE     -> 400
CAPACITY_NOT_AVAILABLE  -> 409
```

Business rules:

```text
BR-FAC-01
BR-RES-01..05
```

Procedure: `usp_CreateReservation`.

### RES-003 — Customer / Operational Reservation Detail

```text
GET /api/v1/reservations/{reservationId}
Roles: CUSTOMER, FACILITY_STAFF, FACILITY_MANAGER, BUSINESS_OPERATIONS_MANAGER
Success: 200 ApiResponse<ReservationDetail>
```

- Customer may read only an owned Reservation under section 10.8.
- Staff may read only a Reservation whose FacilityId equals the actor's authoritative Employee.FacilityId.
- Manager uses the same assigned-Facility restriction; BOM may read system-wide. RES-002 is a server-filtered list for Customer/Manager/BOM only, using the same schema and the standard collection envelope.
- Reuse the canonical ReservationDetail schema in section 13.10.4; lockedRentalPrice is the immutable Reservation snapshot, not current UnitType.RentalPrice.
- reservationVisit contains the related RESERVATION Visit summary with visitId, visitType, visitDate and status; it is null when no such Visit exists. VIS-004 supplies canonical Visit detail for a known VisitId.
- Before first-month receipt acknowledgment, Staff reads the locked amount from this API. FE must not derive the amount from catalogue pricing or a Discount.
- ACTIVE account, cross-Facility 403 FORBIDDEN and nonexistent-resource 404 RESOURCE_NOT_FOUND follow sections 10.7..10.9. Existing Customer ownership handling remains unchanged.

No new Contract/Reservation entity attribute or write permission is introduced.

### RES-004 — Confirm Reservation

Request:

```json
{
  "reservationVisitDate": "2026-11-03"
}
```

Success response:

```json
{
  "data": {
    "reservationId": "uuid",
    "status": "CONFIRMED",
    "reservationVisit": {
      "visitId": "uuid",
      "visitType": "RESERVATION",
      "visitDate": "2026-11-03",
      "status": "SCHEDULED"
    }
  },
  "message": "Reservation confirmed."
}
```

Errors:

```text
DEPOSIT_NOT_PAID          -> 409
VISIT_DATE_OUT_OF_POLICY  -> 400
RESERVATION_INVALID_STATUS-> 409
```

Procedure: `usp_ConfirmReservation`.

### RES-005 — Cancel Reservation

Request:

```json
{
  "reason": "Customer no longer needs storage."
}
```

`reason` is an application/audit note; it does not create a new lifecycle state.

Success returns authoritative Reservation state.

## 13.15 Invoice / Payment API Contracts

### BIL-001 / BIL-002 / PAY-003 — Authorized Invoice and Payment Reads

Reuse canonical InvoiceDetail/PaymentDetail and standard list/detail envelopes. BIL-001 returns a server-filtered paginated Invoice collection; BIL-002/PAY-003 return the authorized detail. Roles are CUSTOMER, FACILITY_STAFF, FACILITY_MANAGER and BUSINESS_OPERATIONS_MANAGER. Apply the Invoice/Payment relationship resolution in section 13.11.1, including Deposit -> Reservation and Rental Fee -> Contract.

Employee GET access can verify persisted payment/invoice state but cannot initiate PAY-001, submit a provider webhook as a trusted actor, mutate money or manually mark PAID/SUCCESS. Neither this read nor CON-004 creates a first-month Invoice/Payment. Expose existing financial status/detail only, not gateway secrets or technical provider payloads.

### PAY-001 — Start payOS Payment for Existing Invoice

`POST /api/v1/invoices/{invoiceId}/payments/payos`; ACTIVE CUSTOMER only, with authoritative ownership checks.
No client redirect URL or amount is accepted. The UUID `Idempotency-Key` header identifies one deliberate action; automatic retries reuse it and new deliberate retries use a new key.
The required storage, lifetime, concurrency, expiry and non-disclosure contract is section 12.7.1. Persist the header UUID unchanged; same-key retries never authorize a second Payment or provider session.

Response: `ApiResponse<{ paymentId, invoiceId, amount, paymentMethod: "PAYOS", status, paymentUrl }>`.
First available session returns 201 with Location; same-key retries return 200 with the same stored attempt.
PAY-003 may also return historical `MOMO`/`VNPAY` methods, but never session URL or idempotency key.
A key bound to another Invoice yields PAYMENT_IDEMPOTENCY_CONFLICT without disclosing its Payment.

Order: authenticate ACTIVE Customer -> load Invoice/ownership -> validate key -> resolve existing key/conflict -> validate new-attempt eligibility/integral positive VND amount -> local gateway configuration/preflight -> atomic CreateOrGet -> provider create only for Created -> persist session.
Existing authorized attempts bypass gateway configuration; terminal attempts stay terminal. Known configuration failures/cancellation do not consume a key or write FAILED. No database transaction spans a provider call.
New attempts are Deposit or Rental Fee with BillingMonth > Contract.StartMonth, Invoice UNPAID/OVERDUE, server-owned AmountDue.
Fractional or non-positive VND yields PAYMENT_AMOUNT_UNSUPPORTED; no rounding or auto-settlement.
ReturnUrl/CancelUrl are server configuration only. Browser return/cancel are not proof of success; refresh PAY-003/BIL-002. Cancel does not implicitly write FAILED.
Unknown provider outcome/timeout leaves PENDING; recovery of stranded attempts remains outside this replacement.

### First-month offline collection — no payment endpoint

The first rental month is collected offline before Staff invokes OPS-004. Successful Contract creation records settlement; no first-month Invoice or Payment is created, returned or linked.

PAY-002 is retired by REV-2026-10-07-FM-OFFLINE and removed from the current endpoint inventory. Its identifier must not be reused. Do not expose/call any old first-month gateway route, substitute PAY-001 for it, or fabricate an Invoice/Payment for Contract.StartMonth.

The amount is Reservation.LockedRentalPrice without Contract Discount. A Discount selected at handover may apply to eligible invoices strictly after Contract.StartMonth. Deposit and later Rental Fee invoices retain PAY-001/PAY-003/PAY-004.

Contract existence records Staff's offline-receipt acknowledgment, not gateway verification. No separate Payment SUCCESS status is required for the first month.

### PAY-004 — payOS Webhook

`POST /api/v1/payments/payos/webhook`, anonymous at JWT boundary, bounded JSON body. No browser-return mutation route exists.
Infrastructure verifies HMAC-SHA256 over alphabetically sorted `data` using official payment-request canonicalization (including null/arrays/types), constant-time comparison when implemented locally.
Reject invalid/malformed/duplicate-property payloads before repository access. Never log raw payload, signature, keys or complete checkoutUrl.
Require `code == "00"`, `success == true`, `data.code == "00"`, positive integral VND, valid reference, and parseable transactionDateTime.
Owner-approved timestamp interpretation: provider `yyyy-MM-dd HH:mm:ss` is UTC+7; convert exactly once to UTC, independently of server timezone.
Business resolves verified orderCode to the persisted PAYOS Payment and checks the persisted amount before entering `usp_ApplyPaymentResult`. Description is display-only and never authoritative identity.
A verified unknown order (including registration sample) returns HTTP200 with no Payment/Invoice mutation. Sequence starts at 1000 so the documented sample orderCode 123 cannot be allocated.
Applied/duplicate/terminal-conflict results acknowledge HTTP200 without repeated financial effects.
Invalid signature/payload -> controlled HTTP400; amount/reference conflicts -> controlled HTTP409; transient processing failure -> controlled HTTP503 with traceId (not acknowledged as successful).
Only verified results enter the idempotent SQL transaction. No registration call to /confirm-webhook or production call is permitted during this code/mock phase.

## 13.16 Contract / Visit API Contracts

### CON-002 — Customer / Operational Contract Detail

```text
GET /api/v1/contracts/{contractId}
Roles: CUSTOMER, FACILITY_STAFF, FACILITY_MANAGER, BUSINESS_OPERATIONS_MANAGER
Success: 200 ApiResponse<ContractDetail>
```

Customer reads an owned Contract; Staff/Manager read a Contract in the actor's assigned Facility; BOM may read system-wide. CON-001 is a server-filtered list for Customer/Manager/BOM only, using the canonical ContractDetail schema and standard collection envelope. Apply authoritative ACTIVE-account and resource/Facility checks, not a client-supplied customerId or facilityId. Nonexistent resource -> 404 RESOURCE_NOT_FOUND; wrong Staff/Manager Facility -> 403 FORBIDDEN.

Reuse section 13.10.6 unchanged: ContractDetail already has reservationId. To obtain the immutable first-month amount and associated RESERVATION Visit, follow that ID through RES-003; use VIS-004 for the required Visit detail. No lockedRentalPrice, Visit snapshot, paid flag or additional ReservationId property is added to the persisted Contract. OPS-004 supplies the contractId for reading the Contract after successful handover.

### VIS-004 — Customer / Operational Visit Detail

```text
GET /api/v1/visits/{visitId}
Roles: CUSTOMER, FACILITY_STAFF, FACILITY_MANAGER, BUSINESS_OPERATIONS_MANAGER
Success: 200 ApiResponse<VisitDetail>
```

Reuse section 13.10.5. Customer reads an owned Visit; Staff/Manager read a Visit whose referenced Reservation (RESERVATION) or Contract (ACCESS/RETURN) belongs to the assigned Facility; BOM may read system-wide. VIS-003 is a server-filtered list for Customer/Manager/BOM only. Visit date/type/status and EntityId are authoritative server data. Reading a historical Visit does not permit check-in, rescheduling, cancellation or handover outside the existing command rules. Wrong Staff/Manager Facility -> 403 FORBIDDEN; nonexistent resource -> 404 RESOURCE_NOT_FOUND.

### CON-004 / CON-005 — Authorized Billing and Return Summary Reads

CUSTOMER reads the owned Contract summary; FACILITY_STAFF/FACILITY_MANAGER read within the assigned Facility; BUSINESS_OPERATIONS_MANAGER reads system-wide. Apply the same Contract scope as CON-002 to every nested Invoice, return Visit, Inspection, fee and settlement summary. GET access neither changes summary calculations nor grants renewal/return/inspection/payment commands.

First-month rent is settled through Contract creation, not an unpaid missing Invoice; preserve the existing offline-first-month and subsequent-billing rules when producing billing/return summaries.

### CON-003 — Renew Contract

Request:

```json
{
  "newEndMonth": "2027-04"
}
```

Response:

```json
{
  "data": {
    "contractId": "uuid",
    "oldEndMonth": "2027-01",
    "newEndMonth": "2027-04",
    "appliedMonthlyPrice": 1650000.00,
    "status": "ACTIVE"
  },
  "message": "Contract renewed."
}
```

Errors:

```text
CONTRACT_NOT_ACTIVE             -> 409
RETURN_VISIT_PENDING            -> 409
RENEWAL_NOT_CONTIGUOUS          -> 400
RENEWAL_CAPACITY_NOT_AVAILABLE  -> 409
```

Procedure: `usp_RenewContract`.

### VIS-001 — Create ACCESS Visit

Request:

```json
{
  "visitDate": "2026-12-03"
}
```

Errors:

```text
CONTRACT_NOT_ACTIVE  -> 409
RETURN_VISIT_PENDING -> 409
```

### VIS-002 — Create RETURN Visit

Request:

```json
{
  "visitDate": "2027-01-28"
}
```

Creates `RETURN / SCHEDULED`.

### VIS-005 — Reschedule Visit

Request:

```json
{
  "visitDate": "2026-11-04"
}
```

Only `SCHEDULED` Visit may be rescheduled. Type-specific date restrictions still apply.

### VIS-006 — Cancel Visit

Request:

```json
{
  "reason": "Customer cancelled visit."
}
```

Errors:

```text
VISIT_INVALID_STATUS -> 409
```

## 13.17 Staff / Handover / Return API Contracts

### OPS-001 — Staff Work Items

Example query:

```text
GET /api/v1/staff/work-items?date=2026-11-03&page=1&pageSize=20
```

Response combines derived work items, not a persisted Task entity:

```json
{
  "data": [
    {
      "workType": "RESERVATION_VISIT",
      "referenceId": "uuid",
      "entityId": "uuid",
      "scheduledDate": "2026-11-03",
      "status": "SCHEDULED",
      "customer": {
        "customerId": "uuid",
        "fullName": "Nguyen Van A",
        "phoneNumber": "0900000000"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

Visit work-item semantics:

- For RESERVATION_VISIT, ACCESS_VISIT and RETURN_VISIT, referenceId is Visit.VisitId and entityId is Visit.EntityId. scheduledDate is Visit.VisitDate; status is Visit.Status.
- RESERVATION_VISIT entityId is a ReservationId; ACCESS_VISIT/RETURN_VISIT entityId is a ContractId, according to BR-VIS-02. The response includes the related Customer's customerId, fullName and phoneNumber for reception identification and authorized BOM-010 navigation, derived from existing resource/profile/account data. Do not expose full profiles, credentials or unrelated Customer records.
- The entire list and every nested Customer/resource field are derived only from resources in the authenticated Staff's assigned Facility. FE filters are not authorization.
- For non-Visit work items, preserve their existing workType/referenceId meanings; Visit-specific entityId/customer properties are omitted. A generic referenceId is not always a VisitId.
- A RESERVATION_VISIT opens VIS-004 by referenceId and RES-003 by entityId. ACCESS/RETURN items open VIS-004 and CON-002. No task/assignment entity or physical-unit reservation is created.
- After successful Check-in or Handover, and after a state-conflict response, FE refreshes authoritative work-item/detail state. It must not fabricate CHECKED_IN/COMPLETED state from a click.

### OPS-002 — Check In Visit

Request body:

```json
{}
```

Authenticated Staff becomes the actual `EmployeeId`.

Errors:

```text
VISIT_INVALID_STATUS  -> 409
VISIT_ENTITY_MISMATCH -> 409
FORBIDDEN             -> 403
```

Procedure: `usp_CheckInVisit`.

### OPS-004 — Complete Handover

Submitting this command as authorized Facility Staff acknowledges that full first-month rent (Reservation.LockedRentalPrice, without Contract Discount) has been received offline and handover is complete. FRMS relies on Staff confirmation; it does not independently verify physical cash through a provider.

Keep the existing Reservation/Visit/Facility/unit/policy/ownership checks and validate any Contract Discount. The payload has no first-month payment reference, Invoice reference, payment method or client-supplied amount. Contract creation records settlement; no separate paid flag is added.

Request:

```json
{
  "visitId": "uuid",
  "storageUnitId": "uuid",
  "discountId": null
}
```

Response:

```json
{
  "data": {
    "contract": {
      "contractId": "uuid",
      "status": "ACTIVE",
      "storageUnitId": "uuid",
      "startMonth": "2026-11",
      "endMonth": "2027-01"
    },
    "reservationStatus": "COMPLETED",
    "visitStatus": "CHECKED_OUT",
    "storageUnitStatus": "IN_USE"
  },
  "message": "Handover completed."
}
```

Errors:

```text
RESERVATION_INVALID_STATUS          -> 409
VISIT_INVALID_STATUS                -> 409
UNIT_NOT_AVAILABLE                  -> 409
UNIT_FACILITY_TYPE_MISMATCH         -> 409
DISCOUNT_NOT_OWNED_BY_CUSTOMER      -> 409
DISCOUNT_NOT_VALID                  -> 409
```

Procedure: `usp_CompleteHandover`.

The transaction creates Contract and updates Unit/Reservation/Visit states without creating a first-month Invoice/Payment. Preserve the authenticated Staff audit trail for handover; it is operational/security history, not a cash-payment entity or the primary settlement record.

Manager unit selection is **not persisted as a separate state/entity**. Manager obtains eligible unit information through `UNIT-001`; the selected `storageUnitId` is supplied to the handover transaction. Because no preassignment entity/field exists, the database can enforce unit eligibility but cannot independently prove who visually selected the ID before Staff submits the handover. This is an intentional non-persisted operational responsibility, not a hidden assignment workflow.

### OPS-005 — Confirm Actual Return

Request:

```json
{
  "actualReturnDate": "2027-01-28"
}
```

Response:

```json
{
  "data": {
    "visitId": "uuid",
    "actualReturnDate": "2027-01-28",
    "inspectionId": "uuid",
    "inspectionStatus": "PENDING",
    "storageUnitStatus": "INSPECTION",
    "returnClassification": "NORMAL"
  }
}
```

`returnClassification` is a derived response value (`NORMAL` / `EARLY`), not a new persisted lifecycle status.

Procedure: `usp_ConfirmActualReturn`.

## 13.18 Inspection / Settlement API Contracts

### INS-003 — Claim Inspection

Request:

```json
{}
```

Authenticated Facility Staff is the claim actor.

Errors:

```text
INSPECTION_ALREADY_CLAIMED -> 409
INSPECTION_INVALID_STATUS  -> 409
FORBIDDEN                  -> 403
```

### INS-004 — Record Damage

Request:

```json
{
  "damageTypeId": "uuid",
  "damageAmount": 250000.00,
  "note": "Broken lock fixture."
}
```

A new DamageRecord MUST be created with `Status = PENDING`.

Facility Staff records Damage but cannot approve/reject it. Same-Facility Facility Manager decides through `INS-009`.

### INS-009 — Decide Damage

```text
POST /api/v1/damage-records/{damageRecordId}/decision
Role: FACILITY_MANAGER
```

Request:

```json
{
  "decision": "APPROVED"
}
```

Allowed values:

```text
APPROVED
REJECTED
```

Rules:

- DamageRecord MUST currently be `PENDING`.
- Manager MUST belong to the same Facility as the related Inspection/Contract.
- Decision changes only `DamageRecord.Status`; Staff-entered amount/note remain unchanged.
- APPROVED and REJECTED are terminal.
- AuditLog records Manager actor and decision.
- Concurrent decisions use a conditional update; exactly one terminal decision wins.

Errors:

```text
DAMAGE_INVALID_STATUS -> 409
FORBIDDEN             -> 403
RESOURCE_NOT_FOUND    -> 404
```

Procedure: `usp_DecideDamage`.

### INS-010 — List Damage Types

```text
GET /api/v1/damage-types
Roles: FACILITY_STAFF, FACILITY_MANAGER
```

Response:

```json
{
  "data": [
    {
      "damageTypeId": "uuid",
      "name": "LOCK_DAMAGE",
      "defaultAmount": null,
      "status": "ACTIVE"
    }
  ]
}
```

Only active seeded Release 1 DamageTypes are returned.

### INS-005 — Record Extra Fee

Request:

```json
{
  "extraFeeTypeId": "uuid",
  "amount": 100000.00,
  "reason": "Key replacement."
}
```

### INS-006 — Upload Inspection Evidence

This endpoint is the binary exception to the JSON-template rule.

```text
Content-Type: multipart/form-data
```

Fields:

```text
file         REQUIRED binary
evidenceType REQUIRED IMAGE | VIDEO | DOCUMENT
```

Binary content is persisted as `InspectionEvidence.FileData`.

### INS-007 — Complete Inspection

Request:

```json
{
  "conditionNote": "Unit inspected."
}
```

Complete the authorized IN_PROGRESS Inspection and record its completion information; StorageUnit remains INSPECTION. This command does not accept storageUnitStatus, release rentable availability, terminate Contract or create settlement. No NextUnitStatus entity field or persisted pre-release choice is introduced. Unresolved Damage continues to block Finalize Return under BR-DMG-03.

### INS-008 — Finalize Return

Request:

```json
{
  "storageUnitStatus": "AVAILABLE"
}
```

Response:

```json
{
  "data": {
    "contractId": "uuid",
    "contractStatus": "COMPLETED",
    "storageUnitStatus": "AVAILABLE",
    "settlement": {
      "depositSettlementId": "uuid",
      "totalDeduction": 350000.00,
      "refundAmount": 1150000.00,
      "additionalAmountDue": 0.00,
      "status": "FINALIZED"
    }
  }
}
```

Errors:

```text
RETURN_ALREADY_FINALIZED  -> 409
INSPECTION_INVALID_STATUS -> 409
DAMAGE_DECISION_PENDING   -> 409
VALIDATION_ERROR          -> 400 for a missing/invalid storageUnitStatus
```

storageUnitStatus is a required non-persisted command parameter with allowed values AVAILABLE or MAINTENANCE; it is moved from INS-007, not copied into Inspection/Contract as an intermediate field. Authorized Staff selects the final operational state at finalization. The response reports the actual committed Unit state, not an optimistic echo.

The authoritative transaction requires the related completed Inspection, no pending Damage decision, the related Unit still INSPECTION and the valid active Contract/return linkage. It creates one DepositSettlement, sets Contract COMPLETED/TERMINATED according to return classification and changes Unit INSPECTION -> the requested AVAILABLE/MAINTENANCE atomically. Before commit, no new handover may use that Unit. A validation/settlement/concurrency failure rolls back all effects and leaves the Unit INSPECTION. UNIT-005 cannot bypass this return workflow; the pending Contract and incomplete settlement must not be bypassed by another status command.

Retry preserves the canonical original settlement/Contract/Unit outcome or returns controlled RETURN_ALREADY_FINALIZED; a different target in a retried request must not mutate an already-finalized Unit.

Safety rule:

```text
IF any DamageRecord for the Inspection has Status = PENDING
    Finalize Return MUST NOT proceed
```

This prevents final settlement from silently excluding unresolved Damage. APPROVED Damage contributes to settlement; REJECTED Damage contributes zero.

Procedure: `usp_FinalizeReturn`.

## 13.19 Support API Contracts

### SUP-001 — Create Support Ticket

Request:

```json
{
  "contractId": "uuid",
  "category": "PAYMENT_ISSUE",
  "description": "Payment status appears incorrect."
}
```

Customer identity is derived from authentication.

`PAYMENT_ISSUE` is one of the fixed Release 1 category values. API validation MUST enforce the DD-09 enum.

### SUP-004 — Cancel Support Ticket

Request:

```json
{
  "reason": "Issue no longer occurs."
}
```

Only eligible Customer-owned ticket may be cancelled.

### SUP-005 — Assign Staff

Request:

```json
{
  "employeeId": "uuid"
}
```

Facility Manager may assign only Facility Staff belonging to the same Facility as the Contract/Ticket.

### SUP-006 — Complete Ticket

Request:

```json
{
  "resultNote": "Issue resolved and confirmed."
}
```

## 13.20 StorageUnit / Manager API Contracts

### UNIT-002 — Create StorageUnit

Request:

```json
{
  "unitTypeId": "uuid",
  "unitCode": "D7-A-001",
  "locationInfo": "Floor 1 - Zone A"
}
```

A StorageUnit created by `UNIT-002` MUST start with:

```text
Status = AVAILABLE
```

It contributes to capacity only when the owning Facility is ACTIVE and all normal capacity eligibility rules are satisfied.

### UNIT-004 — Update StorageUnit

Request:

```json
{
  "unitTypeId": "uuid",
  "locationInfo": "Floor 1 - Zone B"
}
```

This endpoint MUST NOT bypass occupancy/status constraints.

Changing `unitTypeId` while the StorageUnit is `IN_USE` or `INSPECTION` would invalidate the Contract/Inspection-derived UnitType semantics and MUST be rejected. A UnitType change is permitted only when no ACTIVE Contract occupies the unit and the current lifecycle state permits master-data reassignment.

### UNIT-005 — Operational Status Transition

Request:

```json
{
  "status": "MAINTENANCE"
}
```

Only legal transitions from the StorageUnit state matrix are accepted.

V8 explicitly adds `AVAILABLE -> MAINTENANCE` for Facility Manager preventive maintenance. The unit MUST have no ACTIVE Contract and is excluded from rentable capacity while in MAINTENANCE.

INSPECTION -> AVAILABLE/MAINTENANCE during return is not a Manager status shortcut: only INS-008 may perform that transition atomically with terminal Contract/settlement state under BR-RET-07. UNIT-005 must reject a request that would release a Unit before return finalization.

## 13.21 BOM API Contracts

### BOM-002 — Create Facility

Request:

```json
{
  "name": "District 7 Facility",
  "address": "Ho Chi Minh City",
  "contactInfo": "string",
  "description": "string"
}
```

A Facility created by `BOM-002` MUST start with `Status = INACTIVE`. `BOM-004` explicitly activates it; `BOM-005` deactivates it.

### BOM-007 — Update UnitType Price

Request:

```json
{
  "rentalPrice": 1650000.00
}
```

Changing current price MUST NOT rewrite historical Reservation, ContractExtension or issued Invoice snapshots.

### BOM-009 — Create Policy Version

Request:

```json
{
  "depositTimeoutHours": 1,
  "reservationVisitStartDay": 1,
  "reservationVisitEndDay": 5,
  "monthlyPaymentDueDay": 5,
  "overdueStartDay": 6,
  "lateFeeDivisorDays": 31,
  "earlyReturnWaiveFeeUntilDay": 5
}
```

Response includes new `policyId`, incremented `version`, and `ACTIVE` status.

### BOM-010 — Authorized Customer Discount Catalogue

```text
GET /api/v1/business/customers/{customerId}/discounts?page=1&pageSize=20
Roles: CUSTOMER, FACILITY_STAFF, FACILITY_MANAGER, BUSINESS_OPERATIONS_MANAGER
```

Return the standard paginated collection. Each item contains existing read-only fields discountId, customerId, name, percentage, status, effectiveFrom and nullable effectiveTo; timestamps use UTC ISO-8601. These are existing Discount fields, not a redemption/eligibility entity.

Customer must match the path CustomerId to the authenticated Customer. Staff/Manager must have a current Facility assignment and the path Customer must have an existing Reservation or Contract in that Facility. This association is verified server-side, including requests that replace the path CustomerId. BOM retains system-wide management read. A successful catalogue read grants no Discount mutation or cross-Facility resource access.

The catalogue preserves actual ACTIVE/INACTIVE/effective values; reading a row is not proof it is eligible for a new Contract. The existing handover rules authoritatively validate optional DiscountId, Customer ownership, status and effective period. Captured Discounts on existing Contracts retain DD-14 semantics even if now INACTIVE. First-month offline rent is never discounted. FE must not hardcode Discount IDs or invent price reductions.

Example item (illustrative data, not seed):

```json
{
  "discountId": "uuid",
  "customerId": "uuid",
  "name": "LOYALTY_10",
  "percentage": 10.0,
  "status": "ACTIVE",
  "effectiveFrom": "2026-10-02T00:00:00Z",
  "effectiveTo": null
}
```

### BOM-011 — Create Customer Discount

Request:

```json
{
  "name": "LOYALTY_10",
  "percentage": 10.0,
  "effectiveFrom": "2026-10-02T00:00:00Z",
  "effectiveTo": null
}
```

`customerId` is taken from path.

### BOM-012 — Update Discount

Allowed master-data update template:

```json
{
  "name": "LOYALTY_10",
  "percentage": 10.0,
  "status": "ACTIVE",
  "effectiveFrom": "2026-10-02T00:00:00Z",
  "effectiveTo": null
}
```

Issued Invoice snapshots are never retrospectively rewritten.

A Discount referenced by any Contract MUST NOT have `customerId`, `percentage`, `effectiveFrom`, or `effectiveTo` mutated in place. `INACTIVE` prevents new Contract selection but does not invalidate existing Contract usage.

### BOM-013 — Operational / Management ExtraFeeType Catalogue

```text
GET /api/v1/business/extra-fee-types?page=1&pageSize=20
Roles: FACILITY_STAFF, FACILITY_MANAGER, BUSINESS_OPERATIONS_MANAGER
```

Return the standard paginated collection using existing ExtraFeeType fields extraFeeTypeId, name, defaultAmount and status. ACTIVE Staff/Manager with a valid assigned Facility receive ACTIVE types only for authorized operational fee recording. BOM retains the full management catalogue. ExtraFeeType is a global master, not a newly introduced Facility-owned table; actual Inspection/fee commands remain restricted to the actor's Facility.

defaultAmount is the configured decimal amount, not a FE-invented constant. An empty catalogue remains empty; the read does not seed missing rows, choose deployment currency or bypass the Phase 0 seed restriction. INS-005 still validates active type/Inspection scope and snapshots the actual amount under existing rules. BOM-014 remains BOM-only.

Example item (illustrative data, not approved deployment seed):

```json
{
  "extraFeeTypeId": "uuid",
  "name": "KEY_REPLACEMENT",
  "defaultAmount": 100000.00,
  "status": "ACTIVE"
}
```

### BOM-014 — Update ExtraFeeType

```json
{
  "defaultAmount": 100000.00,
  "status": "ACTIVE"
}
```

## 13.22 Reporting Contracts

### REP-001 — Facility Operations

Example response:

```json
{
  "data": {
    "facilityId": "uuid",
    "asOf": "2026-10-02T14:30:00Z",
    "availableUnits": 20,
    "inUseUnits": 30,
    "inspectionUnits": 2,
    "maintenanceUnits": 1,
    "usageRate": 0.5660,
    "overdueContractCount": 3
  }
}
```

For REP-001, usageRate is CALC-REP-01: 30 / (20 + 30 + 2 + 1) = 0.5660 rounded to four decimal places in this example. Include INSPECTION and MAINTENANCE in the physical-unit denominator; if the scoped Facility has no Unit, usageRate is 0. This does not change capacity eligibility.

### REP-003 — Business Overview

Response may include:

```json
{
  "data": {
    "facilityCount": 4,
    "activeContractCount": 120,
    "usageRate": 0.7200,
    "rentalRevenue": 250000000.00
  }
}
```

Revenue follows revised CALC-REP-02 (section 12.4.1): Contract-derived first-month recognition in Contract.StartMonth plus paid later Rental Fee invoices. Keep the rentalRevenue field. An Invoice/Payment-only total omits the first month; counting legacy first-month invoices again duplicates that amount.

### REP-004 — Export

This endpoint is a file response, not JSON.

Required query filters are the same business-report filters exposed by the corresponding report view. Release 1 export format is CSV (`text/csv`, UTF-8) according to DD-03.

## 13.23 Administrator API Contracts

### ADM-003 / ADM-004 — Customer Status

Request body:

```json
{}
```

Deactivation MUST fail when the Customer has:

```text
CONFIRMED Reservation
or
ACTIVE Contract
```

The API returns a controlled conflict according to `BR-ACC-02`.

Procedure: `usp_SetUserAccountStatus`.

### ADM-005 — Create Employee

Request baseline:

```json
{
  "fullName": "Employee A",
  "email": "employee@example.com",
  "phoneNumber": "0900000002",
  "role": "FACILITY_STAFF",
  "facilityId": "uuid"
}
```

Rules:

- Email UNIQUE.
- PhoneNumber UNIQUE.
- Facility required for Facility Staff/Manager.
- Facility not required for global roles.

`DESIGN-LOCKED — V9 FINAL`

Employee credential provisioning:

```text
Admin submits ADM-005
-> create UserAccount/Employee as INACTIVE
-> system generates cryptographically random 16-character initial password
-> store BCrypt hash only
-> send plaintext initial password once through IEmailService
-> mail provider accepts send: activate account
-> mail send fails: keep account INACTIVE
```

Security rules:

- plaintext initial password exists only transiently in application memory and the outgoing email payload;
- it MUST NOT be logged, written to AuditLog/NotificationLog/database, or returned by API;
- `IEmailService` MUST NOT log the credential email body;
- because password reset/change is outside Release 1, the successfully emailed initial password becomes the Employee's current password.

If delivery fails, Admin uses `ADM-012`; retry generates a new password/hash and invalidates the previous unsent credential.

### ADM-012 — Regenerate / Resend Initial Credential

```text
POST /api/v1/admin/employees/{employeeId}/resend-initial-credential
Role: SYSTEM_ADMINISTRATOR
```

Request:

```json
{}
```

Rules:

- Employee account MUST be `INACTIVE`.
- System generates a new cryptographically random 16-character initial password.
- Store only its BCrypt hash.
- Send plaintext only through `IEmailService`.
- On provider-accepted send, set account `ACTIVE`.
- On send failure, keep account `INACTIVE`.
- This is initial-provisioning recovery only; it is not a general password reset for ACTIVE Employees.

Response never includes the password.

Errors:

```text
CREDENTIAL_PROVISIONING_INVALID_STATUS -> 409
EXTERNAL_PROVIDER_UNAVAILABLE          -> 503
```

### ADM-009 — Assign Role / Facility

Request:

```json
{
  "role": "FACILITY_MANAGER",
  "facilityId": "uuid"
}
```

For global roles:

```json
{
  "role": "BUSINESS_OPERATIONS_MANAGER",
  "facilityId": null
}
```

### ADM-010 — Login History

Supported filters:

```text
userAccountId
status
fromUtc
toUtc
page
pageSize
```

### ADM-011 — Audit Logs

Supported filters:

```text
userAccountId
entityType
entityId
action
fromUtc
toUtc
page
pageSize
```

## 13.24 Swagger / OpenAPI Requirements

For every endpoint in Section 13.11, Swagger/OpenAPI MUST expose:

- method/path;
- summary/description;
- authorization requirement;
- request schema;
- response schema;
- enum strings;
- documented success status;
- documented 400/401/403/404/409 responses where applicable.

Contract consistency rule:

```text
SRS
≈ Swagger/OpenAPI
≈ FE TypeScript API models
```

Material mismatch is a defect.

## 13.25 API Security Rules

- Customer ID is derived from JWT/account context for own-resource commands.
- Staff/Manager Facility scope is server-side.
- Administrator role does not grant business-operation mutation authority.
- FE may hide controls but backend remains authoritative.
- Request DTO MUST NOT accept server-owned status/actor/audit/timestamp fields unless that endpoint explicitly represents the corresponding command.

## 13.26 API Time Rules

- API timestamps are UTC ISO-8601.
- FE displays GMT+7.
- business date/day decisions use GMT+7.
- `StartMonth`, `EndMonth`, `BillingMonth` serialize as `YYYY-MM`.
- `VisitDate` and `ActualReturnDate` serialize as calendar date where the model is date-based.

## 13.27 API Contract Exceptions

Not every transport is JSON:

1. `INS-006` uses `multipart/form-data` for binary evidence.
2. `REP-004` returns an export file.
3. `PAY-004` accepts signed payOS JSON through the provider adapter; see the locked PAY-004 contract.

# 14. Frontend Requirements


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

- attach JWT Bearer token;
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

Staff loads Visit/Reservation context through OPS-001 -> VIS-004/RES-003. After OPS-004 succeeds, use its contractId -> CON-002; ContractDetail.reservationId -> RES-003 supplies the immutable first-month amount and handover Visit without adding financial/Visit attributes to Contract. Staff must not call Customer-only list/write endpoints or synthesize missing operational data.

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
| RES-* | `reservationApi.ts` |
| BIL-* / PAY-* | `billingApi.ts` |
| CON-* | `contractApi.ts` |
| VIS-* / OPS-* | `visitApi.ts` |
| INS-* | `inspectionApi.ts` |
| SUP-* | `supportTicketApi.ts` |
| UNIT-* | `storageUnitApi.ts` |
| BOM-* | `businessApi.ts` |
| REP-* | `reportApi.ts` |
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

## 15.3 Notification Processing

`NotificationLog` lifecycle:

```text
PENDING -> SENT
```

Delivery failure keeps:

```text
PENDING
```

for retry.

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

Optional capability.

AI receives existing UnitType metadata and may return:

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
-> customer may continue normal UnitType selection
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

- SQL Server connection;
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

Concurrency correctness MUST be proven with real SQL Server integration tests.

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
SQL Server -> real database integration/concurrency tests
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

Real SQL Server behavior is required to prove:

- stored procedures;
- filtered unique indexes;
- SQL Server locking;
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

## 19.8 Test Data

Test data MUST support:

- all five roles;
- at least two Facilities for scope/isolation tests;
- multiple UnitTypes;
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
| `CWP-01` | Facility & Unit Type Browsing | Triển / Long | CAT-001..004 | BR-FAC-01; CALC-CAP-01 | API-CAT, SEC, E2E-F01 | Yes |
| `CWP-02` | AI Size Guide Recommendation | Triển / Giang | AI-001 | AI non-authoritative rules | INT-AI, API-AI | No |
| `CWP-03` | Storage Reservation | Triển / Khoa | RES-001 | BR-RES-01..05 | UT-RES, DBT-01/02, CON-RES, API-RES, E2E-F01 | Yes |
| `CWP-04` | Reservation & Deposit Management | Triển / Khoa+Giang | RES-002..005, BIL-001..002, PAY-001, PAY-003 | BR-RES-06..08; BR-PAY-01 | UT/API/DBT/E2E-F01 | Yes |
| `CWP-05` | Reservation Visit Management | Triển / Khoa | RES-004, VIS-003..006 | BR-RES-07; BR-VIS-01..02 | UT-VIS, API-VIS, E2E-F01/F02 | Yes |
| `CWP-06` | Rental Management | Triển / Khoa | CON-001..002, CON-004..005 | BR-CON-* | API-CON, SEC, E2E-F03/F06 | Yes |
| `CWP-07` | Access Visit Management | Triển / Khoa | VIS-001, VIS-003..006 | BR-VIS-01..04 | UT/API/E2E-F03 | Yes |
| `CWP-08` | Rental Payment (second month onward) | Triển / Giang | BIL-*, PAY-001, PAY-003, CON-004 | BR-BIL-*; BR-PAY-* | UT-PAY, DBT-08/09, CON-PAY, API-PAY | Yes |
| `CWP-09` | Contract Renewal | Triển / Khoa | CON-003 | BR-REN-01..04 | UT-REN, DBT-11, CON-REN, API-REN, E2E-F06 | Yes |
| `CWP-10` | Storage Return Visit | Triển / Giang | VIS-002..006, CON-005 | BR-RET-01..02 | UT-RET, API-RET, E2E-F06 | Yes |
| `CWP-11` | Fee & Return Tracking | Triển / Giang | CON-004..005, BIL-* | BR-LATE-*; BR-SET-* | UT/API/E2E-F06 | Yes |
| `CWP-12` | Customer Support | Triển / Giang | SUP-001..004 | BR-SUP-01 | UT-SUP, API-SUP, E2E-F07 | Yes |
| `FWP-01` | Daily Work List | Triển / Long | OPS-001; VIS-004/RES-003/CON-002 navigation | Derived Visits/Inspections/Tickets; same-Facility minimal operational read | DAL/API/SEC | Yes |
| `FWP-02` | Reservation Check-in | Triển / Khoa | OPS-001, RES-003, VIS-004, OPS-002 | BR-VIS-*; same-Facility read | UT/API/SEC/E2E-F02 | Yes |
| `FWP-03` | Offline-receipt acknowledgment & Handover Processing | Triển / Khoa | RES-003, VIS-004, BOM-010, OPS-004, CON-002 | BR-HO-01..03; BR-BIL-06; CALC-FIRST-01; immutable rental read | DBT-06/24, CON-HO, API-HO, SEC, E2E-F02 | Yes |
| `FWP-04` | Access Visit Processing | Triển / Khoa | OPS-002..003 | BR-VIS-* | UT/API/E2E-F03 | Yes |
| `FWP-05` | Return Confirmation | Triển / Giang | OPS-002, OPS-005 | BR-RET-01..02,06 | DBT-20, CON-RET, API-RET, E2E-F06 | Yes |
| `FWP-06` | Return Inspection | Triển / Giang | INS-001..003,006..007 | BR-RET-03/07; BR-DMG-01; Unit remains INSPECTION | DBT-10/14, CON-INS, API-INS, E2E-F06 | Yes |
| `FWP-07` | Return Fee Recording & Atomic Unit Release | Triển / Giang | INS-004..005, INS-008, INS-010, BOM-013; BIL-001..002, PAY-003, CON-004..005 reads | BR-DMG-01..03; BR-EXT-01; BR-SET-*; BR-RET-07 | DBT-14..16, DBT-21..23, CON-RET, API-INS, SEC, E2E-F06 | Yes |
| `FWP-08` | Support Request Processing | Triển / Giang | SUP-002..003,006 | BR-SUP-01 | UT/API/E2E-F07 | Yes |
| `MWP-01` | Handover Unit Selection | Mẫn / Khoa | UNIT-001,003; RES-002..003, VIS-003..004, BOM-010 reads; Staff OPS-004 | BR-HO-01; same-Facility operational read; no Manager handover-command grant | API/SEC/E2E-F02 | Yes |
| `MWP-02` | Physical Unit Management | Mẫn / Long | UNIT-001..005 | StorageUnit state rules | UT/DAL/API/SEC | Yes |
| `MWP-03` | Facility Operations Monitoring | Mẫn / Long | UNIT-001, REP-001..002; RES-002..003, CON-001..002/004..005, VIS-003..004, BIL-001..002, PAY-003 | CALC-REP-*; same-Facility reads | DAL/API/SEC | Yes |
| `MWP-04` | Return, Inspection & Damage Decision | Mẫn / Giang | INS-001..002, INS-009..010, REP-001 | BR-RET-*; BR-DMG-02..03 | API/SEC/CON-DMG/E2E-F06 | Yes |
| `MWP-05` | Support Staff Assignment | Mẫn / Giang | SUP-002..005 | BR-SUP-01 | UT/API/SEC/E2E-F07 | Yes |
| `MWP-06` | Facility Reporting | Mẫn / Giang | REP-001..002 | CALC-REP-* | DAL/API/ACC | Yes |
| `BWP-01` | Facility Management | Mẫn / Long | BOM-001..005 | BR-FAC-01 | UT/API/SEC | Yes |
| `BWP-02` | Unit Type Pricing | Mẫn / Long | BOM-006..007 | snapshot rules | UT/API/REG | Yes |
| `BWP-03` | Policy Version Management | Mẫn / Long | BOM-008..009 | BR-POL-01..02 | DBT-12, CON-POL, API | Yes |
| `BWP-04` | Discount Management | Mẫn / Long | BOM-010..012 | BR-DIS-01..04 | DBT-07, API | Yes |
| `BWP-05` | Extra Fee Management | Mẫn / Giang | BOM-013..014 | BR-EXT-01 | API/REG | Yes |
| `BWP-06` | Multi-Facility Monitoring | Mẫn / Giang | BOM-001, REP-003; RES-002..003, CON-001..002/004..005, VIS-003..004, BIL-001..002, PAY-003 | CALC-REP-*; authorized system-wide reads | DAL/API/SEC | Yes |
| `BWP-07` | Business Reporting & Export | Mẫn / Giang | REP-003..004 | CALC-REP-* | DAL/API/ACC | Yes |
| `AWP-01` | User Account Monitoring | Mẫn / Long | ADM-001..002 | BR-ACC-* | API/SEC | Yes |
| `AWP-02` | Customer Account Status Management | Mẫn / Long | ADM-003..004 | BR-ACC-02 | UT/API/DBT | Yes |
| `AWP-03` | Employee Account Management | Mẫn / Long | ADM-005..008, ADM-012 | BR-EMP-01; credential provisioning | UT/API/SEC/INT-EMAIL | Yes |
| `AWP-04` | Role & Facility Assignment | Mẫn / Long | ADM-009 | BR-EMP-01 | UT/API/SEC | Yes |
| `AWP-05` | Access Management | Mẫn / Long | Authorization policies | SSP-17 | SEC-* | Yes |
| `AWP-06` | Login History | Mẫn / Long | ADM-010 | append-only rules | DAL/API/SEC | Yes |
| `AWP-07` | Activity Log Management | Mẫn / Long | ADM-011 | BR-AUD-01 | DAL/API/SEC | Yes |
| `SSP-01` | Unit Type Catalog | System / Long | CAT-003..004, BOM-006 | catalog rules | UT/DAL/API | Yes |
| `SSP-02` | Capacity Management | System / Khoa | CAT-003, RES-001, CON-003 | BR-RES-02..03; BR-REN-03 | DBT-01/11, CON-RES/REN | Yes |
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

Simple read/query endpoints may use repository/EF Core without a stored procedure.

## 20.4 API → Test Group Rule

Every API has applicable:

```text
CTL-*  request/response contract
API-*  HTTP behavior
SEC-*  if protected
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
[ ] approved live payOS scenario works (NOT RUN)
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
[ ] approved live payOS Deposit + Rental Fee flows work (NOT RUN)
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

The export MUST use the same filters/calculations as the corresponding on-screen report.

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

# 25. Final Locked Design Summary

V10 FINAL consolidates the following implementation baseline, including the owner-approved offline-first-month and operational-consistency revisions; V9 decisions not explicitly superseded by those revisions remain in force:
1. Authority order is `SRS V10 FINAL > Data Dictionary V2.1 > Scope V8`.
2. This SRS is the FRMS Release 1 implementation source of truth.
3. Five roles are fixed: CUSTOMER, FACILITY_STAFF, FACILITY_MANAGER, BUSINESS_OPERATIONS_MANAGER, SYSTEM_ADMINISTRATOR.
4. Seven business flows are the core lifecycle scope.
5. React + TypeScript is the frontend baseline.
6. ASP.NET Core Web API + C# is the backend baseline.
7. SQL Server + EF Core is the persistence baseline.
8. Architecture has three logical layers: Presentation / Business / Data Access; Infrastructure is a supporting adapter assembly, not an additional business layer.
9. HTTP dependency flow is Controller -> Service -> Repository -> EF Core/Stored Procedure; application-scheduled flow is Api BackgroundJob -> Service -> Repository, while SQL Server Agent may call authoritative procedures directly.
10. No standalone DAO layer exists in the required architecture.
11. Request DTO, Response DTO, Business Command, Business Result and persistence Entity are separate concerns.
12. Critical multi-table lifecycle is owned by authoritative Stored Procedures/DB transactions.
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
37. FRMS Release 1 schema has 27 persisted entities.
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
53. SRS JSON schemas, Swagger/OpenAPI and FE TypeScript models must remain aligned.
54. Binary InspectionEvidence upload uses multipart/form-data.
55. Report export is a UTF-8 CSV file response.
56. payOS wire payload remains behind the payment adapter.
57. Business algorithms in the SRS are expressed in pseudocode.
58. Ten testing groups are mandatory delivery structure.
59. Real SQL Server integration tests are required for stored procedure/trigger/index/locking proof.
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
72. Manager unit selection remains an operational responsibility without a persisted preassignment entity.
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
100. Release 1 has no separate Worker project. Application-scheduled execution runs in `Frms.Api/BackgroundJobs` and calls Business services; SQL Server Agent may call authoritative stored procedures directly.
101. `Frms.ArchitectureTests` is required to enforce the locked dependency boundaries.
102. RES-003, CON-002 and VIS-004 allow Customer own detail, Staff/Manager same-Facility detail and BOM system-wide detail; no corresponding Staff list permission is granted.
103. RES-002, CON-001 and VIS-003 are Customer-own, Manager-Facility and BOM-system lists; all filtering/counting precedes pagination.
104. OPS-001 Visit work items expose authoritative VisitId/EntityId and minimal Customer identification; non-Visit reference IDs are not interpreted as Visit IDs.
105. ContractDetail already contains ReservationId; operational locked-price/RESERVATION Visit reads follow RES-003/VIS-004 without new Contract attributes or duplicate fields.
106. BOM-010 supports own Customer and authorized same-Facility operational Discount reads; BOM-013 supports active operational ExtraFeeType reads. Catalogue mutation roles are unchanged.
107. BIL-001/002, PAY-003 and CON-004/005 support authorized operational verification/monitoring, not employee payment initiation or manual PAID/SUCCESS mutation.
108. INS-007 completes Inspection but keeps Unit INSPECTION. Required storageUnitStatus moves to INS-008, whose transaction atomically finalizes settlement/Contract and releases Unit AVAILABLE/MAINTENANCE.
109. usageRate = IN_USE / all StorageUnits in authorized report scope, including INSPECTION/MAINTENANCE; zero denominator -> 0. Rental capacity remains a separate calculation.

# 26. Whole-System Acceptance Criteria


FRMS Release 1 is accepted only when all applicable conditions below are true.

## 26.1 Business Acceptance

```text
[ ] Seven business flows operate according to the current SRS lifecycle
[ ] Five-role responsibilities are respected
[ ] Scope exclusions are not silently implemented as conflicting workflows
[ ] Data Dictionary overrides incorporated by SRS are respected
[ ] Explicit overrides in the current SRS are respected
```

## 26.2 Data / Lifecycle Acceptance

```text
[ ] 27-table Release 1 schema represented in migrations
[ ] PK/FK/CHECK/UNIQUE/filtered indexes implemented as approved
[ ] Email UNIQUE
[ ] PhoneNumber UNIQUE
[ ] Historical snapshot rules enforced
[ ] Contract.PolicyId equals Reservation.PolicyId after Complete Handover
[ ] State transitions protected
[ ] Append-only tables protected
[ ] Critical Stored Procedures transactional
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
[ ] Staff detail read RES-003/CON-002/VIS-004 and OPS-001 Visit navigation match the approved operational contract
[ ] ContractDetail remains unchanged; locked rent and handover Visit are read through its ReservationId
[ ] Manager/BOM operational reads and BOM-010/BOM-013 catalogue reads match the role/Facility contracts
[ ] INS-007 has no storageUnitStatus; INS-008 requires AVAILABLE/MAINTENANCE and reports committed Unit state
[ ] usageRate includes every scoped Unit and returns 0 for an empty denominator
```

## 26.5 Security Acceptance

```text
[ ] Customer login by PhoneNumber
[ ] Employee login by Email
[ ] BCrypt verification
[ ] JWT expiry enabled
[ ] Account ACTIVE enforced
[ ] RBAC enforced
[ ] Facility scope enforced
[ ] Customer ownership enforced
[ ] Staff same-Facility operational detail and work-item reads pass positive/cross-Facility tests
[ ] Operational read access does not grant Customer mutation or expose unrelated profiles
[ ] Manager/BOM and financial/catalogue GET scopes pass role, Facility, ownership and pagination-leak tests
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
[ ] SRS V10 FINAL referenced by release
[ ] Deferred Decision resolutions/change records attached where applicable
```

A build that fails any applicable Release Blocker criterion is not aligned with the current SRS.

# Appendix A — Core Lifecycle Summary


Core lifecycle summary:

```text
Browse Facility / UnitType
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

# Appendix D — API Endpoint Index


The normative endpoint inventory is Section 13.11.

Quick index:

| API ID | Method | Endpoint |
|---|---|---|
| `AUTH-001` | `POST` | `/api/v1/auth/customer/register` |
| `AUTH-002` | `POST` | `/api/v1/auth/customer/login` |
| `AUTH-003` | `POST` | `/api/v1/auth/employee/login` |
| `AUTH-004` | `GET` | `/api/v1/auth/me` |
| `AUTH-005` | `PATCH` | `/api/v1/customers/me/profile` |
| `CAT-001` | `GET` | `/api/v1/facilities` |
| `CAT-002` | `GET` | `/api/v1/facilities/{facilityId}` |
| `CAT-003` | `GET` | `/api/v1/facilities/{facilityId}/unit-types` |
| `CAT-004` | `GET` | `/api/v1/unit-types/{unitTypeId}` |
| `AI-001` | `POST` | `/api/v1/ai/unit-type-recommendations` |
| `RES-001` | `POST` | `/api/v1/reservations` |
| `RES-002` | `GET` | `/api/v1/reservations` |
| `RES-003` | `GET` | `/api/v1/reservations/{reservationId}` |
| `RES-004` | `POST` | `/api/v1/reservations/{reservationId}/confirm` |
| `RES-005` | `POST` | `/api/v1/reservations/{reservationId}/cancel` |
| `BIL-001` | `GET` | `/api/v1/invoices` |
| `BIL-002` | `GET` | `/api/v1/invoices/{invoiceId}` |
| `PAY-001` | `POST` | `/api/v1/invoices/{invoiceId}/payments/payos` |
| `PAY-003` | `GET` | `/api/v1/payments/{paymentId}` |
| `PAY-004` | `POST` | `/api/v1/payments/payos/webhook` |
| `CON-001` | `GET` | `/api/v1/contracts` |
| `CON-002` | `GET` | `/api/v1/contracts/{contractId}` |
| `CON-003` | `POST` | `/api/v1/contracts/{contractId}/renew` |
| `CON-004` | `GET` | `/api/v1/contracts/{contractId}/billing` |
| `CON-005` | `GET` | `/api/v1/contracts/{contractId}/return-summary` |
| `VIS-001` | `POST` | `/api/v1/contracts/{contractId}/access-visits` |
| `VIS-002` | `POST` | `/api/v1/contracts/{contractId}/return-visits` |
| `VIS-003` | `GET` | `/api/v1/visits` |
| `VIS-004` | `GET` | `/api/v1/visits/{visitId}` |
| `VIS-005` | `PATCH` | `/api/v1/visits/{visitId}/schedule` |
| `VIS-006` | `POST` | `/api/v1/visits/{visitId}/cancel` |
| `OPS-001` | `GET` | `/api/v1/staff/work-items` |
| `OPS-002` | `POST` | `/api/v1/visits/{visitId}/check-in` |
| `OPS-003` | `POST` | `/api/v1/visits/{visitId}/check-out` |
| `OPS-004` | `POST` | `/api/v1/reservations/{reservationId}/complete-handover` |
| `OPS-005` | `POST` | `/api/v1/visits/{visitId}/confirm-return` |
| `INS-001` | `GET` | `/api/v1/inspections` |
| `INS-002` | `GET` | `/api/v1/inspections/{inspectionId}` |
| `INS-003` | `POST` | `/api/v1/inspections/{inspectionId}/claim` |
| `INS-004` | `POST` | `/api/v1/inspections/{inspectionId}/damages` |
| `INS-005` | `POST` | `/api/v1/inspections/{inspectionId}/extra-fees` |
| `INS-006` | `POST` | `/api/v1/inspections/{inspectionId}/evidence` |
| `INS-007` | `POST` | `/api/v1/inspections/{inspectionId}/complete` |
| `INS-008` | `POST` | `/api/v1/contracts/{contractId}/finalize-return` |
| `INS-009` | `POST` | `/api/v1/damage-records/{damageRecordId}/decision` |
| `INS-010` | `GET` | `/api/v1/damage-types` |
| `SUP-001` | `POST` | `/api/v1/support-tickets` |
| `SUP-002` | `GET` | `/api/v1/support-tickets` |
| `SUP-003` | `GET` | `/api/v1/support-tickets/{ticketId}` |
| `SUP-004` | `POST` | `/api/v1/support-tickets/{ticketId}/cancel` |
| `SUP-005` | `POST` | `/api/v1/support-tickets/{ticketId}/assign` |
| `SUP-006` | `POST` | `/api/v1/support-tickets/{ticketId}/complete` |
| `UNIT-001` | `GET` | `/api/v1/facilities/{facilityId}/storage-units` |
| `UNIT-002` | `POST` | `/api/v1/facilities/{facilityId}/storage-units` |
| `UNIT-003` | `GET` | `/api/v1/storage-units/{storageUnitId}` |
| `UNIT-004` | `PATCH` | `/api/v1/storage-units/{storageUnitId}` |
| `UNIT-005` | `POST` | `/api/v1/storage-units/{storageUnitId}/status` |
| `BOM-001` | `GET` | `/api/v1/business/facilities` |
| `BOM-002` | `POST` | `/api/v1/business/facilities` |
| `BOM-003` | `PATCH` | `/api/v1/business/facilities/{facilityId}` |
| `BOM-004` | `POST` | `/api/v1/business/facilities/{facilityId}/activate` |
| `BOM-005` | `POST` | `/api/v1/business/facilities/{facilityId}/deactivate` |
| `BOM-006` | `GET` | `/api/v1/business/unit-types` |
| `BOM-007` | `PATCH` | `/api/v1/business/unit-types/{unitTypeId}/price` |
| `BOM-008` | `GET` | `/api/v1/business/policies` |
| `BOM-009` | `POST` | `/api/v1/business/policies` |
| `BOM-010` | `GET` | `/api/v1/business/customers/{customerId}/discounts` |
| `BOM-011` | `POST` | `/api/v1/business/customers/{customerId}/discounts` |
| `BOM-012` | `PATCH` | `/api/v1/business/discounts/{discountId}` |
| `BOM-013` | `GET` | `/api/v1/business/extra-fee-types` |
| `BOM-014` | `PATCH` | `/api/v1/business/extra-fee-types/{extraFeeTypeId}` |
| `REP-001` | `GET` | `/api/v1/reports/facilities/{facilityId}/operations` |
| `REP-002` | `GET` | `/api/v1/reports/facilities/{facilityId}/revenue` |
| `REP-003` | `GET` | `/api/v1/reports/business/overview` |
| `REP-004` | `GET` | `/api/v1/reports/business/export` |
| `ADM-001` | `GET` | `/api/v1/admin/users` |
| `ADM-002` | `GET` | `/api/v1/admin/users/{userAccountId}` |
| `ADM-003` | `POST` | `/api/v1/admin/customers/{customerId}/activate` |
| `ADM-004` | `POST` | `/api/v1/admin/customers/{customerId}/deactivate` |
| `ADM-005` | `POST` | `/api/v1/admin/employees` |
| `ADM-006` | `PATCH` | `/api/v1/admin/employees/{employeeId}` |
| `ADM-007` | `POST` | `/api/v1/admin/employees/{employeeId}/activate` |
| `ADM-008` | `POST` | `/api/v1/admin/employees/{employeeId}/deactivate` |
| `ADM-009` | `PUT` | `/api/v1/admin/employees/{employeeId}/assignment` |
| `ADM-010` | `GET` | `/api/v1/admin/login-history` |
| `ADM-011` | `GET` | `/api/v1/admin/audit-logs` |
| `ADM-012` | `POST` | `/api/v1/admin/employees/{employeeId}/resend-initial-credential` |

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
