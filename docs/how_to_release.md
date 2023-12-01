# OverlayPlugin Release Process

This doc describes how to make a release of OverlayPlugin.

## Overview

Anybody can (and should) propose a release by sending a PR if they think a release is needed.
Only @maintainers can currently publish a release.
You can notify that alias if you need somebody to cut a release for you.

## Steps

### Update the AssemblyInfo versions

Update the version number in `Directory.Build.props`

Commit this change and upload a PR, e.g. `Bump version to 0.19.0`.

### Land PR

Get review approval and merge the PR.

### Publish Release

Once the PR has landed, the `release.yml` workflow will run.
You can check <https://github.com/OverlayPlugin/OverlayPlugin/actions/workflows/release.yml> to see the progress of this step.
Once it finishes, a release is published automatically.
