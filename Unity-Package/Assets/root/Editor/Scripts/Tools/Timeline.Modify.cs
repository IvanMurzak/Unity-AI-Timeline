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
using System.Linq;
using com.IvanMurzak.McpPlugin;
using Microsoft.Extensions.Logging;
using com.IvanMurzak.ReflectorNet.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Timeline
    {
        public const string ModifyToolId = "timeline-modify";

        [AiTool
        (
            ModifyToolId,
            Title = "Timeline / Modify Object",
            ReadOnlyHint = false,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [AiSkillDescription("Generic write: apply a `SerializedMember` diff to a Timeline object (the " +
            "`TimelineAsset`, a `TrackAsset`, or a clip's `PlayableAsset`) via ReflectorNet `TryModify`. Use " +
            "'timeline-get' first to inspect the structure. Remember: object *fields* must be supplied through the " +
            "`fields` channel and *properties* through `props` — there is no cross-fallback.")]
        [AiSkillBody("Modify a Timeline object by applying a `SerializedMember` diff via ReflectorNet. This is the " +
            "generic escape hatch for members not covered by the dedicated tools.\n\n" +
            "## Inputs\n\n" +
            "- `assetPath` — required `.playable` TimelineAsset path.\n" +
            "- `data` — the `SerializedMember` diff to apply. Put C# *fields* in the `fields` array and " +
            "*properties* in the `props` array; ReflectorNet resolves them on separate channels with no fallback.\n" +
            "- `trackName` / `trackIndex` — when set, target a track instead of the whole asset.\n" +
            "- `clipIndex` — when >= 0 (and a track is selected), target that clip's `PlayableAsset`.\n\n" +
            "## Behavior\n\n" +
            "Resolves the target, applies the diff via `Reflector.TryModify`, and on success marks the asset dirty " +
            "and saves. The applied logs are returned. Runs on the Unity main thread.")]
        [Description("Generic: apply a SerializedMember diff to a TimelineAsset/TrackAsset/clip PlayableAsset via ReflectorNet TryModify. Fields via 'fields', props via 'props'.")]
        public TimelineModifyResponse ModifyObject
        (
            [Description("Assets-rooted path to the TimelineAsset (.playable).")]
            string assetPath,
            [Description("The SerializedMember diff to apply. Fields go in 'fields', properties in 'props'.")]
            SerializedMember data,
            [Description("Optional track name to target a track instead of the asset.")]
            string? trackName = null,
            [Description("Root-track index used when trackName is null and a track target is wanted (-1 = asset only).")]
            int trackIndex = -1,
            [Description("Clip index to target that clip's PlayableAsset (-1 = the track/asset itself).")]
            int clipIndex = -1
        )
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentNullException(nameof(assetPath));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return MainThread.Instance.Run(() =>
            {
                var timeline = LoadTimelineAsset(assetPath);
                var target = ResolveTimelineTarget(timeline, trackName, trackIndex, clipIndex, out var targetKind);

                var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception(Error.ReflectorNotAvailable());
                var logger = UnityLoggerFactory.LoggerFactory.CreateLogger<Tool_Timeline>();

                var response = new TimelineModifyResponse
                {
                    assetPath = assetPath.Replace('\\', '/'),
                    targetKind = targetKind,
                    targetType = target.GetType().FullName ?? target.GetType().Name
                };

                var logs = new List<string>();
                var modifyLogs = new Logs();
                object? boxed = target;
                if (reflector.TryModify(ref boxed, data, logs: modifyLogs, logger: logger))
                {
                    response.success = true;
                    logs.Add("Object modified successfully.");
                    EditorUtility.SetDirty(target);
                    EditorUtility.SetDirty(timeline);
                    AssetDatabase.SaveAssets();
                    com.IvanMurzak.Unity.MCP.Editor.Utils.EditorUtils.RepaintAllEditorWindows();
                }
                else
                {
                    logs.Add("No modifications were made.");
                }
                logs.AddRange(modifyLogs.Select(l => l.ToString()));

                response.logs = logs.ToArray();
                return response;
            });
        }

        public class TimelineModifyResponse
        {
            [Description("Whether the modification was successful.")]
            public bool success;

            [Description("Project path of the TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Kind of the modified target: TimelineAsset, TrackAsset, or ClipAsset.")]
            public string targetKind = string.Empty;

            [Description("Full type name of the modified target.")]
            public string targetType = string.Empty;

            [Description("Log of modifications and any warnings/errors.")]
            public string[]? logs;
        }
    }
}
