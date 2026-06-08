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
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Timeline
    {
        public const string ClipSetTimingToolId = "timeline-clip-set-timing";

        [AiTool
        (
            ClipSetTimingToolId,
            Title = "Timeline / Set Clip Timing",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Set the timing of a clip on a Timeline track: start, duration, clip-in (trim), " +
            "blend-in / blend-out durations, ease-in / ease-out durations, and time scale. Only the values you " +
            "supply are changed.")]
        [AiSkillBody("Adjust the timing of an existing clip identified by track + clip index. Any value left null " +
            "is untouched, so this tool can tweak a single property or several at once.\n\n" +
            "## Inputs\n\n" +
            "- `assetPath` — required path to the `.playable` TimelineAsset.\n" +
            "- `trackName` / `trackIndex` — which track holds the clip (name wins; index default 0).\n" +
            "- `clipIndex` — required zero-based index of the clip on its track.\n" +
            "- `start` — optional new start time (seconds).\n" +
            "- `duration` — optional new duration (seconds).\n" +
            "- `clipIn` — optional clip-in / trim offset into the source asset (seconds).\n" +
            "- `blendInDuration` / `blendOutDuration` — optional crossfade blend durations (seconds).\n" +
            "- `easeInDuration` / `easeOutDuration` — optional ease (fade) durations (seconds).\n" +
            "- `timeScale` — optional playback time scale of the clip.\n\n" +
            "## Behavior\n\n" +
            "Resolves the clip, applies each supplied value, saves the asset, and returns the resulting timing. " +
            "Runs on the Unity main thread.")]
        [Description("Sets clip timing (start, duration, clipIn, blends, eases, timeScale) on a Timeline clip; only supplied values change.")]
        public ClipTimingResponse SetClipTiming
        (
            [Description("Assets-rooted path to the TimelineAsset (.playable).")]
            string assetPath,
            [Description("Zero-based index of the clip on its track.")]
            int clipIndex,
            [Description("Name of the track holding the clip. Takes precedence over trackIndex.")]
            string? trackName = null,
            [Description("Zero-based root-track index, used when trackName is null/empty.")]
            int trackIndex = 0,
            [Description("Optional new start time in seconds.")]
            double? start = null,
            [Description("Optional new duration in seconds.")]
            double? duration = null,
            [Description("Optional clip-in (trim offset into the source asset) in seconds.")]
            double? clipIn = null,
            [Description("Optional blend-in (crossfade) duration in seconds.")]
            double? blendInDuration = null,
            [Description("Optional blend-out (crossfade) duration in seconds.")]
            double? blendOutDuration = null,
            [Description("Optional ease-in (fade) duration in seconds.")]
            double? easeInDuration = null,
            [Description("Optional ease-out (fade) duration in seconds.")]
            double? easeOutDuration = null,
            [Description("Optional playback time scale of the clip.")]
            double? timeScale = null
        )
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            return MainThread.Instance.Run(() =>
            {
                var timeline = LoadTimelineAsset(assetPath);
                var track = ResolveTrack(timeline, trackName, trackIndex);
                var clip = ResolveClip(track, clipIndex);

                if (start.HasValue) clip.start = start.Value;
                if (duration.HasValue) clip.duration = duration.Value;
                if (clipIn.HasValue) clip.clipIn = clipIn.Value;
                if (blendInDuration.HasValue) clip.blendInDuration = blendInDuration.Value;
                if (blendOutDuration.HasValue) clip.blendOutDuration = blendOutDuration.Value;
                if (easeInDuration.HasValue) clip.easeInDuration = easeInDuration.Value;
                if (easeOutDuration.HasValue) clip.easeOutDuration = easeOutDuration.Value;
                if (timeScale.HasValue) clip.timeScale = timeScale.Value;

                SaveTimeline(timeline, track);

                return new ClipTimingResponse
                {
                    assetPath = assetPath.Replace('\\', '/'),
                    trackName = track.name,
                    clipIndex = clipIndex,
                    displayName = clip.displayName,
                    start = clip.start,
                    duration = clip.duration,
                    end = clip.end,
                    clipIn = clip.clipIn,
                    blendInDuration = clip.blendInDuration,
                    blendOutDuration = clip.blendOutDuration,
                    timeScale = clip.timeScale,
                    success = true
                };
            });
        }

        /// <summary>Resolve a clip on a track by zero-based index (throws when out of range).</summary>
        static TimelineClip ResolveClip(TrackAsset track, int clipIndex)
        {
            var clips = new System.Collections.Generic.List<TimelineClip>(track.GetClips());
            if (clipIndex < 0 || clipIndex >= clips.Count)
                throw new Exception(Error.ClipIndexOutOfRange(clipIndex, clips.Count));
            return clips[clipIndex];
        }

        public class ClipTimingResponse
        {
            [Description("Project path of the TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Name of the track holding the clip.")]
            public string trackName = string.Empty;

            [Description("Index of the clip on its track.")]
            public int clipIndex;

            [Description("Display name of the clip.")]
            public string displayName = string.Empty;

            [Description("Resulting start time in seconds.")]
            public double start;

            [Description("Resulting duration in seconds.")]
            public double duration;

            [Description("Resulting end time in seconds.")]
            public double end;

            [Description("Resulting clip-in (trim) in seconds.")]
            public double clipIn;

            [Description("Resulting blend-in duration in seconds.")]
            public double blendInDuration;

            [Description("Resulting blend-out duration in seconds.")]
            public double blendOutDuration;

            [Description("Resulting time scale of the clip.")]
            public double timeScale;

            [Description("Whether the operation succeeded.")]
            public bool success;
        }
    }
}
