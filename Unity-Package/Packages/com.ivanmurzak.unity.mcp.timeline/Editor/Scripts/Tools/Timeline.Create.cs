/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Timeline)       │
│  Copyright (c) 2025 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.ComponentModel;
using System.IO;
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
        public const string CreateToolId = "timeline-create";

        [AiTool
        (
            CreateToolId,
            Title = "Timeline / Create TimelineAsset",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = false,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Create a new empty `TimelineAsset` (.playable) at a project path. Optionally set the " +
            "frame rate and a fixed duration mode. Returns the created asset path and GUID.")]
        [AiSkillBody("Create a new `TimelineAsset` and save it to disk at an `Assets/`-rooted `.playable` path. A " +
            "`TimelineAsset` is the authoring container holding tracks, clips, and markers for a cutscene or " +
            "sequence; a scene `PlayableDirector` plays it.\n\n" +
            "## Inputs\n\n" +
            "- `assetPath` — required `Assets/...`-rooted path ending in `.playable`. Missing folders are created.\n" +
            "- `frameRate` — optional editor frame rate of the timeline (default 60).\n" +
            "- `durationMode` — optional `BasedOnClips` (default) or `FixedLength`.\n" +
            "- `fixedDuration` — optional duration in seconds, used when `durationMode` is `FixedLength`.\n\n" +
            "## Behavior\n\n" +
            "Creates the intermediate folders, instantiates a `TimelineAsset`, applies the editor settings, writes " +
            "it via `AssetDatabase.CreateAsset`, saves, and returns the path + GUID. Runs on the Unity main thread.")]
        [Description("Creates a new empty TimelineAsset (.playable) at the given project path with optional frame rate and duration mode.")]
        public CreateResponse Create
        (
            [Description("Assets-rooted path ending in '.playable' (e.g. 'Assets/Timelines/Intro.playable').")]
            string assetPath,
            [Description("Editor frame rate of the timeline (default 60).")]
            double frameRate = 60.0,
            [Description("Duration mode: 'BasedOnClips' (default) or 'FixedLength'.")]
            string durationMode = "BasedOnClips",
            [Description("Fixed duration in seconds, used only when durationMode is 'FixedLength'.")]
            double fixedDuration = 0.0
        )
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));

            return MainThread.Instance.Run(() =>
            {
                ValidateTimelinePath(assetPath);
                var normalized = assetPath.Replace('\\', '/');

                var dir = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                    CreateFolders(dir!);

                var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.editorSettings.frameRate = frameRate;

                if (string.Equals(durationMode, "FixedLength", StringComparison.OrdinalIgnoreCase))
                {
                    timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
                    if (fixedDuration > 0.0)
                        timeline.fixedDuration = fixedDuration;
                }
                else
                {
                    timeline.durationMode = TimelineAsset.DurationMode.BasedOnClips;
                }

                AssetDatabase.CreateAsset(timeline, normalized);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(normalized);
                com.IvanMurzak.Unity.MCP.Editor.Utils.EditorUtils.RepaintAllEditorWindows();

                return new CreateResponse
                {
                    assetPath = normalized,
                    guid = AssetDatabase.AssetPathToGUID(normalized),
                    frameRate = timeline.editorSettings.frameRate,
                    durationMode = timeline.durationMode.ToString(),
                    success = true
                };
            });
        }

        /// <summary>Recursively create an Assets-rooted folder chain.</summary>
        static void CreateFolders(string folder)
        {
            var parts = folder.Split('/');
            var current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public class CreateResponse
        {
            [Description("Project path of the created TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("GUID of the created asset.")]
            public string guid = string.Empty;

            [Description("Resolved editor frame rate of the timeline.")]
            public double frameRate;

            [Description("Resolved duration mode.")]
            public string durationMode = string.Empty;

            [Description("Whether the operation succeeded.")]
            public bool success;
        }
    }
}
