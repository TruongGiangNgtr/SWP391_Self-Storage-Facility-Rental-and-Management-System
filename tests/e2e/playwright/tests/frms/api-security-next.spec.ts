import { expect, test } from '@playwright/test'

test.setTimeout(90_000)
// JWT is sensitive: never retain request/response traces for this API file.
test.use({ trace: 'off' })

const api = (process.env.FRMS_API_URL ?? 'http://127.0.0.1:5164').replace(/\/$/, '')

test('FRMS-ST-0150 - Tampered signed JWT is rejected by protected endpoint', async ({ request }) => {
  const phoneNumber = process.env.FRMS_TEST_CUSTOMER_1_PHONE ?? '0901000001'
  const password = process.env.FRMS_TEST_CUSTOMER_1_PASSWORD
  if (!password) throw new Error('FRMS_FIXTURE_MISSING: FRMS_TEST_CUSTOMER_1_PASSWORD is required for JWT tamper test.')
  const login = await request.post(`${api}/api/v1/auth/customer/login`, { data: { phoneNumber, password } })
  expect(login.status(), 'test account must be ACTIVE and correctly configured').toBe(200)
  const body = await login.json() as { data?: { accessToken?: string } }
  const token = body.data?.accessToken
  if (!token || token.split('.').length !== 3) throw new Error('FRMS_FIXTURE_MISSING: login did not yield a signed JWT.')
  const [header, payload, signature] = token.split('.')
  expect(signature.length).toBeGreaterThan(8)
  // Mutate a significant signature byte (not the last base64url character,
  // whose unused bits can produce an equivalent decoded signature).
  const changed = signature[0] === 'A' ? 'B' : 'A'
  const tampered = `${header}.${payload}.${changed}${signature.slice(1)}`
  const rejected = await request.get(`${api}/api/v1/auth/me`, { headers: { Authorization: `Bearer ${tampered}` } })
  expect(rejected.status()).toBe(401)
  const error = await rejected.json() as { code?: string; traceId?: string }
  expect(error.code).toBe('UNAUTHORIZED')
  expect(error.traceId).toBeTruthy()
})
