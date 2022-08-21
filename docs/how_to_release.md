# OverlayPlugin Release Process

This doc describes how to make a release of OverlayPlugin.

## Overview

Anybody can (and should) propose a release by sending a PR if they think a release is needed.
Only @maintainers can currently publish a release.
You can notify that alias if you need somebody to cut a release for you.

## Steps

### Update the AssemblyInfo versions

Run `python tools/set_version.py 0.19.0`
where `0.19.0` is the version you want to update.
This will update all of the `AssemblyInfo.cs` files to the new version number.
You can verify this by running `python tools/validate_versions.py`,
which will print `Versions in sync!` if the versions in all files match.

Commit this change and upload a PR, e.g. `Bump version to 0.19.0`.

### Land PR

Get review approval and merge the PR.

### Publish Release

Once the PR has landed, the `release.yml` workflow will run.
You can check <https://github.com/OverlayPlugin/OverlayPlugin/actions/workflows/release.yml> to see the progress of this step.
Once it finishes, the <https://github.com/OverlayPlugin/OverlayPlugin/releases> page will have a draft release.
Any maintainer can go to that page, and hit `Publish release`.

In the future, we could consider having this publish automatically as a part of the workflow if desired.
