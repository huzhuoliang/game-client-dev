using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using CustomTimeline.Jump.Timeline;
using Sirenix.OdinInspector;

namespace CustomTimeline {
    [TrackColor(0.48f, 0.48f, 0.48f)]
    [TrackClipType(typeof(JumpClip))]
    // [TrackBindingType(typeof())]
    public class JumpTrack : TrackAsset {
        // public TextMeshProUGUI text;

        [Button]
        private void Test() {
            
        }

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
            foreach (TimelineClip clip in GetClips()) {
                // ReSharper disable once UseNegatedPatternMatching
                JumpClip jumpClip = clip.asset as JumpClip;
                if (ReferenceEquals(jumpClip, null))
                    continue;
                jumpClip.start = clip.start;
                jumpClip.end = clip.end;
            }

            return ScriptPlayable<JumpBehaviour>.Create(graph, inputCount);
        }
    }
}
