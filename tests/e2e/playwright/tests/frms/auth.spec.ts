import { expect, test, type Page } from '@playwright/test'

const CUSTOMER_LOGIN = '/auth/customer/login'
const EMPLOYEE_LOGIN = '/auth/employee/login'
const REGISTER = '/auth/customer/register'

async function fillRegistration(page: Page, changes: Record<string, string> = {}) {
  const data = {
    'Full Name': 'Nguyen Van A',
    'Email Address': 'frms-auto-validation@example.test',
    'Phone Number': '0901000011',
    'Password': 'Pass123@',
    'Confirm Password': 'Pass123@',
    ...changes,
  }
  for (const [label, value] of Object.entries(data)) await page.getByLabel(label, { exact: true }).fill(value)
}
async function submitRegistration(page: Page) {
  await page.getByRole('button', { name: 'Create Customer Account', exact: true }).click()
  await expect(page).toHaveURL(/\/auth\/customer\/register(?:[?#]|$)/)
}
function requiredEnv(key: string): string {
  const value = process.env[key]
  if (!value) throw new Error(`FRMS_FIXTURE_MISSING: configure ${key} for an isolated ACTIVE test account.`)
  return value
}
async function customerSignIn(page: Page) {
  await page.goto(CUSTOMER_LOGIN)
  await page.getByLabel('Phone Number').fill(process.env.FRMS_TEST_CUSTOMER_1_PHONE ?? '0901000001')
  await page.getByLabel('Password', { exact: true }).fill(requiredEnv('FRMS_TEST_CUSTOMER_1_PASSWORD'))
  await page.getByRole('button', { name: 'Sign In', exact: true }).click()
  await expect(page).toHaveURL(/\/customer(?:[/?#]|$)/)
}
async function staffSignIn(page: Page) {
  await page.goto(EMPLOYEE_LOGIN)
  await page.getByLabel('Email', { exact: true }).fill(process.env.FRMS_TEST_STAFF_01_EMAIL ?? 'staff.f01@example.com')
  await page.getByLabel('Password', { exact: true }).fill(requiredEnv('FRMS_TEST_STAFF_PASSWORD'))
  await page.getByRole('button', { name: 'Sign In', exact: true }).click()
  await expect(page).toHaveURL(/\/staff\/work-list(?:[/?#]|$)/)
}

test('FRMS-ST-0001 - Registration shows required labels and optional CCCD', async ({ page }) => {
  await page.goto(REGISTER)
  for (const label of ['Full Name', 'Email Address', 'Phone Number', 'Password', 'Confirm Password']) {
    await expect(page.getByLabel(label, { exact: true })).toBeVisible()
  }
  await expect(page.getByRole('button', { name: 'Create Customer Account', exact: true })).toBeVisible()
  await expect(page.locator('#customer-main').getByRole('link', { name: 'Sign In', exact: true })).toBeVisible()
  await page.getByText('Additional profile information (optional)').click()
  await expect(page.getByLabel('National ID / CCCD')).toBeVisible()
})

test('FRMS-ST-0002 - Customer login has phone, masked password and actions', async ({ page }) => {
  await page.goto(CUSTOMER_LOGIN)
  await expect(page.getByLabel('Phone Number')).toBeVisible()
  await expect(page.getByLabel('Password', { exact: true })).toHaveAttribute('type', 'password')
  await expect(page.getByRole('button', { name: 'Sign In', exact: true })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Create Customer Account', exact: true })).toBeVisible()
})

test('FRMS-ST-0003 - Employee login has email, masked password and Sign In', async ({ page }) => {
  await page.goto(EMPLOYEE_LOGIN)
  await expect(page.getByLabel('Email', { exact: true })).toBeVisible()
  await expect(page.getByLabel('Password', { exact: true })).toHaveAttribute('type', 'password')
  await expect(page.getByRole('button', { name: 'Sign In', exact: true })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Internal Sign In' })).toHaveCount(0)
})

test('FRMS-ST-0011 - Empty phone is rejected inline at registration', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Phone Number': '' })
  await submitRegistration(page)
  await expect(page.getByText('Enter your phone number.', { exact: true })).toBeVisible()
})

test('FRMS-ST-0013 - Email without at sign is rejected inline', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Email Address': 'frms.test.example.test' })
  await submitRegistration(page)
  await expect(page.getByText('Enter a valid email address.', { exact: true })).toBeVisible()
})

test('FRMS-ST-0015 - Eleven digit CCCD is rejected by form validation', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page)
  await page.getByText('Additional profile information (optional)').click()
  await page.getByLabel('National ID / CCCD').fill('00120000001')
  await submitRegistration(page)
  await expect(page.getByText('Enter exactly 12 digits (0–9) for your National ID.', { exact: true })).toBeVisible()
})

test('FRMS-ST-0016 - Password 7 chars is rejected at lower boundary minus one', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { Password: 'Pass12@', 'Confirm Password': 'Pass12@' })
  await submitRegistration(page)
  await expect(page.getByText('Password must contain 8 to 64 characters.', { exact: true })).toBeVisible()
})

test('FRMS-ST-0204 - Required Full Name field cannot be blank', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Full Name': '' })
  await submitRegistration(page)
  await expect(page.getByText('Enter your full name.', { exact: true })).toBeVisible()
})

test('FRMS-ST-0204-B - Required Email Address field cannot be blank', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Email Address': '' })
  await submitRegistration(page)
  await expect(page.getByText('Enter a valid email address.', { exact: true })).toBeVisible()
})

test('FRMS-ST-0204-C - Required Phone Number field cannot be blank', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Phone Number': '' })
  await submitRegistration(page)
  await expect(page.getByText('Enter your phone number.', { exact: true })).toBeVisible()
})

test('FRMS-ST-0204-D - Required Password field cannot be blank', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { Password: '', 'Confirm Password': '' })
  await submitRegistration(page)
  await expect(page.getByText('Password must contain 8 to 64 characters.', { exact: true })).toBeVisible()
})

test('FRMS-ST-0204-E - Required Confirm Password field cannot be blank', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { 'Confirm Password': '' })
  await submitRegistration(page)
  await expect(page.getByText('Re-enter your password.', { exact: true })).toBeVisible()
})

test('FRMS-ST-0205 - Short registration password blocks request', async ({ page }) => {
  await page.goto(REGISTER)
  let sent = 0
  page.on('request', request => {
    if (request.method() === 'POST' && request.url().includes('/api/v1/auth/customer/register')) sent++
  })
  await fillRegistration(page, { Password: 'Pass12@', 'Confirm Password': 'Pass12@' })
  await submitRegistration(page)
  await expect(page.getByText('Password must contain 8 to 64 characters.', { exact: true })).toBeVisible()
  expect(sent).toBe(0)
})

test('FRMS-ST-0206 - Mismatched passwords block registration', async ({ page }) => {
  await page.goto(REGISTER)
  await fillRegistration(page, { Password: 'Pass123@', 'Confirm Password': 'Pass124@' })
  await submitRegistration(page)
  await expect(page.getByText('The passwords do not match.', { exact: true })).toBeVisible()
})

test('FRMS-ST-0005 - Active customer signs in using phone number', async ({ page }) => {
  await customerSignIn(page)
  await expect(page.getByRole('main')).toBeVisible()
})

test('FRMS-ST-0024 - Customer role is admitted through customer sign in', async ({ page }) => {
  await customerSignIn(page)
  await expect(page).not.toHaveURL(/\/forbidden/)
})

test('FRMS-ST-0197 - Active customer credentials reach customer portal', async ({ page }) => {
  await customerSignIn(page)
  await expect(page.getByRole('main')).toBeVisible()
})

test('FRMS-ST-0008 - Active staff uses employee sign in', async ({ page }) => {
  await staffSignIn(page)
  await expect(page.getByRole('main')).toBeVisible()
})

test('FRMS-ST-0201 - Active facility staff credentials reach staff work list', async ({ page }) => {
  await staffSignIn(page)
  await expect(page).not.toHaveURL(/\/forbidden/)
})
