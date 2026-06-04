/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Timeline
    {
        public const string TrackListToolId = "timeline-track-list";

        [AiTool
        (
            TrackListToolId,
            Title = "Timeline / List Tracks",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [AiSkillDescription("List the tracks of a `TimelineAsset` — name, type, mute/lock state, clip count, and " +
            "each track's clips (with timing). Read-only.")]
        [AiSkillBody("List the tracks of a `TimelineAsset`. For each output track returns its name, type, muted / " +
            "locked flags, clip count, and (optionally) the clips with their timing.\n\n" +
            "## Inputs\n\n" +
            "- `assetPath` — required path to the `.playable` TimelineAsset.\n" +
            "- `includeClips` — when `true` (default) include each track's clip list; when `false`, counts only.\n\n" +
            "## Behavior\n\n" +
            "Enumerates `TimelineAsset.GetOutputTracks()` and reports each track. Read-only. Runs on the Unity " +
            "main thread.")]
        [Description("Lists every track of a TimelineAsset with type, mute/lock state, clip count and (optionally) the clips. Read-only.")]
        public TrackListResponse ListTracks
        (
            [Description("Assets-rooted path to the TimelineAsset (.playable).")]
            string assetPath,
            [Description("If true (default), include each track's clips and their timing; if false, only counts.")]
            bool includeClips = true
        )
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            return MainThread.Instance.Run(() =>
            {
                var timeline = LoadTimelineAsset(assetPath);

                var tracks = new List<TrackInfo>();
                int rootIndex = 0;
                var rootSet = new HashSet<TrackAsset>(timeline.GetRootTracks());
                foreach (var track in timeline.GetOutputTracks())
                {
                    if (track == null)
                        continue;

                    int clipCount = 0;
                    foreach (var _ in track.GetClips())
                        clipCount++;

                    tracks.Add(new TrackInfo
                    {
                        name = track.name,
                        trackType = track.GetType().Name,
                        isRoot = rootSet.Contains(track),
                        rootIndex = rootSet.Contains(track) ? rootIndex++ : -1,
                        muted = track.muted,
                        locked = track.locked,
                        clipCount = clipCount,
                        clips = includeClips ? DescribeClips(track).ToArray() : Array.Empty<ClipInfo>()
                    });
                }

                return new TrackListResponse
                {
                    assetPath = assetPath.Replace('\\', '/'),
                    durationMode = timeline.durationMode.ToString(),
                    duration = timeline.duration,
                    count = tracks.Count,
                    tracks = tracks.ToArray()
                };
            });
        }

        public class TrackListResponse
        {
            [Description("Project path of the TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Duration mode of the timeline.")]
            public string durationMode = string.Empty;

            [Description("Computed duration of the timeline in seconds.")]
            public double duration;

            [Description("Number of output tracks.")]
            public int count;

            [Description("The output tracks of the timeline.")]
            public TrackInfo[] tracks = Array.Empty<TrackInfo>();
        }

        public class TrackInfo
        {
            [Description("Track name.")]
            public string name = string.Empty;

            [Description("Track type name.")]
            public string trackType = string.Empty;

            [Description("Whether the track is a root (top-level) track.")]
            public bool isRoot;

            [Description("Root-track index, or -1 when nested in a group.")]
            public int rootIndex = -1;

            [Description("Whether the track is muted.")]
            public bool muted;

            [Description("Whether the track is locked.")]
            public bool locked;

            [Description("Number of clips on the track.")]
            public int clipCount;

            [Description("The clips on the track (empty when includeClips is false).")]
            public ClipInfo[] clips = Array.Empty<ClipInfo>();
        }
    }
}
