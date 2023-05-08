using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class QuestSelectionNode : QuestGraphNode
{
    public List<QuestGraphNode> children = new List<QuestGraphNode>();
}
