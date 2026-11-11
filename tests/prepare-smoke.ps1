$ErrorActionPreference = "Stop"
$base = Split-Path $PSScriptRoot -Parent
Push-Location $base
try {
    foreach ($configuration in @("Debug", "Release")) {
        $destination = Join-Path $base "out/smoke/$configuration"
        if (Test-Path $destination) {
            Remove-Item $destination -Recurse -Force
        }
        dotnet build tests/OverlayPlugin.SmokeTests/OverlayPlugin.SmokeTests.csproj -c $configuration
        if ($LASTEXITCODE -ne 0) {
            throw "Smoke host build failed: $configuration"
        }

        if ($configuration -eq "Release") {
            # Use the actual packaged layout, which excludes CefSharp assemblies.
            Copy-Item out/Release/OverlayPlugin/* $destination -Recurse -Force
        } else {
            Copy-Item out/Debug/libs "$destination/libs" -Recurse -Force
        }
        Copy-Item "Thirdparty/ACT/Advanced Combat Tracker.exe" $destination
        Copy-Item Thirdparty/FFXIV_ACT_Plugin/SDK/*.dll "$destination/libs" -Force
        Copy-Item tests/fixtures "$destination/fixtures" -Recurse -Force
    }

    # Use the same native CEF resources as the build, without a published archive.
    $cef = Join-Path $base "out/smoke/cef"
    if (Test-Path $cef) {
        Remove-Item $cef -Recurse -Force
    }
    dotnet msbuild HtmlRenderer/HtmlRenderer.csproj -t:CefRedist64CopyResources -p:Configuration=Debug "-p:CefRedist64TargetDir=$cef"
    if ($LASTEXITCODE -ne 0) {
        throw "CEF bundle preparation failed"
    }
    Copy-Item out/Debug/libs/CefSharp* $cef -Force
} finally {
    Pop-Location
}
