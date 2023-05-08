using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using System;
using UnityEngine.UIElements;

namespace UnityToolkitEditor
{
    public class QuestGraphNodeView : Node
    {
        public Action<QuestGraphNodeView> OnNodeSelected;
        public QuestGraphNode node;

        public Port input;
        public Port output;

        public QuestGraphNodeView(QuestGraphNode node) : base("Assets/Unity-Toolkit/Editor/QuestGraphNodeView.uxml")
        {
            this.node = node;
            title = node.GetType().Name;
            this.viewDataKey = node.GUID;

            CreateInputs();
            CreateOutputs();
            AddStyleClasses();
        }

        void AddStyleClasses()
        {
            if (node is RootNode)
                AddToClassList("rootNode");
            else if (node is QuestDialogNode)
                AddToClassList("dialogNode");
            else if (node is QuestSelectionNode)
                AddToClassList("selectionNode");
            else if (node is QuestActionNode)
                AddToClassList("actionNode");
        }

        void CreateInputs()
        {
            if (node.GetType() == typeof(RootNode))
                return;

            input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));

            if (input != null)
            {
                input.portName = "";
                input.style.flexDirection = FlexDirection.Row;
                inputContainer.Add(input);
            }
        }

        void CreateOutputs()
        {
            var capacity = Port.Capacity.Single;

            var selectionNode = node as QuestSelectionNode;
            if (selectionNode)
                capacity = Port.Capacity.Multi;

            output = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, typeof(bool));

            if (output != null)
            {
                output.portName = "";
                output.style.flexDirection = FlexDirection.RowReverse;
                outputContainer.Add(output);
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected.Invoke(this);
        }
    }
}
