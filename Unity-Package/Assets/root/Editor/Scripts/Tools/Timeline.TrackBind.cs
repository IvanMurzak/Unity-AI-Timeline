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
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Timeline
    {
        public const string TrackBindToolId = "timeline-track-bind";

        [AiTool
        (
            TrackBindToolId,
            Title = "Timeline / Bind Track",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Bind a scene object to a Timeline output track via a `PlayableDirector` generic " +
            "binding — e.g. point an AnimationTrack at an `Animator`, or an ActivationTrack at a GameObject. The " +
            "director must already play the timeline that owns the track.")]
        [AiSkillBody("Set the scene-object binding for a specific output track on a `PlayableDirector`. The " +
            "director's `playableAsset` must be the `TimelineAsset` that owns the track. For an `AnimationTrack` " +
            "the binding is typically an `Animator`; for an `ActivationTrack`/`AudioTrack` it is the GameObject / " +
            "AudioSource that the track drives.\n\n" +
            "## Inputs\n\n" +
            "- `directorRef` — GameObject hosting the `PlayableDirector` that plays the timeline (required).\n" +
            "- `trackName` / `trackIndex` — which output track to bind (name wins; index default 0).\n" +
            "- `targetRef` — the scene GameObject to bind to the track. When the track expects a `Component` " +
            "(e.g. Animator/AudioSource), the matching component on this GameObject is used; otherwise the " +
            "GameObject itself is bound.\n" +
            "- `clearBinding` — when true, clears the binding instead of setting it (ignored if `targetRef` is given).\n\n" +
            "## Behavior\n\n" +
            "Resolves the director and its timeline, finds the track, computes the binding object (component when " +
            "the track output type is a Component, else the GameObject), calls `SetGenericBinding`, marks the " +
            "scene dirty, and returns the bound object's name. Runs on the Unity main thread.")]
        [Description("Binds a scene object/component to a Timeline output track through a PlayableDirector generic binding.")]
        public TrackBindResponse BindTrack
        (
            [Description("Reference to the GameObject hosting the PlayableDirector that plays the timeline.")]
            GameObjectRef directorRef,
            [Description("Name of the output track to bind. Takes precedence over trackIndex.")]
            string? trackName = null,
            [Description("Zero-based root-track index, used when trackName is null/empty.")]
            int trackIndex = 0,
            [Description("Scene GameObject to bind to the track (its matching component is used when the track expects one).")]
            GameObjectRef? targetRef = null,
            [Description("If true, clears the track binding (ignored when targetRef is provided).")]
            bool clearBinding = false
        )
        {
            if (directorRef == null)
                throw new ArgumentNullException(nameof(directorRef));

            return MainThread.Instance.Run(() =>
            {
                var directorGo = ResolveGameObject(directorRef, nameof(directorRef));
                var director = directorGo.GetComponent<PlayableDirector>();
                if (director == null)
                    throw new Exception(Error.PlayableDirectorNotFound());

                var timeline = director.playableAsset as TimelineAsset;
                if (timeline == null)
                    throw new Exception("[Error] The PlayableDirector is not playing a TimelineAsset. Bind one with 'timeline-director-bind' first.");

                var track = ResolveTrack(timeline, trackName, trackIndex);

                UnityEngine.Object? binding = null;
                var targetGo = ResolveOptionalGameObject(targetRef, nameof(targetRef));
                if (targetGo != null)
                {
                    var outputType = track.outputs != null ? GetFirstOutputType(track) : null;
                    if (outputType != null && typeof(UnityEngine.Component).IsAssignableFrom(outputType))
                    {
                        var component = targetGo.GetComponent(outputType);
                        binding = component != null ? component : (UnityEngine.Object)targetGo;
                    }
                    else
                    {
                        binding = targetGo;
                    }
                }
                else if (!clearBinding)
                {
                    throw new ArgumentException("Provide a targetRef to bind, or set clearBinding=true.", nameof(targetRef));
                }

                director.SetGenericBinding(track, binding);

                EditorUtility.SetDirty(director);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(directorGo.scene);
                com.IvanMurzak.Unity.MCP.Editor.Utils.EditorUtils.RepaintAllEditorWindows();

                return new TrackBindResponse
                {
                    directorRef = new GameObjectRef(directorGo),
                    trackName = track.name,
                    boundTo = binding != null ? binding.name : null,
                    boundType = binding != null ? binding.GetType().Name : null,
                    success = true
                };
            });
        }

        /// <summary>Get the binding type the track's first output expects, or null.</summary>
        static Type? GetFirstOutputType(TrackAsset track)
        {
            foreach (var output in track.outputs)
                return output.outputTargetType;
            return null;
        }

        public class TrackBindResponse
        {
            [Description("Reference to the GameObject hosting the PlayableDirector.")]
            public GameObjectRef? directorRef;

            [Description("Name of the bound track.")]
            public string trackName = string.Empty;

            [Description("Name of the bound object, or null when cleared.")]
            public string? boundTo;

            [Description("Type name of the bound object, or null.")]
            public string? boundType;

            [Description("Whether the operation succeeded.")]
            public bool success;
        }
    }
}
