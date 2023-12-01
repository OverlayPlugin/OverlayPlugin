param(
    [Alias('h')]
    [switch] $help = $False,

    [Alias('u')]
    [switch] ${update-hashes} = $False
)

if ($help) {
    Write-Host "Usage:"
    Write-Host "    powershell $($MyInvocation.MyCommand.Name) [--update-hashes|-u]"
    Write-Host "    powershell $($MyInvocation.MyCommand.Name) [--help|-h]"
    Exit 0
}

$base = (Get-Item $PSCommandPath).Directory.Parent.FullName

# We're using Newtonsoft.Json here instead of the native Convert*-Json methods
# because the option to format is extremely ugly before PS 6
# This is only for $deps, as that's the object that we serialize out in the end
# if changes are required

try {
    # If we're running from `Developer Powershell for VS ####`, then Newtonsoft is already available
    # If we're running from a normal Powershell environment, it's not. So detect that and load
    # the assembly from one of the sub-projects if needed
    $checkForNewtonsoft = [Newtonsoft.Json.JsonConvert]
} catch {
    $assemblyLocations = (Get-ChildItem -Path $base -Filter Newtonsoft.Json.dll -Recurse -ErrorAction SilentlyContinue -Force)

    if ($assemblyLocations.Length -lt 1) {
        throw "Could not load Newtonsoft.Json library"
    }

    Add-Type -LiteralPath $assemblyLocations[0].FullName
}

$JsonConvert = [Newtonsoft.Json.JsonConvert]
$jsonConvertOptions = New-Object Newtonsoft.Json.JsonSerializerSettings
$jsonConvertOptions.Formatting = 1

$dl_path = Join-Path $base ".deps_dl"

$deps_path = (Join-Path $base "DEPS.json")

if (-not (Test-Path $deps_path)) {
    throw "Could not find DEPS.json at $deps_path"
}

$cache_path = (Join-Path $base "DEPS.cache")

$deps = $JsonConvert::DeserializeObject((Get-Content $deps_path))

$cache = [PSCustomObject]@{}

if (Test-Path $cache_path) {
    $cache = Get-Content $cache_path | Out-String | ConvertFrom-Json
}

$old = (($cache.PSObject.Properties).Name)
$new = (($deps.Properties()).Name)

[array]$missing = $new | ? {$_ -notin $old}
[array]$obsolete = $old | ? {$_ -notin $new}
$outdated = @()

foreach ($key in $new) {
    $depsVal = $deps[$key]

    if (-not (Test-Path (Join-Path $base $depsVal["dest"].Value)))
    {
        $missing += $key
        continue
    }

    $cacheVal = $cache.$key
    
    if ($cacheVal -eq $null -or $depsVal -eq $null) {
        continue
    }

    $cacheHash = $cacheVal.hash
    $depsHash = $depsVal["hash"]
    
    if ($cacheHash -eq $null -or $depsHash -eq $null -or $depsHash.Count -ne 2) {
        continue
    }
    
    if ($cacheHash[1] -eq $null -or $depsHash[1].Value -eq $null) {
        continue
    }

    if ($cacheHash[1] -ne $depsHash[1].Value) {
        $outdated += $key
    }
}

if (Test-Path $dl_path) {
    Write-Host "Removing left overs..."
    Remove-Item $dl_path -Recurse -Force
    
    if (Test-Path $dl_path) {
        Throw "Failed to remove left overs from previous fetch_deps at $dl_path"
    }
}

mkdir $dl_path > $null

$updated_hash = $False

$toUpdate = $missing + $outdated

if ($toUpdate.Length -gt 0) {
    Write-Host "Fetching missing or outdated dependencies..."
    for ($i = 0; $i -lt $toUpdate.Length; ++$i) {
        $key = $toUpdate[$i]
        Write-Host "[$($i + 1)/$($toUpdate.Length)]: $key"

        $meta = $deps[$key]
        $dlname = Join-Path $dl_path (Split-Path $meta["url"].Value -Leaf)
        $dest = Join-Path $base $meta["dest"].Value

        Invoke-WebRequest -Uri $meta["url"].Value -OutFile $dlname

        if ($meta["hash"] -ne $null -and $meta["hash"].Count -eq 2) {
            Write-Host "Hashing..."

            $hash = (Get-FileHash $dlname -Algorithm $meta["hash"][0].Value).Hash.ToLower()

            if (${update-hashes}) {
                $updated_hash = $True
                $meta["hash"][1].Value = $hash
            } elseif ($hash -ne $meta["hash"][1].Value) {
                Throw "ERROR: $key failed the hash check.`nDownloaded hash: $hash`nExpected hash: $($meta["hash"][0].Value)"
            }
        }

        if (Test-Path $dest) {
            Remove-Item $dest -Recurse -Force
    
            if (Test-Path $dest) {
                Throw "Failed to remove files at $dest"
            }
        }

        Write-Host "Extracting..."

        if ($dlname.ToLower().EndsWith("zip")) {
            # This is a bit ugly but it lets us use the built-in Expand-Archive instead
            # of having to manually extract the zip archive
            $tmpdir = ($dlname + "_tmp")
            $tmpdir2 = ($dlname + "_tmp2")
            Expand-Archive $dlname $tmpdir
            for ($strip = 0; $strip -lt $meta["strip"].Value; ++$strip) {
                $toplevelFolder = (Get-Item (Join-Path $tmpdir "*"))[0].FullName
                mkdir $tmpdir2 > $null
                Move-Item (Join-Path $toplevelFolder "*") $tmpdir2
                Remove-Item $tmpdir -Recurse
                Move-Item $tmpdir2 $tmpdir
            }
            mkdir $dest > $null
            Move-Item (Join-Path $tmpdir "*") $dest
        }
    }
}

if ($obsolete.Length -gt 0) {
    Write-Host "Removing old dependencies..."

    for ($i = 0; $i -lt $obsolete.Length; ++$i) {
        $key = $obsolete[$i]
        Write-Host "[$($i + 1)/$($obsolete.Length)]: $key"
        
        $meta = $cache.$key
        $dest = Join-Path $base $meta.dest

        if (Test-Path $dest) {
            Remove-Item $dest -Recurse -Force
    
            if (Test-Path $dest) {
                Throw "Failed to remove files at $dest"
            }
        }
    }
}

if (($toUpdate + $obsolete).Length -eq 0) {
    Write-Host "Nothing to do."

    Exit 0
}

Write-Host "Saving dependency cache..."
$json = $JsonConvert::SerializeObject($deps, $jsonConvertOptions)
Set-Content -Path $cache_path -Value $json

if ($updated_hash -eq $True) {
    Write-Host "Updating hashes..."
    Set-Content -Path $deps_path -Value $json
}

Write-Host "Cleaning up..."
if (Test-Path $dl_path) {
    Remove-Item $dl_path -Recurse -Force

    if (Test-Path $dl_path) {
        Throw "Failed to remove files at $dl_path"
    }
}
