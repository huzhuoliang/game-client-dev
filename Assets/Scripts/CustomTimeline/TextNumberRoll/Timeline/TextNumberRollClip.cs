using System;
using Timeline.Samples;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CustomTimeline.TextNumberRoll {
    [Serializable]
    public class TextNumberRollClip : PlayableAsset, ITimelineClipAsset, IPropertyPreview {
        
        [NoFoldOut]
        public TextNumberRollBehaviour template = new();

        [HideInInspector]
        public double start;
        [HideInInspector]
        public double end;

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Extrapolation;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
            template.Start = start;
            template.End = end;
            return ScriptPlayable<TextNumberRollBehaviour>.Create(graph, template);
        }

        public void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
            // driver.AddFromName<TextMeshProUGUI>("m_text");
        }
    }
}
