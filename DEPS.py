# If you're familiar with Chromium or other Google projects:
#   Sorry, this file actually has nothing to do with gclient.
#   I just borrowed the basic concept and naming scheme.

deps = {
    'ACT': {
        'url': 'https://github.com/EQAditu/AdvancedCombatTracker/releases/download/3.4.9.271/ACTv3.zip',
        'dest': 'Thirdparty/ACT',
        'strip': 0,
        'hash': ['sha256', 'adf13a38d0938ce90f8e674f8365b227d933b91636ddf72b26c85702f6e3b808'],
    },
    'FFXIV_ACT_Plugin': {
        'url': 'https://github.com/ravahn/FFXIV_ACT_Plugin/raw/master/Releases/FFXIV_ACT_Plugin_SDK_2.0.6.1.zip',
        'dest': 'Thirdparty/FFXIV_ACT_Plugin',
        'strip': 0,
        'hash': ['sha256', '55eed8660848273b285e2f128a65e6822f086c885942be69691baf5c530a7cd6'],
    },
    'curl': {
        'url': 'https://curl.haxx.se/download/curl-7.70.0.tar.xz',
        'dest': 'Thirdparty/curl',
        'strip': 1,
        'hash': ['sha256', '7f84eb2f9409591cb0132cb4b5975be7680eb6daa73d92711880569f86db355b'],
    },
}
