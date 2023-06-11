using System;
using System.Collections;
using System.Collections.Generic;
using AI.BehaviourTree;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Node = AI.BehaviourTree.Node;

public sealed class NodeView : UnityEditor.Experimental.GraphView.Node
{
    public Node Node;
    public Port InputPort;
    public Port OutputPort;
    public Action<NodeView> OnNodeSelected;

    public NodeView(Node node) : base("Assets/Scripts/Editor/NodeView.uxml")
    {
        Node = node;
        title = node.name;
        viewDataKey = node.guid;

        style.left = node.position.x;
        style.top = node.position.y;
        //styleSheets.Add(Resources.Load<StyleSheet>("Node"));
        //var inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        //inputPort.portName = "Input";
        //inputContainer.Add(inputPort);
        //var outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        //outputPort.portName = "Output";
        //outputContainer.Add(outputPort);}
        SetupClasses();
        CreateInputPorts();
        CreateOutputPorts();
        
        Label descriptionLabel = this.Q<Label>("description");
        descriptionLabel.bindingPath = "description";
        descriptionLabel.Bind(new SerializedObject(node));
    }

    private void SetupClasses()
    {
        switch (Node)
        {
            case ActionNode actionNode:
                AddToClassList("action");
                break;
            case CompositeNode compositeNode:
                AddToClassList("composite");
                break;
            case DecoratorNode decoratorNode:
                AddToClassList("decorator");
                break;
            case RootNode rootNode:
                AddToClassList("root");
                break;
        }
    }

    private void CreateInputPorts()
    {
        switch (Node)
        {
            case ActionNode actionNode:
                InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
                break;
            case CompositeNode compositeNode:
                InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
                break;
            case DecoratorNode decoratorNode:
                InputPort = InstantiatePort(Orientation.Vertical, Direction.Input, Port.Capacity.Single, typeof(bool));
                break;
            case RootNode rootNode:
                break;
        }

        if (InputPort != null)
        {
            InputPort.portName = "";
            InputPort.style.flexDirection = FlexDirection.Column;
            inputContainer.Add(InputPort);
        }

    }

    private void CreateOutputPorts()
    {
        switch (Node)
        {
            case ActionNode actionNode:
                break;
            case CompositeNode compositeNode:
                OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Multi, typeof(bool));
                break;
            case DecoratorNode decoratorNode:
                OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
                break;
            case RootNode rootNode:
                OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, Port.Capacity.Single, typeof(bool));
                break;
        }

        if (OutputPort != null)
        {
            OutputPort.portName = "";
            OutputPort.style.flexDirection = FlexDirection.ColumnReverse;
            outputContainer.Add(OutputPort);
        }
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        Undo.RecordObject(Node, "Behaviour Tree (Set Position)");
        Node.position.x = newPos.xMin;
        Node.position.y = newPos.yMin;
        EditorUtility.SetDirty(Node);
    }

    public override void OnSelected()
    {
        base.OnSelected();
        OnNodeSelected?.Invoke(this);
    }

    public void SortChildren()
    {
        CompositeNode compositeNode = Node as CompositeNode;
        if (compositeNode)
        {
            compositeNode.children.Sort(SortByHorizontalPosition);
        }
    }

    private int SortByHorizontalPosition(Node left, Node right)
    {
        return left.position.x < right.position.x ? -1 : 1;
    }

    public void UpdateState()
    {
        RemoveFromClassList("running");
        RemoveFromClassList("failure");
        RemoveFromClassList("success");
        if (Application.isPlaying)
        {
            switch (Node.state)
            {
                case Node.State.Running:
                    if (Node.started)
                        AddToClassList("running");
                    break;
                case Node.State.Failure:
                    AddToClassList("failure");
                    break;
                case Node.State.Success:
                    AddToClassList("success");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
