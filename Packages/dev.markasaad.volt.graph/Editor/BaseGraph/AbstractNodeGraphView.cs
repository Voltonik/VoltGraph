using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;


public abstract class AbstractNodeGraphView : GraphView {
    public readonly Vector2 DefaultNodeSize = new Vector2(150, 200);
    public readonly UndoRedoSystem UndoRedoSystem = new UndoRedoSystem();

    protected BaseNode m_entryNode;

    public Dictionary<string, BaseNode> Nodes = new Dictionary<string, BaseNode>();
    public EditorWindow GraphEditor;

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
        var compatiblePorts = new List<Port>();

        ports.ForEach(p => {
            if (startPort != p && startPort.node != p.node && startPort.direction != p.direction && startPort.portType == p.portType)
                compatiblePorts.Add(p);
        });

        return compatiblePorts;
    }

    public void LinkNodes(Port output, Port input) {
        Edge edge = new Edge {
            input = input,
            output = output
        };

        LinkNodes(edge, output, input);
    }

    public void LinkNodes(Edge edge, Port output, Port input) {
        edge.input = input;
        edge.output = output;

        edge.input.Connect(edge);
        edge.output.Connect(edge);

        AddElement(edge);
    }

    public void DeleteNode(BaseNode node) {
        var targetNode = (BaseNode)nodes.FirstOrDefault(n => ((BaseNode)n).GUID == node.GUID);

        // TODO: This sometimes breaks when undoing and redoing a lot
        if (targetNode == default)
            return;

        foreach (var e in edges.Where(e => e.input.node == targetNode))
            RemoveElement(e);

        RemoveElement(targetNode);

        Nodes.Remove(targetNode.GUID);
    }

    public TNode SetupNode<TNode, TGraphData>(TNode node, Vector2 position = default, object data = null, List<(Type, string, Port.Capacity, Orientation)> addedConnections = null) where TNode : SaveableNode<TGraphData>, new() where TGraphData : BaseGraphData {
        node.title = node.GetNodeTitle();
        node.EntryNode = m_entryNode;

        if (node.EntryPoint)
            node.capabilities &= Capabilities.Deletable;

        var inputPorts = node.InputPorts();
        foreach (var portData in inputPorts) {
            var port = node.InstantiatePort(portData.Item4, Direction.Input, portData.Item3, portData.Item1);

            port.portName = portData.Item2;
            node.inputContainer.Add(port);
        }

        if (addedConnections != null) {
            foreach (var portData in addedConnections) {
                var port = node.InstantiatePort(portData.Item4, Direction.Input, portData.Item3, portData.Item1);

                port.portName = portData.Item2;
                port.contentContainer.Add(new Button(() => {
                    foreach (var c in port.connections)
                        RemoveElement(c);
                    node.inputContainer.Remove(port);
                    node.AddedPorts.Remove(port);
                }) { text = "X" });

                node.inputContainer.Add(port);
                node.AddedPorts.Add(port);
            }
        }

        var outputPorts = node.OutputPorts();
        foreach (var portData in outputPorts) {
            var port = node.InstantiatePort(portData.Item4, Direction.Output, portData.Item3, portData.Item1);

            port.portName = portData.Item2;
            node.outputContainer.Add(port);
        }

        node.mainContainer.AddToClassList("node__main-container");
        node.extensionContainer.AddToClassList("node__extension-container");

        if (node.AddablePorts().Item1) {
            var button = new Button(() => {
                var port = node.InstantiatePort(node.AddablePorts().Item4, Direction.Input, Port.Capacity.Single, node.AddablePorts().Item2);

                port.portName = node.AddablePorts().Item3;
                port.contentContainer.Add(new Button(() => {
                    foreach (var c in port.connections)
                        RemoveElement(c);
                    node.inputContainer.Remove(port);
                    node.AddedPorts.Remove(port);
                }) { text = "X" });


                node.inputContainer.Add(port);
                node.AddedPorts.Add(port);
            }) { text = "Add Port" };
            node.titleContainer.Add(button);
        }

        DrawNodeFields(node, data ?? node.GetDefaultDataInstance(), node.GetData());
        node.DrawCustomGUI(data ?? node.GetDefaultDataInstance(), node.GetData(), this);

        node.RefreshExpandedState();

        node.SetPosition(new Rect(position, DefaultNodeSize));

        AddElement(node);

        Nodes.Add(node.GUID, node);

        UndoRedoSystem.AddAction(new NodeCreateAction<TNode, TGraphData>(node, this));

        return node;
    }

    private void SetNodeValue<TField>(BaseField<TField> nodeField, FieldInfo field, object obj, object value, bool setToIndex = false) {
        UndoRedoSystem.AddAction(new NodeFieldChangeAction<TField>(nodeField, field, obj, setToIndex));

        field.SetValue(obj, value);
    }

    protected void DrawNodeFields(BaseNode node, object inputData, object outputData) {
        setupFields(outputData.GetType(), inputData, outputData);

        void setupFields(Type type, object inputData, object outputData, VisualElement container = default) {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);

            if (container == default) {
                container = new VisualElement();
            }

            foreach (var field in fields) {
                // TODO: Fix
                if (field.CustomAttributes.Any(a => a.AttributeType == typeof(HeaderAttribute))) {
                    CustomAttributeData header = field.CustomAttributes.First(a => a.AttributeType == typeof(HeaderAttribute));
                    node.mainContainer.Add(new Label((string)header.ConstructorArguments[0].Value));
                }
                if (field.IsLiteral || field.CustomAttributes.Any(a => a.AttributeType == typeof(DontDrawNodeField)))
                    continue;

                string fieldLabel = field.Name.FieldToReadableName();
                field.SetValue(outputData, field.GetValue(inputData));

                if (field.FieldType == typeof(int))
                    SetupField(new IntegerField(fieldLabel) { isDelayed = true }, field, node, inputData, outputData, container);
                else if (field.FieldType == typeof(uint))
                    SetupField(new UnsignedIntegerField(fieldLabel) { isDelayed = true }, field, node, inputData, outputData, container);
                else if (field.FieldType == typeof(float)) {
                    if (field.CustomAttributes.Any(a => a.AttributeType == typeof(RangeAttribute))) {
                        CustomAttributeData range = field.CustomAttributes.First(a => a.AttributeType == typeof(RangeAttribute));
                        SetupField(new Slider((float)range.ConstructorArguments[0].Value, (float)range.ConstructorArguments[1].Value) { label = fieldLabel, showInputField = true }, field, node, inputData, outputData, container);
                    } else {
                        SetupField(new FloatField(fieldLabel) { isDelayed = true }, field, node, inputData, outputData, container);
                    }
                } else if (field.FieldType == typeof(string))
                    SetupField(new TextField(fieldLabel) { isDelayed = true }, field, node, inputData, outputData, container);
                else if (field.FieldType == typeof(bool))
                    SetupField(new Toggle(fieldLabel), field, node, inputData, outputData, container);
                else if (field.FieldType == typeof(Vector2))
                    SetupField(new Vector2Field(fieldLabel), field, node, inputData, outputData, container);
                else if (field.FieldType == typeof(Vector3))
                    SetupField(new Vector3Field(fieldLabel), field, node, inputData, outputData, container);
                else if (field.FieldType == typeof(Vector4))
                    SetupField(new Vector4Field(fieldLabel), field, node, inputData, outputData, container);
                else if (field.FieldType == typeof(Vector2Int))
                    SetupField(new Vector2IntField(fieldLabel), field, node, inputData, outputData, container);
                else if (field.FieldType == typeof(Vector3Int))
                    SetupField(new Vector3IntField(fieldLabel), field, node, inputData, outputData, container);
                else if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                    SetupField(new ObjectField(fieldLabel) { objectType = field.FieldType }, field, node, inputData, outputData, container);
                else if (field.FieldType.IsEnum) {
                    var enumField = new EnumField(fieldLabel);
                    enumField.Init((Enum)Activator.CreateInstance(field.FieldType));
                    SetupField(enumField, field, node, inputData, outputData, container);
                } else if (field.FieldType.IsAssignableFrom(typeof(UnityEngine.Object))) {
                    SetupField(new ObjectField(fieldLabel), field, node, inputData, outputData, container);
                } else if (field.FieldType.IsSerializable) {
                    if (field.GetValue(outputData) == null)
                        field.SetValue(outputData, Activator.CreateInstance(field.FieldType));

                    if (field.GetValue(inputData) == null)
                        field.SetValue(inputData, Activator.CreateInstance(field.FieldType));

                    setupFields(field.GetValue(outputData).GetType(), field.GetValue(inputData), field.GetValue(outputData), new Foldout { text = fieldLabel });
                }
            }
        }
    }

    public void SetupField<TField>(BaseField<TField> nodeField, FieldInfo field, BaseNode node, object inputData, object outputData, VisualElement container = null, bool setToIndex = false) {
        nodeField.RegisterValueChangedCallback(evt => {
            SetNodeValue(nodeField, field, outputData, setToIndex ? ((PopupField<TField>)nodeField).index : evt.newValue, setToIndex);
            node.OnFieldUpdated();
        });
        if (!setToIndex)
            nodeField.SetValueWithoutNotify((TField)field.GetValue(inputData));
        else
            ((PopupField<TField>)nodeField).index = (int)field.GetValue(inputData);

        if (!node.UIFields.ContainsKey(nodeField.label))
            node.UIFields.Add(nodeField.label, nodeField);


        if (container != null) {
            container.AddToClassList("node__custom-data-container");

            container.Add(nodeField);
            node.mainContainer.Add(container);
        } else {
            node.mainContainer.Add(nodeField);
        }
    }
}
