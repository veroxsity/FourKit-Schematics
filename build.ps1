#requires -Version 5
# Build Schematics and deploy to the server plugins folder.
# Usage:  .\build.ps1            -> build + deploy (errors if server is running)
#         .\build.ps1 -StopServer -> auto-stop Minecraft.Server.exe first

param(
    [switch]$StopServer,
    [string]$Server
)

$ErrorActionPreference = 'Stop'

$dotnet = "$env:USERPROFILE\.dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

$plug   = $PSScriptRoot
if ($Server) {
    $serverPath = $Server
} elseif ($env:FOURKIT_SERVER) {
    $serverPath = $env:FOURKIT_SERVER
} else {
    $serverPath = Join-Path $plug '..\..\Server'
}
$server = (Resolve-Path $serverPath).Path

# Ensure FourKit reference is present in lib/
$libDll = Join-Path $plug 'lib\Minecraft.Server.FourKit.dll'
if (-not (Test-Path $libDll)) {
    Copy-Item (Join-Path $server 'Minecraft.Server.FourKit.dll') $libDll -Force
    Write-Host "Synced FourKit reference into lib/"
}

& $dotnet build (Join-Path $plug 'Schematics.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)" }

$built  = Join-Path $plug 'bin\Release\net10.0\Schematics.dll'
$target = Join-Path $server 'plugins\Schematics.dll'

$serverDir = $server.TrimEnd('\','/')
$running = Get-Process -Name 'Minecraft.Server' -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -and ([System.IO.Path]::GetDirectoryName($_.Path)).TrimEnd('\','/') -ieq $serverDir
}
if ($running) {
    if ($StopServer) {
        Write-Host "Stopping running server (PID $($running.Id))..."
        Stop-Process -Id $running.Id -Force
        Start-Sleep -Milliseconds 800
    } else {
        Write-Host ""
        Write-Host "WARNING: Minecraft.Server.exe is running (PID $($running.Id))." -ForegroundColor Yellow
        Write-Host "         Stop the server, or re-run with -StopServer." -ForegroundColor Yellow
        Write-Host "Build succeeded but skipped deployment." -ForegroundColor Yellow
        exit 2
    }
}

Copy-Item $built $target -Force

Write-Host ""
Write-Host "Deployed: $target"
Get-Item $target | Format-List FullName, LastWriteTime, Length
