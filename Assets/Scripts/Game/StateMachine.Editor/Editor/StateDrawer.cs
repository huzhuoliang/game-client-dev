using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Game.StateMachine.Editor {
    public class StateDrawer : OdinValueDrawer<State> {
        protected override void DrawPropertyLayout(GUIContent label) {
            State state = ValueEntry.SmartValue;

            SirenixEditorGUI.BeginBox();
            {
                Rect rect = EditorGUILayout.GetControlRect();
                if (label != null) {
                    rect = EditorGUI.PrefixLabel(rect, label);
                }

                EditorGUI.LabelField(rect, state.GetType().ToString());

                GUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField(state.GetType().ToString());
                    CallNextDrawer(null);
                }
                GUILayout.EndVertical();
            }
            SirenixEditorGUI.EndBox();
        }
    }
}
