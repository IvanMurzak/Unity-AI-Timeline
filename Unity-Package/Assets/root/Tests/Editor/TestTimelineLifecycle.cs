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
using System.Collections;
using AIGD;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.TestTools;
using UnityEngine.Timeline;

namespace com.IvanMurzak.Unity.MCP.Timeline.Editor.Tests
{
    public class TestTimelineLifecycle : BaseTest
    {
        [UnityTest]
        public IEnumerator Create_TimelineAsset_OnDisk()
        {
            var path = NewTimelinePath("Intro");
            var tool = new Tool_Timeline();
            var result = tool.Create(assetPath: path, frameRate: 30.0, durationMode: "FixedLength", fixedDuration: 5.0);

            Assert.IsTrue(result.success, "Create should succeed");
            Assert.AreEqual(path, result.assetPath, "Returned path should match");
            Assert.IsFalse(string.IsNullOrEmpty(result.guid), "GUID should be set");

            var asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            Assert.IsNotNull(asset, "TimelineAsset should exist on disk");
            Assert.AreEqual(30.0, asset!.editorSettings.frameRate, "Frame rate should be applied");
            Assert.AreEqual(TimelineAsset.DurationMode.FixedLength, asset.durationMode, "Duration mode should be FixedLength");

            yield return null;
        }

        [UnityTest]
        public IEnumerator AddTracks_OfEachKind()
        {
            var path = NewTimelinePath("Tracks");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);

            var anim = tool.AddTrack(assetPath: path, trackType: "Animation", trackName: "Anim");
            var activation = tool.AddTrack(assetPath: path, trackType: "Activation", trackName: "Activation");
            var audio = tool.AddTrack(assetPath: path, trackType: "Audio", trackName: "Audio");
            var signal = tool.AddTrack(assetPath: path, trackType: "Signal", trackName: "Signal");
            var control = tool.AddTrack(assetPath: path, trackType: "Control", trackName: "Control");

            Assert.IsTrue(anim.success && activation.success && audio.success && signal.success && control.success,
                "All track adds should succeed");
            StringAssert.Contains("AnimationTrack", anim.trackType, "Animation track type");
            StringAssert.Contains("ControlTrack", control.trackType, "Control track type");

            var list = tool.ListTracks(assetPath: path);
            Assert.AreEqual(5, list.count, "Timeline should have 5 tracks");

            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveTrack_ByName()
        {
            var path = NewTimelinePath("Remove");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);
            tool.AddTrack(assetPath: path, trackType: "Animation", trackName: "A");
            tool.AddTrack(assetPath: path, trackType: "Audio", trackName: "B");

            var result = tool.RemoveTrack(assetPath: path, trackName: "A");
            Assert.IsTrue(result.success, "Remove should succeed");
            Assert.AreEqual("A", result.removedTrackName, "Removed track name should match");
            Assert.AreEqual(1, result.remainingRootTrackCount, "One root track should remain");

            yield return null;
        }

        [UnityTest]
        public IEnumerator AddClip_AndSetTiming_AndMove()
        {
            var path = NewTimelinePath("Clips");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);
            tool.AddTrack(assetPath: path, trackType: "Activation", trackName: "Act");

            var add = tool.AddClip(assetPath: path, trackName: "Act", start: 1.0, duration: 3.0, displayName: "Clip0");
            Assert.IsTrue(add.success, "AddClip should succeed");
            Assert.AreEqual(0, add.clipIndex, "First clip index is 0");
            Assert.AreEqual(1.0, add.start, 0.0001, "Start should be 1.0");
            Assert.AreEqual(3.0, add.duration, 0.0001, "Duration should be 3.0");

            // Note: an ActivationPlayableAsset has ClipCaps.None, so blend/ease/clipIn all clamp to 0.
            // start and duration are always directly settable.
            var timing = tool.SetClipTiming(assetPath: path, clipIndex: 0, trackName: "Act",
                start: 0.5, duration: 4.0);
            Assert.IsTrue(timing.success, "SetClipTiming should succeed");
            Assert.AreEqual(4.0, timing.duration, 0.0001, "Duration should be updated to 4.0");
            Assert.AreEqual(0.5, timing.start, 0.0001, "Start should be updated to 0.5");

            var move = tool.MoveClip(assetPath: path, clipIndex: 0, trackName: "Act", deltaSeconds: 2.0);
            Assert.IsTrue(move.success, "MoveClip should succeed");
            Assert.AreEqual(2.5, move.newStart, 0.0001, "Start should move from 0.5 to 2.5");
            Assert.AreEqual(4.0, move.duration, 0.0001, "Duration preserved on move");

            yield return null;
        }

        [UnityTest]
        public IEnumerator AddMarker_OnMarkerTrack()
        {
            var path = NewTimelinePath("Markers");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);

            var result = tool.AddMarker(assetPath: path, time: 2.5, markerType: "Signal");
            Assert.IsTrue(result.success, "AddMarker should succeed");
            StringAssert.Contains("SignalEmitter", result.markerType, "Marker should be a SignalEmitter");
            Assert.AreEqual(2.5, result.time, 0.0001, "Marker time should be 2.5");

            yield return null;
        }

        [UnityTest]
        public IEnumerator BindDirector_AddsPlayableDirector()
        {
            var path = NewTimelinePath("Bind");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);

            var go = new GameObject("DirectorHost");
            var result = tool.BindDirector(
                gameObjectRef: new GameObjectRef(go.GetInstanceID()),
                assetPath: path,
                playOnAwake: true);

            Assert.IsTrue(result.success, "BindDirector should succeed");
            var director = go.GetComponent<PlayableDirector>();
            Assert.IsNotNull(director, "PlayableDirector should be added");
            Assert.IsNotNull(director!.playableAsset, "Director should have a playable asset bound");
            Assert.IsTrue(director.playOnAwake, "playOnAwake should be set");

            yield return null;
        }

        [UnityTest]
        public IEnumerator BindTrack_BindsAnimatorToAnimationTrack()
        {
            var path = NewTimelinePath("TrackBind");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);
            tool.AddTrack(assetPath: path, trackType: "Animation", trackName: "Anim");

            var dirGo = new GameObject("DirectorHost");
            tool.BindDirector(new GameObjectRef(dirGo.GetInstanceID()), path);

            var targetGo = new GameObject("AnimTarget");
            targetGo.AddComponent<Animator>();

            var result = tool.BindTrack(
                directorRef: new GameObjectRef(dirGo.GetInstanceID()),
                trackName: "Anim",
                targetRef: new GameObjectRef(targetGo.GetInstanceID()));

            Assert.IsTrue(result.success, "BindTrack should succeed");
            StringAssert.Contains("Animator", result.boundType, "Should bind the Animator component");

            yield return null;
        }

        [UnityTest]
        public IEnumerator List_FindsCreatedTimeline()
        {
            var path = NewTimelinePath("Listed");
            var tool = new Tool_Timeline();
            tool.Create(assetPath: path);

            var result = tool.List(includeTrackCount: true);
            Assert.GreaterOrEqual(result.count, 1, "At least one TimelineAsset should be found");

            yield return null;
        }
    }
}
