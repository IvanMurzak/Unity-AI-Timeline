/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
#if UNITY_6000_5_OR_NEWER
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
        public const string DirectorBindToolId = "timeline-director-bind";

        [AiTool
        (
            DirectorBindToolId,
            Title = "Timeline / Bind PlayableDirector",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Bind a `TimelineAsset` to a scene GameObject's `PlayableDirector` (adding the " +
            "component if missing). Optionally set the play-on-awake flag. Returns the director's GameObject and " +
            "instanceId.")]
        [AiSkillBody("Assign a `TimelineAsset` as the playable of a scene `PlayableDirector` so the timeline can be " +
            "played at runtime/in the editor. If the GameObject has no `PlayableDirector`, one is added.\n\n" +
            "## Inputs\n\n" +
            "- `gameObjectRef` — the scene GameObject to host/own the `PlayableDirector` (required).\n" +
            "- `assetPath` — the `.playable` TimelineAsset to bind (required).\n" +
            "- `playOnAwake` — optional flag to set `PlayableDirector.playOnAwake`.\n\n" +
            "## Behavior\n\n" +
            "Resolves the GameObject, ensures a `PlayableDirector`, sets `playableAsset` to the loaded " +
            "`TimelineAsset`, applies `playOnAwake` when provided, marks the scene dirty, and returns the director " +
            "GameObject reference + instanceId. Runs on the Unity main thread.")]
        [Description("Binds a TimelineAsset to a GameObject's PlayableDirector (adds it if missing), optionally sets playOnAwake.")]
        public DirectorBindResponse BindDirector
        (
            [Description("Reference to the scene GameObject that should own the PlayableDirector.")]
            GameObjectRef gameObjectRef,
            [Description("Assets-rooted path to the TimelineAsset (.playable) to bind.")]
            string assetPath,
            [Description("Optional value for PlayableDirector.playOnAwake.")]
            bool? playOnAwake = null
        )
        {
            if (gameObjectRef == null)
                throw new ArgumentNullException(nameof(gameObjectRef));
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            return MainThread.Instance.Run(() =>
            {
                var go = ResolveGameObject(gameObjectRef, nameof(gameObjectRef));
                var timeline = LoadTimelineAsset(assetPath);

                var director = go.GetComponent<PlayableDirector>();
                if (director == null)
                    director = go.AddComponent<PlayableDirector>();

                director.playableAsset = timeline;
                if (playOnAwake.HasValue)
                    director.playOnAwake = playOnAwake.Value;

                EditorUtility.SetDirty(director);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
                com.IvanMurzak.Unity.MCP.Editor.Utils.EditorUtils.RepaintAllEditorWindows();

                return new DirectorBindResponse
                {
                    gameObjectRef = new GameObjectRef(go),
                    directorRef = new ComponentRef(director),
                    instanceId = go.GetEntityId(),
                    assetPath = assetPath.Replace('\\', '/'),
                    playOnAwake = director.playOnAwake,
                    success = true
                };
            });
        }

        public class DirectorBindResponse
        {
            [Description("Reference to the GameObject hosting the PlayableDirector.")]
            public GameObjectRef? gameObjectRef;

            [Description("Reference to the PlayableDirector component.")]
            public ComponentRef? directorRef;

            [Description("Instance id of the director GameObject.")]
            public UnityEngine.EntityId instanceId;

            [Description("Project path of the bound TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Resulting playOnAwake flag.")]
            public bool playOnAwake;

            [Description("Whether the operation succeeded.")]
            public bool success;
        }
    }
}
#endif
