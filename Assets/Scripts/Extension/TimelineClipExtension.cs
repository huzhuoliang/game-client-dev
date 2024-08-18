using System.Reflection;
using UnityEngine.Timeline;

namespace DefaultNamespace.Extension {
    public static class TimelineClipExtension {
        public static void SetPreExtrapolationMode(this TimelineClip clip, TimelineClip.ClipExtrapolation extrapolation) {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo fieldInfo = typeof(TimelineClip).GetField("m_PreExtrapolationMode", flags);
            if (!ReferenceEquals(fieldInfo, null)) {
                fieldInfo.SetValue(clip, extrapolation);
            }
        }

        public static void SetPostExtrapolationMode(this TimelineClip clip, TimelineClip.ClipExtrapolation extrapolation) {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo fieldInfo = typeof(TimelineClip).GetField("m_PostExtrapolationMode", flags);
            if (!ReferenceEquals(fieldInfo, null)) {
                fieldInfo.SetValue(clip, extrapolation);
            }
        }
    }
}
