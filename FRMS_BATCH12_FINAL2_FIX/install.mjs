#!/usr/bin/env node
/** Safely installs two-fixture read-only discovery fix, guarded by known checksums. */
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const root = process.cwd();
const here = path.dirname(fileURLToPath(import.meta.url));
const sha = content => crypto.createHash('sha256').update(content).digest('hex');
const files = [
  { name: 'batch12-discover.mjs', dest: 'tests/system/scripts/batch12-discover.mjs',
    baselines: ['a9134abc736019f3646d040fb3fe955509b0c07ed2cb147b5f694b878902a9a6', 'd868ce1274ae7d191b5eeed0e2411a34ccce519ebed7d805de453145234a6639'],
    expected: 'ca8136d312247ebb18def40f0410f0b09583ee0a176c4e283af4558fd5d3d834' },
  { name: 'batch12-staff.spec.ts', dest: 'tests/e2e/playwright/tests/frms/batch12-staff.spec.ts',
    baselines: ['37fe2f07cef4d0e20df336686ba2be2cf033b1f77ad63092f026771fa6efa531'],
    expected: 'ab81d37ef3369a0e768c78ad3928c495a3ef7de0cf1e214b0b4568b4271fd5f4' },
];
function stop(message) { console.error('ERROR:', message); process.exit(2); }
if (!fs.existsSync(path.join(root, 'run-frms-tests.ps1'))) stop('Run this in the repository root.');
let branch;
try { branch = execFileSync('git', ['branch','--show-current'], { encoding:'utf8' }).trim(); }
catch { stop('Cannot identify Git branch.'); }
if (branch !== 'TEST_SWT') stop(`Expected TEST_SWT branch, got ${branch}.`);
const read = file => fs.readFileSync(file);
const operations = [];
for (const f of files) {
  const src = path.join(here, f.name), dst = path.join(root, f.dest);
  if (!fs.existsSync(src) || !fs.existsSync(dst)) stop(`Required file missing: ${f.dest}`);
  const desired = sha(read(src)), current = sha(read(dst));
  if (desired !== f.expected) stop(`Package integrity mismatch: ${f.name}`);
  if (current !== desired && !f.baselines.includes(current))
    stop(`Locally modified or unsupported version: ${f.dest} (SHA256 ${current}); preserving it.`);
  operations.push({ ...f, src, dst, current, desired });
}
execFileSync(process.execPath, ['--check', operations[0].src]);
const launcher = read(path.join(root, 'run-frms-tests.ps1')).toString();
if (!launcher.includes('function Resolve-Batch12Fixtures')) stop('Batch 12 discovery launcher hook not installed.');
const prepared = [];
try {
  for (const op of operations.filter(x => x.current !== x.desired)) {
    const backup = `${op.dst}.before-final2.bak`;
    if (!fs.existsSync(backup)) fs.copyFileSync(op.dst, backup);
    fs.copyFileSync(op.src, op.dst);
    prepared.push(op);
    console.log(`Updated: ${op.dest} (backup: ${path.relative(root, backup)})`);
  }
} catch (error) {
  for (const op of prepared) fs.copyFileSync(`${op.dst}.before-final2.bak`, op.dst);
  stop(`Update failed; installed files restored. ${error.message}`);
}
if (!prepared.length) console.log('Two-fixture discovery fix already installed; no changes.');
console.log('Launcher, credentials, .env and backend database were not modified.');
