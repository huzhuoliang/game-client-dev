using UnityEditor.Experimental.GraphView;

namespace Editor.DialogueGraphView {
    public class DialogueNode : Node {
        public string GUID;

        public string DialogueText;

        public bool EntryPoint = false;
    }
}
