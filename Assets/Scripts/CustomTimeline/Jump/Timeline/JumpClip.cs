using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace CustomTimeline.Jump.Timeline {
    [Serializable]
    [HideMonoScript]
    public class JumpClip : PlayableAsset, ITimelineClipAsset, IPropertyPreview {
        public JumpBehaviour template = new();

        [HideInInspector]
        public double start;

        [HideInInspector]
        public double end;

        public ClipCaps clipCaps => ClipCaps.None;

        [Button]
        private void Test() {
            
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
            template.Start = start;
            template.End = end;
            return ScriptPlayable<JumpBehaviour>.Create(graph, template);
        }

        public void GatherProperties(PlayableDirector director, IPropertyCollector driver) {
        }
    }
}
