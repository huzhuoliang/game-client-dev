using DefaultNamespace;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor {
    public class SerializeByPathOdinDrawer<T> : OdinValueDrawer<SerializeByPath<T>> where T : Object {
        private T _cachedObject;
        private string _cachedObjectPath;

        private readonly Color _guiColor = new(0.41f, 1f, 0.91f);

        protected override void DrawPropertyLayout(GUIContent label) {
            SerializeByPath<T> warper = ValueEntry.SmartValue;
            if (warper == null) {
                SirenixEditorGUI.MessageBox("SerializeByPath is null.", MessageType.Warning);
                return;
            }

            string path = warper.AssetPath;
            if (path != _cachedObjectPath || !IsAssetStillExist()) {
                if (TryLoadAs(path, out T loadedObject)) {
                    _cachedObject = loadedObject;
                    _cachedObjectPath = path;
                } else {
                    ClearCachedObject();
                }
            }


            Color oldColor = GUI.color;
            GUI.color = _guiColor;
            bool showBox = false;
            if (_cachedObject == null && !string.IsNullOrEmpty(path)) {
                SirenixEditorGUI.BeginBox();
                GUILayout.BeginHorizontal();
                string message = $"Missing ({path})";
                SirenixEditorGUI.WarningMessageBox(message);
                if (GUILayout.Button("Clear", GUILayout.Width(50), GUILayout.Height(24))) {
                    warper.AssetPath = string.Empty;
                    ValueEntry.SmartValue = warper;
                    ValueEntry.Property.MarkSerializationRootDirty();
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(3);
                showBox = true;
            }

            label.text = "*" + label.text;
            Object picked = SirenixEditorFields.UnityObjectField(label, _cachedObject, typeof(T), false);
            T newValue = picked as T;
            if (newValue != _cachedObject) {
                _cachedObject = newValue;
                _cachedObjectPath = AssetDatabase.GetAssetPath(newValue);
                warper.AssetPath = _cachedObjectPath;
                ValueEntry.SmartValue = warper;
                ValueEntry.Property.MarkSerializationRootDirty();
            }

            if (showBox) {
                SirenixEditorGUI.EndBox();
            }

            GUI.color = oldColor;
        }

        private void ClearCachedObject() {
            if (_cachedObject != null && !EditorUtility.IsPersistent(_cachedObject)) {
                Object.DestroyImmediate(_cachedObject);
            }

            _cachedObject = null;
            _cachedObjectPath = string.Empty;
        }

        private bool IsAssetStillExist() {
            if (_cachedObject == null) {
                return string.IsNullOrEmpty(_cachedObjectPath);
            }

            return AssetDatabase.GetAssetPath(_cachedObject).Equals(_cachedObjectPath);
        }

        private static bool TryLoadAs<TLoad>(string assetPath, out TLoad result) where TLoad : Object {
            result = null;
            if (string.IsNullOrEmpty(assetPath)) {
                return false;
            }

            // Validate path exists in AssetDatabase
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath))) {
                return false;
            }

            result = AssetDatabase.LoadAssetAtPath<TLoad>(assetPath);
            return result != null;
        }
    }
}
