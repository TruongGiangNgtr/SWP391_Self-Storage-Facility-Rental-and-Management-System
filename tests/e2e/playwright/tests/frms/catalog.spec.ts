import { expect, test, type Page } from '@playwright/test'

/** Compute the FRMS business month in GMT+7 without relying on the CI runner's OS time zone. */
function businessMonth(add = 0): string {
  const parts = new Intl.DateTimeFormat('en-US', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit' }).formatToParts(new Date())
  const year = Number(parts.find(part => part.type === 'year')?.value)
  const month = Number(parts.find(part => part.type === 'month')?.value)
  const date = new Date(Date.UTC(year, month - 1 + add, 1))
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}`
}
function searchUrl(startMonth: string, endMonth: string): string {
  return `/facilities?${new URLSearchParams({ startMonth, endMonth })}`
}
function requiredFixture(key: string): string {
  const value = process.env[key]
  if (!value) throw new Error(`FRMS_FIXTURE_MISSING: configure ${key} and seed available public catalog data in the isolated test DB.`)
  return value
}
function catalogFixture() {
  const rent = Number(requiredFixture('FRMS_TEST_AVAILABLE_UNIT_RENT_VND'))
  if (!Number.isFinite(rent) || rent < 0) throw new Error('FRMS_FIXTURE_MISSING: expected rent must be a nonnegative number.')
  const mode = requiredFixture('FRMS_TEST_AVAILABLE_UNIT_MODE')
  if (!['PRIVATE', 'PUBLIC'].includes(mode)) throw new Error('FRMS_FIXTURE_MISSING: unit mode must be PRIVATE or PUBLIC.')
  return {
    facility: requiredFixture('FRMS_TEST_ACTIVE_FACILITY_NAME'),
    unit: requiredFixture('FRMS_TEST_AVAILABLE_UNIT_NAME'),
    size: requiredFixture('FRMS_TEST_AVAILABLE_UNIT_SIZE'),
    mode,
    rent: new Intl.NumberFormat('en-GB', {style:'currency',currency:'VND',currencyDisplay:'code',maximumFractionDigits:0}).format(rent),
    description: process.env.FRMS_TEST_AVAILABLE_UNIT_DESCRIPTION,
  }
}
async function assertFixtureCard(page: Page) {
  const fixture = catalogFixture()
  const facility = page.locator('.public-result-facility').filter({ has: page.getByRole('heading', {name:fixture.facility,exact:true}) })
  await expect(facility).toBeVisible()
  const card = facility.locator('.public-unit-card').filter({ has: page.getByRole('heading', {name:fixture.unit,exact:true}) })
  await expect(card).toBeVisible()
  await expect(card.locator('.unit-size')).toHaveText(fixture.size)
  await expect(card.getByText(fixture.mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage')).toBeVisible()
  await expect(card.locator('.unit-price strong')).toHaveText(fixture.rent)
  await expect(card.locator('.unit-capacity')).toContainText('Available capacity for')
  if (fixture.description) await expect(card.getByText(fixture.description, {exact:true})).toBeVisible()
  return card
}
async function assertResultsOrEmpty(page: Page) {
  const results = page.getByRole('region', { name: 'Storage search results', exact: true })
  await expect(results.getByRole('heading', { name: 'Storage Search Results' })).toBeVisible()
  const cardsOrEmpty = results.locator('.public-result-facility, .empty-state')
  const retry = results.getByRole('button', { name: 'Retry Search', exact: true })
  // The public catalogue loads asynchronously. Distinguish a server/API error
  // from an empty but valid result instead of timing out on a missing CSS node.
  await expect.poll(async () => {
    if (await cardsOrEmpty.count()) return 'loaded'
    if (await retry.count()) return 'api-error'
    return 'pending'
  }, { message: 'Waiting for public catalogue results or API error', timeout: 15000 }).not.toBe('pending')
  if (await retry.count()) {
    const messages = await results.getByRole('alert').allTextContents()
    throw new Error(`FRMS_CATALOG_QUERY_FAILED: ${messages.join(' ').trim() || 'The frontend displayed Retry Search.'}`)
  }
  await expect(cardsOrEmpty.first()).toBeVisible()
  await expect(results.getByRole('alert')).toHaveCount(0)
}

test('FRMS-ST-0026 - Public storage search shows documented filter controls', async ({ page }) => {
  await page.goto('/facilities')
  const form = page.getByRole('region', { name: 'Storage search', exact: true })
  await expect(form).toBeVisible()
  for (const label of ['Location / Facility', 'Storage Type', 'Storage Size', 'Check-in Month', 'Check-out Month']) {
    await expect(form.getByLabel(label, { exact: true })).toBeVisible()
  }
  await expect(form.getByRole('button', { name: 'Find Storage', exact: true })).toBeVisible()
  await expect(page.getByRole('region', { name: 'Storage search results' })).toBeVisible()
  // Result cards need a seeded catalogue and are asserted separately by
  // FRMS-ST-0027; the form-layout case must not require a business fixture.
})

test('FRMS-ST-0027 - Facility card shows unit size, mode, rent and capacity', async ({ page }) => {
  const start = businessMonth(1)
  await page.goto(searchUrl(start, start))
  const card = await assertFixtureCard(page)
  await expect(card.getByText('Current Monthly Rent', {exact:true})).toBeVisible()
  await expect(card.getByRole('button', {name:'Create Reservation'})).toBeVisible()
})

test('FRMS-ST-0028 - Public visitor can search available facility and unit', async ({ page }) => {
  const start = businessMonth(1)
  const end = businessMonth(2)
  await page.goto('/facilities')
  await page.getByLabel('Check-in Month', { exact: true }).fill(start)
  await page.getByLabel('Check-out Month', { exact: true }).fill(end)
  await page.getByRole('button', {name:'Find Storage',exact:true}).click()
  await expect(page).toHaveURL(new RegExp(`startMonth=${start}.*endMonth=${end}`))
  await assertFixtureCard(page)
})

test('FRMS-ST-0032 - Valid future month range reaches search results', async ({ page }) => {
  await page.goto('/facilities')
  const start = businessMonth(2)
  const end = businessMonth(4)
  await page.getByLabel('Check-in Month', { exact: true }).fill(start)
  await page.getByLabel('Check-out Month', { exact: true }).fill(end)
  await page.getByRole('button', { name: 'Find Storage', exact: true }).click()
  await expect(page).toHaveURL(new RegExp(`startMonth=${start}.*endMonth=${end}`))
  await assertResultsOrEmpty(page)
})

test('FRMS-ST-0033 - Reversed month range displays frontend validation', async ({ page }) => {
  const start = businessMonth(3)
  const end = businessMonth(2)
  await page.goto(searchUrl(start, end))
  await expect(page.getByText('The check-out month cannot be before the check-in month.', { exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Storage Search Results' })).toHaveCount(0)
})

test('FRMS-ST-0034 - Past check-in month displays frontend validation', async ({ page }) => {
  const start = businessMonth(-1)
  const end = businessMonth(2)
  await page.goto(searchUrl(start, end))
  await expect(page.getByText('The check-in month must be after the current month.', { exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Storage Search Results' })).toHaveCount(0)
})

test('FRMS-ST-0035 - Same month is a valid one month search', async ({ page }) => {
  const next = businessMonth(1)
  await page.goto(searchUrl(next, next))
  await assertResultsOrEmpty(page)
})

test('FRMS-ST-0036 - Twelve calendar month rental period is accepted', async ({ page }) => {
  const start = businessMonth(1)
  const end = businessMonth(12)
  await page.goto(searchUrl(start, end))
  await assertResultsOrEmpty(page)
})
