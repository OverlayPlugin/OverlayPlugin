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

$CEFRedistBaseDir = ([System.IO.File]::ReadAllText("$OutDir\Release\cef.redist.x64.Path.txt")).Trim();
$CEFRedistDir = [IO.Path]::Combine($CEFRedistBaseDir, 'CEF', 'win-x64');
$CEFRuntimeDir = [IO.Path]::Combine($CEFRedistBaseDir, 'runtimes', 'win-x64', 'native');
$CEFSharpCommonDir = Join-Path ([System.IO.File]::ReadAllText("$OutDir\Release\CefSharp.Common.Path.txt")).Trim() "lib\net462";
$CEFSharpCommonDepsDir = Join-Path ([System.IO.File]::ReadAllText("$OutDir\Release\CefSharp.Common.Path.txt")).Trim() "CefSharp\x64";
$CEFSharpOffscreenDir = Join-Path ([System.IO.File]::ReadAllText("$OutDir\Release\CefSharp.OffScreen.Path.txt")).Trim() "lib\net462";

echo "==> Building CEF archive from contents of:"
echo "`$CEFRedistDir = $CEFRedistDir"
echo "`$CEFRuntimeDir = $CEFRuntimeDir"
echo "`$CEFSharpCommonDir = $CEFSharpCommonDir"
echo "`$CEFSharpCommonDepsDir = $CEFSharpCommonDepsDir"
echo "`$CEFSharpOffscreenDir = $CEFSharpOffscreenDir"

if (!$CEFRedistDir -or !$CEFRuntimeDir -or !$CEFSharpCommonDir -or !$CEFSharpCommonDepsDir -or !$CEFSharpOffscreenDir) { exit 1 }

Write-Host $PWD

$text = [System.IO.File]::ReadAllText("$CEFRedistBaseDir\README.txt");
$regex = [regex]::New("CEF Version:\s*([0-9.]+)");
$m = $regex.Match($text);

if (-not $m) {
    echo "Error: Version number not found in CEF's README.txt!"
    exit 1
}

$version = $m.Groups[1]
$archive = Join-Path $OutDir "CefSharp-$version-x64.7z"

if (Test-Path $archive) { rm $archive }
7z a $archive "-x!*.xml" "-x!*.pdb" "$CEFRedistDir\*" "$CEFRuntimeDir\*" "$CEFSharpCommonDir\*" "$CEFSharpOffscreenDir\*" "$CEFSharpCommonDepsDir\*"

Pop-Location