# Windows and Wine smoke tests

The [runtime-tests workflow](../.github/workflows/runtime-tests.yml) builds Debug
and Release on Windows, then tests the same binaries on Windows, Ubuntu 24.04
with Wine 9 and 11, and Ubuntu 26.04 with Wine 11. It runs on pull requests
that change `HtmlRenderer/HtmlRenderer.csproj`, the runtime-tests workflow, or
files under `tests/`, and on manual runs.

## What is checked

| Scenario | Assertions |
| --- | --- |
| Debug rendering | CEF loads from the build's `libs` directory. |
| Release rendering | CEF from the build loads from an isolated application-data directory. |
| Transparent overlay | `OverlayForm` produces the expected image and preserves transparency in both offscreen frames and desktop captures. |
| Lifecycle | Resize, reload, and renderer recreation preserve the viewport, JavaScript binding, and rendering. |
| Embedded browser | `OverlayControl` renders in a child window with the expected viewport and JavaScript binding. |
| JavaScript binding | The fixture waits for `OverlayPluginApi.ready`, calls C#, checks the returned value, and reports its name, UUID, and viewport. |
| External browser (Release only) | A native browser connects to `/ws`, receives synthetic `CombatData` and matching handler responses, updates the page, and repeats the exchange after reconnecting. |

Tests use the product assemblies in a WinForms host. A running ACT or FFXIV
instance is not required. The WebSocket check uses an unencrypted local connection.

## Optional installer check

Select `check-installer` when manually running the workflow to test fresh
installation and reuse of the published CEF archive on Windows and all Wine
matrix entries. The product installer installs and validates CEF, then a second
process checks that the installation is reused.

The download uses .NET HTTP because ACT's download dispatcher requires its UI.
ACT's download integration is not covered.

The installer check uses the URL in the tested commit's `CefInstaller`, so a
candidate CEF redist must be published there before this optional check can
pass. Default runtime checks use build output and do not validate installation.

## Run locally

1. On Windows, build the solution with `build.bat -ci`, then run:

   ```powershell
   ./tests/prepare-smoke.ps1
   python tests/run-smoke.py
   ```

   This requires Python 3 and native Chrome/Chromium/Edge. Set `CHROME` to the
   browser executable if it is not found automatically.

   To run only the optional installer check:

   ```powershell
   python tests/run-smoke.py --installer --results out/installer-results
   ```

2. To test on Linux, transfer `out/smoke/` from that Windows build into the same
   location in a checkout. On Ubuntu, select a Wine major version and run:

   ```sh
   sudo bash tests/setup-wine.sh 11
   xvfb-run -a -s '-screen 0 1280x800x24' bash tests/run-wine.sh
   ```

   Setup selects the latest available package within the requested major:
   Wine 9 from Ubuntu, or other majors from WineHQ stable. The argument must be
   a positive integer. Setup fails if the repository for that Ubuntu release or
   the requested major is unavailable. CI tests Ubuntu 24.04 with Wine 9 and 11,
   and Ubuntu 26.04 with Wine 11; other combinations are not verified.
   The selected package version is printed in the setup log. Winetricks remains
   pinned.

   A native Chromium-based browser must also be installed. GitHub's Ubuntu
   runner provides Google Chrome. The scripts install .NET Framework 4.8 and
   the VC++ runtimes required by the plugin.
   Actions caches the initialized prefix separately for each Ubuntu release and
   installed Wine package version. The first run still installs the runtimes;
   later runs reuse them while testing CEF in fresh application-data directories.
   `WINEPREFIX` defaults to `out/wine-prefix`; use an isolated prefix, not an
   existing ACT/game installation. The script terminates that prefix's Wine
   processes on exit. `WINETRICKS_CACHE` may be set to reuse installer downloads.
   To run only the installer check, append
   `--installer --results out/installer-results` to `tests/run-wine.sh`.

3. Each run creates fresh state directories and updates `summary.json` in the
   results directory. Previous captures are preserved. Use
   `--results out/another-smoke-results` to keep runs and summaries separate.
   Run the orchestration regression checks independently with:

   ```sh
   python3 -m unittest discover -s tests -p 'test_*.py' -v
   ```

## Results and limits

`out/smoke-results/summary.json` records completed phases and overall success.
Execution steps print CEF versions and load paths, rendering diagnostics,
browser results, and Wine setup output directly to the workflow log. The summary
step publishes test results even on failure; PNG screenshots are available as
`smoke-screenshots-*` artifacts. Local runs print the same diagnostics to the
terminal and keep result JSON and screenshots. Optional installer results have
their own summary.

These component tests cover the scenarios above, rather than the full ACT/FFXIV
workflow. Linux rendering uses X11/Xvfb and software rendering; GPU and Wayland
compatibility are outside this test's scope.
