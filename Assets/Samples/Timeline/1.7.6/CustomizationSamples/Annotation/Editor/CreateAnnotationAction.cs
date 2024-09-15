using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.Timeline.Actions;
using UnityEngine.Timeline;

// ReSharper disable once CheckNamespace
namespace Timeline.Samples {
    // Adds an item in context menus that will create a new annotation
    // and sets its description field with the clipboard's contents.
    // ReSharper disable once UnusedType.Global
    [MenuEntry("Create Annotation from clipboard contents")]
    public class CreateAnnotationAction : TimelineAction {
        // Specifies the action's prerequisites:
        // - Invalid (grayed out in the menu) if no text content is in the clipboard;
        // - NotApplicable (not shown in the menu) if no track is selected;
        // - Valid (shown in the menu) otherwise.
        public override ActionValidity Validate(ActionContext context) {
            // get the current text content of the clipboard
            string clipboardTextContent = EditorGUIUtility.systemCopyBuffer;
            if (clipboardTextContent.Length == 0) {
                return ActionValidity.Invalid;
            }

            double time;
            if (context.invocationTime.HasValue) {
                time = context.invocationTime.Value;
            } else {
                time = TimelineEditor.inspectedDirector.time;
            }

            if (time < 0f) {
                return ActionValidity.NotApplicable;
            }


            // Timeline's current selected items can be fetched with `context`
            IEnumerable<TrackAsset> selectedTracks = context.tracks;
            bool any = false;
            bool all = true;
            foreach (TrackAsset track in selectedTracks) {
                any = true;
                GroupTrack groupTrack = track as GroupTrack;
                // ReSharper disable once InvertIf
                if (ReferenceEquals(groupTrack, null)) {
                    all = false;
                    break;
                }
            }

            if (!any || all) {
                return ActionValidity.NotApplicable;
            }

            return ActionValidity.Valid;
        }

        // Creates a new annotation and add it to the selected track.
        public override bool Execute(ActionContext context) {
            // to find at which time to create a new marker, we need to consider how this action was invoked.
            // If the action was invoked by a context menu item, then we can use the context's invocation time.
            // If the action was invoked through a keyboard shortcut, we can use Timeline's playhead time instead.
            double time;
            if (context.invocationTime.HasValue) {
                time = context.invocationTime.Value;
            } else {
                time = TimelineEditor.inspectedDirector.time;
            }

            if (time < 0f) {
                return false;
            }

            string clipboardTextContent = EditorGUIUtility.systemCopyBuffer;

            IEnumerable<TrackAsset> selectedTracks = context.tracks;
            foreach (TrackAsset track in selectedTracks) {
                GroupTrack groupTrack = track as GroupTrack;
                if (!ReferenceEquals(groupTrack, null)) {
                    continue;
                }

                // NOTATION track.CreateMarker will not call MarkerEditor.OnCreate
                AnnotationMarker annotation = track.CreateMarker<AnnotationMarker>(time);
                annotation.description = clipboardTextContent;
                annotation.title = "Annotation from clipboard";
                annotation.name = "Annotation";
            }

            return true;
        }
    }
}
