using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class InspectorView : VisualElement
{
    private Editor _editor;
    public new class UxmlFactory : UxmlFactory<InspectorView, VisualElement.UxmlTraits>
    {
        
    }
    public InspectorView()
    {
        
    }

    internal void UpdateSelection(NodeView nodeView)
    {
        Clear();
        Object.DestroyImmediate(_editor);
        _editor = Editor.CreateEditor(nodeView.Node);
        IMGUIContainer container = new IMGUIContainer(() => { if (_editor.target)
            _editor.OnInspectorGUI(); });
        Add(container);
    }
}
