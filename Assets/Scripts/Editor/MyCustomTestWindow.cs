using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Editor {
    public sealed class MyCustomTestWindow : OdinEditorWindow {
        [SerializeField]
        private Vector3 p;

        // [SerializeField]
        // private Vector3 n;

        // [SerializeField]
        // private float r = 100f;

        private MyTestDataClassSO _data;

        private PropertyTree _dataTree;

        private Vector2 _scrollPosition;

        private GUIStyle _areaGUIStyle;

        [MenuItem("Tools/MyCustomTestWindow")]
        private static void OpenWindow() {
            MyCustomTestWindow window = GetWindow<MyCustomTestWindow>();
            window.titleContent = new GUIContent("MessageTimeline");
        }

        protected override void OnEnable() {
            base.OnEnable();

            _data = CreateInstance<MyTestDataClassSO>();
            _dataTree = PropertyTree.Create(_data);
            _areaGUIStyle = new GUIStyle {
                    border = new RectOffset(2, 2, 2, 2)
            };

            // UndoTracker.OnObjectValueModified += HandleObjectValueModified;
            // UndoTracker.OnUndoPerformed += HandleUndoPerformed;
            // UndoTracker.OnRedoPerformed += HandleRedoPerformed;
        }

        protected override void OnDisable() {
            base.OnDisable();

            DestroyImmediate(_data);
            _dataTree.Dispose();
            _dataTree = null;

            // UndoTracker.OnObjectValueModified -= HandleObjectValueModified;
            // UndoTracker.OnUndoPerformed -= HandleUndoPerformed;
            // UndoTracker.OnRedoPerformed -= HandleRedoPerformed;
        }

        protected override void OnImGUI() {
            base.OnImGUI();
            float windowWidth = position.width;
            // Rect rect = GUILayoutUtility.GetRect(windowWidth, 300, GUILayout.ExpandHeight(true));
            // _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, true, true, GUILayout.Width(windowWidth), GUILayout.Height(300));
            // EditorGUI.DrawRect(rect, Color.black);

            Rect rect = new Rect(p.x, p.y, 400, 300);
            EditorGUI.DrawRect(rect, Color.gray);
            GUILayout.BeginArea(rect);
            _dataTree.Draw();
            GUILayout.EndArea();
            
            GUI.Box(new Rect(p.x, p.y, position.width / 2f, position.height / 2f), "Half Box");

            // EditorGUILayout.EndScrollView();

            // rect = EditorGUILayout.GetControlRect();
            // EditorGUI.DrawRect(rect, Color.red);
            
            // Handles.color = Color.red;
            // Handles.DrawSolidDisc(p, n, r);
            // Handles.color = Color.green;
            // Handles.DrawSolidDisc(p, n, 1f);

            // Repaint();
        }

        private void HandleObjectValueModified(UndoTracker.UndoPropertyModificationGroup[] groups) {
            foreach (var group in groups) {
                Debug.Log($"对象 {group.Target.name} 被修改，共有 {group.Modifications.Length} 次修改记录");
            }
            // 可以在此处调用 EditorUtility.SetDirty(target) 来标记数据已更改
        }

        private void HandleUndoPerformed(List<Object> modifiedObjects) {
            Debug.Log("Undo 执行，影响了以下对象：");
            foreach (var obj in modifiedObjects) {
                Debug.Log(obj.name);
            }
        }

        private void HandleRedoPerformed(List<Object> modifiedObjects) {
            Debug.Log("Redo 执行，影响了以下对象：");
            foreach (var obj in modifiedObjects) {
                Debug.Log(obj.name);
            }
        }
    }
}
