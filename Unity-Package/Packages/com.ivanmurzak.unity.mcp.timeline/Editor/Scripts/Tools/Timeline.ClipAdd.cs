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
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Timeline
    {
        public const string ClipAddToolId = "timeline-clip-add";

        [AiTool
        (
            ClipAddToolId,
            Title = "Timeline / Add Clip",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Add a clip to a track on a `TimelineAsset`. For AnimationTracks pass an " +
            "`animationClipPath`; for AudioTracks pass an `audioClipPath`; otherwise a default clip is created. " +
            "Optionally set start, duration and display name.")]
        [AiSkillBody("Add a clip to a track. The clip type depends on the track: an `AnimationTrack` hosts an " +
            "`AnimationClip`, an `AudioTrack` hosts an `AudioClip`, and other tracks get a default clip. When no " +
            "source asset is supplied a default (empty) clip is created.\n\n" +
            "## Inputs\n\n" +
            "- `assetPath` — required path to the `.playable` TimelineAsset.\n" +
            "- `trackName` / `trackIndex` — which track to add the clip to (name wins; index default 0).\n" +
            "- `animationClipPath` — optional Assets path to an `AnimationClip` (AnimationTrack only).\n" +
            "- `audioClipPath` — optional Assets path to an `AudioClip` (AudioTrack only).\n" +
            "- `start` — optional clip start time in seconds (default 0).\n" +
            "- `duration` — optional clip duration in seconds; when > 0 overrides the default duration.\n" +
            "- `displayName` — optional clip display name.\n\n" +
            "## Behavior\n\n" +
            "Resolves the track, creates the appropriate clip (typed when a source asset is given, else a default " +
            "clip), applies start/duration/name, saves the asset, and returns the new clip's index and timing. " +
            "Runs on the Unity main thread.")]
        [Description("Adds a clip to a Timeline track (animation/audio/default) with optional start, duration and name.")]
        public ClipAddResponse AddClip
        (
            [Description("Assets-rooted path to the TimelineAsset (.playable).")]
            string assetPath,
            [Description("Name of the target track. Takes precedence over trackIndex when provided.")]
            string? trackName = null,
            [Description("Zero-based root-track index, used when trackName is null/empty.")]
            int trackIndex = 0,
            [Description("Optional Assets path to an AnimationClip (used for AnimationTracks).")]
            string? animationClipPath = null,
            [Description("Optional Assets path to an AudioClip (used for AudioTracks).")]
            string? audioClipPath = null,
            [Description("Clip start time in seconds (default 0).")]
            double start = 0.0,
            [Description("Clip duration in seconds; when > 0 overrides the default duration.")]
            double duration = 0.0,
            [Description("Optional clip display name.")]
            string? displayName = null
        )
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            return MainThread.Instance.Run(() =>
            {
                var timeline = LoadTimelineAsset(assetPath);
                var track = ResolveTrack(timeline, trackName, trackIndex);

                TimelineClip clip;
                if (track is AnimationTrack animTrack && !string.IsNullOrEmpty(animationClipPath))
                {
                    var animClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationClipPath!.Replace('\\', '/'));
                    if (animClip == null)
                        throw new Exception($"[Error] AnimationClip not found at '{animationClipPath}'.");
                    clip = animTrack.CreateClip(animClip);
                }
                else if (track is AudioTrack audioTrack && !string.IsNullOrEmpty(audioClipPath))
                {
                    var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath!.Replace('\\', '/'));
                    if (audioClip == null)
                        throw new Exception($"[Error] AudioClip not found at '{audioClipPath}'.");
                    clip = audioTrack.CreateClip(audioClip);
                }
                else
                {
                    clip = track.CreateDefaultClip();
                }

                clip.start = start;
                if (duration > 0.0)
                    clip.duration = duration;
                if (!string.IsNullOrEmpty(displayName))
                    clip.displayName = displayName!;

                SaveTimeline(timeline, track);

                int index = -1;
                int i = 0;
                foreach (var c in track.GetClips())
                {
                    if (c == clip) { index = i; break; }
                    i++;
                }

                return new ClipAddResponse
                {
                    assetPath = assetPath.Replace('\\', '/'),
                    trackName = track.name,
                    clipIndex = index,
                    displayName = clip.displayName,
                    start = clip.start,
                    duration = clip.duration,
                    end = clip.end,
                    assetType = clip.asset != null ? clip.asset.GetType().Name : null,
                    success = true
                };
            });
        }

        public class ClipAddResponse
        {
            [Description("Project path of the TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Name of the track the clip was added to.")]
            public string trackName = string.Empty;

            [Description("Index of the new clip on its track.")]
            public int clipIndex = -1;

            [Description("Display name of the clip.")]
            public string displayName = string.Empty;

            [Description("Clip start time in seconds.")]
            public double start;

            [Description("Clip duration in seconds.")]
            public double duration;

            [Description("Clip end time in seconds.")]
            public double end;

            [Description("Type name of the clip's PlayableAsset, or null.")]
            public string? assetType;

            [Description("Whether the operation succeeded.")]
            public bool success;
        }
    }
}
