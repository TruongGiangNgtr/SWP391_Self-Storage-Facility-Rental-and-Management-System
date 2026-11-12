# Authentication & Security — 51 Source Cases / v5 Candidate Procedures

**Draft only.** Source: `TEST_SWT` manifest and `main` React UI at `52057d0`. V5 SRS and 79-journey review guide writing. All case result fields remain `Untested`; none is re-certified. Role-specific execution fixtures are external. API-only and mixed cases are not valid manual UI workbook rows.

**Verified shared UI entry labels:** Homepage `Sign In`, footer `Employee Sign In`, registration `Create Customer Account`, optional section `Additional profile information (optional)`, Administrator sidebar `Customer access`. Other case-specific UI behavior still requires runtime and SRS review.

### FRMS-ST-0001 — Check Customer Registration form labels, fields and action controls.

- Surface: UI; source: `Khánh:MOD01_Auth:TC1`
- Expected Output (existing manifest; **SRS approval pending**): The Create Customer Account form displays Full Name, Email Address, Phone Number, Password, Confirm Password and an optional National ID / CCCD section, with Create Customer Account and Sign In actions.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Check Full Name, Email Address, Phone Number, Password and Confirm Password.
5. Click Additional profile information (optional).
6. Check National ID / CCCD, Create Customer Account and Sign In.

### FRMS-ST-0002 — Verify Customer Login form layout and controls.

- Surface: UI; source: `Khánh:MOD01_Auth:TC2`
- Expected Output (existing manifest; **SRS approval pending**): The Customer Sign In page displays Phone Number and Password fields, a Sign In button and a Create Customer Account link; the password entry is masked.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Check Phone Number, Password, Sign In and Create Customer Account.
4. Check that Password is hidden while typing.

### FRMS-ST-0003 — Check Employee Login form and actual submit control.

- Surface: UI; source: `Khánh:MOD01_Auth:TC3`
- Expected Output (existing manifest; **SRS approval pending**): The Employee Sign In page displays Email and Password fields and the Sign In action; it does not use a separate Internal Sign In button.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Employee Sign In in the Account section of the footer.
3. Check Email, Password, Sign In and Customer Sign In.

### FRMS-ST-0004 — Register a customer using valid required registration data.

- Surface: UI; source: `Khánh:MOD01_Auth:TC4`
- Expected Output (existing manifest; **SRS approval pending**): After valid registration, the browser opens Customer Sign In and displays "Your account was created. Sign in with your phone number and password." It does not automatically log in or reserve a unit.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid values in Full Name, Email Address, Phone Number, Password and Confirm Password.
5. Click Create Customer Account.
6. Check Customer Sign In and the account-created notice.

### FRMS-ST-0005 — Customer Login: successful main flow with registered credentials.

- Surface: UI; source: `Khánh:MOD01_Auth:TC5`
- Expected Output (existing manifest; **SRS approval pending**): The customer is navigated to the customer portal (/customer), or the saved reservation step if login resumed a selection. Customer content becomes accessible.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter the Phone Number and Password for an ACTIVE Customer account.
4. Click Sign In.
5. Check the Customer portal and the signed-in account.

### FRMS-ST-0006 — Customer Login: alternate flow with incorrect password.

- Surface: UI; source: `Khánh:MOD01_Auth:TC6`
- Expected Output (existing manifest; **SRS approval pending**): Customer Sign In remains open. The customer-facing error alert says "The phone number or password is incorrect. Check both and try signing in again." No customer portal opens.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter the Phone Number for an ACTIVE Customer account.
4. Enter an incorrect Password.
5. Click Sign In.
6. Check the error and confirm the Customer portal does not open.

### FRMS-ST-0007 — Customer Login: alternate flow with deactivated account.

- Surface: UI; source: `Khánh:MOD01_Auth:TC7`
- Expected Output (existing manifest; **SRS approval pending**): The sign-in form shows "Your account is inactive. Please contact support." No customer portal is opened.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter the Phone Number and correct Password for an INACTIVE Customer account.
4. Click Sign In.
5. Check the inactive-account message and confirm the Customer portal does not open.

### FRMS-ST-0008 — Employee Login: successful main flow for authorized Facility Staff.

- Surface: UI; source: `Khánh:MOD01_Auth:TC8`
- Expected Output (existing manifest; **SRS approval pending**): The authorized employee is navigated to the portal for that employee role; Facility Staff sees the staff work area.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Employee Sign In in the Account section of the footer.
3. Enter the Email and Password for an ACTIVE Facility Staff account.
4. Click Sign In.
5. Check the Facility Staff work area.

### FRMS-ST-0009 — Check Phone Number: valid 10-digit number with standard prefix.

- Surface: UI; source: `Khánh:MOD01_Auth:TC9`
- Expected Output (existing manifest; **SRS approval pending**): The ten-digit phone input remains valid in the registration frontend (no inline phone-number error). This client-side case deliberately blocks submission with another invalid field; server registration is not asserted.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Client-side acceptance only; no registration success or backend acceptance assertion.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Phone Number.
5. Keep another required field invalid so no account is created.
6. Enter a valid 10-digit number with an approved prefix in Phone Number.
7. Click Create Customer Account.

### FRMS-ST-0010 — Check Phone Number: input contains non-digit characters.

- Surface: UI; source: `Khánh:MOD01_Auth:TC10`
- Expected Output (existing manifest; **SRS approval pending**): Registration is not completed if the submitted phone number fails server validation; the form shows its mapped validation feedback. The frontend does not itself enforce a digits-only phone rule.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: No frontend digits-only/ten-digit length validation; final server outcome requires SRS and isolated runtime confirmation.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Phone Number.
5. Enter non-digit characters in Phone Number.
6. Click Create Customer Account.

### FRMS-ST-0011 — Check Phone Number: input is empty.

- Surface: UI; source: `Khánh:MOD01_Auth:TC11`
- Expected Output (existing manifest; **SRS approval pending**): An inline message "Enter your phone number." appears below Phone Number, and submission does not proceed.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Phone Number.
5. Enter an empty value in Phone Number.
6. Click Create Customer Account.

### FRMS-ST-0012 — Check Email Address: valid RFC format.

- Surface: UI; source: `Khánh:MOD01_Auth:TC12`
- Expected Output (existing manifest; **SRS approval pending**): The RFC-formatted email passes the frontend email-format check without an inline email error. An unrelated required field is left invalid to prevent account creation.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Client-side acceptance only; no registration success or backend acceptance assertion.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Email Address.
5. Keep another required field invalid so no account is created.
6. Enter a valid email address in Email Address.
7. Click Create Customer Account.

### FRMS-ST-0013 — Check Email Address: missing '@' character.

- Surface: UI; source: `Khánh:MOD01_Auth:TC13`
- Expected Output (existing manifest; **SRS approval pending**): An inline message "Enter a valid email address." appears under Email Address; no account is submitted for creation.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Email Address.
5. Enter an email address without @ in Email Address.
6. Click Create Customer Account.

### FRMS-ST-0014 — Check National ID / CCCD: valid 12-digit number.

- Surface: UI; source: `Khánh:MOD01_Auth:TC14`
- Expected Output (existing manifest; **SRS approval pending**): A 12-digit National ID / CCCD passes the optional frontend format check without CCCD feedback. An unrelated required field prevents any registration request.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Client-side acceptance only; no registration success or backend acceptance assertion.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Click Additional profile information (optional).
5. Enter valid data in all required fields except National ID / CCCD.
6. Keep another required field invalid so no account is created.
7. Enter exactly 12 digits in National ID / CCCD.
8. Click Create Customer Account.

### FRMS-ST-0015 — Check National ID / CCCD: length not equal to 12 digits.

- Surface: UI; source: `Khánh:MOD01_Auth:TC15`
- Expected Output (existing manifest; **SRS approval pending**): The National ID / CCCD field shows "Enter exactly 12 digits (0–9) for your National ID." when a nonempty value does not consist of exactly 12 digits.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Click Additional profile information (optional).
5. Enter valid data in all required fields except National ID / CCCD.
6. Enter a non-empty value not consisting of exactly 12 digits in National ID / CCCD.
7. Click Create Customer Account.

### FRMS-ST-0016 — Check Password length: Lower bound L(Password) - 1 = 7 characters.

- Surface: UI; source: `Khánh:MOD01_Auth:TC16`
- Expected Output (existing manifest; **SRS approval pending**): The Password field displays "Password must contain 8 to 64 characters." and the registration remains on the form.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Password.
5. Enter exactly 7 characters in Password.
6. Click Create Customer Account.

### FRMS-ST-0017 — Check Password length: Lower bound L(Password) = 8 characters.

- Surface: UI; source: `Khánh:MOD01_Auth:TC17`
- Expected Output (existing manifest; **SRS approval pending**): An eight-character password passes the frontend 8–64 character/72-byte check without a Password field error; another invalid required field blocks registration.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Client-side acceptance only; no registration success or backend acceptance assertion.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Password.
5. Keep another required field invalid so no account is created.
6. Enter exactly 8 characters in Password.
7. Click Create Customer Account.

### FRMS-ST-0018 — Check Password length: Upper bound U(Password) = 64 characters.

- Surface: UI; source: `Khánh:MOD01_Auth:TC18`
- Expected Output (existing manifest; **SRS approval pending**): A 64-character ASCII password passes the frontend 8–64 character/72-byte check without a Password field error; another invalid required field blocks registration.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Client-side acceptance only; no registration success or backend acceptance assertion.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Password.
5. Keep another required field invalid so no account is created.
6. Enter exactly 64 ASCII characters in Password.
7. Click Create Customer Account.

### FRMS-ST-0019 — Check Password length: Upper bound U(Password) + 1 = 65 characters.

- Surface: UI; source: `Khánh:MOD01_Auth:TC19`
- Expected Output (existing manifest; **SRS approval pending**): The registration Password input has maxLength=64. A 65th normally typed character is not accepted, so the submit-time 65-character validation message is not reachable through ordinary typing.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: UI maxLength blocks direct entry of 65 characters; direct invalid submission is not reachable through normal typing.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Password.
5. Enter a 64-character ASCII password in Password.
6. Try to type another character in Password.
7. Check that the input stays within the 64-character limit.

### FRMS-ST-0020 — Check Phone Number length: L(Phone) - 1 = 9 digits.

- Surface: UI; source: `Khánh:MOD01_Auth:TC20`
- Expected Output (existing manifest; **SRS approval pending**): The registration form has no client-side 10-digit phone-length check. Server rejection, if any, is presented through the form error alert; a fixed phone-length message cannot be asserted from frontend code.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: No frontend digits-only/ten-digit length validation; final server outcome requires SRS and isolated runtime confirmation.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Phone Number.
5. Enter exactly 9 digits in Phone Number.
6. Click Create Customer Account.

### FRMS-ST-0021 — Check Phone Number length: U(Phone) + 1 = 11 digits.

- Surface: UI; source: `Khánh:MOD01_Auth:TC21`
- Expected Output (existing manifest; **SRS approval pending**): The registration form has no client-side 10-digit phone-length check. Server rejection, if any, is presented through the form error alert; a fixed phone-length message cannot be asserted from frontend code.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: No frontend digits-only/ten-digit length validation; final server outcome requires SRS and isolated runtime confirmation.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Phone Number.
5. Enter exactly 11 digits in Phone Number.
6. Click Create Customer Account.

### FRMS-ST-0022 — Unregistered Guest → Submit Registration → ACTIVE Customer Account.

- Surface: UI; source: `Khánh:MOD01_Auth:TC22`
- Expected Output (existing manifest; **SRS approval pending**): The browser opens Customer Sign In with the account-created notice after successful registration. The newly registered customer must sign in to reach the customer portal.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid values in Full Name, Email Address, Phone Number, Password and Confirm Password.
5. Click Create Customer Account.
6. Check Customer Sign In and the account-created notice.

### FRMS-ST-0023 — ACTIVE Customer Account → Admin Deactivates → INACTIVE Customer Account.

- Surface: UI; source: `Khánh:MOD01_Auth:TC23`
- Expected Output (existing manifest; **SRS approval pending**): After an eligible admin confirms deactivation, Customer account status is displayed as INACTIVE; the admin page shows "Customer deactivated". Further sign-in is refused.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Requires isolated eligible account and approved state-changing fixture.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Employee Sign In in the Account section of the footer.
3. Enter an ACTIVE System Administrator account Email and Password.
4. Click Sign In.
5. Click Customer access.
6. Choose an eligible ACTIVE Customer with no confirmed Reservation or active Contract.
7. Click Deactivate.
8. Click Deactivate Customer.
9. Check that the Customer status changes to INACTIVE.

### FRMS-ST-0024 — Apply decision rule R01: Customer Role + Customer Sign In page → Success.

- Surface: UI; source: `Khánh:MOD01_Auth:TC24`
- Expected Output (existing manifest; **SRS approval pending**): Successful customer Sign In opens /customer or resumes the saved customer selection. No backend role code is shown as output.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter the Phone Number and Password for an ACTIVE Customer account.
4. Click Sign In.
5. Check the Customer portal and the signed-in account.

### FRMS-ST-0025 — Apply decision rule R02: Facility Staff Role + Customer Sign In page → Rejected.

- Surface: UI; source: `Khánh:MOD01_Auth:TC25`
- Expected Output (existing manifest; **SRS approval pending**): A staff account must use Employee Sign In. If a staff account is returned through Customer Sign In, the frontend logs out and navigates to the Forbidden page instead of the customer portal.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter the credentials of an ACTIVE Facility Staff account in the Customer form.
4. Click Sign In.
5. Check that the Customer portal is not opened.

### FRMS-ST-0147 — ACTIVE customer signs in with phone and password; /auth/me returns the customer's own identity.

- Surface: UI; source: `Khoa:MOD01_Security:SEC-TC-01`
- Expected Output (existing manifest; **SRS approval pending**): After customer sign-in, the customer portal/profile shows the signed-in customer's information. The direct /auth/me response fields are not rendered by this UI test step.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter the Phone Number and Password for an ACTIVE Customer account.
4. Click Sign In.
5. Check the Customer portal and the signed-in account.

### FRMS-ST-0148 — ACTIVE Facility Staff signs in with email; /auth/me returns role and facility scope.

- Surface: UI; source: `Khoa:MOD01_Security:SEC-TC-02`
- Expected Output (existing manifest; **SRS approval pending**): After employee sign-in, the role-specific employee portal opens with the employee's permitted work area. The raw /auth/me response is not a frontend output.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Employee Sign In in the Account section of the footer.
3. Enter the Email and Password for an ACTIVE Facility Staff account.
4. Click Sign In.
5. Check the Facility Staff work area.

### FRMS-ST-0149 — A protected endpoint called without a token returns the standard 401 error body.

- Surface: Technical; source: `Khoa:MOD01_Security:SEC-TC-03`
- Expected Output (existing manifest; **SRS approval pending**): Not applicable to the current frontend: This procedure calls a protected endpoint without opening a frontend screen. No user-visible Expected Output can be established for this procedure; redesign it as a UI scenario or retain it as a separate API/system test.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Transfer original token/health endpoint test to the separate technical suite; retain source trace.

- Procedure: **Transfer/Hold**, not manual UI.

### FRMS-ST-0150 — Check authentication rejects a token with a tampered payload/signature.

- Surface: Technical; source: `Khoa:MOD01_Security:SEC-TC-04``
- Expected Output (existing manifest; **SRS approval pending**): Not applicable to the current frontend: This procedure mutates an authorization token and calls an endpoint; there is no corresponding UI state to assert. No user-visible Expected Output can be established for this procedure; redesign it as a UI scenario or retain it as a separate API/system test.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Transfer original token/health endpoint test to the separate technical suite; retain source trace.

- Procedure: **Transfer/Hold**, not manual UI.

### FRMS-ST-0150-B — Check authentication rejects a structurally malformed token.

- Surface: Technical; source: `Khoa:MOD01_Security:SEC-TC-04``
- Expected Output (existing manifest; **SRS approval pending**): Not applicable to the current frontend: This procedure sends a malformed bearer token directly; there is no corresponding UI state to assert. No user-visible Expected Output can be established for this procedure; redesign it as a UI scenario or retain it as a separate API/system test.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Transfer original token/health endpoint test to the separate technical suite; retain source trace.

- Procedure: **Transfer/Hold**, not manual UI.

### FRMS-ST-0151 — Check token acceptance immediately before expiry.

- Surface: Technical; source: `Khoa:MOD01_Security:SEC-TC-05``
- Expected Output (existing manifest; **SRS approval pending**): Not applicable to the current frontend: Expiry-minus-one-second behavior requires a controlled backend clock/token, not a rendered page result. No user-visible Expected Output can be established for this procedure; redesign it as a UI scenario or retain it as a separate API/system test.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Transfer original token/health endpoint test to the separate technical suite; retain source trace.

- Procedure: **Transfer/Hold**, not manual UI.

### FRMS-ST-0151-B — Check token rejection immediately after expiry.

- Surface: Technical; source: `Khoa:MOD01_Security:SEC-TC-05``
- Expected Output (existing manifest; **SRS approval pending**): Not applicable to the current frontend: Expiry-plus-one-second behavior requires a controlled backend clock/token, not a rendered page result. No user-visible Expected Output can be established for this procedure; redesign it as a UI scenario or retain it as a separate API/system test.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Transfer original token/health endpoint test to the separate technical suite; retain source trace.

- Procedure: **Transfer/Hold**, not manual UI.

### FRMS-ST-0152 — Session of a customer deactivated by the System Admin stops working.

- Surface: UI; source: `Khoa:MOD01_Security:SEC-TC-06`
- Expected Output (existing manifest; **SRS approval pending**): After admin deactivation, customer pages requiring a valid active session are no longer accessible; the user returns to sign-in or sees the page's access feedback. After reactivation and fresh login, the customer portal opens again.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Needs approved multi-actor state-change journey; draft has no authorized deactivation action and must be rewritten before execution.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter the Phone Number and Password for a Customer account that is ACTIVE.
4. Click Sign In.
5. Click Sign Out.
6. Check the Customer portal cannot be opened again after the assigned account is deactivated through an approved fixture.

### FRMS-ST-0153 — Rule R01: a Customer token cannot call a staff-only endpoint.

- Surface: Mixed; source: `Khoa:MOD01_Security:SEC-TC-07`
- Expected Output (existing manifest; **SRS approval pending**): Opening the Staff Work List as a customer is blocked by route authorization and shows the Forbidden page or redirects to the applicable sign-in route; no staff work items are displayed.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Current case asserts a token/endpoint rule. Separate a role-visible denied UI path from the API-only contract and request SRS approval.

- Procedure: **Transfer/Hold**, not manual UI.

### FRMS-ST-0154 — Rule R02: a Facility Staff token cannot call a System Admin endpoint.

- Surface: Mixed; source: `Khoa:MOD01_Security:SEC-TC-08`
- Expected Output (existing manifest; **SRS approval pending**): Opening System Administrator pages as Facility Staff is blocked by route authorization; no admin user management data is displayed.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Current case asserts a token/endpoint rule. Separate a role-visible denied UI path from the API-only contract and request SRS approval.

- Procedure: **Transfer/Hold**, not manual UI.

### FRMS-ST-0155 — Check health endpoint when database connectivity is healthy.

- Surface: Technical; source: `Khoa:MOD01_Security:SEC-TC-09``
- Expected Output (existing manifest; **SRS approval pending**): Not applicable to the current frontend: The health endpoint is not exposed as a user-facing FRMS page. No user-visible Expected Output can be established for this procedure; redesign it as a UI scenario or retain it as a separate API/system test.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Transfer original token/health endpoint test to the separate technical suite; retain source trace.

- Procedure: **Transfer/Hold**, not manual UI.

### FRMS-ST-0155-B — Check health endpoint when test database connectivity is unavailable.

- Surface: Technical; source: `Khoa:MOD01_Security:SEC-TC-09``
- Expected Output (existing manifest; **SRS approval pending**): Not applicable to the current frontend: Database disconnect/health endpoint is not exposed as a user-facing FRMS page. No user-visible Expected Output can be established for this procedure; redesign it as a UI scenario or retain it as a separate API/system test.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Transfer original token/health endpoint test to the separate technical suite; retain source trace.

- Procedure: **Transfer/Hold**, not manual UI.

### FRMS-ST-0197 — Active customer signs in with a valid phone number and password.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-01`
- Expected Output (existing manifest; **SRS approval pending**): Sign In opens the customer portal for an ACTIVE customer account.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter the Phone Number and Password for an ACTIVE Customer account.
4. Click Sign In.
5. Check the Customer portal and the signed-in account.

### FRMS-ST-0198 — Customer sign-in rejects an incorrect password.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-02`
- Expected Output (existing manifest; **SRS approval pending**): Customer Sign In remains open. The customer-facing error alert says "The phone number or password is incorrect. Check both and try signing in again." No customer portal opens.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter the Phone Number for an ACTIVE Customer account.
4. Enter an incorrect Password.
5. Click Sign In.
6. Check the error and confirm the Customer portal does not open.

### FRMS-ST-0199 — Customer sign-in rejects an unregistered phone number.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-03`
- Expected Output (existing manifest; **SRS approval pending**): The form stays on Customer Sign In and shows "Incorrect sign-in details." No account-existence hint is presented.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter a Phone Number that is not registered.
4. Enter a valid-format Password.
5. Click Sign In.
6. Check the sign-in error and confirm the Customer portal does not open.

### FRMS-ST-0200 — Inactive customer cannot sign in.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-04`
- Expected Output (existing manifest; **SRS approval pending**): Customer Sign In shows "Your account is inactive. Please contact support." No customer portal opens.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Enter the Phone Number and correct Password for an INACTIVE Customer account.
4. Click Sign In.
5. Check the inactive-account message and confirm the Customer portal does not open.

### FRMS-ST-0201 — Active facility staff signs in with email and password.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-05`
- Expected Output (existing manifest; **SRS approval pending**): Employee Sign In navigates to the Facility Staff portal for valid active staff credentials.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Employee Sign In in the Account section of the footer.
3. Enter the Email and Password for an ACTIVE Facility Staff account.
4. Click Sign In.
5. Check the Facility Staff work area.

### FRMS-ST-0202 — Employee sign-in rejects an incorrect password.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-06`
- Expected Output (existing manifest; **SRS approval pending**): Employee Sign In displays "Incorrect sign-in details." and does not navigate to the staff portal.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Employee Sign In in the Account section of the footer.
3. Enter the Email for an ACTIVE Facility Staff account.
4. Enter an incorrect Password.
5. Click Sign In.
6. Check the sign-in error and confirm the Staff portal does not open.

### FRMS-ST-0203 — Employee sign-in rejects an unregistered email.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-07`
- Expected Output (existing manifest; **SRS approval pending**): Employee Sign In displays "Incorrect sign-in details." and does not disclose whether the email is registered.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Employee Sign In in the Account section of the footer.
3. Enter an Email that is not registered.
4. Enter a valid-format Password.
5. Click Sign In.
6. Check the sign-in error and confirm the Staff portal does not open.

### FRMS-ST-0204 — Check required Full Name validation on customer registration.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-08``
- Expected Output (existing manifest; **SRS approval pending**): Inline field feedback reads "Enter your full name." and submission remains blocked.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid values in all required fields except Full Name.
5. Leave Full Name blank.
6. Click Create Customer Account.
7. Check the error shown for Full Name.

### FRMS-ST-0204-B — Check required Email Address validation on customer registration.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-08``
- Expected Output (existing manifest; **SRS approval pending**): Inline field feedback reads "Enter a valid email address." and submission remains blocked.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid values in all required fields except Email Address.
5. Leave Email Address blank.
6. Click Create Customer Account.
7. Check the error shown for Email Address.

### FRMS-ST-0204-C — Check required Phone Number validation on customer registration.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-08``
- Expected Output (existing manifest; **SRS approval pending**): Inline field feedback reads "Enter your phone number." and submission remains blocked.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid values in all required fields except Phone Number.
5. Leave Phone Number blank.
6. Click Create Customer Account.
7. Check the error shown for Phone Number.

### FRMS-ST-0204-D — Check required Password validation on customer registration.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-08``
- Expected Output (existing manifest; **SRS approval pending**): Inline field feedback reads "Password must contain 8 to 64 characters." and submission remains blocked.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid values in all required fields except Password.
5. Leave Password blank.
6. Click Create Customer Account.
7. Check the error shown for Password.

### FRMS-ST-0204-E — Check required Confirm Password validation on customer registration.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-08``
- Expected Output (existing manifest; **SRS approval pending**): Inline field feedback reads "Re-enter your password." and submission remains blocked.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid values in all required fields except Confirm Password.
5. Leave Confirm Password blank.
6. Click Create Customer Account.
7. Check the error shown for Confirm Password.

### FRMS-ST-0205 — Customer registration rejects a password shorter than eight characters.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-09`
- Expected Output (existing manifest; **SRS approval pending**): Inline Password feedback reads "Password must contain 8 to 64 characters." and the Create Customer Account request is not sent.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid data in all required fields except Password.
5. Enter fewer than 8 characters in Password.
6. Click Create Customer Account.

### FRMS-ST-0206 — Customer registration rejects mismatched passwords.

- Surface: UI; source: `Mẫn:Module 1:AUTH-TC-10`
- Expected Output (existing manifest; **SRS approval pending**): Inline Confirm Password feedback reads "The passwords do not match." and account creation is blocked.
- Inter-test case Dependence: *(blank; fixture eligibility is external)*
- Result: `Untested`; Test date: *(blank)*; Note: *(blank)*
- Review: Exact UI and approved oracle require runtime verification.

**Test Case Procedure (draft):**

1. Open Homepage.
2. Click Sign In.
3. Click Create Customer Account.
4. Enter valid Full Name, Email Address, Phone Number and Password.
5. Enter a different value in Confirm Password.
6. Click Create Customer Account.
7. Check the password-mismatch message.
