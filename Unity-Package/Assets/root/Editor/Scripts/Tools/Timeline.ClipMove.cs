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
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Timeline
    {
        public const string ClipMoveToolId = "timeline-clip-move";

        [AiTool
        (
            ClipMoveToolId,
            Title = "Timeline / Move Clip",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Move a clip along its track — either to an absolute `start` time or by a relative " +
            "`deltaSeconds`. Duration is preserved.")]
        [AiSkillBody("Reposition a clip on its track without changing its duration. Provide either an absolute " +
            "`start` or a relative `deltaSeconds` (delta is ignored when `start` is supplied).\n\n" +
            "## Inputs\n\n" +
            "- `assetPath` — required path to the `.playable` TimelineAsset.\n" +
            "- `trackName` / `trackIndex` — which track holds the clip (name wins; index default 0).\n" +
            "- `clipIndex` — required zero-based clip index.\n" +
            "- `start` — optional absolute new start time (seconds).\n" +
            "- `deltaSeconds` — optional relative shift (seconds); used only when `start` is null.\n\n" +
            "## Behavior\n\n" +
            "Computes the new start (clamped to >= 0), sets `clip.start`, saves the asset, and returns the old and " +
            "new start. Runs on the Unity main thread.")]
        [Description("Moves a Timeline clip to an absolute start time or by a relative delta (duration preserved).")]
        public ClipMoveResponse MoveClip
        (
            [Description("Assets-rooted path to the TimelineAsset (.playable).")]
            string assetPath,
            [Description("Zero-based index of the clip on its track.")]
            int clipIndex,
            [Description("Name of the track holding the clip. Takes precedence over trackIndex.")]
            string? trackName = null,
            [Description("Zero-based root-track index, used when trackName is null/empty.")]
            int trackIndex = 0,
            [Description("Optional absolute new start time in seconds.")]
            double? start = null,
            [Description("Optional relative shift in seconds; used only when start is null.")]
            double deltaSeconds = 0.0
        )
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            return MainThread.Instance.Run(() =>
            {
                var timeline = LoadTimelineAsset(assetPath);
                var track = ResolveTrack(timeline, trackName, trackIndex);
                var clip = ResolveClip(track, clipIndex);

                var oldStart = clip.start;
                var newStart = start ?? (oldStart + deltaSeconds);
                if (newStart < 0.0)
                    newStart = 0.0;
                clip.start = newStart;

                SaveTimeline(timeline, track);

                return new ClipMoveResponse
                {
                    assetPath = assetPath.Replace('\\', '/'),
                    trackName = track.name,
                    clipIndex = clipIndex,
                    oldStart = oldStart,
                    newStart = clip.start,
                    duration = clip.duration,
                    end = clip.end,
                    success = true
                };
            });
        }

        public class ClipMoveResponse
        {
            [Description("Project path of the TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Name of the track holding the clip.")]
            public string trackName = string.Empty;

            [Description("Index of the clip on its track.")]
            public int clipIndex;

            [Description("Previous start time in seconds.")]
            public double oldStart;

            [Description("New start time in seconds.")]
            public double newStart;

            [Description("Clip duration in seconds (unchanged).")]
            public double duration;

            [Description("New end time in seconds.")]
            public double end;

            [Description("Whether the operation succeeded.")]
            public bool success;
        }
    }
}
