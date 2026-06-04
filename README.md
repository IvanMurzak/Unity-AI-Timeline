<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-AI-Timeline?tab=readme-ov-file#unity-ai-timeline">Unity AI Timeline</a></h1>

<div align="center" width="100%">

[![MCP](https://badge.mcpx.dev 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp.timeline?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp.timeline/)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=2A2A2A 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![r](https://github.com/IvanMurzak/Unity-AI-Timeline/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-AI-Timeline/actions/workflows/release.yml)</br>
[![Discord](https://img.shields.io/badge/Discord-Join-7289da?logo=discord&logoColor=white&labelColor=333A41 'Join')](https://discord.gg/cfbdMZX99G)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-AI-Timeline 'Stars')](https://github.com/IvanMurzak/Unity-AI-Timeline/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-AI-Timeline?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-AI-Timeline/blob/main/LICENSE)
[![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

</div>

<img width="100%" alt="Timeline" src="https://github.com/IvanMurzak/Unity-AI-Timeline/raw/main/docs/promo/promo-timeline.gif"/>

AI-powered tools for the Unity [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@1.8) cutscene & sequencing workflow. Create `TimelineAsset`s, add Animation / Activation / Audio / Signal / Control tracks, add and time clips (start, duration, blends, eases), place signal markers, bind a `PlayableDirector` and its per-track scene bindings, list and inspect everything, and modify any Timeline object field directly — all through natural language commands, no manual Timeline-window navigation. Wraps `com.unity.timeline` **1.8.12**. Ideal for rapid cutscene blocking, sequence authoring, and procedural timeline rigs. Built on top of the [AI Game Developer](https://github.com/IvanMurzak/Unity-MCP) platform.

### How to use

- [Instructions](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#step-2-install-mcp-client)
- [Video Tutorial for Visual Studio Code](https://www.youtube.com/watch?v=ZhP7Ju91mOE)
- [Video Tutorial for Visual Studio](https://www.youtube.com/watch?v=RGdak4T69mc)

[![DOWNLOAD INSTALLER](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/button/button_download.svg?raw=true)](https://github.com/IvanMurzak/Unity-AI-Timeline/releases/latest/download/AI-Timeline-Installer.unitypackage)

### Stability status

| Unity Version | Editmode                                                                                                                                                                                                  | Playmode                                                                                                                                                                                                  | Standalone                                                                                                                                                                                                  |
| ------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 2022.3.62f3   | [![r](https://github.com/IvanMurzak/Unity-AI-Timeline/workflows/release/badge.svg?job=test-unity-2022-3-62f3-editmode)](https://github.com/IvanMurzak/Unity-AI-Timeline/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-Timeline/workflows/release/badge.svg?job=test-unity-2022-3-62f3-playmode)](https://github.com/IvanMurzak/Unity-AI-Timeline/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-Timeline/workflows/release/badge.svg?job=test-unity-2022-3-62f3-standalone)](https://github.com/IvanMurzak/Unity-AI-Timeline/actions/workflows/release.yml)  |
| 2023.2.22f1   | [![r](https://github.com/IvanMurzak/Unity-AI-Timeline/workflows/release/badge.svg?job=test-unity-2023-2-22f1-editmode)](https://github.com/IvanMurzak/Unity-AI-Timeline/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-Timeline/workflows/release/badge.svg?job=test-unity-2023-2-22f1-playmode)](https://github.com/IvanMurzak/Unity-AI-Timeline/actions/workflows/release.yml)  | [![r](https://github.com/IvanMurzak/Unity-AI-Timeline/workflows/release/badge.svg?job=test-unity-2023-2-22f1-standalone)](https://github.com/IvanMurzak/Unity-AI-Timeline/actions/workflows/release.yml)  |
| 6000.3.1f1    | [![r](https://github.com/IvanMurzak/Unity-AI-Timeline/workflows/release/badge.svg?job=test-unity-6000-3-1f1-editmode)](https://github.com/IvanMurzak/Unity-AI-Timeline/actions/workflows/release.yml)   | [![r](https://github.com/IvanMurzak/Unity-AI-Timeline/workflows/release/badge.svg?job=test-unity-6000-3-1f1-playmode)](https://github.com/IvanMurzak/Unity-AI-Timeline/actions/workflows/release.yml)   | [![r](https://github.com/IvanMurzak/Unity-AI-Timeline/workflows/release/badge.svg?job=test-unity-6000-3-1f1-standalone)](https://github.com/IvanMurzak/Unity-AI-Timeline/actions/workflows/release.yml)   |

## AI Timeline Tools

13 tools, grouped by purpose:

### TimelineAsset lifecycle

- `timeline-create` - Create a new empty `TimelineAsset` (.playable) at a project path
- `timeline-list` - List every `TimelineAsset` in the project with track count and duration
- `timeline-get` - Generic read: serialize a `TimelineAsset`, a track, or a clip's `PlayableAsset`
- `timeline-modify` - Generic write: apply a `SerializedMember` diff to a Timeline object

### Tracks

- `timeline-track-add` - Add an Animation / Activation / Audio / Signal / Control / Playable / Group track
- `timeline-track-remove` - Remove a track (and its clips/markers) by name or index
- `timeline-track-list` - List a timeline's tracks with type, mute/lock state, and clips

### Clips

- `timeline-clip-add` - Add a clip (animation / audio / default) to a track
- `timeline-clip-set-timing` - Set start, duration, clip-in, blends, eases and time scale of a clip
- `timeline-clip-move` - Move a clip to an absolute start or by a relative delta (duration preserved)

### Markers & bindings

- `timeline-marker-add` - Add a marker (SignalEmitter by default) on a track or the marker track
- `timeline-director-bind` - Bind a `TimelineAsset` to a scene `PlayableDirector` (adds it if missing)
- `timeline-track-bind` - Bind a scene object/component to a Timeline output track via the director

## License

[MIT](LICENSE) © [Ivan Murzak](https://github.com/IvanMurzak)
