import { expect, test } from '@playwright/test'

const base = (process.env.FRMS_API_URL ?? 'http://127.0.0.1:5164').replace(/\/$/, '')

function assertSafeErrorBody(body: unknown, code: string) {
  expect(body).not.toBeNull()
  expect(typeof body).toBe('object')
  const actual = body as Record<string, unknown>
  expect(actual.code).toBe(code)
  expect(typeof actual.traceId).toBe('string')
  expect(String(actual.traceId).length).toBeGreaterThan(0)
  const serialized = JSON.stringify(body)
  expect(serialized).not.toMatch(/stackTrace|passwordHash|connectionString|NpgsqlException|SELECT\s+\*/i)
}

test('FRMS-ST-0149 - Anonymous auth/me returns a safe unauthorized error', async ({ request }) => {
  const response = await request.get(`${base}/api/v1/auth/me`)
  expect(response.status()).toBe(401)
  assertSafeErrorBody(await response.json(), 'UNAUTHORIZED')
})

test('FRMS-ST-0150-B - Malformed Bearer token is rejected safely', async ({ request }) => {
  const response = await request.get(`${base}/api/v1/auth/me`, { headers: { Authorization: 'Bearer abc.def' } })
  expect(response.status()).toBe(401)
  assertSafeErrorBody(await response.json(), 'UNAUTHORIZED')
})

test('FRMS-ST-0155 - Isolated healthy database is reported Healthy', async ({ request }) => {
  const response = await request.get(`${base}/health`)
  expect(response.status()).toBe(200)
  expect((await response.text()).trim()).toBe('Healthy')
})
