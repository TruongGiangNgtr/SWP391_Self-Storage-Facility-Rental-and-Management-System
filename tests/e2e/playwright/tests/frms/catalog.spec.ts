import { expect, test, type Page, type Request } from '@playwright/test'

// The remote catalogue can be slow. Keep assertions strict while capturing
// just status/timing diagnostics; never include tokens or response bodies.
test.setTimeout(150_000)

/** Compute the FRMS business month in GMT+7 without relying on the CI runner's OS time zone. */
function businessMonth(add = 0): string {
  const parts = new Intl.DateTimeFormat('en-US', { timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit' }).formatToParts(new Date())
  const year = Number(parts.find(part => part.type === 'year')?.value)
  const month = Number(parts.find(part => part.type === 'month')?.value)
  const date = new Date(Date.UTC(year, month - 1 + add, 1))
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}`
}
function searchUrl(startMonth: string, endMonth: string, facilityId = ''): string {
  const query = new URLSearchParams({ startMonth, endMonth })
  if (facilityId) query.set('facilityId', facilityId)
  return `/facilities?${query}`
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
async function assertFixtureCard(page: Page, probe: CatalogProbe) {
  const fixture = catalogFixture()
  // Search results are asynchronous and may take >5 seconds with a remote DB.
  // Do not assert a fixture before the actual API-backed list is ready.
  await assertResultsOrEmpty(page, probe)
  const results = page.getByRole('region', { name: 'Storage search results', exact: true })
  const facility = results.locator('.public-result-facility').filter({ has: page.getByRole('heading', { name: fixture.facility, exact: true }) })
  if (await facility.count() === 0) {
    const found = await results.locator('.public-result-facility h2').allTextContents()
    throw new Error(`FRMS_FIXTURE_MISSING: ${fixture.facility} was not returned for the selected months. Visible facilities: ${found.join(', ') || '(none)'}. ${probe.summary()}`)
  }
  await expect(facility).toBeVisible()
  // Only the first facility is expanded automatically; explicitly open the
  // selected one without changing the business expectation.
  const expand = facility.getByRole('button', { name: 'View Storage Options', exact: true })
  if (await expand.count()) await expand.click()
  const card = facility.locator('.public-unit-card').filter({ has: page.getByRole('heading', { name: fixture.unit, exact: true }) })
  await expect(card).toBeVisible({ timeout: 45_000 })
  await expect(card.locator('.unit-size')).toHaveText(fixture.size)
  await expect(card.getByText(fixture.mode === 'PRIVATE' ? 'Private Storage' : 'Public Storage')).toBeVisible()
  await expect(card.locator('.unit-price strong')).toHaveText(fixture.rent)
  await expect(card.locator('.unit-capacity')).toContainText('Available capacity for')
  if (fixture.description) await expect(card.getByText(fixture.description, { exact: true })).toBeVisible()
  return card
}
type CatalogProbe = {
  pending: Set<Request>
  events: string[]
  summary(): string
}
function observeCatalog(page: Page): CatalogProbe {
  const pending = new Set<Request>()
  const events: string[] = []
  const started = new Map<Request, number>()
  const relevant = (request: Request): boolean => {
    try {
      return request.method() === 'GET' && new URL(request.url()).pathname === '/api/v1/facilities'
    } catch { return false }
  }
  const record = (message: string) => {
    events.push(message)
    if (events.length > 12) events.shift()
  }
  page.on('request', request => {
    if (!relevant(request)) return
    pending.add(request)
    started.set(request, Date.now())
    record('GET /api/v1/facilities requested')
  })
  page.on('response', response => {
    const request = response.request()
    if (!relevant(request)) return
    const elapsed = Date.now() - (started.get(request) ?? Date.now())
    record(`GET /api/v1/facilities -> HTTP ${response.status()} (${elapsed} ms)`)
  })
  page.on('requestfinished', request => {
    if (relevant(request)) pending.delete(request)
  })
  page.on('requestfailed', request => {
    if (!relevant(request)) return
    pending.delete(request)
    record(`GET /api/v1/facilities failed: ${request.failure()?.errorText ?? 'unknown network failure'}`)
  })
  return {
    pending, events,
    summary: () => `pending=${pending.size}; ${events.join('; ') || 'no facility API requests observed'}`,
  }
}
async function assertResultsOrEmpty(page: Page, probe: CatalogProbe) {
  const results = page.getByRole('region', { name: 'Storage search results', exact: true })
  await expect(results.getByRole('heading', { name: 'Storage Search Results' })).toBeVisible()
  const cardsOrEmpty = results.locator('.public-result-facility, .empty-state')
  const retry = results.getByRole('button', { name: 'Retry Search', exact: true })
  try {
    await expect.poll(async () => {
      if (await cardsOrEmpty.count()) return 'loaded'
      if (await retry.count()) return 'api-error'
      return 'pending'
    }, { message: 'Waiting for public catalogue results or API error', timeout: 45_000 }).not.toBe('pending')
  } catch {
    const loading = (await results.getByRole('status').allTextContents()).join(' ').trim() || 'no loading indicator'
    const diagnostic = `loading=${loading}; ${probe.summary()}`
    if (probe.pending.size > 0) {
      throw new Error(`FRMS_INFRA_UNAVAILABLE: public catalogue request did not finish within 45 seconds; ${diagnostic}`)
    }
    throw new Error(`FRMS_CATALOG_RESULTS_TIMEOUT: catalogue result/empty/error never appeared; ${diagnostic}`)
  }
  if (await retry.count()) {
    const messages = await results.getByRole('alert').allTextContents()
    throw new Error(`FRMS_CATALOG_QUERY_FAILED: ${messages.join(' ').trim() || 'The frontend displayed Retry Search.'}; ${probe.summary()}`)
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
  const probe = observeCatalog(page)
  const start = businessMonth(1)
  // Scope the layout check to the exact ACTIVE facility verified by discovery.
  // The public search backend may exclude other facilities by month/capacity.
  await page.goto(searchUrl(start, start, requiredFixture('FRMS_TEST_ACTIVE_FACILITY_ID')))
  const card = await assertFixtureCard(page, probe)
  await expect(card.getByText('Current Monthly Rent', {exact:true})).toBeVisible()
  await expect(card.getByRole('button', {name:'Create Reservation'})).toBeVisible()
})

test('FRMS-ST-0028 - Public visitor can search available facility and unit', async ({ page }) => {
  const probe = observeCatalog(page)
  const start = businessMonth(1)
  const end = businessMonth(2)
  await page.goto('/facilities')
  await page.getByLabel('Check-in Month', { exact: true }).fill(start)
  await page.getByLabel('Check-out Month', { exact: true }).fill(end)
  await page.getByRole('button', {name:'Find Storage',exact:true}).click()
  await expect(page).toHaveURL(new RegExp(`startMonth=${start}.*endMonth=${end}`))
  await assertFixtureCard(page, probe)
})

test('FRMS-ST-0032 - Valid future month range reaches search results', async ({ page }) => {
  const probe = observeCatalog(page)
  await page.goto('/facilities')
  const start = businessMonth(2)
  const end = businessMonth(4)
  await page.getByLabel('Check-in Month', { exact: true }).fill(start)
  await page.getByLabel('Check-out Month', { exact: true }).fill(end)
  await page.getByRole('button', { name: 'Find Storage', exact: true }).click()
  await expect(page).toHaveURL(new RegExp(`startMonth=${start}.*endMonth=${end}`))
  await assertResultsOrEmpty(page, probe)
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
  const probe = observeCatalog(page)
  const next = businessMonth(1)
  await page.goto(searchUrl(next, next))
  await assertResultsOrEmpty(page, probe)
})

test('FRMS-ST-0036 - Twelve calendar month rental period is accepted', async ({ page }) => {
  const probe = observeCatalog(page)
  const start = businessMonth(1)
  const end = businessMonth(12)
  await page.goto(searchUrl(start, end))
  await assertResultsOrEmpty(page, probe)
})

// Batch 09: read-only public catalog search and month-boundary coverage.
test('FRMS-ST-0029 - Public facility details show identity and available storage options', async ({ page }) => {
  const facilityId = requiredFixture('FRMS_TEST_ACTIVE_FACILITY_ID')
  const fixture = catalogFixture()
  const start = businessMonth(1)
  const query = new URLSearchParams({ startMonth: start, endMonth: start })
  await page.goto(`/facilities/${facilityId}?${query}`)
  await expect(page.getByRole('heading', { level: 1, name: fixture.facility, exact: true })).toBeVisible({ timeout: 45_000 })
  await expect(page.getByRole('heading', { name: 'About This Facility', exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Available Storage Options', exact: true })).toBeVisible()
  const card = page.locator('.public-unit-card').filter({ has: page.getByRole('heading', { name: fixture.unit, exact: true }) })
  await expect(card).toBeVisible({ timeout: 45_000 })
  await expect(card.locator('.unit-size')).toHaveText(fixture.size)
})

test('FRMS-ST-0039 - Positive whole-period capacity enables reservation choice', async ({ page }) => {
  const probe = observeCatalog(page)
  const start = businessMonth(1)
  const end = businessMonth(2)
  await page.goto(searchUrl(start, end, requiredFixture('FRMS_TEST_ACTIVE_FACILITY_ID')))
  const card = await assertFixtureCard(page, probe)
  const capacity = card.locator('.unit-capacity strong')
  await expect(capacity).toBeVisible()
  const count = Number((await capacity.textContent())?.trim())
  expect(count, 'the facility was verified with a positive capacity across the entire range').toBeGreaterThan(0)
  await expect(card.getByRole('button', { name: 'Create Reservation', exact: true })).toBeEnabled({ timeout: 30_000 })
})

test('FRMS-ST-0208 - Current business month is rejected for a new search', async ({ page }) => {
  await page.goto(searchUrl(businessMonth(0), businessMonth(1)))
  await expect(page.getByText('The check-in month must be after the current month.', { exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Storage Search Results', exact: true })).toHaveCount(0)
})

test('FRMS-ST-0209 - Check-out month before check-in is rejected', async ({ page }) => {
  await page.goto(searchUrl(businessMonth(3), businessMonth(2)))
  await expect(page.getByText('The check-out month cannot be before the check-in month.', { exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Storage Search Results', exact: true })).toHaveCount(0)
})

test('FRMS-ST-0210 - Facility mode size and unit-type filters restrict search', async ({ page }) => {
  const fixture = catalogFixture()
  const probe = observeCatalog(page)
  const month = businessMonth(1)
  const query = new URLSearchParams({
    facilityId: requiredFixture('FRMS_TEST_ACTIVE_FACILITY_ID'),
    mode: fixture.mode,
    size: fixture.size,
    unitTypeId: requiredFixture('FRMS_TEST_AVAILABLE_UNIT_TYPE_ID'),
    startMonth: month,
    endMonth: month,
  })
  await page.goto(`/facilities?${query}`)
  const card = await assertFixtureCard(page, probe)
  const results = page.getByRole('region', { name: 'Storage search results', exact: true })
  await expect(results.locator('.public-result-facility')).toHaveCount(1)
  await expect(results.locator('.public-unit-card')).toHaveCount(1)
  await expect(card.getByText(fixture.mode === 'PUBLIC' ? 'Public Storage' : 'Private Storage')).toBeVisible()
})
