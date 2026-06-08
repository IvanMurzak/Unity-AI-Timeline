/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.ComponentModel;
using System.Linq;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Timeline
    {
        public const string MarkerAddToolId = "timeline-marker-add";

        [AiTool
        (
            MarkerAddToolId,
            Title = "Timeline / Add Marker",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Add a marker at a time on a track (or the timeline marker track). `markerType` " +
            "accepts 'Signal' (a SignalEmitter, default) or a full IMarker/ScriptableObject marker type name.")]
        [AiSkillBody("Add a marker at a given time. By default a `SignalEmitter` is created (the standard Timeline " +
            "signal marker). A full marker type name can be supplied to create a custom marker. The marker is " +
            "placed on the named track, or on the timeline's marker track when no track is given.\n\n" +
            "## Inputs\n\n" +
            "- `assetPath` — required path to the `.playable` TimelineAsset.\n" +
            "- `time` — required marker time in seconds.\n" +
            "- `markerType` — 'Signal' (default) or a full marker type name (must implement `IMarker`).\n" +
            "- `trackName` — optional track to host the marker; when null, the timeline's marker track is used.\n" +
            "- `trackIndex` — root-track index used when `trackName` is null AND `useMarkerTrack` is false.\n" +
            "- `useMarkerTrack` — when true (default), null trackName means the timeline marker track; when false, " +
            "the marker is placed on the track at `trackIndex`.\n\n" +
            "## Behavior\n\n" +
            "Resolves the marker holder (a `TrackAsset` or the timeline's marker track), creates the marker via " +
            "`CreateMarker`, sets its time, saves the asset, and returns the marker type and time. Runs on the " +
            "Unity main thread.")]
        [Description("Adds a marker (SignalEmitter by default) at a time on a track or the timeline marker track.")]
        public MarkerAddResponse AddMarker
        (
            [Description("Assets-rooted path to the TimelineAsset (.playable).")]
            string assetPath,
            [Description("Marker time in seconds.")]
            double time,
            [Description("Marker type: 'Signal' (default) or a full marker type name implementing IMarker.")]
            string markerType = "Signal",
            [Description("Optional track to host the marker. Takes precedence over the marker-track / index logic.")]
            string? trackName = null,
            [Description("Root-track index used when trackName is null and useMarkerTrack is false.")]
            int trackIndex = 0,
            [Description("When true (default), a null trackName targets the timeline's marker track; when false, the track at trackIndex.")]
            bool useMarkerTrack = true
        )
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            return MainThread.Instance.Run(() =>
            {
                var timeline = LoadTimelineAsset(assetPath);

                var type = ResolveMarkerType(markerType);

                TrackAsset holder;
                if (!string.IsNullOrEmpty(trackName))
                    holder = ResolveTrack(timeline, trackName, 0);
                else if (useMarkerTrack)
                    holder = timeline.markerTrack != null ? timeline.markerTrack : EnsureMarkerTrack(timeline);
                else
                    holder = ResolveTrack(timeline, null, trackIndex);

                var marker = holder.CreateMarker(type, time);

                SaveTimeline(timeline, holder);

                int markerCount = holder.GetMarkers().Count();

                return new MarkerAddResponse
                {
                    assetPath = assetPath.Replace('\\', '/'),
                    holderTrackName = holder.name,
                    markerType = marker.GetType().Name,
                    time = marker.time,
                    markerCountOnHolder = markerCount,
                    success = true
                };
            });
        }

        /// <summary>Ensure the timeline has a marker track and return it.</summary>
        static TrackAsset EnsureMarkerTrack(TimelineAsset timeline)
        {
            timeline.CreateMarkerTrack();
            return timeline.markerTrack;
        }

        /// <summary>Resolve a marker-kind keyword or a full marker type name to a concrete Type.</summary>
        static Type ResolveMarkerType(string markerType)
        {
            if (string.IsNullOrWhiteSpace(markerType) ||
                string.Equals(markerType.Trim(), "Signal", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(markerType.Trim(), "SignalEmitter", StringComparison.OrdinalIgnoreCase))
                return typeof(SignalEmitter);

            var type = Type.GetType(markerType, throwOnError: false);
            if (type == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(markerType, throwOnError: false);
                    if (type != null)
                        break;
                    type = asm.GetTypes().FirstOrDefault(t =>
                        string.Equals(t.Name, markerType, StringComparison.Ordinal) &&
                        typeof(UnityEngine.Timeline.IMarker).IsAssignableFrom(t));
                    if (type != null)
                        break;
                }
            }

            if (type == null)
                throw new Exception(Error.TypeNotFound(markerType));
            if (!typeof(UnityEngine.Timeline.IMarker).IsAssignableFrom(type))
                throw new Exception($"[Error] Type '{markerType}' does not implement UnityEngine.Timeline.IMarker.");

            return type;
        }

        public class MarkerAddResponse
        {
            [Description("Project path of the TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Name of the track (or marker track) the marker was placed on.")]
            public string holderTrackName = string.Empty;

            [Description("Type name of the created marker.")]
            public string markerType = string.Empty;

            [Description("Marker time in seconds.")]
            public double time;

            [Description("Number of markers on the holding track after adding.")]
            public int markerCountOnHolder;

            [Description("Whether the operation succeeded.")]
            public bool success;
        }
    }
}
