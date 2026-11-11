"""Run the same Windows smoke binaries natively or through Wine; stdlib only."""

import argparse
import functools
import http.server
import json
import os
import shutil
import signal
import subprocess
import threading
import time
from pathlib import Path


def run_process(command, log, timeout=90):
    with open(log, 'w', encoding='utf-8') as output:
        process = subprocess.Popen(
            command,
            stdout=output,
            stderr=subprocess.STDOUT,
            start_new_session=os.name != 'nt',
        )
        try:
            code = process.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            terminate(process)
            raise RuntimeError(f'Timed out after {timeout}s: {command}; see {log}')
        if code:
            raise RuntimeError(f'Exit code {code}: {command}; see {log}')


def terminate(process):
    if process.poll() is not None:
        return
    if os.name == 'nt':
        subprocess.run(['taskkill', '/PID', str(process.pid), '/T', '/F'], check=False)
    else:
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
    process.wait()


def validate_result(result):
    if (
        not isinstance(result, dict)
        or result.get('passed') is not True
        or result.get('sessions') != 2
        or result.get('text') != 'Damage: 123'
    ):
        raise RuntimeError(f'External browser failed: {result}')


class FixtureHandler(http.server.SimpleHTTPRequestHandler):
    def do_POST(self):
        if self.path != '/result':
            self.send_error(404)
            return
        try:
            length = int(self.headers.get('Content-Length', '0'))
            if not 0 < length <= 4096:
                raise ValueError('Invalid result length')
            result = json.loads(self.rfile.read(length))
        except (ValueError, json.JSONDecodeError):
            self.send_error(400)
            return
        self.server.result = result
        self.server.completed.set()
        self.send_response(200)
        self.end_headers()


def external_browser(command, bundle, state, results, chrome):
    handler = functools.partial(FixtureHandler, directory=str(bundle / 'fixtures'))
    with http.server.ThreadingHTTPServer(('127.0.0.1', 0), handler) as server:
        server.completed = threading.Event()
        server.result = None
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        with (
            open(results / 'websocket.log', 'w', encoding='utf-8') as host_log,
            open(results / 'chromium.log', 'w', encoding='utf-8') as browser_log,
        ):
            host = subprocess.Popen(
                command, stdout=host_log, stderr=subprocess.STDOUT, start_new_session=os.name != 'nt'
            )
            browser = None
            try:
                deadline = time.monotonic() + 30
                while not (state / 'websocket-ready').exists():
                    if host.poll() is not None or time.monotonic() > deadline:
                        raise RuntimeError('WebSocket host did not start; see websocket.log')
                    time.sleep(0.1)
                browser = subprocess.Popen(
                    [
                        chrome,
                        '--headless',
                        '--disable-gpu',
                        '--no-sandbox',
                        '--no-first-run',
                        '--no-default-browser-check',
                        f'--user-data-dir={results / "chromium-profile"}',
                        f'http://127.0.0.1:{server.server_port}/websocket.html',
                    ],
                    stdout=browser_log,
                    stderr=subprocess.STDOUT,
                    start_new_session=os.name != 'nt',
                )
                if not server.completed.wait(30):
                    raise RuntimeError('External browser timed out; see chromium.log and websocket.log')
                (results / 'websocket-result.json').write_text(json.dumps(server.result), encoding='utf-8')
                validate_result(server.result)
                (state / 'websocket-stop').write_text('stop', encoding='utf-8')
                if host.wait(timeout=15) != 0:
                    raise RuntimeError('WebSocket host failed; see websocket.log')
            finally:
                if browser:
                    terminate(browser)
                terminate(host)
                server.shutdown()
                thread.join()


def find_chrome():
    if os.environ.get('CHROME'):
        return os.environ['CHROME']
    for name in ('google-chrome', 'chromium', 'chromium-browser'):
        if shutil.which(name):
            return shutil.which(name)
    if os.name == 'nt':
        for variable in ('ProgramFiles(x86)', 'ProgramFiles'):
            if os.environ.get(variable):
                edge = Path(os.environ[variable]) / 'Microsoft/Edge/Application/msedge.exe'
                if edge.is_file():
                    return str(edge)
    raise RuntimeError('Set CHROME to a native Chrome/Chromium/Edge executable')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--bundle', type=Path, default=Path('out/smoke'))
    parser.add_argument('--results', type=Path, default=Path('out/smoke-results'))
    parser.add_argument('--installer', action='store_true', help='Only test published CEF installation and reuse')
    args = parser.parse_args()
    results = args.results.resolve()
    results.mkdir(parents=True, exist_ok=False)
    summary = {'passed': False, 'platform': os.name, 'phases': []}
    try:
        if not args.installer:
            chrome = find_chrome()
            if os.name == 'nt':
                (results / 'browser-version.log').write_text(f'Executable: {chrome}\n', encoding='utf-8')
            else:
                run_process([chrome, '--version'], results / 'browser-version.log', timeout=10)
        configurations = ('Release',) if args.installer else ('Debug', 'Release')
        for configuration in configurations:
            bundle = (args.bundle / configuration).resolve()
            executable = bundle / 'OverlayPlugin.SmokeTests.exe'
            if not executable.is_file():
                raise RuntimeError(f'Missing test host: {executable}')
            output = results / configuration
            state = output / 'state'
            state.mkdir(parents=True)
            prefix = []
            state_argument = str(state)
            if os.name != 'nt':
                prefix = [os.environ.get('WINE', 'wine')]
                state_argument = subprocess.check_output(['winepath', '-w', str(state)], text=True, timeout=30).strip()
            command = prefix + [str(executable)]
            if args.installer:
                run_process(command + ['install', state_argument], output / 'install-fresh.log', timeout=180)
                sentinel = state / 'OverlayPluginCef/x64/ci-reuse-sentinel'
                sentinel.write_text('preserve', encoding='utf-8')
                run_process(command + ['reuse', state_argument], output / 'install-reuse.log', timeout=180)
                if not sentinel.exists():
                    raise RuntimeError('CEF was unnecessarily reinstalled')
                summary['phases'].append('Release/install-fresh-and-reuse')
                continue
            if configuration == 'Release':
                shutil.copytree(args.bundle / 'cef', state / 'OverlayPluginCef/x64')
            run_process(command + ['render', state_argument], output / 'render.log')
            summary['phases'].append(f'{configuration}/render')
            if configuration == 'Release':
                external_browser(command + ['websocket', state_argument], bundle, state, output, chrome)
                summary['phases'].append('Release/websocket')
        summary['passed'] = True
    finally:
        (results / 'summary.json').write_text(json.dumps(summary, indent=2), encoding='utf-8')
    print(json.dumps(summary, indent=2))


if __name__ == '__main__':
    main()
