#!/usr/bin/env bash
set -euo pipefail

export WINEARCH=win64
export WINEPREFIX=${WINEPREFIX:-"$PWD/out/wine-prefix"}
export WINEDEBUG=${WINEDEBUG:--all}
export WINEDLLOVERRIDES='mscoree,mshtml='
export WINETRICKS_CACHE=${WINETRICKS_CACHE:-"$HOME/.cache/winetricks"}
# Wait for Wine's registry writes before the prefix is cached.
trap 'wineserver -k || true; timeout 30s wineserver -w' EXIT

wine --version
winetricks --version

# Winetricks expects VC++ 14.29.30156; aka.ms now redirects to a newer installer.
mkdir -p "$WINETRICKS_CACHE/vcrun2019"
while read -r architecture release checksum; do
    installer="$WINETRICKS_CACHE/vcrun2019/vc_redist.$architecture.exe"
    if ! echo "$checksum  $installer" | sha256sum --check --status; then
        curl --fail --location --retry 3 \
            "https://download.visualstudio.microsoft.com/download/pr/$release/$checksum/VC_redist.$architecture.exe" \
            -o "$installer"
    fi
    echo "$checksum  $installer" | sha256sum --check
done <<'INSTALLERS'
x86 5efdbcb7-d3bd-4432-a2fb-b267c386e2f3 49545CB0F6499C4A65E1E8D5033441EEEB4EDFAE465A68489A70832C6A4F6399
x64 453680ea-b88a-411f-80fd-5db37fdc9dbb 5D9999036F2B3A930F83B7FE3E2186B12E79AE7C007D538F52E3582E986A37C3
INSTALLERS

# Avoid forcing Wine's INF setup again when the installer check reuses the prefix.
timeout 120s wineboot -i
unset WINEDLLOVERRIDES
time timeout 900s winetricks -q dotnet48 vcrun2019 win10 nocrashdialog

openbox &
window_manager=$!
trap 'kill "$window_manager" 2>/dev/null || true; wineserver -k || true; timeout 30s wineserver -w' EXIT
python3 tests/run-smoke.py "$@"
