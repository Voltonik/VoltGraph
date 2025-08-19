using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.Experimental.GraphView;

using UnityEngine.UIElements;

public class DontDrawNodeField : Attribute {

}

public abstract class BaseNode : Node {
    public string GUID;
    public bool EntryPoint;
    public BaseNode EntryNode;

    public Dictionary<string, VisualElement> UIFields = new Dictionary<string, VisualElement>();
    public List<Port> AddedPorts = new List<Port>();


    public BaseNode GetNextNodeOfType(Type type) {
        return (BaseNode)((Port)outputContainer.Children().FirstOrDefault(c => c is Port p && p.portType == type))?.connections?.FirstOrDefault()?.input?.node;
    }

    public BaseNode GetInputNodeByIndex(int portIndex) {
        return (BaseNode)((Port)inputContainer.Children().ElementAt(portIndex))?.connections?.FirstOrDefault()?.output?.node;
    }

    public BaseNode[] GetInputNodesByIndex(int portIndex) {
        if (portIndex < 0 || portIndex >= inputContainer.Children().Count())
            return null;

        return ((Port)inputContainer.Children().ElementAt(portIndex))?.connections?.Select(c => (BaseNode)c?.output?.node).ToArray();
    }

    public override bool IsSelectable() {
        return true;
    }

    public override void OnSelected() {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    public override void OnUnselected() {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView) {
        OnDrawHandles();
    }

    public virtual string GetNodeTitle() => "Node";
    public virtual List<(Type, string, Port.Capacity, Orientation)> InputPorts() => new List<(Type, string, Port.Capacity, Orientation)> { (typeof(bool), "Input", Port.Capacity.Single, Orientation.Horizontal) };
    public virtual List<(Type, string, Port.Capacity, Orientation)> OutputPorts() => new List<(Type, string, Port.Capacity, Orientation)> { (typeof(bool), "Output", Port.Capacity.Single, Orientation.Horizontal) };
    public virtual (bool, Type, string, Orientation) AddablePorts() => (false, typeof(bool), "", Orientation.Horizontal);

    public virtual void Create(GraphView graphView, Dictionary<Edge, Port[]> deletedConnections = null) { }
    public virtual void Delete(GraphView graphView) { }

    public virtual void DrawCustomGUI(object inputData, object outputData, GraphView graphView) { }
    public virtual void OnFieldUpdated() { }

    protected virtual void OnDrawHandles() { }

    public VisualElement GetUIFieldByName(string name) {
        return UIFields[name.FieldToReadableName()];
    }
}