import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

REPORTER = Path(__file__).with_name('report-smoke.py').resolve()


class SmokeReportTests(unittest.TestCase):
    def test_summary_is_appended_with_readable_phase_names(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            results = root / 'out/smoke-results'
            installer = results / 'installer'
            installer.mkdir(parents=True)
            (results / 'summary.json').write_text(
                json.dumps({'passed': False, 'phases': ['Debug/render', 'Release/render', 'Release/websocket']})
            )
            (installer / 'summary.json').write_text(
                json.dumps({'passed': True, 'phases': ['Release/install-fresh-and-reuse']})
            )
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
            self.assertEqual(completed.stdout.strip(), markdown.removeprefix('Previous step\n').strip())

    def test_missing_or_incomplete_results_are_reported(self):
        for incomplete in (False, True):
            with self.subTest(incomplete=incomplete), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                if incomplete:
                    results = root / 'out/smoke-results'
                    results.mkdir(parents=True)
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
                self.assertIn('Runtime smoke tests', completed.stdout)
                status = 'Incomplete results' if incomplete else 'No test results'
                self.assertIn(f'| Runtime | {status} | - |', summary.read_text())
                self.assertIn('| Installer | Setup failed | - |', summary.read_text())


if __name__ == '__main__':
    unittest.main()
