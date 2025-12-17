using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using System.IO;

namespace FMODUnity
{
    public class EventBrowser : EditorWindow, ISerializationCallbackReceiver
    {
        [SerializeField]
        private bool isStandaloneWindow;

        [NonSerialized]
        private float nextRepaintTime;

        [NonSerialized]
        private float[] cachedMetering;

        private const float RepaintInterval = 1 / 30.0f;

        private Texture2D borderIcon;
        private GUIStyle borderStyle;

        [NonSerialized]
        private TreeView treeView;

        [NonSerialized]
        private SearchField searchField;

        [SerializeField]
        private PreviewArea previewArea = new PreviewArea();

        [SerializeField]
        private TreeView.State treeViewState;

        [NonSerialized]
        private DateTime LastKnownCacheTime;

        private SerializedProperty outputProperty;

        public static FMOD.Studio.EventInstance PreviewEventInstance { get; private set; }

        [MenuItem("FMOD/Event Browser", priority = 2)]
        public static void ShowWindow()
        {
            EventBrowser eventBrowser = GetWindow<EventBrowser>("FMOD Events");
            eventBrowser.minSize = new Vector2(380, 600);

            eventBrowser.BeginStandaloneWindow();

            EditorUtils.LoadPreviewBanks();
        }

        public static bool IsOpen
        {
            get; private set;
        }

        public void OnBeforeSerialize()
        {
            treeViewState = treeView.state;
        }

        public void OnAfterDeserialize()
        {
        }

        private void Update()
        {
            bool forceRepaint = false;

            float[] currentMetering = EditorUtils.GetMetering();
            if (cachedMetering == null || !cachedMetering.SequenceEqual(currentMetering))
            {
                cachedMetering = currentMetering;
                forceRepaint = true;
            }

            if (LastKnownCacheTime != EventManager.CacheTime)
            {
                ReadEventCache();
                forceRepaint = true;
            }

            if (forceRepaint || (previewArea != null && previewArea.forceRepaint && nextRepaintTime < Time.realtimeSinceStartup))
            {
                Repaint();
                nextRepaintTime = Time.time + RepaintInterval;
            }
        }

        private void ReadEventCache()
        {
            LastKnownCacheTime = EventManager.CacheTime;
            treeView.Reload();
        }

        private class TreeView : UnityEditor.IMGUI.Controls.TreeView
        {
            private static readonly Texture2D folderOpenIcon = EditorUtils.LoadImage("FolderIconOpen.png");
            private static readonly Texture2D folderClosedIcon = EditorUtils.LoadImage("FolderIconClosed.png");
            private static readonly Texture2D eventIcon = EditorUtils.LoadImage("EventIcon.png");
            private static readonly Texture2D snapshotIcon = EditorUtils.LoadImage("SnapshotIcon.png");
            private static readonly Texture2D bankIcon = EditorUtils.LoadImage("BankIcon.png");
            private static readonly Texture2D continuousParameterIcon = EditorUtils.LoadImage("ContinuousParameterIcon.png");
            private static readonly Texture2D discreteParameterIcon = EditorUtils.LoadImage("DiscreteParameterIcon.png");
            private static readonly Texture2D labeledParameterIcon = EditorUtils.LoadImage("LabeledParameterIcon.png");

            private Dictionary<string, int> itemIDs = new Dictionary<string, int>();

            private const string EventPrefix = "event:/";
            private const string SnapshotPrefix = "snapshot:/";
            private const string BankPrefix = "bank:/";
            private const string ParameterPrefix = "parameter:/";

            bool expandNextFolderSet = false;
            string nextFramedItemPath;
            private string[] searchStringSplit;

            IList<int> noSearchExpandState;

            float oldBaseIndent;

            public TreeView(State state) : base(state.baseState)
            {
                noSearchExpandState = state.noSearchExpandState;
                SelectedObject = state.selectedObject;
                TypeFilter = state.typeFilter;
                DragEnabled = state.dragEnabled;

                for (int i = 0; i < state.itemPaths.Count; ++i)
                {
                    itemIDs.Add(state.itemPaths[i], state.itemIDs[i]);
                }
            }

            public void JumpToEvent(string path)
            {
                JumpToItem(path);
            }

            public void JumpToBank(string name)
            {
                JumpToItem(BankPrefix + name);
            }

            private void JumpToItem(string path)
            {
                nextFramedItemPath = path;
                Reload();

                int itemID;
                if (itemIDs.TryGetValue(path, out itemID))
                {
                    SetSelection(new List<int> { itemID },
                        TreeViewSelectionOptions.RevealAndFrame | TreeViewSelectionOptions.FireSelectionChanged);
                }
                else
                {
                    SetSelection(new List<int>());
                }
            }

            private class LeafItem : TreeViewItem
            {
                public LeafItem(int id, int depth, ScriptableObject data)
                    : base(id, depth)
                {
                    Data = data;
                }

                public ScriptableObject Data;
            }

            private class FolderItem : TreeViewItem
            {
                public FolderItem(int id, int depth, string displayName)
                    : base(id, depth, displayName)
                {
                }
            }

            private FolderItem CreateFolderItem(string name, string path, bool hasChildren, bool forceExpanded,
                TreeViewItem parent)
            {
                FolderItem item = new FolderItem(AffirmItemID("folder:" + path), 0, name);

                bool expanded;

                if (!hasChildren)
                {
                    expanded = false;
                }
                else if (forceExpanded || expandNextFolderSet
                    || (nextFramedItemPath != null && nextFramedItemPath.StartsWith(path)))
                {
                    SetExpanded(item.id, true);
                    expanded = true;
                }
                else
                {
                    expanded = IsExpanded(item.id);
                }

                if (expanded)
                {
                    item.icon = folderOpenIcon;
                }
                else
                {
                    item.icon = folderClosedIcon;

                    if (hasChildren)
                    {
                        item.children = CreateChildListForCollapsedParent();
                    }
                }

                parent.AddChild(item);

                return item;
            }

            protected override TreeViewItem BuildRoot()
            {
                return new TreeViewItem(-1, -1);
            }

            private int AffirmItemID(string path)
            {
                int id;

                if (!itemIDs.TryGetValue(path, out id))
                {
                    id = itemIDs.Count;
                    itemIDs.Add(path, id);
                }

                return id;
            }

            public TypeFilter TypeFilter { get; set; }
            public bool DragEnabled { get; set; }

            protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
            {
                if (hasSearch)
                {
                    searchStringSplit = searchString.Split(' ');
                }

                if (rootItem.children != null)
                {
                    rootItem.children.Clear();
                }

                if ((TypeFilter & TypeFilter.Event) != 0)
                {
                    CreateSubTree(L10n.Tr("Events"), EventPrefix,
                        EventManager.Events.Where(e => e.Path.StartsWith(EventPrefix)), e => e.Path);

                    CreateSubTree(L10n.Tr("Snapshots"), SnapshotPrefix,
                        EventManager.Events.Where(e => e.Path.StartsWith(SnapshotPrefix)), s => s.Path);
                }

                if ((TypeFilter & TypeFilter.Bank) != 0)
                {
                    CreateSubTree(L10n.Tr("Banks"), BankPrefix, EventManager.Banks, b => b.StudioPath);
                }

                if ((TypeFilter & TypeFilter.Parameter) != 0)
                {
                    CreateSubTree(L10n.Tr("Global Parameters"), ParameterPrefix,
                        EventManager.Parameters, p => p.StudioPath);
                }

                List<TreeViewItem> rows = new List<TreeViewItem>();

                AddChildrenInOrder(rows, rootItem);

                SetupDepthsFromParentsAndChildren(rootItem);

                expandNextFolderSet = false;
                nextFramedItemPath = null;

                return rows;
            }

            private static NaturalComparer naturalComparer = new NaturalComparer();

            private void CreateSubTree<T>(string rootName, string rootPath,
                IEnumerable<T> sourceRecords, Func<T, string> GetPath,
                Func<string, T, string> MakeUniquePath = null)
                where T : ScriptableObject
            {
                var records = sourceRecords.Select(r => new { source = r, path = GetPath(r) });

                if (hasSearch)
                {
                    records = records.Where(r => {
                        foreach (var word in searchStringSplit)
                        {
                            if (word.Length > 0 && r.path.IndexOf(word, StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                return false;
                            }
                        }
                        return true;
                    });
                }

                records = records.OrderBy(r => r.path, naturalComparer);

                TreeViewItem root =
                    CreateFolderItem(rootName, rootPath, records.Any(), TypeFilter != TypeFilter.All, rootItem);

                List<TreeViewItem> currentFolderItems = new List<TreeViewItem>();

                foreach (var record in records)
                {
                    string leafName;
                    TreeViewItem parent = CreateFolderItems(record.path, currentFolderItems, root, out leafName);

                    if (parent != null)
                    {
                        string uniquePath;

                        if (MakeUniquePath != null)
                        {
                            uniquePath = MakeUniquePath(record.path, record.source);
                        }
                        else
                        {
                            uniquePath = record.path;
                        }

                        TreeViewItem leafItem = new LeafItem(AffirmItemID(uniquePath), 0, record.source);
                        leafItem.displayName = leafName;
                        leafItem.icon = IconForRecord(record.source);

                        parent.AddChild(leafItem);
                    }
                }
            }

            private Texture2D IconForRecord(ScriptableObject record)
            {
                EditorEventRef eventRef = record as EditorEventRef;
                if (eventRef != null)
                {
                    if (eventRef.Path.StartsWith(SnapshotPrefix))
                    {
                        return snapshotIcon;
                    }
                    else
                    {
                        return eventIcon;
                    }
                }

                EditorBankRef bankRef = record as EditorBankRef;
                if (bankRef != null)
                {
                    return bankIcon;
                }

                EditorParamRef paramRef = record as EditorParamRef;
                if (paramRef != null)
                {
                    switch(paramRef.Type)
                    {
                        case ParameterType.Continuous:
                            return continuousParameterIcon;
                        case ParameterType.Discrete:
                            return discreteParameterIcon;
                        case ParameterType.Labeled:
                            return labeledParameterIcon;
                    }
                }

                return null;
            }

            private TreeViewItem CreateFolderItems(string path, List<TreeViewItem> currentFolderItems,
                TreeViewItem root, out string leafName)
            {
                TreeViewItem parent = root;

                char separator = '/';

                // Skip the type prefix at the start of the path
                int elementStart = path.IndexOf(separator) + 1;

                for (int i = 0; ; ++i)
                {
                    if (!IsExpanded(parent.id))
                    {
                        leafName = null;
                        return null;
                    }

                    int elementEnd = path.IndexOf(separator, elementStart);

                    if (elementEnd < 0)
                    {
                        // No more folders; elementStart points to the event name
                        break;
                    }

                    string folderName = path.Substring(elementStart, elementEnd - elementStart);

                    if (i < currentFolderItems.Count && folderName != currentFolderItems[i].displayName)
                    {
                        currentFolderItems.RemoveRange(i, currentFolderItems.Count - i);
                    }

                    if (i == currentFolderItems.Count)
                    {
                        FolderItem folderItem =
                            CreateFolderItem(folderName, path.Substring(0, elementEnd), true, false, parent);

                        currentFolderItems.Add(folderItem);
                    }

                    elementStart = elementEnd + 1;
                    parent = currentFolderItems[i];
                }

                leafName = path.Substring(elementStart);
                return parent;
            }

            private static void AddChildrenInOrder(List<TreeViewItem> list, TreeViewItem item)
            {
                if (item.children != null)
                {
                    foreach (TreeViewItem child in item.children.Where(child => child is FolderItem))
                    {
                        list.Add(child);

                        AddChildrenInOrder(list, child);
                    }

                    foreach (TreeViewItem child in item.children.Where(child => !(child == null || child is FolderItem)))
                    {
                        list.Add(child);
                    }
                }
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }

            protected override bool CanChangeExpandedState(TreeViewItem item)
            {
                return item.hasChildren;
            }

            protected override bool CanStartDrag(CanStartDragArgs args)
            {
                if (DragEnabled && args.draggedItem is LeafItem)
                {
                    return IsDraggable((args.draggedItem as LeafItem).Data);
                }
                else
                {
                    return false;
                }
            }

            protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
            {
                IList<TreeViewItem> items = FindRows(args.draggedItemIDs);

                if (items[0] is LeafItem)
                {
                    LeafItem item = items[0] as LeafItem;

                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new UnityEngine.Object[] { Instantiate(item.Data) };

                    string title = string.Empty;

                    if (item.Data is EditorEventRef)
                    {
                        title = L10n.Tr("New FMOD Studio Emitter");
                    }
                    else if (item.Data is EditorBankRef)
                    {
                        title = L10n.Tr("New FMOD Studio Bank Loader");
                    }
                    else if (item.Data is EditorParamRef)
                    {
                        title = L10n.Tr("New FMOD Studio Global Parameter Trigger");
                    }

                    DragAndDrop.StartDrag(title);
                }
            }

            protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
            {
                return DragAndDropVisualMode.None;
            }

            protected override void SearchChanged(string newSearch)
            {
                if (!string.IsNullOrEmpty(newSearch.Trim()))
                {
                    expandNextFolderSet = true;

                    if (noSearchExpandState == null)
                    {
                        // A new search is beginning
                        noSearchExpandState = GetExpanded();
                        SetExpanded(new List<int>());
                    }
                }
                else
                {
                    if (noSearchExpandState != null)
                    {
                        // A search is ending
                        SetExpanded(noSearchExpandState);
                        noSearchExpandState = null;
                    }
                }
            }

            public ScriptableObject SelectedObject { get; private set; }
            public ScriptableObject DoubleClickedObject { get; private set; }

            protected override void SelectionChanged(IList<int> selectedIDs)
            {
                SelectedObject = null;

                if (selectedIDs.Count > 0)
                {
                    TreeViewItem item = FindItem(selectedIDs[0], rootItem);

                    if (item is LeafItem)
                    {
                        SelectedObject = (item as LeafItem).Data;
                    }
                }
            }

            protected override void DoubleClickedItem(int id)
            {
                TreeViewItem item = FindItem(id, rootItem);

                if (item is LeafItem)
                {
                    DoubleClickedObject = (item as LeafItem).Data;
                }
            }

            protected override void BeforeRowsGUI()
            {
                oldBaseIndent = baseIndent;
                DoubleClickedObject = null;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (hasSearch)
                {
                    // Hack to undo TreeView flattening the hierarchy when searching
                    baseIndent = oldBaseIndent + args.item.depth * depthIndentWidth;
                }

                base.RowGUI(args);

                TreeViewItem item = args.item;

                if (Event.current.type == EventType.MouseUp && item is FolderItem && item.hasChildren)
                {
                    Rect rect = args.rowRect;
                    rect.xMin = GetContentIndent(item);

                    if (rect.Contains(Event.current.mousePosition))
                    {
                        SetExpanded(item.id, !IsExpanded(item.id));
                        Event.current.Use();
                    }
                }
            }

            protected override void AfterRowsGUI()
            {
                baseIndent = oldBaseIndent;
            }

            [Serializable]
            public class State
            {
                public TreeViewState baseState;
                public List<int> noSearchExpandState;
                public ScriptableObject selectedObject;
                public List<string> itemPaths = new List<string>();
                public List<int> itemIDs = new List<int>();
                public TypeFilter typeFilter = TypeFilter.All;
                public bool dragEnabled = true;

                public State() : this(new TreeViewState())
                {
                }

                public State(TreeViewState baseState)
                {
                    this.baseState = baseState;
                }
            }

            new public State state
            {
                get
                {
                    State result = new State(base.state);

                    if (noSearchExpandState != null)
                    {
                        result.noSearchExpandState = new List<int>(noSearchExpandState);
                    }

                    result.selectedObject = SelectedObject;

                    foreach (var entry in itemIDs)
                    {
                        result.itemPaths.Add(entry.Key);
                        result.itemIDs.Add(entry.Value);
                    }

                    result.typeFilter = TypeFilter;
                    result.dragEnabled = true;

                    return result;
                }
            }
        }

        private void AffirmResources()
        {
            if (borderIcon == null)
            {
                borderIcon = EditorUtils.LoadImage("Border.png");

                borderStyle = new GUIStyle(GUI.skin.box);
                borderStyle.normal.background = borderIcon;
                borderStyle.margin = new RectOffset();
            }
        }

        private bool InChooserMode { get { return outputProperty != null; } }

        private void OnGUI()
        {
            if (!IsOpen)
            {
                return;
            }

            AffirmResources();

            if (InChooserMode)
            {
                GUILayout.BeginVertical(borderStyle, GUILayout.ExpandWidth(true));
            }

            treeView.searchString = searchField.OnGUI(treeView.searchString);

            Rect treeRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            treeRect.y += 2;
            treeRect.height -= 2;

            treeView.OnGUI(treeRect);

            if (InChooserMode)
            {
                GUILayout.EndVertical();
                HandleChooserModeEvents();
            }
            else
            {
                previewArea.treeView = treeView;
                previewArea.OnGUI(position.width, cachedMetering != null ? cachedMetering : EditorUtils.GetMetering());
            }
        }

        private void HandleChooserModeEvents()
        {
            if (Event.current.isKey)
            {
                KeyCode keyCode = Event.current.keyCode;

                if ((keyCode == KeyCode.Return || keyCode == KeyCode.KeypadEnter) && treeView.SelectedObject != null)
                {
                    SetOutputProperty(treeView.SelectedObject);
                    Event.current.Use();
                    Close();
                }
                else if (keyCode == KeyCode.Escape)
                {
                    Event.current.Use();
                    Close();
                }
            }
            else if (treeView.DoubleClickedObject != null)
            {
                SetOutputProperty(treeView.DoubleClickedObject);
                Close();
            }
        }

        private void SetOutputProperty(ScriptableObject data)
        {
            if (data is EditorEventRef)
            {
                EditorEventRef eventRef = data as EditorEventRef;

                outputProperty.SetEventReference(eventRef.Guid, eventRef.Path);

                EditorUtils.UpdateParamsOnEmitter(outputProperty.serializedObject, eventRef.Path);
            }
            else if (data is EditorBankRef)
            {
                outputProperty.stringValue = (data as EditorBankRef).Name;
            }
            else if (data is EditorParamRef)
            {
                outputProperty.stringValue = (data as EditorParamRef).Name;
            }

            outputProperty.serializedObject.ApplyModifiedProperties();
        }

        [Serializable]
        private class PreviewArea
        {
            [NonSerialized]
            public TreeView treeView;

            [NonSerialized]
            private EditorEventRef currentEvent;

            [SerializeField]
            private DetailsView detailsView = new DetailsView();

            [SerializeField]
            private TransportControls transportControls = new TransportControls();

            [SerializeField]
            private Event3DPreview event3DPreview = new Event3DPreview();

            [SerializeField]
            private PreviewMeters meters = new PreviewMeters();

            [SerializeField]
            private EventParameterControls parameterControls = new EventParameterControls();

            private GUIStyle mainStyle;

            private bool isNarrow;

            public bool forceRepaint { get { return transportControls.forceRepaint; } }

            private void SetEvent(EditorEventRef eventRef)
            {
                if (eventRef != currentEvent)
                {
                    currentEvent = eventRef;

                    EditorUtils.PreviewStop(PreviewEventInstance);
                    transportControls.Reset();
                    event3DPreview.Reset();
                    parameterControls.Reset();
                }
            }

            private void AffirmResources()
            {
                if (mainStyle ==  null)
                {
                    mainStyle = new GUIStyle(GUI.skin.box);
                    mainStyle.margin = new RectOffset();
                }
            }

            public void OnGUI(float width, float[] metering)
            {
                isNarrow = width < 600;

                AffirmResources();

                ScriptableObject selectedObject = treeView.SelectedObject;

                if (selectedObject is EditorEventRef)
                {
                    SetEvent(selectedObject as EditorEventRef);
                }
                else
                {
                    SetEvent(null);
                }

                if (selectedObject != null)
                {
                    GUILayout.BeginVertical(mainStyle, GUILayout.ExpandWidth(true));

                    if (selectedObject is EditorEventRef)
                    {
                        EditorEventRef eventRef = selectedObject as EditorEventRef;

                        if (eventRef.Path.StartsWith("event:"))
                        {
                            DrawEventPreview(eventRef, metering);
                        }
                        else if (eventRef.Path.StartsWith("snapshot:"))
                        {
                            detailsView.DrawSnapshot(eventRef);
                        }
                    }
                    else if (selectedObject is EditorBankRef)
                    {
                        detailsView.DrawBank(selectedObject as EditorBankRef);
                    }
                    else if (selectedObject is EditorParamRef)
                    {
                        detailsView.DrawParameter(selectedObject as EditorParamRef);
                    }

                    GUILayout.EndVertical();
                }
            }

            private void DrawSeparatorLine()
            {
                GUILayout.Box(GUIContent.none, GUILayout.Height(1), GUILayout.ExpandWidth(true));
            }

            private void DrawEventPreview(EditorEventRef eventRef, float[] metering)
            {
                detailsView.DrawEvent(eventRef, isNarrow);

                DrawSeparatorLine();

                // Playback controls, 3D Preview and meters
                EditorGUILayout.BeginHorizontal(GUILayout.Height(event3DPreview.Height));
                GUILayout.FlexibleSpace();

                EditorGUILayout.BeginVertical();

                if (!isNarrow)
                {
                    GUILayout.FlexibleSpace();
                }

                transportControls.OnGUI(eventRef, parameterControls.ParameterValues);

                if (isNarrow)
                {
                    EditorGUILayout.Separator();
                    meters.OnGUI(true, metering);
                }
                else
                {
                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.EndVertical();

                event3DPreview.OnGUI(eventRef);

                if (!isNarrow)
                {
                    meters.OnGUI(false, metering);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                DrawSeparatorLine();

                parameterControls.OnGUI(eventRef);
            }
        }

        [Serializable]
        private class DetailsView
        {
            private Texture copyIcon;
            private GUIStyle textFieldNameStyle;

            private void AffirmResources()
            {
                if (copyIcon == null)
                {
                    copyIcon = EditorUtils.LoadImage("CopyIcon.png");

                    textFieldNameStyle = new GUIStyle(EditorStyles.label);
                    textFieldNameStyle.fontStyle = FontStyle.Bold;
                }
            }

            public void DrawEvent(EditorEventRef selectedEvent, bool isNarrow)
            {
                AffirmResources();

                DrawCopyableTextField(L10n.Tr("Full Path"), selectedEvent.Path);

                DrawTextField(L10n.Tr("Banks"), string.Join(", ", selectedEvent.Banks.Select(x => x.Name).ToArray()));

                EditorGUILayout.BeginHorizontal();
                DrawTextField(L10n.Tr("Panning"), selectedEvent.Is3D ? "3D" : "2D");
                DrawTextField(L10n.Tr("Oneshot"), selectedEvent.IsOneShot.ToString());

                TimeSpan t = TimeSpan.FromMilliseconds(selectedEvent.Length);
                DrawTextField(L10n.Tr("Length"), selectedEvent.Length > 0 ? string.Format("{0:D2}:{1:D2}:{2:D3}", t.Minutes, t.Seconds, t.Milliseconds) : "N/A");

                if (!isNarrow) DrawTextField(L10n.Tr("Streaming"), selectedEvent.IsStream.ToString());
                EditorGUILayout.EndHorizontal();
                if (isNarrow) DrawTextField(L10n.Tr("Streaming"), selectedEvent.IsStream.ToString());
            }

            public void DrawSnapshot(EditorEventRef eventRef)
            {
                AffirmResources();

                DrawCopyableTextField(L10n.Tr("Full Path"), eventRef.Path);
            }

            public void DrawBank(EditorBankRef bank)
            {
                AffirmResources();

                DrawCopyableTextField(L10n.Tr("Full Path"), "bank:/" + bank.Name);

                string[] SizeSuffix = { "B", "KB", "MB", "GB" };

                GUILayout.Label(L10n.Tr("Platform Bank Sizes"), textFieldNameStyle);

                EditorGUI.indentLevel++;

                foreach (var sizeInfo in bank.FileSizes)
                {
                    int order = 0;
                    long size = sizeInfo.Value;

                    while (size >= 1024 && order + 1 < SizeSuffix.Length)
                    {
                        order++;
                        size /= 1024;
                    }

                    EditorGUILayout.LabelField(sizeInfo.Name, string.Format("{0} {1}", size, SizeSuffix[order]));
                }

                EditorGUI.indentLevel--;
            }

            public void DrawParameter(EditorParamRef parameter)
            {
                AffirmResources();

                DrawCopyableTextField(L10n.Tr("Name"), parameter.Name);
                DrawCopyableTextField("ID",
                    string.Format("{{ data1 = 0x{0:x8}, data2 = 0x{1:x8} }}", parameter.ID.data1, parameter.ID.data2));
                DrawTextField(L10n.Tr("Minimum"), parameter.Min.ToString());
                DrawTextField(L10n.Tr("Maximum"), parameter.Max.ToString());
            }

            private void DrawCopyableTextField(string name, string value)
            {
                EditorGUILayout.BeginHorizontal();
                DrawTextField(name, value);
                if (GUILayout.Button(copyIcon, GUILayout.ExpandWidth(false)))
                {
                    EditorGUIUtility.systemCopyBuffer = value;
                }
                EditorGUILayout.EndHorizontal();
            }

            private void DrawTextField(string name, string content)
            {
                EditorGUILayout.BeginHorizontal();

                GUILayout.Label(name, textFieldNameStyle, GUILayout.Width(75));
                GUILayout.Label(content);

                EditorGUILayout.EndHorizontal();
            }
        }

        [Serializable]
        private class TransportControls
        {
            private Texture playOff;
            private Texture playOn;
            private Texture stopOff;
            private Texture stopOn;
            private Texture openIcon;
            private GUIStyle buttonStyle;

            public bool forceRepaint { get; private set; }

            public void Reset()
            {
                forceRepaint = false;
            }

            private void AffirmResources()
            {
                if (playOff == null)
                {
                    playOff = EditorUtils.LoadImage("TransportPlayButtonOff.png");
                    playOn = EditorUtils.LoadImage("TransportPlayButtonOn.png");
                    stopOff = EditorUtils.LoadImage("TransportStopButtonOff.png");
                    stopOn = EditorUtils.LoadImage("TransportStopButtonOn.png");
                    openIcon = EditorUtils.LoadImage("transportOpen.png");

                    buttonStyle = new GUIStyle();
                    buttonStyle.padding.left = 4;
                    buttonStyle.padding.top = 10;
                }
            }

            public void OnGUI(EditorEventRef selectedEvent, Dictionary<string, float> parameterValues)
            {
                AffirmResources();

                FMOD.Studio.PLAYBACK_STATE previewState = FMOD.Studio.PLAYBACK_STATE.STOPPED;
                bool paused = false;

                if (PreviewEventInstance.isValid())
                {
                    PreviewEventInstance.getPlaybackState(out previewState);
                    PreviewEventInstance.getPaused(out paused);
                }

                bool playing = previewState == FMOD.Studio.PLAYBACK_STATE.PLAYING;
                bool stopped = previewState == FMOD.Studio.PLAYBACK_STATE.STOPPED;

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(stopped || paused ? stopOn : stopOff, buttonStyle, GUILayout.ExpandWidth(false)))
                {
                    forceRepaint = false;

                    if (paused)
                    {
                        EditorUtils.PreviewStop(PreviewEventInstance);
                        PreviewEventInstance.release();
                        PreviewEventInstance.clearHandle();
                    }
                    if (playing)
                    {
                        EditorUtils.PreviewPause(PreviewEventInstance);
                    }
                }
                if (GUILayout.Button(playing ? playOn : playOff, buttonStyle, GUILayout.ExpandWidth(false)))
                {
                    if (paused)
                    {
                        EditorUtils.PreviewPause(PreviewEventInstance);
                    }
                    else
                    {
                        if (PreviewEventInstance.isValid())
                        {
                            EditorUtils.PreviewStop(PreviewEventInstance);
                        }
                        PreviewEventInstance = EditorUtils.PreviewEvent(selectedEvent, parameterValues);
                    }

                    forceRepaint = true;
                }
                if (GUILayout.Button(new GUIContent(openIcon, L10n.Tr("Show Event in FMOD Studio")), buttonStyle, GUILayout.ExpandWidth(false)))
                {
                    string cmd = string.Format("studio.window.navigateTo(studio.project.lookup(\"{0}\"))", selectedEvent.Guid);
                    EditorUtils.SendScriptCommand(cmd);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        [Serializable]
        private class Event3DPreview
        {
            private bool isDragging;
            private Rect arenaRect;

            private Vector2 eventPosition;
            private float eventDistance = 0;
            private float eventOrientation = 0;

            private Texture arena;
            private Texture emitter;

            public void Reset()
            {
                eventPosition = new Vector2(0, 0);
                eventDistance = 0;
                eventOrientation = 0;
            }

            private void AffirmResources()
            {
                if (arena == null)
                {
                    arena = EditorUtils.LoadImage("Preview.png");
                    emitter = EditorUtils.LoadImage("PreviewEmitter.png");
                }
            }

            public float Height
            {
                get
                {
                    AffirmResources();
                    return GUI.skin.label.CalcSize(new GUIContent(arena)).y;
                }
            }

            public void OnGUI(EditorEventRef selectedEvent)
            {
                AffirmResources();

                var originalColour = GUI.color;
                if (!selectedEvent.Is3D)
                {
                    GUI.color = new Color(1.0f, 1.0f, 1.0f, 0.1f);
                }

                GUILayout.Label(arena, GUILayout.ExpandWidth(false));

                if (Event.current.type == EventType.Repaint)
                {
                    arenaRect = GUILayoutUtility.GetLastRect();
                }

                Vector2 center = arenaRect.center;
                Rect rect2 = new Rect(center.x + eventPosition.x - 6, center.y + eventPosition.y - 6, 12, 12);
                GUI.DrawTexture(rect2, emitter);

                GUI.color = originalColour;

                if (selectedEvent.Is3D)
                {
                    bool useGUIEvent = false;

                    switch (Event.current.type)
                    {
                        case EventType.MouseDown:
                            if (arenaRect.Contains(Event.current.mousePosition))
                            {
                                isDragging = true;
                                useGUIEvent = true;
                            }
                            break;
                        case EventType.MouseUp:
                            if (isDragging)
                            {
                                isDragging = false;
                                useGUIEvent = true;
                            }
                            break;
                        case EventType.MouseDrag:
                            if (isDragging)
                            {
                                useGUIEvent = true;
                            }
                            break;
                    }

                    if (useGUIEvent)
                    {
                        Vector2 newPosition = Event.current.mousePosition;
                        Vector2 delta = newPosition - center;

                        float maximumDistance = (arena.width - emitter.width) / 2;
                        float distance = Math.Min(delta.magnitude, maximumDistance);

                        delta.Normalize();
                        eventPosition = delta * distance;
                        eventDistance = distance / maximumDistance * selectedEvent.MaxDistance;

                        float angle = Mathf.Atan2(delta.y, delta.x);
                        eventOrientation = angle + Mathf.PI * 0.5f;

                        Event.current.Use();
                    }
                }

                if (PreviewEventInstance.isValid())
                {
                    // Listener at origin
                    FMOD.ATTRIBUTES_3D pos = new FMOD.ATTRIBUTES_3D();
                    pos.position.x = (float)Math.Sin(eventOrientation) * eventDistance;
                    pos.position.y = (float)Math.Cos(eventOrientation) * eventDistance;
                    pos.forward.x = 1.0f;
                    pos.up.z = 1.0f;
                    PreviewEventInstance.set3DAttributes(pos);
                }
            }
        }

        [Serializable]
        private class EventParameterControls
        {
            [NonSerialized]
            private Dictionary<string, float> parameterValues = new Dictionary<string, float>();

            [NonSerialized]
            private Vector2 scrollPosition;

            [NonSerialized]
            private bool showGlobalParameters;

            public Dictionary<string, float> ParameterValues { get { return parameterValues; } }

            public void Reset()
            {
                parameterValues.Clear();
            }

            public void OnGUI(EditorEventRef selectedEvent)
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight * 7f));

                foreach (EditorParamRef paramRef in selectedEvent.LocalParameters)
                {
                    if (!parameterValues.ContainsKey(paramRef.Name))
                    {
                        parameterValues[paramRef.Name] = paramRef.Default;
                    }

                    CreateParamRefSlider(paramRef);
                }

                showGlobalParameters = selectedEvent.GlobalParameters.Count > 0 &&
                    EditorGUI.Foldout(EditorGUILayout.GetControlRect(), showGlobalParameters, L10n.Tr("Global Parameters"));

                foreach (EditorParamRef paramRef in selectedEvent.GlobalParameters)
                {
                    if (!parameterValues.ContainsKey(paramRef.Name))
                    {
                        parameterValues[paramRef.Name] = paramRef.Default;
                    }

                    if (showGlobalParameters)
                    {
                        CreateParamRefSlider(paramRef, true);
                    }
                }

                GUILayout.EndScrollView();
            }

            public void CreateParamRefSlider(EditorParamRef paramRef, bool isGlobal = false)
            {
                if (paramRef.Type == ParameterType.Labeled)
                {
                    parameterValues[paramRef.Name] = EditorGUILayout.IntPopup(
                        paramRef.Name, (int)parameterValues[paramRef.Name], paramRef.Labels, null);
                }
                else if (paramRef.Type == ParameterType.Discrete)
                {
                    parameterValues[paramRef.Name] = EditorGUILayout.IntSlider(
                        paramRef.Name, (int)parameterValues[paramRef.Name], (int)paramRef.Min, (int)paramRef.Max);
                }
                else
                {
                    parameterValues[paramRef.Name] = EditorGUILayout.Slider(
                        paramRef.Name, parameterValues[paramRef.Name], paramRef.Min, paramRef.Max);
                }

                if (isGlobal)
                {
                    EditorUtils.System.setParameterByID(paramRef.ID, parameterValues[paramRef.Name]);
                }
                else
                {
                    if (PreviewEventInstance.isValid())
                    {
                        PreviewEventInstance.setParameterByID(paramRef.ID, parameterValues[paramRef.Name]);
                    }
                }
            }
        }

        [Serializable]
        private class PreviewMeters
        {
            private Texture meterOn;
            private Texture meterOff;

            private void AffirmResources()
            {
                if (meterOn == null)
                {
                    meterOn = EditorUtils.LoadImage("LevelMeter.png");
                    meterOff = EditorUtils.LoadImage("LevelMeterOff.png");
                }
            }

            public void OnGUI(bool minimized, float[] metering)
            {
                AffirmResources();

                int meterHeight = minimized ? 86 : 128;
                int meterWidth = (int)((128 / (float)meterOff.height) * meterOff.width);

                List<float> meterPositions = meterPositionsForSpeakerMode(speakerModeForChannelCount(metering.Length), meterWidth, 2, 6);

                const int MeterCountMaximum = 16;

                int minimumWidth = meterWidth * MeterCountMaximum;

                Rect fullRect = GUILayoutUtility.GetRect(minimumWidth, meterHeight,
                    GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                float baseX = fullRect.x + (fullRect.width - (meterWidth * metering.Length)) / 2;

                for(int i = 0; i < metering.Length; i++)
                {
                    Rect meterRect = new Rect(baseX + meterPositions[i], fullRect.y, meterWidth, fullRect.height);

                    GUI.DrawTexture(meterRect, meterOff);

                    float db = 20.0f * Mathf.Log10(metering[i] * Mathf.Sqrt(2.0f));
                    db = Mathf.Clamp(db, -80.0f, 10.0f);
                    float visible = 0;
                    int[] segmentPixels = new int[] { 0, 18, 38, 60, 89, 130, 187, 244, 300 };
                    float[] segmentDB = new float[] { -80.0f, -60.0f, -50.0f, -40.0f, -30.0f, -20.0f, -10.0f, 0, 10.0f };
                    int segment = 1;
                    while (segmentDB[segment] < db)
                    {
                        segment++;
                    }
                    visible = segmentPixels[segment - 1] + ((db - segmentDB[segment - 1]) / (segmentDB[segment] - segmentDB[segment - 1])) * (segmentPixels[segment] - segmentPixels[segment - 1]);

                    visible *= fullRect.height / (float)meterOff.height;

                    Rect levelPosRect = new Rect(meterRect.x, fullRect.height - visible + meterRect.y, meterWidth, visible);
                    Rect levelUVRect = new Rect(0, 0, 1.0f, visible / fullRect.height);
                    GUI.DrawTextureWithTexCoords(levelPosRect, meterOn, levelUVRect);
                }
            }

            private FMOD.SPEAKERMODE speakerModeForChannelCount(int channelCount)
            {
                switch(channelCount)
                {
                case 1:
                    return FMOD.SPEAKERMODE.MONO;
                case 4:
                    return FMOD.SPEAKERMODE.QUAD;
                case 5:
                    return FMOD.SPEAKERMODE.SURROUND;
                case 6:
                    return FMOD.SPEAKERMODE._5POINT1;
                case 8:
                    return FMOD.SPEAKERMODE._7POINT1;
                case 12:
                    return FMOD.SPEAKERMODE._7POINT1POINT4;
                default:
                    return FMOD.SPEAKERMODE.STEREO;
                }
            }

            private List<float> meterPositionsForSpeakerMode(FMOD.SPEAKERMODE mode, float meterWidth, float groupGap, float lfeGap)
            {
                List<float> offsets = new List<float>();

                switch(mode)
                {
                case FMOD.SPEAKERMODE.MONO: // M
                    offsets.Add(0);
                    break;

                case FMOD.SPEAKERMODE.STEREO: // L R
                    offsets.Add(0);
                    offsets.Add(meterWidth);
                    break;

                case FMOD.SPEAKERMODE.QUAD:
                    switch(Settings.Instance.MeterChannelOrdering)
                    {
                    case MeterChannelOrderingType.Standard:
                    case MeterChannelOrderingType.SeparateLFE: // L R | LS RS
                        offsets.Add(0); // L
                        offsets.Add(meterWidth*1); // R
                        offsets.Add(meterWidth*2 + groupGap); // LS
                        offsets.Add(meterWidth*3 + groupGap); // RS
                        break;
                    case MeterChannelOrderingType.Positional: // LS | L R | RS
                        offsets.Add(meterWidth*1 + groupGap); // L
                        offsets.Add(meterWidth*2 + groupGap); // R
                        offsets.Add(0); // LS
                        offsets.Add(meterWidth*3 + groupGap*2); // RS
                        break;
                    }
                    break;

                case FMOD.SPEAKERMODE.SURROUND:
                    switch(Settings.Instance.MeterChannelOrdering)
                    {
                    case MeterChannelOrderingType.Standard:
                    case MeterChannelOrderingType.SeparateLFE: // L R | C | LS RS
                        offsets.Add(0); // L
                        offsets.Add(meterWidth*1); // R
                        offsets.Add(meterWidth*2 + groupGap); // C
                        offsets.Add(meterWidth*3 + groupGap*2); // LS
                        offsets.Add(meterWidth*4 + groupGap*2); // RS
                        break;
                    case MeterChannelOrderingType.Positional: // LS | L C R | RS
                        offsets.Add(meterWidth*1 + groupGap); // L
                        offsets.Add(meterWidth*3 + groupGap); // R
                        offsets.Add(meterWidth*2 + groupGap); // C
                        offsets.Add(0); // LS
                        offsets.Add(meterWidth*4 + groupGap*2); // RS
                        break;
                    }
                    break;

                case FMOD.SPEAKERMODE._5POINT1:
                    switch(Settings.Instance.MeterChannelOrdering)
                    {
                    case MeterChannelOrderingType.Standard: // L R | C | LFE | LS RS
                        offsets.Add(0); // L
                        offsets.Add(meterWidth*1); // R
                        offsets.Add(meterWidth*2 + groupGap); // C
                        offsets.Add(meterWidth*3 + groupGap*2); // LFE
                        offsets.Add(meterWidth*4 + groupGap*3); // LS
                        offsets.Add(meterWidth*5 + groupGap*3); // RS
                        break;
                    case MeterChannelOrderingType.SeparateLFE: // L R | C | LS RS || LFE
                        offsets.Add(0); // L
                        offsets.Add(meterWidth*1); // R
                        offsets.Add(meterWidth*2 + groupGap); // C
                        offsets.Add(meterWidth*5 + groupGap*2 + lfeGap); // LFE
                        offsets.Add(meterWidth*3 + groupGap*2); // LS
                        offsets.Add(meterWidth*4 + groupGap*2); // RS
                        break;
                    case MeterChannelOrderingType.Positional: // LS | L C R | RS || LFE
                        offsets.Add(meterWidth*1 + groupGap); // L
                        offsets.Add(meterWidth*3 + groupGap); // R
                        offsets.Add(meterWidth*2 + groupGap); // C
                        offsets.Add(meterWidth*5 + groupGap*2 + lfeGap); // LFE
                        offsets.Add(0); // LS
                        offsets.Add(meterWidth*4 + groupGap*2); // RS
                        break;
                    }
                    break;

                case FMOD.SPEAKERMODE._7POINT1:
                    switch(Settings.Instance.MeterChannelOrdering)
                    {
                    case MeterChannelOrderingType.Standard: // L R | C | LFE | LS RS | LSR RSR
                        offsets.Add(0); // L
                        offsets.Add(meterWidth*1); // R
                        offsets.Add(meterWidth*2 + groupGap); // C
                        offsets.Add(meterWidth*3 + groupGap*2); // LFE
                        offsets.Add(meterWidth*4 + groupGap*3); // LS
                        offsets.Add(meterWidth*5 + groupGap*3); // RS
                        offsets.Add(meterWidth*6 + groupGap*4); // LSR
                        offsets.Add(meterWidth*7 + groupGap*4); // RSR
                        break;
                    case MeterChannelOrderingType.SeparateLFE: // L R | C | LS RS | LSR RSR || LFE
                        offsets.Add(0); // L
                        offsets.Add(meterWidth*1); // R
                        offsets.Add(meterWidth*2 + groupGap); // C
                        offsets.Add(meterWidth*7 + groupGap*3 + lfeGap); // LFE
                        offsets.Add(meterWidth*3 + groupGap*2); // LS
                        offsets.Add(meterWidth*4 + groupGap*2); // RS
                        offsets.Add(meterWidth*5 + groupGap*3); // LSR
                        offsets.Add(meterWidth*6 + groupGap*3); // RSR
                        break;
                    case MeterChannelOrderingType.Positional: // LSR LS | L C R | RS RSR || LFE
                        offsets.Add(meterWidth*2 + groupGap); // L
                        offsets.Add(meterWidth*4 + groupGap); // R
                        offsets.Add(meterWidth*3 + groupGap); // C
                        offsets.Add(meterWidth*7 + groupGap*2 + lfeGap); // LFE
                        offsets.Add(meterWidth*1); // LS
                        offsets.Add(meterWidth*5 + groupGap*2); // RS
                        offsets.Add(0); // LSR
                        offsets.Add(meterWidth*6 + groupGap*2); // RSR
                        break;
                    }
                    break;

                case FMOD.SPEAKERMODE._7POINT1POINT4:
                    switch(Settings.Instance.MeterChannelOrdering)
                    {
                    case MeterChannelOrderingType.Standard: // L R | C | LFE | LS RS | LSR RSR | TFL TFR TBL TBR
                        offsets.Add(0); // L
                        offsets.Add(meterWidth*1); // R
                        offsets.Add(meterWidth*2 + groupGap); // C
                        offsets.Add(meterWidth*3 + groupGap*2); // LFE
                        offsets.Add(meterWidth*4 + groupGap*3); // LS
                        offsets.Add(meterWidth*5 + groupGap*3); // RS
                        offsets.Add(meterWidth*6 + groupGap*4); // LSR
                        offsets.Add(meterWidth*7 + groupGap*4); // RSR
                        offsets.Add(meterWidth*8 + groupGap*5); // TFL
                        offsets.Add(meterWidth*9 + groupGap*5); // TFR
                        offsets.Add(meterWidth*10 + groupGap*5); // TBL
                        offsets.Add(meterWidth*11 + groupGap*5); // TBR
                        break;
                    case MeterChannelOrderingType.SeparateLFE: // L R | C | LS RS | LSR RSR | TFL TFR TBL TBR || LFE
                        offsets.Add(0); // L
                        offsets.Add(meterWidth*1); // R
                        offsets.Add(meterWidth*2 + groupGap); // C
                        offsets.Add(meterWidth*11 + groupGap*4 + lfeGap); // LFE
                        offsets.Add(meterWidth*3 + groupGap*2); // LS
                        offsets.Add(meterWidth*4 + groupGap*2); // RS
                        offsets.Add(meterWidth*5 + groupGap*3); // LSR
                        offsets.Add(meterWidth*6 + groupGap*3); // RSR
                        offsets.Add(meterWidth*7 + groupGap*4); // TFL
                        offsets.Add(meterWidth*8 + groupGap*4); // TFR
                        offsets.Add(meterWidth*9 + groupGap*4); // TBL
                        offsets.Add(meterWidth*10 + groupGap*4); // TBR
                        break;
                    case MeterChannelOrderingType.Positional: // LSR LS | L C R | RS RSR | TBL TFL TFR TBR || LFE
                        offsets.Add(meterWidth*2 + groupGap); // L
                        offsets.Add(meterWidth*4 + groupGap); // R
                        offsets.Add(meterWidth*3 + groupGap); // C
                        offsets.Add(meterWidth*11 + groupGap*3 + lfeGap); // LFE
                        offsets.Add(meterWidth*1); // LS
                        offsets.Add(meterWidth*5 + groupGap*2); // RS
                        offsets.Add(0); // LSR
                        offsets.Add(meterWidth*6 + groupGap*2); // RSR
                        offsets.Add(meterWidth*8 + groupGap*3); // TFL
                        offsets.Add(meterWidth*9 + groupGap*3); // TFR
                        offsets.Add(meterWidth*7 + groupGap*3); // TBL
                        offsets.Add(meterWidth*10 + groupGap*3); // TBR
                        break;
                    }
                    break;
                }

                return offsets;
            }
        }

        [Flags]
        private enum TypeFilter
        {
            Event = 1,
            Bank = 2,
            Parameter = 4,
            All = Event | Bank | Parameter,
        }

        public void ChooseEvent(SerializedProperty property)
        {
            BeginInspectorPopup(property, TypeFilter.Event);
            string path = property.GetEventReferencePath();
            if (!string.IsNullOrEmpty(path))
            {
                treeView.JumpToEvent(path);
            }
        }

        public void ChooseBank(SerializedProperty property)
        {
            BeginInspectorPopup(property, TypeFilter.Bank);

            if (!string.IsNullOrEmpty(property.stringValue))
            {
                treeView.JumpToBank(property.stringValue);
            }
        }

        public void ChooseParameter(SerializedProperty property)
        {
            BeginInspectorPopup(property, TypeFilter.Parameter);
        }

        public void FrameEvent(string path)
        {
            treeView.JumpToEvent(path);
        }

        private void BeginInspectorPopup(SerializedProperty property, TypeFilter typeFilter)
        {
            treeView.TypeFilter = typeFilter;
            outputProperty = property;
            searchField.SetFocus();
            treeView.DragEnabled = false;
            ReadEventCache();
        }

        private void BeginStandaloneWindow()
        {
            treeView.TypeFilter = TypeFilter.All;
            outputProperty = null;
            searchField.SetFocus();
            treeView.DragEnabled = true;
            isStandaloneWindow = true;
        }

        public void OnEnable()
        {
            if (treeViewState == null)
            {
                treeViewState = new TreeView.State();
            }

            searchField = new SearchField();
            treeView = new TreeView(treeViewState);

			// Delay accessing the event cache as this will cause an error if window is opened on Unity start up.
            EditorApplication.delayCall += () =>
            {
                ReadEventCache();

                searchField.downOrUpArrowKeyPressed += treeView.SetFocus;

                SceneView.duringSceneGui += SceneUpdate;

                EditorApplication.hierarchyWindowItemOnGUI += HierarchyUpdate;

                if (isStandaloneWindow)
                {
                    EditorUtils.LoadPreviewBanks();
                }

                IsOpen = true;
                Repaint();
            };
        }

        public void OnDestroy()
        {
            if (PreviewEventInstance.isValid())
            {
                EditorUtils.PreviewStop(PreviewEventInstance);
                PreviewEventInstance.clearHandle();
            }

            if (isStandaloneWindow)
            {
                EditorUtils.UnloadPreviewBanks();
            }

            IsOpen = false;
        }

        private static bool IsDraggable(UnityEngine.Object data)
        {
            return data is EditorEventRef || data is EditorBankRef || data is EditorParamRef;
        }

        public static bool IsDroppable(UnityEngine.Object[] data)
        {
            return data.Length > 0 && IsDraggable(data[0]);
        }

        // This is an event handler on the hierachy view to handle dragging our objects from the browser
        private void HierarchyUpdate(int instance, Rect rect)
        {
            if (Event.current.type == EventType.DragPerform && rect.Contains(Event.current.mousePosition))
            {
                if (IsDroppable(DragAndDrop.objectReferences))
                {
                    UnityEngine.Object data = DragAndDrop.objectReferences[0];

                    GameObject target = EditorUtility.InstanceIDToObject(instance) as GameObject;

                    if (data is EditorEventRef)
                    {
                        Undo.SetCurrentGroupName(L10n.Tr("Add Studio Event Emitter"));

                        StudioEventEmitter emitter = Undo.AddComponent<StudioEventEmitter>(target);

                        EditorEventRef eventRef = data as EditorEventRef;
                        emitter.EventReference.Path = eventRef.Path;
                        emitter.EventReference.Guid = eventRef.Guid;
                    }
                    else if (data is EditorBankRef)
                    {
                        Undo.SetCurrentGroupName(L10n.Tr("Add Studio Bank Loader"));

                        StudioBankLoader loader = Undo.AddComponent<StudioBankLoader>(target);
                        loader.Banks = new List<string>();
                        loader.Banks.Add((data as EditorBankRef).Name);
                    }
                    else // data is EditorParamRef
                    {
                        Undo.SetCurrentGroupName(L10n.Tr("Add Studio Global Parameter Trigger"));

                        StudioGlobalParameterTrigger trigger = Undo.AddComponent<StudioGlobalParameterTrigger>(target);
                        trigger.Parameter = (data as EditorParamRef).Name;
                    }

                    Selection.activeObject = target;

                    Event.current.Use();
                }
            }
        }

        // This is an event handler on the scene view to handle dragging our objects from the browser
        // and creating new gameobjects
        private void SceneUpdate(SceneView sceneView)
        {
            if (Event.current.type == EventType.DragPerform && IsDroppable(DragAndDrop.objectReferences))
            {
                UnityEngine.Object data = DragAndDrop.objectReferences[0];
                GameObject newObject;

                if (data is EditorEventRef)
                {
                    EditorEventRef eventRef = data as EditorEventRef;

                    string path = eventRef.Path;

                    string name = path.Substring(path.LastIndexOf("/") + 1);
                    newObject = new GameObject(name + " Emitter");

                    StudioEventEmitter emitter = newObject.AddComponent<StudioEventEmitter>();
                    emitter.EventReference.Path = path;
                    emitter.EventReference.Guid = eventRef.Guid;

                    Undo.RegisterCreatedObjectUndo(newObject, L10n.Tr("Create Studio Event Emitter"));
                }
                else if (data is EditorBankRef)
                {
                    newObject = new GameObject("Studio Bank Loader");

                    StudioBankLoader loader = newObject.AddComponent<StudioBankLoader>();
                    loader.Banks = new List<string>();
                    loader.Banks.Add((data as EditorBankRef).Name);

                    Undo.RegisterCreatedObjectUndo(newObject, L10n.Tr("Create Studio Bank Loader"));
                }
                else // data is EditorParamRef
                {
                    string name = (data as EditorParamRef).Name;

                    newObject = new GameObject(name + " Trigger");

                    StudioGlobalParameterTrigger trigger = newObject.AddComponent<StudioGlobalParameterTrigger>();
                    trigger.Parameter = name;

                    Undo.RegisterCreatedObjectUndo(newObject, L10n.Tr("Create Studio Global Parameter Trigger"));
                }

                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                object hit = HandleUtility.RaySnap(ray);

                if (hit != null)
                {
                    newObject.transform.position = ((RaycastHit)hit).point;
                }
                else
                {
                    newObject.transform.position = ray.origin + ray.direction * 10.0f;
                }

                Selection.activeObject = newObject;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragUpdated && IsDroppable(DragAndDrop.objectReferences))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                DragAndDrop.AcceptDrag();
                Event.current.Use();
            }
        }
    }
}
