param (
    [switch]$ci = $false
)

function Try-Fetch-Deps {
    param ([String]$description)
    echo "Dependency '$description' was not found, running `tools/fetch_deps.py` to fetch any missing dependencies"
    if ((Get-Command "python" -ErrorAction SilentlyContinue) -eq $null)
    {
        Write-Host "python does not appear to be in your PATH. Please fix this or manually run tools\fetch_deps.py"
        exit 1
    } 
    python tools\fetch_deps.py
    if ($LASTEXITCODE -ne 0) {
        echo 'Error running fetch_deps.py'
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

    if ( -not (Test-Path "OverlayPlugin.Core\Thirdparty\FFXIVClientStructs\Global" )) {
        Try-Fetch-Deps -description "FFXIVClientStructs"
    }

    $ENV:PATH = "$VS_PATH\MSBuild\Current\Bin;${ENV:PATH}";
    if (Test-Path "C:\Program Files\7-Zip\7z.exe") {
        $ENV:PATH = "C:\Program Files\7-Zip;${ENV:PATH}";
    }

    if (Test-Path "OverlayPlugin.Core\Thirdparty\FFXIVClientStructs") {
        echo "==> Preparing FFXIVClientStructs..."
        
        echo "==> Building StripFFXIVClientStructs..."
        msbuild -p:Configuration=Release -p:Platform=x64 "OverlayPlugin.sln" -t:StripFFXIVClientStructs -restore:True

        cd OverlayPlugin.Core\Thirdparty\FFXIVClientStructs

        $globalUsings = "using System.Runtime.InteropServices;"`
        +"`nusing FFXIVClientStructs.__NSREPLACE__.STD;"`
        +"`nusing FFXIVClientStructs.__NSREPLACE__.FFXIV.Client.Graphics;"`
        +"`nusing FFXIVClientStructs.__NSREPLACE__.FFXIV.Common.Math;"

        # Fix code to compile against .NET 4.8, remove partial funcs and helper funcs, we only want the struct layouts themselves
        gci * | foreach-object {
            $ns = $_.name

            if (-not (Test-Path $ns\FFXIVClientStructs\GlobalUsings.cs)) {
                # return
            }

            echo "==> Stripping FFXIVClientStructs for namespace $ns..."

            ..\..\..\tools\StripFFXIVClientStructs\StripFFXIVClientStructs\bin\Release\netcoreapp3.1\StripFFXIVClientStructs.exe .

            # Delete files we don't need
            rm -ErrorAction SilentlyContinue -r $ns\*.csproj
            rm -ErrorAction SilentlyContinue -r $ns\*.sln
            rm -ErrorAction SilentlyContinue -r $ns\FFXIVClientStructs\GlobalUsings.cs
            rm -ErrorAction SilentlyContinue -r $ns\FFXIVClientStructs\AssemblyAttributes.cs
            rm -ErrorAction SilentlyContinue -r $ns\FFXIVClientStructs\Interop
            rm -ErrorAction SilentlyContinue -r $ns\FFXIVClientStructs\Attributes
            rm -ErrorAction SilentlyContinue -r $ns\FFXIVClientStructs\Havok
            rm -ErrorAction SilentlyContinue -r $ns\FFXIVClientStructs.InteropSourceGenerators
            rm -ErrorAction SilentlyContinue -r $ns\FFXIVClientStructs.ResolverTester
            rm -ErrorAction SilentlyContinue -r $ns\FFXIVClientStructs\STD\Pair.cs
            rm -ErrorAction SilentlyContinue -r $ns\ida\CExporter

            gci -r $ns\*.cs |
                foreach-object {
                    $a = $_.fullname;
                    $b = ( get-content -Raw $a ) `
                    -replace '(?sm)using FFXIVClientStructs.Havok;','' `
                    -replace ('(?sm)namespace FFXIVClientStructs((?:\.FFXIV.*?|\.STD.*?|\.Havok.*?|));([^\x00]*\r?\n\})\r?\n?'),(($globalUsings -replace '__NSREPLACE__',$ns)+"`nnamespace FFXIVClientStructs."+$ns+'$1 {$2}') `
                    -replace ('(?sm)using FFXIVClientStructs((?:\.FFXIV.*?|\.STD.*?|));'),("using FFXIVClientStructs."+$ns+'$1;') `
                    -replace '^namespace [^;]+;$','' `
                    -replace '(?sm)\r?\n[ \t]*\[\]\r?\n',"`n" `
                    -replace '(?sm)\r?\n([ \t]*)\[\] \[',"$1[" `
                    -replace 'delegate\*<[^>]*>','void*' `
                    -replace '(?sm)Pointer<[^>]+>','void*' `
                    -replace '(StdVector|StdDeque|StdMap|StdSet|AtkLinkedList|CVector)<[^;]+>[* ]','$1 ' `
                    -replace '(?sm)hk[^ ]+\*','void*' `
                    -replace '(?sm)using CategoryMap = .*?;','' `
                    -replace '(?sm)\[FieldOffset\(0x0\)\] public CategoryMap\* MainMap;','' `
                    -replace '([^ ]+) : IEquatable<\1>, IFormattable','$1' `
                    -replace '([^ ]+) : IEquatable<\1>','$1' `
                    -replace 'MathF.PI','(float)System.Math.PI'

                    $b | set-content $a
                }

            # Clean up the STD namespace objects
            (get-content ..\..\MemoryProcessors\AtkStage\FFXIVClientStructs\Templates\STD.Map.cs) -replace '__NAMESPACE__',$ns | set-content $ns\FFXIVClientStructs\STD\Map.cs
            (get-content ..\..\MemoryProcessors\AtkStage\FFXIVClientStructs\Templates\STD.Deque.cs) -replace '__NAMESPACE__',$ns | set-content $ns\FFXIVClientStructs\STD\Deque.cs
            (get-content ..\..\MemoryProcessors\AtkStage\FFXIVClientStructs\Templates\STD.Vector.cs) -replace '__NAMESPACE__',$ns | set-content $ns\FFXIVClientStructs\STD\Vector.cs
            (get-content ..\..\MemoryProcessors\AtkStage\FFXIVClientStructs\Templates\STD.Set.cs) -replace '__NAMESPACE__',$ns | set-content $ns\FFXIVClientStructs\STD\Set.cs
            (get-content ..\..\MemoryProcessors\AtkStage\FFXIVClientStructs\Templates\FFXIV.Component.GUI.AtkLinkedList.cs) -replace '__NAMESPACE__',$ns | set-content $ns\FFXIVClientStructs\FFXIV\Component\GUI\AtkLinkedList.cs
            }

        cd ..\..\..
    }

    if ( -not (Test-Path .\OverlayPlugin.Updater\Resources\libcurl.dll)) {
        echo "==> Building cURL..."

        mkdir .\OverlayPlugin.Updater\Resources
        cd Thirdparty\curl\winbuild

        echo "@call `"$VS_PATH\VC\Auxiliary\Build\vcvarsall.bat`" amd64"           | Out-File -Encoding ascii tmp_build.bat
        echo "nmake /f Makefile.vc mode=dll VC=16 GEN_PDB=no DEBUG=no MACHINE=x64" | Out-File -Encoding ascii -Append tmp_build.bat
        echo "@call `"$VS_PATH\VC\Auxiliary\Build\vcvarsall.bat`" x86"             | Out-File -Encoding ascii -Append tmp_build.bat
        echo "nmake /f Makefile.vc mode=dll VC=16 GEN_PDB=no DEBUG=no MACHINE=x86" | Out-File -Encoding ascii -Append tmp_build.bat

        cmd "/c" "tmp_build.bat"
        sleep 3
        del tmp_build.bat

        cd ..\builds
        copy .\libcurl-vc16-x64-release-dll-ipv6-sspi-winssl\bin\libcurl.dll ..\..\..\OverlayPlugin.Updater\Resources\libcurl-x64.dll
        copy .\libcurl-vc16-x86-release-dll-ipv6-sspi-winssl\bin\libcurl.dll ..\..\..\OverlayPlugin.Updater\Resources\libcurl.dll

        cd ..\..\..
    }

    if ($ci) {
        echo "==> Continuous integration flag set. Building Debug..."
        msbuild -p:Configuration=Debug -p:Platform=x64 "OverlayPlugin.sln" -t:Restore
        msbuild -p:Configuration=Debug -p:Platform=x64 "OverlayPlugin.sln"    
    }

    echo "==> Building..."

    msbuild -p:Configuration=Release -p:Platform=x64 "OverlayPlugin.sln" -t:Restore
    msbuild -p:Configuration=Release -p:Platform=x64 "OverlayPlugin.sln"
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


    $text = [System.IO.File]::ReadAllText("$PWD\..\..\OverlayPlugin\Properties\AssemblyInfo.cs");
    $regex = [regex]::New('\[assembly: AssemblyVersion\("([0-9]+\.[0-9]+\.[0-9]+)\.[0-9]+"\)');
    $m = $regex.Match($text);

    if (-not $m) {
        echo "Error: Version number not found in the AssemblyInfo.cs!"
        exit 1
    }

    $version = $m.Groups[1]
    $archive = "..\OverlayPlugin-$version.7z"

    if (Test-Path $archive) { rm $archive }
    cd OverlayPlugin
    7z a ..\$archive .
    cd ..

    $archive = "..\OverlayPlugin-$version.zip"

    if (Test-Path $archive) { rm $archive }
    7z a $archive OverlayPlugin
} catch {
    Write-Error $Error[0]
}
