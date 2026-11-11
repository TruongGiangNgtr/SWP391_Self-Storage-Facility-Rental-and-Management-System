# FRMS — AI-ready test handoff (TEST_SWT)

**Start here when moving to a new ChatGPT conversation or another AI coding session.**

This directory is **`test/handoff/` (singular)** as requested. Executable tests remain in `tests/` (plural). Files here are documentation and machine-readable context only; no runtime file has been changed.

## Read order

1. [HANDOFF.md](HANDOFF.md) — self-contained project handoff and current state
2. [context.json](context.json) — exact coverage, environment, repository and safety metadata
3. [fixtures.json](fixtures.json) — fixture name/value/status catalogue with evidence levels
4. [EVIDENCE.md](EVIDENCE.md) — verified snapshot facts, historical test execution and open discrepancies
5. [RUNBOOK.md](RUNBOOK.md) — exact Windows CMD commands, CI/runner behavior, reports and safe diagnostics
6. [NEXT_STEPS.md](NEXT_STEPS.md) — prioritized backlog and acceptance gates
7. [FRMS-ST-0065.md](FRMS-ST-0065.md) — ready-to-run read-only Manager F01 test data and exact commands
8. [NEGATIVE_LOGIN_FIXTURES.md](NEGATIVE_LOGIN_FIXTURES.md) — verified negative-account fixture preflight and commands

## Canonical source files (do not duplicate or overwrite)

- `tests/system/manifest/cases.json`: **275** testcase IDs and implementation mappings
- `tests/system/README.md`: canonical current automation guide
- `tests/system/PATCH_HISTORY.md`: consolidated patch history
- `RUN_FRMS_TESTS.cmd` + `RUN_FRMS_TESTS.ps1`: one-command test entrypoint
- `run-frms-tests.ps1`: underlying launcher
- `tests/system/scripts/`: discovery, coverage, preflight, result normalization and Jira export
- `tests/e2e/playwright/tests/frms/`: executable specs
- `profile.md`: historical notes; portions of its embedded statistics are outdated
- `test/tools/`: versioned Batch 12 fixture packages and a sanitized PostgreSQL exporter. See [`../tools/README.md`](../tools/README.md). **Do not commit its data output.**

## AI usage and evidence rules

- Prefer `context.json` and `fixtures.json` for structured retrieval; inspect source code before proposing changes.
- A registry item marked `IMPLEMENTED` does **not** mean it passed.
- `SNAPSHOT_CANDIDATE` does **not** mean a current API has authorized or verified it.
- Never invent a fixture UUID, missing account identity, API error text, PASS status, Jira defect or successful role authorization.
- Do not run database writes, migrations, test-data generation, payment callbacks or destructive commands on shared Supabase.
- Do not treat optional patch folders as applied code.
- Work on `TEST_SWT` only; do not touch `main`. For a related change batch, create **one commit**, not one commit per file.
- For a new live run, demand `summary.md`/`results.json` evidence before updating counts.

Last reviewed: **2026-11-11**, against branch head `145e9f965f3a` before adding these handoff files.

- [BATCH13_COVERAGE.md](BATCH13_COVERAGE.md): 37 new Playwright mappings, 50.18% implementation coverage, safety and remaining runtime gates.

- [RUN_20261111T085924993Z.md](RUN_20261111T085924993Z.md): full-run summary, area triage, failure and blocker IDs.
- [RUN_20261111T085924993Z.json](RUN_20261111T085924993Z.json): all 275 case statuses with report provenance and Blocked classification.

- [FIX_RUN_20261111T085924993Z.md](FIX_RUN_20261111T085924993Z.md): 17 testcase automation repairs grounded in uploaded `results.json`; safe targeted rerun procedure and unresolved infrastructure/fixture cases.

- [TARGETED_RERUNS_20261111_AFTER_8826f5e.md](TARGETED_RERUNS_20261111_AFTER_8826f5e.md): four independent post-patch 1/1 PASS runs (console-provenance), coverage check and next validation steps.
