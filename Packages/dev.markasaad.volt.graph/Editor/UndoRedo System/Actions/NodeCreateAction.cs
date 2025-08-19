using UnityEditor.Experimental.GraphView;

public class NodeCreateAction<TNode, TGraphData> : IUndoRedoAction where TNode : SaveableNode<TGraphData> where TGraphData : BaseGraphData {
    private TNode m_node;
    private GraphView m_graphView;

    public NodeCreateAction(TNode node, GraphView graphView) {
        m_node = node;
        m_graphView = graphView;
    }

    public void Undo() {
        m_node.Delete(m_graphView);
    }

    public void Redo() {
        m_node.Create(m_graphView);
    }
}
