using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "QuestGraph", menuName = "UnityToolkit/ScriptableObjects/QuestGraph", order = 1)]
public class QuestGraph : ScriptableObject
{
    public RootNode root;
    public List<QuestGraphNode> nodes = new List<QuestGraphNode>();

    public QuestGraphNode CreateNode(System.Type t)
    {
        QuestGraphNode node = CreateInstance(t) as QuestGraphNode;
        node.name = t.Name;
        node.GUID = GUID.Generate().ToString();
        nodes.Add(node);
        AssetDatabase.AddObjectToAsset(node, this);
        AssetDatabase.SaveAssets();

        return node;
    }

    public void RemoveNode(QuestGraphNode node)
    {
        nodes.Remove(node);
        AssetDatabase.RemoveObjectFromAsset(node);
        AssetDatabase.SaveAssets();
    }

    public void AddChildToNode(QuestGraphNode parent, QuestGraphNode child)
    {
        QuestDialogNode dialog = parent as QuestDialogNode;
        if (dialog)
            dialog.child = child;
        QuestActionNode action = parent as QuestActionNode;
        if (action)
            action.child = child;
        QuestSelectionNode selection = parent as QuestSelectionNode;
        if (selection)
            selection.children.Add(child);
        RootNode root = parent as RootNode;
        if (root)
            root.child = child;
    }
    public void RemoveChildToNode(QuestGraphNode parent, QuestGraphNode child)
    {
        QuestDialogNode dialog = parent as QuestDialogNode;
        if (dialog)
                dialog.child = null;
        QuestActionNode action = parent as QuestActionNode;
        if (action)
                action.child = null;
        QuestSelectionNode selection = parent as QuestSelectionNode;
        if (selection)
            selection.children.Remove(child);
        RootNode root = parent as RootNode;
        if (root)
            if (root.child != null)
                root.child = null;
    }

    public List<QuestGraphNode> GetNodeChildren(QuestGraphNode node)
    {
        List<QuestGraphNode> children = new List<QuestGraphNode>();
        QuestDialogNode dialog = node as QuestDialogNode;
        if (dialog)
            if (dialog.child != null)
                children.Add(dialog.child);
        QuestActionNode action = node as QuestActionNode;
        if (action)
            if (action.child != null)
                children.Add(action.child);
        QuestSelectionNode selection = node as QuestSelectionNode;
        if (selection)
            children.AddRange(selection.children);
        RootNode root = node as RootNode;
        if (root)
            if (root.child != null)
                children.Add(root.child);

        return children;
    }
}
