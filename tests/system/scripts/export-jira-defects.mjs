import fs from 'node:fs'
import path from 'node:path'
import { REPORTS, JIRA_HEADER, csvCell, parseArgs } from './common.mjs'

const args = parseArgs()
const runId = args['run-id']
if (!runId || !/^[A-Za-z0-9_-]{8,80}$/.test(runId)) throw new Error('Pass --run-id <run id>.')
const runDir = path.join(REPORTS, runId)
const data = JSON.parse(fs.readFileSync(path.join(runDir, 'results.json'), 'utf8'))
// This exporter deliberately refuses to infer confirmed product defects from Playwright failures.
// A separate confirmation stage must attach explicit metadata and trusted evidence to results.json.
const eligible = data.cases.filter(row => row.status === 'ConfirmedProductDefect' &&
  row.confirmation?.oracleApproved === true && row.confirmation?.fixturesValid === true &&
  row.confirmation?.reproduced === true && row.confirmation?.failureClass === 'PRODUCT' &&
  row.evidence?.length > 0 && row.confirmation?.rootCauseFingerprint)
const distinct = new Map()
for (const row of eligible) {
  const fp = row.confirmation.rootCauseFingerprint
  if (!distinct.has(fp)) distinct.set(fp, row)
}
const lines = [JIRA_HEADER]
for (const row of distinct.values()) {
  const summary = `[${row.id}] ${String(row.confirmation.summary ?? row.title).slice(0,110)}`
  const description = `TC: ${row.id}\nSteps: ${String(row.confirmation.steps ?? 'See test evidence.').slice(0,160)}\nExpected: ${String(row.confirmation.expected ?? row.expected).slice(0,170)}\nActual: ${String(row.confirmation.actual ?? row.actual).slice(0,170)}\nEvidence: ${row.evidence[0]} | Commit: ${data.sha ?? 'unknown'}`
  lines.push([summary,'Bug',row.confirmation.priority ?? 'Medium','','',description,'','frms-auto-defect'].map(csvCell).join(','))
}
fs.writeFileSync(path.join(runDir, 'jira_defects.csv'), lines.join('\n')+'\n', 'utf8')
console.log(`Jira CSV: ${distinct.size} independently confirmed defect(s), ${data.cases.length} registered cases. Unconfirmed failures are excluded.`)
