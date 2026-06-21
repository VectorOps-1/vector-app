param(
    [int]$Port = 5000,
    [string]$Project = "vector-app-local.csproj",
    [switch]$Pull,
    [switch]$NoPull,
    [switch]$ForcePort,
    [switch]$Watch,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

Write-Host "AcuityOps local launcher"
Write-Host "Repo: $Root"
Write-Host "Port: $Port"

if ($Pull -and -not $NoPull) {
    Write-Host "Pulling latest main..."
    git pull --ff-only origin main
}
else {
    Write-Host "Skipping git pull. Use -Pull when you explicitly want the latest remote main."
}

function Get-PortProcessIds {
    $connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($connections) {
        return $connections | Select-Object -ExpandProperty OwningProcess -Unique
    }

    $netstatRows = netstat -ano -p tcp | Select-String -Pattern ":$Port\s+.*LISTENING\s+(\d+)"
    foreach ($row in $netstatRows) {
        if ($row.Matches.Count -gt 0) {
            $row.Matches[0].Groups[1].Value
        }
    }
}

$processIds = @(Get-PortProcessIds | Where-Object { $_ } | Sort-Object -Unique)

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
        throw "Port $Port is already used by PID $processId ($($process.ProcessName)). Close it or rerun with -ForcePort."
    }
}

Start-Sleep -Milliseconds 500

Write-Host ""
Write-Host "Open:"
Write-Host "  http://localhost:$Port/Access"
Write-Host ""
Write-Host "Starting app..."

$dotnetArgs = @()
if ($Watch) {
    $dotnetArgs += @("watch", "run")
}
else {
    $dotnetArgs += "run"
}

if ($NoBuild) {
    $dotnetArgs += "--no-build"
}

$dotnetArgs += @("--project", $Project, "--urls", "http://0.0.0.0:$Port")

dotnet @dotnetArgs
