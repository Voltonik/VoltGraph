using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;


public class BaseGraphEditor<TEntryNode, TGraphView, TGraphData> : EditorWindow
        where TEntryNode : SaveableNode<TGraphData>, new()
        where TGraphView : AbstractNodeGraphView, new()
        where TGraphData : BaseGraphData {


    protected TGraphView m_graphView;

    protected string m_filePath = "";
    private ObjectField m_filePathField;

    #region Initialization
    private void OnEnable() {
        ConstructGraph();
        GenerateToolbar();
    }

    private void OnDisable() {
        rootVisualElement.Remove(m_graphView);
    }

    protected virtual void ConstructGraph() {
        m_graphView = new TGraphView();
        m_graphView.GraphEditor = this;

        m_graphView.StretchToParentSize();
        m_graphView.MarkDirtyRepaint();
        rootVisualElement.Add(m_graphView);
    }
    #endregion

    #region Elements
    private void GenerateToolbar() {
        var toolbar = new Toolbar();
        m_filePathField = new ObjectField("Graph File") { objectType = typeof(TGraphData) };

        m_filePath = null;

        m_filePathField.SetValueWithoutNotify(AssetDatabase.LoadAssetAtPath<TGraphData>($"Assets/{m_filePath}.asset"));
        m_filePathField.MarkDirtyRepaint();
        m_filePathField.RegisterValueChangedCallback(evt => m_filePath = AssetDatabase.GetAssetPath(evt.newValue));

        toolbar.Add(m_filePathField);
        toolbar.Add(new Button(() => RequestDataOperation(true)) { text = "Save Data" });
        toolbar.Add(new Button(() => RequestDataOperation(false)) { text = "Load Data" });

        EditToolbar(ref toolbar);

        rootVisualElement.Add(toolbar);
    }

    protected virtual void EditToolbar(ref Toolbar toolbar) { }

    protected void GenerateMinimap() {
        var minimap = new MiniMap { anchored = true };
        minimap.SetPosition(new Rect(10, 30, 200, 140));

        m_graphView.Add(minimap);
    }

    #endregion

    #region Saving & Loading
    protected void RequestDataOperation(bool save) {
        if (save) {
            if (string.IsNullOrEmpty(m_filePath))
                m_filePath = EditorUtility.SaveFilePanelInProject("Graph Location", "NewGraphAsset", "asset", "Graph Asset Location");

            SaveGraph(m_filePath);
        } else {
            if (string.IsNullOrEmpty(m_filePath))
                EditorUtility.DisplayDialog("File Not Found", $"Graph file does not exist.", "Ok");

            LoadGraph((TGraphData)m_filePathField.value);
        }
    }

    public void SaveGraph(string filePath) {
        var container = ScriptableObject.CreateInstance<TGraphData>();

        var connectedPorts = m_graphView.edges.Where(e => e.input.node != null);

        foreach (var port in connectedPorts) {
            var outputNode = port.output.node as BaseNode;
            var inputNode = port.input.node as BaseNode;

            container.NodeLinks.Add(new NodeLinkData {
                BaseNodeGUID = outputNode.GUID,
                PortName = port.output.portName,
                TargetNodeGUID = inputNode.GUID,
                InputIndex = inputNode.inputContainer.IndexOf(port.input),
                OutputIndex = outputNode.outputContainer.IndexOf(port.output),
            });
        }

        foreach (var n in m_graphView.Nodes.Values)
            ((SaveableNode<TGraphData>)n).SaveNodeTo(container);

        AssetDatabase.CreateAsset(container, filePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        LoadGraph(container);
    }

    protected virtual void LoadGraph(TGraphData graphDataSO) {
        if (!graphDataSO) {
            EditorUtility.DisplayDialog("File Not Found", $"Graph file does not exist.", "Ok");
            return;
        }

        m_filePathField.value = graphDataSO;

        m_graphView.UndoRedoSystem.ClearActions();

        ClearGraph();
        CreateNodes(graphDataSO);
        ConnectNodes(graphDataSO);
    }

    private void ClearGraph() {
        var nodes = m_graphView.Nodes.Values.ToList();
        foreach (var node in nodes)
            m_graphView.DeleteNode(node);
    }

    protected virtual void CreateNodes(TGraphData graphDataSO) {
    }

    protected TNode CreateNodeFromSO<TNodeFields, TNode>(BaseNodeData<TNodeFields> nodeData, TGraphData graphDataSO) where TNode : SaveableNode<TGraphData>, new() {
        var addedConnections = new List<(Type, string, Port.Capacity, Orientation)>();

        var node = new TNode {
            GUID = nodeData.GUID,
            EntryPoint = nodeData.EntryPoint,
        };

        for (int i = 0; i < nodeData.ExtraPorts; i++)
            addedConnections.Add((node.AddablePorts().Item2, node.AddablePorts().Item3, Port.Capacity.Single, node.AddablePorts().Item4));

        return m_graphView.SetupNode<TNode, TGraphData>(node, nodeData.Position, nodeData.FieldsData, addedConnections);
    }

    private void ConnectNodes(TGraphData graphDataSO) {
        foreach (var node in m_graphView.Nodes.Values) {
            List<NodeLinkData> connections = graphDataSO.NodeLinks.Where(nl => nl.BaseNodeGUID == node.GUID).ToList();

            for (int i = 0; i < connections.Count; i++) {
                string targetNodeGUID = connections[i].TargetNodeGUID;
                var targetNode = m_graphView.Nodes[targetNodeGUID];

                // TODO: fix saving with multiple layers
                m_graphView.LinkNodes(node.outputContainer[connections[i].OutputIndex].Q<Port>(), (Port)targetNode.inputContainer[connections[i].InputIndex]);
            }
        }
    }
    #endregion
}