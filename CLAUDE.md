# CLAUDE.md

## What this is

Unity package `com.ivanmurzak.unity.mcp.timeline` — wraps **Unity Timeline 1.8.12**
(`com.unity.timeline`) and exposes 13 `timeline-*` MCP tools so AI assistants can create
`TimelineAsset`s, add/remove tracks (Animation / Activation / Audio / Signal / Control /
Playable / Group), add and time clips, place markers, bind a `PlayableDirector` and its
per-track scene bindings, list/inspect timelines, and modify arbitrary Timeline object
fields. Built on top of [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)
(`com.ivanmurzak.unity.mcp`).

## Build / run

- Package source: `Unity-Package/Packages/com.ivanmurzak.unity.mcp.timeline/` (only this folder ships; Editor tools under `Editor/Scripts/Tools/`).
- Version source of truth: `Unity-Package/Packages/com.ivanmurzak.unity.mcp.timeline/package.json`. Bump with `.\commands\bump-version.ps1 -NewVersion "x.y.z"` (`-WhatIf` to preview).
- Update Unity-MCP dependency: `.\commands\update-ai-game-developer.ps1` (`-WhatIf` to preview).
- Multi-version test rigs: `Unity-Tests/{2022.3.62f3,2023.2.22f1,6000.3.1f1}`. Tests run inside the Unity Editor (NUnit + `[UnityTest]`); CI uses `game-ci/unity-test-runner@v4`. Releases trigger on push to `main` when the version tag is new.

## Critical invariants

- **Main thread only.** Every Unity API call inside a tool method MUST be wrapped in `MainThread.Instance.Run(() => { ... })` — MCP calls arrive off the main thread. ReflectorNet calls (`reflector.Serialize`, `TryModify`) touch Unity objects and must not run off the main thread.
- **Tool attributes.** The tool host is one `partial class Tool_Timeline` decorated `[AiToolType]`, split one-op-per-file (`Timeline.Create.cs`, `Timeline.TrackAdd.cs`, `Timeline.Modify.cs`, …). Each tool method is decorated `[AiTool(<id>, Title=…, …Hint=…)]` plus `[AiSkillDescription]` / `[AiSkillBody]` (LLM-facing skill copy) and a `[Description]` (parameter/return docs). Tool IDs are declared as `public const string …ToolId = "timeline-…"`. Every `[AiTool]` method declares at least one parameter.
- **EntityId split.** Unity 6.5+ returns `UnityEngine.EntityId` from `GameObject.GetEntityId()`; pre-6.5 returns `int` from `GetInstanceID()`. Files surfacing an instanceId ship as a `*.cs` (`#if UNITY_6000_5_OR_NEWER`) + `*.pre-Unity.6.5.cs` (`#if !UNITY_6000_5_OR_NEWER`) pair — e.g. `Timeline.DirectorBind.cs` / `Timeline.DirectorBind.pre-Unity.6.5.cs`. Keep both variants in sync when editing.
- **Generic modify via ReflectorNet.** `timeline-modify` applies a `SerializedMember` diff through `reflector.TryModify(ref boxed, data, …)`. ReflectorNet resolves the `fields` channel as `FieldInfo` and the `props` channel as `PropertyInfo` with **no cross-fallback** — a public field (e.g. `ControlPlayableAsset.updateParticle`) MUST go in `fields`; a property MUST go in `props`. Putting a field under `props` fails with `Property '…' not found or not writable`.
- **Asset-backed.** TimelineAssets are project assets (`.playable`), not scene objects. Tools load via `AssetDatabase.LoadAssetAtPath<TimelineAsset>`, mutate, then `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets`. Tracks/clips are sub-assets of the timeline.

## Find detail in

- `README.md` — user-facing setup walkthrough and the full `timeline-*` tool list
- `Unity-Package/Packages/com.ivanmurzak.unity.mcp.timeline/Editor/Scripts/Tools/` — one file per tool operation
