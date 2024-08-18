using System.Reflection;
using CustomTimeline.TextNumberRoll;
using DefaultNamespace.Extension;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DefaultNamespace.CustomTimeline.PlayableDirectorModifier {
    [RequireComponent(typeof(PlayableDirector))]
    public class PlayableDirectorModifier : MonoBehaviour {
        private PlayableDirector _playableDirector;

        private void Awake() {
            _playableDirector = GetComponent<PlayableDirector>();
        }

        private void OnValidate() {
            _playableDirector = GetComponent<PlayableDirector>();
        }

        [Button]
        private void Test() {
#if UNITY_EDITOR
            TimelineAsset timelineAsset = _playableDirector.playableAsset as TimelineAsset;
            if (ReferenceEquals(timelineAsset, null))
                return;
            int index = 0;
            foreach (TrackAsset track in timelineAsset.GetOutputTracks()) {
                TextNumberRollTrack textNumberRollTrack = track as TextNumberRollTrack;
                if (ReferenceEquals(textNumberRollTrack, null))
                    continue;
                foreach (TimelineClip clip in textNumberRollTrack.GetClips()) {
                    TextNumberRollClip textNumberRollClip = clip.asset as TextNumberRollClip;
                    if (ReferenceEquals(textNumberRollClip, null))
                        continue;
                    index++;
                    clip.SetPostExtrapolationMode(TimelineClip.ClipExtrapolation.Hold);
                    clip.SetPreExtrapolationMode(TimelineClip.ClipExtrapolation.Hold);
                    clip.displayName = $"clip_{index}";
                    UpdateClip(textNumberRollClip);
                }
            }

            EditorUtility.SetDirty(timelineAsset);
            AssetDatabase.SaveAssetIfDirty(timelineAsset);
            Debug.LogFormat("{0} saved.", timelineAsset.name);
#endif
        }


        private static void UpdateClip(TextNumberRollClip clip) {
            clip.template.numberTo = 1000;
//            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
//            // var extrapolationFieldInfo = typeof(TimelineClip).GetField("m_PostExtrapolationMode", flags);
//            var extrapolationFieldInfo = typeof(PlayableAsset).GetField("m_PostExtrapolationMode", flags);
//            if (!ReferenceEquals(extrapolationFieldInfo, null)) {
//                extrapolationFieldInfo.SetValue(clip, TimelineClip.ClipExtrapolation.Hold);
//            }
        }
    }
}
