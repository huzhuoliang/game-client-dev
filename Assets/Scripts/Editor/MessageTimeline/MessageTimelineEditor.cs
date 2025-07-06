using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor.MessageTimeline {
    public class MessageTimelineEditor : OdinEditorWindow {
        private List<Node> _nodes;

        [MenuItem("Tools/MessageTimeline")]
        private static void OpenWindow() {
            MessageTimelineEditor window = GetWindow<MessageTimelineEditor>();
            window.titleContent = new GUIContent("MessageTimeline");
            window._nodes = new List<Node>();
            window._nodes.Add(new Node(new Vector2(0, 0)));
        }

        protected override void OnImGUI() {
            DrawNodes();

            if (GUI.changed) {
                Repaint();
            }
        }

        private void DrawNodes() {
            for (int i = 0; i < _nodes.Count; i++) {
                _nodes[i].Draw();
            }
        }
    }
}
