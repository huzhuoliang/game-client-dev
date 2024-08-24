using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace CustomTimeline.Jump.Timeline.Editor {
    /// <summary>
    /// JumpTrack 必须有这个脚本才能显示 Odin 的组件
    /// </summary>
    [CustomEditor(typeof(JumpTrack))]
    public class JumpTrackEditor : OdinEditor {
    }
}
