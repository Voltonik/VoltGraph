using System.Collections.Generic;

using UnityEditor.Experimental.GraphView;

using UnityEngine;

public struct GraphViewChangedData {
    public Dictionary<Edge, Port[]> EdgesToRemove;
    public Dictionary<Edge, Port[]> EdgesToCreate;
    public List<GraphElement> MovedElements;
    public Vector2 MoveDelta;
}

public class GraphChangeAction : IUndoRedoAction {
    private readonly AbstractNodeGraphView m_graphView;
    private GraphViewChangedData m_graphViewChange;

    public GraphChangeAction(AbstractNodeGraphView graphView, GraphViewChangedData graphViewChange) {
        m_graphView = graphView;
        m_graphViewChange = graphViewChange;
    }

    public void Undo() {
        if (m_graphViewChange.EdgesToRemove != null) {
            foreach (var edge in m_graphViewChange.EdgesToRemove.Values) {
                m_graphView.LinkNodes(edge[0], edge[1]);
            }
        }

        if (m_graphViewChange.EdgesToCreate != null) {
            foreach (var edge in m_graphViewChange.EdgesToCreate.Keys) {
                m_graphView.DeleteElements(m_graphViewChange.EdgesToCreate[edge][0].connections);
                m_graphView.DeleteElements(m_graphViewChange.EdgesToCreate[edge][1].connections);
            }
        }

        if (m_graphViewChange.MovedElements != null) {
            foreach (var moved in m_graphViewChange.MovedElements) {
                var element = moved;
                Rect rect = element.GetPosition();
                rect.position -= m_graphViewChange.MoveDelta;
                element.SetPosition(rect);
            }
        }
    }

    public void Redo() {
        if (m_graphViewChange.EdgesToRemove != null) {
            foreach (var edge in m_graphViewChange.EdgesToRemove.Keys) {
                m_graphView.DeleteElements(m_graphViewChange.EdgesToRemove[edge][0].connections);
                m_graphView.DeleteElements(m_graphViewChange.EdgesToRemove[edge][1].connections);
            }
        }

        if (m_graphViewChange.EdgesToCreate != null) {
            foreach (var edge in m_graphViewChange.EdgesToCreate.Keys) {
                m_graphView.LinkNodes(edge, m_graphViewChange.EdgesToCreate[edge][0], m_graphViewChange.EdgesToCreate[edge][1]);
            }
        }

        if (m_graphViewChange.MovedElements != null) {
            foreach (var moved in m_graphViewChange.MovedElements) {
                var element = moved;
                Rect rect = element.GetPosition();
                rect.position += m_graphViewChange.MoveDelta;
                element.SetPosition(rect);
            }
        }
    }
}
