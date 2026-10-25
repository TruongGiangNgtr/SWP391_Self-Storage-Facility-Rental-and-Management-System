# FRMS — Data Dictionary & PostgreSQL Persistence Specification (V11 REVIEW CANDIDATE)

**Project:** Self-Storage Facility Rental and Management System (FRMS)  
**Document:** Entity Dictionary, Lifecycle, PostgreSQL Physical Schema, Stored Routines, Constraints, Audit, Migration and DB Tests  
**Version:** V11.0 REVIEW CANDIDATE  
**Date:** 2026-10-21 — recurring BackgroundJobs/current `main` reconciliation  
**Approved CCCD format revision:** 2026-10-26 — `REV-2026-10-26-CCCD-12-DIGITS` (SRS OD-10); varchar(20)/nullability/unique index retained, exact-12-digit acceptance is a service/routine requirement pending BE/DB implementation.
**Approved Customer revision:** 2026-10-25 — `REV-2026-10-25-CUSTOMER-CCCD-CONTINUATION` (SRS OD-09); conditional CCCD for new RES-001 and safe in-flow continuation. Scoped source check at `2945788`; no schema deployment or test pass claimed.
**SRS:** [`FRMS_SRS_V11.md`](FRMS_SRS_V11.md) — business rules and owner approvals  
**API:** [`FRMS_API_V11.md`](FRMS_API_V11.md) — HTTP routes, DTO schemas, RBAC and error responses  
**GitHub baseline:** current read-only `main` `0219f30734b00b3e5527e3453b19840f08f63639` (includes registered BackgroundJobs); earlier V11 audit provenance `90a6c5982d4237a61eab9299f2ca434efd624fcc`  
**Source provenance:** preserves the supplied DD V2.1 entity/constraint/logic catalogue and incorporates approved V11 decisions plus verified current PostgreSQL migration/source observations. This document is **not** a byte-for-byte copy of the `main` DD V2.1 and does **not** certify live database migration state or tests.

**Authority:** approved governing SRS > approved API Specification (HTTP wire behavior) / approved Data Dictionary V11 (persistence) within their respective domains > Scope V8 > code. V11 remains a review candidate pending full ratification; owner-approved §0.0.9 decisions already control their expressly approved V11 target behavior. Never treat source-code existence as a new business approval.

**Owner decisions (2026-10-20):** optional `Customer.Address` retained; `HandoverUnitAssignment` approved as 28th entity; Administrator read-only StorageUnit timeline approved; `Admin deactivation-eligibility` is an HTTP 501 scaffold; public UnitType nullable `imageUrl`/`videoUrl` projection acknowledged. Remaining code-observed extensions are provisional unless separately approved. Full PostgreSQL architecture sign-off and release gates remain pending.

**Core invariants preserved:** five roles; seven business flows; payOS only for eligible Deposit and subsequent Rental Fee invoices; first-month rental fee collected offline at handover with NO first-month Invoice/Payment; Payment idempotency/UTC timestamp; policy snapshot, capacity/return/concurrency controls, immutable evidence.

---

---

## 1. Scope Summary

FRMS Release 1 covers 7 end-to-end business flows:

1. Storage Unit Reservation.
2. Storage Check-in and Handover.
3. Rented Storage Unit Management.
4. Business Rules, Fee Management, and Revenue Monitoring.
5. Facility Storage and Staff Management.
6. Storage Renewal and Overdue Handling.
7. Support Request and Issue Handling.

The system uses exactly five roles:

- `CUSTOMER`
- `FACILITY_STAFF`
- `FACILITY_MANAGER`
- `BUSINESS_OPERATIONS_MANAGER`
- `SYSTEM_ADMINISTRATOR`

The core design is month-based. Reservation holds capacity by `Facility + UnitType + month range`; a concrete Storage Unit is selected only at handover. Contract renewal extends the same Contract contiguously and is recorded by append-only `ContractExtension` rows. Return is represented by `RETURN Visit + Inspection + Contract/StorageUnit state`, not by a separate ReturnProcess entity.

Payment integration uses **payOS** for Deposit Invoices and for Rental Fee invoices **strictly after** `Contract.StartMonth`; the first month is collected **offline** by Staff at handover with no first-month Invoice/Payment. PAY-002 is retired. Historical `MOMO`/`VNPAY` records must remain intact. Late Fee, Extra Fee, Damage, deposit deduction, refund amount and additional amount due are recorded, but external refund/payout/compensation settlement remains outside this Release 1 system.

---

## 2. Design Baseline and Confirmed Overrides

This document integrates the confirmed ERD baseline with all explicit SRS V9 FINAL business decisions and the V10 implementation-structure revision. Where historical V2.1/Scope V8 wording conflicts with approved SRS V11 owner decisions or previously approved offline-first-month/payOS semantics, the higher-priority approved SRS rule is authoritative.

| ID | Confirmed V1 decision | Rationale |
|---|---|---|
| O01 | Use entity name `StorageUnit` for the scope concept `Physical Unit`. | Naming choice only; semantics are unchanged. |
| O02 | `Customer 1:N Discount`. `Discount.CustomerId` is the FK. | A Customer may own multiple discounts. The Scope V8 statement `Discount 1:N Customer` is treated as outdated for this ERD. |
| O03 | Each `Contract` may use at most one Discount via `Contract.DiscountId`. | Customer can have multiple discounts, but a Contract selects zero or one. |
| O04 | Use `Invoice` for both Deposit and Rental Fee. Do not create a separate `MonthlyRentalFee` entity. | One billing abstraction reduces duplicated payment structures. |
| O05 | `Reservation.DepositStatus` is removed. | Deposit payment state is sourced from `Invoice(DEPOSIT)` and `Payment`; `Reservation.DepositAmount` remains the snapshot. |
| O06 | `Visit` uses polymorphic `EntityId`: RESERVATION -> Reservation, ACCESS/RETURN -> Contract. | Reuses one Visit model for all visit types. |
| O07 | `Visit.ActualReturnDate` is kept for RETURN visits. | Required to distinguish Normal Return from Early Return. |
| O08 | `Contract` does not store `UnitTypeId`, `StartDate`, `EndDate`, or `SignedAt`. | UnitType is derived from StorageUnit; contract dates are derived from month range; signing/handover timing is represented by Visit/business flow. |
| O09 | `ContractExtension.CreatedBy` is removed. | Keep renewal history minimal. |
| O10 | `Inspection.NextUnitStatus` is removed. | Return releases contractual occupancy/capacity; StorageUnit operational state is handled directly through its own status lifecycle. |
| O11 | `InspectionEvidence.FileData` stores binary data (`bytea` in PostgreSQL), not FileUrl. | Evidence is stored in-database in Release 1. |
| O12 | `LateFee` and `ExtraFee` have no approval Status. | They are recorded/applied at return according to Contract/return state; they do not have an independent approval lifecycle. |
| O13 | `NotificationLog` has only `PENDING` and `SENT`. | Send failure leaves the record `PENDING` for retry. |
| O14 | Policy is expanded into configurable operational parameters. | Stored procedures read Policy values instead of hard-coding business numbers. |
| O15 | `FacilityAssignment`, `Booking`, `ReturnProcess`, `Renewal`, `Refund`, `DamageFee`, `DailyTask`, `Report`, `DiscountRedemption`, separate `Maintenance`, and separate `MonthlyRentalFee` are not tables. | Their behavior/data is represented by existing entities or derived queries. |
| O16 | `UserAccount.PhoneNumber` is UNIQUE in addition to UNIQUE Email. | Customer login identifier is PhoneNumber; SRS V9 explicit data override. |
| O17 | `Contract.PolicyId = Reservation.PolicyId` at Complete Handover. | Contract must inherit the Reservation-captured Policy; it must not capture the currently active Policy at handover. |
| O18 | First-month rent is collected offline at handover, not through Invoice or Payment. | Amount equals `Reservation.LockedRentalPrice` with no first-month Contract Discount; `Contract` creation records Staff acknowledgement. Discounts may apply only to later eligible Rental Fee invoices. |
| O19 | New Facility starts `INACTIVE`; new StorageUnit starts `AVAILABLE`. | SRS V9 resolves both initial-status decisions. |
| O20 | StorageUnit allows preventive-maintenance transition `AVAILABLE -> MAINTENANCE`. | No separate Maintenance entity/workflow is introduced. |
| O21 | SupportTicket Category is a fixed Release 1 enum and includes `OTHER`. | SRS V9 locks the catalogue. |
| O22 | Damage decision is `PENDING -> APPROVED/REJECTED` by same-Facility `FACILITY_MANAGER`; return finalization is blocked while any DamageRecord is PENDING. | SRS V9 locks approval actor/lifecycle. |
| O23 | A Discount referenced by any Contract has immutable `CustomerId`, `Percentage`, `EffectiveFrom`, and `EffectiveTo`. | Inactivation blocks new selection only; existing Contract usage remains valid. |
| O24 | Production DamageType is fixed seed data: `LOCK_DAMAGE`, `DOOR_DAMAGE`, `WALL_DAMAGE`, `FLOOR_DAMAGE`, `WATER_DAMAGE`, `OTHER`; no Release 1 DamageType CRUD. | SRS V9 locks production provisioning. |
| O25 | `Contract 1:N Inspection` historically. | A Contract may have historical inspection records; each Inspection still belongs to one Contract. |
| O26 | Employee creation provisions a random 16-character initial password by email; account remains INACTIVE until email send is accepted. | Plaintext is never persisted/logged; retry generates a new credential. |
| **V11-O27** | **`Customer.Address` remains optional/nullable (`varchar(1000)` target maximum).** | Owner approval OD-01; own registration/profile PATCH only; no future use/privacy disclosure inferred. |
| **V11-O28** | **Approve persisted `HandoverUnitAssignment` as 28th Release-1 entity.** | OD-02; one active selection per Reservation/StorageUnit, immutable event history, atomic consume during handover. |
| **V11-O29** | **Grant Administrator read-only StorageUnit timeline GETs.** | OD-03; audit-based history, no cross-feature mutation expansion. |
| **V11-O30** | **UnitType `ImageUrl`/`VideoUrl` nullable columns are public catalog fields.** | OD-05 acknowledges CAT-003/004/005 public projection, not upload/CMS permissions. |

---

## 3. Core Design Rationale

### 3.1 Month-based rental period

Reservation and Contract use `StartMonth` / `EndMonth`. This simplifies monthly capacity checking and monthly billing while avoiding arbitrary date-overlap complexity.

### 3.2 Reservation holds capacity, not a concrete unit

Reservation does not store `StorageUnitId`. Capacity is held by Facility, UnitType and month range. After the associated RESERVATION Visit is `CHECKED_IN`, same-Facility Manager may preassign one matching `AVAILABLE` StorageUnit in `HandoverUnitAssignment`; this is a nonoccupancy selection and does not consume capacity. Contract creation at handover still establishes actual unit occupancy and atomically consumes the approved active assignment.

### 3.3 Contract is the occupancy source after handover

When handover succeeds atomically, the Reservation hold ends and the selected StorageUnit becomes occupied by an `ACTIVE` Contract. The system must not double-count Reservation hold and Contract occupancy.

### 3.4 Renewal is append-only history, not a workflow entity

A successful renewal directly updates `Contract.EndMonth` and appends one `ContractExtension`. Failed renewal attempts do not create extension rows.

### 3.5 Policy versioning

Each Policy row is a complete version. Updating policy means inserting a new row and inactivating the previous active version. Reservation captures the active `PolicyId` when created; at Complete Handover, Contract inherits exactly `Reservation.PolicyId`. Existing Reservation/Contract rows continue using that captured Policy version.

Stored procedures contain the **algorithm**, while Policy stores **configurable values**. Changing a policy parameter should therefore not require editing stored procedure code unless the actual algorithm/formula changes.

### 3.6 Visit and Invoice polymorphic references

`Visit.EntityId` and `Invoice.EntityId` are polymorphic references. A normal relational FK cannot reference two possible target tables. Their target validity must therefore be enforced by stored procedures/triggers and application validation.

### 3.7 Return and inspection

Return uses `RETURN Visit` when the Customer returns physically. External recovery can create Inspection without a Visit. `ActualReturnDate` determines normal versus early return. Inspection is claim-based (`PENDING -> IN_PROGRESS -> COMPLETED`) to prevent two Staff members from processing the same inspection concurrently.

### 3.8 Extra charge settlement boundary

Late Fee, Extra Fee and Damage are system records used to calculate Deposit settlement. The system records `TotalDeduction`, `RefundAmount`, and `AdditionalAmountDue`; actual money transfer/collection for those settlement outcomes is outside the core demo.

---

## 4. Entity Data Dictionary

### 4.1 `UserRole`

**Purpose:** Fixed RBAC role master.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `RoleId` | uuid | No | PK | Role identifier. |
| `RoleName` | varchar(50) | No | UNIQUE | One of the five fixed role names. |
| `Description` | varchar(1000) | Yes |  | Optional role description. |

Allowed `RoleName`: `CUSTOMER`, `FACILITY_STAFF`, `FACILITY_MANAGER`, `BUSINESS_OPERATIONS_MANAGER`, `SYSTEM_ADMINISTRATOR`.

### 4.2 `UserAccount`

**Purpose:** Authentication/account state shared by Customer and Employee profiles.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `UserAccountId` | uuid | No | PK | Account identifier. |
| `RoleId` | uuid | No | FK -> UserRole | Assigned role. |
| `Email` | varchar(254) | No | UNIQUE | Login/contact email. |
| `PhoneNumber` | varchar(30) | No | UNIQUE | Customer login identifier; globally unique. |
| `PasswordHash` | varchar(255) | No |  | Password hash. |
| `Status` | varchar(50) | No | CHECK | `ACTIVE`, `INACTIVE`. |
| `EmailVerifiedAt` | timestamptz(3) | Yes |  | Reserved verification timestamp; Release 1 does not implement email verification and this field is not a login precondition. |
| `CreatedAt` | timestamptz(3) | No |  | Account creation time. |

Rules: an inactive account cannot log in or initiate new actions. Historical business records remain intact. Customer login uses unique `PhoneNumber`; Employee login uses unique `Email`. Login identifiers are not changeable in Release 1. Password validation is 8..64 characters, no mandatory composition classes, encoded BCrypt input <= 72 bytes; only `PasswordHash` is persisted.

### 4.3 `Customer`

**Purpose:** Customer business profile.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `CustomerId` | uuid | No | PK | Customer identifier. |
| `UserAccountId` | uuid | No | FK, UNIQUE -> UserAccount | One profile per account. |
| `FullName` | varchar(150) | No |  | Customer name. |
| `Address` | varchar(1000) | Yes | Nullable, optional | Customer-supplied optional physical address; allowed at AUTH-001 and AUTH-005 only, not automatically included in public/other-role read projections. No current core business flow depends on it. |
| `CCCD` | varchar(20) | Yes | UNIQUE when non-null | Optional at registration/profile storage, but saved CCCD matching OD-10 is required for new RES-001 (SRS OD-09 / BR-RES-09). Own AUTH-004 includes nullable profile.cccd; observed in source at 2945788, not runtime-certified. No public/Employee/directory disclosure. |

Rule: Customer has no `DiscountId`; relation is `Customer 1:N Discount`.

**OD-09 conditional requirement:** Keep the physical CCCD column nullable, varchar(20) and unique when present; OD-10 requires newly supplied nonblank values and saved values used by new RES-001 to contain exactly 12 ASCII digits. Null/empty/whitespace is missing for the new-Reservation precondition; OD-10 expressly supplies the 12-digit format rule; do not infer eKYC/verified identity. Keep the existing FullName field; add no LegalName, real-name column, verification state, identity table or continuation/draft table. Existing profile clear/update semantics are unchanged; a later new RES-001 must re-read current profile data. Do not retroactively invalidate old rentals or invent/backfill citizen IDs. See §12.4 and SRS §14.15 for save/return behavior and privacy.

### 4.4 `Employee`

**Purpose:** Employee business profile.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `EmployeeId` | uuid | No | PK | Employee identifier. |
| `UserAccountId` | uuid | No | FK, UNIQUE -> UserAccount | One employee profile per account. |
| `FacilityId` | uuid | Yes | FK -> Facility | Required for Facility Staff/Manager; null for global roles. |
| `FullName` | varchar(150) | No |  | Employee name. |

Rules: `FACILITY_STAFF` and `FACILITY_MANAGER` must have exactly one Facility; Business Operations Manager and System Administrator have no required Facility assignment. Administrator-created Employee accounts start `INACTIVE`; FRMS generates a random 16-character initial password, persists only its BCrypt hash, emails the plaintext credential through `IEmailService`, and activates the account only after the email send is accepted. Retry provisioning generates a new password/hash. Password reset/change is outside Release 1.

### 4.5 `Facility`

**Purpose:** Storage service location.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `FacilityId` | uuid | No | PK | Facility identifier. |
| `Name` | varchar(150) | No |  | Facility name. |
| `Address` | varchar(1000) | No |  | Facility address. |
| `ContactInfo` | varchar(1000) | Yes |  | Contact information. |
| `Description` | varchar(1000) | Yes |  | Facility description. |
| `Status` | varchar(50) | No | CHECK | `ACTIVE`, `INACTIVE`. |

Rules: a newly created Facility starts `INACTIVE`. An inactive Facility cannot accept new Reservations; existing confirmed lifecycles may continue. Activation is explicit.

### 4.6 `UnitType`

**Purpose:** Global storage type master and current rental price.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `UnitTypeId` | uuid | No | PK | Unit type identifier. |
| `Name` | varchar(150) | No |  | Type name. |
| `Mode` | varchar(50) | No | CHECK | `PUBLIC`, `PRIVATE`. |
| `Size` | varchar(150) | No |  | Physical size/specification. |
| `RentalPrice` | numeric(18,2) | No | CHECK >= 0 | Current monthly rental price. |
| `Description` | varchar(1000) | Yes |  | Additional characteristics. |
| `ImageUrl` | varchar(2048) | Yes | Pg012 nullable | Public CAT-003/004/005 `imageUrl` field; URL validation/Swagger parity pending. |
| `VideoUrl` | varchar(2048) | Yes | Pg012 nullable | Public CAT-003/004/005 `videoUrl` field; no upload/CMS permission. |

Rule: price is global in V1; no Facility-specific price table.

**V11 observed Pg012 media extension:** `UnitType.ImageUrl` and `UnitType.VideoUrl` are nullable `varchar(2048)` columns. Both already appear on public CAT-003/004/005 DTOs; do not infer URL upload/CMS or arbitrary private resource access.


### 4.7 `StorageUnit`

**Purpose:** Physical storage position/unit.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `StorageUnitId` | uuid | No | PK | Physical unit identifier. |
| `FacilityId` | uuid | No | FK -> Facility | Owning Facility. |
| `UnitTypeId` | uuid | No | FK -> UnitType | Unit type. |
| `UnitCode` | varchar(50) | No | No composite UNIQUE constraint in Pg001; recommended `(FacilityId, UnitCode)` is a separate proposed rule | Operational unit code. |
| `LocationInfo` | varchar(1000) | Yes |  | Physical location description. |
| `Status` | varchar(50) | No | CHECK | `AVAILABLE`, `IN_USE`, `INSPECTION`, `MAINTENANCE`. |

Legal transitions: `AVAILABLE -> IN_USE`, `AVAILABLE -> MAINTENANCE`, `IN_USE -> INSPECTION`, `INSPECTION -> AVAILABLE`, `INSPECTION -> MAINTENANCE`, `MAINTENANCE -> AVAILABLE`. A newly created StorageUnit starts `AVAILABLE`. Direct `AVAILABLE -> MAINTENANCE` is preventive maintenance by the same-Facility Manager and requires no ACTIVE Contract. UnitType reassignment is rejected while the unit is `IN_USE`, `INSPECTION`, or otherwise occupied by an ACTIVE Contract.

### 4.8 `Reservation`

**Purpose:** Future capacity hold for one Customer, Facility, UnitType and month range.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `ReservationId` | uuid | No | PK | Reservation identifier. |
| `CustomerId` | uuid | No | FK -> Customer | Owner. |
| `FacilityId` | uuid | No | FK -> Facility | Target Facility. |
| `UnitTypeId` | uuid | No | FK -> UnitType | Requested type. |
| `PolicyId` | uuid | No | FK -> Policy | Policy version captured at reservation creation. |
| `StartMonth` | date | No |  | Rental start month; stored as canonical month date. |
| `EndMonth` | date | No | CHECK >= StartMonth | Rental end month. |
| `LockedRentalPrice` | numeric(18,2) | No | CHECK >= 0 | Initial rental price snapshot. |
| `DepositAmount` | numeric(18,2) | No | CHECK >= 0 | Deposit snapshot; initially derived from UnitType price. |
| `Status` | varchar(50) | No | CHECK | `PENDING_DEPOSIT`, `CONFIRMED`, `COMPLETED`, `CANCELLED`. |
| `CreatedAt` | timestamptz(3) | No |  | Creation timestamp. |

No `StorageUnitId`. No `DepositStatus`.

Lifecycle: `PENDING_DEPOSIT -> CONFIRMED -> COMPLETED`; cancellation allowed from `PENDING_DEPOSIT` or `CONFIRMED` according to visit state/rules.

### 4.9 `Visit`

**Purpose:** Customer visit for reservation handover, storage access, or return.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `VisitId` | uuid | No | PK | Visit identifier. |
| `EntityId` | uuid | No | Polymorphic | ReservationId for RESERVATION; ContractId for ACCESS/RETURN. |
| `EmployeeId` | uuid | Yes | FK -> Employee | Actual Staff actor; null before handled. |
| `VisitType` | varchar(50) | No | CHECK | `RESERVATION`, `ACCESS`, `RETURN`. |
| `VisitDate` | date | No |  | Scheduled visit date. |
| `ActualReturnDate` | date | Yes |  | Set only for RETURN when physical return is confirmed. |
| `Status` | varchar(50) | No | CHECK | `SCHEDULED`, `CHECKED_IN`, `CHECKED_OUT`, `CANCELLED`. |

Lifecycle: `SCHEDULED -> CHECKED_IN -> CHECKED_OUT` or `SCHEDULED -> CANCELLED`. After `CHECKED_IN`, cancellation is forbidden.

### 4.10 `Contract`

**Purpose:** Active/finished rental agreement and actual unit occupancy source.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `ContractId` | uuid | No | PK | Contract identifier. |
| `ReservationId` | uuid | No | FK, UNIQUE -> Reservation | One Reservation creates at most one Contract. |
| `CustomerId` | uuid | No | FK -> Customer | Contract owner. |
| `FacilityId` | uuid | No | FK -> Facility | Facility. |
| `StorageUnitId` | uuid | No | FK -> StorageUnit | Assigned physical unit. |
| `PolicyId` | uuid | No | FK -> Policy | Inherited from `Reservation.PolicyId` at Complete Handover; Contract-level lifecycle keeps this version. |
| `DiscountId` | uuid | Yes | FK -> Discount | Zero or one Discount selected for this Contract. |
| `StartMonth` | date | No |  | Contract start month. |
| `EndMonth` | date | No | CHECK >= StartMonth | Current effective end month. |
| `Status` | varchar(50) | No | CHECK | `ACTIVE`, `COMPLETED`, `TERMINATED`. |

Rules: selected Discount must belong to the same Customer and be valid when applied. One StorageUnit may have many historical Contracts but at most one `ACTIVE` Contract at a time.

### 4.11 `ContractExtension`

**Purpose:** Append-only history of successful Contract renewals.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `ContractExtensionId` | uuid | No | PK | Extension identifier. |
| `ContractId` | uuid | No | FK -> Contract | Extended Contract. |
| `OldEndMonth` | date | No |  | End month before renewal. |
| `NewEndMonth` | date | No | CHECK > OldEndMonth | New end month. |
| `AppliedMonthlyPrice` | numeric(18,2) | No | CHECK >= 0 | Price snapshot for extension period. |
| `CreatedAt` | timestamptz(3) | No |  | Renewal completion time. |

No status. UPDATE/DELETE should be blocked.

### 4.12 `Invoice`

**Purpose:** Reservation Deposit Invoice (`DEPOSIT`) or post-first-month Contract Rental Fee Invoice (`RENTAL_FEE`). There is **no first-month Rental Fee Invoice**. Historical evidence is immutable.

| Attribute | PostgreSQL type | Null | Rule |
|---|---|---:|---|
| `InvoiceId` | uuid | No | PK |
| `EntityId` | uuid | No | polymorphic: DEPOSIT -> Reservation; RENTAL_FEE -> Contract |
| `InvoiceType` | varchar(50) | No | `DEPOSIT`, `RENTAL_FEE` |
| `BillingMonth` | date | Conditional | null for DEPOSIT; required and strictly **after** `Contract.StartMonth` for RENTAL_FEE |
| `BaseAmount` | numeric(18,2) | No | nonnegative |
| `DiscountId` | uuid | Yes | null for DEPOSIT; applicable captured Contract Discount only on eligible later-month RENTAL_FEE |
| `DiscountAmount` | numeric(18,2) | No | snapshot, default 0 |
| `AmountDue` | numeric(18,2) | No | nonnegative authoritative amount |
| `DueDate` | timestamptz(3) | No | UTC deadline |
| `Status` | varchar(50) | No | `UNPAID`, `PAID`, `OVERDUE`, `CANCELLED` |
| `PaidAt` | timestamptz(3) | Yes | Set once on first verified `UNPAID/OVERDUE -> PAID` transition using provider timestamp; duplicate results preserve it |
| `CreatedAt` | timestamptz(3) | No | UTC creation |

**Billing:** Staff receives first-month rent offline before handover. The signed/authorized `OPS-004` handover event acknowledges this; `Contract.StartMonth` revenue derives once from `Reservation.LockedRentalPrice`, not from a fabricated Invoice/Payment. Later invoices use Policy/price/Discount snapshots. Never double-count first month or create a post-hoc first-month Invoice.

### 4.13 `Payment`

**Purpose:** Invoice-backed payOS attempt/result; one Invoice may have multiple deliberate payment attempts. Historical `MOMO`/`VNPAY` rows are retained as legacy database evidence, not active payment gateway integrations. The current model has non-null `Payment.InvoiceId`; incompatible historical data must be handled only by an explicitly reviewed migration strategy.

| Attribute | PostgreSQL type | Null | Rule |
|---|---|---:|---|
| `PaymentId` | uuid | No | PK |
| `InvoiceId` | uuid | No | FK -> existing eligible Invoice, never first-month rent |
| `IdempotencyKey` | uuid | No | Global UNIQUE; generated by frontend and retained for Payment row lifetime |
| `ProviderOrderCode` | bigint | Yes | Unique when present; allocated by server PostgreSQL sequence; do not restart sequence against existing merchant history |
| `Amount` | numeric(18,2) | No | > 0; must match payable Invoice |
| `PaymentMethod` | varchar(50) | No | CHECK permits `MOMO`, `VNPAY`, `PAYOS` for preserved history; new attempts create `PAYOS` only |
| `TransactionCode` | varchar(150) | Yes | Provider verified reference; UNIQUE when present |
| `Status` | varchar(50) | No | `PENDING`, `SUCCESS`, `FAILED` |
| `PaidAt` | timestamptz(3) | Yes | Verified provider UTC timestamp for SUCCESS |
| `PaymentUrl` | varchar(2048) | Yes | Persist private checkoutUrl for authorized same-key replay; NEVER disclose through PAY-003 or logs |
| `PaymentUrlExpiresAt` | timestamptz(3) | Yes | Known provider/server expiry; do not invent unavailable timestamps |
| `CreatedAt` | timestamptz(3) | No | UTC creation |

**PAY-001 idempotency lock:** frontend generates fresh UUID per deliberate user action and sends `Idempotency-Key`; retries of the SAME action reuse the key. Same key + same authorized Invoice returns the existing Payment/session without another row/provider session; same key + another Invoice returns `409 PAYMENT_IDEMPOTENCY_CONFLICT` without disclosing another Customer's Payment. Database UNIQUE on `Payment.IdempotencyKey` settles concurrent creation; only the newly created winner may open a provider session. Existing attempts resolve before provider configuration check. No DB transaction may span an external provider request. Failure/cancellation before creating a new attempt must not persist Payment or consume the key. Known expired URL yields controlled conflict/expired response on same-key replay, no hidden recovery.

**PAY-004:** validate payOS signature and normalize provider-local `transactionDateTime` (UTC+7) to UTC. Verified unknown/sample webhook returns HTTP 200 without business mutations. Signed successful callback maps `ProviderOrderCode` to Payment, uses verified `reference` for transaction uniqueness, and calls authoritative PostgreSQL result routine. Successful retry is idempotent: do not duplicate Payment/Invoice transitions, PaidAt, or financial audit. Browser redirect never marks a Payment paid. Historical 2026-10-10 test payment (order 1008, 2,000 VND) was individually verified in the prior approved SRS/DD; this is limited evidence, **not general release acceptance**. No outgoing refunds/transfers are authorized.

**PAY-002:** retired. No first-month online Payment, no new null-Invoice Payment, no synthetic links to preserve historical records. Existing anomalous/historical rows require an explicit reviewed migration plan, never automatic cleanup/backfill.

---

### 4.14 `LateFee`

**Purpose:** Late fee associated with an overdue Rental Fee invoice.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `LateFeeId` | uuid | No | PK | Late fee identifier. |
| `InvoiceId` | uuid | No | FK, UNIQUE -> Invoice | Overdue rental invoice. |
| `OverdueDays` | integer | No | CHECK >= 1 | Number of overdue days. |
| `Amount` | numeric(18,2) | No | CHECK >= 0 | Calculated late fee. |
| `CalculatedAt` | timestamptz(3) | No |  | Last calculation time. |

No separate status. Applicability is decided during return/settlement according to Contract and return state.

### 4.15 `Discount`

**Purpose:** Customer-owned percentage discount definition.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `DiscountId` | uuid | No | PK | Discount identifier. |
| `CustomerId` | uuid | No | FK -> Customer | Owning Customer. |
| `Name` | varchar(150) | No |  | Discount name. |
| `Percentage` | numeric(5,2) | No | CHECK 0..100 | Discount percentage. |
| `Status` | varchar(50) | No | CHECK | `ACTIVE`, `INACTIVE`. |
| `EffectiveFrom` | timestamptz(3) | No |  | Start of validity. |
| `EffectiveTo` | timestamptz(3) | Yes |  | End of validity. |

Confirmed cardinality: `Customer 1:N Discount`. Each Contract uses zero or one Discount. No single-use constraint is imposed. Once a Discount is referenced by any Contract, `CustomerId`, `Percentage`, `EffectiveFrom`, and `EffectiveTo` are immutable. Setting `Status = INACTIVE` prevents new Contract selection but does not invalidate existing Contracts; create a new Discount row to change financial/effective semantics.

### 4.16 `Policy`

**Purpose:** Versioned configurable business parameters.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `PolicyId` | uuid | No | PK | Policy version identifier. |
| `Version` | integer | No | UNIQUE | Version number. |
| `Status` | varchar(50) | No | CHECK | `ACTIVE`, `INACTIVE`. |
| `EffectiveFrom` | timestamptz(3) | No |  | Start of effectiveness. |
| `EffectiveTo` | timestamptz(3) | Yes |  | End of effectiveness. |
| `DepositTimeoutHours` | integer | No | CHECK > 0 | Deposit payment timeout. |
| `ReservationVisitStartDay` | integer | No | CHECK 1..31 | First allowed handover visit day. |
| `ReservationVisitEndDay` | integer | No | CHECK 1..31 | Last allowed handover visit day. |
| `MonthlyPaymentDueDay` | integer | No | CHECK 1..31 | Monthly payment due-day boundary. |
| `OverdueStartDay` | integer | No | CHECK 1..31 | Day unpaid rental invoice becomes overdue. |
| `LateFeeDivisorDays` | integer | No | CHECK > 0 | Divisor used in late fee formula. |
| `EarlyReturnWaiveFeeUntilDay` | integer | No | CHECK 1..31 | Early-return day cutoff for waiving unpaid current-month rental fee. |
| `CreatedAt` | timestamptz(3) | No |  | Version creation time. |

Versioning rule: never overwrite business values of an existing version; create a new active row and inactivate the previous version.

### 4.17 `SupportTicket`

**Purpose:** Track Contract-related support issues and Staff responsibility.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `SupportTicketId` | uuid | No | PK | Ticket identifier. |
| `ContractId` | uuid | No | FK -> Contract | Related Contract. |
| `CustomerId` | uuid | No | FK -> Customer | Requesting Customer. |
| `AssignedEmployeeId` | uuid | Yes | FK -> Employee | Assigned Facility Staff. |
| `Category` | varchar(50) | No | CHECK | Issue category. |
| `Description` | varchar(1000) | No |  | Issue details. |
| `Status` | varchar(50) | No | CHECK | `OPEN`, `IN_PROGRESS`, `COMPLETED`, `CANCELLED`. |
| `ResultNote` | varchar(1000) | Yes |  | Resolution/result note. |
| `CreatedAt` | timestamptz(3) | No |  | Creation time. |
| `CompletedAt` | timestamptz(3) | Yes |  | Completion time. |

Fixed Release 1 categories: `UNIT_ISSUE`, `LOCK_KEY_ISSUE`, `ACCESS_CARD_CODE_ISSUE`, `PAYMENT_ISSUE`, `STORED_ITEM_ISSUE`, `DAMAGE_ISSUE`, `OTHER`. If `Category = OTHER`, `Description` must contain sufficient issue detail. The catalogue is not a configurable master table in Release 1.

### 4.18 `Inspection`

**Purpose:** Official inspection record after actual return or external recovery.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `InspectionId` | uuid | No | PK | Inspection identifier. |
| `ContractId` | uuid | No | FK -> Contract | Contract being returned/recovered. |
| `StorageUnitId` | uuid | No | FK -> StorageUnit | Unit being inspected. |
| `VisitId` | uuid | Yes | FK -> Visit | RETURN Visit; null for external recovery. |
| `EmployeeId` | uuid | Yes | FK -> Employee | Staff who claims/performs inspection. |
| `Status` | varchar(50) | No | CHECK | `PENDING`, `IN_PROGRESS`, `COMPLETED`. |
| `ConditionNote` | varchar(1000) | Yes |  | Condition findings. |
| `CompletedAt` | timestamptz(3) | Yes |  | Completion time. |

No `NextUnitStatus` in Release 1.

### 4.19 `DamageType`

**Purpose:** Master list of damage categories and optional default amount.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `DamageTypeId` | uuid | No | PK | Damage type identifier. |
| `Name` | varchar(150) | No | UNIQUE | Damage type name. |
| `DefaultAmount` | numeric(18,2) | Yes | CHECK >= 0 | Suggested/default amount. |
| `Status` | varchar(50) | No | CHECK | `ACTIVE`, `INACTIVE`. |

Release 1 production rows are fixed seed data: `LOCK_DAMAGE`, `DOOR_DAMAGE`, `WALL_DAMAGE`, `FLOOR_DAMAGE`, `WATER_DAMAGE`, `OTHER`. All start `ACTIVE`; `DefaultAmount` is `NULL` unless approved deployment data provides a value. Release 1 has no DamageType CRUD workflow.

### 4.20 `DamageRecord`

**Purpose:** Damage identified during Inspection.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `DamageRecordId` | uuid | No | PK | Damage record identifier. |
| `InspectionId` | uuid | No | FK -> Inspection | Source Inspection. |
| `DamageTypeId` | uuid | No | FK -> DamageType | Damage category. |
| `DamageAmount` | numeric(18,2) | No | CHECK >= 0 | Actual recorded damage amount. |
| `Note` | varchar(1000) | Yes |  | Damage details. |
| `Status` | varchar(50) | No | CHECK | `PENDING`, `APPROVED`, `REJECTED`. |
| `CreatedAt` | timestamptz(3) | No |  | Record creation time. |

Facility Staff records DamageRecord as `PENDING`. Only the same-Facility Facility Manager may atomically decide `PENDING -> APPROVED` or `PENDING -> REJECTED`; both terminal states are audited. Only APPROVED `DamageAmount` is included in Deposit settlement, and return finalization is blocked while any related DamageRecord remains `PENDING`.

### 4.21 `InspectionEvidence`

**Purpose:** Binary evidence attached to an Inspection.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `InspectionEvidenceId` | uuid | No | PK | Evidence identifier. |
| `InspectionId` | uuid | No | FK -> Inspection | Parent Inspection. |
| `FileData` | bytea | No |  | Binary evidence content. |
| `EvidenceType` | varchar(50) | No | CHECK | `IMAGE`, `VIDEO`, `DOCUMENT`. |
| `CreatedAt` | timestamptz(3) | No |  | Upload/creation time. |

### 4.22 `ExtraFeeType`

**Purpose:** Configurable master for operational extra-fee categories.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `ExtraFeeTypeId` | uuid | No | PK | Type identifier. |
| `Name` | varchar(150) | No | UNIQUE | Extra-fee type. |
| `DefaultAmount` | numeric(18,2) | No | CHECK >= 0 | Default configured amount. |
| `Status` | varchar(50) | No | CHECK | `ACTIVE`, `INACTIVE`. |

Allowed V1 names:

- `KEY_REPLACEMENT`
- `ACCESS_CARD_REPLACEMENT`
- `LOCK_REPLACEMENT`
- `CLEANING_FEE`
- `OTHER`

### 4.23 `ExtraFee`

**Purpose:** Actual extra fee recorded from an Inspection.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `ExtraFeeId` | uuid | No | PK | Extra fee identifier. |
| `InspectionId` | uuid | No | FK -> Inspection | Source Inspection. |
| `ExtraFeeTypeId` | uuid | No | FK -> ExtraFeeType | Configured fee type. |
| `Amount` | numeric(18,2) | No | CHECK >= 0 | Actual amount snapshot. |
| `Reason` | varchar(1000) | No |  | Operational reason/context. |
| `CreatedAt` | timestamptz(3) | No |  | Creation time. |

No independent status in Release 1.

### 4.24 `DepositSettlement`

**Purpose:** Final recorded Deposit deduction/refund/additional obligation for a Contract return.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `DepositSettlementId` | uuid | No | PK | Settlement identifier. |
| `ContractId` | uuid | No | FK, UNIQUE -> Contract | One settlement per Contract. |
| `TotalDeduction` | numeric(18,2) | No | CHECK >= 0 | Applicable LateFee + ExtraFee + Damage amount. |
| `RefundAmount` | numeric(18,2) | No | CHECK >= 0 | Amount recorded as refundable. |
| `AdditionalAmountDue` | numeric(18,2) | No | CHECK >= 0 | Amount beyond paid Deposit. |
| `Status` | varchar(50) | No | CHECK | `PENDING`, `FINALIZED`. |
| `CalculatedAt` | timestamptz(3) | No |  | Calculation time. |

Normal return formula:

`RefundAmount = max(DepositPaidAmount - TotalDeduction, 0)`

`AdditionalAmountDue = max(TotalDeduction - DepositPaidAmount, 0)`

Early Return forfeits Deposit, so `RefundAmount = 0`.

### 4.25 `LoginHistory`

**Purpose:** Authentication history for administration/security review.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `LoginHistoryId` | uuid | No | PK | Log identifier. |
| `UserAccountId` | uuid | No | FK -> UserAccount | Account. |
| `LoginAt` | timestamptz(3) | No |  | Attempt time. |
| `IpAddress` | varchar(45) | Yes |  | Source IP. |
| `DeviceInfo` | varchar(1000) | Yes |  | Browser/device info. |
| `Status` | varchar(50) | No | CHECK | `SUCCESS`, `FAILED`. |

Append-only. A LoginHistory row is written only when the supplied login identifier resolves to a UserAccount; unknown identifiers are recorded only in technical/security logging because `UserAccountId` is non-null.

### 4.26 `AuditLog`

**Purpose:** Business/system activity audit trail.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `AuditLogId` | uuid | No | PK | Audit identifier. |
| `UserAccountId` | uuid | Yes | FK -> UserAccount | Null for system-generated action. |
| `Action` | varchar(150) | No |  | Business/system action. |
| `EntityType` | varchar(150) | No |  | Affected entity type. |
| `EntityId` | uuid | Yes |  | Affected record identifier. |
| `OldValue` | text | Yes | Nullable `text`; PostgreSQL JSON OBJECT/ARRAY CHECK when non-null | Previous value snapshot. |
| `NewValue` | text | Yes | Nullable `text`; PostgreSQL JSON OBJECT/ARRAY CHECK when non-null | New value snapshot. |
| `CreatedAt` | timestamptz(3) | No |  | Action time. |

Append-only.

### 4.27 `NotificationLog`

**Purpose:** Outbound notification queue/history.

| Attribute | PostgreSQL type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `NotificationLogId` | uuid | No | PK | Notification identifier. |
| `UserAccountId` | uuid | No | FK -> UserAccount | Recipient. |
| `NotificationType` | varchar(50) | No |  | e.g. reservation/payment/visit/contract/overdue/support. |
| `Content` | varchar(1000) | No |  | Notification content. |
| `Status` | varchar(50) | No | CHECK | `PENDING`, `SENT`. |
| `SentAt` | timestamptz(3) | Yes |  | Set only when successfully sent. |
| `CreatedAt` | timestamptz(3) | No |  | Creation time. |

Failed delivery remains `PENDING` for retry.

---

### 4.28 `HandoverUnitAssignment` — OWNER APPROVED, V11

**Purpose:** Track a Manager-selected physical StorageUnit after RESERVATION Visit CHECKED_IN and before Staff Complete Handover; preserve non-deletable assignment history independently of Reservation capacity hold.

| Attribute | PostgreSQL type | Null | Constraint / purpose |
|---|---|---:|---|
| `AssignmentId` | uuid | No | PK; UUID generated for new assignment |
| `ReservationId` | uuid | No | FK -> Reservation |
| `StorageUnitId` | uuid | No | FK -> StorageUnit |
| `AssignedByEmployeeId` | uuid | No | FK -> Employee; derived from authenticated Manager account, never client submitted |
| `AssignedAt` | timestamptz(3) | No | UTC creation time, DB `clock_timestamp()` |
| `Status` | varchar(12) | No | CHECK exact `ACTIVE`, `REPLACED`, `CONSUMED`, `RELEASED` |
| `ClosedAt` | timestamptz(3) | Conditional | null only for ACTIVE; non-null for terminal history |

**Physical constraints (Pg013):** partial UNIQUE `ReservationId WHERE Status='ACTIVE'`; partial UNIQUE `StorageUnitId WHERE Status='ACTIVE'`; composite historical index (`ReservationId`, `AssignedAt DESC`). `ClosedAt` CHECK: `(Status='ACTIVE') = (ClosedAt IS NULL)`. FK delete behavior RESTRICT. History guard rejects DELETE, mutation of identifiers, old records and arbitrary status rewrites. Allowed transition: `ACTIVE -> REPLACED | RELEASED | CONSUMED`; never re-activate terminal row. Assigning same unit for already active Reservation returns current row without duplicate history.

**Atomicity:** precondition Reservation `CONFIRMED`, linked RESERVATION Visit `CHECKED_IN`, no Contract yet, matching Facility and UnitType, StorageUnit `AVAILABLE`; actor `FACILITY_MANAGER` assigned to Facility. Concurrency guard must prevent two active Reservations selecting same Unit. Staff may GET only, not assign/release. `usp_CompleteHandover` Contract insertion must consume exact active assignment in same PostgreSQL transaction (guard before Contract INSERT); unit mismatch/no assignment causes rollback. A preassignment does not change StorageUnit from AVAILABLE or consume Reservation capacity a second time.

**Authorization and API source:** SRS V11 §0.0.9 OD-02 and API Spec V11 §13A.1 (`PUT/GET/DELETE /api/v1/reservations/{reservationId}/handover-unit-assignment`). Approval confirmed; live migration/E2E/concurrency test evidence pending.

---

## 5. Relationship Dictionary

| Parent | Cardinality | Child | Implementation / Rule |
|---|---|---|---|
| UserRole | 1:N | UserAccount | `UserAccount.RoleId` FK. |
| UserAccount | 1:0..1 | Customer | `Customer.UserAccountId` UNIQUE FK. |
| UserAccount | 1:0..1 | Employee | `Employee.UserAccountId` UNIQUE FK. |
| Facility | 1:N | Employee | `Employee.FacilityId`; nullable for global roles. |
| Facility | 1:N | StorageUnit | Direct FK. |
| UnitType | 1:N | StorageUnit | Direct FK. |
| Customer | 1:N | Reservation | Direct FK. |
| Facility | 1:N | Reservation | Direct FK. |
| UnitType | 1:N | Reservation | Direct FK. |
| Policy | 1:N | Reservation | Captured policy version. |
| Reservation | 1:0..1 | Contract | `Contract.ReservationId` UNIQUE. |
| Reservation | 1:N historical, 0..1 ACTIVE | HandoverUnitAssignment | Filtered unique ACTIVE ReservationId; no early occupancy. |
| StorageUnit | 1:N historical, 0..1 ACTIVE | HandoverUnitAssignment | Filtered unique ACTIVE StorageUnitId; Manager selection only. |
| Employee | 1:N | HandoverUnitAssignment | `AssignedByEmployeeId`; must resolve from authorized Manager user. |
| Reservation | 1:0..1 | RESERVATION Visit | Via `Visit.EntityId` when type RESERVATION. |
| Customer | 1:N | Contract | Direct FK. |
| Facility | 1:N | Contract | Direct FK. |
| StorageUnit | 1:N historical | Contract | Maximum one ACTIVE Contract per unit. |
| Policy | 1:N | Contract | Captured Contract policy version. |
| Customer | 1:N | Discount | Confirmed V1 direction; `Discount.CustomerId`. |
| Discount | 1:N | Contract | Each Contract has max 1 Discount; `Contract.DiscountId` nullable. |
| Contract | 1:N | ContractExtension | Append-only renewal history. |
| Contract | 1:N | ACCESS/RETURN Visit | Via polymorphic `Visit.EntityId`. |
| Employee | 1:N | Visit | Actual actor; nullable before handling. |
| Reservation | 1:N | DEPOSIT Invoice | Normally one logical Deposit invoice; via `Invoice.EntityId`. |
| Contract | 1:N | RENTAL_FEE Invoice | Via `Invoice.EntityId`. |
| Discount | 1:N | Invoice | Snapshot reference for rental invoices. |
| Invoice | 1:N | Payment | Multiple attempts are allowed. |
| Invoice | 1:0..1 | LateFee | One current late-fee record per overdue rental invoice in Release 1. |
| Contract | 1:N | SupportTicket | Direct FK. |
| Customer | 1:N | SupportTicket | Direct FK. |
| Employee | 1:N | SupportTicket | Assigned Staff; nullable until assigned. |
| Contract | 1:N historical | Inspection | Each Inspection belongs to one Contract; historical return/recovery inspection records may accumulate. |
| StorageUnit | 1:N | Inspection | Historical inspections. |
| RETURN Visit | 1:0..1 | Inspection | Visit optional for external recovery. |
| Employee | 1:N | Inspection | Claimed/performed inspections. |
| Inspection | 1:N | DamageRecord | Damage findings. |
| DamageType | 1:N | DamageRecord | Damage classification. |
| Inspection | 1:N | InspectionEvidence | Binary evidence. |
| Inspection | 1:N | ExtraFee | Extra operational fees. |
| ExtraFeeType | 1:N | ExtraFee | Fee master -> actual fee. |
| Contract | 1:0..1 | DepositSettlement | One final settlement result. |
| UserAccount | 1:N | LoginHistory | Authentication logs. |
| UserAccount | 1:N | AuditLog | Nullable actor for system events. |
| UserAccount | 1:N | NotificationLog | Recipient notifications. |

### Polymorphic reference rules

`Visit.EntityId`:

- `VisitType = RESERVATION` -> must exist in `Reservation.ReservationId`.
- `VisitType IN (ACCESS, RETURN)` -> must exist in `Contract.ContractId`.

`Invoice.EntityId`:

- `InvoiceType = DEPOSIT` -> must exist in `Reservation.ReservationId`.
- `InvoiceType = RENTAL_FEE` -> must exist in `Contract.ContractId`.

These rules cannot be expressed as a single ordinary FK and are validated by stored procedures/triggers.

---

## 6. Lifecycle Matrix

| Entity | Valid lifecycle |
|---|---|
| UserAccount | `ACTIVE <-> INACTIVE` subject to lifecycle safeguards. |
| Facility | `ACTIVE <-> INACTIVE`; inactive blocks new Reservation only. |
| Reservation | `PENDING_DEPOSIT -> CONFIRMED -> COMPLETED`; `PENDING_DEPOSIT/CONFIRMED -> CANCELLED`. |
| Visit | `SCHEDULED -> CHECKED_IN -> CHECKED_OUT`; `SCHEDULED -> CANCELLED`. |
| Payment | `PENDING -> SUCCESS` or `PENDING -> FAILED`; retry normally creates/reuses payment attempt according to gateway integration. |
| Invoice | `UNPAID -> PAID`; rental invoice may `UNPAID -> OVERDUE -> PAID`; eligible cancellation by business flow only. |
| Contract | `ACTIVE -> COMPLETED` or `ACTIVE -> TERMINATED`. |
| StorageUnit | `AVAILABLE -> IN_USE`, `AVAILABLE -> MAINTENANCE`, `IN_USE -> INSPECTION`, `INSPECTION -> AVAILABLE/MAINTENANCE`, `MAINTENANCE -> AVAILABLE`. |
| Inspection | `PENDING -> IN_PROGRESS -> COMPLETED`. |
| SupportTicket | `OPEN -> IN_PROGRESS -> COMPLETED`; `OPEN/IN_PROGRESS -> CANCELLED`. |
| Policy | current row `ACTIVE -> INACTIVE`; new version inserted as `ACTIVE`. |
| NotificationLog | `PENDING -> SENT`; failure remains `PENDING`. |
| HandoverUnitAssignment | `ACTIVE -> REPLACED/RELEASED/CONSUMED` only; terminal history immutable, no DELETE. |
| ContractExtension | Append-only; no lifecycle status. |
| LoginHistory | Append-only. |
| AuditLog | Append-only. |

### Return lifecycle

`Contract ACTIVE -> RETURN Visit -> ActualReturnDate -> StorageUnit INSPECTION -> Inspection -> fees/damage -> DepositSettlement -> StorageUnit AVAILABLE/MAINTENANCE -> Contract COMPLETED or TERMINATED`.

Normal versus Early Return is determined by `ActualReturnDate`, not scheduled `VisitDate`.

### Capacity release note

On Actual Return/Recovery the Contract stops holding future contractual capacity under approved rules; the StorageUnit enters `INSPECTION` and does **not** become AVAILABLE at Complete Inspection. Only **Finalize Return** may release it to `AVAILABLE` or `MAINTENANCE` after settlement and pending-damage guards. Capacity planning and physical availability must be kept distinct; this paragraph supersedes conflicting legacy `CompleteInspection` wording elsewhere in the historical dictionary.

---

## 7. Stored Procedure Specification

Stored procedures own multi-table business transactions and state transitions. Trigger logic should not orchestrate complete business workflows.

Physical backend: PostgreSQL with quoted `public."usp_*"` routines (functions/procedures as deployed), invoked through EF Core/Npgsql repository boundary. Retain historical `usp_` business operation names; no T-SQL syntax is normative for V11.

| Stored Procedure | Main responsibility | Main entities | Policy fields / notes | Atomic transaction |
|---|---|---|---|---:|
| `usp_CreateReservation` | Validate authenticated Customer saved valid 12-digit CCCD (OD-09/10), active Facility, month range, capacity; snapshot price, Deposit and active Policy; create Reservation + Deposit Invoice. | Customer, Facility, UnitType, Reservation, Policy, Invoice | Missing CCCD: reject without Reservation/hold/Invoice; existing DepositTimeoutHours still defines deadline. | Yes |
| `usp_ConfirmReservation` | Validate Deposit Invoice/Payment paid; validate chosen handover date; create RESERVATION Visit; set Reservation CONFIRMED. | Reservation, Invoice, Payment, Visit, Policy | `ReservationVisitStartDay`, `ReservationVisitEndDay`. | Yes |
| `usp_CancelReservation` | Validate cancellation eligibility; cancel Reservation and SCHEDULED reservation Visit; release hold. | Reservation, Visit | Uses captured Reservation Policy if cancellation rules become configurable. | Yes |
| `usp_CheckInVisit` | Validate Visit SCHEDULED, Staff role/facility and date; set actual actor and CHECKED_IN. | Visit, Employee, Facility | No hard-coded business dates. | Yes |
| `usp_CancelVisit` | Permit only SCHEDULED -> CANCELLED and enforce type-specific restrictions. | Visit |  | Yes |
| `usp_CompleteHandover` | Require checked-in Visit and Manager active matching Unit assignment; Staff confirms offline full first-month receipt; create Contract, consume assignment, set Unit IN_USE/Reservation COMPLETED/Visit CHECKED_OUT. **No first-month Invoice or Payment.** | Reservation, Visit, StorageUnit, HandoverUnitAssignment, Contract, Policy, Discount | Contract.PolicyId inherits Reservation.PolicyId; first-month amount is Reservation.LockedRentalPrice without Discount. Pg013 contract trigger consumes active assignment in same transaction; no assignment/mismatch rolls back. | **Yes, mandatory** |
| `usp_AssignHandoverUnit` | Atomic Manager assignment/reassignment with same-Facility/UnitType/AVAILABLE/check-in guards and partial uniqueness; idempotent same-unit selection. | Reservation, Visit, StorageUnit, Employee, HandoverUnitAssignment | Manager only; closes previous ACTIVE row to REPLACED; no StorageUnit status mutation. | **Yes** |
| `usp_ReleaseHandoverUnit` | Close Manager active selection as RELEASED without deleting history. | Reservation, Employee, HandoverUnitAssignment | Manager same Facility only; no Contract already completed. | **Yes** |
| `usp_CreateAccessVisit` | Create ACCESS Visit only for ACTIVE Contract; reject if active RETURN Visit exists. | Contract, Visit |  | Yes |
| `usp_CreateReturnVisit` | Create RETURN Visit for ACTIVE Contract and block contradictory Access/Renewal actions while pending. | Contract, Visit |  | Yes |
| `usp_ConfirmActualReturn` | Set `ActualReturnDate`, classify Normal/Early Return, move StorageUnit to INSPECTION, create Inspection PENDING; apply early-return current-month rule. | Contract, Visit, StorageUnit, Inspection, Invoice, LateFee, Policy | `EarlyReturnWaiveFeeUntilDay`. | **Yes, mandatory** |
| `usp_ClaimInspection` | Atomically claim PENDING Inspection and assign Employee; prevent double claim. | Inspection, Employee |  | **Yes, mandatory** |
| `usp_RecordDamage` | Validate active inspection and active DamageType; Facility Staff adds DamageRecord as `PENDING` using explicit or default amount. | Inspection, DamageType, DamageRecord | Production DamageType is fixed seed data. | Yes |
| `usp_DecideDamage` | Validate same-Facility Facility Manager and atomically decide `PENDING -> APPROVED/REJECTED`; audit terminal decision. | DamageRecord, Inspection, Contract, Employee, Facility, AuditLog | Only PENDING is decidable; concurrent decisions permit one winner. | **Yes** |
| `usp_RecordExtraFee` | Validate inspection and active ExtraFeeType; snapshot actual amount. | Inspection, ExtraFeeType, ExtraFee |  | Yes |
| `usp_AddInspectionEvidence` | Store evidence binary for Inspection. | Inspection, InspectionEvidence | Validate size/type at API and DB as practical. | Yes |
| `usp_CompleteInspection` | Validate claimed inspection; mark COMPLETED but **keep StorageUnit in INSPECTION** pending authoritative Finalize Return. | Inspection, StorageUnit | Unit release to AVAILABLE/MAINTENANCE belongs to Finalize Return. | Yes |
| `usp_FinalizeReturn` | Reject finalization while any related DamageRecord is PENDING; calculate applicable LateFee + ExtraFee + APPROVED Damage, create/finalize DepositSettlement, set Contract COMPLETED or TERMINATED, **release StorageUnit to AVAILABLE/MAINTENANCE**. | Contract, Invoice, LateFee, ExtraFee, DamageRecord, DepositSettlement | REJECTED Damage contributes zero; Early Return => RefundAmount = 0. | **Yes, mandatory** |
| `usp_RenewContract` | Validate ACTIVE Contract, no pending RETURN Visit, contiguous period and capacity; snapshot current UnitType price; append ContractExtension; update EndMonth. | Contract, ContractExtension, StorageUnit, UnitType, Reservation | Contract continues using captured Policy for contract rules. | **Yes, mandatory** |
| `usp_CreateMonthlyInvoice` | Generate subsequent Rental Fee invoice for a BillingMonth using correct price source and captured Contract Discount. | Contract, Reservation, ContractExtension, Invoice, Discount | `MonthlyPaymentDueDay`; initial-period price from Reservation, extension-period price from ContractExtension. First month is collected offline at handover without Invoice/Payment; later months may use captured Contract Discount. | Yes |
| `usp_ApplyPaymentResult` | Process verified payOS webhook idempotently; update Payment/Invoice paid state using verified UTC success time. | Payment, Invoice | Staff cannot manually mark PAID. | **Yes** |
| `usp_CalculateLateFee` | Calculate/update LateFee for overdue Rental Invoice. | Invoice, Contract, Policy, LateFee | Formula uses `Policy.LateFeeDivisorDays`. | Yes |
| `usp_MarkOverdueInvoices` | Mark unpaid rental invoices overdue when their captured Contract Policy threshold is reached. | Invoice, Contract, Policy | `OverdueStartDay`. | Yes |
| `usp_CreatePolicyVersion` | Inactivate prior active Policy and insert new active version with all configuration values. | Policy | Never update old business parameters in place. | **Yes** |
| `usp_AssignSupportTicket` | Validate Facility Manager/Staff facility scope; assign Staff and move OPEN -> IN_PROGRESS where applicable. | SupportTicket, Employee, Contract |  | Yes |
| `usp_CompleteSupportTicket` | Record ResultNote, CompletedAt and set COMPLETED. | SupportTicket | Does not mutate Contract/Payment/StorageUnit lifecycle. | Yes |
| `usp_CancelSupportTicket` | Permit OPEN/IN_PROGRESS -> CANCELLED before completion. | SupportTicket |  | Yes |
| `usp_SetUserAccountStatus` | Activate/deactivate account; reject Customer deactivation when active lifecycle exists. | UserAccount, Customer, Reservation, Contract |  | Yes |
| `usp_QueueNotification` | Insert NotificationLog PENDING. | NotificationLog |  | Yes |
| `usp_RetryPendingNotification` | **Select** oldest eligible `PENDING` item using `FOR UPDATE SKIP LOCKED`, limit 1; does **not** send or mark SENT. | NotificationLog, UserAccount lookup by Repository | `INotificationSender.SendAsync` must deliver; after provider acknowledgement, `usp_MarkNotificationSent` sets SENT/SentAt. On failure record stays PENDING. | SQL queue primitive; worker required |

### Stored Procedure policy rule

A procedure operating on an existing lifecycle must read the Policy captured by that lifecycle:

- Reservation procedure -> `Reservation.PolicyId`.
- Contract procedure -> `Contract.PolicyId`.

Do **not** use the currently ACTIVE Policy for an old Reservation/Contract unless the business explicitly asks for retrospective rule changes.

---

## 8. Trigger Specification

Triggers are intended for integrity guards, append-only enforcement and audit support. They should not implement full multi-table business workflows.

| Trigger | Event | Purpose |
|---|---|---|
| `trg_Visit_ValidateEntity` | INSERT/UPDATE Visit | Validate polymorphic `EntityId` target according to `VisitType`; enforce ActualReturnDate only for RETURN. |
| `trg_Invoice_ValidateEntity` | INSERT/UPDATE Invoice | Validate polymorphic `EntityId`; DEPOSIT -> Reservation, RENTAL_FEE -> Contract; reject Discount on Deposit. |
| `trg_Reservation_StatusTransition` | UPDATE Reservation | Reject invalid lifecycle transitions. |
| `trg_Visit_StatusTransition` | UPDATE Visit | Enforce `SCHEDULED -> CHECKED_IN -> CHECKED_OUT` or `SCHEDULED -> CANCELLED`; block cancellation after CHECKED_IN. |
| `trg_Contract_StatusTransition` | UPDATE Contract | Permit only valid `ACTIVE -> COMPLETED/TERMINATED` terminal transitions; block reactivation. |
| `trg_Inspection_StatusTransition` | UPDATE Inspection | Enforce `PENDING -> IN_PROGRESS -> COMPLETED`; block claim overwrite. |
| `trg_DamageRecord_StatusTransition` | UPDATE DamageRecord | Enforce only `PENDING -> APPROVED/REJECTED`; terminal states cannot be reopened. |
| `trg_StorageUnit_StatusTransition` | UPDATE StorageUnit | Enforce valid operational status transitions. |
| `trg_SupportTicket_StatusTransition` | UPDATE SupportTicket | Enforce ticket lifecycle. |
| `trg_Policy_ProtectHistoricalVersion` | UPDATE/DELETE Policy | Prevent destructive updates/deletes of historical Policy versions; allow controlled status transition if needed by `usp_CreatePolicyVersion`. |
| `trg_ContractExtension_AppendOnly` | UPDATE/DELETE ContractExtension | Reject mutation/deletion. |
| `trg_LoginHistory_AppendOnly` | UPDATE/DELETE LoginHistory | Reject mutation/deletion. |
| `trg_AuditLog_AppendOnly` | UPDATE/DELETE AuditLog | Reject mutation/deletion. |
| `trg_Discount_ValidateContractOwnership` | INSERT/UPDATE Contract | When DiscountId exists, validate `Discount.CustomerId = Contract.CustomerId`; validate active/effective state at initial assignment. |
| `trg_Discount_ProtectReferencedSemantics` | UPDATE Discount | If any Contract references the Discount, block changes to `CustomerId`, `Percentage`, `EffectiveFrom`, `EffectiveTo`; allow Status changes without invalidating existing Contract usage. |
| `trg_Employee_ValidateFacilityByRole` | INSERT/UPDATE Employee/UserAccount role | Facility required for Facility Staff/Manager and optional/null for global roles. Prefer procedure + constraint where possible. |
| `trg_Audit_BusinessChanges` | INSERT/UPDATE/DELETE selected business tables | Write essential changes to AuditLog. Avoid auditing high-volume binary evidence data. |
| `trg_HandoverUnitAssignment_LifecycleGuard` | UPDATE/DELETE HandoverUnitAssignment | Pg013: forbid DELETE/history mutation, permit only ACTIVE -> REPLACED/RELEASED/CONSUMED. |
| `frms_require_handover_assignment_on_contract` | BEFORE INSERT Contract | Pg013: require matching active Manager assignment and consume in same handover transaction. |
| `trg_Audit_StorageUnit_StatusHistory` | INSERT or UPDATE OF Status on StorageUnit | Pg010 AuditLog status timeline capture; no historical backfill. |

### Trigger design rule

Do not create triggers such as `Payment PAID -> automatically create Contract`. Exception: Pg013 BEFORE INSERT Contract guard may verify and atomically consume an existing approved handover assignment; it must not independently orchestrate handover or create Contract. Complete Handover and Return finalization touch many tables and must remain explicit stored-procedure transactions for observability, validation and rollback.

---

## 9. Scheduled / Time-based Procedures — current main reconciliation

Time passing does not fire a database trigger. As of read-only `main` `0219f30734b0`, **six business tasks are implemented as four .NET hosted services**. BackgroundJob -> Business Service -> Repository -> PostgreSQL routine; no SQL Server Agent. See operational cadence, configuration, constraints and acceptance gaps in §27.

| Scheduled task | Current SQL function(s) | Data effect | Runtime registration |
|---|---|---|---|
| Expire unpaid Deposit reservations | `usp_ExpirePendingReservations` | Cancel eligible unpaid Deposit invoice/Reservation after deadline | `ExpirePendingReservationsJob` (existing, startup + 10m) |
| Process handover no-show | `usp_ProcessReservationNoShow` | Process expired Reservation/Visit handover windows | `ProcessReservationNoShowJob` (default OFF, startup + 24h when enabled) |
| Generate recurring rental invoices | `usp_GenerateMonthlyInvoices` → `usp_CreateMonthlyInvoice` | Insert eligible current-month `RENTAL_FEE/UNPAID`, excluding first month | `BillingMaintenanceJob` stage 1 (default ON, startup + 24h) |
| Mark rental invoices overdue | `usp_MarkOverdueInvoices` | `Invoice.UNPAID → OVERDUE` after captured Policy threshold | `BillingMaintenanceJob` stage 2 |
| Refresh open LateFees | `usp_RecalculateOpenLateFees` → `usp_CalculateLateFee` | Insert/update one LateFee per overdue rental Invoice | `BillingMaintenanceJob` stage 3 |
| Retry notifications | `usp_RetryPendingNotification`, sender, `usp_MarkNotificationSent` | Select oldest PENDING, send, then mark SENT on acknowledgment only | `RetryNotificationsJob` (default OFF, startup + 5m; sender currently unconfigured) |

---

## 10. Recommended Database Constraints and Indexes

Use native constraints/indexes before triggers whenever possible.

### Unique / filtered uniqueness

- `UserAccount.Email` UNIQUE.
- `UserAccount.PhoneNumber` UNIQUE.
- `Customer.UserAccountId` UNIQUE.
- `HandoverUnitAssignment.ReservationId` and `.StorageUnitId` individually partial UNIQUE WHERE `Status='ACTIVE'`.
- `HandoverUnitAssignment.Status` CHECK for four states and `ClosedAt` conditional-null CHECK.
- `Payment.IdempotencyKey` globally UNIQUE and NOT NULL (PostgreSQL `uuid`).
- `Employee.UserAccountId` UNIQUE.
- `Customer.CCCD` UNIQUE where not null.
- `Contract.ReservationId` UNIQUE.
- `DepositSettlement.ContractId` UNIQUE.
- `LateFee.InvoiceId` UNIQUE.
- `Payment.TransactionCode` UNIQUE where not null.
- PROPOSED/NOT DEPLOYED: unique `(FacilityId, UnitCode)` for StorageUnit; Pg001 and current EF model do not define this index. Requires owner/DB review before implementation.
- ALREADY DEPLOYMENT-DEFINED: filtered unique Invoice `(EntityId, BillingMonth) WHERE InvoiceType = 'RENTAL_FEE'`; `EntityId` equals ContractId for these rows. Do not add a redundant third-column index.
- ALREADY DEFINED: filtered unique Contract `StorageUnitId WHERE Status='ACTIVE'` in current EF mapping/Pg001.
- ALREADY DEFINED: filtered unique `Policy.Status WHERE Status='ACTIVE'` in current EF mapping/Pg001.

### Check constraints

- Percentage range `0 <= Discount.Percentage <= 100`.
- Non-negative monetary values.
- `EndMonth >= StartMonth`.
- `ContractExtension.NewEndMonth > OldEndMonth`.
- Policy day fields within valid day ranges and `ReservationVisitStartDay <= ReservationVisitEndDay`.
- `Invoice.BillingMonth IS NULL` for DEPOSIT and NOT NULL for RENTAL_FEE (can be trigger/check where DB supports expression logic).
- `NotificationLog.SentAt IS NULL` while PENDING; set on SENT.
- `SupportTicket.Category` must be one of the seven fixed Release 1 values including `OTHER`.

### Concurrency-sensitive validations

The following must execute inside transactions with suitable locking/isolation:

- Reservation capacity check + insert/hold.
- Complete Handover and StorageUnit assignment.
- Renewal capacity check + ContractExtension + EndMonth update.
- Inspection claim.
- payOS signed webhook idempotency.

---

## 11. Entities Intentionally Not Created

| Concept | V1 replacement / reason |
|---|---|
| `Booking` | `Reservation`. |
| `FacilityAssignment` | Current assignment stored as `Employee.FacilityId`; no transfer-history requirement. |
| `MonthlyRentalFee` | `Invoice(InvoiceType=RENTAL_FEE)`. |
| `Renewal` | `ContractExtension` append-only history + updated Contract.EndMonth. |
| `ReturnProcess` / `ReturnRequest` | `RETURN Visit + Inspection + Contract/StorageUnit states`. |
| `DiscountRedemption` | Contract selects one Discount; Invoice snapshots DiscountId/DiscountAmount. |
| `Refund` | `DepositSettlement.RefundAmount`; actual transfer outside system. |
| `DamageFee` | `DamageRecord.DamageAmount`. |
| `Maintenance` | `StorageUnit.Status = MAINTENANCE`; no maintenance workflow entity in core. |
| `DailyTask` | Derived query from Visits + Inspections + SupportTickets. |
| `Report` | Derived/query/export capability, not persisted domain entity; Release 1 export format is UTF-8 CSV. |
| `PriceHistory` | Reservation and ContractExtension/Invoice snapshots preserve transaction pricing. |
| `Appointment` | Visit already models scheduled facility visits. |
| `RenewalFee` | Renewed months continue using Rental Fee invoices. |

---

## 12. V11 Business Implementation Clarifications

### 12.1 First-month rent is offline, not pre-handover Payment

Staff records acknowledgement of full `Reservation.LockedRentalPrice` at authorized Complete Handover. No first-month `RENTAL_FEE` Invoice, no first-month Payment, no null `Payment.InvoiceId` and no new receipt entity. Contract starts in its first month with revenue recorded once through the Contract/Reservation snapshot. PAY-002 is retired. Online payOS billing applies to Deposit or later Rental Fee invoices only.

### 12.2 Contract Policy / Discount snapshot

`Contract.PolicyId = Reservation.PolicyId` at handover. A changed active Policy after Reservation creation must not affect the Contract. Optional Contract Discount cannot reduce offline first-month rent; only invoices later than `Contract.StartMonth` may use the locked eligible Discount.

### 12.3 Financial evidence and returns

Verified payOS webhook and unique reference govern financial state. `Invoice.PaidAt` is first successful provider timestamp only; duplicate success does not overwrite it. `DamageRecord` Manager approval is independent of LateFee/ExtraFee. Pending Damage blocks Finalize Return. `CompleteInspection` leaves unit in INSPECTION; Finalize Return releases the unit. External refund transfers remain outside system.

### 12.4 Identity / optional address

Customer PhoneNumber and account Email remain login identifiers, not editable through AUTH-005. Customer.Address remains nullable/optional with no new disclosure or required use. CCCD retains nullable varchar(20)/unique storage and optional AUTH-001 input; OD-10 requires supplied nonblank values to match exactly 12 ASCII digits after trim; under SRS OD-09 / BR-RES-09 the saved value itself must match OD-10 before a new RES-001 creates a Reservation/hold/Deposit Invoice. This is a conditional operation precondition, not a global NOT NULL schema change. Missing or invalid saved CCCD maps to the existing API HTTP 400 VALIDATION_ERROR with errors.cccd and traceId; allocate no new API error code or guessed PostgreSQL SQLSTATE.

AUTH-004 privately returns profile.cccd as string or explicit null to its owning Customer. Source at 2945788 contains CurrentCustomerProfileResponse and AuthenticationMapping with that projection; running Swagger/serialization/integration acceptance remains unverified here. A missing/malformed response property is unavailable, not null or a verified identity. No Employee/public/directory role inherits CCCD access. AUTH-005 may save CCCD alone, preserving omitted fields; explicit null clearing retains existing semantics, so a subsequent new RES-001 fails its precondition until CCCD is supplied again. A completed profile save is not a reservation command.

The in-flow completion/return contract is SRS §14.15 and API AUTH-004/005 + RES-001. Retain only allowlisted non-sensitive operation context through authentication/CCCD completion; never persist CCCD, raw profile, trusted price/capacity or credentials in a continuation record/URL. No server-side draft/session entity or new endpoint is approved. Ordinary menu/Settings navigation does not acquire forced return behavior. Administrator-created employee credential email workflow and optional Address decisions remain unchanged.

**Deployment/evidence boundary:** This revision changes the approved routine/service precondition, not the physical entity schema. Implementers must align the existing authoritative creation transaction and error mapping, with clean deploy/redeploy/rollback reasoning and real PostgreSQL/concurrency tests if a routine migration is needed; this document does not authorize executing a migration. Preserve old Reservations/Contracts/Invoice/Payment history; no synthetic CCCD backfill. Authentication/PATCH/RES-001, Swagger and FE owners must coordinate rollout before declaring OD-09 working.


**OD-10 persistence and BE implementation handoff (2026-10-26):** The exact format is a whole-string match of 12 ASCII digits; store strings and preserve leading zeroes. Retain the current column/index and legacy stored strings; no schema/routine migration or live-data update is executed in this task. New AUTH-001 / supplied AUTH-005 input must be normalized and checked by Business services, with omitted PATCH CCCD left untouched and explicit null/blank allowed to clear. For new RES-001, re-check the saved value and authenticated owner atomically with capacity and Invoice creation, including concurrent profile clear/replace. A legacy malformed string may be shown to its owner for correction but cannot pass new RES-001. Never pad/truncate/backfill IDs or rewrite old rentals.

Return existing HTTP 400 VALIDATION_ERROR + errors.cccd/traceId for missing/invalid prerequisite or format/unique failure through the existing API mapping. Do not invent a PostgreSQL SQLSTATE in this documentation task. A future defense-in-depth CHECK requires a reviewed forward migration and an explicit plan for legacy rows; keeping varchar(20) alone does not enforce the new input format. Backend validators/routine deployment, generated Swagger and real PostgreSQL/concurrency tests remain pending.

Traceability: SRS OD-10 / BR-ACC-04 / BR-RES-09 -> AUTH-001/005 + RES-001 -> Business validators + existing repository/creation routine -> API-CCCD-004 / DBT-CCCD-003 / REG-CONT-002 -> V11-G15. Required cases include nullable/omit/clear, valid leading-zero strings, 11/13 digits, non-ASCII digits, letters/punctuation/internal whitespace, uniqueness, preserved legacy rows and zero creation writes on failure.

### 12.5 Manager unit assignment

Owner-approved Pg013 `HandoverUnitAssignment` is a distinct persisted entity, **not** the historical `FacilityAssignment` concept. Manager assigns/releases after checked-in reservation visit; Staff reads; matching active selection is consumed as part of atomic Contract creation. See §4.28, §6–8, SRS OD-02 and API §13A.1.

---

## 13. PostgreSQL Physical Data-Type Standard — V11

The active `main` runtime uses PostgreSQL/Npgsql and an independent EF Core migrations project. The legacy SQL Server convention (`uniqueidentifier`, `datetime2`, `varbinary(max)`, `nvarchar(max)`, `GETUTCDATE()`/`SYSUTCDATETIME()`) is **not** the V11 physical schema. Historical SQL Server artifacts remain archived and must not be used as migration commands.

| Logical type | PostgreSQL target | Rule |
|---|---|---|
| UUID | `uuid` | PK/FK; payment `IdempotencyKey uuid NOT NULL UNIQUE` |
| Currency | `numeric(18,2)` or migration-authoritative EF numeric mapping | No floating point financial arithmetic |
| Percentage | `numeric(5,2)` | CHECK range and locking rules |
| Status/enum | bounded `varchar(n)` + CHECK | Status transitions enforced by service + routine/trigger |
| Text | `varchar(n)` or `text` | Unicode input; bounded `Address` 1000, media URLs 2048 |
| E-mail / telephone / CCCD | bounded `varchar` | unique constraints; nullable CCCD unique when present |
| Month / calendar date | `date` | month stored as first day of month; wire month `YYYY-MM` |
| UTC timestamp | `timestamp(3) with time zone` (`timestamptz(3)`) | Persist UTC; display/local calendar Asia/Ho_Chi_Minh |
| JSON payload | `text` for current `AuditLog.OldValue/NewValue` with JSON OBJECT/ARRAY CHECK when non-null | `jsonb` is not the deployed type for these two columns; schema migration would be required for a type change |
| Binary evidence | `bytea` | `InspectionEvidence.FileData`; never send content in audit logs |
| Payment order code | `bigint` sequence-backed | Atomic, non-cycling, prevent collision with existing live payOS merchant order codes |

**Physical mapping verification (`main` `0219f307`, EF model + Pg001/Pg012/Pg013):** The 28 entity tables above enumerate 202 persisted properties. Canonical CLR `DateTime`/`DateTime?` generally maps to `timestamptz(3)`; **exception:** `Visit.ActualReturnDate` is a nullable `date` column despite the CLR `DateTime?` type. CLR `Customer.Cccd` maps to quoted DB column `"CCCD"`. `InspectionEvidence.FileData` is `bytea`. A recommended index is not evidence it already exists. Types shown above describe the current code-defined schema, not a live database catalogue verification.

**Default timestamp example (illustration; exact migrations authoritative):**

```sql
CREATE TABLE public."ExampleAudit" (
    "ExampleAuditId" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "CreatedAt" timestamptz(3) NOT NULL DEFAULT clock_timestamp()
);
```

`Frms.Migrations.PostgreSql` is the only migration discovery/tooling owner. `Frms.DataAccess` shares canonical EF model configuration through linked source; API startup must NOT apply migrations or mutate production schema automatically.

### 13.1 Transaction timestamps and payment time

Database audit `clock_timestamp()` is real creation time. payOS `transactionDateTime` is provider UTC+7 and must be verified then normalized to UTC. `Invoice.PaidAt` must not be fabricated for legacy rows lacking provider timestamp or overwritten on duplicate success. Never reset order-code sequence to 1000 on an existing merchant without an approved collision strategy.

### 13.2 Identifier and physical naming

PostgreSQL entity PK/FK columns are `uuid`; quote mixed-case deployed identifiers such as `public."Contract"` and `public."StorageUnit"` if using raw SQL. PostgreSQL business functions may retain quoted names `public."usp_*"`; do not assume they are SQL Server procedures. Stable API IDs use the companion API specification rather than physical database names.

---

## 14. Enum / Status Dictionary

All enum-like values are persisted as `VARCHAR` plus `CHECK` constraints unless later normalized into lookup tables.

| Domain | Allowed values |
|---|---|
| `UserRole.RoleName` | `CUSTOMER`, `FACILITY_STAFF`, `FACILITY_MANAGER`, `BUSINESS_OPERATIONS_MANAGER`, `SYSTEM_ADMINISTRATOR` |
| `UserAccount.Status` | `ACTIVE`, `INACTIVE` |
| `Facility.Status` | `ACTIVE`, `INACTIVE` |
| `UnitType.Mode` | `PUBLIC`, `PRIVATE` |
| `StorageUnit.Status` | `AVAILABLE`, `IN_USE`, `INSPECTION`, `MAINTENANCE` |
| `HandoverUnitAssignment.Status` | `ACTIVE`, `REPLACED`, `CONSUMED`, `RELEASED` (only ACTIVE transitions terminal) |
| `Reservation.Status` | `PENDING_DEPOSIT`, `CONFIRMED`, `COMPLETED`, `CANCELLED` |
| `Visit.VisitType` | `RESERVATION`, `ACCESS`, `RETURN` |
| `Visit.Status` | `SCHEDULED`, `CHECKED_IN`, `CHECKED_OUT`, `CANCELLED` |
| `Contract.Status` | `ACTIVE`, `COMPLETED`, `TERMINATED` |
| `Invoice.InvoiceType` | `DEPOSIT`, `RENTAL_FEE` |
| `Invoice.Status` | `UNPAID`, `PAID`, `OVERDUE`, `CANCELLED` |
| `Payment.PaymentMethod` | `PAYOS` for new Payment; `MOMO`/`VNPAY` historical evidence only |
| `Payment.Status` | `PENDING`, `SUCCESS`, `FAILED` |
| `Discount.Status` | `ACTIVE`, `INACTIVE` |
| `Policy.Status` | `ACTIVE`, `INACTIVE` |
| `SupportTicket.Status` | `OPEN`, `IN_PROGRESS`, `COMPLETED`, `CANCELLED` |
| `SupportTicket.Category` | `UNIT_ISSUE`, `LOCK_KEY_ISSUE`, `ACCESS_CARD_CODE_ISSUE`, `PAYMENT_ISSUE`, `STORED_ITEM_ISSUE`, `DAMAGE_ISSUE`, `OTHER` |
| `Inspection.Status` | `PENDING`, `IN_PROGRESS`, `COMPLETED` |
| `DamageRecord.Status` | `PENDING`, `APPROVED`, `REJECTED` |
| `DamageType.Status` | `ACTIVE`, `INACTIVE` |
| `InspectionEvidence.EvidenceType` | `IMAGE`, `VIDEO`, `DOCUMENT` |
| `ExtraFeeType.Name` | `KEY_REPLACEMENT`, `ACCESS_CARD_REPLACEMENT`, `LOCK_REPLACEMENT`, `CLEANING_FEE`, `OTHER` |
| `ExtraFeeType.Status` | `ACTIVE`, `INACTIVE` |
| `DepositSettlement.Status` | `PENDING`, `FINALIZED` |
| `LoginHistory.Status` | `SUCCESS`, `FAILED` |
| `NotificationLog.Status` | `PENDING`, `SENT` |

---

## 15. Default Value Dictionary

Defaults are applied only where the initial state is deterministic. Business results must not be defaulted prematurely.

| Table.Column | Recommended default | Notes |
|---|---|---|
| `UserAccount.Status` | `ACTIVE` | Subject to account creation flow. |
| `Facility.Status` | `INACTIVE` | Newly created Facility requires explicit activation before accepting new Reservation. |
| `StorageUnit.Status` | `AVAILABLE` | Locked SRS V9 initial status; preventive maintenance may immediately transition to MAINTENANCE. |
| `HandoverUnitAssignment.Status` | `ACTIVE` | New Manager-selected assignment; terminal history is immutable. |
| `Reservation.Status` | `PENDING_DEPOSIT` | Created before Deposit payment. |
| `Visit.Status` | `SCHEDULED` | All customer visits are scheduled first. |
| `Contract.Status` | `ACTIVE` | Contract row is created only on successful handover. |
| `Invoice.Status` | `UNPAID` | First-month rent has no Invoice; only eligible Deposit/later Rental Fee invoices use this lifecycle. |
| `Invoice.DiscountAmount` | `0` | Deposit always uses zero discount. |
| `Payment.Status` | `PENDING` | Until gateway callback/result. |
| `Discount.Status` | `ACTIVE` | If created for immediate use. |
| `Policy.Status` | `ACTIVE` | Only through `usp_CreatePolicyVersion`, which inactivates the prior active row. |
| `SupportTicket.Status` | `OPEN` | On creation. |
| `Inspection.Status` | `PENDING` | On actual return/recovery. |
| `DamageRecord.Status` | `PENDING` | Until Staff/Manager approval decision. |
| `DamageType.Status` | `ACTIVE` | On master creation unless explicitly disabled. |
| `ExtraFeeType.Status` | `ACTIVE` | On master creation unless explicitly disabled. |
| `DepositSettlement.Status` | `PENDING` | May move to `FINALIZED` in the same transaction if all required data is already available. |
| `NotificationLog.Status` | `PENDING` | Until successful delivery. |
| `CreatedAt` columns | `clock_timestamp()` | Recommended DB default. |

---

## 16. Nullability and Conditional-Column Rules

| Column | Nullable because | Validation rule |
|---|---|---|
| `Customer.CCCD` | Customer may not have supplied CCCD at registration or during ordinary profile use; no verification state is inferred. | Nullable varchar(20) physical storage; unique when present. OD-10 exact-12-digit format applies to supplied nonblank input and the saved value used by new RES-001; preserve legacy rows. |
| `Employee.FacilityId` | Global roles have no Facility. | Required for Facility Staff/Manager; null for Business Operations Manager/System Administrator. |
| `Visit.EmployeeId` | Visit may be scheduled before a Staff actor handles it. | Set when Staff starts handling/checks in. |
| `Visit.ActualReturnDate` | Only meaningful for confirmed physical RETURN. | Must be null for RESERVATION/ACCESS; set for confirmed RETURN. |
| `Contract.DiscountId` | Discount is optional. | If present, Discount must belong to Contract Customer and be valid at Contract creation. |
| `Invoice.BillingMonth` | Deposit is not monthly. | Null for DEPOSIT; required for RENTAL_FEE. |
| `Invoice.DiscountId` | Discount is optional. | Null for DEPOSIT; no first-month RENTAL_FEE invoice exists; later invoices may reference captured eligible Contract Discount. |
| `Payment.InvoiceId` | Every new Payment must be Invoice-backed; PAY-002 retired. | NOT NULL FK for new rows; preserve anomalous historical evidence for reviewed migration, no synthetic links. |
| `Payment.TransactionCode` | May not exist before gateway has created a transaction reference. | Unique when present. |
| `Payment.PaidAt` | Only successful payments have a paid time. | Required when `Status=SUCCESS`; null otherwise. |
| `Policy.EffectiveTo` | Current active Policy is open-ended. | Set when version is inactivated. |
| `SupportTicket.AssignedEmployeeId` | Ticket can be OPEN before assignment. | Must be present when processing as assigned work. |
| `SupportTicket.CompletedAt` | Only completed tickets have completion time. | Required when `Status=COMPLETED`. |
| `Inspection.VisitId` | External recovery may have no RETURN Visit. | If present, referenced Visit must be type RETURN and belong to same Contract. |
| `Inspection.EmployeeId` | Inspection may be PENDING before claim. | Required for IN_PROGRESS/COMPLETED. |
| `Inspection.CompletedAt` | Only completed inspection has completion time. | Required when `Status=COMPLETED`. |
| `DamageType.DefaultAmount` | Some damage types may require manual amount. | If present, non-negative. |
| `AuditLog.UserAccountId` | System/background process may generate audit event. | Null identifies system actor. |
| `NotificationLog.SentAt` | Pending notification has not been sent. | Null while PENDING; required when SENT. |
| `UserAccount.EmailVerifiedAt` | Release 1 has no email-verification workflow. | May remain NULL and is not a login precondition. |

---

## 17. Business Rule Matrix

The following rules are the minimum business rules that DB procedures/triggers must protect. `SP` means stored procedure; `TRG` means trigger/integrity guard; `CHK/IDX` means native constraint/index.

| Rule ID | Business rule | Primary entities | Enforcement |
|---|---|---|---|
| `BR-ACC-01` | Inactive account cannot log in or initiate new business actions. | UserAccount | Application + SP guards |
| `BR-ACC-02` | Customer cannot be deactivated while a CONFIRMED Reservation or ACTIVE Contract exists. | UserAccount, Customer, Reservation, Contract | `usp_SetUserAccountStatus` + TRG safeguard |
| `BR-EMP-01` | Facility Staff/Manager must have a Facility; global employee roles do not require one. | Employee, UserAccount, UserRole | SP + TRG |
| `BR-FAC-01` | INACTIVE Facility cannot accept new Reservation. | Facility, Reservation | `usp_CreateReservation` |
| `BR-FAC-02` | Newly created Facility starts INACTIVE and requires explicit activation. | Facility | default + create procedure/service |
| `BR-UNIT-01` | Newly created StorageUnit starts AVAILABLE; same-Facility Manager may move unused AVAILABLE unit to MAINTENANCE. | StorageUnit, Contract, Employee | default + status SP/TRG |
| `BR-UNIT-02` | UnitType reassignment is blocked while StorageUnit is IN_USE/INSPECTION or occupied by an ACTIVE Contract. | StorageUnit, Contract | SP/TRG |
| `BR-RES-01` | Reservation start month must be future and `EndMonth >= StartMonth`. | Reservation | SP + CHK |
| `BR-RES-02` | Reservation holds capacity by Facility + UnitType + month range, never a concrete StorageUnit. | Reservation, StorageUnit | `usp_CreateReservation` |
| `BR-RES-03` | Capacity check + Reservation creation must be atomic and prevent overbooking. | Reservation, Contract, StorageUnit | `usp_CreateReservation` + transaction locking |
| `BR-RES-04` | Deposit amount is snapshot of UnitType price at Reservation creation. | Reservation, UnitType | `usp_CreateReservation` |
| `BR-RES-09` | New RES-001 requires the authenticated Customer's saved CCCD matching OD-10; missing/invalid -> 400 VALIDATION_ERROR + errors.cccd, no Reservation/hold/Invoice. Registration remains optional; old rentals unchanged. | Customer, Reservation, Invoice | Business Service + authoritative atomic usp_CreateReservation boundary; API §13.14 error envelope |
| `BR-RES-05` | Deposit cannot use Discount. | Invoice, Discount | CHK/TRG |
| `BR-RES-06` | Reservation confirms only after Deposit payment succeeds. | Reservation, Invoice, Payment | `usp_ConfirmReservation` |
| `BR-RES-07` | RESERVATION Visit date must be inside captured Policy visit-day window. | Reservation, Visit, Policy | `usp_ConfirmReservation` |
| `BR-RES-08` | Reservation Facility/UnitType/StartMonth/EndMonth are immutable after creation. | Reservation | TRG or restrict UPDATE surface |
| `BR-VIS-01` | Only SCHEDULED Visit can be cancelled. | Visit | SP + TRG |
| `BR-VIS-02` | RESERVATION Visit references Reservation; ACCESS/RETURN reference Contract. | Visit | SP + `trg_Visit_ValidateEntity` |
| `BR-VIS-03` | ACCESS Visit requires ACTIVE Contract. | Contract, Visit | `usp_CreateAccessVisit` |
| `BR-VIS-04` | Pending RETURN Visit blocks new ACCESS Visit and Renewal. | Visit, Contract | `usp_CreateAccessVisit`, `usp_RenewContract` |
| `BR-HO-01` | Selected StorageUnit must be AVAILABLE and match Reservation Facility + UnitType. | Reservation, StorageUnit | `usp_CompleteHandover` |
| `BR-HO-02` | Staff must acknowledge full first-month offline rent before Complete Handover. | Reservation, Contract | `usp_CompleteHandover`; no Invoice/Payment |
| `BR-HO-03` | Complete Handover requires matching active Manager Unit assignment, atomically creates Contract, consumes assignment, marks Unit IN_USE, Reservation COMPLETED, Visit CHECKED_OUT; preserves captured Policy. | Reservation, Visit, StorageUnit, HandoverUnitAssignment, Contract, Policy | `usp_CompleteHandover` / Pg013 trigger transaction |
| `BR-HO-V11-01` | Only same-Facility Manager may assign/reassign/release matching AVAILABLE Unit after RESERVATION Visit CHECKED_IN; Staff GET only. | HandoverUnitAssignment, Reservation, Visit, StorageUnit | `usp_AssignHandoverUnit` / `usp_ReleaseHandoverUnit` |
| `BR-CON-01` | One Reservation creates at most one Contract. | Reservation, Contract | UNIQUE index |
| `BR-CON-02` | A StorageUnit has at most one ACTIVE Contract at a time. | Contract, StorageUnit | Filtered UNIQUE index + SP |
| `BR-DIS-01` | Customer owns many Discounts; each Discount belongs to exactly one Customer. | Customer, Discount | FK |
| `BR-DIS-02` | Each Contract uses zero or one Discount, and it must belong to the same Customer. | Contract, Discount | FK + TRG/SP |
| `BR-DIS-03` | Contract-selected Discount is fixed for that Contract lifecycle in Release 1. | Contract | TRG/restrict UPDATE |
| `BR-DIS-04` | Rental invoices snapshot DiscountId and DiscountAmount; later Discount edits do not rewrite issued invoices. | Contract, Discount, Invoice | `usp_CreateMonthlyInvoice` |
| `BR-DIS-05` | First-month offline rent has no Contract Discount/Invoice; only later eligible monthly invoices may use captured Contract Discount. | Reservation, Contract, Invoice, Discount | `usp_CompleteHandover` / `usp_CreateMonthlyInvoice` |
| `BR-DIS-06` | Referenced Discount `CustomerId`, `Percentage`, `EffectiveFrom`, `EffectiveTo` are immutable; INACTIVE blocks only new selection. | Discount, Contract | TRG/SP |
| `BR-BIL-01` | Exactly one logical Deposit Invoice per Reservation. | Reservation, Invoice | Filtered UNIQUE index |
| `BR-BIL-02` | At most one Rental Fee Invoice per Contract + BillingMonth. | Contract, Invoice | Filtered UNIQUE index |
| `BR-BIL-03` | Rental invoice initial-period BaseAmount comes from Reservation.LockedRentalPrice. | Reservation, Contract, Invoice | `usp_CreateMonthlyInvoice` |
| `BR-BIL-04` | Rental invoice extension-period BaseAmount comes from corresponding ContractExtension.AppliedMonthlyPrice. | ContractExtension, Invoice | `usp_CreateMonthlyInvoice` |
| `BR-BIL-05` | From Policy overdue threshold, unpaid Rental Invoice becomes OVERDUE. | Invoice, Contract, Policy | scheduled `usp_MarkOverdueInvoices` |
| `BR-PAY-01` | Payment callback is idempotent by gateway transaction/reference. | Payment, Invoice | `usp_ApplyPaymentResult` + UNIQUE index |
| `BR-PAY-02` | Staff/Manager cannot manually mark Deposit/Rental Invoice PAID. | Payment, Invoice | Procedure-only write path + permissions |
| `BR-LATE-01` | LateFee exists only for overdue RENTAL_FEE Invoice. | LateFee, Invoice | SP + TRG |
| `BR-LATE-02` | LateFee amount uses the Contract-captured Policy divisor. | LateFee, Invoice, Contract, Policy | `usp_CalculateLateFee` |
| `BR-REN-01` | Renewal only for ACTIVE Contract with no pending RETURN Visit. | Contract, Visit | `usp_RenewContract` |
| `BR-REN-02` | Renewal period is contiguous from month after current EndMonth. | Contract, ContractExtension | `usp_RenewContract` |
| `BR-REN-03` | Renewal checks capacity for every extension month atomically. | Contract, Reservation, StorageUnit | `usp_RenewContract` |
| `BR-REN-04` | Successful renewal appends ContractExtension and updates Contract.EndMonth in one transaction. | Contract, ContractExtension | `usp_RenewContract` |
| `BR-RET-01` | ActualReturnDate, not VisitDate, determines Normal vs Early Return. | Visit, Contract | `usp_ConfirmActualReturn` |
| `BR-RET-02` | Confirmed return moves StorageUnit IN_USE -> INSPECTION and creates Inspection PENDING atomically. | Contract, Visit, StorageUnit, Inspection | `usp_ConfirmActualReturn` |
| `BR-RET-03` | Inspection claim is atomic: only one Staff can move PENDING -> IN_PROGRESS. | Inspection, Employee | `usp_ClaimInspection` |
| `BR-RET-04` | Early Return before Contract end terminates Contract after return/inspection finalization. | Contract, Visit, Inspection | `usp_FinalizeReturn` |
| `BR-RET-05` | Normal return completes Contract only after actual return/recovery + completed Inspection + settlement record. | Contract, Inspection, DepositSettlement | `usp_FinalizeReturn` |
| `BR-RET-06` | Early return within Policy waive window may cancel/waive unpaid current-month rental invoice and must not generate LateFee for that month. | Contract, Invoice, Policy | `usp_ConfirmActualReturn` |
| `BR-DMG-01` | DamageRecord is attached to Inspection and uses an active DamageType. | Inspection, DamageRecord, DamageType | `usp_RecordDamage` |
| `BR-DMG-02` | Facility Staff records Damage as PENDING; same-Facility Facility Manager atomically decides PENDING -> APPROVED/REJECTED; terminal decisions are audited. | DamageRecord, Inspection, Employee, AuditLog | `usp_DecideDamage` + TRG |
| `BR-DMG-03` | Finalize Return is blocked while any DamageRecord is PENDING; settlement includes APPROVED Damage only and REJECTED contributes zero. | DamageRecord, DepositSettlement | `usp_FinalizeReturn` |
| `BR-EXT-01` | ExtraFee is attached to Inspection and snapshots configured ExtraFeeType amount/actual override. | Inspection, ExtraFee, ExtraFeeType | `usp_RecordExtraFee` |
| `BR-SET-01` | Settlement uses applicable LateFee + ExtraFee + approved Damage amount. | DepositSettlement, LateFee, ExtraFee, DamageRecord | `usp_FinalizeReturn` |
| `BR-SET-02` | Early Return forfeits Deposit; RefundAmount = 0. | Contract, DepositSettlement | `usp_FinalizeReturn` |
| `BR-POL-01` | Policy changes create a new version; historical values are not overwritten. | Policy | `usp_CreatePolicyVersion` + TRG |
| `BR-POL-02` | Existing Reservation/Contract uses its captured PolicyId, not the currently ACTIVE Policy. | Reservation, Contract, Policy | all lifecycle SPs |
| `BR-SUP-01` | Support Ticket is Contract-related and does not directly mutate Contract/Payment/StorageUnit lifecycle. | SupportTicket | SP boundary |
| `BR-NOT-01` | Failed notification remains PENDING and can be retried. | NotificationLog | background job/SP |
| `BR-AUD-01` | ContractExtension, LoginHistory, AuditLog are append-only. | respective tables | TRG |
| `BR-AUTH-01` | Customer login identifier is unique PhoneNumber; Employee login identifier is unique Email; login identifiers are immutable in Release 1. | UserAccount | UNIQUE + application/SP guard |
| `BR-AUTH-02` | Unknown login identifier does not create LoginHistory; Employee initial credential plaintext is never persisted/logged. | UserAccount, LoginHistory, AuditLog | application/service + audit guard |

---

## 18. State Transition Matrix

### 18.1 Reservation

| From | To | Allowed by | Preconditions |
|---|---|---|---|
| `PENDING_DEPOSIT` | `CONFIRMED` | `usp_ConfirmReservation` | Deposit Invoice paid; valid handover Visit date. |
| `PENDING_DEPOSIT` | `CANCELLED` | timeout/customer cancellation procedure | Cancellation/timeout rule satisfied. |
| `CONFIRMED` | `COMPLETED` | `usp_CompleteHandover` | Successful handover transaction. |
| `CONFIRMED` | `CANCELLED` | `usp_CancelReservation` / no-show procedure | RESERVATION Visit not past allowed cancellation state; no Contract created. |

### 18.2 Visit

| From | To | Preconditions |
|---|---|---|
| `SCHEDULED` | `CHECKED_IN` | Correct Staff/Facility/date and referenced lifecycle valid. |
| `CHECKED_IN` | `CHECKED_OUT` | Visit handling complete. |
| `SCHEDULED` | `CANCELLED` | Type-specific cancellation rule satisfied. |

All other transitions are rejected.

### 18.3 Contract

| From | To | Meaning |
|---|---|---|
| `ACTIVE` | `COMPLETED` | Normal return/recovery completed. |
| `ACTIVE` | `TERMINATED` | Early return completed. |

`COMPLETED` and `TERMINATED` are terminal.

### 18.4 StorageUnit

| From | To | Event |
|---|---|---|
| `AVAILABLE` | `IN_USE` | Atomic Complete Handover. |
| `AVAILABLE` | `MAINTENANCE` | Preventive maintenance by same-Facility Manager; no ACTIVE Contract. |
| `IN_USE` | `INSPECTION` | Actual return/recovery confirmed. |
| `INSPECTION` | `AVAILABLE` | Inspection complete, no maintenance required. |
| `INSPECTION` | `MAINTENANCE` | Inspection complete, maintenance required. |
| `MAINTENANCE` | `AVAILABLE` | Maintenance handled outside detailed core workflow; unit manually/operationally released. |

### 18.5 Invoice

| From | To | Condition |
|---|---|---|
| `UNPAID` | `PAID` | Successful gateway payment result. |
| `UNPAID` | `OVERDUE` | Rental Fee crosses captured Policy overdue threshold. |
| `OVERDUE` | `PAID` | Successful gateway payment result. |
| `UNPAID` | `CANCELLED` | Explicit business case such as valid early-return waiver. |

Deposit Invoice should not become OVERDUE; its unpaid timeout cancels the Reservation instead.

### 18.6 Payment

| From | To | Condition |
|---|---|---|
| `PENDING` | `SUCCESS` | Gateway confirms success. |
| `PENDING` | `FAILED` | Gateway confirms failure. |

A retry may create another Payment attempt rather than mutating a FAILED attempt back to PENDING.

### 18.7 Inspection

| From | To | Condition |
|---|---|---|
| `PENDING` | `IN_PROGRESS` | Atomic claim by active Facility Staff. |
| `IN_PROGRESS` | `COMPLETED` | Same claimed Staff/authorized actor completes inspection. |

### 18.8 DamageRecord

| From | To | Condition |
|---|---|---|
| `PENDING` | `APPROVED` | Same-Facility Facility Manager decision. |
| `PENDING` | `REJECTED` | Same-Facility Facility Manager decision. |

`APPROVED` and `REJECTED` are terminal.

### 18.9 SupportTicket

| From | To | Condition |
|---|---|---|
| `OPEN` | `IN_PROGRESS` | Staff assigned/processing begins. |
| `OPEN` | `CANCELLED` | Customer cancels before completion. |
| `IN_PROGRESS` | `COMPLETED` | ResultNote/completion recorded. |
| `IN_PROGRESS` | `CANCELLED` | Allowed cancellation before completion. |

### 18.10 NotificationLog

| From | To | Condition |
|---|---|---|
| `PENDING` | `SENT` | Delivery succeeds. |
| `PENDING` | `PENDING` | Delivery fails; retry later. |

---

## 19. Calculation Dictionary

| Calculation ID | Formula / source | Notes |
|---|---|---|
| `CALC-DEP-01` Deposit amount | `Reservation.DepositAmount = UnitType.RentalPrice` at Reservation creation | Snapshot; later UnitType price change does not rewrite it. |
| `CALC-INV-01` Initial BaseAmount | `Reservation.LockedRentalPrice` | Used for BillingMonth inside original Reservation period. |
| `CALC-INV-02` Renewal BaseAmount | `ContractExtension.AppliedMonthlyPrice` | Select extension covering BillingMonth. |
| `CALC-DIS-01` DiscountAmount | `BaseAmount * Discount.Percentage / 100` | Applies only from the second Rental Fee invoice onward when a Contract Discount is captured; first month is always 0 discount. |
| `CALC-INV-03` AmountDue | `MAX(BaseAmount - DiscountAmount, 0)` | Deposit has DiscountAmount = 0. |
| `CALC-LATE-01` OverdueDays | Number of chargeable overdue days from captured Policy `OverdueStartDay` to calculation/return/payment cutoff | Exact date arithmetic implemented consistently in SP. |
| `CALC-LATE-02` LateFee | `(RentalFeeAmount / Policy.LateFeeDivisorDays) * OverdueDays` | Use `DECIMAL`, not floating-point. |
| `CALC-SET-01` TotalDeduction | `ApplicableLateFee + ApplicableExtraFee + ApprovedDamageAmount` | Late/Extra have no approval status in V1; inclusion is determined by return rules. |
| `CALC-SET-02` RefundAmount | Normal return: `MAX(DepositPaidAmount - TotalDeduction, 0)` | Early return forces 0. |
| `CALC-SET-03` AdditionalAmountDue | `MAX(TotalDeduction - DepositPaidAmount, 0)` | Recorded obligation only. |
| `CALC-CAP-01` Available capacity | `eligible physical-unit capacity - Reservation holds - Contract occupancy` for each month | Must evaluate every month in requested period. |
| `CALC-REP-01` Usage Rate | `IN_USE rentable StorageUnits / total rentable StorageUnits` | Derived reporting value; not persisted. |
| `CALC-REP-02` Revenue | Sum of successfully paid Rental Fee amounts in reporting period | Deposit/late/extra/damage excluded from core revenue. |

### 19.1 Monetary rounding

V1 recommendation: calculate intermediate money values in `DECIMAL(18,4)` inside stored procedures when division is involved, then round final persisted amount to `DECIMAL(18,2)` using one consistent rule. Do not mix application-side binary floating-point calculations with DB-side decimal calculations.

---

## 20. Actor / Ownership Matrix

This matrix is primarily an authorization contract for API/stored-procedure execution. Direct table DML should be restricted in production-like environments.

| Capability / entity | Customer | Facility Staff | Facility Manager | Business Ops Manager | System Administrator |
|---|---:|---:|---:|---:|---:|
| Own profile/account basic view | R/U limited | R/U limited | R/U limited | R/U limited | R all basic accounts |
| Reservation create/cancel/view own | Yes | Operational R | Facility R | System R | No business mutation |
| RESERVATION/ACCESS/RETURN Visit create own | Yes where applicable | Check-in/out | Monitor | R | No business mutation |
| StorageUnit CRUD/status | No | Operational handling | Yes at assigned Facility | R/monitor | No business mutation |
| Contract view own | Yes | Facility R | Facility R/operational handover | System R | No business mutation |
| Contract renewal request | Yes | No | Monitor | Monitor | No |
| Invoice/Payment own view/pay | Yes | Verify state only | Monitor | Report/monitor | No manual PAID mutation |
| Policy | R if exposed | R | R | Create new version | No |
| UnitType price/master | R | R | R | Manage | No |
| Discount | Own eligible list/use | R as needed | R | Manage business data | No |
| Inspection | R own return progress | Claim/perform | Monitor/manage Facility | R | No |
| Damage/ExtraFee | R own settlement | Record Damage/ExtraFee | Decide PENDING Damage in assigned Facility; monitor | Configure ExtraFeeType; DamageType is fixed seed/read-only | No |
| SupportTicket | Create/view/cancel own | Process assigned | Assign/monitor Facility | R/monitor | No |
| User/Employee administration | No | No | No | No | Yes |
| LoginHistory/AuditLog | Own not required | No | No | No | View |

`R/U` above is conceptual; exact API permissions should follow endpoint design and facility scoping.

---

## 21. Foreign Key Delete / Update Strategy

V1 should use **no cascading hard deletes** for core business data. Historical integrity is more important than convenience.

### 21.1 Global rule

- FK action: `ON DELETE NO ACTION` / `ON UPDATE NO ACTION` by default.
- Business master records use `Status=INACTIVE` instead of delete when referenced historically.
- Reservation, Contract, Invoice, Payment, Visit, Inspection, fee, settlement and audit records are never hard-deleted through normal business flows.
- Append-only tables reject UPDATE/DELETE through triggers and permissions.

### 21.2 Entity-specific guidance

| Parent | Child | Delete behavior |
|---|---|---|
| UserRole | UserAccount | NO ACTION |
| UserAccount | Customer/Employee/LoginHistory/AuditLog/NotificationLog | NO ACTION |
| Facility | Employee/StorageUnit/Reservation/Contract | NO ACTION; deactivate Facility instead |
| UnitType | StorageUnit/Reservation | NO ACTION; do not delete referenced UnitType |
| Customer | Reservation/Contract/Discount/SupportTicket | NO ACTION |
| Reservation | Contract | NO ACTION |
| Contract | ContractExtension/SupportTicket/Inspection/DepositSettlement | NO ACTION |
| Invoice | Payment/LateFee | NO ACTION |
| Inspection | DamageRecord/InspectionEvidence/ExtraFee | NO ACTION |
| DamageType | DamageRecord | NO ACTION; inactivate DamageType |
| ExtraFeeType | ExtraFee | NO ACTION; inactivate type |
| Policy | Reservation/Contract | NO ACTION; historical Policy immutable |

---

## 22. Transaction, Concurrency and Isolation Matrix

| Operation | Main race condition | Required protection |
|---|---|---|
| Create Reservation | Two customers reserve last capacity; concurrent profile clear/update can stale the CCCD check. | Existing atomic capacity/create transaction also enforces BR-RES-09 on authoritative Customer data before writes; use consistent transaction/locking semantics so a separate stale FE/login check cannot bypass it. Missing CCCD leaves no Reservation/hold/Invoice. Preserve existing capacity isolation. |
| Complete Handover | Two handovers select same AVAILABLE StorageUnit. | Transaction + update lock on selected StorageUnit; validate `Status=AVAILABLE` at update time. |
| Renew Contract | Renewal and new Reservation consume same future capacity. | Same capacity locking strategy as Reservation across every extension month. |
| Claim Inspection | Two Staff claim one PENDING Inspection. | Single conditional UPDATE `WHERE Status='PENDING'`; require exactly 1 affected row. |
| Apply verified payOS webhook | Gateway retries same signed webhook. | UNIQUE `TransactionCode`/provider reference + idempotent PostgreSQL function. |
| Generate monthly invoice | Job retried / multiple workers. | Unique filtered index on Contract+BillingMonth for RENTAL_FEE + insert-if-absent transaction. |
| Create Deposit invoice | Reservation creation retried. | Unique filtered index on Reservation EntityId for DEPOSIT. |
| Create Policy version | Two admins create versions concurrently. | Transaction + lock current active Policy/version sequence; filtered unique active-policy index. |
| Finalize Return | Duplicate finalization request. | UNIQUE `DepositSettlement.ContractId`; Contract terminal-state check; idempotent procedure behavior. |
| Retry Notification | Multiple job executions pick the same row. | Claim/update pattern or background-job lock; SENT rows are terminal. |

### 22.1 PostgreSQL routine transaction / concurrency template

PostgreSQL routines run within the caller's transaction; explicit `BEGIN TRANSACTION` inside a PL/pgSQL **function** is not supported. EF Core Repository/Service must own an atomic transaction when multiple calls span one command, and individual database functions must maintain internal lock/check/insert integrity.

```sql
-- Illustration only; actual Pg001–Pg013 SQL is authoritative.
CREATE OR REPLACE FUNCTION public."usp_ExampleAtomic"(p_id uuid)
RETURNS void LANGUAGE plpgsql AS $frms$
BEGIN
    PERFORM 1 FROM public."Reservation"
        WHERE "ReservationId" = p_id FOR UPDATE;
    IF NOT FOUND THEN
        RAISE EXCEPTION USING ERRCODE='P1101', MESSAGE='RESOURCE_NOT_FOUND';
    END IF;
    -- Validate role / status / domain invariants and apply one atomic mutation.
END $frms$;
```

Use server-side `FOR UPDATE`, advisory transaction locks or equivalent safe primitives where the real repository SQL demonstrates necessity; unique partial indexes resolve active assignment conflicts. Translate known SQLSTATE codes into stable API errors without leaking raw SQL. Pg013 assignment and handover trigger consumption must succeed or rollback as a single transaction. No external payOS network request inside database transaction.

---

## 23. Idempotency Matrix

| Operation | Idempotency key / invariant | Repeat behavior |
|---|---|---|
| payOS signed webhook | Verified provider reference and order code | Duplicate verified success never re-applies payment, invoice or audit effects. |
| Complete Handover | `Contract.ReservationId` UNIQUE + terminal Reservation state | Repeat after success returns existing result or controlled "already completed" error. |
| Confirm Reservation | Reservation status + one RESERVATION Visit | Repeat must not create second Visit. |
| Monthly invoice generation | `(ContractId, BillingMonth)` filtered uniqueness | Duplicate call returns/uses existing invoice. |
| LateFee calculation | `LateFee.InvoiceId` UNIQUE | Recalculate/update same LateFee row; do not insert duplicates. |
| Renewal | Contract current EndMonth + extension uniqueness/check | Same requested NewEndMonth must not append duplicate extension. |
| Confirm Actual Return | Return-confirmation/RETURN-Visit idempotency + Unit status | Repeating the same return confirmation must not create a duplicate Inspection for that return event; the Contract may still have multiple historical Inspections across distinct recovery/return events. |
| Finalize Return | `DepositSettlement.ContractId` UNIQUE | Repeat uses existing settlement; Contract terminal transition occurs once. |
| Queue notification | Business event key recommended at application level | Avoid duplicate user notifications where event semantics require one. |

---

## 24. Error Code Dictionary for Stored Procedures

Stored procedures should raise stable business error codes/messages that API code can map consistently.

| Error code | Meaning |
|---|---|
| `ACCOUNT_INACTIVE` | Account cannot perform action. |
| `VALIDATION_ERROR` | OD-09 missing saved CCCD is mapped to API HTTP 400 with errors.cccd and traceId; no raw CCCD, conflicting owner or SQL/provider detail. Existing routine error mapping is aligned by the implementation task, not a new guessed SQLSTATE here. |
| `FACILITY_INACTIVE` | New Reservation not allowed at Facility. |
| `INVALID_MONTH_RANGE` | Start/End month invalid. |
| `CAPACITY_NOT_AVAILABLE` | Required month range has insufficient capacity. |
| `RESERVATION_INVALID_STATUS` | Reservation transition/action invalid. |
| `DEPOSIT_NOT_PAID` | Reservation confirmation precondition failed. |
| `VISIT_DATE_OUT_OF_POLICY` | Visit day outside captured Policy window. |
| `VISIT_INVALID_STATUS` | Invalid Visit transition. |
| `VISIT_ENTITY_MISMATCH` | Polymorphic EntityId does not match VisitType. |
| `CONTRACT_NOT_ACTIVE` | Action requires ACTIVE Contract. |
| `RETURN_VISIT_PENDING` | ACCESS/Renewal blocked by RETURN Visit. |
| `UNIT_NOT_AVAILABLE` | StorageUnit cannot be allocated. |
| `UNIT_FACILITY_TYPE_MISMATCH` | Unit does not match Reservation Facility/UnitType. |
| `FIRST_MONTH_PAYMENT_NOT_SUCCESS` | **Historical retired** error; V11 requires Staff offline receipt acknowledgment, not online Payment. |
| `DISCOUNT_NOT_OWNED_BY_CUSTOMER` | Contract Discount belongs to another Customer. |
| `DISCOUNT_NOT_VALID` | Discount inactive/outside effective period. |
| `INVOICE_ALREADY_EXISTS` | Duplicate logical invoice attempt. |
| `PAYMENT_CALLBACK_DUPLICATE` | Callback already processed; should normally be handled idempotently rather than treated as fatal. |
| `INSPECTION_ALREADY_CLAIMED` | Another Staff already claimed Inspection. |
| `INSPECTION_INVALID_STATUS` | Invalid inspection transition. |
| `RENEWAL_CAPACITY_NOT_AVAILABLE` | Future extension capacity unavailable. |
| `RENEWAL_NOT_CONTIGUOUS` | Requested extension period has a gap/invalid end month. |
| `POLICY_VERSION_CONFLICT` | Concurrent policy-version creation conflict. |
| `SUPPORT_TICKET_INVALID_STATUS` | Invalid SupportTicket transition. |
| `RETURN_ALREADY_FINALIZED` | Contract return/settlement already completed. |
| `DAMAGE_DECISION_PENDING` | Return finalization blocked by one or more PENDING DamageRecord rows. |
| `DAMAGE_INVALID_STATUS` | Damage decision attempted from a non-PENDING state. |
| `CREDENTIAL_PROVISIONING_INVALID_STATUS` | Employee credential provisioning/retry attempted from an invalid account state. |
| `EMAIL_ALREADY_EXISTS` | Duplicate UserAccount Email. |
| `PHONE_NUMBER_ALREADY_EXISTS` | Duplicate UserAccount PhoneNumber. |

PostgreSQL routines use stable SQLSTATE / ERRCODE and `RAISE EXCEPTION USING MESSAGE=...` per deployed migration SQL. Do not allocate SQL Server `THROW 510xx` error numbers for V11.

---

## 25. Index Strategy

The following indexes are recommended in addition to PK/unique constraints. Final INCLUDE columns should be tuned after query/API implementation.

| Table | Recommended index | Purpose |
|---|---|---|
| UserAccount | UNIQUE `(Email)` | Login/account lookup. |
| UserAccount | `(RoleId, Status)` | Admin/RBAC filtering. |
| Customer | UNIQUE filtered `(CCCD) WHERE CCCD IS NOT NULL` | Nullable citizen-ID uniqueness; OD-09's new-Reservation precondition does not change this index or make the column NOT NULL. |
| Employee | `(FacilityId)` | Facility staff lookup. |
| Facility | `(Status)` | Browse active Facilities. |
| StorageUnit | UNIQUE `(FacilityId, UnitCode)` | Operational code uniqueness. |
| StorageUnit | `(FacilityId, UnitTypeId, Status)` | Availability/handover/capacity queries. |
| HandoverUnitAssignment | UNIQUE partial `(ReservationId) WHERE Status='ACTIVE'` | Only one active Unit assignment per Reservation. |
| HandoverUnitAssignment | UNIQUE partial `(StorageUnitId) WHERE Status='ACTIVE'` | Prevent simultaneous Unit assignment to two Reservations. |
| HandoverUnitAssignment | `(ReservationId, AssignedAt DESC)` | Immutable Manager assignment history. |
| Reservation | `(FacilityId, UnitTypeId, StartMonth, EndMonth, Status)` | Capacity planning. |
| Reservation | `(CustomerId, Status)` | Customer rental dashboard. |
| Visit | `(VisitType, Status, VisitDate)` | Daily work list. |
| Visit | `(EntityId, VisitType, Status)` | Lifecycle lookup. |
| Contract | UNIQUE `(ReservationId)` | One Contract per Reservation. |
| Contract | filtered UNIQUE `(StorageUnitId) WHERE Status='ACTIVE'` | One active occupancy per unit. |
| Contract | `(CustomerId, Status)` | Customer active contracts. |
| Contract | `(FacilityId, Status)` | Facility monitoring. |
| ContractExtension | `(ContractId, OldEndMonth, NewEndMonth)` | Renewal price/history lookup. |
| Discount | `(CustomerId, Status, EffectiveFrom, EffectiveTo)` | Contract discount selection. |
| Invoice | filtered UNIQUE `(EntityId) WHERE InvoiceType='DEPOSIT'` | One logical Deposit invoice per Reservation. |
| Invoice | filtered UNIQUE `(EntityId, BillingMonth) WHERE InvoiceType='RENTAL_FEE'` | One monthly invoice per Contract/month. |
| Invoice | `(Status, DueDate, InvoiceType)` | Overdue/payment jobs. |
| Invoice | `(EntityId, InvoiceType, BillingMonth)` | Contract/Reservation billing history. |
| Payment | UNIQUE filtered `(TransactionCode) WHERE TransactionCode IS NOT NULL` | Gateway idempotency. |
| Payment | `(InvoiceId, Status, CreatedAt)` | Payment attempts per invoice. |
| LateFee | UNIQUE `(InvoiceId)` | One current late-fee row per invoice. |
| SupportTicket | `(ContractId, Status)` | Customer/contract support list. |
| SupportTicket | `(AssignedEmployeeId, Status)` | Staff work list. |
| Inspection | `(ContractId, Status)` | Return lifecycle. |
| Inspection | `(StorageUnitId, CompletedAt)` | Unit inspection history. |
| DamageRecord | `(InspectionId, Status)` | Settlement calculation. |
| ExtraFee | `(InspectionId)` | Settlement calculation. |
| DepositSettlement | UNIQUE `(ContractId)` | One settlement per Contract. |
| LoginHistory | `(UserAccountId, LoginAt DESC)` | Admin login history. |
| AuditLog | `(EntityType, EntityId, CreatedAt DESC)` | Entity audit trail. |
| AuditLog | `(UserAccountId, CreatedAt DESC)` | User activity history. |
| NotificationLog | `(Status, CreatedAt)` | Pending retry queue. |

---

## 26. Audit Event Dictionary

At minimum, the following actions should be written to `AuditLog`. High-volume reads do not need audit rows unless required later.

| Action | EntityType | When |
|---|---|---|
| `CREATE_RESERVATION` | `RESERVATION` | Reservation created. |
| `CONFIRM_RESERVATION` | `RESERVATION` | Deposit paid + Reservation confirmed. |
| `CANCEL_RESERVATION` | `RESERVATION` | Customer/system cancellation. |
| `CHECK_IN_VISIT` | `VISIT` | Staff checks Customer in. |
| `CHECK_OUT_VISIT` | `VISIT` | Visit completed. |
| `CANCEL_VISIT` | `VISIT` | Scheduled Visit cancelled. |
| `COMPLETE_HANDOVER` | `CONTRACT` | Contract activated and unit handed over. |
| `CREATE_RENTAL_INVOICE` | `INVOICE` | Monthly invoice generated. |
| `PAYMENT_RESULT` | `PAYMENT` | Gateway result applied. |
| `MARK_INVOICE_OVERDUE` | `INVOICE` | Rental invoice becomes overdue. |
| `RENEW_CONTRACT` | `CONTRACT_EXTENSION` | Renewal succeeds. |
| `CONFIRM_ACTUAL_RETURN` | `CONTRACT` / `VISIT` | Actual physical return recorded. |
| `CLAIM_INSPECTION` | `INSPECTION` | Staff claims inspection. |
| `COMPLETE_INSPECTION` | `INSPECTION` | Inspection completed. |
| `RECORD_DAMAGE` | `DAMAGE_RECORD` | Facility Staff records Damage as PENDING. |
| `DECIDE_DAMAGE` | `DAMAGE_RECORD` | Facility Manager approves/rejects PENDING Damage. |
| `RECORD_EXTRA_FEE` | `EXTRA_FEE` | Extra fee recorded. |
| `FINALIZE_RETURN` | `DEPOSIT_SETTLEMENT` | Settlement recorded; Contract terminal state applied. |
| `CREATE_POLICY_VERSION` | `POLICY` | New Policy version activated. |
| `UPDATE_UNIT_PRICE` | `UNIT_TYPE` | Current rental price changed. |
| `CREATE_OR_UPDATE_DISCOUNT` | `DISCOUNT` | Customer discount definition changed. |
| `ASSIGN_SUPPORT_TICKET` | `SUPPORT_TICKET` | Staff assignment. |
| `COMPLETE_SUPPORT_TICKET` | `SUPPORT_TICKET` | Ticket completed. |
| `CHANGE_ACCOUNT_STATUS` | `USER_ACCOUNT` | Activate/deactivate. |
| `CHANGE_EMPLOYEE_ROLE_FACILITY` | `EMPLOYEE` | Admin role/facility assignment changes. |
| `PROVISION_EMPLOYEE_CREDENTIAL` | `USER_ACCOUNT` / `EMPLOYEE` | Initial credential email accepted or provisioning retry outcome changes account state; plaintext password excluded. |

For large `OldValue`/`NewValue`, log only business-relevant fields rather than binary evidence or password hashes.

---

## 27. Scheduled Job Dictionary — implementation-aligned 2026-10-21

The six required business tasks are grouped into four hosted services on `main` `0219f30734b0`. This is a **code-observed implementation state**; deployment, test and production scheduler state are NOT VERIFIED.

| SRS job ID | Hosted service / ordering | Actual source interval | Default flag / effect | Routine |
|---|---|---|---|---|
| `job_ExpirePendingReservations` | Existing `ExpirePendingReservationsJob` | Immediate + every **10 minutes** | Always active with the API host | `usp_ExpirePendingReservations` |
| `job_GenerateMonthlyInvoices` | `BillingMaintenanceJob` step 1 | Immediate + every **24 hours** | Billing **true** when config absent | `usp_GenerateMonthlyInvoices` → `usp_CreateMonthlyInvoice` |
| `job_MarkOverdueInvoices` | `BillingMaintenanceJob` step 2 | Follows successful step 1 | Same Billing flag | `usp_MarkOverdueInvoices` |
| `job_RecalculateOpenLateFees` | `BillingMaintenanceJob` step 3 | Follows successful step 2 | Same Billing flag | `usp_RecalculateOpenLateFees` → `usp_CalculateLateFee` |
| `job_ProcessReservationNoShow` | `ProcessReservationNoShowJob` | Immediate + every **24 hours** when enabled | NoShow **false** when config absent | `usp_ProcessReservationNoShow` |
| `job_RetryNotifications` | `RetryNotificationsJob` | Immediate + every **5 minutes** when enabled | Notifications **false** when absent; current adapter rejects sends | `usp_RetryPendingNotification` → `INotificationSender.SendAsync` → `usp_MarkNotificationSent` |

### 27.1 Host configuration and execution semantics

- `Program.cs` registers `AddFrmsBackgroundJobs()`. `BackgroundJobs:Billing:Enabled` has explicit `EnabledByDefault=true`; environment variable `BackgroundJobs__Billing__Enabled=false` overrides and pauses Billing only after application restart. `BackgroundJobs__NoShow__Enabled` and `BackgroundJobs__Notifications__Enabled` default false. Expiration has no new config switch.
- Jobs use .NET `PeriodicTimer` relative to startup, **not** a fixed 00:00 cron. A run starts at startup, then 10m / 24h / 5m intervals. Persisted financial times UTC; business month/day thresholds `Asia/Ho_Chi_Minh`.
- Required dependency flow: `Api BackgroundJob → IRecurringJobsService → IRecurringJobsRepository → PostgreSQL function`; existing expiration uses `IReservationExpirationService`/Repository. Do not call DbContext directly in jobs. Four registrations do not imply all jobs enabled or running successfully.
- Three financial operations are a **single ordered pipeline**. A failed first or second stage blocks later stages during the run. The job catches/logs exceptions and retries at the next timer tick or restart. No new HTTP mutation endpoint exists.

### 27.2 Financial data effects and invariants

1. **Monthly invoices:** `usp_GenerateMonthlyInvoices` chooses only the current business month (`Asia/Ho_Chi_Minh`) if no argument is given, scans `Contract.Status=ACTIVE`, `StartMonth < month <= EndMonth`; it does not auto-generate missed **past** months. `usp_CreateMonthlyInvoice` rejects first-month generation, uses locked Reservation rental price or `ContractExtension.AppliedMonthlyPrice`, captured Discount and `Policy.MonthlyPaymentDueDay`, inserts `InvoiceType=RENTAL_FEE, Status=UNPAID`.
2. **Idempotency:** Existing `(ContractId/EntityId, BillingMonth, InvoiceType=RENTAL_FEE)` uniqueness plus SQL advisory locking prevent duplicate monthly bills. Re-running does not reprice prior stored invoice snapshots. Test concurrent application instances and any external scheduler before deployment.
3. **Overdue:** `usp_MarkOverdueInvoices` transitions eligible `UNPAID` later-month rental invoices to `OVERDUE` using Contract's captured `Policy.OverdueStartDay` and business calendar threshold.
4. **LateFee:** `usp_RecalculateOpenLateFees` scans eligible `OVERDUE` rental invoices and calls `usp_CalculateLateFee`; for a given `InvoiceId`, it **INSERTs if absent, UPDATEs if present** with `OverdueDays`, `Amount`, `CalculatedAt`. Formula uses `Policy.LateFeeDivisorDays`. Never insert a second LateFee for the same Invoice.
5. **Offline first month:** At handover the Staff records offline receipt through OPS-004/Contract creation. No new first-month Invoice/Payment, gateway job, automatic payOS initiation or refund is authorized.

### 27.3 Notification delivery and concurrency safety

- `usp_RetryPendingNotification` is **selection only**, returning one oldest `PENDING` row (`LIMIT 1 FOR UPDATE SKIP LOCKED`). A successful queue read does NOT mean sent.
- Repository acquires PostgreSQL session advisory lock `70439120` around sender call; no database transaction spans external delivery. `INotificationSender.SendAsync` must acknowledge actual delivery before `usp_MarkNotificationSent` updates `NotificationLog.Status='SENT'`/`SentAt`.
- Current DI binds `INotificationSender` to `UnconfiguredNotificationSender`. It rejects sends; Notifications default OFF. Real sender implementation and provider validation/credentials/deduplication are missing. Failure MUST keep PENDING and avoid logging notification content or recipient PII.
- Provider may deliver immediately before worker crashes, leaving PENDING; retry then may re-send (at-least-once). Use `NotificationLogId` as external provider idempotency reference. One item per enabled 5-minute tick may cause queue backlog; throughput/retry/backoff need acceptance decision.
- `ProcessReservationNoShowJob` is separate from notifications and billing. It remains OFF pending cross-visit/Policy/concurrent-state DB tests. Existing `ExpirePendingReservationsJob` remains ON and unchanged.

### 27.4 Missing acceptance evidence / DB test extension

| Gate | Target checks | Status |
|---|---|---|
| `JOB-DBT-01` | Later-month Invoice created once, first month excluded; captured Policy/Discount and renewal pricing correct | NOT RUN |
| `JOB-DBT-02` | Run/retry/concurrency across two API replicas; SQL function and partial unique index prevent double charges | NOT RUN |
| `JOB-DBT-03` | Overdue GMT+7 boundary, LateFee daily increase, one LateFee/Invoice, no stale/prior-month or paid-invoice side effects | NOT RUN |
| `JOB-DBT-04` | No-show on SCHEDULED vs CHECKED_IN; no accidental close of fulfilled Visit/Reservation; Policy window and concurrency | NOT RUN |
| `JOB-DBT-05` | Notification unconfigured/failing/succeeding provider, PENDING vs SENT, crash after send, advisory lock, one-row backlog | NOT RUN |
| `JOB-OPS-01` | .NET build and full NUnit, Pg001..Pg013 deployment, external scheduler ownership, logging/alerting, flags/pause and multi-instance execution | NOT VERIFIED |
| `JOB-DEC-01` | Decide handling of **missed previous billing months** after outage; current generator only current month. Approval required before backfill/retroactive invoice behavior | PENDING |
| `JOB-DEC-02` | Decide whether startup-relative 24-hour interval meets exact billing/late-fee SLAs and whether a midnight business-time schedule is required | PENDING |

The patch's `RecurringJobRegistrationTests` only asserts four hosted service registrations; it is **not** evidence of any DB routine/financial E2E passing. No new table, migration or HTTP API route is introduced. No SQL Server Agent scheduling is active in the observed PostgreSQL runtime. See SRS §9.11 and `V11-G10..G13`; API §13A.10.

---

## 28. PostgreSQL Routine Interface Conventions

- In this implementation, SQL business routines are PostgreSQL `plpgsql` functions/procedures and SQL functions, with historical names `usp_*` where retained. Consult the actual `backend/sql/postgresql/*.sql` sources and migration resource mappings before copying a function signature.
- Functions accept scoped IDs/business input, not complete serialized EF Entities or client-provided role assertions. Security context is established in API/Service and revalidated where high-risk routines demand it.
- Use PostgreSQL argument naming (`p_reservation_id`, `p_user_account_id`) and `RAISE EXCEPTION USING ERRCODE='P....', MESSAGE='STABLE_CODE'` (per deployed SQL), not SQL Server `@Id`, `THROW`, `SET NOCOUNT`, `XACT_ABORT` or T-SQL `TRY/CATCH`.
- Return created identifiers/state; preserve historical snapshots and sealed rows. Stable error mapping belongs in repository/service response projection.
- Critical operations: reservation capacity, payOS result, renewal, return settlement, assignment create/replace/release and consumption must be atomic and concurrency-safe.
- Payment URL/idempotency key/customer PII and evidence bytes must not appear in logs or diagnostics.
- For clock-sensitive logic use `IClock` at Business level and approved routine parameters where available; provider financial timestamps are not substituted with local server time.

**Observed Pg013 interfaces:** `public."usp_AssignHandoverUnit"(p_reservation_id uuid, p_storage_unit_id uuid, p_user_account_id uuid) RETURNS uuid`; `public."usp_ReleaseHandoverUnit"(p_reservation_id uuid, p_user_account_id uuid) RETURNS void`. `AssignedByEmployeeId` is derived from authenticated Manager `UserAccountId`, not supplied by frontend.

---

## 29. PostgreSQL Trigger Implementation Conventions

- Row-level PostgreSQL trigger functions must check `TG_OP` / `OLD` / `NEW` and explicitly prevent prohibited updates/deletes. Do not use SQL Server `inserted`/`deleted` pseudo-tables or T-SQL syntax.
- Pg013 `frms_handover_assignment_history_guard` rejects DELETE and all terminal-history mutations, allowing only valid active-to-terminal transitions with `ClosedAt`.
- Pg013 `frms_require_handover_assignment_on_contract` is a BEFORE INSERT Contract guard: require matching ACTIVE assignment and mark it CONSUMED within the atomic Contract insert operation; any exception rolls back everything.
- Pg010 `frms_audit_storage_unit_status` records verified StorageUnit status transitions in `AuditLog`; no fabricated pre-capture history. Current timeline GETs are owner-approved for Manager's Facility, system-wide BOM and Administrator read-only.
- Protect `ContractExtension`, `AuditLog`, `LoginHistory` from unauthorized mutation per retained append-only rules. Do not log password hashes, CCCD, PaymentUrl, idempotency keys, or binary evidence.
- Prefer CHECK/FK/UNIQUE/partial indexes over triggers when an invariant can be expressed declaratively. PostgreSQL triggers must never orchestrate external network calls or new business flows.

---

## 30. Seed Data Requirements

Phase 0 requires only the approved values for five UserRoles (§30.1), Policy v1 (§30.2), and six fixed DamageTypes (§30.4). ExtraFeeType schema and CHECK constraints remain in the Phase 0 baseline, but its five rows are deferred beyond Phase 0 pending approved amounts and currency.

### 30.1 `UserRole`

Seed exactly five rows:

```text
CUSTOMER
FACILITY_STAFF
FACILITY_MANAGER
BUSINESS_OPERATIONS_MANAGER
SYSTEM_ADMINISTRATOR
```

Use stable UUID values in migration scripts so environments share the same seed identifiers if that helps tests/configuration.

### 30.2 Initial `Policy`

Initial baseline values from the current scope/design:

| Field | Initial value |
|---|---:|
| `Version` | `1` |
| `Status` | `ACTIVE` |
| `DepositTimeoutHours` | `1` |
| `ReservationVisitStartDay` | `1` |
| `ReservationVisitEndDay` | `5` |
| `MonthlyPaymentDueDay` | `5` |
| `OverdueStartDay` | `6` |
| `LateFeeDivisorDays` | `31` |
| `EarlyReturnWaiveFeeUntilDay` | `5` |

`EffectiveFrom` and `CreatedAt` are set at deployment/seed time.

### 30.3 `ExtraFeeType`

Catalogue for later approved deployment, not a Phase 0 seed requirement:

```text
KEY_REPLACEMENT
ACCESS_CARD_REPLACEMENT
LOCK_REPLACEMENT
CLEANING_FEE
OTHER
```

Phase 0 MUST NOT seed these five rows. `DefaultAmount` and currency must be supplied through approved later deployment data; the source does not define specific monetary values. Do not infer zero or any other amount. The entity/table and existing name/status/amount CHECK constraints remain unchanged.

### 30.4 `DamageType`

Seed exactly these production categories:

```text
LOCK_DAMAGE
DOOR_DAMAGE
WALL_DAMAGE
FLOOR_DAMAGE
WATER_DAMAGE
OTHER
```

All rows start `ACTIVE`. `DefaultAmount = NULL` unless approved deployment data supplies a value. Release 1 has no DamageType CRUD workflow; changing this catalogue requires an explicit SRS/seed revision.

---

## 31. Naming Convention

PostgreSQL runtime object naming (deployed original identifiers may remain PascalCase and must be quoted):

| Object | Convention | Example |
|---|---|---|
| Table | singular PascalCase | `ContractExtension` |
| PK constraint | `PK_<Table>` | `PK_Contract` |
| FK constraint | `FK_<Child>_<Parent>_<Column>` | `FK_Contract_Customer_CustomerId` |
| UNIQUE constraint/index | `UQ_<Table>_<Columns>` | `UQ_UserAccount_Email` |
| CHECK constraint | `CK_<Table>_<Rule>` | `CK_Discount_Percentage` |
| DEFAULT constraint | `DF_<Table>_<Column>` | `DF_Reservation_CreatedAt` |
| Nonunique index | `IX_<Table>_<Columns>` | `IX_Visit_Status_VisitDate` |
| Filtered index | `UX_<Table>_<Purpose>` | `UX_Contract_ActiveStorageUnit` |
| Stored procedure | `usp_<Verb><Noun>` | `usp_CompleteHandover` |
| Trigger | `trg_<Table>_<Purpose>` | `trg_Visit_StatusTransition` |
| Background job | `job_<Purpose>` | `job_MarkOverdueInvoices` via `Frms.Api/BackgroundJobs` |

Avoid reserved words and inconsistent abbreviations. Use the same business term everywhere: `StorageUnit`, not alternating with `PhysicalUnit` in physical schema object names.

---

## 32. PostgreSQL EF Core Migration & Deployment Order

Current GitHub `main` contains independent `Frms.Migrations.PostgreSql` `Pg001` through `Pg013`. Migration history is append-only; Pg001–Pg009 represent base PostgreSQL deployment, while Pg010–Pg013 add narrower behavioral/physical changes. The exact migration IDs in code must be used for deployment; this document does not establish which migrations were applied to an environment.

| Migration | Observed purpose | Required verification |
|---|---|---|
| Pg001 | Initial 27-entity schema + approved master seeds | 27 base tables, five roles, Policy v1, fixed DamageType |
| Pg002 | Payment and append-only | PAYOS, key/code/uniqueness and protected history |
| Pg003 | Integrity/audit/reservations | Capacity, statuses, audit and FK invariants |
| Pg004 | Visits, administration and support | Role scope, Visit/Ticket lifecycles |
| Pg005 | Rental and return lifecycle | First-month offline, later-month billing, return guards |
| Pg006 | Inspection routines | Damage/ExtraFee/decision and settlement invariants |
| Pg007 | Deployment seed, billing and jobs | Approved seed scope and scheduled work |
| Pg008 | Handover audit actor | Authenticated audit actor provenance |
| Pg009 | PostgreSQL17 string-search compatibility | Substring collations and parameterized query behavior |
| Pg010 | StorageUnit status timeline audit | Audit capture only from migration time, no historic backfill |
| Pg011 | Developer-only out-of-date Visit check-in | Feature flag must be impossible outside Development |
| Pg012 | UnitType nullable media URLs | `ImageUrl`/`VideoUrl` nullable varchar(2048), public catalog response |
| Pg013 | Approved HandoverUnitAssignment | 28th table, partial UNIQUE, immutable history, routine/trigger atomicity |

**Deployment lock:** Use migrations assembly as both EF `--project` and `--startup-project` for schema updates. Never apply SQL Server migrations or double-register migrations from the runtime `Frms.DataAccess` project. Verify current database and `__EFMigrationsHistory` before any forward deploy; do not backfill synthetic audits, delete legacy payOS evidence, or rewind an active payment code sequence. Schema migration, live banking and data deletion are not authorized by this documentation task.

---

## 33. Physical Schema Checklist Before Generating DDL

The schema is ready for DDL generation when all items below are explicitly represented in migrations:

- [ ] 27 historical base tables plus approved 28th HandoverUnitAssignment table, with PostgreSQL physical types; verify actual DB deployment/migration history.
- [ ] All PKs and direct FKs created.
- [ ] Polymorphic `Visit.EntityId` and `Invoice.EntityId` validation covered by procedures/triggers.
- [ ] All status/type CHECK constraints created, including fixed SupportTicket categories with `OTHER`.
- [ ] Monetary/date-range CHECK constraints created.
- [ ] Filtered UNIQUE indexes created for active StorageUnit Contract, Deposit Invoice, monthly Rental Invoice, nullable CCCD, nullable TransactionCode, active Policy.
- [ ] Defaults created for deterministic initial statuses and timestamps (`Facility=INACTIVE`, `StorageUnit=AVAILABLE`).
- [ ] `Customer 1:N Discount` implemented through `Discount.CustomerId`.
- [ ] `Contract.DiscountId` nullable and ownership/validity guard implemented; referenced Discount financial/effective fields are immutable.
- [ ] No first-month RENTAL_FEE Invoice or Payment; full offline rent acknowledged at handover and revenue counted once; later invoices may apply Contract Discount.
- [ ] Complete Handover enforces `Contract.PolicyId = Reservation.PolicyId`, matching ACTIVE HandoverUnitAssignment and atomic assignment consume; negative/no-assignment cases rollback.
- [ ] Damage decision lifecycle/Manager authorization and PENDING-finalization block are implemented.
- [ ] Reservation immutable business fields protected after creation.
- [ ] Policy historical values protected from overwrite/delete.
- [ ] Append-only protections implemented for ContractExtension, LoginHistory, AuditLog.
- [ ] Stored procedures implemented with transactions and stable business error codes.
- [ ] Capacity-sensitive procedures use an explicit concurrency strategy.
- [ ] Payment callback and recurring jobs are idempotent.
- [ ] Trigger code is multi-row safe.
- [ ] Audit rules exclude secrets/binary payloads.
- [ ] Phase 0 seed data created for the five roles, initial Policy v1, and six fixed DamageTypes; ExtraFeeType schema/checks exist without seeded rows.
- [ ] Time-based jobs configured outside triggers.
- [ ] DB roles/permissions prevent unauthorized direct lifecycle updates where applicable.
- [ ] Integration tests cover every valid and invalid state transition, Pg013 concurrency/immutability, Pg010 timeline RBAC and Pg012 media projection; existing SQL Server-only tests excluded from compilation do not count as PostgreSQL passes.

---

## 34. Minimum Database Integration Test Matrix

These tests are required to validate the physical database behavior, not only application logic.

| Test ID | Scenario | Expected result |
|---|---|---|
| `DBT-01` | Two concurrent Reservations compete for final capacity. | Exactly one succeeds if only one capacity remains. |
| `DBT-02` | Create Reservation on INACTIVE Facility. | Rejected. |
| `DBT-03` | Confirm Reservation without paid Deposit. | Rejected. |
| `DBT-04` | Create two RESERVATION Visits for one confirmed Reservation. | Second rejected/idempotently returns existing. |
| `DBT-05` | Cancel Visit after CHECKED_IN. | Rejected. |
| `DBT-06` | Two handovers allocate same StorageUnit concurrently. | Exactly one can set it IN_USE. |
| `DBT-07` | Contract selects Discount owned by another Customer. | Rejected. |
| `DBT-08` | Create two Rental invoices for same Contract/BillingMonth. | Second rejected by unique index. |
| `DBT-09` | Same verified payOS signed webhook arrives twice. | No duplicate financial/lifecycle effects; stable first `PaidAt`. |
| `DBT-10` | Two Staff claim same Inspection. | Exactly one succeeds. |
| `DBT-11` | Renewal and new Reservation compete for last future capacity. | Invariant preserved; no overbooking. |
| `DBT-12` | Reservation captures Policy V1; Policy V2 activates before handover. | Contract created at handover inherits Reservation.PolicyId = V1, not active V2. |
| `DBT-13` | Early Return inside configured waive window with unpaid current-month invoice. | Invoice handled per waive rule; no LateFee for that month. |
| `DBT-14` | Normal Return with Late + Extra + approved Damage. | Settlement formula correct. |
| `DBT-15` | Early Return with deductions. | RefundAmount remains 0. |
| `DBT-16` | Repeat `usp_FinalizeReturn`. | No duplicate DepositSettlement or terminal-state corruption. |
| `DBT-17` | Attempt UPDATE/DELETE on ContractExtension/AuditLog/LoginHistory. | Rejected. |
| `DBT-18` | Notification send failure. | Row remains PENDING. |
| `DBT-19` | External recovery with Inspection.VisitId NULL. | Allowed if Contract/StorageUnit valid. |
| `DBT-20` | RETURN Visit with ActualReturnDate then repeat confirmation. | No duplicate current return effect; return classification stable. |
| `DBT-21` | Same-Facility Manager decides PENDING Damage. | Exactly one valid terminal APPROVED/REJECTED state is stored and audited. |
| `DBT-22` | Two concurrent Manager decisions target the same PENDING DamageRecord. | Exactly one terminal decision wins; the other is rejected/idempotently controlled. |
| `DBT-23` | Finalize return with REJECTED Damage. | REJECTED Damage contributes zero; PENDING Damage blocks finalization. |
| `DBT-24` | Contract has Discount at offline first-month handover. | No first-month Invoice/Payment; amount equals Reservation.LockedRentalPrice; next eligible invoice may apply captured Discount. |
| `DBT-25` | Employee initial credential email fails. | Account remains INACTIVE; no plaintext password is persisted or logged. |


| `DBT-V11-01` | Concurrent Manager assignments for the same Unit from different Reservations | Exactly one ACTIVE assignment; loser returns controlled conflict, no duplicate occupancy. |
| `DBT-V11-02` | Manager reassigns Unit and then releases | Old row REPLACED, new row RELEASED; all terminal history immutable, ClosedAt non-null. |
| `DBT-V11-03` | Staff tries assignment mutation, Manager targets another Facility | 403 / controlled business error, no persistent write. |
| `DBT-V11-04` | Complete Handover without ACTIVE assignment or with mismatched unit | Rollback entire Contract/Reservation/Unit/Visit transaction, no consumed history. |
| `DBT-V11-05` | Correct Manager assignment + Staff handover | Single Contract and CONSUMED assignment in same transaction; one Unit IN_USE. |
| `DBT-V11-06` | StorageUnit timeline for Manager/BOM/Admin | Manager own Facility; BOM/Admin system read-only; no Customer/private financial leak; unknown pre-Pg010 history not fabricated. |
| `DBT-V11-07` | Catalog UnitType media URL nullable fields | public `imageUrl`/`videoUrl` exact JSON type and null serialization; no unapproved media mutation. |
| `DBT-V11-08` | Customer optional Address | AUTH-001 optional; AUTH-005 update/clear/omit semantics; no address in unrelated directory/Guest projection. |
| `DBT-V11-09` | Own Customer CCCD profile read | AUTH-004 private profile.cccd string/null, not Employee/public/cross-account; serialization source exists at 2945788, runtime/DB verification remains pending. |
| `DBT-V11-10` | Admin deactivation eligibility scaffold | Route returns HTTP 501 `ENDPOINT_NOT_IMPLEMENTED`; ADM-004 remains independently guarded. |
| `DBT-V11-11` | PostgreSQL Pg001..Pg013 chain | Empty install/redeploy/duplicate update/model parity and protected history; no guessed backfill. |
| `DBT-V11-12` | Offline first-month and payOS later-month | No first-month Invoice/Payment, payOS Deposit/later RENTAL_FEE only, key uniqueness and immutable PaidAt. |
| `DBT-CCCD-001` | OD-09 rejected new Reservation and legacy-data preservation | Missing/blank/invalid CCCD -> 400 errors.cccd, no Reservation/hold/Invoice writes; nullable/unique varchar(20) storage remains; new input follows OD-10; no LegalName/backfill/history changes. |
| `DBT-CCCD-003` | OD-10 exact-format and legacy compatibility | Validate 12 ASCII digits at the atomic new-Reservation boundary; reject invalid saved values without writes, preserve legacy rows, leading zeroes, uniqueness and PATCH omission/clear semantics; BE/DB tests pending. |
| `DBT-CCCD-002` | Concurrent profile clear/update and new RES-001 | The authoritative creation boundary cannot be bypassed by stale profile checks; either accepted creation with the precondition satisfied or controlled rejection, no partial creation; capacity concurrency remains protected. |

These OD-09 cases are required future test evidence, not executed results. API-CCCD-001..003, SEC-CCCD-001, REG-CONT-001 and E2E-F01 are defined in SRS §14.15.3; no test source was added or changed by this documentation task.

---

## 35. Historical V2.1 Change Log (Preserved)

- Established 28 persisted entities (27 historical base + one approved V11 extension).
- Replaced standalone MonthlyRentalFee with Invoice.
- Confirmed `Customer 1:N Discount` and `Contract 0..1 Discount`.
- Removed Reservation.DepositStatus.
- Restored/confirmed Visit.ActualReturnDate for Return classification.
- Removed Inspection.NextUnitStatus.
- Removed LateFee/ExtraFee approval status.
- Added ExtraFeeType master values.
- Defined Policy-driven Stored Procedure behavior.
- Defined lifecycle-oriented Stored Procedures, integrity Triggers and scheduled jobs.
- Added SQL Server physical data-type standards, enum/default/nullability dictionaries, business-rule matrix and detailed state-transition matrix.
- Added calculation rules, authorization ownership guidance, FK delete strategy, transaction/concurrency/idempotency rules and stable procedure error codes.
- Added concrete index strategy, audit-event dictionary, scheduled-job dictionary, procedure/trigger coding conventions, seed-data requirements, naming standards, DDL creation order, physical-schema checklist and database integration test matrix.
- Aligned all SRS V9 FINAL overrides: PhoneNumber uniqueness; Facility/StorageUnit defaults; preventive maintenance transition; Contract Policy inheritance; first-month no-Discount rule; referenced-Discount immutability; fixed SupportTicket categories; Facility Manager Damage decision; fixed DamageType production seed; Employee credential provisioning; LoginHistory unknown-identifier rule; Contract-to-Inspection historical cardinality; DBT-21..25.
- Aligned authority metadata with SRS V10 FINAL. The V10 backend assembly/folder revision does not change this dictionary's database entities, procedures, triggers, jobs, constraints or lifecycle semantics.

---

## 36. V11 Source and Authority

**Owner-approved semantic source:** [`FRMS_SRS_V11.md`](FRMS_SRS_V11.md) §§0.0.9, 9, 12, 17, 27. The rest of V11 still awaits ratification; historically approved V10 business rules remain binding where not expressly superseded.

**API wire source:** [`FRMS_API_V11.md`](FRMS_API_V11.md) §§13.11–13.27 (core) and §13A (observed/provisional/owner-approved deltas). No API contract is silently implied by an EF Entity.

**Data source:** This V11 Data Dictionary, with implemented PostgreSQL EF models/migration scripts as current-code evidence for exact physical names/types. The uploaded V2.1 and GitHub `main` V2.1 remain historical inputs; this is not a certified byte-level main rebase. Source priority is approved SRS > approved API wire / approved dictionary persistence rules > Scope V8 > implementation, within each domain.

**Decisions explicitly in force:** OD-01 optional address, OD-02 handover assignment (28th entity), OD-03 Administrator-only timeline read expansion, OD-04 501 deactivation-eligibility, OD-05 public media response fields. Other read-only proposals need approval. PostgreSQL architecture formally awaits full V11 ratification even though runtime already uses Npgsql.

**Handoff:** Required `FRMS_IMPLEMENTATION_HANDOFF.md` is unavailable in accessible `main`/project files; no active implementation feature change or test status is inferred. GitHub is read-only in this task.

---

## 37. V11 Synchronization & Release Evidence Matrix

| Concern | SRS | API Specification | Data/DB source | Status |
|---|---|---|---|---|
| Customer optional Address | OD-01, §9.6 | AUTH-001/005 | Customer.Address nullable; length <=1000 | APPROVED; API/PII tests PENDING |
| Own Customer CCCD | OD-09, §9.6 / §10 | AUTH-004 | Customer.CCCD nullable varchar(20)/UNIQUE storage; OD-10 exact-12-digit validation | APPROVED; owner-only serialization source at 2945788; runtime/Swagger acceptance PENDING |
| CCCD before new Reservation / safe continuation | OD-09, BR-RES-09 / BR-PUB-05 / §14.15 | AUTH-001/002/004/005 + RES-001 | Existing Customer.CCCD + authoritative creation routine; no new field/table | TEAM APPROVED; new guard/continuation implementation + API/SEC/DBT/REG/E2E evidence PENDING (V11-G14) |
| Manager handover assignment | OD-02, §9.2A | §13A.1 IMPL-HOA-01..03 | §4.28, Pg013, SQL routines/trigger | APPROVED; DB/E2E NOT VERIFIED |
| Administrator timeline | OD-03 | §13A.2 IMPL-UNT-01..03 | Pg010 AuditLog; no backfill | APPROVED read-only; SEC tests PENDING |
| Admin deactivation eligibility | OD-04 | §13A.4 | No new schema | NOT IMPLEMENTED HTTP 501 |
| Public media URLs | OD-05 | CAT-003/004/005 §13A.5 | UnitType ImageUrl/VideoUrl Pg012 | CODE OBSERVED; OWNER ACKNOWLEDGED |
| PostgreSQL provider | §7, §11A, §27 | HTTP unchanged | Pg001..Pg013, EF Npgsql, PL/pgSQL | CODE OBSERVED; architecture approval PENDING |
| Payments | §0.0.3–0.0.5/§12 | PAY-001/003/004 | Invoice/Payment/PaidAt/idempotency | V10 approved; V11 regression NOT RUN |
| First month | §0.0, §12 | OPS-004; PAY-002 retired | Contract+Reservation, no first-month Invoice | V10 approved; PG regression NOT RUN |
| Customer activity and supplemental directories | §11A candidates | §13A.3/13A.4 | Existing tables, no new entity | PROPOSED (except OD-approved items) |
| Recurring BackgroundJobs (6 tasks / 4 services) | §9.11 / V11-G10..G13 | §13A.10 (no new HTTP endpoints) | §§9/27; Pg007 PostgreSQL routines | REGISTERED IN CODE `0219f307`; Billing default ON, NoShow/Notifications OFF; runtime tests NOT VERIFIED |

**Review gates:** `V11-G01..G09` in SRS. Need full main SRS diff, generated Swagger parity, FE contract, PostgreSQL migration/catalogue verification, authorization/PII negative tests, NUnit/Postman/Playwright, owner full-document approval. No release acceptance is claimed here.

**2026-10-25 OD-09 synchronization:** REV-2026-10-25-CUSTOMER-CCCD-CONTINUATION is team-approved (owner confirmed). Update Customer.CCCD semantics, creation precondition/routine responsibility, race/error mappings and acceptance traceability without changing the 28-entity/202-field schema. Reuse existing 400 VALIDATION_ERROR + errors.cccd. Replace the earlier static AUTH-004 gap with source-observed serialization at 2945788, not a deployment/test claim. SRS V11-G14 separately gates the new reservation/continuation behavior. This task edits only the three specifications, not application code, migrations, database data or test files.

## 38. V11 Change Log — 2026-10-20

- DDV11-01: Rebase document authority/links to SRS V11 and separate API Spec; preserve historical provenance and review status.
- DDV11-02: Replace active SQL Server data types/transaction/trigger/routine language with PostgreSQL Npgsql/PLpgSQL mapping; leave historical SQL Server files archived.
- DDV11-03: Align first month offline + payOS-only new invoice-backed Payment, UTC PaidAt and globally unique UUID IdempotencyKey.
- DDV11-04: Add owner-approved 28th persisted HandoverUnitAssignment, four statuses, active unique indexes, immutable history and atomic consumption.
- DDV11-05: Extend relationship, lifecycle, routine, trigger and DBT matrices for Manager assignment and checked-in Visit precondition.
- DDV11-06: Record Owner-approved optional Customer.Address and preserve private nullable Customer.CCCD rule.
- DDV11-07: Align Pg012 UnitType ImageUrl/VideoUrl nullable public projection and Pg010 StorageUnit timeline based on observed AuditLog.
- DDV11-08: Record owner-approved SYSTEM_ADMINISTRATOR timeline read-only exception; no general operational permission.
- DDV11-09: Record Admin deactivation-eligibility HTTP 501 as API implementation status, without creating a persistence workflow.
- DDV11-10: Record Pg001..Pg013 inventory and PostgreSQL tests explicitly NOT RUN in this documentation update.
- DDV11-11: Clarify Complete Inspection vs Finalize Return Unit release boundary to match approved SRS.
- DDV11-12: Add three-document source map, pending architecture/Swagger/DB acceptance gates and no GitHub writes.


## 39. V11 Change Log — 2026-10-21 Recurring BackgroundJobs

- `DDV11-13`: Reconciled newest `main` `0219f30734b0` with `ExpirePendingReservationsJob`, `BillingMaintenanceJob`, `ProcessReservationNoShowJob`, `RetryNotificationsJob`: **four hosted services, six logical tasks**.
- `DDV11-14`: Recorded startup-relative 10-minute / 24-hour / 5-minute intervals, ordered billing stages, `Billing=true`, `NoShow=false`, `Notifications=false` code defaults and associated environment override names.
- `DDV11-15`: Clarified current-month-only Invoice generation, subsequent-month/Discount/extension-price/Policy invariants, idempotent LateFee insert/update, and missing prior-month catch-up acceptance decision.
- `DDV11-16`: Corrected `usp_RetryPendingNotification` semantics: SQL dequeues/selects only; sender delivery and `usp_MarkNotificationSent` are separate. Current provider adapter unconfigured; PENDING is retained on failure.
- `DDV11-17`: Added concrete DBT/operational acceptance gaps, no-show disabled gate, non-midnight scheduler timing and explicit rule that DI presence ≠ production PASS. Data Dictionary updated **without** editing GitHub, migrations, SP/triggers, API code or business data.


## 40. V11 Physical Entity/Field Parity Audit — 2026-10-21

**Code comparison:** `main` commit `0219f30734b00b3e5527e3453b19840f08f63639`, `backend/Frms.DataAccess/Persistence/Entities/Entities.cs`, both EF `FrmsDbContext` classes, `FrmsModelConfiguration`, `HandoverAssignmentModelConfiguration`, migration Pg001/Pg012/Pg013. **Scope:** 28 entity classes; **202 persisted fields**. Missing/extra field names: **0** (physical `Customer.CCCD` mapped from CLR `Customer.Cccd`). Field type and nullability descriptions normalized to code-defined PostgreSQL schema. This is a **static parity check**, not a database introspection or successful EF migration run.

- Physical `InspectionEvidence.FileData`: `bytea` (replaces historical SQL Server/MySQL binary labels).
- Physical `Visit.ActualReturnDate`: `date`, nullable, even though CLR type is `DateTime?`.
- Timestamp fields mapped with `Utc(...)`: `timestamptz(3)`; calendar and month fields: `date`.
- `NotificationLog.Content` and bounded descriptions/reasons: `varchar(1000)`; `UnitType.Size`: `varchar(150)`; public media URLs: nullable `varchar(2048)`.
- `AuditLog.OldValue/NewValue`: nullable `text` with JSON validity/check constraints; not deployed `jsonb`.
- `Payment.InvoiceId`: non-null `uuid` FK in the active current model. Historical rows remain immutable evidence; do not fabricate first-month Payment.
- `StorageUnit.UnitCode`: `varchar(50)`; the proposed unique `(FacilityId, UnitCode)` is **not** part of current EF model/Pg001. No SQL migration was approved or applied by this documentation work.
- Runtime payment adapter remains payOS only. Database CHECK may preserve historical gateway codes for legacy records, without authorizing new non-payOS payment attempts.

**Remaining deployment gates:** compare `__EFMigrationsHistory` and `information_schema`/`pg_catalog` with this mapping; generated Swagger contract parity, ownership/PII authorization tests, live PostgreSQL concurrency/rollback, startup Billing catch-up and delayed execution behavior, real notification delivery integration; approved Release-1 business owner sign-off. Until evidenced, documents are ready for repository review, **not V11 FINAL release certification**.
