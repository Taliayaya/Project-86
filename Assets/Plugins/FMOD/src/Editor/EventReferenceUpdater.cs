using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_INPUTSYSTEM_EXIST
using UnityEngine.InputSystem;
#endif

namespace FMODUnity
{
    public class EventReferenceUpdater : EditorWindow
    {
        public const string MenuPath = "FMOD/Update Event References";

        private const int EventReferenceTransitionVersion = 0x00020200;

        private const BindingFlags DefaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly string HelpText =
            string.Format(L10n.Tr("Click Scan to search your project for obsolete event references."));

        private readonly string[] SearchFolders = {
            "Assets",
        };

        private SceneSetup[] sceneSetup;

        private IEnumerator<string> processingState;

        private SearchProgress prefabProgress;
        private SearchProgress sceneProgress;
        private SearchProgress scriptableObjectProgress;

        [SerializeField]
        private List<Asset> assets = new List<Asset>();

        [SerializeField]
        private List<Component> components = new List<Component>();

        [SerializeField]
        private List<Task> tasks = new List<Task>();

        private int executableTaskCount = 0;

        private TreeViewState taskViewState = new TreeViewState();

        private TaskView taskView;

        [NonSerialized]
        private GUIContent status = GUIContent.none;

        [NonSerialized]
        private Task selectedTask;

        [NonSerialized]
        private Vector2 manualDescriptionScrollPosition;

        [NonSerialized]
        private static GUIContent AssetContent = new GUIContent(L10n.Tr("Asset"));
        private static GUIContent ComponentTypeContent = new GUIContent(L10n.Tr("Component Type"));
        private static GUIContent GameObjectContent = new GUIContent(L10n.Tr("Game Object"));

        private string ExecuteButtonText()
        {
            return string.Format(L10n.Tr("Execute {0} Selected Tasks"), executableTaskCount);
        }

        [MenuItem(MenuPath)]
        public static void ShowWindow()
        {
            EventReferenceUpdater updater = GetWindow<EventReferenceUpdater>(L10n.Tr("FMOD Event Reference Updater"));
            updater.minSize = new Vector2(800, 600);

            updater.SetStatus(HelpText);

            updater.Show();
        }

        public static bool IsUpToDate()
        {
            return Settings.Instance.LastEventReferenceScanVersion >= EventReferenceTransitionVersion;
        }

        private void BeginSearching()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                tasks.Clear();
                executableTaskCount = 0;
                taskView.SetSelection(new List<int>(), TreeViewSelectionOptions.FireSelectionChanged);
                taskView.Reload();

                processingState = SearchProject();
            }
        }

        private void StopProcessing(bool isComplete)
        {
            processingState = null;

            if (isComplete)
            {
                if (tasks.Count == 0)
                {
                    SetStatus(L10n.Tr("No required tasks found. Event references are up to date."));
                    Settings.Instance.LastEventReferenceScanVersion = FMOD.VERSION.number;
                    EditorUtility.SetDirty(Settings.Instance);

                    SetupWizardWindow.SetUpdateTaskComplete(SetupWizardWindow.UpdateTaskType.UpdateEventReferences);
                }
                else if (tasks.All(x => x.HasExecuted))
                {
                    SetStatus(L10n.Tr("Finished executing tasks. New tasks may now be required. Please re-scan your project."));
                }
                else
                {
                    SetStatus(L10n.Tr("Finished scanning. Please execute the tasks above."));
                }
            }
            else
            {
                SetStatus(L10n.Tr("Cancelled."));
            }
        }

        private void BeginExecuting()
        {
            Task[] enabledTasks = tasks.Where(t => t.CanExecute()).ToArray();

            if (enabledTasks.Length == 0)
            {
                return;
            }

            Asset[] affectedAssets = enabledTasks.Select(t => assets[t.AssetIndex]).Distinct().ToArray();

            int prefabCount = affectedAssets.Count(a => IsPrefab(a.Type));
            int sceneCount = affectedAssets.Count(a => a.Type == AssetType.Scene);

            string warningText = string.Format(
                L10n.Tr("Executing these {0} tasks will change {1} prefabs and {2} scenes on disk.\n\nPlease ensure you have committed any outstanding changes to source control before continuing!"),
                enabledTasks.Length, prefabCount, sceneCount);

            if (!EditorUtility.DisplayDialog(L10n.Tr("Confirm Bulk Changes"), warningText, ExecuteButtonText(), L10n.Tr("Cancel")))
            {
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                processingState = ExecuteTasks(enabledTasks);
            }
        }

        private void Cancel()
        {
            if (IsProcessing)
            {
                StopProcessing(false);
            }
            else
            {
                Close();
            }
        }

        private bool IsProcessing { get { return processingState != null; } }

        private struct SearchProgress
        {
            private int maximum;
            private int current;

            public float Fraction()
            {
                return (maximum > 0) ? (current / (float)maximum) : 1;
            }

            public void Increment()
            {
                if (current < maximum)
                {
                    ++current;
                }
            }

            public SearchProgress(int total)
            {
                this.maximum = total;
                this.current = 0;
            }
        }

        private IEnumerator<string> SearchProject()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:GameObject", SearchFolders);
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", SearchFolders);
            string[] scriptableObjectGuids =
                AssetDatabase.FindAssets("t:ScriptableObject", SearchFolders).Distinct().ToArray();

            prefabProgress = new SearchProgress(prefabGuids.Length);
            sceneProgress = new SearchProgress(sceneGuids.Length);
            scriptableObjectProgress = new SearchProgress(scriptableObjectGuids.Length);

            return SearchPrefabs(prefabGuids)
                .Concat(SearchScriptableObjects(scriptableObjectGuids))
                .Concat(SearchScenes(sceneGuids))
                .GetEnumerator();
        }

        private IEnumerable<string> SearchPrefabs(string[] guids)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                yield return string.Format(L10n.Tr("Searching {0}"), path);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                int assetIndex = -1;

                foreach (Task task in SearchGameObject(prefab, prefab))
                {
                    if (assetIndex < 0)
                    {
                        assetIndex = AddAsset(GetAssetType(prefab), path);
                    }

                    task.AssetIndex = assetIndex;

                    AddTask(task);
                }

                prefabProgress.Increment();
            }
        }

        private IEnumerable<string> SearchScriptableObjects(string[] guids)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                yield return string.Format(L10n.Tr("Searching {0}"), path);

                IEnumerable<ScriptableObject> scriptableObjects =
                    AssetDatabase.LoadAllAssetsAtPath(path).OfType<ScriptableObject>();

                int assetIndex = -1;

                foreach (ScriptableObject scriptableObject in scriptableObjects)
                {
                    int componentIndex = -1;

                    foreach (Task task in GetUpdateTasks(scriptableObject))
                    {
                        if (assetIndex < 0)
                        {
                            assetIndex = AddAsset(AssetType.ScriptableObject, path);
                        }

                        if (componentIndex < 0)
                        {
                            componentIndex = AddComponent(scriptableObject);
                        }

                        task.AssetIndex = assetIndex;
                        task.ComponentIndex = componentIndex;

                        AddTask(task);
                    }
                }

                scriptableObjectProgress.Increment();
            }
        }

        private IEnumerable<string> SearchScenes(string[] guids)
        {
            sceneSetup = EditorSceneManager.GetSceneManagerSetup();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                yield return string.Format(L10n.Tr("Searching {0}"), path);

                Scene scene = SceneManager.GetSceneByPath(path);

                if (!scene.IsValid())
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                }

                int assetIndex = -1;

                foreach (GameObject gameObject in scene.GetRootGameObjects())
                {
                    foreach (Task task in SearchGameObject(gameObject, null))
                    {
                        if (assetIndex < 0)
                        {
                            assetIndex = AddAsset(AssetType.Scene,  path);
                        }

                        task.AssetIndex = assetIndex;

                        AddTask(task);
                    }
                }

                sceneProgress.Increment();
            }

            if (sceneSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
            }
        }

        private IEnumerable<Task> SearchGameObject(GameObject gameObject, GameObject root)
        {
            MonoBehaviour[] behaviours = gameObject.GetComponentsInChildren<MonoBehaviour>(true);

            foreach (MonoBehaviour behaviour in behaviours)
            {
                int componentIndex = -1;

                foreach (Task task in GetUpdateTasks(behaviour))
                {
                    if (componentIndex < 0)
                    {
                        componentIndex = AddComponent(behaviour, root);
                    }

                    task.ComponentIndex = componentIndex;

                    yield return task;
                }
            }
        }

        private static IEnumerable<Task> GetUpdateTasks(UnityEngine.Object target)
        {
            if (target == null)
            {
                return Enumerable.Empty<Task>();
            }
            else if (target is StudioEventEmitter)
            {
                return GetEmitterUpdateTasks(target as StudioEventEmitter);
            }
#if UNITY_TIMELINE_EXIST
            else if (target is FMODEventPlayable)
            {
                return GetPlayableUpdateTasks(target as FMODEventPlayable);
            }
#endif
            else
            {
                return GetGenericUpdateTasks(target);
            }
        }

        private static IEnumerable<Task> GetEmitterUpdateTasks(StudioEventEmitter emitter)
        {
            bool hasOwnEvent = true;
            bool hasOwnEventReference = true;

            if (PrefabUtility.IsPartOfPrefabInstance(emitter))
            {
                StudioEventEmitter sourceEmitter = PrefabUtility.GetCorrespondingObjectFromSource(emitter);
                PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(emitter);

                if (modifications != null) // GetPropertyModifications returns null if the prefab instance is disconnected
                {
                    hasOwnEvent = modifications.Any(
                        m => m.target == sourceEmitter && m.propertyPath == "Event");

                    hasOwnEventReference = modifications.Any(
                        m => m.target == sourceEmitter && m.propertyPath.StartsWith("EventReference"));
                }
            }

            if (hasOwnEventReference)
            {
                Task updateTask = GetUpdateEventReferenceTask(emitter.EventReference, "EventReference");
                if (updateTask != null)
                {
                    yield return updateTask;
                }

                if (hasOwnEvent)
                {
#pragma warning disable 0618 // Suppress warnings about using the obsolete StudioEventEmitter.Event field
                    if (!string.IsNullOrEmpty(emitter.Event))
#pragma warning restore 0618
                    {
                        if (emitter.EventReference.IsNull)
                        {
                            yield return Task.MoveEventToEventReference(emitter);
                        }
                        else
                        {
                            yield return Task.ClearEvent(emitter);
                        }
                    }
                }
            }
            else if (hasOwnEvent)
            {
                yield return Task.MoveEventOverrideToEventReference(emitter);
            }
        }

        private static Task GetUpdateEventReferenceTask(EventReference eventReference, string fieldName,
            string subObjectPath = null)
        {
#if !FMOD_SERIALIZE_GUID_ONLY
            if (eventReference.IsNull)
            {
                return null;
            }

            if (Settings.Instance.EventLinkage == EventLinkage.GUID)
            {
                EditorEventRef editorEventRef = EventManager.EventFromGUID(eventReference.Guid);

                if (editorEventRef == null)
                {
                    return null;
                }

                if (eventReference.Path != editorEventRef.Path)
                {
                    return Task.UpdateEventReferencePath(subObjectPath, fieldName, eventReference.Path,
                        editorEventRef.Path, eventReference.Guid);
                }
            }
            else if (Settings.Instance.EventLinkage == EventLinkage.Path)
            {
                EditorEventRef editorEventRef = EventManager.EventFromPath(eventReference.Path);

                if (editorEventRef != null)
                {
                    if (eventReference.Guid != editorEventRef.Guid)
                    {
                        return Task.UpdateEventReferenceGuid(subObjectPath, fieldName, eventReference.Guid,
                            editorEventRef.Guid, eventReference.Path);
                    }
                }
                else if (!eventReference.Guid.IsNull)
                {
                    editorEventRef = EventManager.EventFromGUID(eventReference.Guid);

                    if (editorEventRef != null)
                    {
                        return Task.UpdateEventReferencePath(subObjectPath, fieldName, eventReference.Path,
                            editorEventRef.Path, eventReference.Guid);
                    }
                }
            }
            else
            {
                throw new NotSupportedException("Unrecognized EventLinkage: " + Settings.Instance.EventLinkage);
            }
#endif

            return null;
        }

#if UNITY_TIMELINE_EXIST
        private static IEnumerable<Task> GetPlayableUpdateTasks(FMODEventPlayable playable)
        {
            Task updateTask = GetUpdateEventReferenceTask(playable.EventReference, "EventReference");
            if (updateTask != null)
            {
                yield return updateTask;
            }

#pragma warning disable 0618 // Suppress warnings about using the obsolete FMODEventPlayable.eventName field
            if (!string.IsNullOrEmpty(playable.eventName))
#pragma warning restore 0618
            {
                if (playable.EventReference.IsNull)
                {
                    yield return Task.MoveEventNameToEventReference(playable);
                }
                else
                {
                    yield return Task.ClearEventName(playable);
                }
            }
        }
#endif

#pragma warning disable 0618 // Suppress a warning about using the obsolete EventRefAttribute class
        private static bool IsEventRef(FieldInfo field)
        {
            return field.FieldType == typeof(string) && EditorUtils.HasAttribute<EventRefAttribute>(field);
        }
#pragma warning restore 0618

        private static T GetCustomAttribute<T>(FieldInfo field)
            where T : Attribute
        {
            return Attribute.GetCustomAttribute(field, typeof(T)) as T;
        }

        private static readonly Assembly SystemAssembly = typeof(object).Assembly;

        private static IEnumerable<Task> GetGenericUpdateTasks(object target, string subObjectPath = null, IEnumerable<object> parents = null)
        {
            Type targetType = target.GetType();
            FieldInfo[] fields = targetType.GetFields(DefaultBindingFlags);

            List<FieldInfo> oldFields = new List<FieldInfo>();
            List<FieldInfo> newFields = new List<FieldInfo>();
            List<FieldInfo> subObjectFields = new List<FieldInfo>();

            foreach (FieldInfo f in fields)
            {
                if (IsEventRef(f))
                {
                    oldFields.Add(f);
                }
                else if (f.FieldType == typeof(EventReference))
                {
                    newFields.Add(f);
                }
                else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(f.FieldType))
                {
                    subObjectFields.Add(f);
                }
                else if (f.FieldType.Assembly != SystemAssembly && !f.FieldType.IsEnum)
                {
                    subObjectFields.Add(f);
                }
            }

            int initialOldFieldCount = oldFields.Count;

            // Remove empty [EventRef] fields
            for (int i = 0; i < oldFields.Count; )
            {
                FieldInfo oldField = oldFields[i];

                if (string.IsNullOrEmpty(oldField.GetValue(target) as string))
                {
                    oldFields.RemoveAt(i);

                    yield return Task.RemoveEmptyEventRefField(subObjectPath, oldField.Name, targetType.Name);
                }
                else
                {
                    ++i;
                }
            }

            // Handle conflicts where multiple [EventRef] fields have the same migration target
#pragma warning disable 0618 // Suppress a warning about using the obsolete EventRefAttribute class
            IGrouping<string, FieldInfo>[] conflictingGroups = oldFields
                .GroupBy(f => GetCustomAttribute<EventRefAttribute>(f).MigrateTo)
                .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1)
                .ToArray();
#pragma warning restore 0618

            foreach (IGrouping<string, FieldInfo> group in conflictingGroups)
            {
                foreach (FieldInfo field in group)
                {
                    oldFields.Remove(field);
                }

                yield return Task.FixMigrationTargetConflict(subObjectPath, targetType.Name, group.Select(f => f.Name));
            }

            // Handle [EventRef] fields with MigrateTo set
#pragma warning disable 0618 // Suppress a warning about using the obsolete EventRefAttribute class
            for (int i = 0; i < oldFields.Count; )
            {
                FieldInfo oldField = oldFields[i];

                EventRefAttribute attribute = GetCustomAttribute<EventRefAttribute>(oldField);

                if (!string.IsNullOrEmpty(attribute.MigrateTo))
                {
                    oldFields.RemoveAt(i);

                    string oldValue = oldField.GetValue(target) as string;

                    FieldInfo newField = newFields.FirstOrDefault(f => f.Name == attribute.MigrateTo);

                    if (newField != null)
                    {
                        EventReference newValue = (EventReference)newField.GetValue(target);

                        if (newValue.IsNull)
                        {
                            yield return Task.MoveEventRefFieldToEventReferenceField(subObjectPath, oldValue,
                                oldField.Name, newField.Name);
                        }
                        else
                        {
                            yield return Task.RemoveEventRefField(subObjectPath, oldValue, oldField.Name, targetType.Name);
                        }
                    }
                    else
                    {
                        yield return Task.AddMigrationTarget(subObjectPath, oldValue, oldField.Name, targetType.Name,
                            attribute.MigrateTo);
                    }
                }
                else
                {
                    ++i;
                }
            }
#pragma warning restore 0618

            // Auto-migrate if there is a single old field that hasn't been handled already,
            // and there is a single new field
            if (initialOldFieldCount == 1 && oldFields.Count == 1 && newFields.Count == 1)
            {
                FieldInfo oldField = oldFields[0];

                string oldValue = oldField.GetValue(target) as string;

                FieldInfo newField = newFields[0];

                EventReference newValue = (EventReference)newField.GetValue(target);

                if (newValue.IsNull)
                {
                    yield return Task.MoveEventRefFieldToEventReferenceField(subObjectPath, oldValue,
                        oldField.Name, newField.Name);
                }
                else
                {
                    yield return Task.RemoveEventRefField(subObjectPath, oldValue, oldField.Name, targetType.Name);
                }

                oldFields.RemoveAt(0);
            }

            // Handle old fields with no migration target
            foreach (FieldInfo oldField in oldFields)
            {
                yield return Task.AddMigrationTarget(subObjectPath, oldField.GetValue(target) as string, oldField.Name,
                    targetType.Name);
            }

            // Check new fields for GUID/path mismatches
            foreach (FieldInfo newField in newFields)
            {
                EventReference eventReference = (EventReference)newField.GetValue(target);

                Task updateTask = GetUpdateEventReferenceTask(eventReference, newField.Name, subObjectPath);
                if (updateTask != null)
                {
                    yield return updateTask;
                }
            }

            // Check sub-object fields
            if (subObjectFields.Any())
            {
                if (parents == null)
                {
                    parents = Enumerable.Empty<object>();
                }

                parents = parents.Append(target);

                foreach (FieldInfo subObjectField in subObjectFields)
                {
                    object value = subObjectField.GetValue(target);
                    if (value == null || (value is UnityEngine.Object && !(value as UnityEngine.Object)))
                    {
                        continue;
                    }

                    if (subObjectField.FieldType.IsValueType || !parents.Contains(value))
                    {
                        if (value is System.Collections.IEnumerable && !(value is string))
                        {
                            int index = 0;
                            System.Collections.IEnumerator valueEnumerator = null;

                            try
                            {
                                valueEnumerator = (value as System.Collections.IEnumerable).GetEnumerator();
                            }
                            catch (Exception ex)
                            {
                                RuntimeUtils.DebugLogWarningFormat("[FMOD] Failed to get enumerator for value in field '{0}': {1}", subObjectField.Name, ex.Message);
                                continue;
                            }

                            for (;;)
                            {
                                object item = null;
                                try
                                {
                                    if (!valueEnumerator.MoveNext())
                                    {
                                        break;
                                    }
                                    item = valueEnumerator.Current;
                                }
                                catch (Exception)
                                {
                                    break;
                                }
                                if (item != null && !item.GetType().IsPrimitive && !parents.Contains(item)
                                    && item.GetType().Namespace != "UnityEngine.InputSystem")
                                {
                                    foreach (Task t in GetGenericUpdateTasks(item, FieldPath(subObjectPath, subObjectField.Name, index), parents))
                                    {
                                        yield return t;
                                    }
                                }
                                index++;
                            }
                        }
                        else
                        {
                            foreach (Task t in GetGenericUpdateTasks(value, FieldPath(subObjectPath, subObjectField.Name), parents))
                            {
                                yield return t;
                            }
                        }
                    }
                }
            }
        }

        private IEnumerator<string> ExecuteTasks(Task[] tasks)
        {
            sceneSetup = EditorSceneManager.GetSceneManagerSetup();

            foreach (Task task in tasks)
            {
                yield return string.Format(L10n.Tr("Executing: {0}"), task);

                ExecuteTask(task, SavePolicy.AutoSave);
            }

            EditorSceneManager.SaveOpenScenes();
            UpdateExecutableTaskCount();

            if (sceneSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
            }
        }

        private enum AssetType
        {
            Scene,
            Prefab,
            PrefabModel,
            PrefabVariant,
            ScriptableObject,
        }

        private static bool IsPrefab(AssetType type)
        {
            return type == AssetType.Prefab
                || type == AssetType.PrefabModel
                || type == AssetType.PrefabVariant;
        }

        private static AssetType GetAssetType(GameObject gameObject)
        {
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(gameObject);

            if (prefabType == PrefabAssetType.Model)
            {
                return AssetType.PrefabModel;
            }
            else if (prefabType == PrefabAssetType.Variant)
            {
                return AssetType.PrefabVariant;
            }
            else
            {
                return AssetType.Prefab;
            }
        }

        private enum EnableState
        {
            Enabled,
            Disabled,
            Mixed,
        }

        [Serializable]
        private class Asset
        {
            public AssetType Type;
            public string Path;
            public EnableState EnableState;
        }

        [Serializable]
        private class Component
        {
            public GlobalObjectId GameObjectID;
            public string Type;
            public string Path;
            public string ScriptPath;
        }

        [Serializable]
        private class Task
        {
            public bool Enabled = true;
            public int AssetIndex; // index into the assets list
            public int ComponentIndex; // index into the components list

            private Type type;
            private string[] Data;

            private const string EmitterEventField = "Event";
            private const string EmitterEventReferenceField = "EventReference";
            private const string PlayableEventNameField = "eventName";
            private const string PlayableEventReferenceField = "eventReference";

            private delegate string DescriptionDelegate(string[] data);
            private delegate string ManualInstructionsDelegate(string[] data, Component component);
            private delegate bool IsValidDelegate(string[] data, UnityEngine.Object target);
            private delegate void ExecuteDelegate(string[] data, UnityEngine.Object target);

            private static readonly Delegates[] Implementations;

            private enum Type
            {
                EmitterClearEvent,
                EmitterMoveEventToEventReference,
                EmitterMoveEventOverrideToEventReference,
                PlayableClearEventName,
                PlayableMoveEventNameToEventReference,
                GenericRemoveEventRefField,
                GenericRemoveEmptyEventRefField,
                GenericMoveEventRefFieldToEventReferenceField,
                GenericAddMigrationTarget,
                GenericUpdateEventReferencePath,
                GenericUpdateEventReferenceGuid,
                GenericFixMigrationTargetConflict,

                Count
            }

            public bool HasExecuted { get; private set; }

            // Suppress warnings about using the obsolete StudioEventEmitter.Event and FMODEventPlayable.eventName fields
#pragma warning disable 0618
            public static Task ClearEvent(StudioEventEmitter emitter)
            {
                return new Task()
                {
                    type = Type.EmitterClearEvent,
                    Data = new string[] { emitter.Event },
                };
            }

#if UNITY_TIMELINE_EXIST
            public static Task ClearEventName(FMODEventPlayable playable)
            {
                return new Task()
                {
                    type = Type.PlayableClearEventName,
                    Data = new string[] { playable.eventName },
                };
            }
#endif

            public static Task MoveEventToEventReference(StudioEventEmitter emitter)
            {
                return new Task()
                {
                    type = Type.EmitterMoveEventToEventReference,
                    Data = new string[] { emitter.Event },
                };
            }


#if UNITY_TIMELINE_EXIST
            public static Task MoveEventNameToEventReference(FMODEventPlayable playable)
            {
                return new Task()
                {
                    type = Type.PlayableMoveEventNameToEventReference,
                    Data = new string[] { playable.eventName },
                };
            }
#endif

            public static Task MoveEventOverrideToEventReference(StudioEventEmitter emitter)
            {
                return new Task()
                {
                    type = Type.EmitterMoveEventOverrideToEventReference,
                    Data = new string[] { emitter.Event },
                };
            }
#pragma warning restore 0618

            public static Task RemoveEventRefField(string subObjectPath, string value, string fieldName, string targetType)
            {
                return new Task()
                {
                    type = Type.GenericRemoveEventRefField,
                    Data = new string[] { subObjectPath, value, fieldName, targetType },
                };
            }

            public static Task RemoveEmptyEventRefField(string subObjectPath, string fieldName, string targetType)
            {
                return new Task()
                {
                    type = Type.GenericRemoveEmptyEventRefField,
                    Data = new string[] { subObjectPath, fieldName, targetType },
                };
            }

            public static Task MoveEventRefFieldToEventReferenceField(
                string subObjectPath, string value, string oldFieldName, string newFieldName)
            {
                return new Task()
                {
                    type = Type.GenericMoveEventRefFieldToEventReferenceField,
                    Data = new string[] { subObjectPath, value, oldFieldName, newFieldName },
                };
            }

            public static Task AddMigrationTarget(string subObjectPath, string value, string fieldName, string targetType,
                string targetName = null)
            {
                return new Task()
                {
                    type = Type.GenericAddMigrationTarget,
                    Data = new string[] { subObjectPath, value, fieldName, targetType, targetName },
                };
            }

            public static Task UpdateEventReferencePath(string subObjectPath, string fieldName,
                string oldPath, string newPath, FMOD.GUID guid)
            {
                return new Task()
                {
                    type = Type.GenericUpdateEventReferencePath,
                    Data = new string[] { subObjectPath, fieldName, oldPath, newPath, guid.ToString() },
                };
            }

            public static Task UpdateEventReferenceGuid(string subObjectPath, string fieldName,
                FMOD.GUID oldGuid, FMOD.GUID newGuid, string path)
            {
                return new Task()
                {
                    type = Type.GenericUpdateEventReferenceGuid,
                    Data = new string[] { subObjectPath, fieldName, oldGuid.ToString(), newGuid.ToString(), path },
                };
            }

            public static Task FixMigrationTargetConflict(string subObjectPath, string targetType,
                IEnumerable<string> fieldNames)
            {
                return new Task()
                {
                    type = Type.GenericFixMigrationTargetConflict,
                    Data = (new string[] { subObjectPath, targetType }).Concat(fieldNames).ToArray(),
                };
            }

            private struct Delegates
            {
                public DescriptionDelegate Description;
                public ManualInstructionsDelegate ManualInstructions;
                public IsValidDelegate IsValid;
                public ExecuteDelegate Execute;
            }

            private static void Implement(Type type,
                DescriptionDelegate Description,
                IsValidDelegate IsValid,
                ExecuteDelegate Execute,
                ManualInstructionsDelegate ManualInstructions = null)
            {
                Implementations[(int)type] = new Delegates() {
                    Description = Description,
                    IsValid = IsValid,
                    Execute = Execute,
                    ManualInstructions = ManualInstructions,
                };
            }

            private Delegates GetDelegates()
            {
                return Implementations[(int)type];
            }

            static Task()
            {
                Implementations = new Delegates[(int)Type.Count];

                // Suppress warnings about using the obsolete StudioEventEmitter.Event
                // and FMODEventPlayable.eventName fields
#pragma warning disable 0618

                Implement(Type.EmitterClearEvent,
                    Description: (data) => {
                        return string.Format(L10n.Tr("Clear <b>'{0}'</b> from the <b>{1}</b> field"), data[0], EmitterEventField);
                    },
                    IsValid: (data, target) => {
                        StudioEventEmitter emitter = target as StudioEventEmitter;
                        return emitter != null && emitter.Event == data[0] && !emitter.EventReference.IsNull;
                    },
                    Execute: (data, target) => {
                        StudioEventEmitter emitter = target as StudioEventEmitter;

                        emitter.Event = string.Empty;
                        EditorUtility.SetDirty(emitter);
                    }
                );
                Implement(Type.EmitterMoveEventToEventReference,
                    Description: (data) => {
                        return string.Format(L10n.Tr("Move <b>'{0}'</b> from <b>{1}</b> to <b>{2}</b>"),
                            data[0], EmitterEventField, EmitterEventReferenceField);
                    },
                    IsValid: (data, target) => {
                        StudioEventEmitter emitter = target as StudioEventEmitter;
                        return emitter != null && emitter.Event == data[0] && emitter.EventReference.IsNull;
                    },
                    Execute: (data, target) => {
                        StudioEventEmitter emitter = target as StudioEventEmitter;

                        emitter.EventReference.Path = emitter.Event;
                        emitter.Event = string.Empty;

                        EditorEventRef eventRef = EventManager.EventFromPath(emitter.EventReference.Path);

                        if (eventRef != null)
                        {
                            emitter.EventReference.Guid = eventRef.Guid;
                        }

                        EditorUtility.SetDirty(emitter);
                    }
                );
                Implement(Type.EmitterMoveEventOverrideToEventReference,
                    Description: (data) => {
                        return string.Format(L10n.Tr("Move prefab override <b>'{0}'</b> from <b>{1}</b> to <b>{2}</b>"),
                            data[0], EmitterEventField, EmitterEventReferenceField);
                    },
                    IsValid: (data, target) => {
                        if (!PrefabUtility.IsPartOfPrefabInstance(target))
                        {
                            return false;
                        }

                        StudioEventEmitter emitter = target as StudioEventEmitter;

                        if (emitter == null)
                        {
                            return false;
                        }

                        StudioEventEmitter sourceEmitter = PrefabUtility.GetCorrespondingObjectFromSource(emitter);

                        if (sourceEmitter == null)
                        {
                            return false;
                        }

                        PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(emitter);
                        PropertyModification eventOverride = modifications.FirstOrDefault(
                            m => m.target == sourceEmitter && m.propertyPath == "Event");

                        if (eventOverride == null || eventOverride.value != data[0])
                        {
                            return false;
                        }

                        bool hasEventReferenceOverride = modifications.Any(
                            m => m.target == sourceEmitter && m.propertyPath.StartsWith("EventReference"));

                        if (hasEventReferenceOverride)
                        {
                            return false;
                        }

                        return true;
                    },
                    Execute: (data, target) => {
                        StudioEventEmitter emitter = target as StudioEventEmitter;

                        string path = emitter.Event;

                        // Clear the Event override
                        StudioEventEmitter sourceEmitter = PrefabUtility.GetCorrespondingObjectFromSource(emitter);
                        PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(emitter);

                        modifications = modifications
                            .Where(m => !(m.target == sourceEmitter && m.propertyPath == "Event"))
                            .ToArray();

                        PrefabUtility.SetPropertyModifications(emitter, modifications);

                        // Set the EventReference override
                        emitter.EventReference.Path = path;

                        EditorEventRef eventRef = EventManager.EventFromPath(path);

                        if (eventRef != null)
                        {
                            emitter.EventReference.Guid = eventRef.Guid;
                        }

                        EditorUtility.SetDirty(emitter);
                    }
                );

#if UNITY_TIMELINE_EXIST
                Implement(Type.PlayableClearEventName,
                    Description: (data) => {
                        return string.Format(L10n.Tr("Clear <b>'{0}'</b> from the <b>{1}</b> field"), data[0], PlayableEventNameField);
                    },
                    IsValid: (data, target) => {
                        FMODEventPlayable playable = target as FMODEventPlayable;
                        return playable != null && playable.eventName == data[0] && !playable.EventReference.IsNull;
                    },
                    Execute: (data, target) => {
                        FMODEventPlayable playable = target as FMODEventPlayable;

                        playable.eventName = string.Empty;
                        EditorUtility.SetDirty(playable);
                    }
                );
                Implement(Type.PlayableMoveEventNameToEventReference,
                    Description: (data) => {
                        return string.Format(L10n.Tr("Move <b>'{0}'</b> from <b>{1}</b> to <b>{2}</b>"),
                            data[0], PlayableEventNameField, PlayableEventReferenceField);
                    },
                    IsValid: (data, target) => {
                        FMODEventPlayable playable = target as FMODEventPlayable;
                        return playable != null && playable.eventName == data[0] && playable.EventReference.IsNull;
                    },
                    Execute: (data, target) => {
                        FMODEventPlayable playable = target as FMODEventPlayable;

                        playable.EventReference.Path = playable.eventName;
                        playable.eventName = string.Empty;

                        EditorEventRef eventRef = EventManager.EventFromPath(playable.EventReference.Path);

                        if (eventRef != null)
                        {
                            playable.EventReference.Guid = eventRef.Guid;
                        }

                        EditorUtility.SetDirty(playable);
                    }
                );
#endif
                Implement(Type.GenericRemoveEventRefField,
                    Description: (data) => {
                        return string.Format(L10n.Tr("Remove field <b>{0}</b>"), FieldPath(data[0], data[2]));
                    },
                    ManualInstructions: (data, component) => {
                        string subObjectPath = data[0];
                        string value = data[1];
                        string fieldName = data[2];
                        string targetType = data[3];

                        string fieldPath = FieldPath(subObjectPath, fieldName);

                        return string.Format(
                            L10n.Tr("The {0} field on component {1} has value '{2}', but the corresponding EventReference field already has a value.\n* Ensure no other instances of the {3} type are using the {4} field\n* Edit the definition of the {3} type and remove the {4} field"),
                            fieldPath, component.Type, value, targetType, fieldName);
                    },
                    IsValid: (data, rootObject) => {
                        object target = FindSubObject(rootObject, data[0]);

                        System.Type targetType = target.GetType();
                        FieldInfo field = targetType.GetField(data[2]);

                        return field != null && IsEventRef(field) && (field.GetValue(target) as string) == data[1];
                    },
                    Execute: null
                );
                Implement(Type.GenericRemoveEmptyEventRefField,
                    Description: (data) => {
                        return string.Format(L10n.Tr("Remove empty field <b>{0}</b>"), FieldPath(data[0], data[1]));
                    },
                    ManualInstructions: (data, component) => {
                        string subObjectPath = data[0];
                        string fieldName = data[1];
                        string targetType = data[2];

                        string fieldPath = FieldPath(subObjectPath, fieldName);

                        return string.Format(
                            L10n.Tr("The {0} field on component {1} is empty.\n* Ensure no other instances of the {2} type are using the {3} field\n* Edit the definition of the {2} type and remove the {3} field"),
                            fieldPath, component.Type, targetType, fieldName);
                    },
                    IsValid: (data, rootObject) => {
                        object target = FindSubObject(rootObject, data[0]);

                        System.Type targetType = target.GetType();
                        FieldInfo field = targetType.GetField(data[1]);

                        return field != null && IsEventRef(field)
                            && string.IsNullOrEmpty(field.GetValue(target) as string);
                    },
                    Execute: null
                );
                Implement(Type.GenericMoveEventRefFieldToEventReferenceField,
                    Description: (data) => {
                        string subObjectPath = data[0];
                        string value = data[1];
                        string oldFieldPath = FieldPath(subObjectPath, data[2]);
                        string newFieldPath = FieldPath(subObjectPath, data[3]);

                        return string.Format(L10n.Tr("Move <b>'{0}'</b> from <b>{1}</b> to <b>{2}</b>"),
                            value, oldFieldPath, newFieldPath);
                    },
                    IsValid: (data, rootObject) => {
                        string subObjectPath = data[0];
                        string value = data[1];
                        string oldFieldName = data[2];
                        string newFieldName = data[3];

                        object target = FindSubObject(rootObject, subObjectPath);
                        System.Type targetType = target.GetType();

                        FieldInfo oldField = targetType.GetField(oldFieldName, DefaultBindingFlags);
                        FieldInfo newField = targetType.GetField(newFieldName, DefaultBindingFlags);

                        if (oldField == null || newField == null
                            || !IsEventRef(oldField)
                            || newField.FieldType != typeof(EventReference))
                        {
                            return false;
                        }

                        string oldValue = oldField.GetValue(target) as string;
                        EventReference newValue = (EventReference)newField.GetValue(target);

                        return oldValue == value && newValue.IsNull;
                    },
                    Execute: (data, rootObject) => {
                        string subObjectPath = data[0];
                        string path = data[1];
                        string oldFieldName = data[2];
                        string newFieldName = data[3];

                        object target = FindSubObject(rootObject, subObjectPath);
                        System.Type type = target.GetType();

                        FieldInfo oldField = type.GetField(oldFieldName, DefaultBindingFlags);
                        FieldInfo newField = type.GetField(newFieldName, DefaultBindingFlags);

                        EventReference eventReference = new EventReference() { Path = path };

                        EditorEventRef eventRef = EventManager.EventFromPath(path);

                        if (eventRef != null)
                        {
                            eventReference.Guid = eventRef.Guid;
                        }

                        oldField.SetValue(target, string.Empty);
                        newField.SetValue(target, eventReference);

                        EditorUtility.SetDirty(rootObject);
                    }
                );
                Implement(Type.GenericAddMigrationTarget,
                    Description: (data) => {
                        string value = data[1];
                        string fieldPath = FieldPath(data[0], data[2]);
                        string targetName = data[4];

                        if (!string.IsNullOrEmpty(targetName))
                        {
                            return string.Format(
                                L10n.Tr("Add an <b>FMODUnity.EventReference</b> field named <b>{0}</b> to hold <b>'{1}'</b> from <b>{2}</b>"),
                                targetName, value, fieldPath);
                        }
                        else
                        {
                            return string.Format(L10n.Tr("Add an <b>FMODUnity.EventReference</b> field to hold <b>'{0}'</b> from <b>{1}</b>"),
                                value, fieldPath);
                        }
                    },
                    ManualInstructions: (data, component) => {
                        string fieldName = data[2];
                        string targetType = data[3];
                        string targetName = data[4];
                        string fieldPath = FieldPath(data[0], fieldName);

                        string script;

                        if (targetType != null)
                        {
                            script = string.Format(L10n.Tr("the definition of the {0} type"), targetType);
                        }
                        else
                        {
                            script = component.ScriptPath;
                        }

                        if (!string.IsNullOrEmpty(targetName))
                        {
                            return string.Format(
                                L10n.Tr("The {0} field on component {1} has an [FMODUnity.EventRef(MigrateTo=\"{2}\")] attribute, but the {2} field doesn't exist.\n* Edit {3} and add an FMODUnity.EventReference field named {2}:\n    public FMODUnity.EventReference {2};\n* Re-scan your project"),
                                fieldPath, component.Type, targetName, script);
                        }
                        else
                        {
                            return string.Format(
                                L10n.Tr("The {0} field on component {1} has an [FMODUnity.EventRef] attribute with no migration target specified.\n* Edit {2} and add an FMODUnity.EventReference field:\n    public FMODUnity.EventReference <fieldname>;\n* Change the [FMODUnity.EventRef] attribute on the {3} field to:\n    [FMODUnity.EventRef(MigrateTo=\"<fieldname>\")]\n* Re-scan your project."),
                                fieldPath, component.Type, script, fieldName);
                        }
                    },
                    IsValid: (data, rootObject) => {
                        string value = data[1];
                        string oldFieldName = data[2];

                        object target = FindSubObject(rootObject, data[0]);

                        System.Type targetType = target.GetType();
                        FieldInfo oldField = targetType.GetField(oldFieldName, DefaultBindingFlags);

                        return oldField != null && IsEventRef(oldField)
                            && (oldField.GetValue(target) as string) == value;
                    },
                    Execute: null
                );
                Implement(Type.GenericUpdateEventReferencePath,
                    Description: (data) => {
                        return string.Format(
                            L10n.Tr("Change the path on field <b>{0}</b> from <b>'{1}'</b> to <b>'{2}'</b> (to match GUID <b>{3}</b>)"),
                            FieldPath(data[0], data[1]), data[2], data[3], data[4]);
                    },
                    IsValid: (data, rootObject) => {
                        object target = FindSubObject(rootObject, data[0]);

                        System.Type targetType = target.GetType();
                        FieldInfo field = targetType.GetField(data[1], DefaultBindingFlags);

                        if (field == null || field.FieldType != typeof(EventReference))
                        {
                            return false;
                        }

                        EventReference value = (EventReference)field.GetValue(target);

                        return value.Path == data[2] && value.Guid.ToString() == data[4];
                    },
                    Execute: (data, rootObject) => {
                        object target = FindSubObject(rootObject, data[0]);

                        System.Type targetType = target.GetType();
                        FieldInfo field = targetType.GetField(data[1], DefaultBindingFlags);

                        EventReference value = (EventReference)field.GetValue(target);
                        value.Path = data[3];

                        field.SetValue(target, value);

                        EditorUtility.SetDirty(rootObject);
                    }
                );
                Implement(Type.GenericUpdateEventReferenceGuid,
                    Description: (data) => {
                        return string.Format(
                            L10n.Tr("Change the GUID on field <b>{0}</b> from <b>{1}</b> to <b>{2}</b> (to match path <b>'{3}'</b>)"),
                            FieldPath(data[0], data[1]), data[2], data[3], data[4]);
                    },
                    IsValid: (data, rootObject) => {
                        object target = FindSubObject(rootObject, data[0]);

                        System.Type targetType = target.GetType();
                        FieldInfo field = targetType.GetField(data[1], DefaultBindingFlags);

                        if (field == null || field.FieldType != typeof(EventReference))
                        {
                            return false;
                        }

                        EventReference value = (EventReference)field.GetValue(target);

                        return value.Guid.ToString() == data[2] && value.Path == data[4];
                    },
                    Execute: (data, rootObject) => {
                        object target = FindSubObject(rootObject, data[0]);

                        System.Type targetType = target.GetType();
                        FieldInfo field = targetType.GetField(data[1], DefaultBindingFlags);

                        EventReference value = (EventReference)field.GetValue(target);
                        value.Guid = FMOD.GUID.Parse(data[3]);

                        field.SetValue(target, value);

                        EditorUtility.SetDirty(rootObject);
                    }
                );
                Implement(Type.GenericFixMigrationTargetConflict,
                    Description: (data) => {
                        string subObjectPath = data[0];
                        IEnumerable<string> fieldPaths = data.Skip(2).Select(field => FieldPath(subObjectPath, field));

                        return string.Format(L10n.Tr("Fix conflicting migration targets on fields <b>{0}</b>"),
                            EditorUtils.SeriesString("</b>, <b>", L10n.Tr("</b> and <b>"), fieldPaths));
                    },
                    ManualInstructions: (data, component) => {
                        return string.Format(
                            L10n.Tr("Fields {0} on the {1} type have [FMODUnity.EventRef] attributes with the same MigrateTo value.\n* Edit the definition of the {1} type and make sure all [FMODUnity.EventRef] attributes have different MigrateTo values\n* Re-scan your project"),
                            EditorUtils.SeriesString(", ", L10n.Tr(" and "), data.Skip(2)), data[1]);
                    },
                    IsValid: (data, target) => {
                        return true;
                    },
                    Execute: null
                );

#pragma warning restore 0618
            }

            public override string ToString()
            {
                return GetDelegates().Description(Data);
            }

            public string PlainDescription()
            {
                return Regex.Replace(ToString(), "</?b>", string.Empty);
            }

            public string ManualInstructions(Component component)
            {
                Delegates delegates = GetDelegates();

                if (delegates.ManualInstructions != null)
                {
                    return delegates.ManualInstructions(Data, component);
                }
                else
                {
                    return null;
                }
            }

            public bool CanExecute()
            {
                return Enabled && !IsManual() && !HasExecuted;
            }

            public bool IsManual()
            {
                return GetDelegates().Execute == null;
            }

            public bool IsValid(UnityEngine.Object target)
            {
                return GetDelegates().IsValid(Data, target);
            }

            public bool Execute(UnityEngine.Object target)
            {
                if (IsValid(target))
                {
                    Delegates delegates = GetDelegates();

                    if (delegates.Execute != null)
                    {
                        delegates.Execute(Data, target);
                        HasExecuted = true;
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private static string FieldPath(string subObjectPath, string fieldName)
        {
            if (subObjectPath != null)
            {
                return string.Format("{0}.{1}", subObjectPath, fieldName);
            }
            else
            {
                return fieldName;
            }
        }

        private static string FieldPath(string subObjectPath, string fieldName, int index)
        {
            if (subObjectPath != null)
            {
                return string.Format("{0}.{1}[{2}]", subObjectPath, fieldName, index);
            }
            else
            {
                return string.Format("{0}[{1}]", fieldName, index);
            }
        }

        private static object FindSubObject(object o, string path)
        {
            if (path == null)
            {
                return o;
            }

            object result = o;

            foreach (string pathElement in path.Split('.'))
            {
                Type type = result.GetType();

                Regex regex = new Regex(@"(\w+)\[(\d+)\]$");
                Match match = regex.Match(pathElement);
                int index = -1;
                string fieldName = pathElement;

                if (match.Success)
                {
                    fieldName = match.Groups[1].Value;
                    index = int.Parse(match.Groups[2].Value);
                }

                FieldInfo field = type.GetField(fieldName, DefaultBindingFlags);

                if (field == null)
                {
                    return null;
                }

                result = field.GetValue(result);

                if (index >= 0)
                {
                    System.Collections.IEnumerable enumerable = result as System.Collections.IEnumerable;

                    result = null;

                    if (enumerable != null)
                    {
                        int i = 0;

                        foreach (object obj in enumerable)
                        {
                            if (index == i)
                            {
                                result = obj;
                                break;
                            }
                            i++;
                        }
                    }
                }

                if (result == null)
                {
                    return null;
                }
            }

            return result;
        }

        private void ExecuteTask(Task task, SavePolicy savePolicy)
        {
            Asset asset = assets[task.AssetIndex];

            if (asset.Type == AssetType.ScriptableObject)
            {
                ExecuteScriptableObjectTask(task, savePolicy);
            }
            else
            {
                ExecuteGameObjectTask(task, savePolicy);
            }
        }

        private void ExecuteScriptableObjectTask(Task task, SavePolicy savePolicy)
        {
            Asset asset = assets[task.AssetIndex];
            Component component = components[task.ComponentIndex];

            IEnumerable<ScriptableObject> scriptableObjects =
                AssetDatabase.LoadAllAssetsAtPath(asset.Path).OfType<ScriptableObject>();

            foreach (ScriptableObject scriptableObject in scriptableObjects)
            {
                if (scriptableObject.GetType().Name == component.Type)
                {
                    if (task.Execute(scriptableObject))
                    {
                        break;
                    }
                }
            }
        }

        private void ExecuteGameObjectTask(Task task, SavePolicy savePolicy)
        {
            GameObject gameObject = LoadTargetGameObject(task, savePolicy);

            if (gameObject == null)
            {
                return;
            }

            Selection.activeGameObject = gameObject;
            EditorGUIUtility.PingObject(gameObject);

            Component component = components[task.ComponentIndex];

            foreach (MonoBehaviour behaviour in gameObject.GetComponents<MonoBehaviour>())
            {
                if (behaviour.GetType().Name == component.Type)
                {
                    if (task.Execute(behaviour))
                    {
                        break;
                    }
                }
            }
        }

        private enum SavePolicy
        {
            AskToSave,
            AutoSave,
        }

        private GameObject LoadTargetGameObject(Task task, SavePolicy savePolicy)
        {
            Asset asset = assets[task.AssetIndex];
            Component component = components[task.ComponentIndex];

            if (IsPrefab(asset.Type))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset.Path);

                if (prefab == null)
                {
                    return null;
                }

                if (!AssetDatabase.OpenAsset(prefab))
                {
                    return null;
                }

                return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(component.GameObjectID) as GameObject;
            }
            else if (asset.Type == AssetType.Scene)
            {
                Scene scene = SceneManager.GetSceneByPath(asset.Path);

                if (!scene.IsValid())
                {
                    if (savePolicy == SavePolicy.AskToSave)
                    {
                        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            return null;
                        }
                    }
                    else if (savePolicy == SavePolicy.AutoSave)
                    {
                        EditorSceneManager.SaveOpenScenes();
                    }
                    else
                    {
                        throw new ArgumentException("Unrecognized SavePolicy: " + savePolicy, "savePolicy");
                    }

                    scene = EditorSceneManager.OpenScene(asset.Path, OpenSceneMode.Single);

                    if (!scene.IsValid())
                    {
                        return null;
                    }
                }

                return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(component.GameObjectID) as GameObject;
            }
            else
            {
                return null;
            }
        }

        private int AddAsset(AssetType type, string path)
        {
            Asset asset = new Asset() {
                Type = type,
                Path = path,
            };

            assets.Add(asset);

            return assets.Count - 1;
        }

        private int AddComponent(MonoBehaviour behaviour, GameObject root)
        {
            MonoScript script = MonoScript.FromMonoBehaviour(behaviour);

            Component component = new Component() {
                GameObjectID = GlobalObjectId.GetGlobalObjectIdSlow(behaviour.gameObject),
                Type = behaviour.GetType().Name,
                Path = EditorUtils.GameObjectPath(behaviour, root),
                ScriptPath = AssetDatabase.GetAssetPath(script),
            };

            components.Add(component);

            return components.Count - 1;
        }

        private int AddComponent(ScriptableObject scriptableObject)
        {
            MonoScript script = MonoScript.FromScriptableObject(scriptableObject);

            Component component = new Component() {
                Type = scriptableObject.GetType().Name,
                ScriptPath = AssetDatabase.GetAssetPath(script),
            };

            components.Add(component);

            return components.Count - 1;
        }

        private void UpdateExecutableTaskCount()
        {
            executableTaskCount = tasks.Count(t => t.CanExecute());
        }

        private void AddTask(Task task)
        {
            tasks.Add(task);
            UpdateExecutableTaskCount();
            taskView.Reload();
            taskView.ExpandAll();
        }

        private void UpdateProcessing()
        {
            if (processingState != null)
            {
                if (processingState.MoveNext())
                {
                    SetStatus(processingState.Current);
                }
                else
                {
                    StopProcessing(true);
                }

                Repaint();
            }
        }

        private void OnEnable()
        {
            taskView = new TaskView(taskViewState, tasks, assets, components);
            taskView.Reload();
            taskView.taskSelected += OnTaskSelected;
            taskView.taskDoubleClicked += OnTaskDoubleClicked;
            taskView.taskEnableStateChanged += OnTaskEnableStateChanged;
            taskView.assetEnableStateChanged += ApplyAssetEnableStateToTasks;

            EditorApplication.update += UpdateProcessing;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateProcessing;
        }

        private void OnTaskSelected(Task task)
        {
            selectedTask = task;
        }

        private void OnTaskDoubleClicked(Task task)
        {
            Asset asset = assets[task.AssetIndex];

            if (asset.Type == AssetType.ScriptableObject)
            {
                UnityEngine.Object target = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.Path);

                if (target == null)
                {
                    return;
                }

                if (!AssetDatabase.OpenAsset(target))
                {
                    return;
                }

                Component component = components[task.ComponentIndex];

                IEnumerable<ScriptableObject> scriptableObjects =
                    AssetDatabase.LoadAllAssetsAtPath(asset.Path).OfType<ScriptableObject>();

                foreach (ScriptableObject scriptableObject in scriptableObjects)
                {
                    if (scriptableObject.GetType().Name == component.Type
                        && task.IsValid(scriptableObject))
                    {
                        Selection.activeObject = scriptableObject;
                    }
                }
            }
            else
            {
                GameObject gameObject = LoadTargetGameObject(task, SavePolicy.AskToSave);

                if (gameObject == null)
                {
                    return;
                }

                Selection.activeGameObject = gameObject;
                EditorGUIUtility.PingObject(gameObject);
            }
        }

        private void OnTaskEnableStateChanged(Task task)
        {
            UpdateAssetEnableState(task.AssetIndex);
            UpdateExecutableTaskCount();
        }

        private void UpdateAssetEnableState(int assetIndex)
        {
            Asset asset = assets[assetIndex];

            asset.EnableState = tasks
                .Where(t => t.AssetIndex == assetIndex)
                .Select(t => t.Enabled ? EnableState.Enabled : EnableState.Disabled)
                .Aggregate((current, next) => (current == next) ? current : EnableState.Mixed);
        }

        private void ApplyAssetEnableStateToTasks(Asset asset)
        {
            int assetIndex = assets.IndexOf(asset);

            foreach (Task task in tasks.Where(t => t.AssetIndex == assetIndex))
            {
                task.Enabled = (asset.EnableState == EnableState.Enabled);
            }

            UpdateExecutableTaskCount();
        }

        private class Styles
        {
            public static GUIStyle RichText;
            public static GUIStyle RichTextBox;
            public static GUIStyle TreeViewRichText;

            private static bool Initialized = false;

            public static void Affirm()
            {
                if (!Initialized)
                {
                    Initialized = true;

                    RichText = new GUIStyle(GUI.skin.label) { richText = true };
                    RichTextBox = new GUIStyle(EditorStyles.helpBox) { richText = true };
                    TreeViewRichText = new GUIStyle(TreeView.DefaultStyles.label) { richText = true };
                }
            }
        }

        private class Icons
        {
            public static Texture2D Scene;
            public static Texture2D Prefab;
            public static Texture2D PrefabModel;
            public static Texture2D PrefabVariant;
            public static Texture2D ScriptableObject;
            public static Texture2D GameObject;

            private static bool Initialized = false;

            public static void Affirm()
            {
                if (!Initialized)
                {
                    Initialized = true;

                    Scene = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
                    Prefab = EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
                    PrefabModel = EditorGUIUtility.IconContent("PrefabModel Icon").image as Texture2D;
                    PrefabVariant = EditorGUIUtility.IconContent("PrefabVariant Icon").image as Texture2D;
                    ScriptableObject = EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D;
                    GameObject = EditorGUIUtility.IconContent("GameObject Icon").image as Texture2D;
                }
            }

            public static Texture2D GetAssetIcon(AssetType type)
            {
                Affirm();

                if (type == AssetType.Scene)
                {
                    return Scene;
                }
                else if (type == AssetType.Prefab)
                {
                    return Prefab;
                }
                else if (type == AssetType.PrefabModel)
                {
                    return PrefabModel;
                }
                else if (type == AssetType.PrefabVariant)
                {
                    return PrefabVariant;
                }
                else if (type == AssetType.ScriptableObject)
                {
                    return ScriptableObject;
                }
                else
                {
                    throw new ArgumentException("Unrecognized AssetType: " + type, "type");
                }
            }

            public static Texture2D GetComponentIcon(Component component)
            {
                return AssetDatabase.GetCachedIcon(component.ScriptPath) as Texture2D;
            }
        }

        private void SetStatus(string text)
        {
            status = new GUIContent(text, EditorGUIUtility.IconContent("console.infoicon.sml").image);
        }

        private void OnGUI()
        {
            Styles.Affirm();

            float buttonHeight = EditorGUIUtility.singleLineHeight * 2;

            // Task List
            using (var scope = new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
            {
                taskView.DrawLayout(scope.rect);
            }

            // Selected Task
            if (selectedTask != null)
            {
                Asset asset = assets[selectedTask.AssetIndex];
                Component component = components[selectedTask.ComponentIndex];

                DrawSelectableLabel(selectedTask.PlainDescription(), EditorStyles.wordWrappedLabel);

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField(AssetContent,
                        new GUIContent(asset.Path, Icons.GetAssetIcon(asset.Type)));
                    EditorGUILayout.LabelField(ComponentTypeContent,
                        new GUIContent(component.Type, Icons.GetComponentIcon(component)));

                    if (!string.IsNullOrEmpty(component.Path))
                    {
                        EditorGUILayout.LabelField(GameObjectContent, new GUIContent(component.Path, Icons.GameObject));
                    }

                    if (selectedTask.IsManual())
                    {
                        Rect buttonsRect = EditorGUILayout.GetControlRect(false, buttonHeight);
                        buttonsRect = EditorGUI.IndentedRect(buttonsRect);

                        GUIContent openScriptContent = new GUIContent(L10n.Tr("Open ") + component.ScriptPath);

                        Rect openScriptRect = buttonsRect;
                        openScriptRect.width = GUI.skin.button.CalcSize(openScriptContent).x;

                        if (GUI.Button(openScriptRect, openScriptContent))
                        {
                            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(component.ScriptPath);
                            AssetDatabase.OpenAsset(script);
                        }

                        GUIContent viewDocumentationContent = new GUIContent(L10n.Tr("View Documentation"));

                        Rect viewDocumentationRect = buttonsRect;
                        viewDocumentationRect.x = openScriptRect.xMax + GUI.skin.button.margin.left;
                        viewDocumentationRect.width = GUI.skin.button.CalcSize(viewDocumentationContent).x;

                        if (GUI.Button(viewDocumentationRect, viewDocumentationContent))
                        {
                            EditorUtils.OpenOnlineDocumentation("unity", "tools", "manual-tasks");
                        }

                        using (var scope = new EditorGUILayout.ScrollViewScope(manualDescriptionScrollPosition, GUILayout.Height(100)))
                        {
                            manualDescriptionScrollPosition = scope.scrollPosition;

                            DrawSelectableLabel(selectedTask.ManualInstructions(component), EditorStyles.wordWrappedLabel);
                        }
                    }
                    else
                    {
                        GUIContent buttonContent = new GUIContent(L10n.Tr("Execute"));

                        Rect buttonRect = EditorGUILayout.GetControlRect(false, buttonHeight);
                        buttonRect.width = EditorGUIUtility.labelWidth;
                        buttonRect = EditorGUI.IndentedRect(buttonRect);

                        if (GUI.Button(buttonRect, buttonContent))
                        {
                            ExecuteTask(selectedTask, SavePolicy.AskToSave);
                        }
                    }
                }
            }

            // Status
            if (IsProcessing)
            {
                DrawProgressBar(L10n.Tr("Prefabs"), prefabProgress);
                DrawProgressBar(L10n.Tr("ScriptableObjects"), scriptableObjectProgress);
                DrawProgressBar(L10n.Tr("Scenes"), sceneProgress);
            }

            GUILayout.Label(status, Styles.RichTextBox);

            // Buttons
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(L10n.Tr("Cancel"), GUILayout.Height(buttonHeight)))
                {
                    Cancel();
                }

                using (new EditorGUI.DisabledScope(IsProcessing))
                {
                    if (GUILayout.Button(L10n.Tr("Scan"), GUILayout.Height(buttonHeight)))
                    {
                        BeginSearching();
                    }

                    using (new EditorGUI.DisabledScope(executableTaskCount == 0))
                    {
                        if (GUILayout.Button(ExecuteButtonText(), GUILayout.Height(buttonHeight)))
                        {
                            BeginExecuting();
                        }
                    }
                }
            }

            if (focusedWindow == this
                && Event.current.type == EventType.KeyDown
                && Event.current.keyCode == KeyCode.Escape)
            {
                Cancel();
                Event.current.Use();
            }
        }

        private static void DrawProgressBar(string label, SearchProgress progress)
        {
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.ProgressBar(rect, progress.Fraction(), label);
        }

        private static void DrawSelectableLabel(string text, GUIStyle style)
        {
            float height = style.CalcHeight(new GUIContent(text), EditorGUIUtility.currentViewWidth);

            EditorGUILayout.SelectableLabel(text, style, GUILayout.Height(height));
        }

        private class TaskView : TreeView
        {
            private List<Task> tasks;
            private List<Asset> assets;
            private List<Component> components;

            public delegate void TaskEventHandler(Task task);

            public event TaskEventHandler taskSelected;
            public event TaskEventHandler taskDoubleClicked;
            public event TaskEventHandler taskEnableStateChanged;

            public delegate void AssetEventHandler(Asset asset);

            public event AssetEventHandler assetEnableStateChanged;

            public TaskView(TreeViewState state, List<Task> tasks, List<Asset> assets, List<Component> components)
                : base(state, new MultiColumnHeader(CreateHeaderState()))
            {
                this.tasks = tasks;
                this.assets = assets;
                this.components = components;

                showAlternatingRowBackgrounds = true;
                showBorder = true;

                multiColumnHeader.ResizeToFit();
            }

            public static MultiColumnHeaderState CreateHeaderState()
            {
                MultiColumnHeaderState.Column[] columns = new MultiColumnHeaderState.Column[] {
                    new MultiColumnHeaderState.Column()
                    {
                        headerContent = new GUIContent(L10n.Tr("Target")),
                        width = 225,
                        autoResize = false,
                        allowToggleVisibility = false,
                        canSort = false,
                    },
                    new MultiColumnHeaderState.Column() {
                        headerContent = new GUIContent(L10n.Tr("Task")),
                        autoResize = true,
                        allowToggleVisibility = false,
                        canSort = false,
                    },
                    new MultiColumnHeaderState.Column()
                    {
                        headerContent = new GUIContent(L10n.Tr("Status")),
                        width = 175,
                        autoResize = false,
                        allowToggleVisibility = false,
                        canSort = false,
                    }
                };

                return new MultiColumnHeaderState(columns);
            }

            public void DrawLayout(Rect rect)
            {
                extraSpaceBeforeIconAndLabel = ToggleWidth();

                OnGUI(rect);
            }

            public enum Column
            {
                Asset,
                Task,
                Status,
            }

            private class AssetItem : TreeViewItem
            {
                public Asset asset;
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

                    AssetItem assetItem = null;

                    foreach (Task task in tasks)
                    {
                        Asset asset = assets[task.AssetIndex];

                        if (assetItem == null || assetItem.asset != asset)
                        {
                            assetItem = new AssetItem() {
                                id = index++,
                                asset = asset,
                                displayName = asset.Path,
                                icon = Icons.GetAssetIcon(asset.Type),
                            };

                            root.AddChild(assetItem);
                        }

                        TreeViewItem taskItem = new TaskItem() {
                            id = index++,
                            task = task,
                        };

                        assetItem.AddChild(taskItem);
                    }
                }
                else
                {
                    TreeViewItem item = new TreeViewItem(0);
                    item.displayName = L10n.Tr("No tasks.");

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

            protected override void SingleClickedItem(int id)
            {
                TreeViewItem item = FindItem(id, rootItem);

                if (!(item is TaskItem))
                {
                    SetExpanded(id, !IsExpanded(id));
                }
                else
                {
                    base.SingleClickedItem(id);
                }
            }

            protected override void DoubleClickedItem(int id)
            {
                if (taskDoubleClicked != null)
                {
                    TaskItem item = FindItem(id, rootItem) as TaskItem;

                    if (item == null)
                    {
                        return;
                    }

                    taskDoubleClicked(item.task);
                }
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                TreeViewItem item = args.item;

                if (item is TaskItem)
                {
                    Task task = (item as TaskItem).task;

                    Rect toggleRect = args.rowRect;
                    toggleRect.x = GetContentIndent(item);
                    toggleRect.width = ToggleWidth();

                    TaskToggle(toggleRect, task);

                    for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                    {
                        Rect rect = args.GetCellRect(i);

                        if (i == 0)
                        {
                            rect.xMin = toggleRect.xMax;
                        }

                        CellGUI(rect, task, args.GetColumn(i), args.selected, args.focused);
                    }
                }
                else if (item is AssetItem)
                {
                    base.RowGUI(args);

                    Rect rect = args.rowRect;
                    rect.x = GetContentIndent(item);
                    rect.width = ToggleWidth();

                    AssetToggle(rect, (item as AssetItem).asset);
                }
                else
                {
                    base.RowGUI(args);
                }
            }

            private static float ToggleWidth()
            {
                return GUI.skin.toggle.CalcSize(GUIContent.none).x;
            }

            private void AssetToggle(Rect rect, Asset asset)
            {
                using (var scope = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.showMixedValue = (asset.EnableState == EnableState.Mixed);

                    bool enabled = EditorGUI.Toggle(rect, asset.EnableState == EnableState.Enabled);

                    EditorGUI.showMixedValue = false;

                    if (scope.changed)
                    {
                        asset.EnableState = enabled ? EnableState.Enabled : EnableState.Disabled;

                        if (assetEnableStateChanged != null)
                        {
                            assetEnableStateChanged(asset);
                        }
                    }
                }
            }

            private void TaskToggle(Rect rect, Task task)
            {
                if (!task.IsManual())
                {
                    using (var scope = new EditorGUI.ChangeCheckScope())
                    {
                        task.Enabled = EditorGUI.Toggle(rect, task.Enabled);

                        if (scope.changed && taskEnableStateChanged != null)
                        {
                            taskEnableStateChanged(task);
                        }
                    }
                }
            }

            private void CellGUI(Rect rect, Task task, int columnIndex, bool selected, bool focused)
            {
                Component component = components[task.ComponentIndex];

                switch ((Column)columnIndex)
                {
                    case Column.Asset:
                        if (Event.current.type == EventType.Repaint)
                        {
                            Texture2D typeIcon = Icons.GetComponentIcon(components[task.ComponentIndex]);

                            using (new GUI.GroupScope(rect))
                            {
                                Rect iconRect = new Rect(0, 0, rect.height, rect.height);

                                GUI.DrawTexture(iconRect, typeIcon, ScaleMode.ScaleToFit);

                                GUIContent type = new GUIContent(component.Type);

                                bool hasGameObjectPath = !string.IsNullOrEmpty(component.Path);

                                if (hasGameObjectPath)
                                {
                                    type.text += L10n.Tr(" on");
                                }

                                Rect typeRect = new Rect(iconRect.xMax, 0,
                                    DefaultStyles.label.CalcSize(type).x, rect.height);

                                DefaultGUI.Label(typeRect, type.text, selected, focused);

                                if (hasGameObjectPath)
                                {
                                    iconRect.x = typeRect.xMax;

                                    GUI.DrawTexture(iconRect, Icons.GameObject, ScaleMode.ScaleToFit);

                                    GUIContent gameObject = new GUIContent(component.Path);

                                    Rect gameObjectRect = new Rect(iconRect.xMax, 0,
                                        DefaultStyles.label.CalcSize(gameObject).x, rect.height);

                                    DefaultGUI.Label(gameObjectRect, gameObject.text, selected, focused);
                                }
                            }
                        }

                        break;
                    case Column.Task:
                        if (Event.current.type == EventType.Repaint)
                        {
                            string text = task.ToString();

                            if (task.IsManual())
                            {
                                text = L10n.Tr("Manual task: ") + text;
                            }

                            Styles.TreeViewRichText.Draw(rect, text, false, false, selected, focused);
                        }
                        break;
                    case Column.Status:
                        if (Event.current.type == EventType.Repaint)
                        {
                            if (task.IsManual())
                            {
                                DefaultGUI.Label(rect, L10n.Tr("Manual Changes Required"), selected, focused);
                            }
                            else
                            {
                                DefaultGUI.Label(rect, task.HasExecuted ? L10n.Tr("Complete") : L10n.Tr("Pending"), selected, focused);
                            }
                        }
                        break;
                }
            }
        }
    }
}
