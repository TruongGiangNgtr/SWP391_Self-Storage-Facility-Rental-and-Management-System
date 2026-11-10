// Read-only diagnostics for catalog loading. Never emit database credentials,
// customer data, response bodies, or bearer tokens.
import fs from 'node:fs'
import path from 'node:path'
import { REPORTS } from './common.mjs'

const api = process.env.FRMS_API_URL ?? 'http://127.0.0.1:5164'
const origin = new URL(api)
if (!['localhost', '127.0.0.1', '[::1]'].includes(origin.hostname) ||
    !['http:', 'https:'].includes(origin.protocol)) {
  console.error('PROBE: API URL must be a loopback Testing backend.')
  process.exit(2)
}
const localParts = new Intl.DateTimeFormat('en-US', {
  timeZone: 'Asia/Ho_Chi_Minh', year: 'numeric', month: '2-digit',
}).formatToParts(new Date())
const year = Number(localParts.find(part => part.type === 'year')?.value)
const month = Number(localParts.find(part => part.type === 'month')?.value)
function businessMonth(offset) {
  const date = new Date(Date.UTC(year, month - 1 + offset, 1))
  return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}`
}
const windows = [
  { id: 'FRMS-ST-0032', start: businessMonth(2), end: businessMonth(4) },
  { id: 'FRMS-ST-0035', start: businessMonth(1), end: businessMonth(1) },
  { id: 'FRMS-ST-0036', start: businessMonth(1), end: businessMonth(12) },
]
const runId = new Date().toISOString().replace(/[-:.]/g, '').replace(/Z$/, 'Z')
const report = { runId, target: `${origin.protocol}//${origin.hostname}:${origin.port}`, rows: [] }
for (const item of windows) {
  const target = new URL('/api/v1/facilities', origin)
  for (const [key, value] of Object.entries({
    startMonth: item.start, endMonth: item.end,
    page: '1', pageSize: '20', sortBy: 'name', sortDirection: 'asc',
  })) target.searchParams.set(key, value)
  const start = Date.now()
  const row = { id: item.id, startMonth: item.start, endMonth: item.end }
  try {
    const response = await fetch(target, { method: 'GET', signal: AbortSignal.timeout(45_000) })
    row.status = response.status
    // Do not record response body. Only assert public contract metadata.
    const json = await response.json().catch(() => null)
    row.count = Array.isArray(json?.data) ? json.data.length : null
    row.page = json?.pagination?.page ?? null
    row.pageSize = json?.pagination?.pageSize ?? null
    row.errorCode = typeof json?.code === 'string' ? json.code : null
  } catch (error) {
    row.error = error?.name === 'TimeoutError' ? 'TIMEOUT' : (error?.code ?? error?.name ?? 'NETWORK_ERROR')
  }
  row.elapsedMs = Date.now() - start
  report.rows.push(row)
  console.log(`${row.id}: HTTP ${row.status ?? 'unavailable'}; ${row.elapsedMs} ms; count=${row.count ?? 'n/a'}; ${row.error ?? row.errorCode ?? 'ok'}`)
}
fs.mkdirSync(REPORTS, { recursive: true })
const file = path.join(REPORTS, `catalog-probe-${runId}.json`)
fs.writeFileSync(file, JSON.stringify(report, null, 2) + '\n')
console.log(`Catalog diagnostic report: ${path.relative(process.cwd(), file)}`)
if (report.rows.some(row => row.status !== 200)) process.exitCode = 2
