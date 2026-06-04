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
using UnityEditor;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Timeline
    {
        public const string ListToolId = "timeline-list";

        [AiTool
        (
            ListToolId,
            Title = "Timeline / List TimelineAssets",
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false
        )]
        [AiSkillDescription("List every `TimelineAsset` in the project with its path, GUID, track count and " +
            "duration. Read-only.")]
        [AiSkillBody("Search the project AssetDatabase for `TimelineAsset` (.playable) assets and report each " +
            "one's path, GUID, track count, and computed duration.\n\n" +
            "## Inputs\n\n" +
            "- `includeTrackCount` (bool, default true) — when true, load each asset to count its output tracks; " +
            "when false, skip loading (faster, no counts).\n\n" +
            "## Behavior\n\n" +
            "Runs `AssetDatabase.FindAssets(\"t:TimelineAsset\")`, resolves each to a path, and (optionally) loads " +
            "it for track count + duration. Read-only. Runs on the Unity main thread.")]
        [Description("Lists all TimelineAssets in the project with path, GUID, track count and duration. Read-only.")]
        public ListResponse List
        (
            [Description("If true (default), load each asset to report its track count and duration; if false, skip loading.")]
            bool includeTrackCount = true
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var guids = AssetDatabase.FindAssets("t:TimelineAsset");
                var items = new List<ListItem>(guids.Length);
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var item = new ListItem { assetPath = path, guid = guid };
                    if (includeTrackCount)
                    {
                        var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
                        if (timeline != null)
                        {
                            int count = 0;
                            foreach (var _ in timeline.GetOutputTracks())
                                count++;
                            item.trackCount = count;
                            item.duration = timeline.duration;
                        }
                    }
                    items.Add(item);
                }

                return new ListResponse
                {
                    count = items.Count,
                    timelines = items.ToArray()
                };
            });
        }

        public class ListResponse
        {
            [Description("Number of TimelineAssets found.")]
            public int count;

            [Description("The TimelineAssets in the project.")]
            public ListItem[] timelines = Array.Empty<ListItem>();
        }

        public class ListItem
        {
            [Description("Project path of the TimelineAsset.")]
            public string assetPath = string.Empty;

            [Description("Asset GUID.")]
            public string guid = string.Empty;

            [Description("Number of output tracks (0 when includeTrackCount is false).")]
            public int trackCount;

            [Description("Computed duration in seconds (0 when includeTrackCount is false).")]
            public double duration;
        }
    }
}
