param(
    [int]$Port = 5000,
    [string]$Project = "vector-app-local.csproj",
    [switch]$NoPull,
    [switch]$ForcePort
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

Write-Host "Vector dev launcher"
Write-Host "Repo: $Root"
Write-Host "Port: $Port"

if (-not $NoPull) {
    Write-Host "Pulling latest main..."
    git pull --ff-only origin main
}

$connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
$processIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique

foreach ($processId in $processIds) {
    $process = Get-Process -Id $processId -ErrorAction SilentlyContinue

    if ($null -eq $process) {
        continue
    }

    if ($process.ProcessName -like "dotnet*" -or $ForcePort) {
        Write-Host "Stopping PID $processId ($($process.ProcessName))..."
        Stop-Process -Id $processId -Force
    }
    else {
        Write-Host "Leaving PID $processId ($($process.ProcessName)) running. Use -ForcePort to stop it anyway."
    }
}

Write-Host "Starting app..."
dotnet watch run --project $Project --urls "http://0.0.0.0:$Port"
