/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using Microsoft.Extensions.Logging;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEngine;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Timeline
    {
        public const string GetToolId = "timeline-get";

        [AiTool
        (
            GetToolId,
            Title = "Timeline / Get Object",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Generic read: serialize a Timeline object via ReflectorNet — the `TimelineAsset` " +
            "itself, a `TrackAsset` (by name/index), or a clip's `PlayableAsset` (track + clipIndex). Pair with " +
            "'timeline-modify' to write changes back. Read-only.")]
        [AiSkillBody("Serialize a Timeline object via ReflectorNet. This is the generic escape hatch for fields " +
            "not covered by the dedicated tools (e.g. an `AnimationPlayableAsset`'s settings or a track's mixer " +
            "properties).\n\n" +
            "## Inputs\n\n" +
            "- `assetPath` — required `.playable` TimelineAsset path.\n" +
            "- `trackName` / `trackIndex` — when set, target a track instead of the whole asset.\n" +
            "- `clipIndex` — when >= 0 (and a track is selected), target that clip's `PlayableAsset`.\n" +
            "- `deepSerialization` — when `true`, recurse through nested objects.\n\n" +
            "## Behavior\n\n" +
            "Resolves the target object (asset / track / clip asset), serializes it via ReflectorNet, and returns " +
            "the serialized member plus the resolved type name. Read-only. Runs on the Unity main thread.")]
        [Description("Generic: serialize a TimelineAsset, a TrackAsset, or a clip's PlayableAsset via ReflectorNet. Read-only.")]
        public TimelineGetResponse GetObject
        (
            [Description("Assets-rooted path to the TimelineAsset (.playable).")]
            string assetPath,
            [Description("Optional track name to target a track instead of the asset.")]
            string? trackName = null,
            [Description("Root-track index used when trackName is null and a track target is wanted (-1 = asset only).")]
            int trackIndex = -1,
            [Description("Clip index to target that clip's PlayableAsset (-1 = the track/asset itself).")]
            int clipIndex = -1,
            [Description("Performs deep serialization including nested objects. Otherwise only top-level members.")]
            bool deepSerialization = false
        )
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            return MainThread.Instance.Run(() =>
            {
                var timeline = LoadTimelineAsset(assetPath);
                var target = ResolveTimelineTarget(timeline, trackName, trackIndex, clipIndex, out var targetKind);

                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception(Error.ReflectorNotAvailable());
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Timeline>();

                return new TimelineGetResponse
                {
                    assetPath = assetPath.Replace('\\', '/'),
                    targetKind = targetKind,
                    targetType = target.GetType().FullName ?? target.GetType().Name,
                    data = reflector.Serialize(
                        obj: target,
                        name: target.GetType().Name,
                        recursive: deepSerialization,
                        logger: logger)
                };
            });
        }

        /// <summary>Resolve the asset / a track / a clip's PlayableAsset as a serialization target.</summary>
        static UnityEngine.Object ResolveTimelineTarget(TimelineAsset timeline, string? trackName, int trackIndex, int clipIndex, out string targetKind)
        {
            bool wantsTrack = !string.IsNullOrEmpty(trackName) || trackIndex >= 0;
            if (!wantsTrack)
            {
                targetKind = "TimelineAsset";
                return timeline;
            }

            var track = ResolveTrack(timeline, trackName, trackIndex < 0 ? 0 : trackIndex);
            if (clipIndex < 0)
            {
                targetKind = "TrackAsset";
                return track;
            }

            var clip = ResolveClip(track, clipIndex);
            var asset = clip.asset as UnityEngine.Object;
            if (asset == null)
                throw new Exception("[Error] The selected clip has no PlayableAsset to serialize.");
            targetKind = "ClipAsset";
            return asset;
        }

        public class TimelineGetResponse
        {
            [Description("Project path of the TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Kind of the serialized target: TimelineAsset, TrackAsset, or ClipAsset.")]
            public string targetKind = string.Empty;

            [Description("Full type name of the serialized target.")]
            public string targetType = string.Empty;

            [Description("Serialized target data.")]
            public SerializedMember? data;
        }
    }
}
