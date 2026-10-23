# Builds the plugin and copies it into the test server.
# Usage (in the plugin folder):  .\deploy.ps1
# Other server folder:           .\deploy.ps1 -ServerDir E:\mein\server\cs2
param(
    [string]$ServerDir = "D:\cs2server\cs2",
    [switch]$RebuildMaps
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

# Map geometry for the fog: convert every map that has no .actri yet (or after a game update with -RebuildMaps).
$maps = "de_ancient", "de_anubis", "de_dust2", "de_inferno", "de_mirage", "de_nuke", "de_overpass", "de_train", "de_vertigo", "cs_italy", "cs_office"
$mapTarget = Join-Path $target "maps"
New-Item -ItemType Directory -Force $mapTarget | Out-Null
$missing = $maps | Where-Object { $RebuildMaps -or -not (Test-Path (Join-Path $mapTarget "$_.actri")) }
if ($missing) {
    dotnet build "$PSScriptRoot\AntiCheat.MapTool" -c Release --nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Map tool build failed." }
    $tool = "$PSScriptRoot\AntiCheat.MapTool\bin\Release\net10.0\AntiCheat.MapTool.exe"
    foreach ($map in $missing) {
        $vpk = Join-Path $ServerDir "game\csgo\maps\$map.vpk"
        if (-not (Test-Path $vpk)) { Write-Warning "$map.vpk not found, skipped."; continue }
        & $tool $vpk (Join-Path $mapTarget "$map.actri") | Select-Object -First 1
    }
}
