import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

export const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../..')
export const MANIFEST = path.join(ROOT, 'tests/system/manifest/cases.json')
export const REPORTS = path.join(ROOT, 'tests/system/reports')
export const JIRA_HEADER = 'Summary,Issue Type,Priority,Created,Components,Description,Resolution,Labels'

export function loadManifest() {
  return JSON.parse(fs.readFileSync(MANIFEST, 'utf8'))
}
export function parseArgs(argv = process.argv.slice(2)) {
  const opts = {}
  for (let index = 0; index < argv.length; index++) {
    if (argv[index] === '--id' || argv[index] === '--area' || argv[index] === '--run-id') {
      opts[argv[index].slice(2)] = argv[++index]
    } else if (argv[index] !== '--') {
      throw new Error(`Unknown argument: ${argv[index]}`)
    }
  }
  return opts
}
export function selectCases(manifest, options) {
  if (options.id && options.area) throw new Error('Choose either --id or --area, not both.')
  const selection = manifest.filter(item => (!options.id || item.id === options.id) && (!options.area || item.area === options.area))
  if (selection.length === 0) throw new Error('No matching FRMS test case.')
  return selection
}
export function safeRunId() {
  return new Date().toISOString().replace(/[-:.]/g, '').replace('T', 'T').replace(/Z$/, 'Z')
}
export function assertSafeRunId(name) {
  if (!/^[A-Za-z0-9_-]{8,80}$/.test(name)) throw new Error('Invalid run id; use only letters, digits, - and _.')
  return name
}
export function sanitize(input) {
  return String(input ?? '')
    .replace(/Bearer\s+[A-Za-z0-9._~+/-]+/gi, 'Bearer [REDACTED]')
    .replace(/(password|signingkey|authorization|secret|token)\s*[:=]\s*[^\s,;]+/gi, '$1=[REDACTED]')
    .replace(/\b09\d{8}\b/g, '[TEST_PHONE]')
    .replace(/\b\d{12}\b/g, '[REDACTED_ID]')
    .slice(0, 1500)
}
export function escapeHtml(s) {
  return String(s).replace(/[&<>"']/g, character => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[character]))
}
export function csvCell(input) {
  let text = String(input ?? '').replace(/\r/g, '')
  // Protect spreadsheet viewers from formula injection. Jira accepts plain text values.
  if (/^[\s]*[=+@\-\t]/.test(text)) text = `'${text}`
  return `"${text.replace(/"/g, '""')}"`
}
