using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;

namespace UnityToolkitEditor
{
    public class SplitView : TwoPaneSplitView
    {
        public SplitView()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Unity-Toolkit/Editor/QuestGraph.uss");
            styleSheets.Add(styleSheet);
        }

        public new class UxmlFactory : UxmlFactory<SplitView, TwoPaneSplitView.UxmlTraits> { }
    }
}