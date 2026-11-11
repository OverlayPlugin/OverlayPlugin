import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

REPORTER = Path(__file__).with_name('report-smoke.py').resolve()


class SmokeReportTests(unittest.TestCase):
    def test_results_and_diagnostics_are_published_without_binary_state(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            results = root / 'out/smoke-results'
            state = results / 'Release/state'
            state.mkdir(parents=True)
            installer = results / 'installer/Release'
            installer.mkdir(parents=True)
            wine = root / 'out/wine-logs'
            wine.mkdir(parents=True)
            (results / 'summary.json').write_text(
                json.dumps({'passed': False, 'phases': ['Debug/render', 'Release/render', 'Release/websocket']})
            )
            (installer.parent / 'summary.json').write_text(
                json.dumps({'passed': True, 'phases': ['Release/install-fresh-and-reuse']})
            )
            for path in (
                results / 'Release/render.log',
                state / 'cef.log',
                installer / 'install-fresh.log',
                wine / 'setup.log',
            ):
                path.write_text('diagnostic: ' + path.name)
            (results / 'Release/websocket-result.json').write_text('{"passed": false}')
            (state / 'capture.png').write_bytes(b'binary screenshot')
            (state / 'libcef.dll').write_bytes(b'binary runtime')
            summary = root / 'job-summary.md'
            summary.write_text('Previous step\n')

            completed = subprocess.run(
                [sys.executable, str(REPORTER)],
                cwd=root,
                env={**os.environ, 'GITHUB_STEP_SUMMARY': str(summary)},
                capture_output=True,
                text=True,
                check=True,
                timeout=10,
            )
            markdown = summary.read_text()
            self.assertTrue(markdown.startswith('Previous step\n'))
            self.assertIn(
                '| Runtime | Failed | Debug rendering, Release rendering, WebSocket communication |', markdown
            )
            self.assertIn('| Installer | Passed | CEF installation and reuse |', markdown)
            self.assertNotIn('Debug/render', markdown)
            self.assertNotIn('Release/install-fresh-and-reuse', markdown)
            for name in ('render.log', 'cef.log', 'install-fresh.log', 'setup.log', 'websocket-result.json'):
                self.assertIn(name, completed.stdout)
            self.assertIn('::group::', completed.stdout)
            self.assertIn('::endgroup::', completed.stdout)
            self.assertNotIn('binary', completed.stdout)

    def test_missing_or_incomplete_results_do_not_hide_setup_logs(self):
        for incomplete in (False, True):
            with self.subTest(incomplete=incomplete), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                wine = root / 'out/wine-logs'
                wine.mkdir(parents=True)
                (wine / 'setup.log').write_text('runtime setup failed')
                if incomplete:
                    results = root / 'out/smoke-results'
                    results.mkdir()
                    (results / 'summary.json').write_text('{')
                summary = root / 'job-summary.md'
                completed = subprocess.run(
                    [sys.executable, str(REPORTER)],
                    cwd=root,
                    env={**os.environ, 'GITHUB_STEP_SUMMARY': str(summary), 'INSTALLER_OUTCOME': 'failure'},
                    capture_output=True,
                    text=True,
                    check=True,
                    timeout=10,
                )
                self.assertIn('runtime setup failed', completed.stdout)
                status = 'Incomplete results' if incomplete else 'No test results'
                self.assertIn(f'| Runtime | {status} | - |', summary.read_text())
                self.assertIn('| Installer | Setup failed | - |', summary.read_text())


if __name__ == '__main__':
    unittest.main()
