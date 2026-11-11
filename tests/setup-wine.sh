#!/usr/bin/env bash
set -euo pipefail

# Run as root; prefix initialization and tests run as the runner user separately.
case "${1:-}" in
    9|11) version=$1 ;;
    *) echo 'Usage: sudo bash tests/setup-wine.sh 9|11' >&2; exit 2 ;;
esac

# shellcheck source=/dev/null
source /etc/os-release
if [[ $EUID != 0 || $ID != ubuntu || ( $VERSION_ID != 24.04 && $VERSION_ID != 26.04 ) ]]; then
    echo 'This installer requires root on Ubuntu 24.04 or 26.04.' >&2
    exit 1
fi
if [[ $version == 9 && $VERSION_ID != 24.04 ]]; then
    echo 'Wine 9 is only tested on Ubuntu 24.04.' >&2
    exit 1
fi

export DEBIAN_FRONTEND=noninteractive
# Ubuntu 26.04's WineHQ package uses the new WoW64 mode without i386 libraries.
if [[ $VERSION_ID == 24.04 ]]; then
    dpkg --add-architecture i386
fi
apt-get update
apt-get install -y --no-install-recommends ca-certificates curl cabextract unzip \
    xvfb xauth openbox x11-utils fonts-liberation fonts-wine

if [[ $version == 11 ]]; then
    mkdir -p /etc/apt/keyrings
    curl --fail --location --retry 3 https://dl.winehq.org/wine-builds/winehq.key \
        -o /etc/apt/keyrings/winehq.asc
    echo "deb [signed-by=/etc/apt/keyrings/winehq.asc] https://dl.winehq.org/wine-builds/ubuntu/ $VERSION_CODENAME main" \
        > /etc/apt/sources.list.d/overlayplugin-winehq.list
    apt-get update
    apt-get install -y --install-recommends "winehq-stable=11.0.0.0~${VERSION_CODENAME}-1"
else
    apt-get install -y --install-recommends wine=9.0~repack-4build3 \
        wine64=9.0~repack-4build3 wine32:i386=9.0~repack-4build3
fi

curl --fail --location --retry 3 \
    https://raw.githubusercontent.com/Winetricks/winetricks/20260125/src/winetricks \
    -o /usr/local/bin/winetricks
echo '431f82fc74000e6c864409f1d8fb495d696c03928808e3e8acffc45179312a7b  /usr/local/bin/winetricks' | sha256sum --check
chmod +x /usr/local/bin/winetricks
wine --version
[[ $(wine --version) == wine-${version}.* ]]
