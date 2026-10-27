# Manager & BOM Reports — Backend → Frontend API Handoff (2026-10-27)

Scope: `MWP-06` Manager Facility Reporting, `BWP-07` Business Reporting and Export; GitHub main read-only. Contract alignment with SRS V11 `CALC-REP-01/02` and existing `REP-001..004`.

All JSON responses use `{ "data": ... }`, URLs below start with `/api/v1`.

| FE section | GET route | Role | Response data |
|---|---|---|---|
| Manager current summary | `/reports/facilities/{facilityId}/operations` | FACILITY_MANAGER, same Facility | `facilityId`, `asOf`, `availableUnits`, `inUseUnits`, `inspectionUnits`, `maintenanceUnits`, `usageRate`, `overdueContractCount` |
| Manager monthly units + inventory | `/reports/facilities/{facilityId}/units?month=2026-10&page=1&pageSize=20` | FACILITY_MANAGER, same Facility | `facilityId`, `month`, `asOf`, `inventoryScope`, `historicalInventorySupported`, `unitTypes[]`, `inventory[]`, `pagination`, `dailyTrend[]`, `averageOccupancy`, `peakDay` |
| Manager monthly revenue | `/reports/facilities/{facilityId}/revenue?month=2026-10` | FACILITY_MANAGER, same Facility | `facilityId`, `month`, `firstMonthRecognizedRevenue`, `subsequentPaidRentalRevenue`, `rentalRevenue` |
| BOM grouped facility tab | `/reports/business/analysis?view=facility&year=2026&page=1&pageSize=20` | BUSINESS_OPERATIONS_MANAGER | `view`, `year`, `quarter`, `asOf`, `summary`, `rows[]`, `pagination` |
| BOM grouped unit type tab | `/reports/business/analysis?view=unitType&year=2026&page=1&pageSize=20` | BUSINESS_OPERATIONS_MANAGER | same as above |
| BOM current status tab | `/reports/business/analysis?view=status&page=1&pageSize=20` | BUSINESS_OPERATIONS_MANAGER | same as above; rows grouped by Facility × UnitType, containing all four physical status counts |
| BOM revenue tab | `/reports/business/monthly?year=2026&quarter=4` | BUSINESS_OPERATIONS_MANAGER | `year`, `quarter`, `facilityId`, `unitTypeId`, three revenue totals, `months[]` |
| BOM CSV export | `/reports/business/export?view=facility&year=2026&quarter=4` | BUSINESS_OPERATIONS_MANAGER | `text/csv; charset=utf-8`, attachment |

BOM routes support optional `facilityId` and `unitTypeId`. Business `year` defaults to the current business year and `quarter` can be omitted (full year). Accepted export `view`: `revenue` (default, backward-compatible), `facility`, `unitType`, `status`. The `revenue` CSV preserves the old header `month,firstMonthRecognizedRevenue,subsequentPaidRentalRevenue,rentalRevenue`; the other three views export every filtered row from `/business/analysis` with headers `facilityId,facilityName,unitTypeId,unitTypeName,unitTypeSize,totalUnits,availableUnits,inUseUnits,inspectionUnits,maintenanceUnits,usageRate,firstMonthRecognizedRevenue,subsequentPaidRentalRevenue,rentalRevenue`. BOM CSV export is read-only, has `Cache-Control: no-store`, and neutralizes spreadsheet formula injection in text cells.

### FE contract constraints

- Usage rate is a **fraction** `IN_USE / (AVAILABLE + IN_USE + INSPECTION + MAINTENANCE)`; multiply by 100 only to display a percentage.
- Rental revenue is **first-month Contract-recognized rent + paid later-month RENTAL_FEE invoices**. It is **not** total Invoice/Payment receipts. Deposit, LateFee, ExtraFee and Damage excluded. Money from the server is authoritative.
- `year`/`quarter` affect **revenue**, not current physical inventory. Do not label present-day unit counts as historical ones.
- Manager `/units` returns `inventoryScope="CURRENT"`, `historicalInventorySupported=false` because reliable historical UnitType/location/Contract/customer snapshots are not persisted. `dailyTrend[].usageRate` may be `null` when AuditLog coverage is incomplete; `unknownUnits` indicates missing evidence. The FE must show an unavailable/incomplete history state instead of inventing percentages. This is an unresolved historical-data limitation, not a runtime error.
- Manager Facility name lookup used in the existing UI: `GET /facilities/{facilityId}` returns **ACTIVE** Facility details from the public catalog; for inactive assigned facilities it may be unavailable. Do not infer this as a report authorization failure.
- Manager ownership enforced server-side; BOM scope system-wide. No sensitive profile or CCCD data is exported. No database schema migration is required for this patch.
- Expected errors: missing authentication `401`, wrong role or Manager outside Facility `403`, unknown Facility/UnitType `404`, malformed date/period/view/pagination `400`. Validate these against deployed API; source review is not runtime proof.
- `.csv` download requires the FE to handle a binary/text response with its access token; the generic JSON `httpClient.get()` cannot decode it as an API JSON envelope.

**Verification status:** Source-alignment check only. Build/Swagger/API/RBAC/data-accuracy/E2E tests remain pending until executed in the target .NET 10/PostgreSQL environment. Existing historical snapshot gaps must not be called release-complete.
