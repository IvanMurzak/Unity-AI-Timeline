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
using System.Collections;
using com.IvanMurzak.ReflectorNet.Model;
using AIGD;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Timeline.Editor.Tests
{
    public class TestTimelineGeneric : BaseTest
    {
        [UnityTest]
        public IEnumerator Get_SerializesTimelineAsset()
        {
            var path = NewTimelinePath("GetAsset");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);

            var result = tool.GetObject(assetPath: path);
            Assert.IsNotNull(result.data, "Serialized data should not be null");
            Assert.AreEqual("TimelineAsset", result.targetKind, "Target kind should be TimelineAsset");
            StringAssert.Contains("TimelineAsset", result.targetType, "Type should be reported");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Get_SerializesTrack()
        {
            var path = NewTimelinePath("GetTrack");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);
            tool.AddTrack(assetPath: path, trackType: "Animation", trackName: "Anim");

            var result = tool.GetObject(assetPath: path, trackName: "Anim");
            Assert.AreEqual("TrackAsset", result.targetKind, "Target kind should be TrackAsset");
            StringAssert.Contains("AnimationTrack", result.targetType, "Track type should be reported");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Modify_ControlPlayableAsset_UpdateParticle_ViaFieldsChannel()
        {
            var path = NewTimelinePath("ModifyFields");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);
            tool.AddTrack(assetPath: path, trackType: "Control", trackName: "Control");
            // A ControlTrack's default clip is a ControlPlayableAsset.
            tool.AddClip(assetPath: path, trackName: "Control");

            // updateParticle is a public *field* on ControlPlayableAsset (default true), so it must be
            // supplied through the 'fields' channel (AddField). ReflectorNet's TryModify resolves
            // 'props' as PropertyInfo only and 'fields' as FieldInfo only — no cross-fallback.
            var reflector = UnityMcpPluginEditor.Instance.Reflector ?? throw new Exception("Reflector not available.");
            var diff = SerializedMember.FromValue(
                    reflector: reflector,
                    name: nameof(ControlPlayableAsset),
                    type: typeof(ControlPlayableAsset),
                    value: null)
                .AddField(SerializedMember.FromValue(
                    reflector: reflector,
                    name: "updateParticle",
                    value: false));

            var result = tool.ModifyObject(assetPath: path, data: diff, trackName: "Control", clipIndex: 0);
            Assert.IsTrue(result.success, "Modify should succeed");

            // Verify the field actually changed on the underlying asset.
            var timeline = UnityEditor.AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            ControlPlayableAsset? control = null;
            foreach (var track in timeline!.GetOutputTracks())
                foreach (var clip in track.GetClips())
                    control = clip.asset as ControlPlayableAsset;

            Assert.IsNotNull(control, "ControlPlayableAsset should exist");
            Assert.IsFalse(control!.updateParticle, "updateParticle field should be modified to false via the fields channel");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ModifyJson_ControlPlayableAsset_FieldsChannel_Dispatch()
        {
            var path = NewTimelinePath("ModifyJson");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);
            tool.AddTrack(assetPath: path, trackType: "Control", trackName: "Control");
            tool.AddClip(assetPath: path, trackName: "Control");

            var json = $@"{{
                ""assetPath"": ""{path}"",
                ""trackName"": ""Control"",
                ""clipIndex"": 0,
                ""data"": {{
                    ""typeName"": ""UnityEngine.Timeline.ControlPlayableAsset"",
                    ""fields"": [
                        {{
                            ""name"": ""updateParticle"",
                            ""typeName"": ""System.Boolean"",
                            ""value"": false
                        }}
                    ]
                }}
            }}";

            var result = RunToolAllowWarnings(Tool_Timeline.ModifyToolId, json);
            Assert.IsNotNull(result, "Result should not be null");

            yield return null;
        }
    }
}
