# FRMS — Backend Docker configuration (Render)

This package adds **two files** to the existing repository. No GitHub changes were made automatically.

Copy the files exactly to:

- `backend/Dockerfile`
- `backend/.dockerignore`

The Docker build **context must be `backend/`**, not `backend/Frms.Api/`, because `Frms.Api` references `Frms.Business`, `Frms.DataAccess`, `Frms.Infrastructure` and `Frms.DataAccess` compiles linked files from `Frms.Migrations.PostgreSql`.

## Local build and test

Run from the *repository root*:

```bash
docker build -f backend/Dockerfile -t frms-api:local ./backend
```

Set runtime secrets via an ignored env file or `--env-file` (do not copy secrets into the image):

```bash
docker run --rm -p 10000:10000 --env-file backend/Frms.Api/.env frms-api:local
curl -i http://localhost:10000/health
```

The local `.env` must have valid .NET environment variable names and values; Docker `--env-file` parsing is distinct from DotNetEnv. `ASPNETCORE_ENVIRONMENT` in the image is `Production`; local .env file itself is not copied to the container.

## Render web service

- Runtime/language: **Docker**
- Root directory: **`backend`**
- Dockerfile path: **`Dockerfile`** (relative to root directory)
- Docker build context directory: **`.`** (relative to root directory)
- Port: **`10000`**; server listens on `0.0.0.0:10000`
- Health check path: **`/health`**

If Render shows repo-root-relative settings instead, use Dockerfile Path `backend/Dockerfile` and Docker Build Context `backend`; do not mix reference bases.

## Production environment variables (Render dashboard; do not commit values)

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Frms=Host=<SUPABASE_HOST>;Port=<SUPABASE_PORT>;Database=<DB>;Username=<DB_USER>;Password=<DB_PASSWORD>;SSL Mode=VerifyFull;Multiplexing=false;Include Error Detail=false
Jwt__SigningKey=<RANDOM_SECRET_AT_LEAST_32_UTF8_BYTES>
# Optional if defaults in backend/Frms.Api/appsettings.json are approved:
# Jwt__Issuer=frms-api
# Jwt__Audience=frms-web

# For payment features ONLY once approved for real-payOS usage:
# Payment__PayOS__ClientId=<client_id>
# Payment__PayOS__ApiKey=<secret>
# Payment__PayOS__ChecksumKey=<secret>
# Payment__PayOS__ApiBaseUrl=https://api-merchant.payos.vn
# Payment__PayOS__ReturnUrl=https://<VERCEL_DOMAIN>/customer/payments/result
# Payment__PayOS__CancelUrl=https://<VERCEL_DOMAIN>/customer/payments/result
# Payment__PayOS__WebhookUrl=https://<RENDER_DOMAIN>/api/v1/payments/payos/webhook
# Payment__PayOS__ExpiryMinutes=15
```

**Supabase TLS:** `SSL Mode=VerifyFull` must validate the actual hostname and certificate chain. If the selected Supabase database endpoint requires the bundled Supabase 2021 CA, append `Root Certificate=/app/certs/supabase-root-2021.crt` to the Npgsql connection string. Do not blindly apply that CA to unrelated endpoints.

**Supabase pooling:** Use direct DB or *session* pooling based on your approved Supabase connection values; current FRMS DataAccess requires session-affine connections and explicitly rejects Npgsql multiplexing. Do not assume transaction pooling is suitable.

**payOS safety:** No payOS credentials are included. The backend README explicitly disallows live payOS operations before separate approval; do not perform real-payment tests based on this Docker setup alone.

## Caveats

- A successful container start is **not** proof that the PostgreSQL schema and payment workflow are ready. `/health` checks the PostgreSQL connection and returns 503 when it fails.
- Docker image creation does not apply EF Core migrations. Do not run schema changes automatically against the existing Supabase database.
- Swagger UI is configured only in Development. Production exposes `/openapi/v1.json` (not necessarily Swagger UI).
- This Docker config is prepared from GitHub `main` as inspected on 2026-11-03. Actual Docker build and runtime smoke test have not been executed in this environment.
