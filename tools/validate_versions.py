import sys
import os.path
import re

BASE_DIR = os.path.join(os.path.dirname(__file__), '..')
PROJECTS = ('HtmlRenderer', 'OverlayPlugin', 'OverlayPlugin.Common', 'OverlayPlugin.Core', 'OverlayPlugin.Updater')

# [assembly: AssemblyVersion("0.9.0.0")]
ASM_VER_RE = re.compile(r'\[assembly: AssemblyVersion\("([0-9]+\.[0-9]+\.[0-9]+)\.[0-9]+"\)\]')

last = None
for project in PROJECTS:
    path = os.path.join(BASE_DIR, project, 'Properties', 'AssemblyInfo.cs')

    with open(path, 'r', encoding='utf8') as f:
        for line in f:
            m = ASM_VER_RE.search(line)
            if not m:
                continue
            if last and m.group(0) != last:
                print('Found mismatching versions!')
                sys.exit(1)
            last = m.group(0)

print('Versions in sync!')
