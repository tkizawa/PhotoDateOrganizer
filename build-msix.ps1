[CmdletBinding()]
param(
    [string]$Version = "1.0.0.0",
    [string]$Publisher = "",
    [string]$IdentityName = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
$projectFile = Join-Path $scriptDir "PhotoDateOrganizer.csproj"
$manifestTemplate = Join-Path $scriptDir "Package.appxmanifest"
$msixOutputDir = Join-Path $scriptDir "MSIX"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " PhotoDateOrganizer MSIX Package Builder for Microsoft Store" -ForegroundColor Cyan
Write-Host " Version: $Version" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

if (-not (Test-Path $msixOutputDir)) {
    New-Item -ItemType Directory -Path $msixOutputDir -Force | Out-Null
}

$sdkTools = Get-ChildItem -Path "$env:USERPROFILE\.nuget\packages\microsoft.windows.sdk.buildtools" -Filter "makeappx.exe" -Recurse | Where-Object { $_.FullName -like "*x64*" -or $_.FullName -like "*arm64*" } | Sort-Object FullName -Descending | Select-Object -First 1

if (-not $sdkTools) {
    throw "MakeAppx.exe not found in Microsoft.Windows.SDK.BuildTools."
}

$makeAppxExe = $sdkTools.FullName
Write-Host "MakeAppx: $makeAppxExe" -ForegroundColor Gray

function Build-Arch-Package {
    param(
        [string]$Arch,
        [string]$RuntimeId
    )

    Write-Host "`n>>> Building Release for $Arch ($RuntimeId)..." -ForegroundColor Yellow

    $publishDir = Join-Path $scriptDir "bin\Release\net10.0-windows10.0.19041.0\$RuntimeId\publish"
    
    if (Test-Path $publishDir) {
        Remove-Item -Path $publishDir -Recurse -Force
    }

    dotnet publish $projectFile -c Release -r $RuntimeId --self-contained true -p:PublishSingleFile=false -p:WindowsPackageType=None
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $Arch with exit code $LASTEXITCODE"
    }

    Write-Host "Packaging $Arch MSIX..." -ForegroundColor Yellow

    $manifestXml = [xml](Get-Content $manifestTemplate -Raw)
    
    $manifestXml.Package.Identity.SetAttribute("Version", $Version)
    $manifestXml.Package.Identity.SetAttribute("ProcessorArchitecture", $Arch)

    if ($Publisher) {
        $manifestXml.Package.Identity.SetAttribute("Publisher", $Publisher)
    }
    if ($IdentityName) {
        $manifestXml.Package.Identity.SetAttribute("Name", $IdentityName)
    }

    $destManifest = Join-Path $publishDir "AppxManifest.xml"
    $manifestXml.Save($destManifest)

    $assetsSrc = Join-Path $scriptDir "Assets"
    if (Test-Path $assetsSrc) {
        Copy-Item -Path $assetsSrc -Destination $publishDir -Recurse -Force
    }

    $msixFile = Join-Path $msixOutputDir "PhotoDateOrganizer_${Version}_${Arch}.msix"
    if (Test-Path $msixFile) {
        Remove-Item -Path $msixFile -Force
    }

    & $makeAppxExe pack /d $publishDir /p $msixFile /nv
    if ($LASTEXITCODE -ne 0) {
        throw "MakeAppx pack failed for $Arch with exit code $LASTEXITCODE"
    }

    Write-Host "Created: $msixFile" -ForegroundColor Green
}

Build-Arch-Package -Arch "x64" -RuntimeId "win-x64"
Build-Arch-Package -Arch "arm64" -RuntimeId "win-arm64"

$x64Msix = Join-Path $msixOutputDir "PhotoDateOrganizer_${Version}_x64.msix"
$arm64Msix = Join-Path $msixOutputDir "PhotoDateOrganizer_${Version}_arm64.msix"

Write-Host "`n>>> Creating combined MSIX Bundle (.msixbundle)..." -ForegroundColor Yellow

$bundleInputDir = Join-Path $msixOutputDir "bundle_temp"
if (Test-Path $bundleInputDir) {
    Remove-Item -Path $bundleInputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $bundleInputDir -Force | Out-Null

Copy-Item $x64Msix -Destination $bundleInputDir
Copy-Item $arm64Msix -Destination $bundleInputDir

$bundleOutputFile = Join-Path $msixOutputDir "PhotoDateOrganizer_${Version}_bundle.msixbundle"
if (Test-Path $bundleOutputFile) {
    Remove-Item -Path $bundleOutputFile -Force
}

& $makeAppxExe bundle /d $bundleInputDir /p $bundleOutputFile /bv $Version /o

Remove-Item -Path $bundleInputDir -Recurse -Force

if ($LASTEXITCODE -ne 0) {
    Write-Warning "Bundle creation failed. Individual .msix files are ready."
} else {
    Write-Host "Created Bundle: $bundleOutputFile" -ForegroundColor Green
}

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host " MSIX Packaging Complete!" -ForegroundColor Green
Write-Host " Output: $msixOutputDir" -ForegroundColor Green
Write-Host " - x64:   PhotoDateOrganizer_${Version}_x64.msix" -ForegroundColor White
Write-Host " - Arm64: PhotoDateOrganizer_${Version}_arm64.msix" -ForegroundColor White
if (Test-Path $bundleOutputFile) {
    Write-Host " - Bundle: PhotoDateOrganizer_${Version}_bundle.msixbundle" -ForegroundColor White
}
Write-Host "==========================================================" -ForegroundColor Cyan
