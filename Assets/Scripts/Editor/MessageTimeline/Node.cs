using UnityEngine;

namespace Editor.MessageTimeline {
    public class Node {
        public Rect NodeRect;

        public Node(Vector2 position) {
            NodeRect = new Rect(position.x, position.y, 160, 40);
        }

        public void Draw() {
            GUI.Box(NodeRect, "MyNode");
        }
    }
}
