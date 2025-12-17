using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace FMODUnity
{
    public class CreateEventPopup : EditorWindow
    {
        private class FolderEntry
        {
            public FolderEntry parent;
            public string name;
            public string guid;
            public List<FolderEntry> entries = new List<FolderEntry>();
            public Rect rect;
        }

        private SerializedProperty outputProperty;

        private FolderEntry rootFolder;
        private FolderEntry currentFolder;
        private List<BankEntry> banks;

        private int lastHover = 0;
        private string eventFolder = "/";
        private string eventName = "";
        private string currentFilter = "";
        private int selectedBank = 0;
        private bool resetCursor = true;
        private Vector2 scrollPos = new Vector2();
        private Rect scrollRect = new Rect();
        private bool isConnected = false;

        internal void SelectEvent(SerializedProperty property)
        {
            outputProperty = property;
        }

        private class BankEntry
        {
            public string name;
            public string guid;
        }

        public CreateEventPopup()
        {
        }

        private void BuildTree()
        {
            var rootGuid = EditorUtils.GetScriptOutput("studio.project.workspace.masterEventFolder.id");
            rootFolder = new FolderEntry();
            rootFolder.guid = rootGuid;
            BuildTreeItem(rootFolder);
            wantsMouseMove = true;
            banks = new List<BankEntry>();

            const string buildBankTreeFunc =
                @"function() {
                    var output = """";
                    const items = [ studio.project.workspace.masterBankFolder ];
                    while (items.length > 0) {
                        var currentItem = items.shift();
                        if (currentItem.isOfType(""BankFolder"")) {
                            currentItem.items.reverse().forEach(function(val) {
                                items.unshift(val);
                            });
                        } else {
                            output += "","" + currentItem.id + currentItem.getPath().replace(""bank:/"", """");
                        }
                    }
                    return output;
                }";

            string bankList = EditorUtils.GetScriptOutput(string.Format("({0})()", buildBankTreeFunc));
            string[] bankListSplit = bankList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var bank in bankListSplit)
            {
                var entry = new BankEntry();
                entry.guid = bank.Substring(0, 38);
                entry.name = bank.Substring(38);
                banks.Add(entry);
            }
        }

        private void BuildTreeItem(FolderEntry entry)
        {
            // lookup the entry
            EditorUtils.GetScriptOutput(string.Format("cur = studio.project.lookup(\"{0}\");", entry.guid));

            // get child count
            string itemCountString = EditorUtils.GetScriptOutput("cur.items.length;");
            int itemCount;
            Int32.TryParse(itemCountString, out itemCount);

            // iterate children looking for folder
            for (int item = 0; item < itemCount; item++)
            {
                EditorUtils.GetScriptOutput(String.Format("child = cur.items[{0}]", item));

                // check if it's a folder
                string isFolder = EditorUtils.GetScriptOutput("child.isOfExactType(\"EventFolder\")");
                if (isFolder == "false")
                {
                    continue;
                }

                // Get guid and name
                string info = EditorUtils.GetScriptOutput("child.id + child.name");

                var childEntry = new FolderEntry();
                childEntry.guid = info.Substring(0, 38);
                childEntry.name = info.Substring(38);
                childEntry.parent = entry;
                entry.entries.Add(childEntry);
            }

            // Recurse for child entries
            foreach(var childEntry in entry.entries)
            {
                BuildTreeItem(childEntry);
            }
        }

        public void OnGUI()
        {
            var borderIcon = EditorUtils.LoadImage("Border.png");
            var border = new GUIStyle(GUI.skin.box);
            border.normal.background = borderIcon;
            GUI.Box(new Rect(1, 1, position.width - 1, position.height - 1), GUIContent.none, border);

            if (Event.current.type == EventType.Layout)
            {
                isConnected = EditorUtils.IsConnectedToStudio();
            }

            if (!isConnected)
            {
                this.ShowNotification(new GUIContent("FMOD Studio not running"));
                return;
            }

            this.RemoveNotification();

            if (rootFolder == null)
            {
                BuildTree();
                currentFolder = rootFolder;
            }

            var arrowIcon = EditorUtils.LoadImage("ArrowIcon.png");
            var hoverIcon = EditorUtils.LoadImage("SelectedAlt.png");

            var nextEntry = currentFolder;

            var filteredEntries = currentFolder.entries.FindAll((x) => x.name.StartsWith(currentFilter, StringComparison.CurrentCultureIgnoreCase));

            // Process key strokes for the folder list
            {
                if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    if (Event.current.type == EventType.KeyDown)
                    {
                        lastHover = Math.Max(lastHover - 1, 0);
                        if (filteredEntries[lastHover].rect.y < scrollPos.y)
                        {
                            scrollPos.y = filteredEntries[lastHover].rect.y;
                        }
                    }
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    if (Event.current.type == EventType.KeyDown)
                    {
                        lastHover = Math.Min(lastHover + 1, filteredEntries.Count - 1);
                        if (filteredEntries[lastHover].rect.y + filteredEntries[lastHover].rect.height > scrollPos.y + scrollRect.height)
                        {
                            scrollPos.y = filteredEntries[lastHover].rect.y - scrollRect.height + filteredEntries[lastHover].rect.height * 2;
                        }
                    }
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.RightArrow)
                {
                    if (Event.current.type == EventType.KeyDown)
                        nextEntry = filteredEntries[lastHover];
                    Event.current.Use();
                }
                if (Event.current.keyCode == KeyCode.LeftArrow)
                {
                    if (Event.current.type == EventType.KeyDown)
                        if (currentFolder.parent != null)
                        {
                            nextEntry = currentFolder.parent;
                        }
                    Event.current.Use();
                }
            }

            bool disabled = eventName.Length == 0;
            EditorGUI.BeginDisabledGroup(disabled);
            if (GUILayout.Button("Create Event"))
            {
                CreateEventInStudio();
                this.Close();
            }
            EditorGUI.EndDisabledGroup();

            {
                GUI.SetNextControlName("name");

                EditorGUILayout.LabelField("Name");
                eventName = EditorGUILayout.TextField(eventName);
            }

            {
                EditorGUILayout.LabelField("Bank");
                selectedBank = EditorGUILayout.Popup(selectedBank, banks.Select(x => x.name).ToArray());
            }

            bool updateEventPath = false;
            {
                GUI.SetNextControlName("folder");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.LabelField("Path");
                eventFolder = GUILayout.TextField(eventFolder);
                if (EditorGUI.EndChangeCheck())
                {
                    updateEventPath = true;
                }
            }

            if (resetCursor)
            {
                resetCursor = false;

                var textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                if (textEditor != null)
                {
                    textEditor.MoveCursorToPosition(new Vector2(9999, 9999));
                }
            }

            // Draw the current folder as a title bar, click to go back one level
            {
                Rect currentRect = EditorGUILayout.GetControlRect();

                var bg = new GUIStyle(GUI.skin.box);
                Rect bgRect = new Rect(currentRect);
                bgRect.x = 2;
                bgRect.width = position.width-4;
                GUI.Box(bgRect, GUIContent.none, bg);

                Rect textureRect = currentRect;
                textureRect.width = arrowIcon.width;
                if (currentFolder.name != null)
                {
                    GUI.DrawTextureWithTexCoords(textureRect, arrowIcon, new Rect(1, 1, -1, -1));
                }

                Rect labelRect = currentRect;
                labelRect.x += arrowIcon.width;
                labelRect.width -= arrowIcon.width;
                GUI.Label(labelRect, currentFolder.name != null ? currentFolder.name : "Folders", EditorStyles.boldLabel);

                if (Event.current.type == EventType.MouseDown && currentRect.Contains(Event.current.mousePosition) &&
                    currentFolder.parent != null)
                {
                    nextEntry = currentFolder.parent;
                    Event.current.Use();
                }
            }

            var normal = new GUIStyle(GUI.skin.label);
            normal.padding.left = 14;
            var hover = new GUIStyle(normal);
            hover.normal.background = hoverIcon;

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

            for (int i = 0; i < filteredEntries.Count; i++)
            {
                var entry = filteredEntries[i];
                var content = new GUIContent(entry.name);
                var rect = EditorGUILayout.GetControlRect();
                if ((rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseMove) || i == lastHover)
                {
                    lastHover = i;

                    GUI.Label(rect, content, hover);
                    if (rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
                    {
                        nextEntry = entry;
                    }
                }
                else
                {
                    GUI.Label(rect, content, normal);
                }

                Rect textureRect = rect;
                textureRect.x = textureRect.width - arrowIcon.width;
                textureRect.width = arrowIcon.width;
                GUI.DrawTexture(textureRect, arrowIcon);

                if (Event.current.type == EventType.Repaint)
                {
                    entry.rect = rect;
                }
            }
            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
            {
                scrollRect = GUILayoutUtility.GetLastRect();
            }

            if (currentFolder != nextEntry)
            {
                lastHover = 0;
                currentFolder = nextEntry;
                UpdateTextFromList();
                Repaint();
            }

            if (updateEventPath)
            {
                UpdateListFromText();
            }

            if (Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }
        }

        private void CreateEventInStudio()
        {
            string eventGuid = EditorUtils.CreateStudioEvent(eventFolder, eventName);

            if (!string.IsNullOrEmpty(eventGuid))
            {
                EditorUtils.GetScriptOutput(String.Format("studio.project.lookup(\"{0}\").relationships.banks.add(studio.project.lookup(\"{1}\"));", eventGuid, banks[selectedBank].guid));
                EditorUtils.GetScriptOutput("studio.project.build();");

                if (!eventFolder.EndsWith("/"))
                {
                    eventFolder += "/";
                }

                string fullPath = "event:" + eventFolder + eventName;
                outputProperty.SetEventReference(FMOD.GUID.Parse(eventGuid), fullPath);
                EditorUtils.UpdateParamsOnEmitter(outputProperty.serializedObject, fullPath);
                outputProperty.serializedObject.ApplyModifiedProperties();
            }
        }

        private void UpdateListFromText()
        {
            int endFolders = eventFolder.LastIndexOf("/");
            currentFilter = eventFolder.Substring(endFolders + 1);

            var folders = eventFolder.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            FolderEntry entry = rootFolder;
            int i;
            for (i = 0; i < folders.Length; i++)
            {
                var newEntry = entry.entries.Find((x) => x.name.Equals(folders[i], StringComparison.CurrentCultureIgnoreCase));
                if (newEntry == null)
                {
                    break;
                }
                entry = newEntry;
            }
            currentFolder = entry;

            // Treat an exact filter match as being in that folder and clear the filter
            if (entry.name != null && entry.name.Equals(currentFilter, StringComparison.CurrentCultureIgnoreCase))
            {
                currentFilter = "";
            }
        }

        private void UpdateTextFromList()
        {
            string path = "";
            var entry = currentFolder;
            while (entry.parent != null)
            {
                path = entry.name + "/" + path;
                entry = entry.parent;
            }

            eventFolder = "/" + path;
            resetCursor = true;
            currentFilter = "";
        }
    }
}
