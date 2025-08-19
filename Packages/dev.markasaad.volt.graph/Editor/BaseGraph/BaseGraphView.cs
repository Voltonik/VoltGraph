using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.Experimental.GraphView;

using UnityEngine;
using UnityEngine.UIElements;


public class BaseGraphView<TEntryNode, TEditor, TGraphData> : AbstractNodeGraphView
        where TEntryNode : SaveableNode<TGraphData>, new()
        where TEditor : EditorWindow
        where TGraphData : BaseGraphData {

    public TEntryNode EntryNode {
        get {
            return (TEntryNode)m_entryNode;
        }
        set {
            m_entryNode = value;
        }
    }


    public BaseGraphView() {
        SetupZoom(ContentZoomer.DefaultMinScale, 4);

        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        grid.StretchToParentSize();
        Insert(0, grid);

        styleSheets.Add(Resources.Load<StyleSheet>("BaseGraph"));
        styleSheets.Add(Resources.Load<StyleSheet>("BaseNodeStyle"));

        EntryNode = CreateNode<TEntryNode>(true, new Vector2(100, 200));

        graphViewChanged += change => {
            if (change.elementsToRemove != null) {
                List<SaveableNode<TGraphData>> nodesToRemove = new List<SaveableNode<TGraphData>>();
                Dictionary<Edge, Port[]> deletedConnections = new Dictionary<Edge, Port[]>();

                foreach (var element in change.elementsToRemove) {
                    if (element is SaveableNode<TGraphData> n)
                        nodesToRemove.Add(n);
                    else if (element is Edge e)
                        deletedConnections.Add(e, new Port[] { e.output, e.input });
                }

                if (nodesToRemove.Count > 0) {
                    change.elementsToRemove = null;

                    nodesToRemove.ForEach(n => Nodes.Remove(n.GUID));
                    UndoRedoSystem.AddAction(new NodesDeleteAction<SaveableNode<TGraphData>, TGraphData>(nodesToRemove, deletedConnections, this));
                }
            }


            // TODO: Sometimes edges get recreated twice when undoing a lot.
            // TODO: Sometimes moved elements dont get undone/redone when undoing a lot.
            if (change.movedElements != null || change.elementsToRemove != null || change.edgesToCreate != null) {
                UndoRedoSystem.AddAction(new GraphChangeAction(this, new GraphViewChangedData {
                    MovedElements = change.movedElements?.ToList(),
                    EdgesToRemove = change.elementsToRemove?.Where(e => e is Edge)?.ToDictionary(k => (Edge)k, v => new Port[] { ((Edge)v).output, ((Edge)v).input }),
                    EdgesToCreate = change.edgesToCreate?.ToDictionary(k => k, v => new Port[] { v.output, v.input }),
                    MoveDelta = change.moveDelta
                }));
            }
            return change;
        };

        RegisterCallback<KeyDownEvent>(evt => {
            if (evt.ctrlKey && evt.keyCode == KeyCode.Z) {
                UndoRedoSystem.Undo();
                evt.StopPropagation();
            } else if (evt.ctrlKey && evt.keyCode == KeyCode.Y) {
                UndoRedoSystem.Redo();
                evt.StopPropagation();
            }
        });
    }

    public TNode CreateNode<TNode>(bool isEntryPoint = false, Vector2 position = default, string guid = "", object data = null, List<(Type, string, Port.Capacity, Orientation)> addedConnections = null)
            where TNode : SaveableNode<TGraphData>, new() {
        var node = new TNode {
            GUID = string.IsNullOrEmpty(guid) ? Guid.NewGuid().ToString() : guid,
            EntryPoint = isEntryPoint,
        };

        return SetupNode<TNode, TGraphData>(node, position, data, addedConnections);
    }

    public Vector2 GetLocalMousePosition(Vector2 mousePos) {
        return contentViewContainer.WorldToLocal(mousePos - GraphEditor.position.position);
    }
}
