[CmdletBinding()]
param(
    [ValidateSet('Start', 'Stop', 'Restart', 'Status')]
    [string] $Action = 'Status',
    [string] $EnvFile
)

$ErrorActionPreference = 'Stop'
$backendRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $backendRoot 'compose.postgresql.yml'
if ([string]::IsNullOrWhiteSpace($EnvFile)) {
    $EnvFile = Join-Path $backendRoot 'Frms.Api/.env'
}
if (-not (Test-Path -LiteralPath $EnvFile -PathType Leaf)) {
    throw 'Create the ignored local API .env using config/postgresql.local.template; preserve existing secrets.'
}
$dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
$dockerPath = if ($dockerCommand) { $dockerCommand.Source } else { $null }
if (-not $dockerPath) {
    # An existing Codex/terminal process may predate Docker's PATH update.
    $dockerCandidates = @(
        (Join-Path $env:ProgramFiles 'Docker/Docker/resources/bin/docker.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs/DockerDesktop/resources/bin/docker.exe')
    )
    $dockerPath = $dockerCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}
if (-not $dockerPath) {
    throw 'Docker CLI is unavailable. Install and start Docker Desktop with its WSL2 Linux-container backend.'
}

$composeArguments = @('compose', '--env-file', (Resolve-Path -LiteralPath $EnvFile).Path, '-f', $composeFile)
switch ($Action) {
    'Start'   { & $dockerPath @composeArguments up --detach --wait postgres }
    'Stop'    { & $dockerPath @composeArguments stop postgres }
    'Restart' { & $dockerPath @composeArguments restart postgres }
    'Status'  { & $dockerPath @composeArguments ps postgres }
}
if ($LASTEXITCODE -ne 0) { throw "Docker Compose failed with exit code $LASTEXITCODE." }
# Deliberately no volume-deletion operation: Stop/Restart preserve local data.
