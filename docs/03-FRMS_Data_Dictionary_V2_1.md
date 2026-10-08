# FRMS Data Dictionary — Version 2.1 (SRS V10 Aligned)

> REV-2026-10-09-PAYOS aligns Payment with the governing SRS. Only code/mock tests are authorized; no sandbox/staging, live calls, links, registration or transfers. Live acceptance is pending. Description FRMS; transactionDateTime UTC+7 -> UTC; verified unknown/sample webhook HTTP200 without mutation. First month remains offline, PAY-002 retired; every current Payment is Invoice-backed. Downgrade must protect PAYOS/code evidence.

**Project:** Self-Storage Facility Rental and Management System (FRMS)
**Document:** Data Dictionary / Lifecycle / Database Automation Specification
**Version:** 2.1
**Date:** 2026-10-04
**Baseline:** `FRMS_SRS_V10.md` (highest authority), including REV-2026-10-07-FM-OFFLINE and REV-2026-10-09-PAYOS.
**Authority:** `SRS V10 FINAL > Data Dictionary V2.1 > aligned Scope > implementation detail`.
**Revision scope:** Phase 0 seed clarification plus Payment-related alignment with the approved offline-first-month and payOS code/mock revision. Unrelated domain requirements are unchanged.

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

Payment integration uses payOS for Deposit and Rental Fee invoices strictly after Contract.StartMonth. payOS has no separate sandbox/staging; this phase authorizes code/mock tests only, not production calls. First-month rent is received offline and acknowledged by Staff at handover; no first-month Invoice or Payment is created. Historical MOMO/VNPAY rows are legacy evidence, not active gateways. Late Fee, Extra Fee, Damage-related amounts, Deposit Deduction, RefundAmount and AdditionalAmountDue are recorded by the system, while actual refund/collection/compensation settlement is outside the core system.

---

## 2. Design Baseline and Confirmed Overrides

This document integrates the confirmed ERD baseline with all explicit SRS V9 FINAL business decisions and the V10 implementation-structure revision. Where older Data Dictionary V1 or Scope V8 wording conflicts with SRS V10 FINAL, the SRS rule is authoritative.

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
| O11 | `InspectionEvidence.FileData` stores binary data (`VARBINARY(MAX)` / equivalent BLOB), not FileUrl. | Evidence is stored in-database in Release 1. |
| O12 | `LateFee` and `ExtraFee` have no approval Status. | They are recorded/applied at return according to Contract/return state; they do not have an independent approval lifecycle. |
| O13 | `NotificationLog` has only `PENDING` and `SENT`. | Send failure leaves the record `PENDING` for retry. |
| O14 | Policy is expanded into configurable operational parameters. | Stored procedures read Policy values instead of hard-coding business numbers. |
| O15 | `FacilityAssignment`, `Booking`, `ReturnProcess`, `Renewal`, `Refund`, `DamageFee`, `DailyTask`, `Report`, `DiscountRedemption`, separate `Maintenance`, and separate `MonthlyRentalFee` are not tables. | Their behavior/data is represented by existing entities or derived queries. |
| O16 | `UserAccount.PhoneNumber` is UNIQUE in addition to UNIQUE Email. | Customer login identifier is PhoneNumber; SRS V9 explicit data override. |
| O17 | `Contract.PolicyId = Reservation.PolicyId` at Complete Handover. | Contract must inherit the Reservation-captured Policy; it must not capture the currently active Policy at handover. |
| O18 | First-month rent is received offline without Contract Discount or an Invoice/Payment. | Amount equals `Reservation.LockedRentalPrice`; Contract creation records Staff acknowledgment. Captured Contract Discount may apply to invoices strictly after Contract.StartMonth. |
| O19 | New Facility starts `INACTIVE`; new StorageUnit starts `AVAILABLE`. | SRS V9 resolves both initial-status decisions. |
| O20 | StorageUnit allows preventive-maintenance transition `AVAILABLE -> MAINTENANCE`. | No separate Maintenance entity/workflow is introduced. |
| O21 | SupportTicket Category is a fixed Release 1 enum and includes `OTHER`. | SRS V9 locks the catalogue. |
| O22 | Damage decision is `PENDING -> APPROVED/REJECTED` by same-Facility `FACILITY_MANAGER`; return finalization is blocked while any DamageRecord is PENDING. | SRS V9 locks approval actor/lifecycle. |
| O23 | A Discount referenced by any Contract has immutable `CustomerId`, `Percentage`, `EffectiveFrom`, and `EffectiveTo`. | Inactivation blocks new selection only; existing Contract usage remains valid. |
| O24 | Production DamageType is fixed seed data: `LOCK_DAMAGE`, `DOOR_DAMAGE`, `WALL_DAMAGE`, `FLOOR_DAMAGE`, `WATER_DAMAGE`, `OTHER`; no Release 1 DamageType CRUD. | SRS V9 locks production provisioning. |
| O25 | `Contract 1:N Inspection` historically. | A Contract may have historical inspection records; each Inspection still belongs to one Contract. |
| O26 | Employee creation provisions a random 16-character initial password by email; account remains INACTIVE until email send is accepted. | Plaintext is never persisted/logged; retry generates a new credential. |

---

## 3. Core Design Rationale

### 3.1 Month-based rental period

Reservation and Contract use `StartMonth` / `EndMonth`. This simplifies monthly capacity checking and monthly billing while avoiding arbitrary date-overlap complexity.

### 3.2 Reservation holds capacity, not a concrete unit

Reservation does not store `StorageUnitId`. Capacity is held by Facility, UnitType and month range. An `AVAILABLE` StorageUnit is selected only during check-in/handover.

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

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `RoleId` | UUID | No | PK | Role identifier. |
| `RoleName` | VARCHAR | No | UNIQUE | One of the five fixed role names. |
| `Description` | VARCHAR/TEXT | Yes |  | Optional role description. |

Allowed `RoleName`: `CUSTOMER`, `FACILITY_STAFF`, `FACILITY_MANAGER`, `BUSINESS_OPERATIONS_MANAGER`, `SYSTEM_ADMINISTRATOR`.

### 4.2 `UserAccount`

**Purpose:** Authentication/account state shared by Customer and Employee profiles.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `UserAccountId` | UUID | No | PK | Account identifier. |
| `RoleId` | UUID | No | FK -> UserRole | Assigned role. |
| `Email` | VARCHAR | No | UNIQUE | Login/contact email. |
| `PhoneNumber` | VARCHAR | No | UNIQUE | Customer login identifier; globally unique. |
| `PasswordHash` | VARCHAR | No |  | Password hash. |
| `Status` | ENUM/VARCHAR | No | CHECK | `ACTIVE`, `INACTIVE`. |
| `EmailVerifiedAt` | DATETIME | Yes |  | Reserved verification timestamp; Release 1 does not implement email verification and this field is not a login precondition. |
| `CreatedAt` | DATETIME | No |  | Account creation time. |

Rules: an inactive account cannot log in or initiate new actions. Historical business records remain intact. Customer login uses unique `PhoneNumber`; Employee login uses unique `Email`. Login identifiers are not changeable in Release 1. Password validation is 8..64 characters, no mandatory composition classes, encoded BCrypt input <= 72 bytes; only `PasswordHash` is persisted.

### 4.3 `Customer`

**Purpose:** Customer business profile.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `CustomerId` | UUID | No | PK | Customer identifier. |
| `UserAccountId` | UUID | No | FK, UNIQUE -> UserAccount | One profile per account. |
| `FullName` | VARCHAR | No |  | Customer name. |
| `Address` | VARCHAR/TEXT | Yes |  | Customer address. |
| `CCCD` | VARCHAR | Yes | UNIQUE | Citizen ID; nullable. |

Rule: Customer has no `DiscountId`; relation is `Customer 1:N Discount`.

### 4.4 `Employee`

**Purpose:** Employee business profile.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `EmployeeId` | UUID | No | PK | Employee identifier. |
| `UserAccountId` | UUID | No | FK, UNIQUE -> UserAccount | One employee profile per account. |
| `FacilityId` | UUID | Yes | FK -> Facility | Required for Facility Staff/Manager; null for global roles. |
| `FullName` | VARCHAR | No |  | Employee name. |

Rules: `FACILITY_STAFF` and `FACILITY_MANAGER` must have exactly one Facility; Business Operations Manager and System Administrator have no required Facility assignment. Administrator-created Employee accounts start `INACTIVE`; FRMS generates a random 16-character initial password, persists only its BCrypt hash, emails the plaintext credential through `IEmailService`, and activates the account only after the email send is accepted. Retry provisioning generates a new password/hash. Password reset/change is outside Release 1.

### 4.5 `Facility`

**Purpose:** Storage service location.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `FacilityId` | UUID | No | PK | Facility identifier. |
| `Name` | VARCHAR | No |  | Facility name. |
| `Address` | VARCHAR/TEXT | No |  | Facility address. |
| `ContactInfo` | VARCHAR/TEXT | Yes |  | Contact information. |
| `Description` | TEXT | Yes |  | Facility description. |
| `Status` | ENUM/VARCHAR | No | CHECK | `ACTIVE`, `INACTIVE`. |

Rules: a newly created Facility starts `INACTIVE`. An inactive Facility cannot accept new Reservations; existing confirmed lifecycles may continue. Activation is explicit.

### 4.6 `UnitType`

**Purpose:** Global storage type master and current rental price.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `UnitTypeId` | UUID | No | PK | Unit type identifier. |
| `Name` | VARCHAR | No |  | Type name. |
| `Mode` | ENUM/VARCHAR | No | CHECK | `PUBLIC`, `PRIVATE`. |
| `Size` | VARCHAR/DECIMAL structured as implemented | No |  | Physical size/specification. |
| `RentalPrice` | DECIMAL | No | CHECK >= 0 | Current monthly rental price. |
| `Description` | TEXT | Yes |  | Additional characteristics. |

Rule: price is global in V1; no Facility-specific price table.

### 4.7 `StorageUnit`

**Purpose:** Physical storage position/unit.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `StorageUnitId` | UUID | No | PK | Physical unit identifier. |
| `FacilityId` | UUID | No | FK -> Facility | Owning Facility. |
| `UnitTypeId` | UUID | No | FK -> UnitType | Unit type. |
| `UnitCode` | VARCHAR | No | UNIQUE within Facility recommended | Operational unit code. |
| `LocationInfo` | VARCHAR/TEXT | Yes |  | Physical location description. |
| `Status` | ENUM/VARCHAR | No | CHECK | `AVAILABLE`, `IN_USE`, `INSPECTION`, `MAINTENANCE`. |

Legal transitions: `AVAILABLE -> IN_USE`, `AVAILABLE -> MAINTENANCE`, `IN_USE -> INSPECTION`, `INSPECTION -> AVAILABLE`, `INSPECTION -> MAINTENANCE`, `MAINTENANCE -> AVAILABLE`. A newly created StorageUnit starts `AVAILABLE`. Direct `AVAILABLE -> MAINTENANCE` is preventive maintenance by the same-Facility Manager and requires no ACTIVE Contract. UnitType reassignment is rejected while the unit is `IN_USE`, `INSPECTION`, or otherwise occupied by an ACTIVE Contract.

### 4.8 `Reservation`

**Purpose:** Future capacity hold for one Customer, Facility, UnitType and month range.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `ReservationId` | UUID | No | PK | Reservation identifier. |
| `CustomerId` | UUID | No | FK -> Customer | Owner. |
| `FacilityId` | UUID | No | FK -> Facility | Target Facility. |
| `UnitTypeId` | UUID | No | FK -> UnitType | Requested type. |
| `PolicyId` | UUID | No | FK -> Policy | Policy version captured at reservation creation. |
| `StartMonth` | DATE | No |  | Rental start month; stored as canonical month date. |
| `EndMonth` | DATE | No | CHECK >= StartMonth | Rental end month. |
| `LockedRentalPrice` | DECIMAL | No | CHECK >= 0 | Initial rental price snapshot. |
| `DepositAmount` | DECIMAL | No | CHECK >= 0 | Deposit snapshot; initially derived from UnitType price. |
| `Status` | ENUM/VARCHAR | No | CHECK | `PENDING_DEPOSIT`, `CONFIRMED`, `COMPLETED`, `CANCELLED`. |
| `CreatedAt` | DATETIME | No |  | Creation timestamp. |

No `StorageUnitId`. No `DepositStatus`.

Lifecycle: `PENDING_DEPOSIT -> CONFIRMED -> COMPLETED`; cancellation allowed from `PENDING_DEPOSIT` or `CONFIRMED` according to visit state/rules.

### 4.9 `Visit`

**Purpose:** Customer visit for reservation handover, storage access, or return.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `VisitId` | UUID | No | PK | Visit identifier. |
| `EntityId` | UUID | No | Polymorphic | ReservationId for RESERVATION; ContractId for ACCESS/RETURN. |
| `EmployeeId` | UUID | Yes | FK -> Employee | Actual Staff actor; null before handled. |
| `VisitType` | ENUM/VARCHAR | No | CHECK | `RESERVATION`, `ACCESS`, `RETURN`. |
| `VisitDate` | DATE | No |  | Scheduled visit date. |
| `ActualReturnDate` | DATE/DATETIME | Yes |  | Set only for RETURN when physical return is confirmed. |
| `Status` | ENUM/VARCHAR | No | CHECK | `SCHEDULED`, `CHECKED_IN`, `CHECKED_OUT`, `CANCELLED`. |

Lifecycle: `SCHEDULED -> CHECKED_IN -> CHECKED_OUT` or `SCHEDULED -> CANCELLED`. After `CHECKED_IN`, cancellation is forbidden.

### 4.10 `Contract`

**Purpose:** Active/finished rental agreement and actual unit occupancy source.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `ContractId` | UUID | No | PK | Contract identifier. |
| `ReservationId` | UUID | No | FK, UNIQUE -> Reservation | One Reservation creates at most one Contract. |
| `CustomerId` | UUID | No | FK -> Customer | Contract owner. |
| `FacilityId` | UUID | No | FK -> Facility | Facility. |
| `StorageUnitId` | UUID | No | FK -> StorageUnit | Assigned physical unit. |
| `PolicyId` | UUID | No | FK -> Policy | Inherited from `Reservation.PolicyId` at Complete Handover; Contract-level lifecycle keeps this version. |
| `DiscountId` | UUID | Yes | FK -> Discount | Zero or one Discount selected for this Contract. |
| `StartMonth` | DATE | No |  | Contract start month. |
| `EndMonth` | DATE | No | CHECK >= StartMonth | Current effective end month. |
| `Status` | ENUM/VARCHAR | No | CHECK | `ACTIVE`, `COMPLETED`, `TERMINATED`. |

Rules: selected Discount must belong to the same Customer and be valid when applied. One StorageUnit may have many historical Contracts but at most one `ACTIVE` Contract at a time.

### 4.11 `ContractExtension`

**Purpose:** Append-only history of successful Contract renewals.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `ContractExtensionId` | UUID | No | PK | Extension identifier. |
| `ContractId` | UUID | No | FK -> Contract | Extended Contract. |
| `OldEndMonth` | DATE | No |  | End month before renewal. |
| `NewEndMonth` | DATE | No | CHECK > OldEndMonth | New end month. |
| `AppliedMonthlyPrice` | DECIMAL | No | CHECK >= 0 | Price snapshot for extension period. |
| `CreatedAt` | DATETIME | No |  | Renewal completion time. |

No status. UPDATE/DELETE should be blocked.

### 4.12 `Invoice`

**Purpose:** Billing record for Deposit and Rental Fee.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `InvoiceId` | UUID | No | PK | Invoice identifier. |
| `EntityId` | UUID | No | Polymorphic | ReservationId for DEPOSIT; ContractId for RENTAL_FEE. |
| `InvoiceType` | ENUM/VARCHAR | No | CHECK | `DEPOSIT`, `RENTAL_FEE`. |
| `BillingMonth` | DATE | Yes |  | Required for RENTAL_FEE; null for DEPOSIT. |
| `BaseAmount` | DECIMAL | No | CHECK >= 0 | Amount before discount. |
| `DiscountId` | UUID | Yes | FK -> Discount | Snapshot reference for rental invoice; null for Deposit. |
| `DiscountAmount` | DECIMAL | No | DEFAULT 0 | Snapshot discount amount. |
| `AmountDue` | DECIMAL | No | CHECK >= 0 | Final amount due. |
| `DueDate` | DATE/DATETIME | No |  | Due date/deadline. |
| `Status` | ENUM/VARCHAR | No | CHECK | `UNPAID`, `PAID`, `OVERDUE`, `CANCELLED`. |
| `PaidAt` | DATETIME | Yes | UTC timestamp | Verified provider time on first UNPAID/OVERDUE -> PAID transition; retain on duplicate, later successful attempt or cancellation. Historical missing evidence stays null. |
| `CreatedAt` | DATETIME | No |  | Creation timestamp. |

Rules: Deposit cannot use Discount. Handover creates no first-month Invoice/Payment; first-month offline rent equals `Reservation.LockedRentalPrice` without Discount. RENTAL_FEE requires `BillingMonth > Contract.StartMonth`; eligible later invoices snapshot the captured Contract Discount. Preserve historical invoices without recreating or double-counting first-month charges.

### 4.13 `Payment`

**Purpose:** Invoice-backed Payment attempt/result through payOS. Only code/mock tests are currently authorized; existing MOMO/VNPAY rows remain legacy history.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `PaymentId` | UUID | No | PK | Payment identifier. |
| `InvoiceId` | UUID | No for current-model rows | FK -> Invoice | Every new Payment references an existing eligible Invoice. No first-month Payment. |
| `Amount` | DECIMAL | No | CHECK > 0 | Payment amount. |
| `PaymentMethod` | VARCHAR/ENUM | No | CHECK | New attempts: `PAYOS`; preserve historical `MOMO` and `VNPAY` only as legacy. |
| `TransactionCode` | VARCHAR | Yes | Filtered UNIQUE when non-null | Successful payOS webhook `reference`; not paymentLinkId or orderCode. Legacy gateway references are preserved. |
| `Status` | ENUM/VARCHAR | No | CHECK | `PENDING`, `SUCCESS`, `FAILED`. |
| `PaidAt` | DATETIME | Yes, conditional | Required only for SUCCESS | Verified provider success time normalized to UTC. |
| `CreatedAt` | DATETIME | No |  | Creation time in UTC. |
| `ProviderOrderCode` | BIGINT | Yes | Filtered UNIQUE when non-null | Server sequence dbo.ProviderOrderCodeSequence starts 1000, increment 1, NO CYCLE; allocated atomically for PAYOS attempts. Legacy MOMO/VNPAY rows remain null. |
| `IdempotencyKey` | UNIQUEIDENTIFIER | No | Globally UNIQUE | UUID identifying one deliberate Customer payment action; HTTP retries reuse the stored attempt. |
| `PaymentUrl` | NVARCHAR(2048) | Yes | Bounded session value | Persist the checkoutUrl for the same authorized attempt; never expose through PAY-003 or logs. |
| `PaymentUrlExpiresAt` | DATETIME | Yes | UTC timestamp | Provider response expiry or explicitly requested server expiry, persisted UTC; an expired attempt requires a new action/key. |

The superseded pre-handover null-InvoiceId model is not used for new payments. If unexpected historical null rows exist, preserve them pending the migration/deployment review required by SRS §0.0/§9.6; never fabricate Invoice links or delete historical evidence.

PAY-001 is `POST /api/v1/invoices/{invoiceId}/payments/payos` for Deposit or Rental Fee after the first month. PAY-002 remains retired. PAY-003 is the authorized Payment-detail read. PAY-004 is signature-verified payOS `POST /api/v1/payments/payos/webhook`; browser return/cancel never change financial state. `usp_ApplyPaymentResult` applies normalized verified webhook results atomically and idempotently.

### 4.14 `LateFee`

**Purpose:** Late fee associated with an overdue Rental Fee invoice.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `LateFeeId` | UUID | No | PK | Late fee identifier. |
| `InvoiceId` | UUID | No | FK, UNIQUE -> Invoice | Overdue rental invoice. |
| `OverdueDays` | INT | No | CHECK >= 1 | Number of overdue days. |
| `Amount` | DECIMAL | No | CHECK >= 0 | Calculated late fee. |
| `CalculatedAt` | DATETIME | No |  | Last calculation time. |

No separate status. Applicability is decided during return/settlement according to Contract and return state.

### 4.15 `Discount`

**Purpose:** Customer-owned percentage discount definition.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `DiscountId` | UUID | No | PK | Discount identifier. |
| `CustomerId` | UUID | No | FK -> Customer | Owning Customer. |
| `Name` | VARCHAR | No |  | Discount name. |
| `Percentage` | DECIMAL | No | CHECK 0..100 | Discount percentage. |
| `Status` | ENUM/VARCHAR | No | CHECK | `ACTIVE`, `INACTIVE`. |
| `EffectiveFrom` | DATE/DATETIME | No |  | Start of validity. |
| `EffectiveTo` | DATE/DATETIME | Yes |  | End of validity. |

Confirmed cardinality: `Customer 1:N Discount`. Each Contract uses zero or one Discount. No single-use constraint is imposed. Once a Discount is referenced by any Contract, `CustomerId`, `Percentage`, `EffectiveFrom`, and `EffectiveTo` are immutable. Setting `Status = INACTIVE` prevents new Contract selection but does not invalidate existing Contracts; create a new Discount row to change financial/effective semantics.

### 4.16 `Policy`

**Purpose:** Versioned configurable business parameters.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `PolicyId` | UUID | No | PK | Policy version identifier. |
| `Version` | INT | No | UNIQUE | Version number. |
| `Status` | ENUM/VARCHAR | No | CHECK | `ACTIVE`, `INACTIVE`. |
| `EffectiveFrom` | DATETIME | No |  | Start of effectiveness. |
| `EffectiveTo` | DATETIME | Yes |  | End of effectiveness. |
| `DepositTimeoutHours` | INT | No | CHECK > 0 | Deposit payment timeout. |
| `ReservationVisitStartDay` | INT | No | CHECK 1..31 | First allowed handover visit day. |
| `ReservationVisitEndDay` | INT | No | CHECK 1..31 | Last allowed handover visit day. |
| `MonthlyPaymentDueDay` | INT | No | CHECK 1..31 | Monthly payment due-day boundary. |
| `OverdueStartDay` | INT | No | CHECK 1..31 | Day unpaid rental invoice becomes overdue. |
| `LateFeeDivisorDays` | INT | No | CHECK > 0 | Divisor used in late fee formula. |
| `EarlyReturnWaiveFeeUntilDay` | INT | No | CHECK 1..31 | Early-return day cutoff for waiving unpaid current-month rental fee. |
| `CreatedAt` | DATETIME | No |  | Version creation time. |

Versioning rule: never overwrite business values of an existing version; create a new active row and inactivate the previous version.

### 4.17 `SupportTicket`

**Purpose:** Track Contract-related support issues and Staff responsibility.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `SupportTicketId` | UUID | No | PK | Ticket identifier. |
| `ContractId` | UUID | No | FK -> Contract | Related Contract. |
| `CustomerId` | UUID | No | FK -> Customer | Requesting Customer. |
| `AssignedEmployeeId` | UUID | Yes | FK -> Employee | Assigned Facility Staff. |
| `Category` | ENUM/VARCHAR | No | CHECK | Issue category. |
| `Description` | TEXT | No |  | Issue details. |
| `Status` | ENUM/VARCHAR | No | CHECK | `OPEN`, `IN_PROGRESS`, `COMPLETED`, `CANCELLED`. |
| `ResultNote` | TEXT | Yes |  | Resolution/result note. |
| `CreatedAt` | DATETIME | No |  | Creation time. |
| `CompletedAt` | DATETIME | Yes |  | Completion time. |

Fixed Release 1 categories: `UNIT_ISSUE`, `LOCK_KEY_ISSUE`, `ACCESS_CARD_CODE_ISSUE`, `PAYMENT_ISSUE`, `STORED_ITEM_ISSUE`, `DAMAGE_ISSUE`, `OTHER`. If `Category = OTHER`, `Description` must contain sufficient issue detail. The catalogue is not a configurable master table in Release 1.

### 4.18 `Inspection`

**Purpose:** Official inspection record after actual return or external recovery.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `InspectionId` | UUID | No | PK | Inspection identifier. |
| `ContractId` | UUID | No | FK -> Contract | Contract being returned/recovered. |
| `StorageUnitId` | UUID | No | FK -> StorageUnit | Unit being inspected. |
| `VisitId` | UUID | Yes | FK -> Visit | RETURN Visit; null for external recovery. |
| `EmployeeId` | UUID | Yes | FK -> Employee | Staff who claims/performs inspection. |
| `Status` | ENUM/VARCHAR | No | CHECK | `PENDING`, `IN_PROGRESS`, `COMPLETED`. |
| `ConditionNote` | TEXT | Yes |  | Condition findings. |
| `CompletedAt` | DATETIME | Yes |  | Completion time. |

No `NextUnitStatus` in Release 1.

### 4.19 `DamageType`

**Purpose:** Master list of damage categories and optional default amount.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `DamageTypeId` | UUID | No | PK | Damage type identifier. |
| `Name` | VARCHAR | No | UNIQUE | Damage type name. |
| `DefaultAmount` | DECIMAL | Yes | CHECK >= 0 | Suggested/default amount. |
| `Status` | ENUM/VARCHAR | No | CHECK | `ACTIVE`, `INACTIVE`. |

Release 1 production rows are fixed seed data: `LOCK_DAMAGE`, `DOOR_DAMAGE`, `WALL_DAMAGE`, `FLOOR_DAMAGE`, `WATER_DAMAGE`, `OTHER`. All start `ACTIVE`; `DefaultAmount` is `NULL` unless approved deployment data provides a value. Release 1 has no DamageType CRUD workflow.

### 4.20 `DamageRecord`

**Purpose:** Damage identified during Inspection.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `DamageRecordId` | UUID | No | PK | Damage record identifier. |
| `InspectionId` | UUID | No | FK -> Inspection | Source Inspection. |
| `DamageTypeId` | UUID | No | FK -> DamageType | Damage category. |
| `DamageAmount` | DECIMAL | No | CHECK >= 0 | Actual recorded damage amount. |
| `Note` | TEXT | Yes |  | Damage details. |
| `Status` | ENUM/VARCHAR | No | CHECK | `PENDING`, `APPROVED`, `REJECTED`. |
| `CreatedAt` | DATETIME | No |  | Record creation time. |

Facility Staff records DamageRecord as `PENDING`. Only the same-Facility Facility Manager may atomically decide `PENDING -> APPROVED` or `PENDING -> REJECTED`; both terminal states are audited. Only APPROVED `DamageAmount` is included in Deposit settlement, and return finalization is blocked while any related DamageRecord remains `PENDING`.

### 4.21 `InspectionEvidence`

**Purpose:** Binary evidence attached to an Inspection.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `InspectionEvidenceId` | UUID | No | PK | Evidence identifier. |
| `InspectionId` | UUID | No | FK -> Inspection | Parent Inspection. |
| `FileData` | VARBINARY(MAX) / LONGBLOB | No |  | Binary evidence content. |
| `EvidenceType` | ENUM/VARCHAR | No | CHECK | `IMAGE`, `VIDEO`, `DOCUMENT`. |
| `CreatedAt` | DATETIME | No |  | Upload/creation time. |

### 4.22 `ExtraFeeType`

**Purpose:** Configurable master for operational extra-fee categories.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `ExtraFeeTypeId` | UUID | No | PK | Type identifier. |
| `Name` | ENUM/VARCHAR | No | UNIQUE | Extra-fee type. |
| `DefaultAmount` | DECIMAL | No | CHECK >= 0 | Default configured amount. |
| `Status` | ENUM/VARCHAR | No | CHECK | `ACTIVE`, `INACTIVE`. |

Allowed V1 names:

- `KEY_REPLACEMENT`
- `ACCESS_CARD_REPLACEMENT`
- `LOCK_REPLACEMENT`
- `CLEANING_FEE`
- `OTHER`

### 4.23 `ExtraFee`

**Purpose:** Actual extra fee recorded from an Inspection.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `ExtraFeeId` | UUID | No | PK | Extra fee identifier. |
| `InspectionId` | UUID | No | FK -> Inspection | Source Inspection. |
| `ExtraFeeTypeId` | UUID | No | FK -> ExtraFeeType | Configured fee type. |
| `Amount` | DECIMAL | No | CHECK >= 0 | Actual amount snapshot. |
| `Reason` | TEXT | No |  | Operational reason/context. |
| `CreatedAt` | DATETIME | No |  | Creation time. |

No independent status in Release 1.

### 4.24 `DepositSettlement`

**Purpose:** Final recorded Deposit deduction/refund/additional obligation for a Contract return.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `DepositSettlementId` | UUID | No | PK | Settlement identifier. |
| `ContractId` | UUID | No | FK, UNIQUE -> Contract | One settlement per Contract. |
| `TotalDeduction` | DECIMAL | No | CHECK >= 0 | Applicable LateFee + ExtraFee + Damage amount. |
| `RefundAmount` | DECIMAL | No | CHECK >= 0 | Amount recorded as refundable. |
| `AdditionalAmountDue` | DECIMAL | No | CHECK >= 0 | Amount beyond paid Deposit. |
| `Status` | ENUM/VARCHAR | No | CHECK | `PENDING`, `FINALIZED`. |
| `CalculatedAt` | DATETIME | No |  | Calculation time. |

Normal return formula:

`RefundAmount = max(DepositPaidAmount - TotalDeduction, 0)`

`AdditionalAmountDue = max(TotalDeduction - DepositPaidAmount, 0)`

Early Return forfeits Deposit, so `RefundAmount = 0`.

### 4.25 `LoginHistory`

**Purpose:** Authentication history for administration/security review.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `LoginHistoryId` | UUID | No | PK | Log identifier. |
| `UserAccountId` | UUID | No | FK -> UserAccount | Account. |
| `LoginAt` | DATETIME | No |  | Attempt time. |
| `IpAddress` | VARCHAR | Yes |  | Source IP. |
| `DeviceInfo` | VARCHAR/TEXT | Yes |  | Browser/device info. |
| `Status` | ENUM/VARCHAR | No | CHECK | `SUCCESS`, `FAILED`. |

Append-only. A LoginHistory row is written only when the supplied login identifier resolves to a UserAccount; unknown identifiers are recorded only in technical/security logging because `UserAccountId` is non-null.

### 4.26 `AuditLog`

**Purpose:** Business/system activity audit trail.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `AuditLogId` | UUID | No | PK | Audit identifier. |
| `UserAccountId` | UUID | Yes | FK -> UserAccount | Null for system-generated action. |
| `Action` | VARCHAR | No |  | Business/system action. |
| `EntityType` | VARCHAR | No |  | Affected entity type. |
| `EntityId` | UUID | Yes |  | Affected record identifier. |
| `OldValue` | JSON/TEXT | Yes |  | Previous value snapshot. |
| `NewValue` | JSON/TEXT | Yes |  | New value snapshot. |
| `CreatedAt` | DATETIME | No |  | Action time. |

Append-only.

### 4.27 `NotificationLog`

**Purpose:** Outbound notification queue/history.

| Attribute | Type | Null | Key / Constraint | Description |
|---|---|---:|---|---|
| `NotificationLogId` | UUID | No | PK | Notification identifier. |
| `UserAccountId` | UUID | No | FK -> UserAccount | Recipient. |
| `NotificationType` | VARCHAR/ENUM | No |  | e.g. reservation/payment/visit/contract/overdue/support. |
| `Content` | TEXT | No |  | Notification content. |
| `Status` | ENUM/VARCHAR | No | CHECK | `PENDING`, `SENT`. |
| `SentAt` | DATETIME | Yes |  | Set only when successfully sent. |
| `CreatedAt` | DATETIME | No |  | Creation time. |

Failed delivery remains `PENDING` for retry.

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
| ContractExtension | Append-only; no lifecycle status. |
| LoginHistory | Append-only. |
| AuditLog | Append-only. |

### Return lifecycle

`Contract ACTIVE -> RETURN Visit -> ActualReturnDate -> StorageUnit INSPECTION -> Inspection -> fees/damage -> DepositSettlement -> StorageUnit AVAILABLE/MAINTENANCE -> Contract COMPLETED or TERMINATED`.

Normal versus Early Return is determined by `ActualReturnDate`, not scheduled `VisitDate`.

### Capacity release note

At actual return/recovery, the Contract stops holding future occupancy/capacity according to the return decision. The StorageUnit may still remain operationally unavailable in `INSPECTION` or `MAINTENANCE` until inspection/maintenance completes. Capacity planning and physical availability must therefore be treated as related but distinct checks.

---

## 7. Stored Procedure Specification

Stored procedures own multi-table business transactions and state transitions. Trigger logic should not orchestrate complete business workflows.

Recommended SQL Server naming prefix: `usp_`.

| Stored Procedure | Main responsibility | Main entities | Policy fields / notes | Atomic transaction |
|---|---|---|---|---:|
| `usp_CreateReservation` | Validate active Facility, month range, capacity; snapshot price, Deposit and active Policy; create Reservation + Deposit Invoice. | Facility, UnitType, Reservation, Policy, Invoice | `DepositTimeoutHours` used for Deposit deadline. | Yes |
| `usp_ConfirmReservation` | Validate Deposit Invoice/Payment paid; validate chosen handover date; create RESERVATION Visit; set Reservation CONFIRMED. | Reservation, Invoice, Payment, Visit, Policy | `ReservationVisitStartDay`, `ReservationVisitEndDay`. | Yes |
| `usp_CancelReservation` | Validate cancellation eligibility; cancel Reservation and SCHEDULED reservation Visit; release hold. | Reservation, Visit | Uses captured Reservation Policy if cancellation rules become configurable. | Yes |
| `usp_CheckInVisit` | Validate Visit SCHEDULED, Staff role/facility and date; set actual actor and CHECKED_IN. | Visit, Employee, Facility | No hard-coded business dates. | Yes |
| `usp_CancelVisit` | Permit only SCHEDULED -> CANCELLED and enforce type-specific restrictions. | Visit |  | Yes |
| `usp_CompleteHandover` | Validate Reservation/Visit, selected AVAILABLE StorageUnit and optional Contract Discount; authorized Staff acknowledges full offline first-month receipt; create Contract, set Unit IN_USE, Reservation COMPLETED, Visit CHECKED_OUT. No first-month Invoice/Payment. | Reservation, Visit, StorageUnit, Contract, Discount, Policy | `Contract.PolicyId = Reservation.PolicyId`. Offline amount is `Reservation.LockedRentalPrice` without Discount; captured Discount may apply to later invoices. | **Yes, mandatory** |
| `usp_CreateAccessVisit` | Create ACCESS Visit only for ACTIVE Contract; reject if active RETURN Visit exists. | Contract, Visit |  | Yes |
| `usp_CreateReturnVisit` | Create RETURN Visit for ACTIVE Contract and block contradictory Access/Renewal actions while pending. | Contract, Visit |  | Yes |
| `usp_ConfirmActualReturn` | Set `ActualReturnDate`, classify Normal/Early Return, move StorageUnit to INSPECTION, create Inspection PENDING; apply early-return current-month rule. | Contract, Visit, StorageUnit, Inspection, Invoice, LateFee, Policy | `EarlyReturnWaiveFeeUntilDay`. | **Yes, mandatory** |
| `usp_ClaimInspection` | Atomically claim PENDING Inspection and assign Employee; prevent double claim. | Inspection, Employee |  | **Yes, mandatory** |
| `usp_RecordDamage` | Validate active inspection and active DamageType; Facility Staff adds DamageRecord as `PENDING` using explicit or default amount. | Inspection, DamageType, DamageRecord | Production DamageType is fixed seed data. | Yes |
| `usp_DecideDamage` | Validate same-Facility Facility Manager and atomically decide `PENDING -> APPROVED/REJECTED`; audit terminal decision. | DamageRecord, Inspection, Contract, Employee, Facility, AuditLog | Only PENDING is decidable; concurrent decisions permit one winner. | **Yes** |
| `usp_RecordExtraFee` | Validate inspection and active ExtraFeeType; snapshot actual amount. | Inspection, ExtraFeeType, ExtraFee |  | Yes |
| `usp_AddInspectionEvidence` | Store evidence binary for Inspection. | Inspection, InspectionEvidence | Validate size/type at API and DB as practical. | Yes |
| `usp_CompleteInspection` | Validate claimed inspection; mark COMPLETED; move StorageUnit to AVAILABLE or MAINTENANCE according to operator action/business rule. | Inspection, StorageUnit | `NextUnitStatus` is not stored on Inspection. | Yes |
| `usp_FinalizeReturn` | Reject finalization while any related DamageRecord is PENDING; calculate applicable LateFee + ExtraFee + APPROVED Damage, create/finalize DepositSettlement, set Contract COMPLETED or TERMINATED. | Contract, Invoice, LateFee, ExtraFee, DamageRecord, DepositSettlement | REJECTED Damage contributes zero; Early Return => RefundAmount = 0. | **Yes, mandatory** |
| `usp_RenewContract` | Validate ACTIVE Contract, no pending RETURN Visit, contiguous period and capacity; snapshot current UnitType price; append ContractExtension; update EndMonth. | Contract, ContractExtension, StorageUnit, UnitType, Reservation | Contract continues using captured Policy for contract rules. | **Yes, mandatory** |
| `usp_CreateMonthlyInvoice` | Generate Rental Fee invoice strictly after Contract.StartMonth using the correct price source and captured Contract Discount. | Contract, Reservation, ContractExtension, Invoice, Discount | `MonthlyPaymentDueDay`; initial-period price from Reservation, extension-period price from ContractExtension. Never create a first-month Invoice. | Yes |
| `usp_ApplyPaymentResult` | Process normalized, verified payOS webhook result idempotently; update Payment and related Invoice status. | Payment, Invoice, AuditLog | Webhook is POST JSON; verified code/data.code=00 and success=true are required. Browser return and Staff cannot manually mark PAID. | **Yes** |
| `usp_CalculateLateFee` | Calculate/update LateFee for overdue Rental Invoice. | Invoice, Contract, Policy, LateFee | Formula uses `Policy.LateFeeDivisorDays`. | Yes |
| `usp_MarkOverdueInvoices` | Mark unpaid rental invoices overdue when their captured Contract Policy threshold is reached. | Invoice, Contract, Policy | `OverdueStartDay`. | Yes |
| `usp_CreatePolicyVersion` | Inactivate prior active Policy and insert new active version with all configuration values. | Policy | Never update old business parameters in place. | **Yes** |
| `usp_AssignSupportTicket` | Validate Facility Manager/Staff facility scope; assign Staff and move OPEN -> IN_PROGRESS where applicable. | SupportTicket, Employee, Contract |  | Yes |
| `usp_CompleteSupportTicket` | Record ResultNote, CompletedAt and set COMPLETED. | SupportTicket | Does not mutate Contract/Payment/StorageUnit lifecycle. | Yes |
| `usp_CancelSupportTicket` | Permit OPEN/IN_PROGRESS -> CANCELLED before completion. | SupportTicket |  | Yes |
| `usp_SetUserAccountStatus` | Activate/deactivate account; reject Customer deactivation when active lifecycle exists. | UserAccount, Customer, Reservation, Contract |  | Yes |
| `usp_QueueNotification` | Insert NotificationLog PENDING. | NotificationLog |  | Yes |
| `usp_RetryPendingNotification` | Retry pending notification; success => SENT + SentAt, failure => remain PENDING. | NotificationLog | Usually executed by an application background job rather than pure DB networking logic. | No/short |

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

### Trigger design rule

Do not create triggers such as `Payment PAID -> automatically create Contract`. Complete Handover and Return finalization touch many tables and must remain explicit stored-procedure transactions for observability, validation and rollback.

---

## 9. Scheduled / Time-based Procedures

Time passing does not fire a database trigger. Time-based rules therefore require SQL Server Agent or `Frms.Api/BackgroundJobs` scheduling authoritative execution.

| Scheduled task | Recommended procedure | Purpose |
|---|---|---|
| Expire unpaid Deposit reservations | `usp_ExpirePendingReservations` | When Reservation Deposit deadline passes, cancel reservation and release capacity. Deadline is derived from captured Policy `DepositTimeoutHours`. |
| Process handover no-show | `usp_ProcessReservationNoShow` | After configured visit window ends, cancel SCHEDULED no-show Visit/Reservation or close incomplete checked-in handover according to business rule. |
| Generate recurring rental invoices | `usp_GenerateMonthlyInvoices` | Create missing Rental Fee invoice for active Contract billing month. |
| Mark rental invoices overdue | `usp_MarkOverdueInvoices` | Apply Contract Policy `OverdueStartDay`. |
| Refresh current late fee amounts | `usp_RecalculateOpenLateFees` | Recalculate overdue days/amount for overdue invoices where needed. |
| Retry notifications | `usp_RetryPendingNotification` / API background job | Reprocess `PENDING` notifications. |

---

## 10. Recommended Database Constraints and Indexes

Use native constraints/indexes before triggers whenever possible.

### Unique / filtered uniqueness

- `UserAccount.Email` UNIQUE.
- `UserAccount.PhoneNumber` UNIQUE.
- `Customer.UserAccountId` UNIQUE.
- `Employee.UserAccountId` UNIQUE.
- `Customer.CCCD` UNIQUE where not null.
- `Contract.ReservationId` UNIQUE.
- `DepositSettlement.ContractId` UNIQUE.
- `LateFee.InvoiceId` UNIQUE.
- `Payment.TransactionCode` UNIQUE where not null.
- Recommended unique `(FacilityId, UnitCode)` for StorageUnit.
- Recommended unique `(ContractId, BillingMonth, InvoiceType)` for Rental Fee invoices.
- Recommended one ACTIVE Contract per StorageUnit using a filtered unique index when supported.
- Recommended at most one active Policy row using a filtered unique index on `Status='ACTIVE'` when business requires a single global active Policy.

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
- Verified payOS signed POST webhook idempotency.

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

## 12. Known V1 Implementation Notes

### 12.1 Offline first-month settlement

Staff receives the full `Reservation.LockedRentalPrice` offline before Complete Handover. The authorized OPS-004 command acknowledges receipt; Contract creation records settlement without first-month Invoice, Payment, receipt entity or paid flag. PAY-002 is retired and its identifier must not be reused. Every new online Payment is Invoice-backed. Preserve any legacy first-month or unlinked historical rows pending approved deployment review, without fabricating links or counting revenue twice.

This is a database implementation decision that should be tested carefully for idempotency and incorrect-payment binding.

### 12.2 Contract Policy capture

SRS V9 locks the rule: `Contract.PolicyId = Reservation.PolicyId` during `usp_CompleteHandover`. A newly active Policy between Reservation and handover must not drift the Contract to a different version.

### 12.3 Discount reuse and immutability

Customer can have many Discounts; each Contract uses at most one and no single-use rule exists. First month does not use Contract Discount. Once a Discount is referenced by any Contract, `CustomerId`, `Percentage`, `EffectiveFrom`, and `EffectiveTo` are immutable; inactivation blocks only new Contract selection.

### 12.4 Damage decision versus Late/Extra Fee

`DamageRecord` uses `PENDING -> APPROVED/REJECTED` with decision authority assigned to same-Facility Facility Manager. `LateFee` and `ExtraFee` do not have an independent approval status. Finalize Return is blocked by PENDING Damage and settlement includes APPROVED Damage only.

### 12.5 Authentication/account alignment

Customer login identifier is unique `PhoneNumber`; Employee login identifier is unique `Email`. Email verification and password reset/change are outside Release 1. Administrator-created Employee accounts remain INACTIVE until the generated 16-character initial password is successfully emailed; plaintext credentials are never stored or logged.

---


## 13. SQL Server Physical Data-Type Standard

This section converts the logical types used above into a consistent SQL Server physical schema. These are implementation conventions for V1 and do not change business semantics.

| Logical type | SQL Server type | V1 convention |
|---|---|---|
| UUID | `UNIQUEIDENTIFIER` | PK/FK identifier. Prefer application-generated UUID or `NEWSEQUENTIALID()` for clustered PKs where appropriate. |
| Short code / enum | `VARCHAR(50)` | Enforced by `CHECK`; do not use SQL Server-specific custom enum types. |
| Name / label | `NVARCHAR(150)` | Unicode text. |
| Email | `VARCHAR(254)` | Case normalization handled by application; UNIQUE index. |
| Phone | `VARCHAR(30)` | Store normalized phone text, not numeric. |
| CCCD | `VARCHAR(20)` | Nullable, filtered UNIQUE index. |
| Address / note / description | `NVARCHAR(1000)` or `NVARCHAR(MAX)` | Use bounded length where practical; `MAX` only for genuinely large text. |
| Money / amount / price | `DECIMAL(18,2)` | Never use `FLOAT`/`REAL` for money. |
| Percentage | `DECIMAL(5,2)` | `CHECK (Percentage BETWEEN 0 AND 100)`. |
| Month | `DATE` | Canonical first day of month recommended, e.g. `2026-10-01`. |
| Calendar date | `DATE` | Visit/Billing day when time is not needed. |
| Timestamp | `DATETIME2(3)` | Store UTC where possible; API/UI performs timezone conversion. |
| IP address | `VARCHAR(45)` | Supports IPv4 and IPv6 textual form. |
| JSON | `NVARCHAR(MAX)` | Add `CHECK (ISJSON(column)=1)` when non-null. |
| Binary evidence | `VARBINARY(MAX)` | Used by `InspectionEvidence.FileData`. |

### 13.1 Timestamp convention

Recommended defaults:

```sql
CreatedAt DATETIME2(3) NOT NULL
    CONSTRAINT DF_<Table>_CreatedAt DEFAULT SYSUTCDATETIME()
```

`PaidAt`, `CompletedAt`, `SentAt`, `ActualReturnDate`, `EmailVerifiedAt`, and similar event timestamps must be set only when the corresponding event actually occurs.

### 13.2 Identifier convention

All persisted entities use a single-column `UNIQUEIDENTIFIER` PK. FK columns use the exact same SQL type as their referenced PK. Avoid mixing integer surrogate keys with UUID keys inside V1.

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
| `Reservation.Status` | `PENDING_DEPOSIT`, `CONFIRMED`, `COMPLETED`, `CANCELLED` |
| `Visit.VisitType` | `RESERVATION`, `ACCESS`, `RETURN` |
| `Visit.Status` | `SCHEDULED`, `CHECKED_IN`, `CHECKED_OUT`, `CANCELLED` |
| `Contract.Status` | `ACTIVE`, `COMPLETED`, `TERMINATED` |
| `Invoice.InvoiceType` | `DEPOSIT`, `RENTAL_FEE` |
| `Invoice.Status` | `UNPAID`, `PAID`, `OVERDUE`, `CANCELLED` |
| `Payment.PaymentMethod` | New attempts: `PAYOS`; existing `MOMO`/`VNPAY` only as legacy history |
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
| `Reservation.Status` | `PENDING_DEPOSIT` | Created before Deposit payment. |
| `Visit.Status` | `SCHEDULED` | All customer visits are scheduled first. |
| `Contract.Status` | `ACTIVE` | Contract row is created only on successful handover. |
| `Invoice.Status` | `UNPAID` | No first-month Invoice is created during handover. Verified payOS webhook may mark an eligible existing Invoice PAID. |
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
| `CreatedAt` columns | `SYSUTCDATETIME()` | Recommended DB default. |

---

## 16. Nullability and Conditional-Column Rules

| Column | Nullable because | Validation rule |
|---|---|---|
| `Customer.CCCD` | Customer may not have provided/verified CCCD yet. | If present, unique. |
| `Employee.FacilityId` | Global roles have no Facility. | Required for Facility Staff/Manager; null for Business Operations Manager/System Administrator. |
| `Visit.EmployeeId` | Visit may be scheduled before a Staff actor handles it. | Set when Staff starts handling/checks in. |
| `Visit.ActualReturnDate` | Only meaningful for confirmed physical RETURN. | Must be null for RESERVATION/ACCESS; set for confirmed RETURN. |
| `Contract.DiscountId` | Discount is optional. | If present, Discount must belong to Contract Customer and be valid at Contract creation. |
| `Invoice.BillingMonth` | Deposit is not monthly. | Null for DEPOSIT; required for RENTAL_FEE. |
| `Invoice.DiscountId` | Discount is optional; Deposit cannot use it and first-month Invoice does not exist. | Null for DEPOSIT; eligible RENTAL_FEE after StartMonth may reference the Contract-selected Discount. |
| `Payment.InvoiceId` | Every new Payment is Invoice-backed. | Non-null for current-model rows; historical null rows require approved deployment review, never fabricated links. |
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
| `BR-RES-05` | Deposit cannot use Discount. | Invoice, Discount | CHK/TRG |
| `BR-RES-06` | Reservation confirms only after Deposit payment succeeds. | Reservation, Invoice, Payment | `usp_ConfirmReservation` |
| `BR-RES-07` | RESERVATION Visit date must be inside captured Policy visit-day window. | Reservation, Visit, Policy | `usp_ConfirmReservation` |
| `BR-RES-08` | Reservation Facility/UnitType/StartMonth/EndMonth are immutable after creation. | Reservation | TRG or restrict UPDATE surface |
| `BR-VIS-01` | Only SCHEDULED Visit can be cancelled. | Visit | SP + TRG |
| `BR-VIS-02` | RESERVATION Visit references Reservation; ACCESS/RETURN reference Contract. | Visit | SP + `trg_Visit_ValidateEntity` |
| `BR-VIS-03` | ACCESS Visit requires ACTIVE Contract. | Contract, Visit | `usp_CreateAccessVisit` |
| `BR-VIS-04` | Pending RETURN Visit blocks new ACCESS Visit and Renewal. | Visit, Contract | `usp_CreateAccessVisit`, `usp_RenewContract` |
| `BR-HO-01` | Selected StorageUnit must be AVAILABLE and match Reservation Facility + UnitType. | Reservation, StorageUnit | `usp_CompleteHandover` |
| `BR-HO-02` | Staff acknowledges full offline first-month receipt before Complete Handover. | Reservation, Contract | `usp_CompleteHandover` |
| `BR-HO-03` | Complete Handover atomically creates Contract without first-month Invoice/Payment, marks Unit IN_USE, Reservation COMPLETED, Visit CHECKED_OUT; Contract inherits Reservation.PolicyId. | Reservation, Visit, StorageUnit, Contract, Policy | `usp_CompleteHandover` transaction |
| `BR-CON-01` | One Reservation creates at most one Contract. | Reservation, Contract | UNIQUE index |
| `BR-CON-02` | A StorageUnit has at most one ACTIVE Contract at a time. | Contract, StorageUnit | Filtered UNIQUE index + SP |
| `BR-DIS-01` | Customer owns many Discounts; each Discount belongs to exactly one Customer. | Customer, Discount | FK |
| `BR-DIS-02` | Each Contract uses zero or one Discount, and it must belong to the same Customer. | Contract, Discount | FK + TRG/SP |
| `BR-DIS-03` | Contract-selected Discount is fixed for that Contract lifecycle in Release 1. | Contract | TRG/restrict UPDATE |
| `BR-DIS-04` | Rental invoices snapshot DiscountId and DiscountAmount; later Discount edits do not rewrite issued invoices. | Contract, Discount, Invoice | `usp_CreateMonthlyInvoice` |
| `BR-DIS-05` | First-month offline rent does not apply Contract Discount; eligible invoices strictly after Contract.StartMonth may apply the captured Discount. | Reservation, Contract, Invoice, Discount | `usp_CompleteHandover` / `usp_CreateMonthlyInvoice` |
| `BR-DIS-06` | Referenced Discount `CustomerId`, `Percentage`, `EffectiveFrom`, `EffectiveTo` are immutable; INACTIVE blocks only new selection. | Discount, Contract | TRG/SP |
| `BR-BIL-01` | Exactly one logical Deposit Invoice per Reservation. | Reservation, Invoice | Filtered UNIQUE index |
| `BR-BIL-02` | At most one Rental Fee Invoice per Contract + BillingMonth. | Contract, Invoice | Filtered UNIQUE index |
| `BR-BIL-03` | Rental invoice initial-period BaseAmount comes from Reservation.LockedRentalPrice. | Reservation, Contract, Invoice | `usp_CreateMonthlyInvoice` |
| `BR-BIL-04` | Rental invoice extension-period BaseAmount comes from corresponding ContractExtension.AppliedMonthlyPrice. | ContractExtension, Invoice | `usp_CreateMonthlyInvoice` |
| `BR-BIL-05` | From Policy overdue threshold, unpaid Rental Invoice becomes OVERDUE. | Invoice, Contract, Policy | scheduled `usp_MarkOverdueInvoices` |
| `BR-PAY-01` | Verified payOS signed POST webhook is idempotent by gateway transaction/reference. | Payment, Invoice | `usp_ApplyPaymentResult` + UNIQUE index |
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
| `CALC-DIS-01` DiscountAmount | `BaseAmount * Discount.Percentage / 100` | Eligible Rental Fee invoices have BillingMonth > Contract.StartMonth and may apply the captured Discount. First-month offline rent has no Discount or Invoice. |
| `CALC-INV-03` AmountDue | `MAX(BaseAmount - DiscountAmount, 0)` | Deposit has DiscountAmount = 0. |
| `CALC-LATE-01` OverdueDays | Number of chargeable overdue days from captured Policy `OverdueStartDay` to calculation/return/payment cutoff | Exact date arithmetic implemented consistently in SP. |
| `CALC-LATE-02` LateFee | `(RentalFeeAmount / Policy.LateFeeDivisorDays) * OverdueDays` | Use `DECIMAL`, not floating-point. |
| `CALC-SET-01` TotalDeduction | `ApplicableLateFee + ApplicableExtraFee + ApprovedDamageAmount` | Late/Extra have no approval status in V1; inclusion is determined by return rules. |
| `CALC-SET-02` RefundAmount | Normal return: `MAX(DepositPaidAmount - TotalDeduction, 0)` | Early return forces 0. |
| `CALC-SET-03` AdditionalAmountDue | `MAX(TotalDeduction - DepositPaidAmount, 0)` | Recorded obligation only. |
| `CALC-CAP-01` Available capacity | `eligible physical-unit capacity - Reservation holds - Contract occupancy` for each month | Must evaluate every month in requested period. |
| `CALC-REP-01` Usage Rate | `IN_USE rentable StorageUnits / total rentable StorageUnits` | Derived reporting value; not persisted. |
| `CALC-REP-02` Revenue | Contract-derived first-month amount in Contract.StartMonth plus paid later Rental Fee invoice amounts | First-month amount is Reservation.LockedRentalPrice once per Contract. Exclude Deposit/late/extra/damage and legacy first-month invoice double counting. |

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
| Create Reservation | Two customers reserve last capacity simultaneously. | Single transaction; lock/read capacity source under `SERIALIZABLE` or equivalent key-range locking strategy; re-check before insert. |
| Complete Handover | Two handovers select same AVAILABLE StorageUnit. | Transaction + update lock on selected StorageUnit; validate `Status=AVAILABLE` at update time. |
| Renew Contract | Renewal and new Reservation consume same future capacity. | Same capacity locking strategy as Reservation across every extension month. |
| Claim Inspection | Two Staff claim one PENDING Inspection. | Single conditional UPDATE `WHERE Status='PENDING'`; require exactly 1 affected row. |
| Apply payOS signed POST webhook | Gateway retries the same verified webhook. | UNIQUE `TransactionCode`/gateway reference + idempotent procedure; no second financial effect. |
| Generate monthly invoice | Job retried / multiple workers. | Unique filtered index on Contract+BillingMonth for RENTAL_FEE + insert-if-absent transaction. |
| Create Deposit invoice | Reservation creation retried. | Unique filtered index on Reservation EntityId for DEPOSIT. |
| Create Policy version | Two admins create versions concurrently. | Transaction + lock current active Policy/version sequence; filtered unique active-policy index. |
| Finalize Return | Duplicate finalization request. | UNIQUE `DepositSettlement.ContractId`; Contract terminal-state check; idempotent procedure behavior. |
| Retry Notification | Multiple job executions pick the same row. | Claim/update pattern or background-job lock; SENT rows are terminal. |

### 22.1 Stored-procedure transaction template

Business procedures that span multiple tables should use the equivalent of:

```sql
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    -- validate current state using appropriate locks
    -- apply all related changes
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
```

Use the narrowest isolation/locking approach that still protects the invariant; capacity-sensitive operations require stronger protection than simple single-row status changes.

---

## 23. Idempotency Matrix

| Operation | Idempotency key / invariant | Repeat behavior |
|---|---|---|
| PAY-001 payOS attempt creation | UUID Idempotency-Key, globally unique Payment.IdempotencyKey | Same Invoice/key returns the stored attempt without another provider session; another Invoice with the same key returns a controlled conflict without exposing the first Payment. |
| payOS signed POST webhook | SQL-sequence ProviderOrderCode / verified reference | Same verified webhook returns already-confirmed acknowledgement without duplicate Payment/Invoice effects. |
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
| `FIRST_MONTH_PAYMENT_NOT_SUCCESS` | Handover payment precondition failed. |
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

Exact numeric SQL error numbers can be allocated during DDL implementation (for example, custom `THROW 510xx, ...`).

---

## 25. Index Strategy

The following indexes are recommended in addition to PK/unique constraints. Final INCLUDE columns should be tuned after query/API implementation.

| Table | Recommended index | Purpose |
|---|---|---|
| UserAccount | UNIQUE `(Email)` | Login/account lookup. |
| UserAccount | `(RoleId, Status)` | Admin/RBAC filtering. |
| Customer | UNIQUE filtered `(CCCD) WHERE CCCD IS NOT NULL` | Optional citizen-ID uniqueness. |
| Employee | `(FacilityId)` | Facility staff lookup. |
| Facility | `(Status)` | Browse active Facilities. |
| StorageUnit | UNIQUE `(FacilityId, UnitCode)` | Operational code uniqueness. |
| StorageUnit | `(FacilityId, UnitTypeId, Status)` | Availability/handover/capacity queries. |
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

## 27. Scheduled Job Dictionary

| Job name | Suggested cadence | Procedure | Idempotency / note |
|---|---|---|---|
| `job_ExpirePendingReservations` | Every 5-15 minutes | `usp_ExpirePendingReservations` | Uses Reservation captured Policy timeout; process only eligible PENDING_DEPOSIT rows. |
| `job_ProcessReservationNoShow` | Daily after configured handover window / periodic | `usp_ProcessReservationNoShow` | Must not cancel already completed/cancelled lifecycle. |
| `job_GenerateMonthlyInvoices` | Daily; especially month boundary | `usp_GenerateMonthlyInvoices` | Unique invoice index prevents duplicates. |
| `job_MarkOverdueInvoices` | Daily | `usp_MarkOverdueInvoices` | Uses each Contract captured Policy. |
| `job_RecalculateOpenLateFees` | Daily and/or before settlement/payment display | `usp_RecalculateOpenLateFees` | Updates one LateFee row per overdue invoice. |
| `job_RetryNotifications` | Every few minutes / scheduled queue | notification background job / `usp_RetryPendingNotification` | Failed delivery remains PENDING. |

SQL Server Agent is appropriate if available. Otherwise `Frms.Api/BackgroundJobs` invokes Business services that reach the same procedures.

---

## 28. Stored Procedure Interface Conventions

For consistency across API integration:

- Stored procedures accept IDs and business input values, not complete serialized entities.
- Procedures validate authorization-relevant facility/customer ownership supplied by the caller context or application layer.
- Use `SET NOCOUNT ON` and `SET XACT_ABORT ON`.
- Business validation failures use `THROW` with stable application error codes/messages.
- A successful create procedure returns the created identifier and essential resulting state.
- Idempotent procedures may return the existing entity/result when the requested transition was already completed successfully.
- Procedures must not silently update historical snapshot values such as `Reservation.LockedRentalPrice`, `ContractExtension.AppliedMonthlyPrice`, or issued `Invoice` amounts.
- Procedures operating on Reservation/Contract always load the captured `PolicyId` first.

Recommended parameter naming:

```text
@ReservationId
@ContractId
@VisitId
@EmployeeId
@UserAccountId
@NowUtc              -- optional injected time for deterministic testing
```

Using an optional/test-controlled `@NowUtc` wrapper or clock abstraction can make time-rule integration tests deterministic; production can default to `SYSUTCDATETIME()`.

---

## 29. Trigger Implementation Conventions

All triggers must be **multi-row safe**. Never assume only one row exists in `inserted` or `deleted`.

Requirements:

- `SET NOCOUNT ON`.
- Validate sets using joins against `inserted`/`deleted`.
- Use `THROW` for invariant violations.
- Do not open network calls or long-running operations from triggers.
- Do not recursively orchestrate multi-table business lifecycle from triggers.
- Keep audit trigger payloads bounded; exclude `PasswordHash` and `InspectionEvidence.FileData`.
- Prefer `CHECK`, FK, UNIQUE and filtered indexes over triggers when the invariant can be expressed natively.

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

Recommended SQL Server object naming:

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
| SQL Agent job | `job_<Purpose>` | `job_MarkOverdueInvoices` |

Avoid reserved words and inconsistent abbreviations. Use the same business term everywhere: `StorageUnit`, not alternating with `PhysicalUnit` in physical schema object names.

---

## 32. DDL Creation / Migration Order

A practical creation order that avoids FK dependency problems:

1. Database/schema settings and helper conventions.
2. `UserRole`.
3. `Policy`.
4. `Facility`.
5. `UnitType`.
6. `UserAccount`.
7. `Customer`.
8. `Employee`.
9. `Discount`.
10. `StorageUnit`.
11. `Reservation`.
12. `Visit` (polymorphic business reference; direct Employee FK only).
13. `Contract`.
14. `ContractExtension`.
15. `Invoice`.
16. `Payment`.
17. `LateFee`.
18. `SupportTicket`.
19. `Inspection`.
20. `DamageType`.
21. `DamageRecord`.
22. `InspectionEvidence`.
23. `ExtraFeeType`.
24. `ExtraFee`.
25. `DepositSettlement`.
26. `LoginHistory`.
27. `AuditLog`.
28. `NotificationLog`.
29. Native CHECK/UNIQUE/filtered indexes not already declared inline.
30. Seed the five `UserRole` rows, initial `Policy` v1, and fixed production `DamageType` rows for Phase 0; defer `ExtraFeeType` rows to a later deployment with approved amounts and currency.
31. Integrity/audit triggers.
32. Stored procedures.
33. SQL Agent jobs / application scheduler configuration.
34. Permission grants / revoke direct business-table DML where procedure-only access is desired.

Migrations should be versioned, repeatable/idempotent where possible, and never silently modify production historical snapshot data.

---

## 33. Physical Schema Checklist Before Generating DDL

The schema is ready for DDL generation when all items below are explicitly represented in migrations:

- [ ] 27 tables created with consistent SQL Server physical types.
- [ ] All PKs and direct FKs created.
- [ ] Polymorphic `Visit.EntityId` and `Invoice.EntityId` validation covered by procedures/triggers.
- [ ] All status/type CHECK constraints created, including fixed SupportTicket categories with `OTHER`.
- [ ] Monetary/date-range CHECK constraints created.
- [ ] Filtered UNIQUE indexes created for active StorageUnit Contract, Deposit Invoice, monthly Rental Invoice, nullable CCCD, nullable TransactionCode, active Policy.
- [ ] Defaults created for deterministic initial statuses and timestamps (`Facility=INACTIVE`, `StorageUnit=AVAILABLE`).
- [ ] `Customer 1:N Discount` implemented through `Discount.CustomerId`.
- [ ] `Contract.DiscountId` nullable and ownership/validity guard implemented; referenced Discount financial/effective fields are immutable.
- [ ] First-month rent is offline without Invoice/Payment; eligible invoices strictly after Contract.StartMonth may apply captured Contract Discount.
- [ ] Complete Handover enforces `Contract.PolicyId = Reservation.PolicyId`.
- [ ] Damage decision lifecycle/Manager authorization and PENDING-finalization block are implemented.
- [ ] Reservation immutable business fields protected after creation.
- [ ] Policy historical values protected from overwrite/delete.
- [ ] Append-only protections implemented for ContractExtension, LoginHistory, AuditLog.
- [ ] Stored procedures implemented with transactions and stable business error codes.
- [ ] Capacity-sensitive procedures use an explicit concurrency strategy.
- [ ] Verified payOS signed POST webhook and recurring jobs are idempotent; browser return does not mutate financial state.
- [ ] Trigger code is multi-row safe.
- [ ] Audit rules exclude secrets/binary payloads.
- [ ] Phase 0 seed data created for the five roles, initial Policy v1, and six fixed DamageTypes; ExtraFeeType schema/checks exist without seeded rows.
- [ ] Time-based jobs configured outside triggers.
- [ ] DB roles/permissions prevent unauthorized direct lifecycle updates where applicable.
- [ ] Integration tests cover every valid and invalid state transition.

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
| `DBT-09` | Same verified payOS signed POST webhook arrives twice. | Already-confirmed acknowledgement; no duplicate financial/lifecycle effects. |
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
| `DBT-24` | Offline first-month handover with Contract Discount. | No first-month Invoice/Payment; first eligible later invoice may apply captured Contract Discount; first-month revenue is Contract-derived once. |
| `DBT-25` | Employee initial credential email fails. | Account remains INACTIVE; no plaintext password is persisted or logged. |

---

## 35. Version 2.1 Change Log

- Established 27 persisted entities.
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

## 36. Source and Authority

Primary implementation source: `FRMS_SRS_V10.md` (V10 FINAL, including approved 2026-10-07 offline-first-month and payOS revisions). Unrelated database rules retain their existing baseline.

Authority order:

1. `FRMS_SRS_V10.md`.
2. This aligned Data Dictionary V2.1.
3. Aligned Scope document.
4. Implementation detail.

No database migration, trigger, procedure, EF configuration, seed, Swagger contract, or test fixture may silently create semantics that conflict with this authority order.
