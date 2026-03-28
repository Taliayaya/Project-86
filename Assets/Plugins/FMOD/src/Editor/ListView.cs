using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    public class ListView : UnityEditorInternal.ReorderableList
    {
        private const float ElementPadding = 2;

        public DrawElementWithLabelDelegate drawElementWithLabelCallback;

        public delegate void DrawElementWithLabelDelegate(Rect rect, float labelRight, int index,
            bool active, bool focused);

        private float labelRight;

        public ListView(SerializedProperty property)
            : base(property.serializedObject, property, true, false, true, true)
        {
            headerHeight = 3;
            elementHeight = EditorGUIUtility.singleLineHeight + ElementPadding;
            drawElementCallback = DrawElementWrapper;
        }

        public void DrawLayout()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, GetHeight());

            labelRight = rect.x + EditorGUIUtility.labelWidth;

            DoList(EditorGUI.IndentedRect(rect));
        }

        private void DrawElementWrapper(Rect rect, int index, bool active, bool focused)
        {
            if (drawElementWithLabelCallback != null)
            {
                rect.height -= ElementPadding;

                drawElementWithLabelCallback(rect, labelRight, index, active, focused);
            }
        }
    }
}

