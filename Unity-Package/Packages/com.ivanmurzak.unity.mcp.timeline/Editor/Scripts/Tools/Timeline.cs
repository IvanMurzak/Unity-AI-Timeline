/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
│  Copyright (c) 2025 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.McpPlugin;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [AiToolType]
    public partial class Tool_Timeline
    {
        public static class Error
        {
            public static string GameObjectNotFound()
                => "[Error] GameObject not found. Provide a valid reference to an existing GameObject.";

            public static string TimelineAssetNotFound(string path)
                => $"[Error] TimelineAsset not found at '{path}'. Make sure the asset exists and is a TimelineAsset (.playable).";

            public static string TimelineAssetPathInvalid(string path)
                => $"[Error] Invalid TimelineAsset path '{path}'. The path must start with 'Assets/' and end with '.playable'.";

            public static string TrackNotFound(string trackName)
                => $"[Error] Track '{trackName}' not found on the TimelineAsset. Use 'timeline-track-list' to inspect existing tracks.";

            public static string TrackIndexOutOfRange(int index, int count)
                => $"[Error] Track index {index} is out of range. The TimelineAsset has {count} root track(s).";

            public static string ClipIndexOutOfRange(int index, int count)
                => $"[Error] Clip index {index} is out of range. The track has {count} clip(s).";

            public static string TrackTypeNotResolved(string typeName)
                => $"[Error] Track type '{typeName}' could not be resolved. Provide a known kind " +
                   "(Animation, Activation, Audio, Signal, Control, Playable, Group) or a full TrackAsset type name " +
                   "(e.g. 'UnityEngine.Timeline.AnimationTrack').";

            public static string NotATrackAsset(string typeName)
                => $"[Error] Type '{typeName}' does not derive from UnityEngine.Timeline.TrackAsset.";

            public static string PlayableDirectorNotFound()
                => "[Error] PlayableDirector component not found on the target GameObject. " +
                   "Make sure the GameObject has a PlayableDirector, or allow this tool to add one.";

            public static string TypeNotFound(string typeName)
                => $"[Error] Type '{typeName}' could not be resolved. Provide a full type name.";

            public static string ReflectorNotAvailable()
                => "[Error] ReflectorNet reflector is not available.";
        }
    }
}
