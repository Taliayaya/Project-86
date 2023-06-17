using System;
using System.Collections.Generic;
using AI.BehaviourTree.CoreNodes;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace AI.BehaviourTree
{
    [CreateAssetMenu()]
    public class BehaviourTree : ScriptableObject
    {
        public Node rootNode;
        public Node.State treeState = Node.State.Running;
        public List<Node> nodes = new List<Node>();
        public BlackBoard blackBoard = new BlackBoard();

        public Node.State Update()
        {
            if (rootNode.state == Node.State.Running)
                treeState = rootNode.Update();

            return treeState;
        }

        
#if UNITY_EDITOR
        public Node CreateNode(Type type)
        {
            var node = CreateInstance(type) as Node;
            node!.name = type.Name;
            node.guid = GUID.Generate().ToString();
            
            Undo.RecordObject(this, "Behaviour Tree (Create Node)");
            nodes.Add(node);

            if (!Application.isPlaying)
                AssetDatabase.AddObjectToAsset(node, this);
            Undo.RegisterCreatedObjectUndo(node, "Behaviour Tree (Create Node)");
            AssetDatabase.SaveAssets();
            return node;
        }

        public void DeleteNode(Node node)
        {
            Undo.RecordObject(this, "Behaviour Tree (Delete Node)");
            nodes.Remove(node);
            
            Undo.DestroyObjectImmediate(node);
            AssetDatabase.SaveAssets();
        }

        public void AddChild(Node parent, Node child)
        {
            switch (parent)
            {
                case ActionNode actionNode:
                    break;
                case CompositeNode compositeNode:
                    Undo.RecordObject(compositeNode, "Behaviour Tree (Add Child)");
                    compositeNode.children.Add(child);
                    EditorUtility.SetDirty(compositeNode);
                    break;
                case DecoratorNode decoratorNode:
                    Undo.RecordObject(decoratorNode, "Behaviour Tree (Add Child)");
                    decoratorNode.child = child;
                    EditorUtility.SetDirty(decoratorNode);
                    break;
                case RootNode root:
                    Undo.RecordObject(root, "Behaviour Tree (Add Child)");
                    root.child = child;
                    EditorUtility.SetDirty(root);
                    break;
            }
        }

        public void RemoveChild(Node parent, Node child)
        {
            switch (parent)
            {
                case ActionNode actionNode:
                    break;
                case CompositeNode compositeNode:
                    Undo.RecordObject(compositeNode, "Behaviour Tree (Add Child)");
                    compositeNode.children.Remove(child);
                    EditorUtility.SetDirty(compositeNode);
                    break;
                case DecoratorNode decoratorNode:
                    Undo.RecordObject(decoratorNode, "Behaviour Tree (Add Child)");
                    decoratorNode.child = null;
                    EditorUtility.SetDirty(decoratorNode);
                    break;
                case RootNode root:
                    Undo.RecordObject(root, "Behaviour Tree (Add Child)");
                    root.child = null;
                    EditorUtility.SetDirty(root);
                    break;
            }
        }

        public List<Node> GetChildren(Node parent)
        {
            List<Node> children = new List<Node>();
            switch (parent)
            {
                case ActionNode actionNode:
                    break;
                case CompositeNode compositeNode:
                    if (compositeNode.children.Count > 0)
                        children = compositeNode.children;
                    break;
                case DecoratorNode decoratorNode:
                    if (decoratorNode.child != null)
                        children.Add(decoratorNode.child);
                    break;
                case RootNode root:
                    if (root.child != null)
                        children.Add(root.child);
                    break;
            }

            return children;
        }

        public void Traverse(Node node, Action<Node> visitor)
        {
            if (node)
            {
                visitor.Invoke(node);
                var children = GetChildren(node);
                children.ForEach(c => Traverse(c, visitor));
            }
        }
        public BehaviourTree Clone()
        {
            BehaviourTree tree = Instantiate(this);
            tree.rootNode = rootNode.Clone();
            tree.nodes = new List<Node>();
            Traverse(tree.rootNode, node => tree.nodes.Add(node));
            return tree;
        }

        public void Bind()
        {
            Traverse(rootNode, node =>
            {
                node.blackBoard = blackBoard;
            });
        }
#endif
    }
}