# CWP-03 — Storage Reservation Test Log (11/1/2016)

**Project:** SWP391 — Self-Storage Facility Rental and Management System (FRMS)  
**Feature:** CWP-03 — Storage Reservation  
**Primary API:** `POST /api/v1/reservations` (`RES-001`)  
**Execution date:** 2026-11-01  
**Environment:** Local frontend (`http://localhost:5173`), local .NET API (`http://localhost:5164`), PostgreSQL on Supabase (shared/demo environment)  
**Tools:** Postman, Windows PowerShell 5.1, .NET `HttpClient`  
**Tester:** Manual API/UI verification (user-executed; results recorded from supplied responses)

## 1. Scope and Result Summary

| Case ID | Test objective | Result | Evidence / limitation |
|---|---|---|---|
| TC-01 | Create valid Reservation | **PASS** | UI successfully created Reservation and Deposit Invoice; returned `201`, reservation details shown. |
| TC-02 | Required `facilityId` | **PASS** | Omitting property returned `VALIDATION_ERROR`. Public search without facility is separately valid behavior. |
| TC-03 | Required `unitTypeId` | **PASS** | Omitting property returned `VALIDATION_ERROR`. Public search without Storage Type is separately valid behavior. |
| TC-04 | Required `startMonth` | **PASS** | Omitting property returned `VALIDATION_ERROR`. |
| TC-05 | Required `endMonth` | **PASS** | Omitting property returned `VALIDATION_ERROR`. |
| TC-06 | Reject invalid month range | **PASS** | UI prevented end month earlier than start month; API responded HTTP `400`, `INVALID_MONTH_RANGE` for `2027-03` → `2027-02`. |
| TC-07 | Reject Reservation at INACTIVE Facility | **BLOCKED** | Public facility listing returned 63 ACTIVE facilities, no INACTIVE facility available in response. Public endpoint might filter INACTIVE records, so this does **not** establish absence in DB. |
| TC-08 | Reject an existing Unit Type not offered at chosen Facility | **BLOCKED** | At tested Facility all eight system catalog Unit Types were offered. No verified invalid Facility–Unit Type pair. Prior cross-facility attempt used an actually offered Unit Type and is not evidence of a bug. |
| TC-09 | Reject reservation when capacity is zero | **PASS (API)** | Capacity fell from 1 to 0 after a successful reservation; next POST returned `CAPACITY_NOT_AVAILABLE`. HTTP 409 expected, but status was not separately captured in the retained evidence for this case. |
| TC-10 | Concurrent Reservation for last available slot | **PASS (single run)** | Two distinct ACTIVE Customers submitted near-simultaneous POST requests: one `201`, one `409 CAPACITY_NOT_AVAILABLE`; capacity fell from 1 to 0. Winning Reservation and its Invoice were retrieved afterward. |
| TC-11 | Reject missing Authorization | **PASS** | Without Bearer token returned HTTP `401`, `UNAUTHORIZED`. |
| TC-12 | Reject Employee acting as Customer | **PASS (business response)** | Employee JWT returned `FORBIDDEN`; expected HTTP 403, exact status not separately retained. |
| TC-13 | Reject Customer without valid saved CCCD | **PASS (business response)** | Returned `VALIDATION_ERROR` with `errors.cccd` requesting a valid 12-digit National ID. Exact status not separately retained. |
| TC-14 | Ignore client-provided server-owned fields | **PASS (response-level)** | Attempted tampering with `depositAmount`, `lockedRentalPrice`, `status`, `customerId`; API returned server-calculated values and actual authorized Customer ID. No independent database read was performed for this case. |
| TC-15 | Roll back Reservation if Deposit Invoice creation fails | **NOT EXECUTED** | Requires controlled failure injection in isolated PostgreSQL test database. Existing script and some tests target SQL Server; shared Supabase demo database was deliberately left unchanged. |

**Summary:** 12 PASS, 2 BLOCKED, 1 NOT EXECUTED (15 documented cases). PASS labels represent the evidence and limits described in each row; not every case includes DB-level verification.

## 2. Core Test Data

| Field | Value |
|---|---|
| Facility | FStorage Ba Ria - Vung Tau |
| Facility ID | `6fbfaac4-5094-44d7-b1dc-38aae081e3ea` |
| Unit Type | Kho Rieng An Tam (`PRIVATE`, `10x10`) |
| Unit Type ID | `fd247f9b-be97-4171-971c-27f2088571bf` |
| Alternate Unit Type | Ngan Chung Nho Xinh (`PUBLIC`, `5x5`) |
| Alternate Unit Type ID | `46d45763-ecc4-4356-882f-0861e5e2aa8b` |
| Standard API | `POST http://localhost:5164/api/v1/reservations` |
| Valid request body fields | `facilityId`, `unitTypeId`, `startMonth`, `endMonth` |
| Customer A ID | `92c93644-15b5-420b-8648-8440b91f9c8c` |
| Customer B ID | `def68a20-5cb2-49af-a492-4fd851ea86aa` |

**Security:** Access tokens, passwords and full CCCD numbers are intentionally omitted from this report.

## 3. Detailed Execution Notes

### TC-01 — Happy-path Reservation

- Created Reservation using Customer UI; reservation ID: `5e0d872e-3c28-4b14-90a8-227ec44ad598`.
- Period: `2026-12` through `2027-01`.
- HTTP result: `201 Created`.
- Locked rental price: `2000`; deposit: `2000`.
- Reservation status: `PENDING_DEPOSIT`; deposit invoice status: `UNPAID`.
- **Result:** PASS.

### TC-02 to TC-05 — Mandatory POST properties

- Removed each required property independently from JSON payload.
- Each submission returned `VALIDATION_ERROR` identifying the missing property.
- **Result:** PASS (request validation). Note that optional filters in the public search UI/API are not equivalent to mandatory creation fields.

### TC-06 — Invalid month range

- Submitted `startMonth=2027-03`, `endMonth=2027-02`.
- API returned HTTP `400`, `INVALID_MONTH_RANGE`.
- UI also disallowed an earlier checkout month.
- **Result:** PASS.

### TC-07 — INACTIVE Facility

- Queried `GET /api/v1/facilities?page=1&pageSize=100`.
- Response pagination: `totalItems=63`, `totalPages=1`.
- All returned Facility records had `status=ACTIVE`.
- **Result:** BLOCKED. Need an independently verified INACTIVE Facility, preferably in a dedicated test dataset or admin read endpoint. Public results may intentionally exclude inactive locations.

### TC-08 — Existing Unit Type not offered by Facility

- Queried offered Unit Types for Facility `6fbfaac4-5094-44d7-b1dc-38aae081e3ea`, period `2027-06`–`2027-07`.
- The facility's query returned eight Unit Types (`totalItems=8`, `totalPages=1`).
- Queried `GET /api/v1/unit-types`, which likewise returned exactly those eight types.
- Earlier cross-facility POST with Unit Type `46d45763-ecc4-4356-882f-0861e5e2aa8b` succeeded (Reservation `454190d2-7792-47d1-b8e2-95e275f2da77`), but subsequent lookup confirmed this Unit Type **is offered** at the tested Facility.
- **Result:** BLOCKED. No actual mismatch pair was identified. Using a random nonexistent UUID would test `UNIT_TYPE_NOT_FOUND`, not the intended mismatch.
- **Implementation observation, not a confirmed bug:** The PostgreSQL creation routine found in `backend/sql/postgresql/Reservations.sql` verifies Unit Type existence and computes capacity from actual Storage Units. A separate explicit membership check was not observed in the inspected excerpt. Do not conclude the behavior is defective without an isolated test with a real mismatched pair and the agreed API specification.

### TC-09 — Capacity exhausted

- For Unit Type `46d45763-ecc4-4356-882f-0861e5e2aa8b` and period beginning `2026-12`, capacity was observed at `1`.
- A valid POST created Reservation `d9b856d3-60ea-4b3e-90b2-93d950f4f670` and decreased capacity to `0`.
- A subsequent POST returned `CAPACITY_NOT_AVAILABLE`.
- **Result:** PASS at API behavior level; retain status screenshot / DB row count if stricter formal evidence is required.

### TC-10 — Concurrent last-slot request

**Setup:** Two distinct Customer accounts were validated with `GET /api/v1/auth/me` (both `ACTIVE`, `CUSTOMER`, and saved 12-digit CCCD). Capacity was prepared from 4 to 1 using three successful Reservation requests in the demo test period `2027-04`–`2027-05`.

**Execution:** Windows PowerShell 5.1 lacked `Start-ThreadJob`. Two `.NET HttpClient.PostAsync` calls were launched without awaiting the first before starting the second.

| Metric | Actual |
|---|---|
| Available Capacity before | `1` |
| Customer A | HTTP `409`, `CAPACITY_NOT_AVAILABLE` |
| Customer B | HTTP `201` |
| Available Capacity after | `0` |
| Winning Reservation | `45b61bba-5d61-446d-8ca2-cfcee5c31172` |
| Winning Customer ID | `def68a20-5cb2-49af-a492-4fd851ea86aa` |
| Period | `2027-04`–`2027-05` |
| Reservation Status on GET | `PENDING_DEPOSIT` |
| Locked Rental Price | `2000.00` |
| Deposit Amount | `2000.00` |
| Linked Invoice ID | `0353d981-e02e-4bfd-aed5-c0ca25f36bf1` |
| Invoice Status | `UNPAID` |
| Invoice Amount Due | `2000.00` |
| Created At | `2026-11-01T06:59:49.201+00:00` |
| Deposit Due At | `2026-11-01T07:59:49.201+00:00` |

**Result:** PASS for this execution. Verified that the successful Reservation could be retrieved and includes a linked Deposit Invoice. This single near-concurrent run and API-level read do not prove exhaustive race safety or independent DB invariants. Pending deposits may auto-expire and restore capacity later.

### TC-11 and TC-12 — Authorization

- TC-11: POST with no JWT → HTTP `401`, `UNAUTHORIZED` → PASS.
- TC-12: POST with Employee JWT → `FORBIDDEN` → PASS for rejection response; retain HTTP status separately if required.

### TC-13 — Customer CCCD requirement

- Customer account without saved valid CCCD attempted Reservation.
- API returned `VALIDATION_ERROR` and `errors.cccd`: `Add a valid 12-digit National ID / CCCD to continue your reservation.`
- **Result:** PASS for validation response; exact HTTP status was not separately retained.

### TC-14 — Server-owned data integrity

**Submitted extra fields:** `depositAmount=0`, `lockedRentalPrice=1`, `status=CONFIRMED`, `customerId=00000000-0000-0000-0000-000000000001`.

**Actual response:**

- Reservation ID: `af244d43-5147-419c-a641-5e9c1e726bc1`.
- `lockedRentalPrice=2000`, `depositAmount=2000`.
- `status=PENDING_DEPOSIT`.
- `customerId=92c93644-15b5-420b-8648-8440b91f9c8c` (authenticated Customer, not forged ID).
- **Result:** PASS at response level. Subsequent GET / database audit was not performed.

### TC-15 — Atomic rollback after invoice write failure

- Source examination located PostgreSQL routine `public."usp_CreateReservation"` in `backend/sql/postgresql/Reservations.sql` with `INSERT` into `Reservation` followed by `INSERT` into `Invoice`.
- Also located legacy SQL Server testing materials: `Verify-CreateReservation.ps1`, `SqlScriptTestDatabase.cs`, `DBT_PAY_014` in `PaymentResultSqlTests.cs`. Their `Microsoft.Data.SqlClient`, SQL Server trigger syntax and SQL Server database creation logic **must not** be run against the connected PostgreSQL database.
- Database connection was traced to an ignored `.env` setting `ConnectionStrings__FrmsDb`, with Supabase PostgreSQL pooler host and database `postgres`; no local Docker or `psql` CLI was available.
- **Result:** NOT EXECUTED. Fault injection must be conducted in an independently provisioned and authorized test PostgreSQL database. Do not modify triggers or schema of the shared Supabase demo environment.

## 4. Observations, Defects, and Follow-up

**Confirmed application defects from recorded tests:** None.

**Test coverage gaps:**

1. **TC-07:** Provision or locate a truly INACTIVE Facility using a privileged/read-only operational view in an isolated environment; verify HTTP error, no new Reservation or Invoice, and unchanged capacity.
2. **TC-08:** Create a catalog Unit Type that exists but has no Storage Units at the chosen Facility *in an isolated test dataset*; test whether creation is rejected and whether no related rows are persisted.
3. **TC-15:** Add PostgreSQL integration test that injects an Invoice-write failure, then asserts Reservation and Invoice row counts and effective capacity are unchanged. Run on disposable database only.
4. **Evidence strengthening:** For TC-09, TC-12 and TC-13 capture the precise HTTP statuses; for TC-10 verify DB row counts if formal transactional evidence is required; for TC-14 perform an authorized GET to verify persisted values.

**Final assessment:** Core Reservation creation, validation, capacity protection, role authorization and one near-concurrent last-slot scenario behaved as expected in the observed tests. TC-07 and TC-08 remain blocked by the available dataset. TC-15 has not been run and cannot be treated as PASS. No confirmed application bug should be filed from these three coverage gaps alone.

## 5. References

- FRMS API contract: `docs/FRMS_API_V11.md` (RES-001 and public catalog endpoints).
- FRMS SRS: `docs/FRMS_SRS_V11.md` (CWP-03 scope).
- PostgreSQL Reservation routine: `backend/sql/postgresql/Reservations.sql`.
- Observed responses: manually executed Postman / PowerShell responses in the test session dated 2026-11-01.

**Data sensitivity:** This report contains demo record IDs but no Bearer tokens, passwords, CCCD numbers or database credentials.
