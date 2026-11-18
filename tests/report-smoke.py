"""Publish smoke test results to the job summary."""

import json
import os
from pathlib import Path

PHASE_NAMES = {
    'Debug/render': 'Debug rendering',
    'Release/render': 'Release rendering',
    'Release/websocket': 'WebSocket communication',
    'Release/install-fresh-and-reuse': 'CEF installation and reuse',
}


def main():
    results = Path('out/smoke-results')
    summary = ['## Runtime smoke tests', '', '| Check | Result | Completed phases |', '| --- | --- | --- |']
    for name, path in (('Runtime', results / 'summary.json'), ('Installer', results / 'installer/summary.json')):
        if name == 'Installer' and os.environ.get('INSTALLER_OUTCOME') == 'skipped':
            continue
        if path.is_file():
            try:
                result = json.loads(path.read_text(encoding='utf-8'))
                status = 'Passed' if result['passed'] else 'Failed'
                phases = ', '.join(PHASE_NAMES.get(phase, phase) for phase in result['phases']) or '-'
            except json.JSONDecodeError:
                status, phases = 'Incomplete results', '-'
            summary.append(f'| {name} | {status} | {phases} |')
        elif name == 'Runtime':
            summary.append('| Runtime | No test results | - |')
        elif os.environ.get('INSTALLER_OUTCOME') not in (None, '', 'skipped'):
            outcome = os.environ['INSTALLER_OUTCOME']
            status = {'failure': 'Setup failed', 'cancelled': 'Cancelled'}.get(outcome, 'No test results')
            summary.append(f'| Installer | {status} | - |')

    markdown = '\n'.join(summary) + '\n'
    print(markdown, flush=True)
    if os.environ.get('GITHUB_STEP_SUMMARY'):
        with open(os.environ['GITHUB_STEP_SUMMARY'], 'a', encoding='utf-8') as output:
            output.write(markdown)


if __name__ == '__main__':
    main()
