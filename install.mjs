#!/usr/bin/env node
/** Idempotent installer: injects a verified negative-fixture preflight into the existing wrapper. */
import fs from 'node:fs';
import path from 'node:path';

const dirname = path.dirname(new URL(import.meta.url).pathname.replace(/^\/(?=[A-Za-z]:\/)/, ''));
const repo = path.resolve(dirname, '..');
const wrapper = path.join(repo, 'RUN_FRMS_TESTS.ps1');
const target = path.join(repo, 'tests', 'system', 'scripts', 'batch12-verify-unregistered.mjs');
const source = path.join(dirname, 'batch12-verify-unregistered.mjs');
const anchor = "    Write-Host '--- Batch 12 environment audit (missing fixture data is not an assertion failure) ---'";
const marker = '### BATCH12_VERIFIED_NEGATIVE_PREFLIGHT ###';
if (!fs.existsSync(wrapper)) {
  console.error('Cannot locate RUN_FRMS_TESTS.ps1 in repo root. Place this folder beside that file.');
  process.exit(2);
}
if (!fs.existsSync(source)) { console.error('Missing packaged verifier.'); process.exit(2); }
let original = fs.readFileSync(wrapper, 'utf8');
if (!original.includes(anchor) && !original.includes(marker)) {
  console.error('Existing test runner has an unexpected layout. No files changed.'); process.exit(2);
}
const inject = `    # ${marker}\n    # Preflight uses the full Admin directory. No existing phone/email or token is printed.\n    $negativeVerifier = Join-Path $Repo 'tests\\system\\scripts\\batch12-verify-unregistered.mjs'\n    if ((-not $env:FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE -or -not $env:FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL) -and\n        (Test-Path -LiteralPath $negativeVerifier)) {\n        $negativeRaw = (& node $negativeVerifier | Out-String).Trim()\n        if ($LASTEXITCODE -eq 0 -and $negativeRaw) {\n            try {\n                $negativeResult = $negativeRaw | ConvertFrom-Json -ErrorAction Stop\n                if ($negativeResult.verified -eq $true) {\n                    Set-DefaultEnv 'FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE' ([string]$negativeResult.fixtures.FRMS_TEST_UNREGISTERED_CUSTOMER_PHONE)\n                    Set-DefaultEnv 'FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL' ([string]$negativeResult.fixtures.FRMS_TEST_UNREGISTERED_EMPLOYEE_EMAIL)\n                    Write-Host ('Negative fixture preflight: two unknown identifiers verified absent from ' + $negativeResult.accountCount + ' Admin account records.')\n                } else {\n                    Write-Warning ('Negative fixture preflight: not verified (' + $negativeResult.reason + '); corresponding test cases remain Blocked.')\n                }\n            } catch { Write-Warning 'Negative fixture preflight returned unreadable result; negative tests remain Blocked.' }\n        }\n    }\n\n`;
fs.mkdirSync(path.dirname(target), { recursive: true });
if (!original.includes(marker)) {
  fs.copyFileSync(wrapper, `${wrapper}.negative-fixtures.bak`);
  fs.writeFileSync(wrapper, original.replace(anchor, inject + anchor), 'utf8');
}
fs.copyFileSync(source, target);
console.log(original.includes(marker) ? 'Negative fixture preflight already installed; verifier refreshed.' : 'Negative fixture preflight installed. Backup: RUN_FRMS_TESTS.ps1.negative-fixtures.bak');
console.log('No production code, DB records or credentials were modified.');
