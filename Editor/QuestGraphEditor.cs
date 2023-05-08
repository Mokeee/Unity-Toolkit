using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityToolkitEditor
{
    public class QuestGraphEditor : EditorWindow
    {
        public QuestGraphView graphView;
        public InspectorView inspectorView;

        [MenuItem("Window/Unity Toolkit/QuestGraph")]
        public static void ShowGraph()
        {
            ShowGraph(null);
        }
        private static void ShowGraph(QuestGraph graph)
        {
            QuestGraphEditor wnd = GetWindow<QuestGraphEditor>();
            if(graph == null)
                wnd.titleContent = new GUIContent("QuestGraph: None");
            else
                wnd.titleContent = new GUIContent("QuestGraph: " + graph.name);
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Unity-Toolkit/Editor/QuestGraph.uxml");
            visualTree.CloneTree(root);

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Unity-Toolkit/Editor/QuestGraph.uss");
            root.styleSheets.Add(styleSheet);

            graphView = root.Q<QuestGraphView>();
            inspectorView = root.Q<InspectorView>();

            graphView.OnNodeSelected += OnNodeSelectionChanged;

            OnSelectionChange();
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            if(Selection.activeObject is QuestGraph)
            {
                ShowGraph(Selection.activeObject as QuestGraph);
                return true;
            }
            return false;
        }

        private void OnSelectionChange()
        {
            QuestGraph graph = Selection.activeObject as QuestGraph;

            if (graph != null && AssetDatabase.CanOpenAssetInEditor(graph.GetInstanceID()))
            {
                titleContent.text = "QuestGraph: " + graph.name;
                graphView.PopulateView(graph);
            }
        }

        public void OnNodeSelectionChanged(QuestGraphNodeView nodeView)
        {
            inspectorView.InspectNodeView(nodeView);
        }

        private void OnInspectorUpdate()
        {
            if(graphView.graph == null)
            {
                titleContent.text = "QuestGraph: None";
                graphView.DepopulateView();
            }
        }
    }
}