using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using CustomTimeline.TextNumberRoll;

namespace CustomTimeline {
    [TrackClipType(typeof(TextNumberRollClip))]
    [TrackColor(0.48f, 0.48f, 0.48f)]
    [TrackBindingType(typeof(TextNumberRoll.TextNumberRoll))]
    public class TextNumberRollTrack : TrackAsset {
        public TextMeshProUGUI text;

        public TextNumberRollClip clipAsset;

        [Button]
        private void Test() {
            Debug.LogError("123");
        }
        
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount) {
            foreach (TimelineClip clip in GetClips()) {
                // ReSharper disable once UseNegatedPatternMatching
                TextNumberRollClip textClip = clip.asset as TextNumberRollClip;
                if (ReferenceEquals(textClip, null))
                    continue;
                textClip.start = clip.start;
                textClip.end = clip.end;
            }

            return ScriptPlayable<TextNumberRollMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
