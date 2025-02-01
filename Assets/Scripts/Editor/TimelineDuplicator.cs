using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Editor {
    // [CustomEditor(typeof(PlayableDirector))]
    public class TimelineDuplicator : UnityEditor.Editor {
        [MenuItem("CONTEXT/PlayableDirector/复制 Timeline（绑定原物体）")]
        public static void DuplicateTimeline(MenuCommand command) {
            PlayableDirector pd = (PlayableDirector)command.context;
            if (pd == null) {
                Debug.LogError("复制 Timeline 失败，PlayableDirector 组件为空");
                return;
            }

            TimelineAsset asset = pd.playableAsset as TimelineAsset;
            if (asset == null) {
                Debug.LogError("复制 Timeline 失败，PlayableDirector 组件不存在 PlayableAsset 资产");
                return;
            }

            string originalPath = AssetDatabase.GetAssetPath(asset);

            Debug.LogError($"复制 Timeline ({originalPath})");
        }

        [MenuItem("CONTEXT/PlayableDirector/复制 Timeline（绑定原物体）", validate = true)]
        public static bool DuplicateTimelineValidate(MenuCommand command) {
            PlayableDirector pd = (PlayableDirector)command.context;
            if (pd == null)
                return false;

            return pd.playableAsset != null;
        }
    }
}
