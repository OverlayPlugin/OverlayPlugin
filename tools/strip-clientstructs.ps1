# cd to project root if we're not already there
$base = (Get-Item $PSCommandPath).Directory.Parent.FullName
Push-Location $base

if (Test-Path "OverlayPlugin.Core\Thirdparty\FFXIVClientStructs\Base") {
    echo "==> Preparing FFXIVClientStructs..."
    
    echo "==> Building StripFFXIVClientStructs..."
    # There's probably a better way to target just StripFFXIVClientStructs for build but `dotnet` was insisting on
    # building all associated projects even when passing the `-t:StripFFXIVClientStructs` flag through to msbuild
    dotnet publish -v quiet -c release -a x64 ".\tools\StripFFXIVClientStructs\StripFFXIVClientStructs\StripFFXIVClientStructs.csproj"

    cd OverlayPlugin.Core\Thirdparty\FFXIVClientStructs\Base

    # Fix code to compile against .NET 4.8, remove partial funcs and helper funcs, we only want the struct layouts themselves
    gci * | foreach-object {
        $ns = $_.name

        echo "==> Stripping FFXIVClientStructs for namespace $ns..."

        ..\..\..\..\tools\StripFFXIVClientStructs\StripFFXIVClientStructs\bin\Release\StripFFXIVClientStructs.exe $ns .\$ns ..\Transformed\$ns
    }

    cd ..\..\..\..
}

Pop-Location
