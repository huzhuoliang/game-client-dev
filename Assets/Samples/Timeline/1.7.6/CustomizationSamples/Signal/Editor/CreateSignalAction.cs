using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.Timeline;

// ReSharper disable once CheckNamespace
namespace Timeline.Samples {
    // ReSharper disable once UnusedType.Global
    [MenuEntry("Create Timeline Event")]
    public class CreateSignalAction : TimelineAction {
        public override bool Execute(ActionContext context) {
            double time;
            if (context.invocationTime.HasValue) {
                time = context.invocationTime.Value;
            } else {
                time = TimelineEditor.inspectedDirector.time;
            }

            if (time < 0f) {
                return false;
            }

            IEnumerable<TrackAsset> selectedTracks = context.tracks;
            foreach (TrackAsset track in selectedTracks) {
                GroupTrack groupTrack = track as GroupTrack;
                if (!ReferenceEquals(groupTrack, null)) {
                    continue;
                }

                if (!track.supportsNotifications) {
                    continue;
                }

                SignalEmitter signalEmitter = track.CreateMarker<SignalEmitter>(time);
                SignalAsset signalAsset = ScriptableObject.CreateInstance<SignalAsset>();
                signalAsset.name = "signal";
                signalEmitter.asset = signalAsset;
                AssetDatabase.AddObjectToAsset(signalAsset, context.timeline);
                AssetDatabase.SaveAssets();
            }

            return true;
        }

        public override ActionValidity Validate(ActionContext context) {
            bool any = false;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (TrackAsset track in context.tracks) {
                // ReSharper disable once InvertIf
                if (track.supportsNotifications) {
                    any = true;
                    break;
                }
            }

            return any ? ActionValidity.Valid : ActionValidity.NotApplicable;
        }
    }
}
