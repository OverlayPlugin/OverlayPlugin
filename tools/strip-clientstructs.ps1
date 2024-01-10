param(
    [Parameter(Mandatory=$false)]
    [Alias('p')]
    [ValidateScript({
        if (-not ($_ | Test-Path)) {
            throw "`$exePath does not exist: $_"
        }
        if (-not ($_ | Test-Path -PathType Leaf)) {
            throw "`$exePath must be a file"
        }
        if ($_ -notmatch "\.exe") {
            throw "`$exePath must be an exe file"
        }
        return $true 
    })]
    [System.IO.FileInfo]$exePath
)

# cd to project root if we're not already there
$base = (Get-Item $PSCommandPath).Directory.Parent.FullName
Push-Location $base

if (Test-Path "OverlayPlugin.Core\Thirdparty\FFXIVClientStructs\Base") {
    echo "==> Preparing FFXIVClientStructs..."
    
    if ($exePath -eq $null) {
        echo "==> Building StripFFXIVClientStructs..."
        # There's probably a better way to target just StripFFXIVClientStructs for build but `dotnet` was insisting on
        # building all associated projects even when passing the `-t:StripFFXIVClientStructs` flag through to msbuild
        dotnet publish -v quiet -c release -a x64 ".\tools\StripFFXIVClientStructs\StripFFXIVClientStructs\StripFFXIVClientStructs.csproj"
    }

    cd OverlayPlugin.Core\Thirdparty\FFXIVClientStructs\Base
    
    if ($exePath -eq $null) {
        $exePath = (Join-Path $base "tools\StripFFXIVClientStructs\StripFFXIVClientStructs\bin\Release\StripFFXIVClientStructs.exe")
    }

    # Reassign here to force relative paths to resolve, otherwise `Invoke-Expression` is unhappy
    $exePath = $exePath.FullName

    # Fix code to compile against .NET 4.8, remove partial funcs and helper funcs, we only want the struct layouts themselves
    gci * | foreach-object {
        $ns = $_.name

        echo "==> Stripping FFXIVClientStructs for namespace $ns..."

        Invoke-Expression "$exePath $ns .\$ns ..\Transformed\$ns"
    }

    cd ..\..\..\..
}

Pop-Location
