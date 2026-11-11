#!/usr/bin/env node
/** Replace only the installed Batch12 read-only discovery helper; never touch launcher or credentials. */
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const originalSha = 'a9134abc736019f3646d040fb3fe955509b0c07ed2cb147b5f694b878902a9a6';
const updatedSha = 'd868ce1274ae7d191b5eeed0e2411a34ccce519ebed7d805de453145234a6639';
const sha = data => crypto.createHash('sha256').update(data).digest('hex');
const stop = message => { console.error('ERROR:', message); process.exitCode = 2; };
const root = process.cwd();
const src = path.join(path.dirname(fileURLToPath(import.meta.url)), 'batch12-discover.mjs');
const dest = path.join(root, 'tests', 'system', 'scripts', 'batch12-discover.mjs');
const launcher = path.join(root, 'run-frms-tests.ps1');
if (!fs.existsSync(src) || !fs.existsSync(dest) || !fs.existsSync(launcher)) {
  stop('Run from the TEST_SWT repository root with the Batch 12 launcher and discovery helper already installed.');
} else {
  let branch = '';
  try { branch = execFileSync('git', ['branch', '--show-current'], { cwd: root, encoding: 'utf8' }).trim(); }
  catch { stop('Cannot determine current Git branch.'); }
  if (branch && branch !== 'TEST_SWT') stop(`Expected branch TEST_SWT; found ${branch}.`);
  else if (!fs.readFileSync(launcher, 'utf8').includes('function Resolve-Batch12Fixtures')) {
    stop('Batch 12 fixture launcher hook not present. Existing launcher left unchanged.');
  } else {
    const current = fs.readFileSync(dest);
    const next = fs.readFileSync(src);
    if (sha(next) !== updatedSha) stop('Refinement source checksum mismatch.');
    else if (sha(current) === updatedSha) console.log('Batch 12 fixture refinement already installed; no changes.');
    else if (sha(current) !== originalSha) stop('Existing discovery script differs from expected Batch 12 baseline; not overwriting custom changes.');
    else {
      execFileSync(process.execPath, ['--check', src], { stdio: 'pipe' });
      const backup = `${dest}.before-refinement.bak`;
      if (!fs.existsSync(backup)) fs.copyFileSync(dest, backup);
      fs.copyFileSync(src, dest);
      console.log('Updated read-only discovery script:', path.relative(root, dest));
      console.log('Backup:', path.relative(root, backup));
      console.log('Launcher, .env, database, production code, and credentials were not modified.');
    }
  }
}
