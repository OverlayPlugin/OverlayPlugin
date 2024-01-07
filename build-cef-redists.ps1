try {
    # This assumes Visual Studio 2019 is installed in C:. You might have to change this depending on your system.
    $VS_PATH = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community"

    $ENV:PATH = "$VS_PATH\MSBuild\Current\Bin;${ENV:PATH}";
    if (Test-Path "C:\Program Files\7-Zip\7z.exe") {
        $ENV:PATH = "C:\Program Files\7-Zip;${ENV:PATH}";
    }

    $OutDir = Join-Path $PWD out

    echo "==> Building PrintCEFDir to find CEF redist path..."

    dotnet build -c release -t:PrintCEFDir .\HtmlRenderer\HtmlRenderer.csproj
    if (-not $?) { exit 1 }

    $CEFDir = Join-Path ([System.IO.File]::ReadAllText("$OutDir\Release\CEFPath.txt")).Trim() CEF;

    echo "==> Building CEF archive from contents of $CEFDir..."

    Push-Location $CEFDir

    Write-Host $PWD

    $text = [System.IO.File]::ReadAllText("$CEFDir\README.txt");
    $regex = [regex]::New("CEF Version:\s*([0-9.]+)");
    $m = $regex.Match($text);

    if (-not $m) {
        echo "Error: Version number not found in CEF's README.txt!"
        exit 1
    }

    $version = $m.Groups[1]
    $archive = Join-Path $OutDir "CefSharp-$version-x64.7z"

    if (Test-Path $archive) { rm $archive }
    7z a $archive "-x!*.xml" "-x!*.pdb" .

    Pop-Location
} catch {
    Write-Error $Error[0]
}
