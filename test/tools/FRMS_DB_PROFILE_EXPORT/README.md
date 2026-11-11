# FRMS PostgreSQL Profile Export (read-only)

Creates a **sanitized, complete-row snapshot of safe metadata** from all **base tables in `public`**. All tables are inventoried, and each table's column names/types are described. Data values are exported only for allowlisted ID, state, role, date, facility/unit codes, and capacity fields. It never exports passwords, hashes, JWTs, API keys, emails, phone numbers, CCCDs, addresses, arbitrary text or JSON values. Inherently sensitive credential/token tables are skipped entirely. **This is not a raw database backup.**

It reads `backend/Frms.Api/.env` locally for `ConnectionStrings__Frms` / `ConnectionStrings__FrmsDb` (the same configuration keys used by the .NET backend), or honors existing process environment variables. It does not print or package `.env` or connection strings. It creates a PostgreSQL **READ ONLY** transaction: no inserts/updates/deletes/DDL/migrations.

## Run on Windows CMD

1. The tracked exporter lives in `test/tools/FRMS_DB_PROFILE_EXPORT/` beneath the repository. From the **repository root**, run the commands below. The exporter finds `backend/Frms.Api` by walking up its parent directories.
2. Run the exporter from the repo root. `RUN_EXPORT.cmd` automatically creates `%TEMP%\frms-db-profile-export-venv`, repairs pip inside that environment when possible, and installs `psycopg[binary]` on first use. **Do not use global `py -3 -m pip install`**, as the system pip may be broken.

3. Run:

   ```bat
   .\test\tools\FRMS_DB_PROFILE_EXPORT\RUN_EXPORT.cmd
   ```

   Alternatively: `py -3 .\test\tools\FRMS_DB_PROFILE_EXPORT\export_frms_profile.py --out ".\test\reports\db-profile"` to use the same destination when calling Python directly. Without `--out`, the standalone Python script still defaults to `%USERPROFILE%\FRMS_PROFILE_EXPORTS\`.

4. The `RUN_EXPORT.cmd` default output is **inside the repository but Git-ignored** at `test/reports/db-profile/`:

   - `FRMS_PROFILE_SAFE_<UTC>.zip` — sanitized schema, all eligible rows, manifest, candidate fixture report.
   - `FRMS_PROFILE_SUMMARY_<UTC>.json` — counts, chosen safe columns, and fixture candidates. Share this first if the ZIP is large.

Optional: `RUN_EXPORT.cmd --row-limit 5000` to cap rows in very large databases, or `RUN_EXPORT.cmd --out "D:\safe-output"` to override the default output directory. The script passes its default `--out` first; a caller-supplied `--out` overrides it. Default exports **all rows** for each eligible table, streaming to ZIP without reading the whole DB into memory. It only exports `public` by default; never point this at unrelated `auth`, `storage`, or system schemas.

## What you should share

Attach `FRMS_PROFILE_SAFE_...zip` or the standalone summary JSON in chat to build and verify `profile.md`. **Do not attach** PostgreSQL `.env`, connection strings, raw DB dumps, JWTs, password hashes or full `UserAccounts` rows. IDs can still identify people through other datasets, so review contents and share only if authorized.

The ZIP includes `fixture_candidates.json`:
- `assignable_manager_f01`: Reservation `CONFIRMED` with a Reservation Visit `CHECKED_IN` and matching same-facility/same-UnitType physical Storage Unit `AVAILABLE`.
- `cross_facility_visit_f32`: Visits tied to Reservations at Facility 32 for cross-facility access tests.

These are **candidates only**. Final tests must verify actual roles, ownership, current state, and API permissions. This script never makes business-state changes to manufacture fixtures.

## Failure scenarios

- `Cannot locate backend/Frms.Api`: keep the exporter inside this repository's directory tree, under `test/tools/`.
- `Missing dependency`: the CMD launcher should install `psycopg[binary]` automatically in its own venv. If installation fails, inspect network access or the venv's pip; do not use system pip.
- `Missing ConnectionStrings`: ensure the backend `.env` exists in your local repo (do not upload it).
- `PostgreSQL connection/export failed`: check Supabase/PG network access, TLS, or DB permissions on your own machine. Do not paste credentials.

Do **not commit** the resulting ZIP or JSON to Git. The default output folder now lives under `test/reports/db-profile/` and is excluded from Git by `test/reports/.gitignore`. This is a protection against accidental tracking, not a substitute for reviewing sensitive IDs before sharing.

**Windows Python pip fix:** The CMD launcher does not import pip from your system Python. It creates and reuses `%TEMP%\frms-db-profile-export-venv` and installs the dependency within that environment. If the isolated environment itself is damaged, close sessions using it, remove **only** that temporary venv (after reviewing its path), and rerun the launcher. You can still run Python directly using `--out ".\test\reports\db-profile"`; direct Python execution requires you to use the venv interpreter yourself.
