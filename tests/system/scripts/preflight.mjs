const api = process.env.FRMS_API_URL ?? 'http://127.0.0.1:5164'
const ui = process.env.FRMS_UI_URL ?? 'http://127.0.0.1:4173'
const environment = process.env.FRMS_ENVIRONMENT ?? 'Testing'
if (environment !== 'Testing' || process.env.NODE_ENV === 'production') {
  console.error('PREFLIGHT: refusing to run against non-Testing environment.')
  process.exit(2)
}
for (const url of [api, ui]) {
  let parsed
  try { parsed = new URL(url) } catch { console.error(`PREFLIGHT: invalid URL ${url}`); process.exit(2) }
  if (!['127.0.0.1', 'localhost', '[::1]'].includes(parsed.hostname) || !['http:', 'https:'].includes(parsed.protocol)) {
    console.error(`PREFLIGHT: target must be loopback test service: ${parsed.origin}`)
    process.exit(2)
  }
}
if (process.env.FRMS_TEST_DB_ID && !/^frms[_-](automation|test)([_-][a-z0-9_-]+)?$/i.test(process.env.FRMS_TEST_DB_ID)) {
  console.error('PREFLIGHT: FRMS_TEST_DB_ID is not a clearly isolated test database.')
  process.exit(2)
}
try {
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), 6000)
  let response
  try { response = await fetch(`${api.replace(/\/$/, '')}/health`, { signal: controller.signal }) }
  finally { clearTimeout(timer) }
  if (!response.ok || (await response.text()).trim() !== 'Healthy') {
    console.error('PREFLIGHT: API /health is not Healthy. Confirm backend and isolated PostgreSQL test DB are running.')
    process.exit(2)
  }
  console.log(`PREFLIGHT OK: Testing API ${new URL(api).origin} is healthy; web UI will start via Playwright.`)
} catch (error) {
  console.error(`PREFLIGHT: API unavailable (${error.cause?.code ?? error.name}). Run the test backend/database first.`)
  process.exit(2)
}
