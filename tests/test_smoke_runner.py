import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest import mock

spec = importlib.util.spec_from_file_location('runner', Path(__file__).with_name('run-smoke.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class SmokeRunnerTests(unittest.TestCase):
    def test_nonzero_host_fails_and_preserves_log(self):
        with tempfile.TemporaryDirectory() as directory:
            log = Path(directory) / 'host.log'
            with self.assertRaisesRegex(RuntimeError, 'Exit code 7'):
                runner.run_process([sys.executable, '-c', "print('failure evidence'); exit(7)"], log)
            self.assertIn('failure evidence', log.read_text())

    def test_hung_host_fails(self):
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(RuntimeError, 'Timed out'):
                runner.run_process(
                    [sys.executable, '-c', 'import time; time.sleep(60)'], Path(directory) / 'host.log', timeout=0.1
                )

    def test_partial_browser_exchange_fails(self):
        for result in (
            None,
            [],
            {'passed': True},
            {'passed': 1, 'sessions': 2, 'text': 'Damage: 123'},
            {'passed': False, 'error': 'connection closed'},
            {'passed': True, 'sessions': 1, 'text': 'Damage: 123'},
            {'passed': True, 'sessions': 2, 'text': 'Waiting for OverlayPlugin'},
        ):
            with self.subTest(result=result), self.assertRaises(RuntimeError):
                runner.validate_result(result)

    def test_complete_browser_exchange_passes(self):
        runner.validate_result({'passed': True, 'sessions': 2, 'text': 'Damage: 123'})

    def test_browser_diagnostics_do_not_change_the_result(self):
        runner.validate_result({'passed': True, 'sessions': 2, 'text': 'Damage: 123', 'browser': 'Chrome'})

    def test_windows_edge_lookup_checks_both_program_files_locations(self):
        for variable in ('ProgramFiles(x86)', 'ProgramFiles'):
            with self.subTest(variable=variable), tempfile.TemporaryDirectory() as directory:
                executable = Path(directory) / 'Microsoft/Edge/Application/msedge.exe'
                executable.parent.mkdir(parents=True)
                executable.touch()
                windows = SimpleNamespace(name='nt', environ={variable: directory})
                with (
                    mock.patch.object(runner, 'os', windows),
                    mock.patch.object(runner.shutil, 'which', return_value=None),
                ):
                    self.assertEqual(runner.find_chrome(), str(executable))

    def test_winepath_timeout_records_failed_summary(self):
        def hung_winepath(command, *, text, timeout):
            self.assertTrue(text)
            self.assertGreater(timeout, 0)
            self.assertLessEqual(timeout, 30)
            raise subprocess.TimeoutExpired(command, timeout)

        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            bundle = root / 'bundle/Debug'
            bundle.mkdir(parents=True)
            (bundle / 'OverlayPlugin.SmokeTests.exe').touch()
            results = root / 'results'
            arguments = ['run-smoke.py', '--bundle', str(root / 'bundle'), '--results', str(results)]
            with (
                mock.patch.object(sys, 'argv', arguments),
                mock.patch.object(runner, 'os', SimpleNamespace(name='posix', environ={})),
                mock.patch.object(runner, 'find_chrome', return_value='chromium'),
                mock.patch.object(runner, 'run_process'),
                mock.patch.object(runner.subprocess, 'check_output', side_effect=hung_winepath),
                self.assertRaises(subprocess.TimeoutExpired),
            ):
                runner.main()

            summary = json.loads((results / 'summary.json').read_text())
            self.assertFalse(summary['passed'])
            self.assertEqual(summary['phases'], [])

    def test_runtime_and_installer_use_separate_cef_sources(self):
        for installer in (False, True):
            with self.subTest(installer=installer), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                for configuration in ('Debug', 'Release'):
                    bundle = root / 'bundle' / configuration
                    bundle.mkdir(parents=True)
                    (bundle / 'OverlayPlugin.SmokeTests.exe').touch()
                cef = root / 'bundle/cef'
                cef.mkdir()
                (cef / 'libcef.dll').write_text('build CEF')
                results = root / 'results'
                arguments = ['run-smoke.py', '--bundle', str(root / 'bundle'), '--results', str(results)]
                if installer:
                    arguments.append('--installer')

                def run_host(command, log, timeout=90):
                    if command[1] == 'install':
                        (Path(command[2]) / 'OverlayPluginCef/x64').mkdir(parents=True)

                with (
                    mock.patch.object(sys, 'argv', arguments),
                    mock.patch.object(runner, 'os', SimpleNamespace(name='nt', environ={})),
                    mock.patch.object(runner, 'find_chrome', return_value='chromium') as find_chrome,
                    mock.patch.object(runner, 'run_process', side_effect=run_host) as run_process,
                    mock.patch.object(runner, 'external_browser') as browser,
                ):
                    runner.main()

                modes = [call.args[0][1] for call in run_process.call_args_list]
                summary = json.loads((results / 'summary.json').read_text())
                self.assertTrue(summary['passed'])
                if installer:
                    self.assertEqual(modes, ['install', 'reuse'])
                    self.assertEqual(summary['phases'], ['Release/install-fresh-and-reuse'])
                    find_chrome.assert_not_called()
                    browser.assert_not_called()
                    self.assertFalse((results / 'Release/state/OverlayPluginCef/x64/libcef.dll').exists())
                else:
                    self.assertEqual(modes, ['render', 'render'])
                    self.assertEqual(summary['phases'], ['Debug/render', 'Release/render', 'Release/websocket'])
                    browser.assert_called_once()
                    self.assertEqual(browser.call_args.args[1], root / 'bundle/Release')
                    self.assertEqual(
                        (results / 'Release/state/OverlayPluginCef/x64/libcef.dll').read_text(), 'build CEF'
                    )


if __name__ == '__main__':
    unittest.main()
