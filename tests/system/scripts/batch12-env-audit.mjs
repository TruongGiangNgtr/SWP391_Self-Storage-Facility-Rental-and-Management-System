import fs from 'node:fs'
import path from 'node:path'
import { ROOT } from './common.mjs'

/** Non-secret process and fixture inventory; does not print tokens/passwords/URLs with credentials. */
const env = process.env
const baseline = {
  FRMS_ENVIRONMENT: 'Testing',
  FRMS_API_URL: 'http://127.0.0.1:5164',
  FRMS_UI_URL: 'http://127.0.0.1:4173',
  VITE_API_PROXY_TARGET: 'http://127.0.0.1:5164',
}
console.log('FRMS Batch 12: environment audit (keys/status only; secret values are never printed)')
let failures = 0
for (const [key, target] of Object.entries(baseline)) {
  const valid = env[key] === target
  console.log(`${valid ? 'OK' : 'CHECK'} ${key}: ${valid ? 'matches profile' : 'not configured to profile baseline'}`)
  if (!valid) failures++
}
for (const key of ['FRMS_TEST_CUSTOMER_1_PASSWORD','FRMS_TEST_STAFF_PASSWORD']) {
  const valid = Boolean(env[key] || env.FRMS_TEST_DEMO_PASSWORD)
  console.log(`${valid ? 'OK' : 'CHECK'} ${key}: ${valid ? 'configured' : 'not configured'}`)
  if (!valid) failures++
}
for (const [role, identity, pw] of [
  ['Facility Manager', 'FRMS_TEST_MANAGER_01_EMAIL', 'FRMS_TEST_MANAGER_PASSWORD'],
  ['Business Operations Manager', 'FRMS_TEST_BOM_EMAIL', 'FRMS_TEST_BOM_PASSWORD'],
  ['System Administrator', 'FRMS_TEST_ADMIN_EMAIL', 'FRMS_TEST_ADMIN_PASSWORD'],
]) {
  const valid = Boolean(env[identity] && (env[pw] || env.FRMS_TEST_DEMO_PASSWORD))
  console.log(`${valid ? 'OK' : 'OPTIONAL MISSING'} ${role}: ${valid ? 'configured' : `set ${identity} and ${pw} (or FRMS_TEST_DEMO_PASSWORD) to run role-specific cases`}`)
}
const dir = path.join(ROOT, 'tests/e2e/playwright/tests/frms')
const names = new Set()
for (const file of fs.readdirSync(dir).filter(f => /^batch12-.*\.spec\.ts$/.test(f))) {
  const source = fs.readFileSync(path.join(dir,file),'utf8')
  for (const match of source.matchAll(/\bfixture\(\s*['"]([A-Z0-9_]+)['"]\s*\)/g)) names.add(match[1])
}
const targetCase = (env.FRMS_TARGET_CASE_ID || '').trim()
const negativeKeys = new Set(['FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE', 'FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL'])
const negativeRelevant = !targetCase || ['FRMS-ST-0199', 'FRMS-ST-0203'].includes(targetCase)
const applicable = [...names].filter(key => negativeRelevant || !negativeKeys.has(key))
const missing = applicable.filter(key => !env[key]).sort()
console.log(`${applicable.length} applicable fixture keys from ${names.size} Batch 12 references; ${missing.length} missing (affected tests will be SKIPPED/Blocked, not PASS).`)
if (!negativeRelevant) console.log('Unregistered login prerequisites do not apply to this targeted testcase.')
if (missing.length) console.log('Missing fixture names only:\n  '+missing.join('\n  '))
const unsafe = env.FRMS_TEST_DB_ISOLATED === 'true' || env.FRMS_ALLOW_STATE_CHANGES === 'YES'
console.log(`${unsafe ? 'WARNING' : 'OK'} State-changing flags: ${unsafe ? 'one or more enabled; disable when connected to shared Supabase' : 'not enabled for this read-only batch'}`)
console.log('Database separation must be verified externally; process flags alone never prove isolation.')
if (failures) { console.error(`Core environment check failed (${failures} mandatory keys).`); process.exitCode = 2 }
else console.log('Core process variables matched profile. Backend health and database state are NOT verified by this script.')
