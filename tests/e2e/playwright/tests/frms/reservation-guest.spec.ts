import { expect, test, type Page } from '@playwright/test'

// Guest-only tests: no reservation API request, no payment, no DB mutation.
test.setTimeout(150_000)

function month(offset: number): string {
  const parts = new Intl.DateTimeFormat('en-US', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit' }).formatToParts(new Date())
  const year = Number(parts.find(p => p.type === 'year')?.value)
  const number = Number(parts.find(p => p.type === 'month')?.value)
  const d = new Date(Date.UTC(year, number - 1 + offset, 1))
  return `${d.getUTCFullYear()}-${String(d.getUTCMonth() + 1).padStart(2, '0')}`
}
function fixture(name: string): string {
  const value = process.env[name]
  if (!value) throw new Error(`FRMS_FIXTURE_MISSING: ${name} is required for available guest reservation choice.`)
  return value
}
async function openGuestGate(page: Page) {
  const startMonth = month(1), endMonth = month(2)
  const params = new URLSearchParams({
    facilityId: fixture('FRMS_TEST_ACTIVE_FACILITY_ID'),
    unitTypeId: fixture('FRMS_TEST_AVAILABLE_UNIT_TYPE_ID'),
    startMonth, endMonth,
  })
  let reservationWrites = 0
  page.on('request', request => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method()) && new URL(request.url()).pathname.startsWith('/api/v1/')) reservationWrites++
  })
  await page.goto(`/facilities?${params}`)
  const facility = page.locator('.public-result-facility').filter({
    has: page.getByRole('heading', { name: fixture('FRMS_TEST_ACTIVE_FACILITY_NAME'), exact: true }),
  })
  await expect(facility).toBeVisible({ timeout: 75_000 })
  const expand = facility.getByRole('button', { name: 'View Storage Options', exact: true })
  if (await expand.count()) await expand.click()
  const card = facility.locator('.public-unit-card').filter({
    has: page.getByRole('heading', { name: fixture('FRMS_TEST_AVAILABLE_UNIT_NAME'), exact: true }),
  })
  await expect(card).toBeVisible({ timeout: 45_000 })
  const action = card.getByRole('button', { name: 'Create Reservation', exact: true })
  await expect(action).toBeEnabled({ timeout: 30_000 })
  await action.click()
  const dialog = page.getByRole('dialog', { name: 'Sign in to continue' })
  await expect(dialog).toBeVisible()
  expect(reservationWrites, 'guest gate cannot reserve capacity or initiate payment').toBe(0)
  return { dialog, startMonth, endMonth, writes: () => reservationWrites }
}

test('FRMS-ST-0056 - Anonymous reservation choice opens sign-in dialog', async ({ page }) => {
  const { dialog, startMonth, endMonth, writes } = await openGuestGate(page)
  await expect(dialog.getByText(fixture('FRMS_TEST_AVAILABLE_UNIT_NAME'), { exact: true })).toBeVisible()
  await expect(dialog.getByText(fixture('FRMS_TEST_ACTIVE_FACILITY_NAME'), { exact: true })).toBeVisible()
  await expect(dialog.getByText('Rental Period', { exact: true })).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Sign In to Continue', exact: true })).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Create Customer Account', exact: true })).toBeVisible()
  await expect(dialog).toContainText(startMonth.slice(0, 4))
  expect(endMonth).not.toBe(startMonth)
  expect(writes()).toBe(0)
})

test('FRMS-ST-0211 - Guest selection does not hold capacity or issue deposit', async ({ page }) => {
  const { dialog, writes } = await openGuestGate(page)
  await expect(dialog.getByText('This selection does not hold capacity or create a deposit.', { exact: false })).toBeVisible()
  await expect(dialog.getByText('Current Monthly Rent', { exact: true })).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Back to Storage Options', exact: true })).toBeVisible()
  expect(writes()).toBe(0)
})
