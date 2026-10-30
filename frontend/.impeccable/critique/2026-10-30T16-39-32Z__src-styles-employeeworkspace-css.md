---
target: employee pages
total_score: 22
max_score: 40
na_heuristics: 
p0_count: 1
p1_count: 2
target_identity: "file:D:\\SWP391_Self-Storage-Facility-Rental-and-Management-System\\frontend\\src\\styles\\employeeWorkspace.css"
target_fingerprint: "sha256:bceb5b48b448a7fbf3362a1bf7b65cfc196b02c0905b2fd247c5301536d8d7b4"
target_path: "D:\\SWP391_Self-Storage-Facility-Rental-and-Management-System\\frontend\\src\\styles\\employeeWorkspace.css"
timestamp: 2026-10-30T16-39-32Z
slug: src-styles-employeeworkspace-css
---
Method: dual-agent (A: 0e3648ae-aa42-4cab-b7af-7adf6297c93a · B: 708a48df-f355-4409-bae7-bf0654e05d4a)

# Design Critique: FRMS Employee Pages (`staff`, `manager`, `business`, `admin`, `EmployeeLoginPage`)

## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|---|---:|---|
| 1 | Visibility of System Status | 2 | Client-side filters & tabs operate only on the 5–20 rows of the current server page (`ACTIVE ON THIS PAGE`), hiding matching records on page 2+; Staff & Manager shells omit assigned Facility name |
| 2 | Match System / Real World | 2 | Schema and protocol terms leak into operator UI (`multipart/form-data`, `StorageUnit`, `DamageType`, `ExtraFeeType`, `Flow 7`, raw `error.code` alert titles) |
| 3 | User Control and Freedom | 2 | Clicking `Complete Inspection` on Tab 2 of `StaffReturnsPage` immediately locks Tab 3's Damage & Extra Fee forms with no confirmation or undo; single-click discount deactivation |
| 4 | Consistency and Standards | 2 | 10+ competing CSS class prefixes (`fwp-*`, `mop-*`, `mwp05-*`, `fr-*`, `facility-*`, `mfm-*`, `br-*`, `awp01-*`..`awp07-*`), 3 incompatible footer patterns, and mixed `window.alert()` vs inline alerts |
| 5 | Error Prevention | 2 | Global policy activation (`Create & Activate Version`), inspection completion, and manager damage approval (`Approve`/`Reject` without viewing photos) execute irreversible state transitions without confirmation or diff review |
| 6 | Recognition Rather Than Recall | 2 | `EmployeeEntityLabel` stops after page 1 (`pageSize=100`) and renders `"Name not loaded. [Load more names]"` inside table cells; Login History & Activity Logs require copying 36-char UUIDs from `/admin/users` |
| 7 | Flexibility and Efficiency | 2 | Search on `FacilityOperationsPage`, `ReturnsInspectionsPage`, and `MultiFacilityMonitoringPage` runs `JSON.stringify(item)` on unresolved DTOs, returning 0 results when searching customer names or unit codes |
| 8 | Aesthetic and Minimalist Design | 2 | `employeeWorkspace.css` overrides `DESIGN.md` with 7 competing blue fills across headers, KPI cards, filter bars, and tables; 228 CSS token drift findings across feature stylesheets |
| 9 | Error Recovery | 3 | Strong partial-failure recovery on employee creation (`EXTERNAL_PROVIDER_UNAVAILABLE`), but `UnitTypePricingPage` and `DiscountManagementPage` fall back to blocking `window.alert()` |
| 10 | Help and Documentation | 3 | `RolePermissionsPanel` and `StaffHandoverPage` checklist clarify complex rules, though several helper banners read like copy-pasted SRS specification clauses |
| **Total** | | **22/40** | **Acceptable (55% — significant functional & visual friction across core operational queues)** |

---

## Design Specificity Verdict

**Domain-grounded in workflow mechanics, visually sabotaged by a global "blue-wash" override and stitched-together multi-author templates.**

### LLM Assessment
At the domain-workflow level, several employee surfaces are genuinely authored for FRMS self-storage operations rather than generic SaaS CRUD:
- `HandoverUnitSelectionPage.tsx` pairs confirmed reservations awaiting unit assignment on the left with an interactive physical-unit grid (`AVAILABLE`, `OCCUPIED`, `MAINTENANCE`, `INSPECTION`) on the right.
- `StaffHandoverPage.tsx` models the counter handover checklist: verify physical unit, confirm offline first-month payment receipt, and execute atomic handover.
- `FacilityOperationsPage.tsx` (`StorageUnitTimeline.tsx`) and `ActivityLogManagementPage.tsx` (`AuditLogDetailDrawer`) translate storage occupancy timelines and before/after audit state snapshots into readable operational tools.

However, at the visual and structural level, the four portals (`FACILITY_STAFF`, `FACILITY_MANAGER`, `BUSINESS_OPERATIONS_MANAGER`, `SYSTEM_ADMINISTRATOR`) and `EmployeeLoginPage.tsx` suffer from three systemic problems:
1. **Direct contradiction of `DESIGN.md` ("The One Blue Rule"):** While `DESIGN.md` mandates a quiet `#F8FAFC` Cool Canvas, `#FFFFFF` White Surface cards, `#0F172A` Slate Ink headings, and `#2563EB` Confident Blue reserved strictly for primary actions and active selection, `src/styles/employeeWorkspace.css` uses a quadruple-class selector hack (`.employee-workspace.employee-workspace.employee-identity.employee-identity:is(...)`) to repaint almost every surface in **seven competing shades of blue** (`#102a56` sidebars, `#eaf0f9` canvas, `#174b9b` hero page headers, `#dce8fa` KPI strips and filter bars, `#e7effc` secondary cards, `#183961` table headers, and `#f1f6fe` zebra rows). Because page headers are painted `#174b9b` saturated blue, header action buttons had to be inverted to white backgrounds with blue text (`employeeWorkspace.css:168-174`), making secondary `Refresh` buttons visually identical to primary `+ Add Facility` or `Create employee` CTAs.
2. **Incomplete `:is(...)` selector lists and stitched-together templates:** Across the 4 portals, 10+ bespoke CSS prefix families coexist (`fwp-*`, `mop-*`, `mwp05-*`, `fr-*`, `facility-*`, `mfm-*`, `br-*`, `awp01-*`..`awp07-*`). Where `employeeWorkspace.css:157` misses `.page-heading` (`StaffHandoverPage.tsx:62`, `StaffAccessVisitPage.tsx:20`, `StaffContractListPage.tsx:56`, `StaffContractPage.tsx:68`), navigating from `/staff/work-list` to a handover or contract page causes the `#174b9b` header banner to vanish into plain unboxed text. Conversely, on `/business/discounts` (`DiscountAndExtraFeePage.tsx:6` + `DiscountManagementPage.tsx:535`), parent and child components both render `.facility-page-heading`, stacking **two dark-blue hero banners directly on top of each other**.
3. **Missing operational context in shells and high-stakes decisions:** Both facility-scoped shells (`FacilityStaffLayout.tsx:46-68` and `FacilityManagerLayout.tsx:78-114`) render the logged-in employee's name, avatar initials, and role badge **twice within 120px**, while omitting the employee's **assigned Facility name**. Meanwhile, `ReturnsInspectionsPage.tsx:202-209` asks Facility Managers to approve or reject financial `DamageRecord` claims without rendering the uploaded inspection photos (`"This view lists evidence details only"`).

### Deterministic Scan
Running `impeccable detect --json` across all 51 employee `.tsx` files and their 20 feature/component directories exited with code `0` and reported **235 advisory quality findings** (`0` structural/accessibility/AI-slop antipattern errors):
- **`design-system-color` (115 findings — 96 genuine token drift, 19 defensible chart/semantic borders, 17 on dead CSS rules):**
  - `src/features/manager-reports/FacilityReportsPage.tsx` (`L16-19, L134, L142, L155` — 7 inline hex findings), `src/features/manager-reports/facilityReports.css` (43 findings), and `src/features/multi-facility-monitoring/multiFacilityMonitoring.css` (49 findings) introduce a competing indigo/periwinkle palette (`#4964ee`, `#3e63dd`, `#5473e8`, `#526fee`, `#3e61e5`, `#334fb4`, `#99b0ff`) and custom dark navies/greys (`#223149`, `#1d3150`, `#7d899d`, `#8793a6`) that violate `DESIGN.md`'s One Blue Rule (`#2563EB`) and fail WCAG AA 4.5:1 text contrast on tinted backgrounds.
  - Notably, the detector uncovered **17 color findings on dead/unused CSS selectors** in `facilityReports.css` (`L51-L61`, e.g., `.fr-pending-badge`, `.fr-charts`, `.fr-ring*`, `.fr-revenue-total`) that are never referenced in JSX.
  - Additionally, `FacilityReportsPage.tsx` (`#26a879`, `#4964ee`, `#e9a022`, `#8392aa`) and `storageUnitTimeline.css` (`#d3f3e4`, `#243e9f`, `#ffedbb`, `#344256`) use two different categorical color palettes for the exact same four unit statuses (`AVAILABLE`, `IN_USE`, `INSPECTION`, `MAINTENANCE`).
- **`design-system-font-size` (84 findings — 37 genuine drift, 47 false positives):**
  - **47 false positives:** The detector checks only `DESIGN.md` YAML frontmatter (`30px`, `20px`, `14px`), flagging `12px` (29×), `13px` (13×), `18px` (1×), and `26px–29px` page headings (4×) even though the `DESIGN.md` prose typography table (`lines 135-145`) explicitly approves `12–13px` Metadata, `18px` Subheadings, and `26–30px` Page headings.
  - **37 genuine drift findings:** `facilityReports.css` and `multiFacilityMonitoring.css` use **21 sub-12px micro-text sizes** (`10px` × 7, `11px` × 14) on table headers, badges, and legends, plus **16 off-scale intermediate/KPI sizes** (`15px`, `16px`, `17px`, `22px`, `23px`, `24px`, `31px`).
- **`design-system-radius` (36 findings — 34 genuine drift, 2 false positives):**
  - `managerSupport.css` (10), `facilityReports.css` (12), `multiFacilityMonitoring.css` (11), and `discount-and-fees.css` (1) use arbitrary radii (`4px`, `5px`, `6px`, `7px`, `9px`, `10px`, `11px`, `14px`, `15px`, `20px`) instead of the `DESIGN.md` scale (`8px`, `12px`, `16px`, `999px`).
  - **2 false positives:** `3px` radius on a `13×13px` legend swatch (`storageUnitTimeline.css:L8`) and a `10px`-tall progress bar track (`businessReports.css:L52`).

### Visual Overlays
No reliable user-visible browser overlay is available in this session because no browser automation / mutable DOM script-injection tool is exposed in the active runtime toolset (and employee routes require an active employee JWT session plus backend API proxy target `VITE_API_PROXY_TARGET`). Fallback signal used: deterministic CLI detector (`impeccable detect --json`) + static source and CSS cascade inspection.

---

## Overall Impression

The employee portals already encode the complex domain rules of FRMS—capacity-to-physical-unit handover, inspection-gated return finalization, append-only policy versioning, and idempotent account provisioning—with impressive fidelity. However, day-to-day usability is undermined by **client-side filters applied to server-paginated slices** (making valid customer visits and contracts vanish when they sit on page 2+), **broken `JSON.stringify` search boxes** that cannot match rendered customer names or unit codes, and a **heavy-handed blue CSS override (`employeeWorkspace.css`)** that drowns action hierarchy in seven shades of blue. Fixing the data-lookup/search mechanics and restoring `DESIGN.md`'s quiet surface hierarchy is the single biggest opportunity to turn these four portals into fast, trustworthy operational tools.

---

## What's Working

1. **Master-Detail Spatial Unit Selection (`src/features/handover-unit-selection/HandoverUnitSelectionPage.tsx:422-637`):** Pairing the confirmed-reservation queue on the left with a status-coded physical `StorageUnit` grid on the right (`AVAILABLE`, `OCCUPIED`, `MAINTENANCE`, `INSPECTION`) and a clear binding confirmation dialog turns an abstract foreign-key assignment into an intuitive spatial task.
2. **Idempotent Partial-Failure Recovery in Employee Provisioning (`src/features/admin-employee-management/EmployeeAccountManagementPage.tsx:164-175, 247-256, 529-568`):** When SQL Server creates an `INACTIVE` employee account but the email provider fails (`EXTERNAL_PROVIDER_UNAVAILABLE`), the UI closes the create modal, refreshes the table to reveal the new row, and warns the administrator not to resubmit the form and to use `Resend credential` instead.
3. **Humanized Audit State Diff Drawer (`src/features/admin-activity-logs/ActivityLogManagementPage.tsx:137-166, 188-252`):** `snapshotFields()` and `AuditLogDetailDrawer` strip internal UUID keys from `oldValue` / `newValue` JSON payloads, resolve human names via `<EmployeeEntityLabel>` and `<AuditResource>`, and present clean side-by-side `Previous state` vs. `Resulting state` cards.

---

## Priority Issues

### 1. [P0] What: "Page-1 Filter Mirage", Broken `JSON.stringify` Search, and Manual Cell Name Loading Across Operational Queues
- **Why it matters:**
  - Across `StaffWorkItemsPage.tsx:50-55, 137-151`, `StaffReturnsPage.tsx:79, 126`, `StaffContractListPage.tsx:35-37, 90`, `StaffSupportPage.tsx:78-94`, `FacilityOperationsPage.tsx:239, 298`, `ReturnsInspectionsPage.tsx:83, 104`, `FacilityManagementPage.tsx:60, 137-170, 480-509` (`pageSize = 5`), and Admin tables (`AdminListNavigation.tsx:28-35`), search inputs, status filters, and route-mode queues (`/staff/handle-handover`, `/staff/access-visits`) filter **only the 5–20 items on the current server page**. If a checked-in customer's handover is item #21 (page 2), `/staff/handle-handover` displays `"No matching work items"` on page 1 while the KPI card above says `Total work items: 42`.
  - Worse, `FacilityOperationsPage.tsx:65`, `ReturnsInspectionsPage.tsx:104-105`, and `MultiFacilityMonitoringPage.tsx:279` filter rows via `JSON.stringify(item).toLowerCase().includes(query)`. Because customer names and unit codes are resolved asynchronously inside `<EmployeeEntityLabel>` and do not exist on `item`, typing a customer's name or unit code returns **0 matches**, while typing `"null"` matches every row.
  - Meanwhile, `EmployeeEntityLabel.tsx:26-46, 148-154` only auto-fetches page 1 (`pageSize=100`) of directory lookups, rendering `"Name not loaded. [Load more names]"` buttons inside individual table cells when an entity sits on page 2+.
- **Fix:**
  1. Fetch full scoped pages (`fetchAllPages`, as `PhysicalUnitManagementPage.tsx:303-325` already does) whenever client-side mode splitting, search, or KPI counting is used, and reset `page` to `1` on filter change.
  2. Replace `JSON.stringify(item)` in `FacilityOperationsPage.tsx`, `ReturnsInspectionsPage.tsx`, and `MultiFacilityMonitoringPage.tsx` with field-aware matching against resolved directory labels (customer name, phone, email, unit code, unit type, status).
  3. Update `EmployeeEntityLabel.tsx` (`ensureLoaded`) to automatically paginate directory lookups until the target ID is resolved or pages are exhausted, removing the manual `"Load more names"` button.
- **Suggested command:** `$impeccable harden`

### 2. [P1] What: High-Stakes Workflow Traps — Return Inspection Tab Lockout, Blind Damage Decisions, and Missing Handover Fee Amount
- **Why it matters:**
  - In `StaffReturnsPage.tsx:379-530`, the horizontal step tabs read `1. Confirm Return` → `2. Return Inspection` → `3. Fees & Finalize`. Tab 2 ends with a primary button `Complete Inspection` (`line 460`), while `Record Damage` (`lines 480-499`) and `Record Extra Fee` (`lines 501-530`) live on Tab 3. However, recording damage or extra fees requires `inspection.status === 'IN_PROGRESS'` (`canEditInspection` at `line 86`). Staff who naturally click `Complete Inspection` at the bottom of Tab 2 before switching to Tab 3 permanently lock out the Damage and Extra Fee forms with no confirmation dialog and no way to reopen the inspection.
  - In `ReturnsInspectionsPage.tsx:202-209`, Facility Managers approve or reject financial `DamageRecord` claims (`line 209`), but the Evidence section (`line 205`) prints only `{ev.mediaType} · {formatDateTime(ev.createdAt)}` and *"This view lists evidence details only"*—providing no image preview or link to inspect the actual damage photo.
  - In `StaffHandoverPage.tsx:92-133`, staff must check `"I confirm the Customer's first-month payment has been received offline"` (`line 115`), but the card never displays the first-month rental amount in VND that the staff member is confirming they collected.
- **Fix:**
  1. Move `Record Damage` and `Record Extra Fee` into Step 2 (`Return Inspection`) *above* `Complete Inspection` in `StaffReturnsPage.tsx` (renaming Step 3 to `Settlement & Finalize Return`), and require a confirmation dialog on `Complete Inspection` summarizing recorded damages (`0 damages` vs `N damages`) before locking the inspection.
  2. Render clickable evidence thumbnails/preview modals in `ReturnsInspectionsPage.tsx:202-209` and `StaffReturnsPage.tsx:464-476`, and add a confirmation prompt before `Approve`/`Reject` damage decisions.
  3. Display the captured monthly rental price / first-month payment amount (formatted in VND) inside Step 2 of `StaffHandoverPage.tsx:92-118`.
- **Suggested command:** `$impeccable clarify`

### 3. [P1] What: Global `.employee-workspace` "Blue-Wash" Override & Feature CSS Drift Violate `DESIGN.md` and Invert Button Hierarchy
- **Why it matters:**
  - `src/styles/employeeWorkspace.css` (`lines 3-30, 157-308`) overrides all four employee portals with 7 shades of blue (`#174b9b` hero headers, `#dce8fa` KPI cards and filter bars, `#e7effc` secondary panels, `#183961` table headers, `#eaf0f9` canvas), directly violating `DESIGN.md`'s One Blue Rule (`#2563EB` reserved for primary actions and active selection on `#F8FAFC` / `#FFFFFF` surfaces).
  - Inverting header buttons to white on `#174b9b` blue (`employeeWorkspace.css:168-174`) gives secondary `Refresh` buttons (`UserAccountMonitoringPage.tsx:74`, `FacilityOperationsPage.tsx:167`) the exact same high-contrast white fill as primary `+ Add Facility` and `Create employee` CTAs.
  - Missing `.page-heading` in `employeeWorkspace.css:157` causes headers to disappear on `StaffHandoverPage.tsx`, `StaffAccessVisitPage.tsx`, `StaffContractListPage.tsx`, and `StaffContractPage.tsx`, while nested `.facility-page-heading` blocks stack two `#174b9b` hero banners on `/business/discounts` (`DiscountAndExtraFeePage.tsx:6` + `DiscountManagementPage.tsx:535`).
  - Meanwhile, `facilityReports.css`, `multiFacilityMonitoring.css`, and `managerSupport.css` introduce 228 token drift findings, including competing indigo blues (`#4964ee`, `#3e63dd`), 21 sub-12px font sizes (`10px`–`11px`), and low-contrast muted greys (`#7d899d`, `#8793a6`).
- **Fix:**
  1. Refactor `employeeWorkspace.css` to match `DESIGN.md`: `#F8FAFC` canvas, `#FFFFFF` cards with `#E2E8F0` borders, `#0F172A` Slate Ink page headings, `#F8FAFC`/`#F1F5F9` table headers with `#475569` text, `#2563EB` solid fill exclusively on primary CTAs, and neutral `#FFFFFF` secondary buttons for `Refresh`/`Clear`.
  2. Include `.page-heading` in the shared page-header rule and remove the duplicate inner `.facility-page-heading` banner inside `DiscountManagementPage.tsx:535` and `ExtraFeeManagementPage.tsx:31`.
  3. Tokenize colors, font sizes (`>=12px`), and border radii (`8px`/`12px`/`16px`/`999px`) in `facilityReports.css`, `multiFacilityMonitoring.css`, `managerSupport.css`, `storageUnitTimeline.css`, and `FacilityReportsPage.tsx`, and delete the 17 dead CSS selectors in `facilityReports.css`.
- **Suggested command:** `$impeccable colorize`

### 4. [P2] What: Shell Navigation Sprawl, Missing Facility Context, and Redundant Marketing Footers
- **Why it matters:**
  - `FacilityStaffLayout.tsx:46-68` and `FacilityManagerLayout.tsx:78-114` render the employee's name, avatar initials, and role badge **twice** in the top-left corner while never displaying the employee's **assigned Facility name** (`DESIGN.md:194`).
  - `FacilityStaffLayout.tsx:13-22` lists **8 flat sidebar items**, 4 of which (`Work Items`, `Reservation Check-In`, `Handle Handover`, `Access Visits`) mount the same `StaffWorkItemsPage` component and 2 of which (`Support Tickets`, `Assigned Tickets`) mount the same `StaffSupportPage` component.
  - `FacilityStaffLayout.tsx:99`, `FacilityManagerLayout.tsx:157`, and `BusinessOperationsLayout.tsx:160-210` render multi-column marketing-style footers (`EmployeePortalFooter.tsx` and `.bo-footer`) with duplicate nav links, a second Sign Out button, non-interactive `<span>Privacy Policy</span>` text (`EmployeePortalFooter.tsx:78-79`), and a broken horizontal flex collapse in `.bo-footer-grid` (`business-sidebar.css:20`, `display: contents`).
- **Fix:**
  1. Remove the duplicate `.staff-profile` sidebar block in `FacilityStaffLayout.tsx` and `FacilityManagerLayout.tsx` and display a clear **Assigned Facility** badge in the header bar.
  2. Group sidebar navigation into labeled operational sections and consolidate duplicate mode links in `FacilityStaffLayout.tsx` behind in-page segmented tabs.
  3. Replace `<EmployeePortalFooter>` and `.bo-footer` with the compact operational status footer pattern used in `SystemAdministratorLayout.tsx:136-139`.
- **Suggested command:** `$impeccable distill`

### 5. [P2] What: Keyboard Accessibility Gaps, Unconfirmed Live Mutations, Blocking `window.alert()`, and SRS Jargon Leaks
- **Why it matters:**
  - Clickable `<tr onClick={...}>` rows in `DiscountManagementPage.tsx:614-621` and `EmployeeAccountManagementPage.tsx:434-438` lack `tabIndex={0}`, `onKeyDown`, or button triggers, blocking keyboard-only customer selection.
  - Custom `<div className="facility-modal-backdrop">` modals in `FacilityManagementPage.tsx:1045-1378` and `PolicyVersionManagementPage.tsx:700-1000` bypass `<EmployeeModal>`, lacking focus trapping and `Escape` dismissal; `<EmployeeModal>` itself (`EmployeeModal.tsx:9-59`) does not restore focus to the trigger element when closed.
  - `PolicyVersionManagementPage.tsx:854` (`Create & Activate Version`) and `DiscountManagementPage.tsx:715` (`Deactivate`) mutate live billing rules and customer discounts on a single click without confirmation or diff review, while `UnitTypePricingPage.tsx:235, 239` and `DiscountManagementPage.tsx:445, 459, 473` use blocking `window.alert()` calls.
  - `LoginHistoryPage.tsx:115` and `ActivityLogManagementPage.tsx:382` force admins to copy-paste 36-char UUIDs to filter by user, and raw schema/SRS terms (`multipart/form-data`, `StorageUnit`, `DamageType`, `Flow 7`) leak into labels.
- **Fix:**
  1. Wrap all modals in `<EmployeeModal>` with focus restoration on unmount, add keyboard handlers/buttons to selectable table rows, and ensure icon buttons meet the `44×44px` minimum target (`managerSupport.css:L159` is currently `38×38px`).
  2. Add a before/after diff confirmation step to `PolicyVersionManagementPage.tsx`, add confirmation before deactivating discounts, and replace all `window.alert()` calls with `<ApiErrorAlert>`.
  3. Add an account/user picker (or name/email search) to `LoginHistoryPage.tsx` and `ActivityLogManagementPage.tsx`, and replace schema/SRS jargon with plain operational language.
- **Suggested command:** `$impeccable polish`

---

## Persona Red Flags

- **Alex (Impatient Power User):**
  - Forced to leave `LoginHistoryPage.tsx:115` and `ActivityLogManagementPage.tsx:382`, navigate to `/admin/users`, copy a 36-character UUID, and paste it into an `Account ID` field validated by `UUID_PATTERN` just to filter logs by person.
  - Must manually click `"Load more names"` inside table cells (`EmployeeEntityLabel.tsx:148-154`) whenever a customer, unit, or employee sits past the first 100 directory records.
  - Typing a customer name or unit code (`A-101`) into Search on `FacilityOperationsPage.tsx:65`, `ReturnsInspectionsPage.tsx:104`, or `MultiFacilityMonitoringPage.tsx:279` returns **0 results** because `JSON.stringify(item)` only inspects raw DTO UUIDs.
  - Hunting for an arriving customer on `/staff/handle-handover` (`StaffWorkItemsPage.tsx:137-151`) requires clicking `Next` through server pages one by one because client filters only inspect the current 20-row page.
- **Sam (Accessibility-Dependent User):**
  - Cannot select a customer row on `DiscountManagementPage.tsx:614-621` using a keyboard (`<tr onClick>` has no `tabIndex`, `onKeyDown`, or `<button>`).
  - Trapped without `Escape` dismissal or focus containment inside raw `<div className="facility-modal-backdrop">` modals on `FacilityManagementPage.tsx:1045-1378` and `PolicyVersionManagementPage.tsx:700-1000`, and loses focus context when closing `<EmployeeModal>` (`EmployeeModal.tsx:9-59`).
  - Screen reader flow is interrupted by native `window.alert()` popups (`UnitTypePricingPage.tsx:235, 239`, `DiscountManagementPage.tsx:445, 459, 473`) and generic table buttons labeled only `Open` or `Select` (`StaffWorkItemsPage.tsx:258`, `FacilityOperationsPage.tsx:290`) without row-specific `aria-label` context.
- **Riley (Deliberate Stress Tester):**
  - Clicks `Complete Inspection` on Tab 2 of `StaffReturnsPage.tsx:460` with zero evidence and blank notes, then switches to Tab 3 (`Fees & Finalize`)—permanently locking out `Record Damage` and `Record Extra Fee` and enabling `Finalize Return` without billing for damages.
  - Types `"null"` or `"facilityid"` into the search bar on `FacilityOperationsPage.tsx:65` and matches every single row because search runs `JSON.stringify(item).toLowerCase().includes(query)`.
  - Navigates to Page 3 of `FacilityManagementPage.tsx`, applies a filter that matches 0 rows on Page 3 (while Page 1 has matches), and leaves the table stuck on an empty `Page 3 of 4`.

---

## Minor Observations

- **Hardcoded `pageSize = 5` on Facility Management (`FacilityManagementPage.tsx:60`):** Even on wide desktop monitors, the Facilities table displays at most 5 rows per page while dedicating 4 summary cards above it to count `LOADED ON THIS PAGE (5)`, `ACTIVE ON THIS PAGE`, and `INACTIVE ON THIS PAGE`.
- **Dead Legal Spans in Employee Footer (`EmployeePortalFooter.tsx:78-79`):** `<span>Privacy Policy</span>` and `<span>Terms of Service</span>` are styled like links in the footer but have no `href` or interaction.
- **Broken `.bo-footer-grid` Layout (`business-sidebar.css:20`):** `.bo-footer-grid { display: contents; }` inside a flex row collapses 4 footer columns horizontally into a single wrapping line.
- **Duplicate Actor & Resource Blocks in Audit Drawer (`ActivityLogManagementPage.tsx:211-232`):** `AuditLogDetailDrawer` renders `Actor` and `Business resource` in `.awp07-detail-grid` and immediately repeats the exact same two blocks in `.awp07-identifier-block` below.
- **Duplicated Local `formatDateTime` Implementations:** At least 7 employee pages re-implement their own local `formatDateTime()` helper with inconsistent fallback/locale rules instead of sharing a single `Asia/Ho_Chi_Minh` formatter in `src/utils/formatters.ts`.
- **Sub-44px Close Button Target (`managerSupport.css:159`):** `.mwp05-icon-button` is `38×38px`, below the `44×44px` minimum touch/click target specified in `DESIGN.md:178`.

---

## Questions to Consider

1. **Can we retire the `.employee-workspace` seven-blue override in `employeeWorkspace.css` and converge on `DESIGN.md`'s quiet white-surface / single-blue hierarchy?** Right now, `employeeWorkspace.css` fights both `DESIGN.md` and the individual page stylesheets.
2. **Why do `/staff/work-list`, `/staff/reservation-check-in`, `/staff/handle-handover`, and `/staff/access-visits` occupy 4 separate sidebar links when they all mount `StaffWorkItemsPage`?** Consolidating them into one **Visits & Handovers** workspace with prominent stage tabs (backed by full dataset filtering) would eliminate both the 8-item sidebar clutter and the page-1 filter mirage.
3. **Should a Facility Staff member ever be allowed to click `Complete Inspection` before reviewing the Damage & Extra Fee section, or a Facility Manager click `Approve` on a damage charge without seeing the photo evidence?** Combining inspection notes, evidence previews, and damage/fee entry into a single pre-completion checklist would close the most dangerous operational trap in the app.
