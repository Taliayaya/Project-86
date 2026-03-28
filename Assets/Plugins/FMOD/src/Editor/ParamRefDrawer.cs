using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    [CustomPropertyDrawer(typeof(ParamRefAttribute))]
    public class ParamRefDrawer : PropertyDrawer
    {
        public bool MouseDrag(Event e)
        {
            bool isDragging = false;

            if (e.type == EventType.DragPerform)
            {
                isDragging = true;
            }

            return isDragging;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Texture browseIcon = EditorUtils.LoadImage("SearchIconBlack.png");
            Texture openIcon = EditorUtils.LoadImage("BrowserIcon.png");
            Texture addIcon = EditorUtils.LoadImage("AddIcon.png");

            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty pathProperty = property;

            Event e = Event.current;
            if (MouseDrag(e) && position.Contains(e.mousePosition))
            {
                if (DragAndDrop.objectReferences.Length > 0 &&
                    DragAndDrop.objectReferences[0] != null &&
                    DragAndDrop.objectReferences[0].GetType() == typeof(EditorParamRef))
                {
                    pathProperty.stringValue = ((EditorParamRef)DragAndDrop.objectReferences[0]).Name;
                    GUI.changed = true;
                    e.Use();
                }
            }
            if (e.type == EventType.DragUpdated && position.Contains(e.mousePosition))
            {
                if (DragAndDrop.objectReferences.Length > 0 &&
                    DragAndDrop.objectReferences[0] != null &&
                    DragAndDrop.objectReferences[0].GetType() == typeof(EditorParamRef))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    DragAndDrop.AcceptDrag();
                    e.Use();
                }
            }

            float baseHeight = GUI.skin.textField.CalcSize(new GUIContent()).y;

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding.top = 1;
            buttonStyle.padding.bottom = 1;

            Rect addRect = new Rect(position.x + position.width - addIcon.width - 7, position.y, addIcon.width + 7, baseHeight);
            Rect openRect = new Rect(addRect.x - openIcon.width - 7, position.y, openIcon.width + 6, baseHeight);
            Rect searchRect = new Rect(openRect.x - browseIcon.width - 9, position.y, browseIcon.width + 8, baseHeight);
            Rect pathRect = new Rect(position.x, position.y, searchRect.x - position.x - 3, baseHeight);

            EditorGUI.PropertyField(pathRect, pathProperty, GUIContent.none);

            if (GUI.Button(searchRect, new GUIContent(browseIcon, "Search"), buttonStyle))
            {
                var eventBrowser = ScriptableObject.CreateInstance<EventBrowser>();

                eventBrowser.ChooseParameter(property);
                var windowRect = position;
                windowRect.position = GUIUtility.GUIToScreenPoint(windowRect.position);
                windowRect.height = openRect.height + 1;
                eventBrowser.ShowAsDropDown(windowRect, new Vector2(windowRect.width, 400));
            }
        }
    }
}
