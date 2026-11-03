# Build context: repository's backend/ directory.
# Example (from repository root): docker build -f backend/Dockerfile -t frms-api ./backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Keep restore layers cacheable. Central package versions live in backend/.
COPY Directory.Build.props Directory.Packages.props NuGet.Config ./
COPY Frms.Api/Frms.Api.csproj Frms.Api/
COPY Frms.Business/Frms.Business.csproj Frms.Business/
COPY Frms.DataAccess/Frms.DataAccess.csproj Frms.DataAccess/
COPY Frms.Infrastructure/Frms.Infrastructure.csproj Frms.Infrastructure/
COPY Frms.Migrations.PostgreSql/Frms.Migrations.PostgreSql.csproj Frms.Migrations.PostgreSql/
RUN dotnet restore Frms.Api/Frms.Api.csproj --configfile NuGet.Config

# Includes migrations' linked model configurations and API Content/*.json.
COPY . .
RUN dotnet publish Frms.Api/Frms.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

COPY --from=build /app/publish ./
# Optional Supabase CA certificate is already tracked in this repository.
# Only reference it in the connection string when required for the chosen DB endpoint.
COPY --from=build /src/Frms.Api/prod-ca-2021.crt ./certs/supabase-root-2021.crt

# Use the non-root user provided by official .NET images.
USER $APP_UID
ENTRYPOINT ["dotnet", "Frms.Api.dll"]
