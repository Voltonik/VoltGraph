using System.Collections.Generic;

using UnityEditor.Experimental.GraphView;

public class NodesDeleteAction<TNode, TGraphData> : IUndoRedoAction where TNode : SaveableNode<TGraphData> where TGraphData : BaseGraphData {
    private List<TNode> m_deletedNodes;
    private GraphView m_graphView;
    private Dictionary<Edge, Port[]> m_deletedConnections;

    public NodesDeleteAction(List<TNode> deletedNodes, Dictionary<Edge, Port[]> deletedConnections, GraphView graphView) {
        m_deletedNodes = deletedNodes;
        m_deletedConnections = deletedConnections;
        m_graphView = graphView;
    }

    public void Undo() {
        foreach (var node in m_deletedNodes) {
            node.Create(m_graphView, m_deletedConnections);
        }
    }

    public void Redo() {
        foreach (var node in m_deletedNodes) {
            node.Delete(m_graphView);
        }
    }
}
