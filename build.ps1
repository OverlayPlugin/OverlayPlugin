param (
    [switch]$ci = $false
)

function Try-Fetch-Deps {
    param ([String]$description)
    echo "Dependency '$description' was not found, running ``tools/fetch_deps.ps1` to fetch any missing dependencies"
    .\tools\fetch_deps.ps1
    if ($LASTEXITCODE -ne 0) {
        echo 'Error running fetch_deps.ps1'
        exit 1
    }
    else {
        echo 'Fetched deps successfully'
    }
}

try {
    # This assumes Visual Studio 2022 is installed in C:. You might have to change this depending on your system.
    $DEFAULT_VS_PATH = "C:\Program Files\Microsoft Visual Studio\2022\Community"

    $DEFAULT_VSWHERE_PATH = "${Env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if ( -not (Test-Path "$DEFAULT_VSWHERE_PATH")) {
        echo "Unable to find vswhere.exe, defauling to $DEFAULT_VS_PATH value in build.ps1."
        if ( -not (Test-Path "$DEFAULT_VS_PATH")) {
            echo "Error: DEFAULT_VS_PATH isn't set correctly! Update the variable in build.ps1 for your system."
            exit 1
        }
        $VS_PATH = $DEFAULT_VS_PATH
    } else {
        $VS_PATH = & "$DEFAULT_VSWHERE_PATH" -latest -property installationPath
    }

    if ( -not (Test-Path "Thirdparty\ACT\Advanced Combat Tracker.exe" )) {
        Try-Fetch-Deps -description "Advanced Combat Tracker.exe"
    }


    if ( -not (Test-Path "Thirdparty\FFXIV_ACT_Plugin\SDK\FFXIV_ACT_Plugin.Common.dll" )) {
        Try-Fetch-Deps -description "FFXIV_ACT_Plugin.Common.dll"
    }

    if ( -not (Test-Path "OverlayPlugin.Core\Thirdparty\FFXIVClientStructs\Base\Global" )) {
        Try-Fetch-Deps -description "FFXIVClientStructs"
    }

    $ENV:PATH = "$VS_PATH\MSBuild\Current\Bin;${ENV:PATH}";
    if (Test-Path "C:\Program Files\7-Zip\7z.exe") {
        $ENV:PATH = "C:\Program Files\7-Zip;${ENV:PATH}";
    }

    .\tools\strip-clientstructs.ps1

    if ($ci) {
        echo "==> Continuous integration flag set. Building Debug..."
        dotnet publish -v quiet -c debug
        
        if (-not $?) { exit 1 }
    }

    echo "==> Building..."

    dotnet publish -v quiet -c release
    
    if (-not $?) { exit 1 }

    echo "==> Building archive..."

    cd out\Release

    if (Test-Path OverlayPlugin) { rm -Recurse OverlayPlugin }
    mkdir OverlayPlugin\libs

    cp @("OverlayPlugin.dll", "OverlayPlugin.dll.config", "README.md", "LICENSE.txt") OverlayPlugin
    cp -Recurse libs\resources OverlayPlugin
    cp -Recurse libs\*.dll OverlayPlugin\libs
    del OverlayPlugin\libs\CefSharp.*

    # Translations
    cp -Recurse @("de-DE", "fr-FR", "ja-JP", "ko-KR", "zh-CN") OverlayPlugin
    cp -Recurse @("libs\de-DE", "libs\fr-FR", "libs\ja-JP", "libs\ko-KR", "libs\zh-CN") OverlayPlugin\libs


    [xml]$csprojcontents = Get-Content -Path "$PWD\..\..\Directory.Build.props";
    $version = $csprojcontents.Project.PropertyGroup.AssemblyVersion;
    $version = ($version | Out-String).Trim()
    $archive = "..\OverlayPlugin-$version.7z"

    if (Test-Path $archive) { rm $archive }
    cd OverlayPlugin
    7z a ..\$archive .
    cd ..

    $archive = "..\OverlayPlugin-$version.zip"

    if (Test-Path $archive) { rm $archive }
    7z a $archive OverlayPlugin

    cd ..\..
} catch {
    Write-Error $Error[0]
}
