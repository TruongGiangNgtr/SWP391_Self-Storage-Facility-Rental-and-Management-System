import { expect, test, type Page } from '@playwright/test'

// Batch 09: read-only authentication and client-side boundary scenarios.
// Do not submit valid registration requests to the shared/remote database.
const REGISTER = '/auth/customer/register'
const CUSTOMER_LOGIN = '/auth/customer/login'
const STAFF_LOGIN = '/auth/employee/login'
test.setTimeout(90_000)
// File-level override: trace is worker-scoped and cannot be configured inside describe.
// This file handles demo credentials and a signed JWT; do not capture them in traces.
test.use({ trace: 'off' })

const PASSWORD = process.env.FRMS_TEST_CUSTOMER_1_PASSWORD ?? '11111111'
const STAFF_PASSWORD = process.env.FRMS_TEST_STAFF_PASSWORD ?? '11111111'

function customerPhone(): string { return process.env.FRMS_TEST_CUSTOMER_1_PHONE ?? '0901000001' }
function staffEmail(): string { return process.env.FRMS_TEST_STAFF_01_EMAIL ?? 'staff.f01@example.com' }

async function fillRegistration(page: Page, changes: Record<string, string>) {
  const fields = {
    'Full Name': 'Automated Validation Only',
    'Email Address': 'automation@example.test',
    'Phone Number': '0901000001',
    'Password': 'Valid123!',
    'Confirm Password': 'Valid123!',
    ...changes,
  }
  for (const [name, value] of Object.entries(fields)) {
    await page.getByLabel(name, { exact: true }).fill(value)
  }
}
async function assertClientValidationWithoutRegistration(page: Page, expectedError: string) {
  let registrationPosts = 0
  page.on('request', request => {
    if (request.method() === 'POST' && new URL(request.url()).pathname === '/api/v1/auth/customer/register') registrationPosts++
  })
  await page.getByRole('button', { name: 'Create Customer Account', exact: true }).click()
  await expect(page).toHaveURL(/\/auth\/customer\/register(?:[?#]|$)/)
  await expect(page.getByText(expectedError, { exact: true })).toBeVisible()
  expect(registrationPosts, 'registration must remain blocked by another invalid field').toBe(0)
}
async function loginCustomer(page: Page) {
  await page.goto(CUSTOMER_LOGIN)
  await page.getByLabel('Phone Number', { exact: true }).fill(customerPhone())
  await page.getByLabel('Password', { exact: true }).fill(PASSWORD)
  await page.getByRole('button', { name: 'Sign In', exact: true }).click()
  await expect(page).toHaveURL(/\/customer(?:[/?#]|$)/, { timeout: 30_000 })
}
async function loginStaff(page: Page) {
  await page.goto(STAFF_LOGIN)
  await page.getByLabel('Email', { exact: true }).fill(staffEmail())
  await page.getByLabel('Password', { exact: true }).fill(STAFF_PASSWORD)
  await page.getByRole('button', { name: 'Sign In', exact: true }).click()
  await expect(page).toHaveURL(/\/staff\/work-list(?:[/?#]|$)/, { timeout: 30_000 })
}

// Isolate protected-route tests from the Customer Portal's unrelated background GETs.
// Use the real authentication API and validate the signed session before opening
// the target route. Never log the access token or serialize it into reports.
async function openAuthenticatedCustomerRoute(page: Page, route: string) {
  const api = (process.env.FRMS_API_URL ?? 'http://127.0.0.1:5164').replace(/\/$/, '')
  const login = await page.request.post(`${api}/api/v1/auth/customer/login`, {
    data: { phoneNumber: customerPhone(), password: PASSWORD },
    timeout: 30_000,
  })
  if (login.status() !== 200) {
    throw new Error(`FRMS_FIXTURE_AUTH_FAILED: Customer login returned HTTP ${login.status()}. Check ACTIVE demo account and password.`)
  }
  const response = await login.json() as { data?: { accessToken?: string } }
  const token = response.data?.accessToken
  if (!token || token.split('.').length !== 3) {
    throw new Error('FRMS_FIXTURE_AUTH_FAILED: Customer login did not return a signed access token.')
  }

  const current = await page.request.get(`${api}/api/v1/auth/me`, {
    headers: { Authorization: `Bearer ${token}` },
    timeout: 30_000,
  })
  if (current.status() !== 200) {
    throw new Error(`FRMS_FIXTURE_AUTH_FAILED: /auth/me returned HTTP ${current.status()} immediately after login.`)
  }
  const account = await current.json() as { data?: { role?: string; status?: string } }
  if (account.data?.role !== 'CUSTOMER' || account.data?.status !== 'ACTIVE') {
    throw new Error(`FRMS_FIXTURE_AUTH_FAILED: Expected ACTIVE CUSTOMER; received role=${account.data?.role ?? 'missing'}, status=${account.data?.status ?? 'missing'}.`)
  }

  // Same-origin sessionStorage requires a browser document first. Do not use
  // addInitScript to reinsert credentials after a real 401: that masks logout.
  await page.goto(CUSTOMER_LOGIN)
  await page.evaluate(value => { sessionStorage.setItem('frms.accessToken', value) }, token)
  await page.goto(route)
}

async function rejectBadCustomerPassword(page: Page) {
  await page.goto(CUSTOMER_LOGIN)
  await page.getByLabel('Phone Number', { exact: true }).fill(customerPhone())
  await page.getByLabel('Password', { exact: true }).fill(`${PASSWORD}__wrong`)
  await page.getByRole('button', { name: 'Sign In', exact: true }).click()
  await expect(page).toHaveURL(/\/auth\/customer\/login(?:[?#]|$)/, { timeout: 30_000 })
  // CustomerErrorAudience produces user-facing copy, not the raw API error code.
  await expect(page.getByRole('alert')).toContainText('The phone number or password is incorrect.', { timeout: 30_000 })
}

// These cases assert the *client-side class*, not a successful registration.
// Deliberately invalidate an unrelated field to guarantee no DB mutation.
test('FRMS-ST-0009 - Valid ten digit phone passes client-side registration validation', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Phone Number': '0901000001', 'Email Address': 'not-an-email' })
  await assertClientValidationWithoutRegistration(page, 'Enter a valid email address.')
  await expect(page.getByLabel('Phone Number', { exact: true })).toHaveAttribute('aria-invalid', 'false')
})

test('FRMS-ST-0012 - Valid email format passes client-side validation', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Full Name': '', 'Email Address': 'frms.valid@example.test' })
  await assertClientValidationWithoutRegistration(page, 'Enter your full name.')
  await expect(page.getByLabel('Email Address', { exact: true })).toHaveAttribute('aria-invalid', 'false')
})

test('FRMS-ST-0014 - Twelve digit optional CCCD passes client-side validation', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Full Name': '' })
  await page.getByText('Additional profile information (optional)', { exact: true }).click()
  await page.getByLabel('National ID / CCCD', { exact: true }).fill('001200000001')
  await assertClientValidationWithoutRegistration(page, 'Enter your full name.')
  await expect(page.getByLabel('National ID / CCCD', { exact: true })).toHaveAttribute('aria-invalid', 'false')
})

test('FRMS-ST-0017 - Eight character password passes client-side lower boundary', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Full Name': '', Password: 'Abc123!@', 'Confirm Password': 'Abc123!@' })
  await assertClientValidationWithoutRegistration(page, 'Enter your full name.')
  await expect(page.getByLabel('Password', { exact: true })).toHaveAttribute('aria-invalid', 'false')
})

test('FRMS-ST-0018 - Sixty-four character password passes client-side upper boundary', async ({ page }) => {
  const password = 'Aa1!' + 'x'.repeat(60)
  expect(password).toHaveLength(64)
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Full Name': '', Password: password, 'Confirm Password': password })
  await assertClientValidationWithoutRegistration(page, 'Enter your full name.')
  await expect(page.getByLabel('Password', { exact: true })).toHaveAttribute('aria-invalid', 'false')
})

test('FRMS-ST-0019 - Password field prevents typing sixty-five characters', async ({ page }) => {
  await page.goto(REGISTER)
  await expect(page.getByLabel('Password', { exact: true })).toHaveAttribute('maxlength', '64')
  await page.getByLabel('Password', { exact: true }).pressSequentially('x'.repeat(65))
  await expect(page.getByLabel('Password', { exact: true })).toHaveValue('x'.repeat(64))
  // The browser caps the input; a 65-char submit-time validation message is not reachable through normal typing.
})

test('FRMS-ST-0006 - Incorrect customer password is rejected with frontend feedback', async ({ page }) => {
  await rejectBadCustomerPassword(page)
})

test('FRMS-ST-0198 - Customer wrong-password sign in stays unauthenticated', async ({ page }) => {
  await rejectBadCustomerPassword(page)
  await expect(page.getByRole('heading', { name: 'Sign in to your account' })).toBeVisible()
})

test('FRMS-ST-0202 - Staff wrong-password sign in is rejected', async ({ page }) => {
  await page.goto(STAFF_LOGIN)
  await page.getByLabel('Email', { exact: true }).fill(staffEmail())
  await page.getByLabel('Password', { exact: true }).fill(`${STAFF_PASSWORD}__wrong`)
  await page.getByRole('button', { name: 'Sign In', exact: true }).click()
  await expect(page).toHaveURL(/\/auth\/employee\/login(?:[?#]|$)/, { timeout: 30_000 })
  await expect(page.getByRole('alert')).toContainText('Incorrect sign-in details.', { timeout: 30_000 })
})

test('FRMS-ST-0148 - Staff role reaches the authorized work list', async ({ page }) => {
  await loginStaff(page)
  await expect(page.getByRole('main')).toBeVisible()
  await expect(page).not.toHaveURL(/\/forbidden/)
  // This UI case does not assert raw /auth/me JSON or prove cross-facility data isolation.
})

test('FRMS-ST-0154 - Facility staff is forbidden from administrator pages', async ({ page }) => {
  await loginStaff(page)
  await page.goto('/admin/users')
  await expect(page).toHaveURL(/\/forbidden(?:[?#]|$)/, { timeout: 30_000 })
  await expect(page.getByRole('heading', { name: /403.*Access Denied/ })).toBeVisible()
})

// Signed-JWT route tests reuse the file-level trace policy above.
test.describe('Authenticated Customer route boundaries', () => {

  test('FRMS-ST-0147 - Customer identity is shown in own account settings', async ({ page }) => {
    await openAuthenticatedCustomerRoute(page, '/customer/account-settings')
    await expect(page.getByRole('heading', { name: 'Account Settings', exact: true })).toBeVisible({ timeout: 15_000 })
    await expect(page.locator('#settings-phone')).toHaveValue(customerPhone(), { timeout: 30_000 })
    await expect(page.locator('#settings-full-name')).not.toHaveValue('', { timeout: 30_000 })
  })

  test('FRMS-ST-0153 - Customer is forbidden from staff work list', async ({ page }) => {
    await openAuthenticatedCustomerRoute(page, '/staff/work-list')
    await expect(page).toHaveURL(/\/forbidden(?:[?#]|$)/, { timeout: 30_000 })
    await expect(page.getByRole('heading', { name: /403.*Access Denied/ })).toBeVisible()
  })
})
