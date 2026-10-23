# Builds the plugin and copies it into the test server.
# Usage (in the plugin folder):  .\deploy.ps1
# Other server folder:           .\deploy.ps1 -ServerDir E:\mein\server\cs2
param(
    [string]$ServerDir = "D:\cs2server\cs2"
)

$ErrorActionPreference = "Stop"
$target = Join-Path $ServerDir "game\csgo\addons\counterstrikesharp\plugins\AntiCheat"
if (-not (Test-Path (Join-Path $ServerDir "game\csgo\addons\counterstrikesharp"))) {
    throw "CounterStrikeSharp not found in $ServerDir. See docs/server-setup.md."
}

dotnet build "$PSScriptRoot\AntiCheat.CounterStrikeSharp" -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

New-Item -ItemType Directory -Force $target | Out-Null
Copy-Item "$PSScriptRoot\AntiCheat.CounterStrikeSharp\bin\Release\net10.0\*" $target -Force
Write-Host "Plugin copied to $target"
