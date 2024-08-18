using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace CustomTimeline.TextNumberRoll.Timeline.Editor {
    /// <summary>
    /// TextNumberRollTrack 必须有这个脚本才能显示 Odin 的组件
    /// </summary>
    [CustomEditor(typeof(TextNumberRollTrack))]
    public class TextNumberRollTrackEditor : OdinEditor {
    }
}
