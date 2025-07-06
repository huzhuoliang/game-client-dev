using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.DialogueGraphView {
    public class DialogueGraphView : GraphView {
        private readonly Vector2 defaultNodeSize = new Vector2(150, 200);
        
        public DialogueGraphView() {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            AddElement(GenerateEntryPointNode());
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            List<Port> compatiblePorts = new List<Port>();
            foreach (Port port in ports) {
                if (startPort != port && startPort.node != port.node) {
                    compatiblePorts.Add(port);
                }
            }
            return compatiblePorts;
        }

        private Port GeneratePort(DialogueNode node, Direction portDirection, Port.Capacity capacity = Port.Capacity.Single) {
            return node.InstantiatePort(Orientation.Horizontal, portDirection, capacity, typeof(float));
        }

        private DialogueNode GenerateEntryPointNode() {
            DialogueNode node = new DialogueNode {
                    title = "START",
                    GUID = Guid.NewGuid().ToString(),
                    DialogueText = "ENTRYPOINT",
                    EntryPoint = true,
            };

            Port generatedPort = GeneratePort(node, Direction.Output);
            generatedPort.portName = "Next";
            node.outputContainer.Add(generatedPort);

            node.RefreshExpandedState();
            node.RefreshPorts();

            node.SetPosition(new Rect(100, 200, 100, 150));
            return node;
        }

        public void CreateNode(string nodeName) {
            AddElement(CreateDialogueNode(nodeName));
        }

        public DialogueNode CreateDialogueNode(string nodeName) {
            DialogueNode dialogueNode = new DialogueNode() {
                    title = nodeName,
                    DialogueText = nodeName,
                    GUID = Guid.NewGuid().ToString()
            };

            Port inputPort = GeneratePort(dialogueNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            dialogueNode.inputContainer.Add(inputPort);
            Button button = new Button(() => { AddChoicePort(dialogueNode); }) {
                    text = "New Choice"
            };
            dialogueNode.titleContainer.Add(button);
            
            dialogueNode.RefreshExpandedState();
            dialogueNode.RefreshPorts();
            dialogueNode.SetPosition(new Rect(Vector2.zero, defaultNodeSize));
            return dialogueNode;
        }

        private void AddChoicePort(DialogueNode dialogueNode) {
            Port generatePort = GeneratePort(dialogueNode, Direction.Output);
            int outputPortCout = dialogueNode.outputContainer.Query("connector").ToList().Count;
            generatePort.portName = $"Choice {outputPortCout}";
            dialogueNode.outputContainer.Add(generatePort);
            dialogueNode.RefreshExpandedState();
            dialogueNode.RefreshPorts();
        }
    }
}
