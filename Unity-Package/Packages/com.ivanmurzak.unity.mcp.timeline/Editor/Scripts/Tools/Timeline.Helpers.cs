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
using System;
using System.Collections.Generic;
using System.Linq;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Timeline
    {
        /// <summary>Validate an Assets-rooted TimelineAsset path (.playable).</summary>
        static void ValidateTimelinePath(string path)
        {
            var normalized = (path ?? string.Empty).Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) ||
                !normalized.EndsWith(".playable", StringComparison.OrdinalIgnoreCase))
                throw new Exception(Error.TimelineAssetPathInvalid(path ?? string.Empty));
        }

        /// <summary>Load an existing TimelineAsset by project path (throws on failure).</summary>
        static TimelineAsset LoadTimelineAsset(string assetPath)
        {
            ValidateTimelinePath(assetPath);
            var normalized = assetPath.Replace('\\', '/');
            var asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(normalized);
            if (asset == null)
                throw new Exception(Error.TimelineAssetNotFound(normalized));
            return asset;
        }

        /// <summary>Resolve a required GameObjectRef to its GameObject (throws on failure).</summary>
        static GameObject ResolveGameObject(GameObjectRef? gameObjectRef, string paramName)
        {
            if (gameObjectRef == null)
                throw new ArgumentNullException(paramName);
            if (!gameObjectRef.IsValid(out var validationError))
                throw new ArgumentException(validationError, paramName);

            var go = gameObjectRef.FindGameObject(out var error);
            if (error != null)
                throw new Exception(error);
            if (go == null)
                throw new Exception(Error.GameObjectNotFound());

            return go;
        }

        /// <summary>Resolve an optional GameObjectRef to its GameObject (null when ref is null/empty).</summary>
        static GameObject? ResolveOptionalGameObject(GameObjectRef? gameObjectRef, string paramName)
        {
            if (gameObjectRef == null || !gameObjectRef.IsValid(out _))
                return null;
            return ResolveGameObject(gameObjectRef, paramName);
        }

        /// <summary>
        /// Resolve a track-kind keyword OR a full TrackAsset type name to a concrete TrackAsset Type.
        /// Throws when the type cannot be resolved or does not derive from TrackAsset.
        /// </summary>
        static Type ResolveTrackType(string trackType)
        {
            if (string.IsNullOrWhiteSpace(trackType))
                throw new Exception(Error.TrackTypeNotResolved(trackType ?? string.Empty));

            switch (trackType.Trim().ToLowerInvariant())
            {
                case "animation": return typeof(AnimationTrack);
                case "activation": return typeof(ActivationTrack);
                case "audio": return typeof(AudioTrack);
                case "signal": return typeof(SignalTrack);
                case "control": return typeof(ControlTrack);
                case "playable": return typeof(PlayableTrack);
                case "group": return typeof(GroupTrack);
            }

            // Full type name fallback (search across loaded assemblies).
            var type = Type.GetType(trackType, throwOnError: false);
            if (type == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(trackType, throwOnError: false);
                    if (type != null)
                        break;
                    type = asm.GetTypes().FirstOrDefault(t =>
                        string.Equals(t.Name, trackType, StringComparison.Ordinal) &&
                        typeof(TrackAsset).IsAssignableFrom(t));
                    if (type != null)
                        break;
                }
            }

            if (type == null)
                throw new Exception(Error.TrackTypeNotResolved(trackType));
            if (!typeof(TrackAsset).IsAssignableFrom(type))
                throw new Exception(Error.NotATrackAsset(trackType));

            return type;
        }

        /// <summary>Find a root track on a TimelineAsset by name (case-sensitive), else by index when name is null.</summary>
        static TrackAsset ResolveTrack(TimelineAsset timeline, string? trackName, int trackIndex)
        {
            var roots = timeline.GetRootTracks().ToList();
            if (!string.IsNullOrEmpty(trackName))
            {
                var byName = roots.FirstOrDefault(t => t != null && t.name == trackName);
                if (byName == null)
                    throw new Exception(Error.TrackNotFound(trackName!));
                return byName;
            }
            if (trackIndex < 0 || trackIndex >= roots.Count)
                throw new Exception(Error.TrackIndexOutOfRange(trackIndex, roots.Count));
            return roots[trackIndex];
        }

        /// <summary>Persist edits to a TimelineAsset and the owning track to disk.</summary>
        static void SaveTimeline(TimelineAsset timeline, UnityEngine.Object? alsoDirty = null)
        {
            EditorUtility.SetDirty(timeline);
            if (alsoDirty != null)
                EditorUtility.SetDirty(alsoDirty);
            AssetDatabase.SaveAssets();
            com.IvanMurzak.Unity.MCP.Editor.Utils.EditorUtils.RepaintAllEditorWindows();
        }

        /// <summary>List clip summaries on a track.</summary>
        static List<ClipInfo> DescribeClips(TrackAsset track)
        {
            var clips = new List<ClipInfo>();
            int i = 0;
            foreach (var clip in track.GetClips())
            {
                clips.Add(new ClipInfo
                {
                    index = i++,
                    displayName = clip.displayName,
                    start = clip.start,
                    duration = clip.duration,
                    end = clip.end,
                    blendInDuration = clip.blendInDuration,
                    blendOutDuration = clip.blendOutDuration,
                    assetType = clip.asset != null ? clip.asset.GetType().Name : null
                });
            }
            return clips;
        }

        public class ClipInfo
        {
            [System.ComponentModel.Description("Zero-based index of the clip on its track.")]
            public int index;

            [System.ComponentModel.Description("Display name of the clip.")]
            public string displayName = string.Empty;

            [System.ComponentModel.Description("Clip start time in seconds.")]
            public double start;

            [System.ComponentModel.Description("Clip duration in seconds.")]
            public double duration;

            [System.ComponentModel.Description("Clip end time in seconds.")]
            public double end;

            [System.ComponentModel.Description("Blend-in duration in seconds.")]
            public double blendInDuration;

            [System.ComponentModel.Description("Blend-out duration in seconds.")]
            public double blendOutDuration;

            [System.ComponentModel.Description("Type name of the clip's PlayableAsset, or null.")]
            public string? assetType;
        }
    }
}
