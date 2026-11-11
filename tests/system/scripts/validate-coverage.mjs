import fs from 'node:fs'
import path from 'node:path'
import { ROOT, loadManifest } from './common.mjs'

const EXPECTED = {
  'Authentication & Security': 51,
  'Storage Search & AI Size Guide': 19,
  'Reservation': 22,
  'Handover': 29,
  'Facility Operations': 10,
  'Access Visits': 25,
  'Billing & Payments': 23,
  'Renewal': 16,
  'Return & Settlement': 22,
  'Support Tickets': 25,
  'Administration': 19,
  'Pricing & Reference Data': 14,
}
// Preserve the 275-case SRS baseline while accepting separately traced defect cases.
// Supplemental cases may extend an existing area or the Return Inspection area.
// They never silently change the fixed baseline counts.
const BASELINE_TOTAL = Object.values(EXPECTED).reduce((sum, n) => sum + n, 0)
const SUPPLEMENTAL_AREAS = new Set(['Return Inspection'])
const manifest = loadManifest()
const errors = []
const supplemental = manifest.filter(item => typeof item.sourceKey === 'string' && item.sourceKey.startsWith('Supplemental:'))
if (manifest.length !== BASELINE_TOTAL + supplemental.length)
  errors.push(`Expected ${BASELINE_TOTAL} baseline IDs plus ${supplemental.length} supplemental IDs, received ${manifest.length}.`)
const ids = new Set()
const counts = Object.fromEntries(Object.keys(EXPECTED).map(area => [area, 0]))
const implementationIndex = new Map()
for (const item of manifest) {
  if (!/^FRMS-ST-\d{4}(?:-[A-Z])?$/.test(item.id)) errors.push(`Malformed ID ${item.id}`)
  if (ids.has(item.id)) errors.push(`Duplicate ID ${item.id}`)
  ids.add(item.id)
  const isSupplemental = typeof item.sourceKey === 'string' && item.sourceKey.startsWith('Supplemental:')
  if (isSupplemental) {
    const match = /^FRMS-ST-(\d{4})(?:-[A-Z])?$/.exec(item.id)
    if (!match || Number(match[1]) <= BASELINE_TOTAL)
      errors.push(`Supplemental testcase must use an ID after the ${BASELINE_TOTAL}-case baseline: ${item.id}`)
    if (!(item.area in EXPECTED) && !SUPPLEMENTAL_AREAS.has(item.area))
      errors.push(`Unknown supplemental area for ${item.id}: ${item.area}`)
  } else if (!(item.area in EXPECTED)) errors.push(`Unknown primary area for ${item.id}: ${item.area}`)
  else counts[item.area]++
  if (!['UI', 'API', 'REVIEW', 'UI_API_DB', 'INTEGRATION'].includes(item.channel)) errors.push(`Invalid channel for ${item.id}`)
  if (!['IMPLEMENTED', 'NOT_IMPLEMENTED', 'REVIEW'].includes(item.automation)) errors.push(`Invalid automation state for ${item.id}`)
  if (item.automation === 'REVIEW' && !item.reviewReason) errors.push(`No review reason for ${item.id}`)
  if (item.automation === 'IMPLEMENTED') {
    if (!item.implementation) { errors.push(`Missing implementation for ${item.id}`); continue }
    const filepath = path.join(ROOT, item.implementation)
    if (!fs.existsSync(filepath)) { errors.push(`Implementation missing for ${item.id}: ${item.implementation}`); continue }
    const source = fs.readFileSync(filepath, 'utf8')
    const pattern = new RegExp(`\\btest\\s*\\(\\s*['\"\\x60]${item.id}(?![-A-Z0-9])`)
    if (!pattern.test(source)) errors.push(`No executable test() declaration for ${item.id} in ${item.implementation}`)
    const implementations = implementationIndex.get(item.implementation) ?? []
    implementations.push(item.id)
    implementationIndex.set(item.implementation, implementations)
  } else if (item.implementation) errors.push(`Non-implemented case has implementation: ${item.id}`)
}
for (const [area, expected] of Object.entries(EXPECTED)) if (counts[area] !== expected) errors.push(`${area}: ${counts[area]} != ${expected}`)
const specRoot = path.join(ROOT, 'tests/e2e/playwright/tests/frms')
if (fs.existsSync(specRoot)) {
  for (const file of fs.readdirSync(specRoot).filter(file => file.endsWith('.spec.ts'))) {
    const relative = path.relative(ROOT, path.join(specRoot, file)).replaceAll('\\', '/')
    const source = fs.readFileSync(path.join(specRoot, file), 'utf8')
    const found = [...source.matchAll(/\btest\s*\(\s*['"`](FRMS-ST-\d{4}(?:-[A-Z])?)(?![-A-Z0-9])/g)].map(match => match[1])
    for (const id of found) {
      const item = manifest.find(entry => entry.id === id)
      if (!item || item.automation !== 'IMPLEMENTED' || item.implementation !== relative) errors.push(`Orphan/misrouted test ${id} in ${relative}`)
    }
    if (new Set(found).size !== found.length) errors.push(`Duplicate test() IDs in ${relative}`)
  }
}
console.log(`Registry: ${manifest.length} cases / ${ids.size} unique / ${manifest.filter(x => x.automation === 'IMPLEMENTED').length} implemented / ${manifest.filter(x => x.automation === 'REVIEW').length} review / ${manifest.filter(x => x.automation === 'NOT_IMPLEMENTED').length} outstanding`)
console.log(`Registry partitions: ${BASELINE_TOTAL} baseline / ${supplemental.length} supplemental`)
if (errors.length) { errors.forEach(e => console.error(`COVERAGE ERROR: ${e}`)); process.exitCode = 3 }
else console.log('Registry structure and executable mappings valid. This is NOT a claim of runtime passes or SRS approval.')
