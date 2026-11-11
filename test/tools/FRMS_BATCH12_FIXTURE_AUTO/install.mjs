#!/usr/bin/env node
/** Idempotently installs Batch 12 fixture discovery into the existing launcher. */
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
const root = process.cwd();
const launcher = path.join(root, 'run-frms-tests.ps1');
const audit = path.join(root, 'tests', 'system', 'scripts', 'batch12-env-audit.mjs');
const source = path.join(path.dirname(fileURLToPath(import.meta.url)), 'tests', 'system', 'scripts', 'batch12-discover.mjs');
const target = path.join(root, 'tests', 'system', 'scripts', 'batch12-discover.mjs');
function stop(s) { console.error(`ERROR: ${s}`); process.exit(2); }
if (!fs.existsSync(launcher) || !fs.existsSync(audit)) stop('Run this installer from the TEST_SWT repo root after applying Batch 12.');
if (!fs.existsSync(source)) stop('Missing fixture discover script next to the installer.');
let branch = '';
try { branch = execFileSync('git', ['branch', '--show-current'], { cwd: root, encoding: 'utf8' }).trim(); } catch { stop('Unable to read current git branch.'); }
if (branch !== 'TEST_SWT') stop(`Expected branch TEST_SWT, current: ${branch}`);
const raw = fs.readFileSync(launcher, 'utf8');
const eol = raw.includes('\r\n') ? '\r\n' : '\n';
if (raw.includes('function Resolve-Batch12Fixtures')) {
  console.log('Batch 12 launcher hook already installed; preserving existing launcher.');
} else {
  const match = (source, text, description) => {
    const count = source.split(text).length - 1;
    if (count !== 1) stop(`Launcher layout changed (${description}: ${count} anchors). Nothing modified.`);
  };
  const roleAnchor = "    Set-DefaultEnv 'FRMS_TEST_STAFF_PASSWORD' '11111111'";
  const fnAnchor = 'try {'; // Use unique tail-level anchor with an adjacent call in the script.
  const runAnchor = "        if ($InstallBrowser) {";
  match(raw, roleAnchor, 'demo credential defaults');
  match(raw, runAnchor, 'launcher run hook');
  const functionAnchor = `${eol}try {${eol}    Set-Location -LiteralPath $Repo`;
  match(raw, functionAnchor, 'entry point');
  const defaults = [
    "    Set-DefaultEnv 'FRMS_TEST_CUSTOMER_2_PASSWORD' '11111111'",
    "    Set-DefaultEnv 'FRMS_TEST_MANAGER_01_EMAIL' 'manager.f01@example.com'",
    "    Set-DefaultEnv 'FRMS_TEST_MANAGER_PASSWORD' '11111111'",
    "    Set-DefaultEnv 'FRMS_TEST_BOM_EMAIL' 'bom@example.com'",
    "    Set-DefaultEnv 'FRMS_TEST_BOM_PASSWORD' '11111111'",
    "    Set-DefaultEnv 'FRMS_TEST_ADMIN_EMAIL' 'admin@example.com'",
    "    Set-DefaultEnv 'FRMS_TEST_ADMIN_PASSWORD' '11111111'",
  ];
  const functionText = [
    'function Resolve-Batch12Fixtures {',
    "    $scriptFile = Join-Path $Repo 'tests\\system\\scripts\\batch12-discover.mjs'",
    '    if (-not (Test-Path -LiteralPath $scriptFile)) {',
    "        Write-Warning 'Batch 12 fixture discovery script not found; missing fixtures remain blocked.'",
    '        return',
    '    }',
    '    try {',
    '        $rawJson = & node $scriptFile',
    "        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($rawJson)) { throw 'Node discovery failed.' }",
    '        $result = $rawJson | ConvertFrom-Json',
    '        $count = 0',
    '        foreach ($item in $result.fixtures.PSObject.Properties) {',
    "            if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($item.Name, 'Process'))) {",
    "                [Environment]::SetEnvironmentVariable($item.Name, [string]$item.Value, 'Process')",
    '                $count++',
    '            }',
    '        }',
    '        Write-Host "Batch 12 fixture discovery: $count verified value(s) supplied to this launcher process."',
    '        foreach ($item in $result.roles.PSObject.Properties) {',
    '            $state = if ($item.Value) { "verified" } else { "unavailable" }',
    '            Write-Host "  $($item.Name): $state"',
    '        }',
    '        foreach ($note in @($result.notes)) { Write-Warning "Batch 12 fixture: $note" }',
    "        & node (Join-Path $Repo 'tests\\system\\scripts\\batch12-env-audit.mjs') | Out-Host",
    "        if ($LASTEXITCODE -ne 0) { throw 'Batch 12 mandatory environment audit failed.' }",
    '    } catch {',
    '        Write-Warning ("Batch 12 fixture discovery unavailable: " + $_.Exception.Message)',
    "        Write-Warning 'Unresolved fixtures remain SKIPPED/Blocked, not PASS.'",
    '    }',
    '}',
  ].join(eol);
  let revised = raw.replace(roleAnchor, `${roleAnchor}${eol}${defaults.join(eol)}`);
  revised = revised.replace(functionAnchor, `${eol}${functionText}${eol}${functionAnchor}`);
  revised = revised.replace(runAnchor, `        if ($Mode -in @('all','case','area')) { Resolve-Batch12Fixtures }${eol}${runAnchor}`);
  const backup = `${launcher}.batch12.bak`;
  if (!fs.existsSync(backup)) fs.writeFileSync(backup, raw, 'utf8');
  fs.writeFileSync(launcher, revised, 'utf8');
  console.log('Launcher hook installed idempotently. Local backup: run-frms-tests.ps1.batch12.bak');
}
fs.copyFileSync(source, target);
console.log('Read-only discovery script: tests/system/scripts/batch12-discover.mjs');
console.log('No .env, database data, production code or credentials were modified.');
