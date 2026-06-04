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
        public const string TrackAddToolId = "timeline-track-add";

        [AiTool
        (
            TrackAddToolId,
            Title = "Timeline / Add Track",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Add a track to a `TimelineAsset`. `trackType` accepts a kind keyword (Animation, " +
            "Activation, Audio, Signal, Control, Playable, Group) or a full TrackAsset type name. Optionally nest " +
            "the new track under an existing GroupTrack.")]
        [AiSkillBody("Add a new track to a `TimelineAsset`. Tracks are the lanes a timeline plays: an " +
            "`AnimationTrack` drives an Animator, an `ActivationTrack` toggles a GameObject active, an `AudioTrack` " +
            "plays clips, a `SignalTrack` emits signals, a `ControlTrack` drives nested directors/particles, and a " +
            "`GroupTrack` organizes other tracks.\n\n" +
            "## Inputs\n\n" +
            "- `assetPath` — required path to the `.playable` TimelineAsset.\n" +
            "- `trackType` — required kind keyword or full TrackAsset type name.\n" +
            "- `trackName` — optional display name for the new track.\n" +
            "- `parentGroupName` — optional name of an existing GroupTrack to nest the new track under.\n\n" +
            "## Behavior\n\n" +
            "Resolves the track type, creates the track via `TimelineAsset.CreateTrack`, parents it under the " +
            "named group when provided, saves the asset, and returns the new track's name, type and root index. " +
            "Runs on the Unity main thread.")]
        [Description("Adds a track (Animation/Activation/Audio/Signal/Control/Playable/Group or a full type name) to a TimelineAsset.")]
        public TrackAddResponse AddTrack
        (
            [Description("Assets-rooted path to the TimelineAsset (.playable).")]
            string assetPath,
            [Description("Track kind keyword (Animation/Activation/Audio/Signal/Control/Playable/Group) or full TrackAsset type name.")]
            string trackType,
            [Description("Optional display name for the new track.")]
            string? trackName = null,
            [Description("Optional name of an existing GroupTrack to nest the new track under.")]
            string? parentGroupName = null
        )
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));
            if (string.IsNullOrEmpty(trackType))
                throw new ArgumentNullException(nameof(trackType));

            return MainThread.Instance.Run(() =>
            {
                var timeline = LoadTimelineAsset(assetPath);
                var type = ResolveTrackType(trackType);

                TrackAsset? parent = null;
                if (!string.IsNullOrEmpty(parentGroupName))
                {
                    foreach (var t in timeline.GetOutputTracks())
                    {
                        if (t is GroupTrack && t.name == parentGroupName)
                        {
                            parent = t;
                            break;
                        }
                    }
                    if (parent == null)
                        throw new Exception(Error.TrackNotFound(parentGroupName!));
                }

                var name = string.IsNullOrEmpty(trackName) ? type.Name : trackName!;
                var track = timeline.CreateTrack(type, parent, name);

                SaveTimeline(timeline, track);

                int index = -1;
                var roots = timeline.GetRootTracks();
                int i = 0;
                foreach (var r in roots)
                {
                    if (r == track) { index = i; break; }
                    i++;
                }

                return new TrackAddResponse
                {
                    assetPath = assetPath.Replace('\\', '/'),
                    trackName = track.name,
                    trackType = track.GetType().Name,
                    rootIndex = index,
                    parentGroup = parent != null ? parent.name : null,
                    success = true
                };
            });
        }

        public class TrackAddResponse
        {
            [Description("Project path of the TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Name of the created track.")]
            public string trackName = string.Empty;

            [Description("Type name of the created track.")]
            public string trackType = string.Empty;

            [Description("Index of the track among the root tracks (-1 if nested under a group).")]
            public int rootIndex = -1;

            [Description("Name of the parent GroupTrack, or null.")]
            public string? parentGroup;

            [Description("Whether the operation succeeded.")]
            public bool success;
        }
    }
}
