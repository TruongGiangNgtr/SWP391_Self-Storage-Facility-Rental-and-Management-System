# FRMS PostgreSQL Profile Export (read-only)

Creates a **sanitized, complete-row snapshot of safe metadata** from all **base tables in `public`**. All tables are inventoried, and each table's column names/types are described. Data values are exported only for allowlisted ID, state, role, date, facility/unit codes, and capacity fields. It never exports passwords, hashes, JWTs, API keys, emails, phone numbers, CCCDs, addresses, arbitrary text or JSON values. Inherently sensitive credential/token tables are skipped entirely. **This is not a raw database backup.**

It reads `backend/Frms.Api/.env` locally for `ConnectionStrings__Frms` / `ConnectionStrings__FrmsDb` (the same configuration keys used by the .NET backend), or honors existing process environment variables. It does not print or package `.env` or connection strings. It creates a PostgreSQL **READ ONLY** transaction: no inserts/updates/deletes/DDL/migrations.

## Run on Windows CMD

1. The tracked exporter lives in `test/tools/FRMS_DB_PROFILE_EXPORT/` beneath the repository. From the **repository root**, run the commands below. The exporter finds `backend/Frms.Api` by walking up its parent directories.
2. Install the single Python dependency:

   ```bat
   py -3 -m pip install "psycopg[binary]"
   ```

3. Run the exporter from the repo root:

   ```bat
   .\test\tools\FRMS_DB_PROFILE_EXPORT\RUN_EXPORT.cmd
   ```

   Alternatively: `py -3 .\test\tools\FRMS_DB_PROFILE_EXPORT\export_frms_profile.py`.

4. Output is outside the repo at `%USERPROFILE%\FRMS_PROFILE_EXPORTS\`:

   - `FRMS_PROFILE_SAFE_<UTC>.zip` — sanitized schema, all eligible rows, manifest, candidate fixture report.
   - `FRMS_PROFILE_SUMMARY_<UTC>.json` — counts, chosen safe columns, and fixture candidates. Share this first if the ZIP is large.

Optional: `RUN_EXPORT.cmd --row-limit 5000` to cap rows in very large databases, or `--out "D:\safe-output"` to choose output directory. Default exports **all rows** for each eligible table, streaming to ZIP without reading the whole DB into memory. It only exports `public` by default; never point this at unrelated `auth`, `storage`, or system schemas.

## What you should share

Attach `FRMS_PROFILE_SAFE_...zip` or the standalone summary JSON in chat to build and verify `profile.md`. **Do not attach** PostgreSQL `.env`, connection strings, raw DB dumps, JWTs, password hashes or full `UserAccounts` rows. IDs can still identify people through other datasets, so review contents and share only if authorized.

The ZIP includes `fixture_candidates.json`:
- `assignable_manager_f01`: Reservation `CONFIRMED` with a Reservation Visit `CHECKED_IN` and matching same-facility/same-UnitType physical Storage Unit `AVAILABLE`.
- `cross_facility_visit_f32`: Visits tied to Reservations at Facility 32 for cross-facility access tests.

These are **candidates only**. Final tests must verify actual roles, ownership, current state, and API permissions. This script never makes business-state changes to manufacture fixtures.

## Failure scenarios

- `Cannot locate backend/Frms.Api`: keep the exporter inside this repository's directory tree, under `test/tools/`.
- `Missing dependency`: run the installation command above; no need to modify .NET project dependencies.
- `Missing ConnectionStrings`: ensure the backend `.env` exists in your local repo (do not upload it).
- `PostgreSQL connection/export failed`: check Supabase/PG network access, TLS, or DB permissions on your own machine. Do not paste credentials.

Do **not commit** the resulting ZIP or JSON to Git. The default output folder intentionally lives outside the repository.

**Windows Python pip repair:** The tracked `RUN_EXPORT.cmd` uses system `py -3`; if its pip is broken, do not alter project dependencies. Create a separate virtual environment (for example `%TEMP%\frms-profile-export-venv`), install `psycopg[binary]` inside it and invoke `export_frms_profile.py` with that environment's `python.exe`. The absolute `__file__` lookup makes this work from any current working directory.
