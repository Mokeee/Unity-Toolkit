using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityToolkitEditor
{
    public class InspectorView : VisualElement
    {
        Editor editor;

        public InspectorView()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Unity-Toolkit/Editor/QuestGraph.uss");
            styleSheets.Add(styleSheet);
        }

        public new class UxmlFactory : UxmlFactory<InspectorView, VisualElement.UxmlTraits> { }

        internal void InspectNodeView(QuestGraphNodeView nodeView)
        {
            Clear();

            UnityEngine.Object.DestroyImmediate(editor);
            editor = Editor.CreateEditor(nodeView.node);
            IMGUIContainer container = new IMGUIContainer(() => { editor.OnInspectorGUI(); });
            Add(container);
        }
    }
}
