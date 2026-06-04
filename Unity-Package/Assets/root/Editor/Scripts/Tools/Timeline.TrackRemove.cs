/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
│  Copyright (c) 2025 Ivan Murzak                                             │
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
        public const string TrackRemoveToolId = "timeline-track-remove";

        [AiTool
        (
            TrackRemoveToolId,
            Title = "Timeline / Remove Track",
            ReadOnlyHint = false,
            DestructiveHint = true,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Remove a root track (and its clips/markers) from a `TimelineAsset`, identified by " +
            "`trackName` or by `trackIndex`. Destructive.")]
        [AiSkillBody("Remove a track from a `TimelineAsset`. The track is identified by name (preferred) or by its " +
            "root index. All clips and markers on the track are deleted with it.\n\n" +
            "## Inputs\n\n" +
            "- `assetPath` — required path to the `.playable` TimelineAsset.\n" +
            "- `trackName` — name of the track to remove. When provided, takes precedence over `trackIndex`.\n" +
            "- `trackIndex` — zero-based root-track index used when `trackName` is null/empty (default 0).\n\n" +
            "## Behavior\n\n" +
            "Resolves the track, deletes it via `TimelineAsset.DeleteTrack`, saves the asset, and returns the " +
            "remaining track count. Destructive. Runs on the Unity main thread.")]
        [Description("Removes a track (by name or root index) and all its clips/markers from a TimelineAsset. Destructive.")]
        public TrackRemoveResponse RemoveTrack
        (
            [Description("Assets-rooted path to the TimelineAsset (.playable).")]
            string assetPath,
            [Description("Name of the track to remove. Takes precedence over trackIndex when provided.")]
            string? trackName = null,
            [Description("Zero-based root-track index, used when trackName is null/empty.")]
            int trackIndex = 0
        )
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            return MainThread.Instance.Run(() =>
            {
                var timeline = LoadTimelineAsset(assetPath);
                var track = ResolveTrack(timeline, trackName, trackIndex);
                var removedName = track.name;
                var removedType = track.GetType().Name;

                timeline.DeleteTrack(track);
                SaveTimeline(timeline);

                int remaining = 0;
                foreach (var _ in timeline.GetRootTracks())
                    remaining++;

                return new TrackRemoveResponse
                {
                    assetPath = assetPath.Replace('\\', '/'),
                    removedTrackName = removedName,
                    removedTrackType = removedType,
                    remainingRootTrackCount = remaining,
                    success = true
                };
            });
        }

        public class TrackRemoveResponse
        {
            [Description("Project path of the TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Name of the removed track.")]
            public string removedTrackName = string.Empty;

            [Description("Type name of the removed track.")]
            public string removedTrackType = string.Empty;

            [Description("Number of root tracks remaining after removal.")]
            public int remainingRootTrackCount;

            [Description("Whether the operation succeeded.")]
            public bool success;
        }
    }
}
