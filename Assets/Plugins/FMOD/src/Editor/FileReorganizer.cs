using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

#if UNITY_6000_2_OR_NEWER
using TreeView = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#endif

namespace FMODUnity
{
    public class FileReorganizer : EditorWindow, ISerializationCallbackReceiver
    {
        public const string ReorganizerMenuItemPath = "FMOD/Reorganize Plugin Files";

        [SerializeField]
        private List<Task> tasks = new List<Task>();

        [SerializeField]
        private int taskCount;

        [SerializeField]
        private int currentTask;

        private TaskView taskView;

        [SerializeField]
        private TreeViewState taskViewState = new TreeViewState();

        [SerializeField]
        private MultiColumnHeaderState taskHeaderState;

        [SerializeField]
        private bool reloadingFromSerializedState = false;

        [NonSerialized]
        private GUIContent statusContent = GUIContent.none;

        private IEnumerator<string> processingState;

        [MenuItem(ReorganizerMenuItemPath)]
        public static void ShowWindow()
        {
            FileReorganizer reorganizer = GetWindow<FileReorganizer>(L10n.Tr("FMOD File Reorganizer"));
            reorganizer.minSize = new Vector2(850, 600);

            reorganizer.PopulateTasks();

            reorganizer.Show();
        }

        [Serializable]
        private class Task
        {
            public int step = int.MaxValue;

            private Task()
            {
            }

            public static Task Move(string source, string destination, Platform platform)
            {
                return new Task() {
                    type = Type.Move,
                    status = Status.Pending,
                    platform = platform,
                    source = source,
                    destination = destination,
                    statusText = string.Format(L10n.Tr("{0} will be moved to\n{1}"), source, destination),
                };
            }

            public static Task RemoveFolder(string path)
            {
                return new Task() {
                    type = Type.RemoveFolder,
                    status = Status.Pending,
                    source = path,
                    statusText = string.Format(L10n.Tr("{0} will be removed if it is empty"), path),
                };
            }

            public static Task Missing(string path, Platform platform)
            {
                return new Task() {
                    type = Type.Missing,
                    status = Status.Missing,
                    platform = platform,
                    source = path,
                    statusText = string.Format(
                        L10n.Tr("{0} is missing.\nYou may need to reinstall the {1} support package from {2}."),
                        path, platform.DisplayName, EditorSettings.DownloadURL),
                };
            }

            public static Task RemoveAsset(string path, Platform platform)
            {
                return new Task() {
                    type = Type.RemoveAsset,
                    status = Status.Pending,
                    platform = platform,
                    source = path,
                    statusText = string.Format(L10n.Tr("{0} will be removed"), path),
                };
            }

            public Platform platform { get; private set; }
            public string source { get; private set; }
            public string destination { get; private set; }

            public enum Status
            {
                Pending,
                Succeeded,
                Failed,
                Missing,
            }

            public Status status { get; private set; }
            public string statusText { get; private set; }

            public void SetSucceeded(string message)
            {
                status = Status.Succeeded;
                statusText = message;
            }

            public void SetFailed(string message)
            {
                status = Status.Failed;
                statusText = message;
            }

            public enum Type
            {
                Move,
                RemoveFolder,
                RemoveAsset,
                Missing,
            }

            public Type type { get; private set; }

            public string platformName { get { return (platform != null) ? platform.DisplayName : string.Empty; } }
        }

        public void OnBeforeSerialize()
        {
            taskViewState = taskView.state;
            taskHeaderState = taskView.multiColumnHeader.state;
        }

        public void OnAfterDeserialize()
        {
        }

        private void OnEnable()
        {
            {
                MultiColumnHeaderState newHeaderState = TaskView.CreateHeaderState();

                if (MultiColumnHeaderState.CanOverwriteSerializedFields(taskHeaderState, newHeaderState))
                {
                    MultiColumnHeaderState.OverwriteSerializedFields(taskHeaderState, newHeaderState);
                }

                taskHeaderState = newHeaderState;
            }

            MultiColumnHeader taskHeader = new MultiColumnHeader(taskHeaderState);

            taskView = new TaskView(taskViewState, taskHeader, tasks);
            taskView.taskSelected += OnTaskSelected;

            taskView.Reload();

            if (reloadingFromSerializedState)
            {
                taskView.SortRows();
            }
            else
            {
                taskHeader.ResizeToFit();
                taskHeader.SetSorting((int)TaskView.Column.Step, true);
            }

            reloadingFromSerializedState = true;

            EditorApplication.update += ProcessNextTask;
        }

        private void OnDestroy()
        {
            EditorApplication.update -= ProcessNextTask;

            StopProcessing();
        }

        private void PopulateTasks()
        {
            tasks.Clear();

            TaskGenerator.Generate(tasks);

            SetTaskSequence();
            UpdateTaskCount();
            SetDefaultStatus();

            taskView.Reload();
            taskView.SortRows();
        }

        public static bool IsUpToDate()
        {
            List<Task> tasks = new List<Task>();

            TaskGenerator.Generate(tasks);

            return !tasks.Any(t => t.type != Task.Type.Missing);
        }

        private void SetDefaultStatus()
        {
            int missingCount = tasks.Count(t => t.type == Task.Type.Missing);

            if (missingCount > 0)
            {
                string message;

                if (missingCount == 1)
                {
                    message = L10n.Tr("There is a file missing. Select it above for more information.");
                }
                else
                {
                    message = string.Format(
                        L10n.Tr("There are {0} files missing. Select them above for more information."), missingCount);
                }

                statusContent = new GUIContent(message, Resources.StatusIcon[Task.Status.Missing]);
            }
            else
            {
                statusContent = GUIContent.none;
            }
        }

        private void SetTaskSequence()
        {
            int step = 1;

            foreach (Task task in tasks.Where(t => t.type == Task.Type.Move))
            {
                task.step = step;
                ++step;
            }

            foreach (Task task in tasks.Where(t => t.type == Task.Type.RemoveAsset))
            {
                task.step = step;
                ++step;
            }

            // Sort folder tasks in reverse path order, so subfolders are processed before their parents
            foreach (Task task in tasks.Where(t => t.type == Task.Type.RemoveFolder).OrderByDescending(t => t.source))
            {
                task.step = step;
                ++step;
            }

            tasks.Sort((a, b) => a.step.CompareTo(b.step));
        }

        private void UpdateTaskCount()
        {
            taskCount = tasks.Count(t => t.status == Task.Status.Pending);
        }

        private class TaskView : TreeView
        {
            private List<Task> tasks;

            public delegate void TaskSelectedHandler(Task task);

            public event TaskSelectedHandler taskSelected;

            public TaskView(TreeViewState state, MultiColumnHeader header, List<Task> tasks)
                : base(state, header)
            {
                this.tasks = tasks;

                showAlternatingRowBackgrounds = true;

                header.sortingChanged += SortRows;
            }

            public static MultiColumnHeaderState CreateHeaderState()
            {
                MultiColumnHeaderState.Column[] columns = new MultiColumnHeaderState.Column[] {
                    new MultiColumnHeaderState.Column()
                    {
                        headerContent = new GUIContent(L10n.Tr("Task #")),
                        width = 50,
                        autoResize = false,
                        allowToggleVisibility = false,
                    },
                    new MultiColumnHeaderState.Column()
                    {
                        headerContent = new GUIContent(L10n.Tr("Status")),
                        width = 100,
                        autoResize = false,
                        allowToggleVisibility = false,
                    },
                    new MultiColumnHeaderState.Column() {
                        headerContent = new GUIContent(L10n.Tr("Platform")),
                        width = 150,
                        autoResize = false,
                        allowToggleVisibility = false,
                    },
                    new MultiColumnHeaderState.Column()
                    {
                        headerContent = new GUIContent(L10n.Tr("Description")),
                        minWidth = 500,
                        allowToggleVisibility = false,
                    },
                };

                return new MultiColumnHeaderState(columns);
            }

            public enum Column
            {
                Step,
                Status,
                Platform,
                Description,
            }

            private class TaskItem : TreeViewItem
            {
                public Task task;
            }

            protected override TreeViewItem BuildRoot()
            {
                TreeViewItem root = new TreeViewItem(-1, -1);

                if (tasks.Count > 0)
                {
                    int index = 0;

                    foreach (Task task in tasks)
                    {
                        TreeViewItem taskItem = new TaskItem() {
                            id = index++,
                            task = task,
                        };

                        root.AddChild(taskItem);
                    }
                }
                else
                {
                    TreeViewItem item = new TreeViewItem(0);
                    item.displayName = L10n.Tr("Nothing to do here.");

                    root.AddChild(item);
                }

                SetupDepthsFromParentsAndChildren(root);

                return root;
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                base.SelectionChanged(selectedIds);

                if (taskSelected != null)
                {
                    if (selectedIds.Count > 0)
                    {
                        TaskItem item = FindItem(selectedIds[0], rootItem) as TaskItem;

                        if (item != null)
                        {
                            taskSelected(item.task);
                            return;
                        }
                    }

                    taskSelected(null);
                }
            }

            public void SortRows()
            {
                SortRows(multiColumnHeader);
            }

            private void SortRows(MultiColumnHeader header)
            {
                IList<TreeViewItem> rows = GetRows();
                int[] sortedColumns = header.state.sortedColumns;

                if (sortedColumns.Length > 0 && rows.Count > 1)
                {
                    int firstColumn = sortedColumns[0];

                    IOrderedEnumerable<TreeViewItem> query =
                        InitialQuery(rows, (Column)firstColumn, header.IsSortedAscending(firstColumn));

                    for (int i = 1; i < sortedColumns.Length; ++i)
                    {
                        query = SubQuery(query, sortedColumns[i], header.IsSortedAscending(sortedColumns[i]));
                    }

                    // We need to execute the query before clearing rows, otherwise it returns nothing
                    List<TreeViewItem> newRows = query.ToList();

                    rows.Clear();

                    foreach (TreeViewItem item in newRows)
                    {
                        rows.Add(item);
                    }
                }

                RefreshCustomRowHeights();
            }

            private IOrderedEnumerable<TreeViewItem> InitialQuery(IList<TreeViewItem> rows, Column column, bool ascending)
            {
                switch (column)
                {
                    case Column.Step:
                        return Sort(rows, r => (r as TaskItem).task.step, ascending);
                    case Column.Status:
                        return Sort(rows, r => (r as TaskItem).task.status, ascending);
                    case Column.Platform:
                        return Sort(rows, r => (r as TaskItem).task.platformName, ascending);
                    case Column.Description:
                        return Sort(rows, r => (r as TaskItem).task.source, ascending);
                    default:
                        throw new ArgumentException("Unrecognised column: " + column);
                }
            }

            private static IOrderedEnumerable<TreeViewItem> SubQuery(
                IOrderedEnumerable<TreeViewItem> query, int column, bool ascending)
            {
                switch ((Column)column)
                {
                    case Column.Step:
                        return SubSort(query, r => (r as TaskItem).task.step, ascending);
                    case Column.Status:
                        return SubSort(query, r => (r as TaskItem).task.status, ascending);
                    case Column.Platform:
                        return SubSort(query, r => (r as TaskItem).task.platformName, ascending);
                    case Column.Description:
                        return SubSort(query, r => (r as TaskItem).task.source, ascending);
                    default:
                        throw new ArgumentException("Unrecognised column: " + column);
                }
            }

            protected override float GetCustomRowHeight(int row, TreeViewItem item)
            {
                if (item is TaskItem)
                {
                    Task task = (item as TaskItem).task;

                    if (task.type == Task.Type.Move)
                    {
                        return EditorGUIUtility.singleLineHeight * 2;
                    }
                    else
                    {
                        return Resources.StatusHeight();
                    }
                }
                else
                {
                    return base.GetCustomRowHeight(row, item);
                }
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (args.item is TaskItem)
                {
                    TaskItem taskItem = args.item as TaskItem;

                    for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                    {
                        CellGUI(args.GetCellRect(i), taskItem.task, args.GetColumn(i));
                    }
                }
                else
                {
                    base.RowGUI(args);
                }
            }

            private void CellGUI(Rect rect, Task task, int columnIndex)
            {
                switch ((Column)columnIndex)
                {
                    case Column.Step:
                        if (task.step != int.MaxValue)
                        {
                            GUI.Label(rect, task.step.ToString(), Resources.StepStyle());
                        }
                        break;
                    case Column.Status:
                        GUI.Label(rect, Resources.StatusContent[task.status], Resources.StatusColumnStyle());
                        break;
                    case Column.Platform:
                        GUI.Label(rect, task.platformName, Resources.PlatformStyle());
                        break;
                    case Column.Description:
                        DrawDescription(rect, task);
                        break;
                }
            }

            private void DrawDescription(Rect rect, Task task)
            {
                switch (task.type)
                {
                    case Task.Type.Move:
                        DrawMoveDescription(rect, task);
                        break;
                    case Task.Type.RemoveFolder:
                        DrawRemoveFolderDescription(rect, task);
                        break;
                    case Task.Type.RemoveAsset:
                        DrawRemoveAssetDescription(rect, task);
                        break;
                    case Task.Type.Missing:
                        DrawMissingDescription(rect, task);
                        break;
                }
            }

            private void DrawMoveDescription(Rect rect, Task task)
            {
                Rect sourcePrefixRect = new Rect(rect.x, rect.y, Resources.PrefixSize().x, Resources.PrefixSize().y);

                Rect destinationPrefixRect = sourcePrefixRect;
                destinationPrefixRect.y = sourcePrefixRect.yMax;

                Rect sourceRect = sourcePrefixRect;
                sourceRect.x = sourcePrefixRect.xMax;
                sourceRect.xMax = rect.xMax;

                Rect destinationRect = destinationPrefixRect;
                destinationRect.x = destinationPrefixRect.xMax;
                destinationRect.xMax = rect.xMax;

                EditorGUI.BeginDisabledGroup(true);

                GUI.Label(sourcePrefixRect, Resources.SourcePrefix, Resources.PrefixStyle());
                GUI.Label(destinationPrefixRect, Resources.DestinationPrefix, Resources.PrefixStyle());

                EditorGUI.EndDisabledGroup();

                DrawAssetPath(sourceRect, task.source);
                DrawAssetPath(destinationRect, task.destination);
            }

            private void DrawRemoveFolderDescription(Rect rect, Task task)
            {
                Rect prefixRect = new Rect(rect.x, rect.y, Resources.PrefixSize().x, Resources.PrefixSize().y);

                Rect pathRect = prefixRect;
                pathRect.x = prefixRect.xMax;
                pathRect.width = Resources.AssetPathStyle().CalcSize(new GUIContent(task.source)).x;

                Rect suffixRect = prefixRect;
                suffixRect.x = pathRect.xMax;
                suffixRect.xMax = rect.xMax;

                EditorGUI.BeginDisabledGroup(true);

                GUI.Label(prefixRect, Resources.RemovePrefix, Resources.PrefixStyle());

                EditorGUI.EndDisabledGroup();

                DrawAssetPath(pathRect, task.source);

                EditorGUI.BeginDisabledGroup(true);

                GUI.Label(suffixRect, L10n.Tr("if empty"), Resources.SuffixStyle());

                EditorGUI.EndDisabledGroup();
            }

            private void DrawRemoveAssetDescription(Rect rect, Task task)
            {
                Rect prefixRect = new Rect(rect.x, rect.y, Resources.PrefixSize().x, Resources.PrefixSize().y);

                Rect pathRect = prefixRect;
                pathRect.x = prefixRect.xMax;
                pathRect.width = Resources.AssetPathStyle().CalcSize(new GUIContent(task.source)).x;

                EditorGUI.BeginDisabledGroup(true);

                GUI.Label(prefixRect, Resources.RemovePrefix, Resources.PrefixStyle());

                EditorGUI.EndDisabledGroup();

                DrawAssetPath(pathRect, task.source);
            }

            private void DrawMissingDescription(Rect rect, Task task)
            {
                Rect sourceRect = rect;
                sourceRect.xMin += Resources.PrefixSize().x;

                DrawAssetPath(sourceRect, task.source);
            }
        }

        private static IOrderedEnumerable<T1> Sort<T1, T2>(IEnumerable<T1> enumerable,
            Func<T1, T2> keySelector, bool ascending)
        {
            if (ascending)
            {
                return enumerable.OrderBy(keySelector);
            }
            else
            {
                return enumerable.OrderByDescending(keySelector);
            }
        }

        private static IOrderedEnumerable<T1> SubSort<T1, T2>(IOrderedEnumerable<T1> enumerable,
            Func<T1, T2> keySelector, bool ascending)
        {
            if (ascending)
            {
                return enumerable.ThenBy(keySelector);
            }
            else
            {
                return enumerable.ThenByDescending(keySelector);
            }
        }

        private class Resources
        {
            private static GUIStyle statusColumnStyle;

            private static GUIStyle statusBarStyle;

            private static float statusHeight;

            private static GUIStyle stepStyle;

            public static readonly GUIContent SourcePrefix = new GUIContent("Move");
            public static readonly GUIContent DestinationPrefix = new GUIContent("to");
            public static readonly GUIContent RemovePrefix = new GUIContent(L10n.Tr("Remove"));

            private static Vector2 prefixSize;

            private static GUIStyle prefixStyle;

            private static GUIStyle suffixStyle;

            private static GUIStyle assetPathStyle;

            private static bool cacheInitialized = false;

            public static readonly Dictionary<Task.Status, Texture> StatusIcon =
                new Dictionary<Task.Status, Texture>() {
                {  Task.Status.Pending, EditorGUIUtility.FindTexture("TestNormal") },
                {  Task.Status.Succeeded, EditorGUIUtility.FindTexture("TestPassed") },
                {  Task.Status.Failed, EditorGUIUtility.FindTexture("TestFailed") },
                {  Task.Status.Missing, EditorGUIUtility.FindTexture("console.warnicon.sml") },
            };

            public static readonly Dictionary<Task.Status, GUIContent> StatusContent =
                new Dictionary<Task.Status, GUIContent>() {
                {  Task.Status.Pending, new GUIContent(L10n.Tr("Pending"), StatusIcon[Task.Status.Pending]) },
                {  Task.Status.Succeeded, new GUIContent(L10n.Tr("Succeeded"), StatusIcon[Task.Status.Succeeded]) },
                {  Task.Status.Failed, new GUIContent(L10n.Tr("Failed"), StatusIcon[Task.Status.Failed]) },
                {  Task.Status.Missing, new GUIContent(L10n.Tr("Missing"), StatusIcon[Task.Status.Missing]) },
            };

            public static GUIStyle StatusColumnStyle()
            {
                AffirmCache();
                return statusColumnStyle;
            }

            public static GUIStyle StatusBarStyle()
            {
                AffirmCache();
                return statusBarStyle;
            }

            public static GUIStyle PlatformStyle()
            {
                return StatusColumnStyle();
            }

            public static float StatusHeight()
            {
                if (statusHeight == 0)
                {
                    foreach (var current in StatusIcon)
                    {
                        statusHeight = Math.Max(statusHeight, current.Value.height + 4);
                    }
                }

                return statusHeight;
            }

            public static GUIStyle StepStyle()
            {
                AffirmCache();
                return stepStyle;
            }

            public static Vector2 PrefixSize()
            {
                AffirmCache();
                return prefixSize;
            }

            public static GUIStyle PrefixStyle()
            {
                AffirmCache();
                return prefixStyle;
            }

            public static GUIStyle SuffixStyle()
            {
                AffirmCache();
                return suffixStyle;
            }

            public static GUIStyle AssetPathStyle()
            {
                AffirmCache();
                return assetPathStyle;
            }

            private static void AffirmCache()
            {
                if (!cacheInitialized)
                {
                    cacheInitialized = true;

                    statusColumnStyle = new GUIStyle(GUI.skin.label) {
                        alignment = TextAnchor.MiddleLeft,
                    };

                    statusBarStyle = new GUIStyle(GUI.skin.label) {
                        alignment = TextAnchor.UpperLeft,
                        wordWrap = true,
                    };

                    stepStyle = new GUIStyle(GUI.skin.label) {
                        alignment = TextAnchor.MiddleRight,
                    };

                    prefixStyle = new GUIStyle(GUI.skin.label) {
                        alignment = TextAnchor.MiddleRight,
                    };

                    suffixStyle = new GUIStyle(GUI.skin.label) {
                        alignment = TextAnchor.MiddleLeft,
                    };

                    assetPathStyle = new GUIStyle(GUI.skin.label);

                    prefixSize = prefixStyle.CalcSize(SourcePrefix);
                    prefixSize = Vector2.Max(prefixSize, prefixStyle.CalcSize(DestinationPrefix));
                    prefixSize = Vector2.Max(prefixSize, prefixStyle.CalcSize(RemovePrefix));
                }
            }
        }

        private void OnTaskSelected(Task task)
        {
            if (task != null)
            {
                statusContent = new GUIContent(task.statusText, Resources.StatusIcon[task.status]);
            }
            else
            {
                SetDefaultStatus();
            }
        }

        private void OnGUI()
        {
            if (focusedWindow == this
                && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Escape)
            {
                Cancel();
                Event.current.Use();
            }

            // Task list
            GUILayout.BeginVertical(GUI.skin.box);

            Rect treeViewRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            taskView.OnGUI(treeViewRect);

            GUILayout.EndVertical();

            // Status bar
            GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.Height(EditorGUIUtility.singleLineHeight * 2));

            GUILayout.Label(statusContent.image, GUILayout.ExpandWidth(false));
            EditorGUILayout.SelectableLabel(statusContent.text, Resources.StatusBarStyle());

            GUILayout.EndHorizontal();

            // Buttons
            float buttonHeight = EditorGUIUtility.singleLineHeight * 2;

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(L10n.Tr("Cancel"), GUILayout.Height(buttonHeight)))
            {
                Cancel();
            }

            EditorGUI.BeginDisabledGroup(IsProcessing());

            if (GUILayout.Button(L10n.Tr("Refresh"), GUILayout.Height(buttonHeight)))
            {
                PopulateTasks();
            }

            EditorGUI.EndDisabledGroup();

            if (IsProcessing())
            {
                EditorGUI.BeginDisabledGroup(true);

                GUILayout.Button(string.Format(L10n.Tr("Processing Task {0} of {1}"), currentTask, taskCount), GUILayout.Height(buttonHeight));

                EditorGUI.EndDisabledGroup();
            }
            else
            {
                if (GUILayout.Button(string.Format(L10n.Tr("Process {0} Tasks"), taskCount), GUILayout.Height(buttonHeight)))
                {
                    StartProcessing();
                }
            }

            GUILayout.EndHorizontal();
        }

        private void Cancel()
        {
            if (IsProcessing())
            {
                StopProcessing();
            }
            else
            {
                Close();
            }
        }

        private static void DrawAssetPath(Rect rect, string path)
        {
            GUIStyle pathStyle = Resources.AssetPathStyle();
            GUIContent pathContent = new GUIContent(path);

            Rect pathRect = rect;
            pathRect.width = pathStyle.CalcSize(pathContent).x;

            GUI.Label(pathRect, pathContent, pathStyle);
            EditorGUIUtility.AddCursorRect(pathRect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown
                && pathRect.Contains(Event.current.mousePosition))
            {
                SelectAssetOrParentFolder(path);
                Event.current.Use();
            }
        }

        private static void SelectAssetOrParentFolder(string path)
        {
            while (!AssetExists(path))
            {
                path = EditorUtils.GetParentFolder(path);

                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
        }

        private void OnInspectorUpdate()
        {
            ProcessNextTask();
        }

        private struct TaskGenerator
        {
            private const string AssetsFolder = "Assets";
            private const string FMODRoot = "Assets/Plugins/FMOD";
            private const string FMODSource = FMODRoot + "/src";

            private static readonly string[] BaseFolders = {
                FMODSource,
                FMODRoot,
                "Assets/Plugins",
                "Assets",
            };

            private static readonly MoveRecord[] looseAssets = {
                // Release 1.10 layout
                new MoveRecord() { source = FMODRoot + "/fmodplugins.cpp", destination = "obsolete" },
                new MoveRecord() { source = "Assets/Editor/FMODMigrationUtil.cs", destination = "obsolete" },
                new MoveRecord() { source = "Assets/GoogleVR", destination = "addons" },
                new MoveRecord() { source = "Assets/ResonanceAudio", destination = "addons" },
                new MoveRecord() { source = "Assets/Resources/FMODStudioSettings.asset", destination = "Resources" },
                new MoveRecord() { source = "Assets/FMODStudioCache.asset", destination = "Resources" },

                // Release 2.0 layout
                new MoveRecord() { source = FMODRoot + "/src/Runtime/fmodplugins.cpp", destination = "obsolete" },

                // Release 2.1 layout
                new MoveRecord() { source = FMODRoot + "/src/Runtime/fmod_static_plugin_support.h", destination = "obsolete" },
                new MoveRecord() { source = FMODRoot + "/src/Runtime/CodeGeneration.cs", destination = "src/Editor" },

                // Release 2.2 layout
                new MoveRecord() { source = FMODRoot + "/src/fmodplugins.cpp", destination = "obsolete" },
                new MoveRecord() { source = FMODRoot + "/src/fmod_static_plugin_support.h", destination = "obsolete" },
                new MoveRecord() { source = FMODSource + "/CodeGeneration.cs", destination = "src/Editor" },

                // Release 2.3 layout
                new MoveRecord() { source = FMODRoot + "/platforms/html5/lib/libfmodstudiounityplugin.bc", destination = "obsolete" },
                new MoveRecord() { source = FMODRoot + "/platforms/html5/lib/libfmodstudiounitypluginL.bc", destination = "obsolete" },
            };

            private static readonly string[] fmodFoldersToCleanUp = {
                "Assets/Plugins/FMOD/Runtime",
                "Assets/Plugins/FMOD/lib",
            };
            private static readonly string[] publicFoldersToCleanUp = {
                "Assets/Plugins/Editor",
            };

            private List<Task> tasks;

            public static void Generate(List<Task> tasks)
            {
                TaskGenerator generator = new TaskGenerator() { tasks = tasks };

                Settings.Instance.Platforms.ForEach(generator.GenerateTasksForPlatform);
                generator.GenerateTasksForLooseAssets();
                generator.GenerateTasksForCodeFolders();
                generator.GenerateTasksForLegacyCodeFiles();
                generator.GenerateTasksForFolderCleanup();
            }

            private void GenerateTasksForPlatform(Platform platform)
            {
                IEnumerable<Platform.FileInfo> files = platform.GetSourceFileInfo().Cast<Platform.FileInfo>();

                foreach (BuildTarget buildTarget in platform.GetBuildTargets())
                {
                    files = files.Concat(platform.GetBinaryFileInfo(buildTarget, Platform.BinaryType.All).Cast<Platform.FileInfo>());
                }

                foreach (Platform.FileInfo info in files)
                {
                    string newPath = info.LatestLocation();

                    if (!AssetExists(newPath))
                    {
                        bool foundPath = false;
                        string oldPath = null;

                        foreach (string path in info.OldLocations())
                        {
                            oldPath = path;

                            if (tasks.Any(t => t.source == oldPath))
                            {
                                foundPath = true;
                                break;
                            }

                            if (AssetExists(oldPath))
                            {
                                tasks.Add(Task.Move(oldPath, newPath, platform));

                                foundPath = true;
                                break;
                            }
                        }

                        if (oldPath != null)
                        {
                            string oldFolder = EditorUtils.GetParentFolder(oldPath);
                            string newFolder = EditorUtils.GetParentFolder(newPath);

                            if (newFolder != oldFolder)
                            {
                                AddFolderTasks(oldFolder);
                            }
                        }

                        if (!foundPath && ((info.type & Platform.BinaryType.Optional) == 0)
                            && !tasks.Any(t => t.source == newPath))
                        {
                            tasks.Add(Task.Missing(newPath, platform));
                        }
                    }
                }

                foreach (string path in platform.GetObsoleteAssetPaths())
                {
                    if (AssetExists(path) && !tasks.Any(t => t.source == path))
                    {
                        tasks.Add(Task.RemoveAsset(path, platform));
                    }
                }
            }

           private void AddFolderTasks(string path)
            {
                string baseFolder = BaseFolders.First(f => path.StartsWith(f));

                string currentFolder = path;

                // Find the last folder in the path that exists, without leaving the base folder
                while (currentFolder.StartsWith(baseFolder) && !AssetDatabase.IsValidFolder(currentFolder))
                {
                    currentFolder = EditorUtils.GetParentFolder(currentFolder);
                }

                while (currentFolder.StartsWith(baseFolder) && currentFolder != baseFolder)
                {
                    AddFolderTask(currentFolder);
                    currentFolder = EditorUtils.GetParentFolder(currentFolder);
                }
            }

            private void AddFolderTask(string path)
            {
                if (!tasks.Any(t => t.type == Task.Type.RemoveFolder && t.source == path))
                {
                    tasks.Add(Task.RemoveFolder(path));
                }
            }

            private struct MoveRecord
            {
                public string source;
                public string destination;
            }

            private static readonly MoveRecord[] codeFolders = {
                // Release 2.0 layout
                new MoveRecord() { source = FMODSource + "/Runtime", destination = "src" },
                new MoveRecord() { source = FMODSource + "/Runtime/Timeline", destination = "src" },
                new MoveRecord() { source = FMODSource + "/Runtime/wrapper", destination = "src" },
                new MoveRecord() { source = FMODSource + "/Editor/Timeline", destination = "src/Editor" },

                // Release 1.10 layout
                new MoveRecord() { source = FMODRoot + "/Timeline", destination = "src" },
                new MoveRecord() { source = FMODRoot + "/Wrapper", destination = "src" },
                new MoveRecord() { source = "Assets/Plugins/Editor/FMOD", destination = "src/Editor" },
                new MoveRecord() { source = "Assets/Plugins/Editor/FMOD/Timeline", destination = "src/Editor" },
            };

            private void AddMoveTask(string source, string destination)
            {
                if (!tasks.Any(t => t.source == source))
                {
                    tasks.Add(Task.Move(source, destination, null));
                }
            }

            private void GenerateTasksForCodeFolders()
            {
                foreach (MoveRecord folder in codeFolders)
                {
                    if (AssetDatabase.IsValidFolder(folder.source))
                    {
                        foreach (string sourcePath in FindFileAssets(folder.source))
                        {
                            string filename = Path.GetFileName(sourcePath);

                            AddMoveTask(
                                sourcePath, $"{RuntimeUtils.PluginBasePath}/{folder.destination}/{filename}");

                        }

                        AddFolderTask(folder.source);
                    }
                }
            }

            private void GenerateTasksForLooseAssets()
            {
                foreach (MoveRecord asset in looseAssets)
                {
                    string filename = Path.GetFileName(asset.source);
                    string destinationPath = $"{RuntimeUtils.PluginBasePath}/{asset.destination}/{filename}";

                    if (AssetExists(asset.source) && !AssetExists(destinationPath))
                    {
                        AddMoveTask(asset.source, destinationPath);
                        AddFolderTasks(EditorUtils.GetParentFolder(asset.source));
                    }
                    else if (AssetDatabase.IsValidFolder(asset.source) && AssetDatabase.IsValidFolder(destinationPath))
                    {
                        GenerateFolderMergeTasks(asset.source, destinationPath);
                        AddFolderTasks(asset.source);
                    }
                }
            }

            private void GenerateFolderMergeTasks(string sourceFolder, string destinationFolder)
            {
                IEnumerable<string> assetPaths = AssetDatabase.FindAssets(string.Empty, new string[] { sourceFolder })
                    .Select(g => AssetDatabase.GUIDToAssetPath(g))
                    .Where(p => !AssetDatabase.IsValidFolder(p) || IsFolderEmpty(p));

                foreach (string sourcePath in assetPaths)
                {
                    int prefixLength = sourceFolder.Length;

                    if (!sourceFolder.EndsWith("/"))
                    {
                        ++prefixLength;
                    }

                    string relativePath = sourcePath.Substring(prefixLength);
                    string destinationPath = string.Format("{0}/{1}", destinationFolder, relativePath);

                    if (!AssetExists(destinationPath))
                    {
                        AddMoveTask(sourcePath, destinationPath);
                        AddFolderTasks(EditorUtils.GetParentFolder(sourcePath));
                    }
                    else if (AssetDatabase.IsValidFolder(sourcePath))
                    {
                        AddFolderTasks(sourcePath);
                    }
                }
            }

            private void GenerateTasksForLegacyCodeFiles()
            {
                foreach (string path in FindFileAssets(FMODRoot).Where(p => p.EndsWith(".cs")))
                {
                    string destinationPath = $"{RuntimeUtils.PluginBasePath}/src/{Path.GetFileName(path)}";

                    if (!AssetExists(destinationPath))
                    {
                        AddMoveTask(path, destinationPath);
                    }
                }
            }

            private void GenerateTasksForFolderCleanup()
            {
                foreach (string folder in publicFoldersToCleanUp)
                {
                    if (AssetDatabase.IsValidFolder(folder))
                    {
                        AddFolderTask(folder);
                    }
                }
                foreach (string folder in fmodFoldersToCleanUp)
                {
                    SearchSubFolders(folder);
                }
            }

            private void SearchSubFolders(string folder)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    var subdirs = AssetDatabase.GetSubFolders(folder);
                    foreach (var subfolder in subdirs)
                    {
                        SearchSubFolders(subfolder);
                        AddFolderTask(subfolder);
                    }
                    AddFolderTask(folder);
                }
            }

            private static IEnumerable<string> FindFileAssets(string folder)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    return AssetDatabase.FindAssets(string.Empty, new string[] { folder })
                        .Select(g => AssetDatabase.GUIDToAssetPath(g))
                        .Where(p => (EditorUtils.GetParentFolder(p) == folder) && !AssetDatabase.IsValidFolder(p));
                }
                else
                {
                    return Enumerable.Empty<string>();
                }
            }
        }

        private void StartProcessing()
        {
            if (!IsProcessing())
            {
                EditorApplication.LockReloadAssemblies();

                currentTask = 0;
                processingState = ProcessMoveTasks()
                    .Concat(ProcessRemoveAssetTasks())
                    .Concat(ProcessRemoveFolderTasks())
                    .GetEnumerator();
            }
        }

        private void StopProcessing()
        {
            if (IsProcessing())
            {
                processingState = null;
                UpdateTaskCount();
                SetDefaultStatus();

                EditorApplication.UnlockReloadAssemblies();

                if (taskCount == 0)
                {
                    SetupWizardWindow.SetUpdateTaskComplete(SetupWizardWindow.UpdateTaskType.ReorganizePluginFiles);
                }
            }
        }

        private bool IsProcessing()
        {
            return processingState != null;
        }

        private void ProcessNextTask()
        {
            if (processingState != null)
            {
                if (processingState.MoveNext())
                {
                    statusContent = new GUIContent(processingState.Current);
                    Repaint();
                }
                else
                {
                    StopProcessing();
                }
            }
        }

        private IEnumerable<string> ProcessMoveTasks()
        {
            foreach (Task task in tasks.Where(t => t.type == Task.Type.Move && t.status == Task.Status.Pending))
            {
                EditorUtils.EnsureFolderExists(EditorUtils.GetParentFolder(task.destination));

                currentTask = task.step;

                yield return string.Format(L10n.Tr("Moving {0} to {1}"), task.source, task.destination);

                string result = AssetDatabase.MoveAsset(task.source, task.destination);

                if (string.IsNullOrEmpty(result))
                {
                    task.SetSucceeded(string.Format(L10n.Tr("{0} was moved to\n{1}"), task.source, task.destination));
                }
                else
                {
                    task.SetFailed(string.Format(L10n.Tr("{0} could not be moved to\n{1}: '{2}'"),
                        task.source, task.destination, result));
                }

                yield return task.statusText;
            }
        }

        private static bool AssetExists(string path)
        {
            return EditorUtils.AssetExists(path);
        }

        private IEnumerable<string> ProcessRemoveAssetTasks()
        {
            foreach (Task task in tasks.Where(t => t.type == Task.Type.RemoveAsset && t.status == Task.Status.Pending))
            {
                currentTask = task.step;

                if (AssetDatabase.MoveAssetToTrash(task.source))
                {
                    task.SetSucceeded(string.Format(L10n.Tr("{0} was removed"), task.source));
                }
                else
                {
                    task.SetFailed(string.Format(L10n.Tr("{0} could not be removed"), task.source));
                }

                yield return task.statusText;
            }
        }

        private static bool IsFolderEmpty(string path)
        {
            return AssetDatabase.FindAssets(string.Empty, new string[] { path }).Length == 0;
        }

        private IEnumerable<string> ProcessRemoveFolderTasks()
        {
            foreach (Task task in tasks.Where(t => t.type == Task.Type.RemoveFolder && t.status == Task.Status.Pending))
            {
                currentTask = task.step;

                foreach (string result in RemoveFolderIfEmpty(task))
                {
                    yield return result;
                }
            }
        }

        private static IEnumerable<string> RemoveFolderIfEmpty(Task task)
        {
            if (!Directory.Exists(Application.dataPath + "/../" + task.source))
            {
                task.SetSucceeded(string.Format(L10n.Tr("{0} has already been removed"), task.source));
                yield break;
            }

            if (!AssetDatabase.IsValidFolder(task.source))
            {
                task.SetFailed(string.Format(L10n.Tr("{0} is not a valid folder"), task.source));
                yield break;
            }

            if (!IsFolderEmpty(task.source))
            {
                task.SetFailed(string.Format(L10n.Tr("{0} is not empty"), task.source));
                yield break;
            }

            yield return string.Format(L10n.Tr("Removing empty folder {0}"), task.source);

            if (AssetDatabase.MoveAssetToTrash(task.source))
            {
                task.SetSucceeded(string.Format(L10n.Tr("{0} was removed"), task.source));
            }
            else
            {
                task.SetFailed(string.Format(L10n.Tr("{0} could not be removed"), task.source));
            }

            yield return task.statusText;
        }
    }
}
