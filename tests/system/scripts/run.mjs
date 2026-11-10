import fs from 'node:fs'
import path from 'node:path'
import { spawnSync, spawn } from 'node:child_process'
import { ROOT, REPORTS, loadManifest, parseArgs, selectCases, safeRunId, assertSafeRunId } from './common.mjs'
import { normalize } from './normalize-results.mjs'

const options = parseArgs()
const workers = Number(process.env.FRMS_PLAYWRIGHT_WORKERS ?? '4')
const retries = Number(process.env.FRMS_PLAYWRIGHT_RETRIES ?? '1')
if (!Number.isInteger(workers) || workers < 1 || workers > 16) throw new Error('FRMS_PLAYWRIGHT_WORKERS must be an integer in [1, 16].')
if (!Number.isInteger(retries) || retries < 0 || retries > 3) throw new Error('FRMS_PLAYWRIGHT_RETRIES must be an integer in [0, 3].')
const manifest = loadManifest()
const selection = selectCases(manifest, options)
const runnable = selection.filter(item => item.automation === 'IMPLEMENTED')
const runId = assertSafeRunId(options['run-id'] ?? safeRunId())
const runDir = path.join(REPORTS, runId)
if (fs.existsSync(runDir)) throw new Error(`Run directory already exists: ${runId}. Supply a new --run-id.`)
fs.mkdirSync(runDir, { recursive: true })
const sha = spawnSync('git', ['rev-parse','HEAD'], { cwd:ROOT, encoding:'utf8' }).stdout?.trim() ?? null
let exitCode = 0
let runnerError = false

// Never invoke a shell to execute Node: Windows installations often live under
// "C:\Program Files", which shell:true splits or misquotes (DEP0190).
// Running the Playwright CLI through the current Node binary also avoids .cmd
// wrappers and preserves --grep as one argument on every supported platform.
function execute(command, argv, extraEnv = {}, cwd = ROOT) {
  return new Promise(resolve => {
    let settled = false
    const finish = code => { if (!settled) { settled = true; resolve(code) } }
    const child = spawn(command, argv, {
      cwd, stdio: 'inherit', shell: false,
      env: { ...process.env, ...extraEnv },
    })
    child.on('error', error => {
      console.error(`RUNNER: child process could not start (${error.code ?? error.name}).`)
      finish(2)
    })
    child.on('close', code => finish(code ?? 2))
  })
}
if (runnable.length) {
  const preflight = await execute(process.execPath, ['tests/system/scripts/preflight.mjs'])
  if (preflight !== 0) { runnerError = true; exitCode = 2 }
  else {
    const grep = runnable.map(item => item.id.replace(/[-]/g, '\\-')+'(?![-A-Z0-9])').join('|')
    const playwrightRoot = path.join(ROOT, 'tests', 'e2e', 'playwright')
    const playwrightCli = path.join(playwrightRoot, 'node_modules', '@playwright', 'test', 'cli.js')
    if (!fs.existsSync(playwrightCli)) {
      console.error('RUNNER: Playwright CLI is missing. Run pnpm install --frozen-lockfile from the repository root.')
      runnerError = true
      exitCode = 2
    } else {
      exitCode = await execute(process.execPath, [playwrightCli, 'test', 'tests/frms', '--grep', grep,
        '--reporter=json', `--retries=${retries}`, `--workers=${workers}`, '--output', path.join(runDir, 'artifacts')], {
        PLAYWRIGHT_JSON_OUTPUT_FILE: path.join(runDir, 'playwright-results.json'),
        FRMS_API_URL: process.env.FRMS_API_URL ?? 'http://127.0.0.1:5164',
        FRMS_UI_URL: process.env.FRMS_UI_URL ?? 'http://127.0.0.1:4173',
        FRMS_ENVIRONMENT: 'Testing',
      }, playwrightRoot)
      if (exitCode !== 0 && !fs.existsSync(path.join(runDir, 'playwright-results.json'))) runnerError = true
    }
  }
}
const result = normalize(runDir, selection.map(item => item.id), { sha, runnerExitCode:exitCode, runnerError })
const exporter = spawnSync(process.execPath, ['tests/system/scripts/export-jira-defects.mjs','--run-id',runId], {cwd:ROOT, stdio:'inherit'})
if (exporter.status !== 0) exitCode = 2
const relevant = result.cases.filter(item => selection.some(selected => selected.id === item.id))
const state = relevant.map(row => row.status)
if (state.includes('UnconfirmedFailure')) exitCode = 1
else if (state.includes('Blocked')) exitCode = 2
else if (state.some(s => ['Not Run','Review','Flaky'].includes(s))) exitCode = 3
console.log(`FRMS run ${runId}: ${relevant.length} selected, ${relevant.filter(row => row.attempts).length} executed. Reports: tests/system/reports/${runId}`)
process.exitCode = exitCode
