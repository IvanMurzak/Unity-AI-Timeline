# AGENTS.md — Unity-AI-Timeline

Unity package `com.ivanmurzak.unity.mcp.timeline` — **Unity Timeline** (`com.unity.timeline`) for AI agents. It
ships no MCP tools: an RQL module for Unity-MCP's `rql-query` / `rql-mutate` (create `TimelineAsset`s, add/remove tracks,
add and time clips, place markers, bind a `PlayableDirector` and its per-track scene bindings). An extension of
[AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) (`com.ivanmurzak.unity.mcp`), generated from
`Unity-AI-Tools-Template`; the former tool → RQL table is in `README.md`.

## Layout

- `Unity-Package/` — Unity project hosting the package; only `Packages/com.ivanmurzak.unity.mcp.timeline/`
  ships.
  - `package.json` — the version source of truth and the dependency pins.
  - `Editor/Scripts/Rql/` — the `TimelineModule` (IRqlModule: `timeline` source, one adapter per file, the agent
    guide); replaces the former `timeline-*` tools.
  - `Runtime/` — runtime assembly (currently empty).
  - `Tests/Editor/Rql/` (EditMode, `TimelineTestBase` + one file per adapter / source) and `Tests/Runtime/` (PlayMode).
- `Unity-Tests/<unity-version>/` — one test project per supported Unity version, consuming the package via
  `file:`. A new version must also be added to `test_pull_request.yml` and `release.yml` (and
  `open-all-projects-*`).
- `Installer/` — Unity project that exports the `.unitypackage` installer
  (`Assets/com.IvanMurzak/<name> Installer/`).
- `commands/` — PowerShell helpers (below) and `check-versions.py` (synced in by the release cascade — do not
  hand-edit; CI runs it as the `version-consistency` gate).
- `.github/workflows/` — `test_pull_request.yml` (PR test matrix), `test_unity_plugin.yml` (reusable test
  job), `release.yml`, `bump_version.yml`.
- `docs/` — user docs: `Deploy-{OpenUPM,GitHub,npmjs}.md`, `Manual-Package-Rename.md`, `openupm-signing.md`,
  `img/`, `promo/`.

## Commands (PowerShell, from the repo root)

```powershell
./commands/bump-version.ps1 -NewVersion "x.y.z"   # package.json + Installer.cs in lock-step; -WhatIf to preview
./commands/get-version.ps1
./commands/update-ai-game-developer.ps1           # bump the com.ivanmurzak.unity.mcp pin; -WhatIf to preview
./commands/open-all-projects-windows.ps1          # open all Unity projects (or -unix.sh)
```

Tests run inside the Unity Editor (NUnit + `[UnityTest]`); CI runs them with `game-ci/unity-test-runner` in
EditMode / PlayMode / Standalone for every Unity version. `release.yml` releases on push to `main` only when
the `package.json` version has no tag yet (installer, signed UPM tarball for OpenUPM, test matrix, tag, GitHub
Release); dispatch `bump_version.yml` to bump in CI. Under `pull_request_target`, tests need the
maintainer-applied `ci-ok` label and abort if the PR touches `.github/workflows/`.

## Critical invariants

- **RQL surface.** Agents use Unity-MCP's `rql-query` / `rql-mutate`; operations are adapter
  methods on the owner type (TimelineAsset.AddTrack, TrackAsset.AddClip / AddMarker / Delete, TimelineClip.Delete,
  Marker.Delete), owner-less creation lives on the `timeline` source (`[RqlCallable]`), and every example belongs in the
  module guide (`TimelineModule.Guide`, rendered into the `unity-rql` skill).
- **Plain-class writes.** `TimelineClip` is not a `UnityEngine.Object`, so the engine cannot record or dirty it: its
  adapter wraps every writable member to `Undo.RecordObject` the parent track and dirty track + timeline. Do the same for
  any new non-Object Timeline type.
- **Asset-backed.** TimelineAssets are project assets (`.playable`); tracks, clips' PlayableAssets and markers are its
  sub-assets. RQL edits mark them dirty (no `SaveAssets`); Timeline's own create/delete calls register their Undo steps.
- **Hidden internals.** Private `m_*` serialized fields of TimelineAsset / TrackAsset / Marker (m_Tracks, m_Clips …)
  are hidden: they are only consistent when changed through Timeline's API.
- Every C# file starts with the copyright box header and `#nullable enable`.
