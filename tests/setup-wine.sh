#!/usr/bin/env bash
set -euo pipefail

# Run as root; prefix initialization and tests run as the runner user separately.
if [[ $# != 1 || ! ${1:-} =~ ^[1-9][0-9]*$ ]]; then
    echo 'Usage: sudo bash tests/setup-wine.sh <positive Wine major version>' >&2
    exit 2
fi
version=$1

# shellcheck source=/dev/null
source /etc/os-release
if [[ $EUID != 0 || $ID != ubuntu ]]; then
    echo 'This installer requires root on Ubuntu.' >&2
    exit 1
fi

export DEBIAN_FRONTEND=noninteractive
# Wine 9 and WineHQ packages for Ubuntu before 26.04 require i386 libraries.
if [[ $version == 9 ]] || dpkg --compare-versions "$VERSION_ID" lt 26.04; then
    dpkg --add-architecture i386
fi
apt-get update
apt-get install -y --no-install-recommends ca-certificates curl cabextract unzip \
    xvfb xauth openbox x11-utils fonts-liberation fonts-wine

if [[ $version != 9 ]]; then
    mkdir -p /etc/apt/keyrings
    curl --fail --location --retry 3 https://dl.winehq.org/wine-builds/winehq.key \
        -o /etc/apt/keyrings/winehq.asc
    echo "deb [signed-by=/etc/apt/keyrings/winehq.asc] https://dl.winehq.org/wine-builds/ubuntu/ $VERSION_CODENAME main" \
        > /etc/apt/sources.list.d/overlayplugin-winehq.list
    apt-get update
    apt-get install -y --install-recommends "winehq-stable=${version}.*"
else
    apt-get install -y --install-recommends "wine=${version}.*" \
        "wine64=${version}.*" "wine32:i386=${version}.*"
fi

curl --fail --location --retry 3 \
    https://raw.githubusercontent.com/Winetricks/winetricks/20260125/src/winetricks \
    -o /usr/local/bin/winetricks
echo '431f82fc74000e6c864409f1d8fb495d696c03928808e3e8acffc45179312a7b  /usr/local/bin/winetricks' | sha256sum --check
chmod +x /usr/local/bin/winetricks
wine --version
[[ $(wine --version) == wine-${version}.* ]]
