using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.DialogueGraphView {
    public class DialogueGraph : EditorWindow {
        private DialogueGraphView _graphView;

        [MenuItem("Graph/Dialogue Graph")]
        public static void OpenWindow() {
            DialogueGraph window = GetWindow<DialogueGraph>();
            window.titleContent = new GUIContent("Dialogue Graph");
        }

        private void OnEnable() {
            ConstructGraphView();
            GenerateToolBar();
        }

        private void OnDisable() {
            rootVisualElement.Remove(_graphView);
        }

        private void GenerateToolBar() {
            Toolbar toolbar = new Toolbar();
            Button nodeCreateButton = new Button(() => { _graphView.CreateNode("Dialogue Node"); }) {
                    text = "Create Node"
            };
            toolbar.Add(nodeCreateButton);
            rootVisualElement.Add(toolbar);
        }

        private void ConstructGraphView() {
            _graphView = new DialogueGraphView() {
                    name = "Dialogue Graph"
            };

            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }
    }
}
