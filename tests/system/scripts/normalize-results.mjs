import fs from 'node:fs'
import path from 'node:path'
import { loadManifest, REPORTS, escapeHtml, sanitize, JIRA_HEADER } from './common.mjs'

export function normalize(runDir, selectedIds, options = {}) {
  const manifest = loadManifest()
  const selected = new Set(selectedIds)
  const file = path.join(runDir, 'playwright-results.json')
  const parsed = fs.existsSync(file) ? JSON.parse(fs.readFileSync(file, 'utf8')) : null
  const testResults = new Map()
  function walk(suites) {
    for (const suite of suites ?? []) {
      for (const spec of suite.specs ?? []) {
        const id = (spec.title ?? '').match(/FRMS-ST-\d{4}(?:-[A-Z])?(?![-A-Z0-9])/)?.[0]
        if (id) {
          const entries = testResults.get(id) ?? []
          for (const test of spec.tests ?? []) entries.push(...(test.results ?? []))
          testResults.set(id, entries)
        }
      }
      walk(suite.suites)
    }
  }
  walk(parsed?.suites)
  const rows = manifest.map(item => {
    let status = item.automation === 'REVIEW' ? 'Review' : 'Not Run'
    let reason = item.automation === 'REVIEW' ? item.reviewReason : null
    let attempts = 0, actual = null, evidence = []
    if (selected.has(item.id) && item.automation === 'IMPLEMENTED') {
      const records = testResults.get(item.id) ?? []
      attempts = records.length
      if (records.length > 0) {
        const last = records.at(-1)
        const firstFailure = records.find(record => record.status !== 'passed' && record.status !== 'skipped')
        evidence = [...new Set(records.flatMap(record => (record.attachments ?? []).map(attachment => attachment.path).filter(Boolean)).map(p => path.relative(runDir, path.resolve(p))))]
        actual = firstFailure?.error?.message ? sanitize(firstFailure.error.message) : null
        if (last.status === 'passed') status = firstFailure ? 'Flaky' : 'Pass'
        else if (records.some(record => /FRMS_FIXTURE_MISSING|FRMS_INFRA_UNAVAILABLE/.test(record.error?.message ?? ''))) { status = 'Blocked'; reason = 'TEST_FIXTURE_OR_INFRA' }
        else if (last.status === 'skipped') { status = 'Blocked'; reason = 'PLAYWRIGHT_SKIPPED' }
        else { status = 'UnconfirmedFailure'; reason = 'Needs oracle/fixture verification and independent reproduction' }
      } else if (options.runnerError) { status = 'Blocked'; reason = 'RUNNER_OR_ENVIRONMENT_FAILURE' }
    }
    return { id:item.id, area:item.area, title:item.name, channel:item.channel, status, reason,
      oracleApproval:item.oracleApproval, automation:item.automation, attempts, actual,
      expected:item.expectedUI, evidence, confirmation:null }
  })
  const totals = Object.fromEntries([...new Set(rows.map(r => r.status))].map(status => [status, rows.filter(row => row.status === status).length]))
  const result = { runId:path.basename(runDir), timestamp:new Date().toISOString(), sha:options.sha ?? null,
    environment:'Testing', selectedIds:[...selected], runnerExitCode:options.runnerExitCode ?? null,
    totals, cases:rows, warning:'Automated assertions are not independently approved SRS oracles. Failures are candidates only, not Jira defects.' }
  fs.mkdirSync(runDir, {recursive:true})
  fs.writeFileSync(path.join(runDir, 'results.json'), JSON.stringify(result, null, 2)+'\n')
  let md = `# FRMS automation ${result.runId}\n\nCommit: \`${result.sha ?? 'unavailable'}\` | Generated: ${result.timestamp}\n\n`
  md += `**Scope:** ${selected.size} selected / 275 registered. **Executed:** ${rows.filter(row => row.attempts > 0).length}.\n\n`
  for (const [status, count] of Object.entries(totals)) md += `- ${status}: ${count}\n`
  md += '\nFailures must be reproduced with approved oracles and valid fixtures before Jira export. Cases not implemented remain Not Run.\n\n'
  for (const row of rows.filter(row => selected.has(row.id))) md += `- ${row.id}: ${row.status}${row.reason ? ` (${row.reason})` : ''}\n`
  fs.writeFileSync(path.join(runDir, 'summary.md'), md)
  const body = rows.map(row => `<tr><td>${escapeHtml(row.id)}</td><td>${escapeHtml(row.area)}</td><td>${escapeHtml(row.status)}</td><td>${escapeHtml(row.oracleApproval)}</td><td>${row.attempts}</td></tr>`).join('\n')
  fs.writeFileSync(path.join(runDir, 'report.html'), `<!doctype html><html lang="en"><head><meta charset="utf-8"><title>FRMS ${escapeHtml(result.runId)}</title><style>body{font:14px system-ui;margin:2rem;line-height:1.5}table{border-collapse:collapse;width:100%}th,td{padding:.5rem;text-align:left;border:1px solid #ccc}thead{background:#eee}</style></head><body><h1>FRMS test run ${escapeHtml(result.runId)}</h1><p>${escapeHtml(md.split('\n').slice(2,5).join(' '))}</p><table><thead><tr><th>Case</th><th>Area</th><th>Outcome</th><th>Oracle approval</th><th>Attempts</th></tr></thead><tbody>${body}</tbody></table></body></html>`)
  const csv = path.join(runDir, 'jira_defects.csv')
  if (!fs.existsSync(csv)) fs.writeFileSync(csv, JIRA_HEADER+'\n')
  return result
}
