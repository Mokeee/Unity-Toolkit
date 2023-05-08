using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


namespace UnityToolkitEditor
{
    public class QuestGraphView : GraphView
    {
        public Action<QuestGraphNodeView> OnNodeSelected;
        public QuestGraph graph;

        private Vector2 mousePosition;

        public QuestGraphView()
        {
            Insert(0, new GridBackground());

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Unity-Toolkit/Editor/QuestGraph.uss");
            styleSheets.Add(styleSheet);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.RegisterCallback<PointerDownEvent>(OnPointerDownEvent, TrickleDown.TrickleDown);
        }

        private void OnPointerDownEvent(PointerDownEvent evt)
        {
            mousePosition = evt.position;
        }

        public new class UxmlFactory : UxmlFactory<QuestGraphView, GraphView.UxmlTraits> { }

        internal void PopulateView(QuestGraph graph)
        {
            this.graph = graph;

            DepopulateView();

            if (graph.root == null)
            {
                graph.root = graph.CreateNode(typeof(RootNode)) as RootNode;
                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssets();
            }

            foreach (var node in graph.nodes)
                CreateNodeView(node);

            foreach (var node in graph.nodes)
            {
                var parent = GetNodeByGuid(node.GUID) as QuestGraphNodeView;
                foreach (var child in graph.GetNodeChildren(node))
                    CreateEdge(parent, GetNodeByGuid(child.GUID) as QuestGraphNodeView);
            }
        }

        internal void DepopulateView()
        {
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if(graphViewChange.elementsToRemove != null)
            {
                foreach (var elem in graphViewChange.elementsToRemove)
                {
                    var nodeView = elem as QuestGraphNodeView;
                    if(nodeView != null)
                        graph.RemoveNode(nodeView.node);
                    else if(elem.GetType() == typeof(Edge))
                    {
                        var edge = elem as Edge;
                        graph.RemoveChildToNode((edge.output.node as QuestGraphNodeView).node, (edge.input.node as QuestGraphNodeView).node);
                    }
                }
            }
            if(graphViewChange.movedElements != null)
            {
                foreach (var elem in graphViewChange.movedElements)
                {
                    var nodeView = elem as QuestGraphNodeView;
                    if (nodeView != null)
                        nodeView.node.position = elem.GetPosition().position;
                }
            }
            if(graphViewChange.edgesToCreate != null)
            {
                foreach (var elem in graphViewChange.edgesToCreate)
                    graph.AddChildToNode((elem.output.node as QuestGraphNodeView).node, (elem.input.node as QuestGraphNodeView).node);
            }

            return graphViewChange;
        }

        private void CreateNodeView(QuestGraphNode node)
        {
            QuestGraphNodeView nodeView = new QuestGraphNodeView(node);
            AddElement(nodeView);
            nodeView.SetPosition(new Rect(node.position, new Vector2(1, 1)));
            nodeView.OnNodeSelected += OnNodeSelected.Invoke;            
        }

        private void CreateNode(Type type)
        {
            var node = graph.CreateNode(type);
            node.position = (mousePosition - this.placematContainer.worldBound.position) * (1 / scale);      
            CreateNodeView(node);
        }

        private void CreateEdge(QuestGraphNodeView parent, QuestGraphNodeView child)
        {
            Edge edge = parent.output.ConnectTo(child.input);
            AddElement(edge);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            var types = TypeCache.GetTypesDerivedFrom<QuestSelectionNode>();
            foreach (var type in types)
                evt.menu.AppendAction("Add " + type.Name, (a) => CreateNode(type));

            types = TypeCache.GetTypesDerivedFrom<QuestDialogNode>();
            foreach (var type in types)
                evt.menu.AppendAction("Add " + type.Name, (a) => CreateNode(type));

            types = TypeCache.GetTypesDerivedFrom<QuestActionNode>();
            foreach (var type in types)
                evt.menu.AppendAction("Add " + type.Name, (a) => CreateNode(type));
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort => endPort.direction != startPort.direction && endPort.node != startPort.node).ToList();
        }
    }
}