using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace FMODUnity
{
    [CustomPropertyDrawer(typeof(EventReference))]
    public class EventReferenceDrawer : PropertyDrawer
    {
        private static readonly Texture RepairIcon = EditorUtils.LoadImage("Wrench.png");
        private static readonly Texture WarningIcon = EditorUtils.LoadImage("NotFound.png");
        private static readonly GUIContent NotFoundWarning = new GUIContent("Event Not Found", WarningIcon);

        private static GUIStyle buttonStyle;

        private static Vector2 WarningSize()
        {
            return GUI.skin.label.CalcSize(NotFoundWarning);
        }

        private static float GetBaseHeight()
        {
            return GUI.skin.textField.CalcSize(GUIContent.none).y;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.padding.top = 1;
                buttonStyle.padding.bottom = 1;
            }

            Texture browseIcon = EditorUtils.LoadImage("SearchIconBlack.png");
            Texture openIcon = EditorUtils.LoadImage("BrowserIcon.png");
            Texture addIcon = EditorUtils.LoadImage("AddIcon.png");
            Texture copyIcon = EditorUtils.LoadImage("CopyIcon.png");

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                HandleDragEvents(position, property);

                EventReference eventReference = property.GetEventReference();
                EditorEventRef editorEventRef = GetEditorEventRef(eventReference);

                float baseHeight = GetBaseHeight();

                Rect headerRect = position;
                headerRect.width = EditorGUIUtility.labelWidth;
                headerRect.height = baseHeight;

                property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

                Rect addRect = new Rect(position.xMax - addIcon.width - 7, position.y, addIcon.width + 7, baseHeight);
                Rect openRect = new Rect(addRect.x - openIcon.width - 7, position.y, openIcon.width + 6, baseHeight);
                Rect searchRect = new Rect(openRect.x - browseIcon.width - 9, position.y, browseIcon.width + 8, baseHeight);
                Rect pathRect = position;
                pathRect.xMin = headerRect.xMax;
                pathRect.xMax = searchRect.x - 3;
                pathRect.height = baseHeight;

                SerializedProperty pathProperty = GetPathProperty(property);

                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.PropertyField(pathRect, pathProperty, GUIContent.none);

                    if (scope.changed)
                    {
                        SetEvent(property, pathProperty.stringValue);
                    }
                }

                if (GUI.Button(searchRect, new GUIContent(browseIcon, "Search"), buttonStyle))
                {
                    var eventBrowser = ScriptableObject.CreateInstance<EventBrowser>();

                    eventBrowser.ChooseEvent(property);
                    var windowRect = position;
                    windowRect.xMin = pathRect.xMin;
                    windowRect.position = GUIUtility.GUIToScreenPoint(windowRect.position);
                    windowRect.height = openRect.height + 1;
                    windowRect.width = Mathf.Max(windowRect.width, 300f);
                    eventBrowser.ShowAsDropDown(windowRect, new Vector2(windowRect.width, 400));

                }
                if (GUI.Button(addRect, new GUIContent(addIcon, "Create New Event in Studio"), buttonStyle))
                {
                    var addDropdown = EditorWindow.CreateInstance<CreateEventPopup>();

                    addDropdown.SelectEvent(property);
                    var windowRect = position;
                    windowRect.xMin = pathRect.xMin;
                    windowRect.position = GUIUtility.GUIToScreenPoint(windowRect.position);
                    windowRect.height = openRect.height + 1;
                    windowRect.width = Mathf.Max(windowRect.width, 300f);
                    addDropdown.ShowAsDropDown(windowRect, new Vector2(windowRect.width, 500));

                }
                if (GUI.Button(openRect, new GUIContent(openIcon, "Open In Browser"), buttonStyle))
                {
                    EventBrowser.ShowWindow();
                    EventBrowser eventBrowser = EditorWindow.GetWindow<EventBrowser>();
                    eventBrowser.FrameEvent(pathProperty.stringValue);
                }

                if (editorEventRef != null)
                {
                    float labelY = headerRect.y + baseHeight;

                    MismatchInfo mismatch = GetMismatch(eventReference, editorEventRef);

                    if (mismatch != null)
                    {
                        Rect warningRect = pathRect;
                        warningRect.xMax = position.xMax;
                        warningRect.y = labelY;
                        warningRect.height = WarningSize().y;

                        DrawMismatchUI(warningRect, openRect.x, openRect.width, mismatch, property);

                        labelY = warningRect.yMax;
                    }

                    if (property.isExpanded)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            Rect labelRect = EditorGUI.IndentedRect(headerRect);
                            labelRect.y = labelY;

                            Rect valueRect = labelRect;
                            valueRect.xMin = labelRect.xMax;
                            valueRect.xMax = position.xMax - copyIcon.width - 7;

                            GUI.Label(labelRect, new GUIContent("GUID"));
                            GUI.Label(valueRect, eventReference.Guid.ToString());

                            Rect copyRect = valueRect;
                            copyRect.xMin = valueRect.xMax;
                            copyRect.xMax = position.xMax;

                            if (GUI.Button(copyRect, new GUIContent(copyIcon, "Copy To Clipboard")))
                            {
                                EditorGUIUtility.systemCopyBuffer = eventReference.Guid.ToString();
                            }

                            valueRect.xMax = position.xMax;

                            labelRect.y += baseHeight;
                            valueRect.y += baseHeight;

                            GUI.Label(labelRect, new GUIContent("Banks"));
                            GUI.Label(valueRect, string.Join(", ", editorEventRef.Banks.Select(x => x.Name).ToArray()));
                            labelRect.y += baseHeight;
                            valueRect.y += baseHeight;

                            GUI.Label(labelRect, new GUIContent("Panning"));
                            GUI.Label(valueRect, editorEventRef.Is3D ? "3D" : "2D");
                            labelRect.y += baseHeight;
                            valueRect.y += baseHeight;

                            GUI.Label(labelRect, new GUIContent("Stream"));
                            GUI.Label(valueRect, editorEventRef.IsStream.ToString());
                            labelRect.y += baseHeight;
                            valueRect.y += baseHeight;

                            GUI.Label(labelRect, new GUIContent("Oneshot"));
                            GUI.Label(valueRect, editorEventRef.IsOneShot.ToString());
                            labelRect.y += baseHeight;
                            valueRect.y += baseHeight;
                        }
                    }
                }
                else
                {
                    EditorEventRef renamedEvent = GetRenamedEventRef(eventReference);

                    if (renamedEvent != null)
                    {
                        MismatchInfo mismatch = new MismatchInfo() {
                            Message = string.Format("Moved to {0}", renamedEvent.Path),
                            HelpText = string.Format(
                                "This event has been moved in FMOD Studio.\n" +
                                "You can click the repair button to update the path to the new location, or run " +
                                "the <b>{0}</b> command to scan your project for similar issues and fix them all.",
                                EventReferenceUpdater.MenuPath),
                            RepairTooltip = string.Format("Repair: set path to {0}", renamedEvent.Path),
                            RepairAction = (p) => {
                                p.FindPropertyRelative("Path").stringValue = renamedEvent.Path;
                            },
                        };

                        using (new EditorGUI.IndentLevelScope())
                        {
                            Rect mismatchRect = pathRect;

                            mismatchRect.xMin = position.xMin;
                            mismatchRect.xMax = position.xMax;
                            mismatchRect.y += baseHeight;
                            mismatchRect.height = baseHeight;

                            mismatchRect = EditorGUI.IndentedRect(mismatchRect);

                            DrawMismatchUI(mismatchRect, openRect.x, openRect.width, mismatch, property);
                        }
                    }
                    else
                    {
                        Rect labelRect = pathRect;
                        labelRect.xMax = position.xMax;
                        labelRect.y += baseHeight;
                        labelRect.height = WarningSize().y;

                        GUI.Label(labelRect, NotFoundWarning);
                    }
                }
            }
        }

        private static void HandleDragEvents(Rect position, SerializedProperty property)
        {
            Event e = Event.current;

            if (e.type == EventType.DragPerform && position.Contains(e.mousePosition))
            {
                if (DragAndDrop.objectReferences.Length > 0 &&
                    DragAndDrop.objectReferences[0] != null &&
                    DragAndDrop.objectReferences[0].GetType() == typeof(EditorEventRef))
                {
                    EditorEventRef eventRef = DragAndDrop.objectReferences[0] as EditorEventRef;

                    property.SetEventReference(eventRef.Guid, eventRef.Path);

                    GUI.changed = true;
                    e.Use();
                }
            }

            if (e.type == EventType.DragUpdated && position.Contains(e.mousePosition))
            {
                if (DragAndDrop.objectReferences.Length > 0 &&
                    DragAndDrop.objectReferences[0] != null &&
                    DragAndDrop.objectReferences[0].GetType() == typeof(EditorEventRef))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    DragAndDrop.AcceptDrag();
                    e.Use();
                }
            }
        }

        private class MismatchInfo
        {
            public string Message;
            public string HelpText;
            public string RepairTooltip;
            public Action<SerializedProperty> RepairAction;
        }

        private static void DrawMismatchUI(Rect rect, float repairButtonX, float repairButtonWidth,
            MismatchInfo mismatch, SerializedProperty property)
        {
            rect = EditorUtils.DrawHelpButton(rect, () => new SimpleHelp(mismatch.HelpText, 400));

            Rect repairRect = new Rect(repairButtonX, rect.y, repairButtonWidth, GetBaseHeight());

            if (GUI.Button(repairRect, new GUIContent(RepairIcon, mismatch.RepairTooltip), buttonStyle))
            {
                mismatch.RepairAction(property);
            }

            Rect labelRect = rect;
            labelRect.xMax = repairRect.xMin;

            GUI.Label(labelRect, new GUIContent(mismatch.Message, WarningIcon));
        }

        private static MismatchInfo GetMismatch(EventReference eventReference, EditorEventRef editorEventRef)
        {
            if (EventManager.GetEventLinkage(eventReference) == EventLinkage.Path)
            {
                if (eventReference.Guid != editorEventRef.Guid)
                {
                    return new MismatchInfo() {
                        Message = "GUID doesn't match path",
                        HelpText = string.Format(
                            "The GUID on this EventReference doesn't match the path.\n" +
                            "You can click the repair button to update the GUID to match the path, or run the " +
                            "<b>{0}</b> command to scan your project for similar issues and fix them all.",
                            EventReferenceUpdater.MenuPath),
                        RepairTooltip = string.Format("Repair: set GUID to {0}", editorEventRef.Guid),
                        RepairAction = (property) => {
                            property.FindPropertyRelative("Guid").SetGuid(editorEventRef.Guid);
                        },
                    };
                }
            }
            else // EventLinkage.GUID
            {
                if (eventReference.Path != editorEventRef.Path)
                {
                    return new MismatchInfo() {
                        Message = "Path doesn't match GUID",
                        HelpText = string.Format(
                            "The path on this EventReference doesn't match the GUID.\n" +
                            "You can click the repair button to update the path to match the GUID, or run the " +
                            "<b>{0}</b> command to scan your project for similar issues and fix them all.",
                            EventReferenceUpdater.MenuPath),
                        RepairTooltip = string.Format("Repair: set path to '{0}'", editorEventRef.Path),
                        RepairAction = (property) => {
                            property.FindPropertyRelative("Path").stringValue = editorEventRef.Path;
                        },
                    };
                }
            }

            return null;
        }

        private static void SetEvent(SerializedProperty property, string path)
        {
            EditorEventRef eventRef = EventManager.EventFromPath(path);

            if (eventRef != null)
            {
                property.SetEventReference(eventRef.Guid, eventRef.Path);
            }
            else
            {
                property.SetEventReference(new FMOD.GUID(), path);
            }
        }

        private static SerializedProperty GetGuidProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("Guid");
        }

        private static SerializedProperty GetPathProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("Path");
        }

        private static EditorEventRef GetEditorEventRef(EventReference eventReference)
        {
            if (EventManager.GetEventLinkage(eventReference) == EventLinkage.Path)
            {
                return EventManager.EventFromPath(eventReference.Path);
            }
            else // Assume EventLinkage.GUID
            {
                return EventManager.EventFromGUID(eventReference.Guid);
            }
        }

        private static EditorEventRef GetRenamedEventRef(EventReference eventReference)
        {
            if (Settings.Instance.EventLinkage == EventLinkage.Path && !eventReference.Guid.IsNull)
            {
                EditorEventRef editorEventRef = EventManager.EventFromGUID(eventReference.Guid);

                if (editorEventRef != null && editorEventRef.Path != eventReference.Path)
                {
                    return editorEventRef;
                }
            }

            return null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = GetBaseHeight();

            EventReference eventReference = property.GetEventReference();
            EditorEventRef editorEventRef = GetEditorEventRef(eventReference);

            if (editorEventRef == null)
            {
                return baseHeight + WarningSize().y;
            }
            else
            {
                float height;

                if (property.isExpanded)
                {
                    height = baseHeight * 6; // 5 lines of info
                }
                else
                {
                    height = baseHeight;
                }

                if (GetMismatch(eventReference, editorEventRef) != null)
                {
                    height += WarningSize().y;
                }

                return height;
            }
        }
    }

#pragma warning disable 0618 // Suppress the warning about using the obsolete EventRefAttribute class
    [CustomPropertyDrawer(typeof(EventRefAttribute))]
#pragma warning restore 0618
    public class LegacyEventRefDrawer : PropertyDrawer
    {
        private GUIStyle RichTextStyle;

        private const string HelpText =
            "This field has the <b>[FMODUnity.EventRef]</b> attribute, which is obsolete.\n" +
            "To resolve this issue:\n" +
            "* Add a field of type <b>FMODUnity.EventReference</b> to this class\n" +
            "* Set the <b>MigrateTo</b> property on the <b>[FMODUnity.EventRef]</b> attribute: " +
            "<b>[FMODUnity.EventRef(MigrateTo=\"yourNewFieldName\")]</b>\n" +
            "* Run the <b>" + EventReferenceUpdater.MenuPath + "</b> command to " +
            "automatically migrate values from this field to the <b>FMODUnity.EventReference</b> field";

        private static readonly Texture InfoIcon = EditorGUIUtility.IconContent("console.infoicon.sml").image;
        private static readonly Texture WarningIcon = EditorUtils.LoadImage("NotFound.png");

        private void AffirmStyles()
        {
            if (RichTextStyle == null)
            {
                RichTextStyle = new GUIStyle(GUI.skin.label) { richText = true };
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            AffirmStyles();

            label = EditorGUI.BeginProperty(position, label, property);

            Rect pathRect = position;
            pathRect.height = EditorGUIUtility.singleLineHeight;

            pathRect = EditorGUI.PrefixLabel(pathRect, label);
            EditorGUI.PropertyField(pathRect, property, GUIContent.none);

            using (new EditorGUI.IndentLevelScope())
            {
                GUIContent content = StatusContent(property);

                Rect infoRect = EditorGUI.IndentedRect(position);
                infoRect.y = pathRect.yMax;
                infoRect.height = StatusSize(content).y;

                infoRect = EditorUtils.DrawHelpButton(infoRect, () => new SimpleHelp(HelpText, 400));

                GUI.Label(infoRect, content, RichTextStyle);
            }

            EditorGUI.EndProperty();
        }

        private GUIContent StatusContent(SerializedProperty property)
        {
#pragma warning disable 0618 // Suppress the warning about using the obsolete EventRefAttribute class
            string migrationTarget = (attribute as EventRefAttribute).MigrateTo;
#pragma warning restore 0618

            if (string.IsNullOrEmpty(migrationTarget))
            {
                return new GUIContent("<b>[FMODUnity.EventRef]</b> is obsolete - use the <b>FMODUnity.EventReference</b> type instead.",
                    WarningIcon);

            }
            else
            {
                int parentPathLength = property.propertyPath.LastIndexOf('.');

                if (parentPathLength >= 0)
                {
                    migrationTarget = string.Format("{0}.{1}", property.propertyPath.Remove(parentPathLength), migrationTarget);
                }

                SerializedProperty targetProperty = property.serializedObject.FindProperty(migrationTarget);

                if (targetProperty != null)
                {
                    return new GUIContent(string.Format("Will be migrated to <b>{0}</b>", targetProperty.displayName),
                        InfoIcon);
                }
                else
                {
                    return new GUIContent(string.Format("Migration target <b>{0}</b> is missing", migrationTarget),
                        WarningIcon);
                }
            }
        }

        private Vector2 StatusSize(GUIContent content)
        {
            AffirmStyles();

            return Vector2.Max(RichTextStyle.CalcSize(content), EditorUtils.GetHelpButtonSize());
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + StatusSize(StatusContent(property)).y;
        }
    }
}
