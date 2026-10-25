# FRMS — API Contract & Authorization Specification

**Version:** V11.0 REVIEW CANDIDATE  
**Date:** 2026-10-22 (Customer Transaction History requirements approval; recurring BackgroundJobs reconciliation dated 2026-10-21)\
**Source:** `FRMS_SRS_V11.md` **§0.0.9 approved owner decisions**, including Customer activity OD-06/07 dated 2026-10-22; locally supplied V10.2 API §13; historical V10 main profile revision (partially superseded); read-only GitHub `main` source at `0219f30734b0` (BackgroundJobs now registered; initial V11 audit at `90a6c5982d42`); Transaction History controller/DTO/service/repository rechecked at `1ec61d6` for OD-07\
**Status:** **V11 REVIEW CANDIDATE / PARTIAL OWNER APPROVAL** — optional Customer.Address, persisted Manager handover assignment, Administrator timeline-only reads and Customer sent-notification/Transaction History reads approved in governing SRS §0.0.9; other supplemental routes remain provisional. Implementation and test acceptance are separate from requirement approval. A route existing in code is not by itself a release-test pass.
**Repository:** https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/tree/main  
**Companion Data Dictionary:** [`FRMS_Data_Dictionary_V11.md`](FRMS_Data_Dictionary_V11.md) — 28-entity V11 target, PostgreSQL mapping, routines, integrity and DB tests

## A. Normative boundary and precedence

This file contains the extracted full API contract catalogue formerly embedded in SRS V10 §13. It retains IDs, request/response templates, envelope formats, errors, authorization, paging, OpenAPI and integration requirements, except where V11 revisions explicitly supersede them.

- Approved business semantics, ownership, lifecycle, money, return, policy and capacity are defined in `FRMS_SRS_V11.md` (subject to owner approval). Database entities and stored invariants are in `FRMS_Data_Dictionary_V11.md` (28 target entities including approved handover assignment). This API document must not grant access or change business rules by itself.
- When this file differs from the deployed controller, DTO or Swagger, log **CONTRACT DRIFT**; do not silently choose the more permissive behavior.
- Existing V10 endpoint IDs (`AUTH-*`, `CAT-*`, `OPS-*`, etc.) are retained. `IMPL-*` identifiers are **stable reconciliation labels**, not automatic evidence of release acceptance. `IMPL-HOA-01..03`, SYSTEM_ADMINISTRATOR timeline RBAC and `IMPL-CUSACT-01..02` Customer activity reads are explicitly owner-approved in SRS §0.0.9; other `IMPL-*` scopes remain provisional.
- This is a separate, directly reviewable spec; SRS cross-references to historical “§13.x” now resolve to **this file §13.x**.
- All API responses must remain JSON `camelCase`; UTC timestamps; month `YYYY-MM`; stable `error.code` and `traceId`; authenticated scope and server-side authorization. All mutation commands require authoritative service/repository validation.

## B. V11 API changes at a glance

| Topic | V10 contract | V11 review treatment |
|---|---|---|
| AUTH-001 / AUTH-005 | Historical V10 latest revision removed Customer `address` | **Owner-approved V11 reversal:** retain optional nullable `address` in registration/profile PATCH, current code accepts it; no new use/share by inference |
| AUTH-004 | Own-profile example primarily Employee | Customer own-profile has `profile.cccd: string | null`; do not project CCCD publicly |
| PostgreSQL | SQL Server-specific backend conventions | PostgreSQL/Npgsql transaction, UUID, PL/pgSQL deployment; HTTP contract otherwise unchanged |
| Persisted Manager assignment | Absent from §13 | **APPROVED §0.0.9:** `IMPL-HOA-01..03` with 28th entity/four statuses; tests pending |
| Unit status timeline | Absent from §13 | **APPROVED RBAC §0.0.9:** read-only AuditLog-derived; System Administrator allowed these 3 GETs system-wide only |
| Customer activity | Absent from §13 | `IMPL-CUSACT-01` sent history approved by SRS OD-06; `IMPL-CUSACT-02` own Payment history requirements approved by SRS OD-07; implementation/testing acceptance separate |
| Facility/BOM directories & lookups | Absent from §13 | `IMPL-RBAC-*` supplementary GETs, reconcile role/PII scopes |
| UnitType media URLs | Not in canonical JSON | **Observed in public CAT-003/004/005 DTO/controller**: nullable `imageUrl`/`videoUrl` (Pg012); governance/Swagger test pending |
| BackgroundJobs / billing read | Existing business routines; no scheduler status guarantee | **`main` `0219f307` registers four hosted services for six tasks**; no HTTP job-management API; persisted reads and payOS unchanged; notification sender not operational |

---

# 13. API Contract Catalogue


## 13.1 API Design Status

`V10-APPROVED CORE CONTRACT — V11 REVIEW CANDIDATE`

This companion API specification preserves the approved V10 core API surface for BE–FE implementation and records provisional V11 deltas separately. Endpoints wrap approved business capabilities; they do not introduce additional business lifecycles.

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

For a requested period it is the minimum per-month capacity defined in section 12.3. With no month range, `requestedPeriod` and `availableCapacity` are null, not a fabricated availability result.

### 13.10.3a Public UnitTypeSummary

CAT-004/005 reuse the approved existing UnitType fields. Public-access approval did **not** introduce a persisted image/amenity/stock-history field. **Code now separately includes Pg012 nullable `UnitType.ImageUrl`/`VideoUrl`; public exposure is an unapproved V11 implementation extension (§13A.5) and MUST NOT be inferred from this approved catalog JSON example.**

```json
{
  "unitTypeId": "uuid",
  "name": "Medium Private",
  "mode": "PRIVATE",
  "size": "string",
  "rentalPrice": 1500000.00,
  "description": "string"
}
```

### 13.10.3b PublicPolicySummary / PublicPolicyDetail

This is a non-persisted read projection of approved explanatory content plus the current ACTIVE Policy. It is not a new Policy master/history schema. PUB-POL-001 items have the fields below except `sections`; PUB-POL-002 includes `sections`.

```json
{
  "policyKey": "reservation-cancellation",
  "title": "Reservation and cancellation",
  "summary": "Public explanation of the current reservation and cancellation rules.",
  "policyId": "uuid",
  "version": 1,
  "effectiveFrom": "2026-10-01T00:00:00Z",
  "effectiveTo": null,
  "sections": [
    {
      "heading": "Before making a reservation",
      "body": "Choose a facility, unit type and rental period. Sign in before creating your reservation."
    }
  ]
}
```

Policy identity/version/effective values come from the current ACTIVE Policy, not from hard-coded article metadata. `body` is plain text; embedded executable HTML is not accepted. Text examples are illustrative, not deployment-approved policy copy. No Customer/Contract identifier, personalized amount, administrator identity or historical Policy row is included.

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

The CAT-001..005 public access/filter/view changes, PUB-POL-001/002 and Anonymous AI access below are approved governing contracts under section 0.0.6. Previously approved protected business/authentication contracts remain unchanged. Endpoint implementation and Swagger/frontend synchronization must be verified separately before feature acceptance.

| API ID | Method | Endpoint | Description | Allowed access / role(s) | Feature |
|---|---|---|---|---|---|
| `AUTH-001` | `POST` | `/api/v1/auth/customer/register` | Customer self-registration | Anonymous | Supporting / CWP |
| `AUTH-002` | `POST` | `/api/v1/auth/customer/login` | Customer login by PhoneNumber | Anonymous | Auth |
| `AUTH-003` | `POST` | `/api/v1/auth/employee/login` | Employee login by Email | Anonymous | Auth |
| `AUTH-004` | `GET` | `/api/v1/auth/me` | Get current authenticated account/profile | Authenticated | Auth |
| `AUTH-005` | `PATCH` | `/api/v1/customers/me/profile` | Update limited Customer profile fields | CUSTOMER | Supporting |
| `IMPL-CUSACT-01` | `GET` | `/api/v1/customers/me/notifications` | Own sent delivery history; SRS OD-06 and §13A.3 | ACTIVE CUSTOMER only | Supporting Customer Notifications / Overview |
| `IMPL-CUSACT-02` | `GET` | `/api/v1/customers/me/transactions` | Own Payment attempt history; SRS OD-07 and §13A.3 | ACTIVE CUSTOMER only | Supporting Customer Transaction History |
| `CAT-001` | `GET` | `/api/v1/facilities` | Public active Facility search with matching UnitType/range filters | Public (Anonymous and authenticated same projection) | GWP-01 / CWP-01 |
| `CAT-002` | `GET` | `/api/v1/facilities/{facilityId}` | Public active Facility detail | Public | GWP-01 / CWP-01 |
| `CAT-003` | `GET` | `/api/v1/facilities/{facilityId}/unit-types` | Public UnitTypes and optional requested-period capacity; explicit protected operational view | Public; operational view: CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER | GWP-01 / CWP-01 / operational read |
| `CAT-004` | `GET` | `/api/v1/unit-types/{unitTypeId}` | Public offered UnitType detail; explicit protected operational view | Public; operational view: CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER | GWP-01/02 / CWP-01 / operational read |
| `CAT-005` | `GET` | `/api/v1/unit-types` | Public offered UnitType list for shared Size Guide | Public | GWP-02 / CWP-01 |
| `PUB-POL-001` | `GET` | `/api/v1/public/policies` | Current public Help Center policy summaries | Public | GWP-04 |
| `PUB-POL-002` | `GET` | `/api/v1/public/policies/{policyKey}` | Current public policy article/detail | Public | GWP-04 |
| `AI-001` | `POST` | `/api/v1/ai/unit-type-recommendations` | Optional shared anonymous/Customer AI Size Guide | Public | GWP-02 / CWP-02 |
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
| `CON-001` | `GET` | `/api/v1/contracts` | Customer own / Staff & Manager assigned-Facility / BOM system Contract list | CUSTOMER,FACILITY_STAFF,FACILITY_MANAGER,BUSINESS_OPERATIONS_MANAGER | CWP-06 / Staff operational read (OD-08) / monitoring |
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

The access/role sets in section 13.11 are authoritative for each endpoint; a broad role in the actor matrix does not grant access to an unlisted endpoint. Public means the identity-independent projection in sections 3.8/10.6.1, not general operational access. Under owner-approved SRS OD-08, Staff may call CON-001 **only** for its assigned-Facility read-only Contract list, but still cannot call RES-002 or VIS-003; OPS-001 supplies the Staff daily work list. Manager and BOM keep their current list/detail scopes. SYSTEM_ADMINISTRATOR receives only the same public catalog/policy information as any visitor, not new private/operational business-read authority through the Guest revision.

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

All protected operational/private reads require ACTIVE account and authoritative current role/assignment; this requirement does not apply to the public allowlist. Wrong Facility or endpoint role -> 403 FORBIDDEN; nonexistent accessible detail resource -> 404 RESOURCE_NOT_FOUND. Customer ownership denial retains section 10.8's consistent security design. Employees do not inherit Customer create/cancel/reschedule/renew/payment commands from GET access. Immutable money/status remains server-owned. Listing Discount or ExtraFeeType rows does not grant their write endpoints.

CAT-003/004 use an explicit `view=public|operational` query, default `public`, to distinguish the projection. `view=operational` retains the listed Customer/Staff/Manager role gate and current ACTIVE account checks. Staff/Manager CAT-003 additionally requires assignment to the path Facility; CAT-004 retains existing operational UnitType read semantics and does not grant Unit management. Anonymous operational requests -> 401; authenticated unlisted role -> 403. A denied operational request MUST NOT fall back to public results. `view=public` never contains operational fields, even for employees. BOM-006/008 and UNIT-* remain protected; do not mark an entire catalog/business controller anonymous without endpoint/view-specific enforcement.

## 13.12 Authentication API Contracts

### AUTH-001 — Customer Registration

**Owner-approved V11-OD-01 (supersedes historical V10 removal):** Customer `address` is **optional** in registration, nullable and limited to 1,000 characters per existing request DTO. It may be entered and stored for potential future use, but is never mandatory for registration or other current workflows. Do not infer any future processing or public/employee/BOM disclosure. `Facility.address` is a distinct, required field.

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
  "address": null,
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
| `address` | No | nullable Customer physical address, max 1,000 characters; not validated/used as a Facility address; omit or set `null` when unknown |
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

Customer own-profile response (example with a saved CCCD):

```json
{
  "data": {
    "userAccountId": "uuid",
    "role": "CUSTOMER",
    "status": "ACTIVE",
    "email": "customer@example.com",
    "phoneNumber": "0900000000",
    "profile": {
      "customerId": "uuid",
      "employeeId": null,
      "fullName": "Customer A",
      "facilityId": null,
      "cccd": "012345678901"
    }
  }
}
```

`profile.cccd` MUST be present with JSON `null` for a Customer who has not
provided a citizen ID. The field MUST NOT be present in Employee `profile`,
login-token payloads or public/operational Customer directory responses.
Read only the Customer row joined by the authenticated account ID; never
accept a Customer ID from the request. No address/ID-verification/status
changes are implied. The running Swagger/OpenAPI output and FE-API contract
require verification before the release gate is marked PASS.

**V11 Customer own-profile rule (CCCD retained; Address collection superseded by V11-OD-01):**
The existing employee response example above is unchanged. For a `CUSTOMER`,
`data.profile` additionally exposes `customerId`, `fullName`, and optional
`cccd: string | null`; registration may accept optional `Customer.address`, but the **current AUTH-004 response DTO does not expose Address and V11-OD-01 does not introduce a new read projection**. `phoneNumber` and `email` remain read-only login/contact identifiers.
Until the running backend contract includes `cccd`, frontend clients MUST treat
missing/malformed `cccd` as unavailable and disable its editing rather than
reinterpreting absence as `null`. Do not expose CCCD via public directories,
telemetry, URLs, or storage.

### AUTH-005 — Update Customer Profile

Request:

```json
{
  "fullName": "Updated Name",
  "address": null,
  "cccd": "optional"
}
```

**V11-OD-01:** `address` is an optional string of max 1,000 characters or explicit `null` (clear saved address); omit to leave unchanged. The example shows clearing it; supplying a nonempty string persists it. The implementation's parser only accepts `fullName`, `address`, `cccd`; `fullName` when supplied must be nonblank (max 150); `cccd` is nullable/max 20/unique when present. This API must not expose Address in public or other-role directory DTOs without separate approval. This endpoint still **MUST NOT** change login `PhoneNumber` or account `Email` (DD-07).

## 13.13 Public Catalog / Policy / AI API Contracts

`TEAM-APPROVED — REV-2026-10-13-GUEST`

These contracts define the target implementation; Swagger/DTOs/FE are updated in the owning implementation tasks. Public GETs require no account, return the same catalog/policy projection for the same data snapshot/query and never create business rows. Standard success/error envelopes and pagination from sections 13.3..7 apply.

### CAT-001 — Browse / Search Active Facilities

Unfiltered browse remains supported:

```text
GET /api/v1/facilities?page=1&pageSize=20
```

Example complete search:

```text
GET /api/v1/facilities?location=Hanoi&mode=PRIVATE&size=Medium
    &startMonth=2026-11&endMonth=2027-01&page=1&pageSize=20
```

| Query | Contract |
|---|---|
| `location` | Optional trimmed text; case-insensitive literal substring match against existing Facility.Name or Facility.Address; no new City/Location entity or inferred geographic coordinates |
| `facilityId` | Optional UUID for an exact existing Facility selection; combined with other filters using AND |
| `mode` | Optional `PRIVATE` or `PUBLIC`; applies to UnitType.Mode |
| `size` | Optional exact existing UnitType.Size value, trimmed and compared case-insensitively; UI obtains available values from CAT-005 rather than inventing size enums |
| `unitTypeId` | Optional UUID for an exact UnitType selection, combined with mode/size using AND |
| `startMonth`, `endMonth` | Both omitted for general browsing or both supplied as `YYYY-MM`; public reservation search requires StartMonth > current business month and EndMonth >= StartMonth |
| `sortBy`, `sortDirection` | `sortBy=name` only, default name ascending; FacilityId is a stable tie-breaker; no unsupported "recommended" or revenue/usage ranking |
| `page`, `pageSize` | Common pagination limits |

Blank location/size means no corresponding filter. Unsupported enum/sort/UUID/pagination -> 400 VALIDATION_ERROR. A single month parameter, malformed month, non-future public StartMonth or EndMonth < StartMonth -> 400 INVALID_MONTH_RANGE. A valid nonexistent Facility/UnitType filter returns an empty collection, not private detail.

Return paginated `FacilitySummary`, one item per matching ACTIVE Facility. The location/Facility filters apply first. With mode/size/UnitType/range filters, an offered UnitType must satisfy all of them together. With a range it must have availableCapacity > 0 for the entire inclusive range, using section 12.3; one unavailable month excludes that UnitType. A Facility is included only if at least one such UnitType exists. Facility filtering and totalItems are computed server-side before pagination; FE MUST NOT fetch an unfiltered Facility page and filter it after pagination.

With no UnitType/range filters, CAT-001 remains a general ACTIVE-Facility browse, including Facilities whose units are currently full. Existence of matching UnitTypes is required when the user supplies UnitType/range filters. No customer-specific price/Discount, UnitCode, tenant, occupancy history or future booking detail is returned.

### CAT-002 — Public Active Facility Detail

```text
GET /api/v1/facilities/{facilityId}
```

Return `data: FacilitySummary` for an ACTIVE Facility. Absent/INACTIVE Facility -> 404 RESOURCE_NOT_FOUND for public detail. Staff contacts beyond the existing public ContactInfo, internal operation metrics and physical-unit/access details are excluded. Use CAT-003 for offered UnitTypes; the existing Facility data schema is preserved.

### CAT-003 — Offered UnitTypes / Requested-Period Capacity

```text
GET /api/v1/facilities/{facilityId}/unit-types?view=public
    &mode=PRIVATE&size=Medium&startMonth=2026-11&endMonth=2027-01
    &availableOnly=true&page=1&pageSize=20
```

- Return paginated `UnitTypeAvailability` using existing eligible-capacity logic. An offered UnitType is linked through existing StorageUnits at the target Facility; offering does not require capacity > 0 unless `availableOnly` is true.
- Public Facility must be ACTIVE. Absent/INACTIVE Facility -> 404 RESOURCE_NOT_FOUND; preserve 409 FACILITY_INACTIVE where a protected operation requires activation.
- Accept optional `mode`, `size`, `unitTypeId`, month pair and pagination with CAT-001 semantics. With no range the two availability fields are null. Operational period reads keep their existing valid-month rules and do not acquire the public future-booking constraint.
- `availableOnly` defaults false for catalog browsing; true requires both months and includes only UnitTypes with positive full-period availableCapacity. Invalid boolean or true without a range -> 400 VALIDATION_ERROR.
- A Facility search-result expansion MUST pass the same UnitType filters/months as CAT-001 and `availableOnly=true` when a range was searched. It MUST NOT show non-matching/full UnitTypes as available search results. If capacity changes between calls, show updated/empty results and refresh; search is not a hold.
- Public list ordering is name ascending then UnitTypeId; supported sortBy values are `name` and `rentalPrice`, with common sortDirection; apply filters before pagination/counting.
- `view` defaults public. Operational view applies section 13.11.1, including same-Facility employee authorization before querying; no anonymous operational fallback. Invalid view -> 400 VALIDATION_ERROR.

### CAT-004 / CAT-005 — Public UnitType Detail / Shared Size Guide Catalog

```text
GET /api/v1/unit-types/{unitTypeId}?view=public
GET /api/v1/unit-types?mode=PRIVATE&page=1&pageSize=20
```

CAT-004 returns `data: Public UnitTypeSummary`. Public detail must belong to the offered catalog: a UnitType linked through existing StorageUnits to at least one ACTIVE Facility. Absent or non-public UnitType -> 404 RESOURCE_NOT_FOUND. Explicit operational view preserves the original authenticated role contract; invalid view -> 400 VALIDATION_ERROR.

CAT-005 returns a paginated, deduplicated collection of the same summary; it supports optional mode/size/unitTypeId filters and name/rentalPrice sorting as CAT-003. Each UnitType appears once even if offered at multiple Facilities. It does not assert current stock or accept a month range; use CAT-001/003 for location/period availability. No UnitType status or separate FacilityUnitType table is added. Sizing guidance uses existing size/description/price data; public access does not add dimensions/amenities to the persisted model.

### PUB-POL-001 / PUB-POL-002 — Current Public Policies

```text
GET /api/v1/public/policies?page=1&pageSize=20
GET /api/v1/public/policies/{policyKey}
```

Read-only projection for these Help Center topics, in the listed order:

```text
reservation-cancellation
identity-verification
deposit-payment-refund
storage-access-handover-contract
facility-rules-prohibited-items
account-privacy
```

PUB-POL-001 returns paginated PublicPolicySummary; PUB-POL-002 returns PublicPolicyDetail, with schemas in section 13.10.3b. No current ACTIVE Policy -> 404 RESOURCE_NOT_FOUND for either endpoint. With a current Policy, the list includes only published topics (possibly empty); unknown/unpublished policyKey or missing article content -> 404 RESOURCE_NOT_FOUND for detail. Never fabricate a policy version, success article or threshold. All six required GWP-04 topics must have approved published content before acceptance can pass.

Article text is reviewed/version-controlled deployment content, exposed through a Business-owned read service/provider and API response mapping. Current numerical thresholds/version/effective dates are sourced through the existing Policy service/repository. Use a single current Policy snapshot per response; no new table, content-editor CRUD endpoint, policy approval workflow or historical-publication capability is introduced. Explanatory copy must be finalized by the team before publishing; SAMPLE POLICY, OTP/Google/password-reset promises, arbitrary fee/currency values and unsupported prohibited-item rules are not authoritative content.

Current public policy describes new rentals. Existing Reservation/Contract rules remain governed by captured PolicyId, accessible only through authorized private-resource APIs. A policy topic is not a persisted Policy row and the public API does not return BOM-008 policy history, CustomerId, captured Contract terms or internal management fields.

Rental-process guidance (How It Works), general contact information and non-numerical sizing explanations are public informational content consistent with this SRS. They require no additional domain API, persisted entity or CMS workflow. Their presentation is specified separately by the frontend owner. Public contact information does not authorize creating a SupportTicket.

### AI-001 — Shared Optional AI Size Guide

```text
POST /api/v1/ai/unit-type-recommendations
```

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

Anonymous and Customer use the same request/response contract; no CustomerId/ContractId/history is inferred or required. Input must be non-empty and bounded by documented DTO validation. AI receives only submitted description/mode and public offered UnitType metadata. Output MUST be validated against CAT-005's public offered UnitTypes and requested mode; it cannot expose hidden operational/master data. If provider fails:

```text
EXTERNAL_PROVIDER_UNAVAILABLE -> 503
```

Anonymous/Customer can continue manual selection. AI-001 remains a stateless recommendation capability; this revision requires neither a specific conversation interface nor persisted ChatSession/message history or a new AI workflow. No provider/model/session identifier or secret is accepted as a domain field. Existing provider isolation remains in force; public abuse controls and their environment limits must be documented during implementation, without inventing a business payment obligation or numeric SLA.

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

### CON-001 — Authorized Contract List (SRS OD-08)

```text
GET /api/v1/contracts?page=1&pageSize=20
Roles: CUSTOMER, FACILITY_STAFF, FACILITY_MANAGER, BUSINESS_OPERATIONS_MANAGER
Success: 200 ContractListResponse (data + pagination)
```

- For CUSTOMER, filter by the authenticated CustomerId; for FACILITY_STAFF and FACILITY_MANAGER, resolve the current assigned Facility from the authenticated Employee and filter Contract.FacilityId; for BUSINESS_OPERATIONS_MANAGER, list system-wide. Do not accept client FacilityId/CustomerId as an authorization shortcut.
- Apply the ownership/Facility predicate to both SQL COUNT and sorted rows before pagination. Use `page >= 1`, `1 <= pageSize <= 100`, defaults 1/20, with the existing deterministic sort (ACTIVE first, EndMonth descending, ContractId tie-break). Existing `ContractSummaryResponse` includes contractId, facilityId, customerId, storageUnitId, unitCode, unitTypeId/name/mode, startMonth, endMonth and status. It does not publish CCCD or full Customer details.
- No assignment -> 403 FORBIDDEN; unauthenticated -> 401; invalid pagination -> 400; unrelated Staff Facility rows must never be returned. GET is read-only. This changes only CON-001 authorization, not Contract mutation or the restrictions on RES-002 and VIS-003. FE Staff list implementation is owned separately.

### CON-002 — Customer / Operational Contract Detail

```text
GET /api/v1/contracts/{contractId}
Roles: CUSTOMER, FACILITY_STAFF, FACILITY_MANAGER, BUSINESS_OPERATIONS_MANAGER
Success: 200 ApiResponse<ContractDetail>
```

Customer reads an owned Contract; Staff/Manager read a Contract in the actor's assigned Facility; BOM may read system-wide. CON-001 is a server-filtered, paginated `ContractSummaryResponse` list for Customer/Staff/Manager/BOM as defined above; CON-002 returns the `ContractDetailResponse` projection. Apply authoritative ACTIVE-account and resource/Facility checks, not a client-supplied customerId or facilityId. Nonexistent detail -> 404 RESOURCE_NOT_FOUND; wrong Staff/Manager detail Facility -> 403 FORBIDDEN.

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
- For a RETURN_VISIT already physically confirmed, the work item may include `actualReturnDate` (`YYYY-MM-DD`) and `inspectionId` (UUID). Both properties are omitted when null and for unrelated work types. A confirmed Visit can remain `CHECKED_IN`; clients must not offer Confirm Return again when `actualReturnDate` exists. The `inspectionId` allows navigation to the precise existing Inspection without extra per-row requests. These read-only properties do not alter Visit state, authorization, or return processing.

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
- Only the explicit public allowlist may execute without an account. Private list/count/detail/command requests without authentication return 401 UNAUTHORIZED before any personal data is read/returned; authenticated role/Facility/ownership denials retain the existing contract.
- Public catalog, policy and AI DTOs MUST NOT accept CustomerId or expose tenant identity, UnitCode/access credentials, personal Discount/history or private resource references. Anonymous draft/search is not a database business command.

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


---

# 13A. V11 API Additions — Approved Scope and Provisional Reads

`IMPL-*` is a reconciliation label for endpoints observed in GitHub `main` at the anchored commit. **Approved by SRS §0.0.9:** handover assignment (`IMPL-HOA-01..03`), Administrator read-only RBAC for `IMPL-UNT-01..03` and Customer sent-notification/Payment history (`IMPL-CUSACT-01..02`, OD-06/07). **Observed and acknowledged:** UnitType public media projection and Admin deactivation-eligibility **501 stub**. Other supplemental reads remain provisional pending separate review. No endpoint is release-certified without tests.

## 13A.1 Owner-approved Manager handover-unit assignment (`IMPL-HOA-01..03`)

**Source:** `backend/Frms.Api/Controllers/HandoverUnitAssignmentsController.cs`, `backend/Frms.Api/DTOs/Requests/HandoverUnitAssignmentRequests.cs`, `backend/Frms.Api/DTOs/Responses/HandoverUnitAssignmentResponses.cs`, `backend/Frms.Business/Services/Implementations/HandoverUnitAssignmentService.cs`, and `backend/sql/postgresql/HandoverAssignments.sql`.

| Approved V11 ID | Method / route | Approved role | Purpose |
|---|---|---|---|
| `IMPL-HOA-01` | `PUT /api/v1/reservations/{reservationId}/handover-unit-assignment` | FACILITY_MANAGER | Assign or replace the selected StorageUnit |
| `IMPL-HOA-02` | `GET /api/v1/reservations/{reservationId}/handover-unit-assignment` | FACILITY_MANAGER, FACILITY_STAFF | Read active selection; may be null |
| `IMPL-HOA-03` | `DELETE /api/v1/reservations/{reservationId}/handover-unit-assignment` | FACILITY_MANAGER | Release active selection; preserve history |

### Assignment request and response

**PUT request (observed `AssignHandoverUnitRequest`):**

```json
{"storageUnitId": "00000000-0000-0000-0000-000000000001"}
```

**PUT / GET non-null success `data` (observed `HandoverUnitAssignmentResponse`; UUID/time values illustrative):**

```json
{
  "data": {
    "assignmentId": "00000000-0000-0000-0000-000000000002",
    "reservationId": "00000000-0000-0000-0000-000000000003",
    "storageUnitId": "00000000-0000-0000-0000-000000000001",
    "unitCode": "A-101",
    "locationInfo": null,
    "assignedByEmployeeId": "00000000-0000-0000-0000-000000000004",
    "assignedAt": "2026-10-20T03:00:00Z",
    "status": "ACTIVE"
  }
}
```

`PUT` returns HTTP 200; `GET` returns HTTP 200 with `data: null` when no active assignment; `DELETE` returns HTTP 204 with no response body, according to the pinned controller source. `assignedAt` is UTC; the exact OpenAPI shape and negative-path status mapping remain release-test obligations.

**Server invariants evidenced in SQL:** Manager must be an ACTIVE employee in the Reservation Facility. Reservation must be CONFIRMED with no Contract; its RESERVATION Visit must be CHECKED_IN. Unit must exist, match Facility and UnitType, and be AVAILABLE. Only one `ACTIVE` assignment is allowed per Reservation and StorageUnit. Reassignment closes prior selection (`REPLACED`). Unassign closes it (`RELEASED`). Handover Contract insertion requires matching `ACTIVE` selection and consumes it (`CONSUMED`) atomically. Duplicate same-unit assignment is idempotent according to current SQL. Manager selection is not capacity hold and is not Contract occupancy.

**Error examples from implementation:** `FORBIDDEN` (403), `RESOURCE_NOT_FOUND` (404), `RESERVATION_INVALID_STATUS` (409), `VISIT_INVALID_STATUS` (409), `UNIT_FACILITY_TYPE_MISMATCH` (409), `UNIT_NOT_AVAILABLE` (409), and `HANDOVER_ASSIGNMENT_NOT_FOUND` (409). SQL includes distinct guard errors `HANDOVER_ASSIGNMENT_REQUIRED` / `HANDOVER_UNIT_MISMATCH` for the Contract creation path.

**Owner-approved V11 scope:** Additional persisted `HandoverUnitAssignment` entity, four statuses, checked-in prerequisite and Manager/Staff endpoint RBAC are approved by SRS §0.0.9. **Pending acceptance, not approval:** generated OpenAPI/DTO parity, observed response status codes, Staff mutation denial, Facility isolation, PostgreSQL concurrency/idempotency/guard rollback and E2E handover. Do not implement a Reservation `StorageUnitId` shortcut.

## 13A.2 StorageUnit status timeline (`IMPL-UNT-01..03`)

**Source:** `backend/Frms.Api/Controllers/StorageUnitTimelineController.cs` and migration `Pg010_StorageUnitStatusTimelineAudit`.

| Provisional ID | Method / route | Observed controller roles |
|---|---|---|
| `IMPL-UNT-01` | `GET /api/v1/facilities/{facilityId}/storage-units/timeline?month=YYYY-MM&page=1&pageSize=20` | FACILITY_MANAGER, BUSINESS_OPERATIONS_MANAGER, SYSTEM_ADMINISTRATOR |
| `IMPL-UNT-02` | `GET /api/v1/facilities/{facilityId}/storage-units/{storageUnitId}/timeline?month=YYYY-MM` | Same |
| `IMPL-UNT-03` | `GET /api/v1/facilities/{facilityId}/unit-types/{unitTypeId}/storage-units/timeline?month=YYYY-MM&page=1&pageSize=20` | Same |

**Observed fields:** `storageUnitId`, `unitCode`, `unitTypeName`, `facilityName`, `locationInfo`, `intervals[{status,from,to}]`. Paginated endpoints use `data[]` and `pagination` envelope. These are **AuditLog-derived reads**, not an independently persisted status-history entity. Migration Pg010 records CREATE_STORAGE_UNIT / CHANGE_STORAGE_UNIT_STATUS audit events, without backfilling past events.

**Owner-approved V11-OD-03 RBAC:** Facility Manager **own Facility only**; BOM and `SYSTEM_ADMINISTRATOR` **system-wide** for **these three read-only timeline GETs only**. No Administrator Unit mutations or other operational/financial access is granted. `month` filtering, missing/partial AuditLog history, scope checks, ordering and no-data behavior require API/SQL/security tests.

## 13A.3 Customer activity reads (`IMPL-CUSACT-01..02`)

**Source:** `backend/Frms.Api/Controllers/CustomerActivityController.cs` and `backend/Frms.Api/DTOs/Responses/CustomerActivityResponses.cs`.

| ID / approval | Method / route | Access |
|---|---|---|
| `IMPL-CUSACT-01` — **APPROVED SRS OD-06** | `GET /api/v1/customers/me/notifications?page=1&pageSize=20` | Authenticated ACTIVE CUSTOMER, own UserAccount only |
| `IMPL-CUSACT-02` — **REQUIREMENTS APPROVED SRS OD-07** | `GET /api/v1/customers/me/transactions?page=1&pageSize=20` | Authenticated ACTIVE CUSTOMER, own Customer's Payment evidence only |

`notifications`: derived from existing `NotificationLog`. **Actual `CustomerNotificationResponse` property name is `notificationId`** (not `notificationLogId`), followed by `notificationType`, `content`, `createdAt` and `sentAt`. The controller maps the persisted `NotificationLogId` to DTO field `NotificationId`; both time fields are non-null `DateTimeOffset` in the pinned DTO. This is delivery history, **not** a read/unread inbox or additional notification workflow.

**Approved notification contract — `REV-2026-10-22-CUSTOMER-NOTIFICATIONS-READ`:** SRS §0.0.9 OD-06 authorizes only `IMPL-CUSACT-01`. Server scope derives from the authenticated ACTIVE Customer account, never request IDs. Return only own `SENT` rows with non-null `sentAt`, ordered by `createdAt DESC`, then `notificationId DESC`. Pagination uses the standard `data[]`/`pagination` envelope, defaults 1/20 and max pageSize 100; existing overflow-safe page bounds remain enforced. Zero results return `data: []` with totalItems/totalPages zero. Beyond the last page returns an empty data array with the requested page and actual totals. Invalid pagination -> HTTP 400 `VALIDATION_ERROR`; unauthenticated -> 401 `UNAUTHORIZED`; wrong role/inactive or missing active Customer -> 403 `FORBIDDEN` under the existing controller/service guards. Errors retain the standard code/message/traceId shape. No new error code or mutation endpoint is introduced.

`notificationType` and `content` are strings, not a newly approved type enum or HTML. The response has **no** unread flag/count, resource IDs, action URL or title field. FE must render plain text, use a generic presentation for unknown types and must not infer links/state from content. Notifications page reads pageSize 20; Overview reads page 1/pageSize 3 from the same endpoint. Neither rendering nor navigating writes read state, retries delivery or triggers financial/business commands. Initial credential content is excluded from ordinary NotificationLog usage under the existing security requirements. Notification IDs are internal keys, not Customer-facing references.

**Approved Transaction History contract — `REV-2026-10-22-CUSTOMER-TRANSACTION-HISTORY-READ`:** SRS §0.0.9 OD-07 approves `IMPL-CUSACT-02` as an own-history GET, not a payment command, wallet or settlement ledger. The endpoint takes only standard pagination parameters; derive the ACTIVE Customer from the authenticated account, never from a caller-supplied Customer/account ID. Scope before counting/paging through Payment -> Invoice -> the actor's Reservation (`DEPOSIT`) or Contract (`RENTAL_FEE`, after the offline first month). No Guest/Employee access to this Customer endpoint is introduced.

- Return one row per actual Payment attempt, with `paymentId`, `invoiceId`, `invoiceType`, `amount`, `paymentMethod`, `status`, `paidAt`, `createdAt` exactly as in the existing `CustomerPaymentHistoryResponse`. `invoiceType` is `DEPOSIT` or `RENTAL_FEE`; `status` is `PENDING`, `SUCCESS` or `FAILED`. Multiple attempts for one Invoice remain separate. An Invoice with no Payment yields no history row. `PENDING` is not proof of received money; FE must not sum attempts as collected funds, calculate financial amounts or derive Invoice/business transitions from this projection.
- `amount` is a server-provided decimal. `createdAt` is non-null UTC; `paidAt` is nullable UTC and must not be fabricated when absent. FE displays times in GMT+7. Preserve actual historical method evidence, including MOMO/VNPAY if present; this does not re-enable retired-gateway writes or add a payment method.
- Use the standard `data[]`/`pagination` collection envelope, defaults `page=1&pageSize=20`, max `pageSize=100`, existing overflow-safe page bounds. Order `createdAt DESC`, then `paymentId DESC`. Zero results return an empty array and zero totals; beyond the last page returns an empty array with the requested page and actual totals. Invalid pagination -> 400 `VALIDATION_ERROR`; unauthenticated -> 401 `UNAUTHORIZED`; wrong role/inactive or missing active Customer -> 403 `FORBIDDEN` under the existing guards. Errors retain code/message/traceId; no new error code is introduced.
- FE shows purpose/type, amount, method, explicit status, creation/paid time and **View Invoice** using the returned `invoiceId`, via `/customer/invoices/{invoiceId}` and existing owner-protected `BIL-002`. Payment/Invoice IDs are internal keys/targets, not primary display labels. Opening/paging/refreshing the shared Transaction History tab or following View Invoice must not create a payment, retry a failed attempt, mark paid, transfer a refund or change business state.
- Do not synthesize first-month Invoice/Payment records from offline handover or Contract creation. Do not project Damage/ExtraFee/DepositSettlement deductions, refund-transfer claims, wallet balances or Figma sample amounts as transactions without an approved matching transaction-evidence/API contract. The current response has no deduction/refund/balance fields; Fees & Return Tracking remains a separate read.

**Requirement approval is complete for this read.** Frontend implementation, generated OpenAPI parity, live ownership/security tests and real database integration remain separately tracked acceptance work. Supplemental directories/RBAC, billing backfill/scheduling, providers and other pending decisions are unchanged by OD-07. This documentation task changes no controller/DTO, frontend model, database or financial routine.

**Observed response DTO examples (values illustrative, actual field names from pinned source):**

```json
{
  "data": [
    {
      "notificationId": "00000000-0000-0000-0000-000000000005",
      "notificationType": "PAYMENT_RESULT",
      "content": "string",
      "createdAt": "2026-10-20T03:00:00Z",
      "sentAt": "2026-10-20T03:00:01Z"
    }
  ],
  "pagination": {"page": 1, "pageSize": 20, "totalItems": 1, "totalPages": 1}
}
```

The example above is for `/notifications`. `/transactions` has the same collection envelope with the following data-item shape:

```json
{
  "paymentId": "00000000-0000-0000-0000-000000000006",
  "invoiceId": "00000000-0000-0000-0000-000000000007",
  "invoiceType": "DEPOSIT",
  "amount": 2000.0,
  "paymentMethod": "PAYOS",
  "status": "SUCCESS",
  "paidAt": "2026-10-20T03:00:00Z",
  "createdAt": "2026-10-20T02:55:00Z"
}
```

Example values do not assert that these exact records exist; they illustrate the observed DTO properties. The runtime OpenAPI, authorization and actual database result remain unverified.

The controller comment predates OD-06/07 and still describes both reads as proposals. **Documentation/code-comment drift only:** Notifications requirements are approved under OD-06 and Transaction History requirements under OD-07. Runtime OpenAPI, live database output, ownership/security and delivery tests remain unverified by these approvals. OD-07 is a scoped requirements-documentation decision, not completed frontend implementation, backend modification or provider activation.

## 13A.4 Supplemental RBAC directories / operational lookup

**Verified controller routes from `main`; roles listed are controller-level observations, not approval of expanded business scope:**

| Provisional ID | Method / route | Observed role / concern |
|---|---|---|
| `IMPL-RBAC-01` | `GET /api/v1/admin/facilities/lookup` | Admin facility assignment read; verify minimal scope |
| `IMPL-RBAC-02` | `GET /api/v1/admin/customers/{customerId}/deactivation-eligibility` | **NOT IMPLEMENTED**: route exists under Administrator guard but returns HTTP **501** `ENDPOINT_NOT_IMPLEMENTED` (`UX-RBAC-11`) |
| `IMPL-RBAC-09` | `GET /api/v1/facilities/{facilityId}/staff/lookup?page=1&pageSize=20` | FACILITY_MANAGER, Staff assigned to the authorized Facility (active-only filtering must be verified) |
| `IMPL-RBAC-12A` | `GET /api/v1/facilities/{facilityId}/customers?page=1&pageSize=20` | FACILITY_STAFF / FACILITY_MANAGER, scoped customers with Reservation/Contract |
| `IMPL-RBAC-12B` | `GET /api/v1/facilities/{facilityId}/customers/{customerId}` | Same Facility only; minimal customer operational detail |
| `IMPL-RBAC-14A` | `GET /api/v1/business/customers?page=1&pageSize=20` | BUSINESS_OPERATIONS_MANAGER, system customers read |
| `IMPL-RBAC-14B` | `GET /api/v1/business/customers/{customerId}` | BUSINESS_OPERATIONS_MANAGER, minimal Customer detail |
| `IMPL-RBAC-16A` | `GET /api/v1/business/employees?page=1&pageSize=20` | BUSINESS_OPERATIONS_MANAGER, minimal Employee directory |
| `IMPL-RBAC-16B` | `GET /api/v1/business/employees/{employeeId}` | BUSINESS_OPERATIONS_MANAGER, minimal Employee detail |

**Admin deactivation-eligibility status:** `IMPL-RBAC-02` is a **scaffold**, not a functioning API. `AdminSupplementController.CustomerDeactivationEligibility()` calls `ScaffoldNotImplemented("UX-RBAC-11")`, which returns HTTP 501 with stable code `ENDPOINT_NOT_IMPLEMENTED`. Frontend MUST NOT depend on this endpoint or label it implemented; existing `ADM-004` deactivation guard is independent. A separate feature task is required to implement it.

Required acceptance for other candidate reads: RBAC 401/403/404, Facility/owner enforcement at repository level before pagination, no credentials/password hashes, no cross-facility leakage, no forbidden CCCD projection, query limits, ordering/pagination and Swagger/FE contract parity. Administrator ability to view the *timeline exception* does not grant BOM Facility-management command access.

## 13A.5 UnitType media projection (`IMPL-MEDIA-01`)

**Source:** migration `Pg012_UnitTypeMediaUrls.cs`; adds two nullable columns on UnitType:

```text
ImageUrl: varchar(2048) NULL
VideoUrl: varchar(2048) NULL
```

**Verified code observation and owner acknowledgement (V11-OD-05):** Public `CAT-003`/`CAT-004`/`CAT-005` response mappings already include `imageUrl: string | null` and `videoUrl: string | null`. Evidence: `UnitTypeAvailabilityResponse`, `UnitTypeSummary`, `CatalogResponseMapping`, `CatalogController`, `PublicAccessController`. These are optional nullable fields, not a new media-management command. Do not include unverified external URLs, private resources or user-generated content without validation. **Still pending:** actual generated Swagger comparison, null serialization behavior, URL/content validation, FE rendering/fallback and privacy/security tests.

## 13A.6 Release acceptance checklist for API V11

- [x] Owner approved `IMPL-HOA-01..03` roles, 28th entity/four states, and Administrator read-only `IMPL-UNT-01..03` scope (SRS §0.0.9; **documentation decision only**, not testing).
- [x] Owner approved `IMPL-CUSACT-01..02` Customer sent-notification/Payment-history requirements (SRS OD-06/07); this checks requirement approval only, not implementation/tests.
- [ ] Resolve remaining provisional `IMPL-*` directory scopes; verify public media and Admin 501 notes remain accurate.
- [ ] Verify approved Customer activity DTO/pagination/order, account/owner guards, nullable paid time, View Invoice, UI states and absence of fabricated financial records or write side effects against implementation and real integration/security tests.
- [ ] Confirm `notificationId` (not `notificationLogId`) and full HandoverUnitAssignment DTO fields against generated OpenAPI, not illustrative guesses.
- [ ] Every listed route matches actual current controller attributes and generated OpenAPI.
- [ ] Request/response DTOs, required/nullable fields, status codes and error envelopes validated.
- [ ] 401/403/404, cross-facility and cross-customer negative tests pass.
- [ ] Handover assignment SQL race/unique/rollback tests pass (PostgreSQL 17+).
- [ ] Audit timeline correctness and historical no-backfill behavior tested.
- [ ] Registration / AUTH-005 optional nullable Customer.address persistence and clearing (no mandatory input), forbidden cross-role projection, AUTH-004 private CCCD and frontend fallback tests pass.
- [ ] Integration tests prove payOS idempotency and later-month Rental Fee compatibility.
- [ ] No unrelated feature or unapproved Staff/Admin mutation path becomes accessible.

## 13A.7 Source references

- [Governing SRS on main](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/main/docs/FRMS_SRS_V10.md)
- [Handover assignment controller](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/main/backend/Frms.Api/Controllers/HandoverUnitAssignmentsController.cs)
- [Handover SQL routines](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/main/backend/sql/postgresql/HandoverAssignments.sql)
- [StorageUnit timeline controller](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/main/backend/Frms.Api/Controllers/StorageUnitTimelineController.cs)
- [Customer activity controller](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/main/backend/Frms.Api/Controllers/CustomerActivityController.cs)
- [DataAccess configuration](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/main/backend/Frms.DataAccess/DependencyInjection/DataAccessServiceCollectionExtensions.cs)


## 13A.8 Companion synchronization and implementation-evidence boundary

| Concern | Owner source within this V11 package | Acceptance boundary |
|---|---|---|
| Business actors/financial and lifecycle invariants | `FRMS_SRS_V11.md` §§9–12, 17 and 27 | V10 approval remains in force until V11 ratification |
| Exact HTTP routes/method/roles and request/response wire format | This API file §13.11–13.27 and §13A | Existing V10 contract retained; §13A scopes are approved only where expressly recorded in SRS §0.0.9, including OD-06/07 for Customer activity |
| Schema/migrations/SQL implementation | [`FRMS_Data_Dictionary_V11.md`](FRMS_Data_Dictionary_V11.md) + source at `main` current review HEAD `0219f30734b0` (earlier V11 baseline `90a6c5982d42`) | V11 data target and implementation evidence; no deploy/DBT pass claimed |
| Optional Customer.Address | **Latest owner decision SRS §0.0.9 supersedes historical V10 §0.0.7** | AUTH-001/005 optional input and persistence accepted; AUTH-004 own `cccd` still missing in BE |
| Handover assignment | This file §13A.1; SRS §§0.0.9, 9.2A | **Owner-approved** 28th entity and 3 APIs; PostgreSQL/E2E tests pending |
| Unit timeline and Admin scope | This file §13A.2; SRS §§0.0.9, 11A | **Owner-approved** read-only Administrator timeline; negative auth tests pending |
| Customer activity | This file §13A.3 | Notifications requirements approved by SRS OD-06; Transaction History requirements approved by OD-07; implementation/testing separate; controller comment drift; no read/unread inbox or fabricated financial ledger implied |
| Admin deactivation-eligibility | This file §13A.4 | **NOT IMPLEMENTED** (HTTP 501) despite existing route; separate feature required |
| Public UnitType media URL | This file §13A.5 | **Already emitted in public DTO mapping**, owner acknowledged; validation/Swagger/FE tests pending |

**No implied parity:** These three documents were editorially synchronized and checked for self-consistency. They were **not** generated from a running Swagger endpoint and do **not** certify controller/DTO coverage, NUnit, SQL migrations or Playwright tests. See SRS V11-G07–G09.


## 13A.9 Data Dictionary V11 cross-contract traceability

| API / requirement | Data Dictionary V11 authority | Observed/release status |
|---|---|---|
| AUTH-001, AUTH-005 | §4.3 Customer.Address nullable varchar(1000), Customer.CCCD unique when present; §12.4 | Owner-approved optional address, backend request accepted; negative PII tests not run |
| AUTH-004 | §4.3 nullable CCCD; §12.4 | Customer private `profile.cccd: string|null` target; backend DTO still missing field |
| IMPL-HOA-01..03; OPS-004 | §4.28, §6–8, §29, §32 Pg013 | Owner-approved Manager assignment/read/release + Staff atomic consume; DB concurrency tests pending |
| IMPL-UNT-01..03 | §29 Pg010; §37 roles | Owner-approved Admin/BOM system-wide + Manager Facility read-only; no synthetic history |
| CAT-003/004/005 | §4.6, §32 Pg012 | Public nullable media DTOs observed; Swagger/URL governance validation pending |
| IMPL-RBAC-02 | §37, no database entity | Admin deactivation-eligibility is HTTP 501 scaffold, not released functionality |
| PAY-001/003/004 | §4.12–13, §13, §23 | payOS only for Deposit/later Rental Fee, global UUID key and verified PaidAt, test evidence pending |
| PAY-002 | §12.1 retired | No first-month online Payment or Invoice |

This API crosswalk does not authorize any `IMPL-*` API not explicitly approved in SRS §0.0.9. The latest GitHub `main` controller source is evidence, not a substitute for generated OpenAPI or application/runtime tests.


## 13A.10 BackgroundJobs integration — internal workers, **no new HTTP endpoints** (2026-10-21)

**Boundary:** SRS V11 §9.11 and Data Dictionary V11 §§9/27 define the six scheduled business tasks. Source `main` at `0219f30734b00b3e5527e3453b19840f08f63639` registers four `IHostedService` implementations. **These are internal execution components, not customer/admin APIs.** Do not add invented `POST /jobs/*`, `GET /jobs/status`, manual trigger, webhook or billing mutation routes to the HTTP API inventory. Any such route needs a separate SRS change/authorization contract.

| Internal task | Trigger in current code | Data affected | Existing HTTP surface reading the persisted result |
|---|---|---|---|
| `job_ExpirePendingReservations` | `ExpirePendingReservationsJob`, startup + 10 minutes | Expired Deposit Invoice and Reservation become `CANCELLED` | `GET /api/v1/reservations`, `GET /api/v1/reservations/{reservationId}`, `GET /api/v1/invoices/{invoiceId}` (authorized views) |
| `job_GenerateMonthlyInvoices` | `BillingMaintenanceJob` step 1, startup + 24 hours | `Invoice(RENTAL_FEE, UNPAID)` for eligible current month only | `GET /api/v1/invoices`, `GET /api/v1/invoices/{invoiceId}`, `GET /api/v1/contracts/{contractId}/billing` |
| `job_MarkOverdueInvoices` | `BillingMaintenanceJob` step 2 | Eligible `Invoice.Status=OVERDUE` | Same `BIL-001`, `BIL-002`, `CON-004` reads |
| `job_RecalculateOpenLateFees` | `BillingMaintenanceJob` step 3 | Insert/update one `LateFee` per eligible overdue Invoice | `GET /api/v1/contracts/{contractId}/billing` (`CON-004`) |
| `job_ProcessReservationNoShow` | `ProcessReservationNoShowJob`, startup + 24 hours when enabled | Eligible Reservation/Visit states after Policy window | Existing authorized `RES-002/003`, `VIS-003/004` reads |
| `job_RetryNotifications` | `RetryNotificationsJob`, startup + 5 minutes when enabled; one pending item/tick | `NotificationLog.SENT/SentAt` **only after provider send succeeds** | Provisional private `GET /api/v1/customers/me/notifications` (only `SENT` records) |

**Current enabled flags (source default):** `BackgroundJobs:Billing:Enabled = true`; `BackgroundJobs:NoShow:Enabled = false`; `BackgroundJobs:Notifications:Enabled = false`. Existing expiration always runs while the API host is running. Environment overrides use `BackgroundJobs__Billing__Enabled` etc. **Timing is interval-from-start, not fixed local 00:00.** The job is *registered* in `main` but its production execution, database migration state, monitoring and full test suite are not verified here.

**Billing/customer behavior:** Scheduled creation never produces an Invoice for `Contract.StartMonth`: first month is an offline Staff acknowledgment under OPS-004. New `RENTAL_FEE` invoices are payable by **existing** `POST /api/v1/invoices/{invoiceId}/payments/payos` (`PAY-001`) with the same ownership, idempotency and verified webhook constraints as any other eligible invoice. Job execution MUST NOT silently initiate payment, set Invoice `PAID` or invent Provider references. Customer UI must display persisted state; `CON-004` does not create/reprice an Invoice merely because it is read.

**Notification/API boundary:** PostgreSQL `usp_RetryPendingNotification` selects one queued `PENDING` notification, while a configured `INotificationSender.SendAsync` must actually deliver it. `usp_MarkNotificationSent` is called only after success. Current `UnconfiguredNotificationSender` does not deliver, so the notification job is not operational out of the box. The existing Customer notification GET must neither expose pending unsent content nor provide a public retry endpoint. Preserve Customer authorization/ownership and avoid logging PII.

**Acceptance / known gaps:** (1) generated Swagger route/DTO parity NOT VERIFIED; (2) BackgroundJob registration-only NUnit test does not prove execution; (3) real PostgreSQL first/subsequent month, policy threshold, idempotency and multi-instance DBT NOT RUN; (4) current-month generator does not auto-backfill past missed months; (5) 24-hour schedule and alerting/SLA require validation; (6) NoShow default-off with unchecked boundary conditions; (7) delivery provider and retry/deduplication tests pending. See SRS `V11-G10..G13` and Data Dictionary §§27.1–27.4.

**Source anchors:**

- [BackgroundJob DI registration](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/0219f30734b00b3e5527e3453b19840f08f63639/backend/Frms.Api/BackgroundJobs/BackgroundJobRegistration.cs)
- [Hosted services and default flags](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/0219f30734b00b3e5527e3453b19840f08f63639/backend/Frms.Api/BackgroundJobs/RecurringBackgroundJobs.cs)
- [Business job orchestration](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/0219f30734b00b3e5527e3453b19840f08f63639/backend/Frms.Business/Services/Implementations/RecurringJobsService.cs)
- [PostgreSQL calls and notification delivery lease](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/0219f30734b00b3e5527e3453b19840f08f63639/backend/Frms.DataAccess/Repositories/Implementations/RecurringJobsRepository.cs)
- [Billing routines](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/0219f30734b00b3e5527e3453b19840f08f63639/backend/sql/postgresql/Billing.sql) / [job routines](https://github.com/TruongGiangNgtr/SWP391_Self-Storage-Facility-Rental-and-Management-System/blob/0219f30734b00b3e5527e3453b19840f08f63639/backend/sql/postgresql/Jobs.sql)


## 13A.11 V11 GitHub review and model consistency — 2026-10-21

- Publication set: `FRMS_SRS_V11.md` (business authority), `FRMS_API_V11.md` (HTTP wire), `FRMS_Data_Dictionary_V11.md` (28-entity physical mapping). All three are review-candidate documents; their filenames and relative links are synchronized.
- Current DB-model comparison covers **28 entity classes and 202 fields**; this **does not** imply 202 public API properties or create CRUD endpoints for every field. `Customer.Cccd` maps to DB column `CCCD`, and the private AUTH-004 `profile.cccd` projection remains an outstanding backend gap.
- `PAY-001` creates new `PAYOS` payment attempts only; permitted legacy payment-method codes in database storage/PAY-003 reads do not enable non-payOS checkout endpoints.
- `GET /api/v1/admin/customers/{customerId}/deactivation-eligibility` is still a 501 scaffold. `SYSTEM_ADMINISTRATOR` has no write expansion outside expressly approved endpoints.
- BackgroundJobs are internal hosted services (four services/six tasks; billing default ON), not HTTP endpoints; notification sender remains unconfigured.
- Publication verification excludes running Swagger generation, .NET/NUnit, PostgreSQL migration history/integration/concurrency tests, browser E2E and final owner approval. Mark any such acceptance gate `PENDING` until its own tests and reviews pass.
