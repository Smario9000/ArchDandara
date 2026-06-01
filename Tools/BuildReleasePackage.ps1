param(
    [string]$Configuration = "Release",
    [string]$Platform = "x86",
    [string]$PackageName = "ArchDandara"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$distRoot = Join-Path $repoRoot "dist"
$packageRoot = Join-Path $distRoot $PackageName
$modsRoot = Join-Path $packageRoot "Mods"
$toolsRoot = Join-Path $packageRoot "UserData\ArchDandaraData\Tools"
$apWorldRoot = Join-Path $packageRoot "APWorld"
$assetsRoot = Join-Path $packageRoot "Assets"
$tempRoot = Join-Path $distRoot "_temp"

function Copy-RequiredFile {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (!(Test-Path -LiteralPath $Source)) {
        throw "Missing required release file: $Source"
    }

    $destinationFolder = Split-Path -Parent $Destination
    if (!(Test-Path -LiteralPath $destinationFolder)) {
        New-Item -ItemType Directory -Path $destinationFolder | Out-Null
    }

    Copy-Item -LiteralPath $Source -Destination $Destination -Force
}

function Remove-PythonCache {
    param([string]$Root)

    Get-ChildItem -LiteralPath $Root -Recurse -Directory -Filter "__pycache__" -ErrorAction SilentlyContinue |
        Remove-Item -Recurse -Force
    Get-ChildItem -LiteralPath $Root -Recurse -File -Filter "*.pyc" -ErrorAction SilentlyContinue |
        Remove-Item -Force
}

Write-Host "Building ArchDandara mod..."
dotnet build (Join-Path $repoRoot "ArchDandara.csproj") -p:Configuration=$Configuration -p:Platform=$Platform

Write-Host "Building Archipelago WSS bridge..."
dotnet build (Join-Path $repoRoot "Tools\ArchipelagoWssBridge\ArchipelagoWssBridge.csproj") -p:Configuration=$Configuration

if (Test-Path -LiteralPath $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}

if (Test-Path -LiteralPath $tempRoot) {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $modsRoot, $toolsRoot, $apWorldRoot, $assetsRoot, $tempRoot | Out-Null

$modBuildRoot = Join-Path $repoRoot "bin\$Platform\$Configuration"
$bridgeBuildRoot = Join-Path $repoRoot "Tools\ArchipelagoWssBridge\bin\$Configuration"
$apNet35Root = Join-Path $repoRoot "packages\Archipelago.MultiClient.Net.6.7.1\lib\net35"
$harmonyNet35Root = Join-Path $repoRoot "packages\HarmonyX.2.16.1\lib\net35"

Write-Host "Copying mod files..."
Copy-RequiredFile (Join-Path $modBuildRoot "ArchDandara.dll") (Join-Path $modsRoot "ArchDandara.dll")
Copy-RequiredFile (Join-Path $harmonyNet35Root "0Harmony.dll") (Join-Path $modsRoot "0Harmony.dll")
Copy-RequiredFile (Join-Path $apNet35Root "Archipelago.MultiClient.Net.dll") (Join-Path $modsRoot "Archipelago.MultiClient.Net.dll")
Copy-RequiredFile (Join-Path $apNet35Root "Newtonsoft.Json.dll") (Join-Path $modsRoot "Newtonsoft.Json.dll")
Copy-RequiredFile (Join-Path $apNet35Root "websocket-sharp.dll") (Join-Path $modsRoot "websocket-sharp.dll")

Write-Host "Copying bridge files..."
Copy-RequiredFile (Join-Path $bridgeBuildRoot "ArchipelagoWssBridge.exe") (Join-Path $toolsRoot "ArchipelagoWssBridge.exe")
Copy-RequiredFile (Join-Path $bridgeBuildRoot "websocket-sharp.dll") (Join-Path $toolsRoot "websocket-sharp.dll")

Write-Host "Copying APWorld source and assets..."
Copy-Item -LiteralPath (Join-Path $repoRoot "DandaraAPWorld\dandara") -Destination (Join-Path $apWorldRoot "dandara") -Recurse -Force
Remove-PythonCache (Join-Path $apWorldRoot "dandara")
Copy-RequiredFile (Join-Path $repoRoot "DandaraAPWorld\Dandara.yaml") (Join-Path $apWorldRoot "Dandara.yaml")
Copy-RequiredFile (Join-Path $repoRoot "Banner.png") (Join-Path $assetsRoot "Banner.png")
Copy-RequiredFile (Join-Path $repoRoot "INSTALL.md") (Join-Path $packageRoot "INSTALL.md")
Copy-RequiredFile (Join-Path $repoRoot "README.md") (Join-Path $packageRoot "README.md")

Write-Host "Creating APWorld package..."
$apworldTempPackage = Join-Path $tempRoot "apworld"
New-Item -ItemType Directory -Path $apworldTempPackage | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot "DandaraAPWorld\dandara") -Destination (Join-Path $apworldTempPackage "dandara") -Recurse -Force
Remove-PythonCache (Join-Path $apworldTempPackage "dandara")
$apworldPath = Join-Path $distRoot "ArchDandara.apworld"
$apworldZipPath = Join-Path $distRoot "ArchDandara.apworld.zip"
if (Test-Path -LiteralPath $apworldPath) {
    Remove-Item -LiteralPath $apworldPath -Force
}
if (Test-Path -LiteralPath $apworldZipPath) {
    Remove-Item -LiteralPath $apworldZipPath -Force
}
Compress-Archive -Path (Join-Path $apworldTempPackage "dandara") -DestinationPath $apworldZipPath -Force
Move-Item -LiteralPath $apworldZipPath -Destination $apworldPath -Force

Write-Host "Creating release zip..."
$zipPath = Join-Path $distRoot "$PackageName-release.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $zipPath -Force

Remove-Item -LiteralPath $tempRoot -Recurse -Force

Write-Host "Release package created:"
Write-Host "  $zipPath"
Write-Host "  $apworldPath"
