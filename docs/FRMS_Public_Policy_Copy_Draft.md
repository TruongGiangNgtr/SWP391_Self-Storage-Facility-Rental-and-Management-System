# FStoRent — Four Help Center Policy Guides

**Ngày soạn:** 2026-10-29

**Baseline:** `origin/main` tại `835bafb`; owner duyệt cấu trúc bốn chủ đề trong `REV-2026-10-29-PUBLIC-POLICY-CONSOLIDATION` (SRS §0.0.11).

**Trạng thái nội bộ:** LOCAL REVIEW. Nội dung đã được đưa vào catalogue để owner test thủ công, chưa được coi là duyệt pháp lý, công bố production hoặc nghiệm thu runtime. Không đổi Policy trong DB, công thức tài chính, luồng đăng ký, quyền truy cập hoặc trạng thái của danh mục AI.

Bốn bài dưới đây viết bằng tiếng Anh để khớp giao diện Help Center. Chỉ `title`, `summary` và `sections` là nội dung hiển thị cho khách. `policyKey` là khóa API; ghi chú ở đầu và phụ lục dưới đây không được đưa vào thân bài công khai. Hai bài cũ `identity-verification` và `account-privacy` được gom vào bài Reservation & Account Requirements; không giới thiệu eKYC hay một tuyên bố pháp lý đầy đủ về quyền riêng tư.

---

## 1. reservation-cancellation

**Title:** Reservation & Account Requirements

**Summary:** How to choose storage, meet account requirements and confirm or cancel a reservation.

### Choosing a rental period

Choose an active facility and storage type. Your check-in month must be after the current business month, and your check-out month must be the same or later. Availability is checked across every selected month. Browsing or saving a selection does not hold capacity or guarantee a price.

### Your account and CCCD

You may browse without an account. To create a reservation, sign in as an active Customer and save a valid CCCD in your own profile. CCCD is optional at registration; a newly supplied value must contain exactly 12 digits from 0 to 9 after surrounding spaces are removed, with leading zeroes preserved, and must be unique. Your CCCD is not public; a valid format does not mean your identity has been independently verified.

### Confirming a reservation

After signing in or saving CCCD, review the current price and availability and explicitly confirm your reservation. Saving CCCD alone does not book or pay. A successful reservation holds capacity by facility, storage type and month range, not a physical unit, and creates a deposit invoice. Deposit payment must succeed before the reservation is confirmed.

### Cancelling a reservation

You can request cancellation of an eligible reservation from its details. The current Reservation and Visit state must allow it. A cancelled Reservation does not, by itself, confirm a deposit refund.

---

## 2. deposit-payment-refund

**Title:** Deposit, Invoices & Refunds

**Summary:** How your deposit, rental invoices and return settlement are recorded.

### Deposit and first-month rent

The deposit is a snapshot of the selected storage type's rental price and is not reduced by a Discount. Pay the deposit invoice through payOS within the time limit set by the Policy captured for your reservation. The first rental month is collected offline at handover for the reservation's original monthly price, without a Contract discount; it has no Rental Fee invoice or payOS Payment. Staff acknowledge full receipt when completing handover.

### Later rental invoices

Rental Fee invoices cover eligible months after the Contract's first month. They use the original captured price or the price captured for an extension, with any eligible Contract discount. Review each issued invoice for its amount and payment status, and use payOS for an eligible unpaid invoice. Returning from payOS is not proof of successful payment until FStoRent records it.

### Overdue fees and deductions

An unpaid later Rental Fee invoice may become overdue under the Contract's captured Policy. Applicable LateFee records, ExtraFee records and approved Damage charges are considered at return settlement. A fee record alone is not a separate payOS payment request or proof that money has already been deducted from the deposit. A pending Damage decision blocks final settlement.

### Settlement and refund

For a normal return, the recorded refund is the paid deposit less final deductions, with a minimum of zero. Early return forfeits the deposit and records no deposit refund. Deductions beyond the paid deposit create an additional amount due. Settlement amounts are records, not confirmation that any refund has been transferred.

---

## 3. storage-access-handover-contract

**Title:** Storage Rental & Return

**Summary:** What happens at handover and during visits, extensions and return.

### Handover and your Contract

A confirmed reservation uses a handover Visit within the window set by its captured Policy. After the Visit is checked in, the Facility Manager selects a matching available physical unit. Facility Staff verify that selection, acknowledge full first-month rent received offline and complete handover, creating an active Contract. The reservation itself holds capacity, not a physical unit.

### Access visits

You may schedule an access Visit for your own active Contract. Facility Staff handle check-in and check-out. A pending return Visit blocks a new access Visit. An access Visit does not change Contract ownership or extend the rental period.

### Extending your rental

An active Contract can be extended when there is no pending return Visit and capacity exists for every extension month. The new period starts directly after the current end month. A successful extension records its monthly price at the time of extension; it may differ from the original price.

### Returning storage

Schedule a return Visit through your Contract. The actual return date determines normal or early return. Confirming physical return moves the unit to inspection; finishing a Visit or Inspection alone does not finalize the Contract. Final settlement waits for pending Damage decisions and releases the unit only during return finalization.

### Terms for existing rentals

Your Reservation and Contract retain the Policy captured for that rental. New public wording or Policy versions do not rewrite captured prices, issued invoices or earlier Contract terms.

---

## 4. facility-rules-prohibited-items

**Title:** Facility Rules & Prohibited Items

**Summary:** What FStoRent does not accept for ordinary storage and how unclear items are reviewed.

### Items not accepted for storage

FStoRent does not accept these groups for ordinary self-storage: regulated weapons, ammunition and supporting tools; explosive materials and devices; controlled drugs; flammable gases, fuels and materials with a high fire or explosion risk; banned or hazardous chemicals requiring specialist storage; radioactive or nuclear materials; and hazardous waste requiring specialist handling. These are storage acceptance exclusions, not a claim that every possession of such an item is unlawful.

### Ordinary belongings and conditions

Examples of ordinary belongings include dry books and documents, clean clothing, household furniture, clean empty packaging, household utensils, safely prepared electrical appliances, non-hazardous toys and ordinary sports equipment. Items must be clean, dry and free of excluded dangerous contents or residues. Electrical devices must be switched off, protected from moisture and free of damaged batteries; equipment with refrigerants or accumulators needs separate review. Toys and sports equipment must not include weapons, explosives or pressure cylinders; toy weapons and special batteries need review.

### Unclear items and AI guidance

An ordinary item in the same description does not make an excluded item acceptable. Unlisted or ambiguous descriptions need clarification; describe the item's type, condition and contents. AI guidance is not permission to store an item. This item-acceptance catalogue remains a draft pending legal and business review.

---

## Phụ lục đối chiếu — không đưa vào giao diện

| Bài | Căn cứ hiện có |
|---|---|
| Reservation & Account Requirements | SRS OD-09/10, BR-ACC-01/04, BR-RES-01..09, BR-PUB-01/04/05/07; API AUTH-001/004/005, RES-001/005. CCCD chỉ là điều kiện cho Reservation mới, không phải xác minh danh tính. |
| Deposit, Invoices & Refunds | SRS BR-RES-04..06, BR-HO-02/03, BR-DIS-05, BR-BIL-01..06, BR-LATE-01/02, BR-SET-01/02; CALC-DEP-01, CALC-FIRST-01, CALC-SET-01..03; API BIL/PAY. Không suy ra chuyển khoản hoàn tiền từ Settlement. |
| Storage Rental & Return | SRS OD-02, BR-HO-01..03, BR-VIS-03/04, BR-REN-01..04, BR-RET-01..07, BR-PUB-06; Data Dictionary §4.28. Manager chọn Unit sau Visit CHECKED_IN, trước Staff Complete Handover. |
| Facility Rules & Prohibited Items | `backend/Frms.Api/Content/AiStorageRules/storage-items.vi-VN.json`: bảy nhóm PROHIBITED, tám nhóm ALLOWED và classificationPolicy; nguồn vẫn `DRAFT_REQUIRES_LEGAL_AND_BUSINESS_APPROVAL`, `effectiveDate: null`. Không đổi AI approvalStatus hoặc suy ra tuyên bố pháp lý từ legalReferences. |

**Giới hạn nội dung:** Không thêm số ngày, phí hay tỷ lệ cấu hình từ seed. Metadata `PolicyId/version/effectiveFrom/effectiveTo` là của Policy ACTIVE trong DB, không phải phiên bản riêng của copy. Không thêm bảng, trạng thái, bước xác minh CCCD, quyền xem CCCD của nhân viên, thanh toán payOS cho tháng đầu hoặc settlement. SRS/API phải thống nhất bốn khóa và thứ tự; dữ liệu cũ `identity-verification` và `account-privacy` không còn là bài riêng. Trước công bố production cần nhóm duyệt copy, nhất là danh mục vật phẩm, và chạy nghiệm thu GWP-04/API-PUB-006.
