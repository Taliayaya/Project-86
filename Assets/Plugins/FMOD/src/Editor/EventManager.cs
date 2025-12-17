using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using System.Reflection;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace FMODUnity
{
    [InitializeOnLoad]
    public class EventManager : MonoBehaviour
    {
        private const string FMODLabel = "FMOD";

        private const string AssetsFolderName = "Assets";

        private const string CacheAssetName = "FMODStudioCache";
        public static string CacheAssetFullName = EditorUtils.WritableAssetPath(CacheAssetName);
        private static EventCache eventCache;

        private const string StringBankExtension = "strings.bank";
        private const string BankExtension = "bank";

        private static HashSet<string> newBankPaths = new HashSet<string>();

#if UNITY_EDITOR
        [MenuItem("FMOD/Refresh Banks", priority = 1)]
        public static void RefreshBanks()
        {
            string result = UpdateCache();

            if (eventCache != null)
            {
                OnCacheChange();
                if (Settings.Instance.ImportType == ImportType.AssetBundle)
                {
                    UpdateBankStubAssets(EditorUserBuildSettings.activeBuildTarget);
                }
            }

            BankRefresher.HandleBankRefresh(result);
        }
#endif

        private static void ClearCache()
        {
            eventCache.CacheTime = DateTime.MinValue;
            eventCache.EditorBanks.Clear();
            eventCache.EditorEvents.Clear();
            eventCache.EditorEventsDict.Clear();
            eventCache.EditorParameters.Clear();
            eventCache.StringsBanks.Clear();
            eventCache.MasterBanks.Clear();
        }

        private static void AffirmEventCache()
        {
            if (eventCache == null)
            {
                UpdateCache();
            }
        }

        private static string UpdateCache()
        {
            if (eventCache == null)
            {
                eventCache = AssetDatabase.LoadAssetAtPath(CacheAssetFullName, typeof(EventCache)) as EventCache;

                // If new libraries need to be staged, or the staging process is in progress, clear the cache and exit.
                if (StagingSystem.SourceLibsExist)
                {
                    if (eventCache != null)
                    {
                        ClearCache();
                    }
                    return null;
                }

                if (eventCache == null || eventCache.cacheVersion != FMOD.VERSION.number)
                {
                    RuntimeUtils.DebugLog("FMOD: Event cache is missing or in an old format; creating a new instance.");

                    eventCache = ScriptableObject.CreateInstance<EventCache>();
                    eventCache.cacheVersion = FMOD.VERSION.number;

                    Directory.CreateDirectory(Path.GetDirectoryName(CacheAssetFullName));
                    AssetDatabase.CreateAsset(eventCache, CacheAssetFullName);
                }
            }

            var settings = Settings.Instance;
            var editorSettings = EditorSettings.Instance;

            if (string.IsNullOrEmpty(settings.SourceBankPath))
            {
                ClearCache();
                return null;
            }

            EditorUtils.ValidateSource(out bool isValid, out string reason);
            if (!isValid)
            {
                ClearCache();
                return reason;
            }

            string defaultBankFolder = null;

            if (!settings.HasPlatforms)
            {
                defaultBankFolder = settings.SourceBankPath;
            }
            else
            {
                Platform platform = editorSettings.CurrentEditorPlatform;

                if (platform == settings.DefaultPlatform)
                {
                    platform = settings.PlayInEditorPlatform;
                }

                defaultBankFolder = RuntimeUtils.GetCommonPlatformPath(Path.Combine(settings.SourceBankPath, platform.BuildDirectory));
            }

            string[] bankPlatforms = EditorUtils.GetBankPlatforms();
            string[] bankFolders = new string[bankPlatforms.Length];
            for (int i = 0; i < bankPlatforms.Length; i++)
            {
                bankFolders[i] = RuntimeUtils.GetCommonPlatformPath(Path.Combine(settings.SourceBankPath, bankPlatforms[i]));
            }

            if (!Directory.Exists(defaultBankFolder))
            {
                ClearCache();
                return string.Format("Directory {0} doesn't exist. Please confirm project directory in the settings.", defaultBankFolder);
            }

            // Get all banks and set cache time to most recent write time
            List<string> bankFileNames = new List<string>(Directory.GetFiles(defaultBankFolder, "*.bank", SearchOption.AllDirectories));

            if (bankFileNames.Count == 0)
            {
                ClearCache();
                return string.Format("Directory {0} doesn't contain any banks.\nBuild the banks in Studio or check the path in the settings.", defaultBankFolder);
            }

            DateTime lastWriteTime = bankFileNames.Max(fileName => File.GetLastWriteTime(fileName));

            // Exit early if cache is up to date
            if (lastWriteTime == eventCache.CacheTime)
            {
                return null;
            }

            eventCache.CacheTime = lastWriteTime;

            // Remove string banks from list
            bankFileNames.RemoveAll(x => x.Contains(".strings"));

            List<string> stringBanks = new List<string>(0);
            try
            {
                var files = Directory.GetFiles(defaultBankFolder, "*." + StringBankExtension, SearchOption.AllDirectories);
                stringBanks = new List<string>(files);
            }
            catch
            {
            }

            // Strip out OSX resource-fork files that appear on FAT32
            stringBanks.RemoveAll((x) => Path.GetFileName(x).StartsWith("._"));

            if (stringBanks.Count == 0)
            {
                ClearCache();
                return string.Format("Directory {0} doesn't contain any banks.\nBuild the banks in Studio or check the path in the settings.", defaultBankFolder);
            }

            // Stop editor preview so no stale data being held
            EditorUtils.StopAllPreviews();

            bool reloadPreviewBanks = EditorUtils.PreviewBanksLoaded;
            if (reloadPreviewBanks)
            {
                EditorUtils.UnloadPreviewBanks();
            }

            List<string> reducedStringBanksList = new List<string>();
            HashSet<FMOD.GUID> stringBankGuids = new HashSet<FMOD.GUID>();

            foreach (string stringBankPath in stringBanks)
            {
                FMOD.Studio.Bank stringBank;
                EditorUtils.CheckResult(EditorUtils.System.loadBankFile(stringBankPath, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out stringBank));

                if (!stringBank.isValid())
                {
                    return string.Format("{0} is not a valid bank.", stringBankPath);
                }
                else
                {
                    // Unload the strings bank
                    stringBank.unload();
                }

                FMOD.GUID stringBankGuid;
                EditorUtils.CheckResult(stringBank.getID(out stringBankGuid));

                if (!stringBankGuids.Add(stringBankGuid))
                {
                    // If we encounter multiple string banks with the same GUID then only use the first. This handles the scenario where
                    // a Studio project is cloned and extended for DLC with a new master bank name.
                    continue;
                }

                reducedStringBanksList.Add(stringBankPath);
            }

            stringBanks = reducedStringBanksList;

            // Reload the strings banks
            List<FMOD.Studio.Bank> loadedStringsBanks = new List<FMOD.Studio.Bank>();

            bool eventRenameOccurred = false;

            try
            {
                AssetDatabase.StartAssetEditing();

                eventCache.EditorBanks.ForEach((x) => x.Exists = false);
                HashSet<string> masterBankFileNames = new HashSet<string>();

                foreach (string stringBankPath in stringBanks)
                {
                    FMOD.Studio.Bank stringBank;
                    EditorUtils.CheckResult(EditorUtils.System.loadBankFile(stringBankPath, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out stringBank));

                    if (!stringBank.isValid())
                    {
                        ClearCache();
                        return string.Format("{0} is not a valid bank.", stringBankPath);
                    }

                    loadedStringsBanks.Add(stringBank);

                    FileInfo stringBankFileInfo = new FileInfo(stringBankPath);

                    string masterBankFileName = Path.GetFileName(stringBankPath).Replace(StringBankExtension, BankExtension);
                    masterBankFileNames.Add(masterBankFileName);

                    EditorBankRef stringsBankRef = eventCache.StringsBanks.Find(x => RuntimeUtils.GetCommonPlatformPath(stringBankPath) == x.Path);

                    if (stringsBankRef == null)
                    {
                        stringsBankRef = ScriptableObject.CreateInstance<EditorBankRef>();
                        stringsBankRef.FileSizes = new List<EditorBankRef.NameValuePair>();
                        AssetDatabase.AddObjectToAsset(stringsBankRef, eventCache);
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(stringsBankRef));
                        eventCache.EditorBanks.Add(stringsBankRef);
                        eventCache.StringsBanks.Add(stringsBankRef);
                    }

                    stringsBankRef.SetPath(stringBankPath, defaultBankFolder);
                    string studioPath;
                    stringBank.getPath(out studioPath);
                    stringsBankRef.SetStudioPath(studioPath);
                    stringsBankRef.LastModified = stringBankFileInfo.LastWriteTime;
                    stringsBankRef.Exists = true;
                    stringsBankRef.FileSizes.Clear();

                    if (Settings.Instance.HasPlatforms)
                    {
                        for (int i = 0; i < bankPlatforms.Length; i++)
                        {
                            stringsBankRef.FileSizes.Add(new EditorBankRef.NameValuePair(bankPlatforms[i], stringBankFileInfo.Length));
                        }
                    }
                    else
                    {
                        stringsBankRef.FileSizes.Add(new EditorBankRef.NameValuePair("", stringBankFileInfo.Length));
                    }
                }

                eventCache.EditorParameters.ForEach((x) => x.Exists = false);

                foreach (string bankFileName in bankFileNames)
                {
                    EditorBankRef bankRef = eventCache.EditorBanks.Find((x) => RuntimeUtils.GetCommonPlatformPath(bankFileName) == x.Path);

                    // New bank we've never seen before
                    if (bankRef == null)
                    {
                        bankRef = ScriptableObject.CreateInstance<EditorBankRef>();
                        AssetDatabase.AddObjectToAsset(bankRef, eventCache);
                        AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(bankRef));

                        bankRef.SetPath(bankFileName, defaultBankFolder);
                        bankRef.LastModified = DateTime.MinValue;
                        bankRef.FileSizes = new List<EditorBankRef.NameValuePair>();

                        eventCache.EditorBanks.Add(bankRef);
                    }

                    bankRef.Exists = true;

                    FileInfo bankFileInfo = new FileInfo(bankFileName);

                    // Update events from this bank if it has been modified,
                    // or it is a master bank (so that we get any global parameters)
                    if (bankRef.LastModified != bankFileInfo.LastWriteTime
                        || masterBankFileNames.Contains(Path.GetFileName(bankFileName)))
                    {
                        bankRef.LastModified = bankFileInfo.LastWriteTime;
                        UpdateCacheBank(bankRef, ref eventRenameOccurred);
                    }

                    // Update file sizes
                    bankRef.FileSizes.Clear();
                    if (Settings.Instance.HasPlatforms)
                    {
                        for (int i = 0; i < bankPlatforms.Length; i++)
                        {
                            string platformBankPath = RuntimeUtils.GetCommonPlatformPath(bankFolders[i] + bankFileName.Replace(defaultBankFolder, ""));
                            var fileInfo = new FileInfo(platformBankPath);
                            if (fileInfo.Exists)
                            {
                                bankRef.FileSizes.Add(new EditorBankRef.NameValuePair(bankPlatforms[i], fileInfo.Length));
                            }
                        }
                    }
                    else
                    {
                        string platformBankPath = RuntimeUtils.GetCommonPlatformPath(Path.Combine(Settings.Instance.SourceBankPath, bankFileName));
                        var fileInfo = new FileInfo(platformBankPath);
                        if (fileInfo.Exists)
                        {
                            bankRef.FileSizes.Add(new EditorBankRef.NameValuePair("", fileInfo.Length));
                        }
                    }

                    if (masterBankFileNames.Contains(bankFileInfo.Name))
                    {
                        if (!eventCache.MasterBanks.Exists(x => RuntimeUtils.GetCommonPlatformPath(bankFileName) == x.Path))
                        {
                            eventCache.MasterBanks.Add(bankRef);
                        }
                    }
                }

                // Remove any stale entries from bank, event and parameter lists
                eventCache.EditorBanks.FindAll((bankRef) => !bankRef.Exists).ForEach((bankRef) =>
                {
                    eventCache.EditorEvents.ForEach((eventRef) => eventRef.Banks.Remove(bankRef));
                    DestroyImmediate(bankRef, true);
                });
                eventCache.EditorBanks.RemoveAll((x) => x == null);
                eventCache.MasterBanks.RemoveAll((x) => x == null);
                eventCache.StringsBanks.RemoveAll((x) => x == null);

                eventCache.EditorEvents.FindAll((eventRef) => eventRef.Banks.Count == 0).ForEach((eventRef) =>
                {
                    eventRef.Parameters.ForEach((paramRef) => DestroyImmediate(paramRef, true));
                    DestroyImmediate(eventRef, true);
                });
                eventCache.EditorEvents.RemoveAll((x) => x == null);

                eventCache.EditorParameters.FindAll((paramRef) => !paramRef.Exists).ForEach((paramRef) =>
                {
                    DestroyImmediate(paramRef, true);
                });
                eventCache.EditorParameters.RemoveAll((x) => x == null);

                eventCache.BuildDictionary();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                // Unload the strings banks
                loadedStringsBanks.ForEach(x => x.unload());
                AssetDatabase.StopAssetEditing();

                if (reloadPreviewBanks)
                {
                    EditorUtils.LoadPreviewBanks();
                }

                RuntimeUtils.DebugLog("FMOD: Cache updated.");
            }

            if (eventRenameOccurred)
            {
                EditorApplication.delayCall += ShowEventsRenamedDialog;
            }

            // Check if any specified banks are missing
            if (Settings.Instance.BankLoadType == BankLoadType.Specified)
            {
                foreach (var bank in Settings.Instance.BanksToLoad)
                {
                    string bankPath = Path.Combine(defaultBankFolder, bank + ".bank").Replace('\\', '/');
                    if (!File.Exists(bankPath))
                    {
                        RuntimeUtils.DebugLogWarningFormat(
                            "FMOD: Specified bank '{0}' not found at: {1}. It may be missing from the current Studio project or the path is incorrect. " +
                            "Please check 'FMOD > Edit Settings' to verify your Studio project and bank load list.",
                            bank, bankPath);
                    }
                }
            }

            return null;
        }

        private static void ShowEventsRenamedDialog()
        {
#if !FMOD_SERIALIZE_GUID_ONLY
            bool runUpdater = EditorUtility.DisplayDialog("Events Renamed",
                string.Format("Some events have been renamed in FMOD Studio. Do you want to run {0} " +
                "to find and update any references to them?", EventReferenceUpdater.MenuPath), "Yes", "No");

            if (runUpdater)
            {
                EventReferenceUpdater.ShowWindow();
            }
#endif
        }

        private static void UpdateCacheBank(EditorBankRef bankRef, ref bool renameOccurred)
        {
            // Clear out any cached events from this bank
            eventCache.EditorEvents.ForEach((x) => x.Banks.Remove(bankRef));

            FMOD.Studio.Bank bank;
            FMOD.RESULT loadResult = EditorUtils.System.loadBankFile(bankRef.Path, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out bank);

            if (loadResult == FMOD.RESULT.OK)
            {
                // Get studio path
                string studioPath;
                bank.getPath(out studioPath);
                bankRef.SetStudioPath(studioPath);

                // Iterate all events in the bank and cache them
                FMOD.Studio.EventDescription[] eventList;
                var result = bank.getEventList(out eventList);
                if (result == FMOD.RESULT.OK)
                {
                    foreach (var eventDesc in eventList)
                    {
                        string path;
                        result = eventDesc.getPath(out path);

                        FMOD.GUID guid;
                        eventDesc.getID(out guid);

                        EditorEventRef eventRef = eventCache.EditorEvents.Find((x) => string.Compare(x.Path, path, StringComparison.CurrentCultureIgnoreCase) == 0);
                        if (eventRef == null)
                        {
                            eventRef = ScriptableObject.CreateInstance<EditorEventRef>();
                            AssetDatabase.AddObjectToAsset(eventRef, eventCache);
                            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(eventRef));
                            eventRef.Banks = new List<EditorBankRef>();
                            eventCache.EditorEvents.Add(eventRef);
                            eventRef.Parameters = new List<EditorParamRef>();

                            if (!renameOccurred)
                            {
                                EditorEventRef eventRefByGuid = eventCache.EditorEvents.Find((x) => x.Guid == guid);

                                if (eventRefByGuid != null)
                                {
                                    renameOccurred = true;
                                }
                            }
                        }
                        else if (eventRef.Guid != guid)
                        {
                            renameOccurred = true;
                        }

                        eventRef.Banks.Add(bankRef);
                        eventRef.Guid = guid;
                        eventRef.Path = eventRef.name = path;
                        eventDesc.is3D(out eventRef.Is3D);
                        eventDesc.isOneshot(out eventRef.IsOneShot);
                        eventDesc.isStream(out eventRef.IsStream);
                        eventDesc.getMinMaxDistance(out eventRef.MinDistance, out eventRef.MaxDistance);
                        eventDesc.getLength(out eventRef.Length);
                        int paramCount = 0;
                        eventDesc.getParameterDescriptionCount(out paramCount);
                        eventRef.Parameters.ForEach((x) => x.Exists = false);
                        for (int paramIndex = 0; paramIndex < paramCount; paramIndex++)
                        {
                            FMOD.Studio.PARAMETER_DESCRIPTION param;
                            eventDesc.getParameterDescriptionByIndex(paramIndex, out param);
                            // Skip if readonly and not global
                            if ((param.flags & FMOD.Studio.PARAMETER_FLAGS.READONLY) != 0 && (param.flags & FMOD.Studio.PARAMETER_FLAGS.GLOBAL) == 0)
                            {
                                continue;
                            }
                            EditorParamRef paramRef = eventRef.Parameters.Find((x) => x.ID.Equals(param.id));
                            if (paramRef == null)
                            {
                                paramRef = ScriptableObject.CreateInstance<EditorParamRef>();
                                AssetDatabase.AddObjectToAsset(paramRef, eventCache);
                                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(paramRef));
                                eventRef.Parameters.Add(paramRef);
                            }

                            InitializeParamRef(paramRef, param, (labelIndex) => {
                                string label;
                                eventDesc.getParameterLabelByIndex(paramIndex, labelIndex, out label);
                                return label;
                            });

                            paramRef.name = "parameter:/" + Path.GetFileName(path) + "/" + paramRef.Name;
                            paramRef.Exists = true;
                        }
                        eventRef.Parameters.FindAll((x) => !x.Exists).ForEach((x) => DestroyImmediate(x, true));
                        eventRef.Parameters.RemoveAll((x) => x == null);
                    }
                }

                // Update global parameter list for each bank
                FMOD.Studio.PARAMETER_DESCRIPTION[] parameterDescriptions;
                result = EditorUtils.System.getParameterDescriptionList(out parameterDescriptions);
                if (result == FMOD.RESULT.OK)
                {
                    for (int i = 0; i < parameterDescriptions.Length; i++)
                    {
                        FMOD.Studio.PARAMETER_DESCRIPTION param = parameterDescriptions[i];
                        if ((param.flags & FMOD.Studio.PARAMETER_FLAGS.GLOBAL) == FMOD.Studio.PARAMETER_FLAGS.GLOBAL)
                        {
                            EditorParamRef paramRef = eventCache.EditorParameters.Find((x) => x.ID.Equals(param.id));
                            if (paramRef == null)
                            {
                                paramRef = ScriptableObject.CreateInstance<EditorParamRef>();
                                AssetDatabase.AddObjectToAsset(paramRef, eventCache);
                                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(paramRef));
                                eventCache.EditorParameters.Add(paramRef);
                            }

                            InitializeParamRef(paramRef, param, (index) => {
                                string label;
                                EditorUtils.System.getParameterLabelByID(param.id, index, out label);
                                return label;
                            });

                            paramRef.name = "parameter:/" + param.name;
                            EditorUtils.System.lookupPath(param.guid, out paramRef.StudioPath);
                            paramRef.Exists = true;
                        }
                    }
                }
                bank.unload();
            }
            else
            {
                RuntimeUtils.DebugLogError(string.Format("FMOD Studio: Unable to load {0}: {1}", bankRef.Name, FMOD.Error.String(loadResult)));
                eventCache.CacheTime = DateTime.MinValue;
            }
        }

        private static void InitializeParamRef(EditorParamRef paramRef, FMOD.Studio.PARAMETER_DESCRIPTION description,
            Func<int, string> getLabel)
        {
            paramRef.Name = description.name;
            paramRef.Min = description.minimum;
            paramRef.Max = description.maximum;
            paramRef.Default = description.defaultvalue;
            paramRef.ID = description.id;
            paramRef.IsGlobal = (description.flags & FMOD.Studio.PARAMETER_FLAGS.GLOBAL) != 0;

            if ((description.flags & FMOD.Studio.PARAMETER_FLAGS.LABELED) != 0)
            {
                paramRef.Type = ParameterType.Labeled;
                paramRef.Labels = GetParameterLabels(description, getLabel);
            }
            else if ((description.flags & FMOD.Studio.PARAMETER_FLAGS.DISCRETE) != 0)
            {
                paramRef.Type = ParameterType.Discrete;
            }
            else
            {
                paramRef.Type = ParameterType.Continuous;
            }
        }

        private static string[] GetParameterLabels(FMOD.Studio.PARAMETER_DESCRIPTION parameterDescription,
            Func<int, string> getLabel)
        {
            string[] labels = new string[(int)parameterDescription.maximum + 1];

            for (int i = 0; i <= parameterDescription.maximum; ++i)
            {
                labels[i] = getLabel(i);
            }

            return labels;
        }

        static EventManager()
        {
            BuildStatusWatcher.OnBuildStarted += () => {
                BuildTargetChanged();
                CopyToStreamingAssets(EditorUserBuildSettings.activeBuildTarget);
                ApplyFMODLabel();
            };
            BuildStatusWatcher.OnBuildEnded += () => {
                UpdateBankStubAssets(EditorUserBuildSettings.activeBuildTarget);
            };
        }

        public static void Startup()
        {
            EventReference.GuidLookupDelegate = (path) => {
                EditorEventRef editorEventRef = EventFromPath(path);

                return (editorEventRef != null) ? editorEventRef.Guid : new FMOD.GUID();
            };

            // Avoid throwing exceptions so we don't stop other startup code from running
            try
            {
                RefreshBanks();
            }
            catch (Exception e)
            {
                RuntimeUtils.DebugLogException(e);
            }
        }

        public static void ValidateEventReferences(Scene scene)
        {
            foreach (GameObject gameObject in scene.GetRootGameObjects())
            {
                MonoBehaviour[] behaviours = gameObject.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour != null)
                    {
                        if (behaviour is StudioEventEmitter)
                        {
                            ValidateEventEmitter(behaviour as StudioEventEmitter, scene);
                        }
                        else
                        {
                            ValidateEventReferenceFields(behaviour, scene);
                        }
                    }
                }
            }
        }

        private static readonly string UpdaterInstructions =
            string.Format("Please run {0} to resolve this issue.", EventReferenceUpdater.MenuPath);

        private static void ValidateEventEmitter(StudioEventEmitter emitter, Scene scene)
        {
#pragma warning disable 0618 // Suppress a warning about using the obsolete StudioEventEmitter.Event field
            if (!string.IsNullOrEmpty(emitter.Event))
#pragma warning restore 0618
            {
                RuntimeUtils.DebugLogWarningFormat("FMOD: A Studio Event Emitter in scene '{0}' on GameObject '{1}' is using the "
                    + "obsolete Event field. {2}",
                    scene.name, EditorUtils.GameObjectPath(emitter), UpdaterInstructions);
            }

            bool changed;
            if (!ValidateEventReference(ref emitter.EventReference, emitter, scene, out changed))
            {
                RuntimeUtils.DebugLogWarningFormat(
                    "FMOD: A Studio Event Emitter in scene '{0}' on GameObject '{1}' has an invalid event reference: {2}",
                    scene.name, EditorUtils.GameObjectPath(emitter), emitter.EventReference);
            }
        }

        private static void ValidateEventReferenceFields(MonoBehaviour behaviour, Scene scene)
        {
            Type type = behaviour.GetType();

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
#pragma warning disable 0618 // Suppress a warning about using the obsolete EventRefAttribute class
                if (EditorUtils.HasAttribute<EventRefAttribute>(field))
#pragma warning restore 0618
                {
                    RuntimeUtils.DebugLogWarningFormat("FMOD: A component of type {0} in scene '{1}' on GameObject '{2}' has an "
                        + "obsolete [FMODUnity.EventRef] attribute on field {3}. {4}",
                        type.Name, scene.name, EditorUtils.GameObjectPath(behaviour), field.Name,
                        UpdaterInstructions);
                }
                else if (field.FieldType == typeof(EventReference))
                {
                    EventReference eventReference = (EventReference)field.GetValue(behaviour);

                    bool changed;
                    if (!ValidateEventReference(ref eventReference, behaviour, scene, out changed))
                    {
                        RuntimeUtils.DebugLogWarningFormat(
                            "FMOD: A component of type {0} in scene '{1}' on GameObject '{2}' has an "
                            + "invalid event reference in field '{3}': {4}",
                            type.Name, scene.name, EditorUtils.GameObjectPath(behaviour), field.Name, eventReference);
                    }

                    if (changed)
                    {
                        field.SetValue(behaviour, eventReference);
                    }
                }
            }
        }

        // Returns true if eventReference is valid, sets changed if eventReference was changed
        private static bool ValidateEventReference(ref EventReference eventReference,
            Component parent, Scene scene, out bool changed)
        {
            changed = false;

            if (eventReference.IsNull)
            {
                return true;
            }

            EditorEventRef editorEventRef;

            EventLinkage eventLinkage = GetEventLinkage(eventReference);

            if (eventLinkage == EventLinkage.GUID)
            {
                editorEventRef = EventFromGUID(eventReference.Guid);

                if (editorEventRef == null)
                {
                    return false;
                }

                if (eventReference.Path != editorEventRef.Path)
                {
                    RuntimeUtils.DebugLogWarningFormat(
                        "FMOD: EventReference path '{0}' doesn't match GUID {1} on object '{2}' in scene '{3}'. {4}",
                        eventReference.Path, eventReference.Guid, EditorUtils.GameObjectPath(parent), scene.name,
                        UpdaterInstructions);
                }

                return true;
            }
            else if (eventLinkage == EventLinkage.Path)
            {
                editorEventRef = EventFromPath(eventReference.Path);

                if (editorEventRef == null)
                {
                    return false;
                }

                if (eventReference.Guid != editorEventRef.Guid)
                {
                    RuntimeUtils.DebugLogWarningFormat(
                        "FMOD: Changing EventReference GUID to {0} to match path '{1}' on object '{2}' in scene '{3}'. {4}",
                        editorEventRef.Guid, eventReference.Path, EditorUtils.GameObjectPath(parent), scene.name,
                        UpdaterInstructions);

                    eventReference.Guid = editorEventRef.Guid;
                    EditorUtility.SetDirty(parent);

                    changed = true;
                }

                return true;
            }
            else
            {
                throw new NotSupportedException("Unrecognized EventLinkage: " + eventLinkage);
            }
        }

        public static void CopyToStreamingAssets(BuildTarget buildTarget)
        {
            if (Settings.Instance.ImportType == ImportType.AssetBundle && BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            if (string.IsNullOrEmpty(Settings.Instance.SourceBankPath))
                return;

            Platform platform = EditorSettings.Instance.GetPlatform(buildTarget);

            if (platform == Settings.Instance.DefaultPlatform)
            {
                RuntimeUtils.DebugLogWarningFormat("FMOD Studio: copy banks for platform {0} : Unsupported platform", buildTarget);
                return;
            }

            string bankTargetFolder =
                Settings.Instance.ImportType == ImportType.StreamingAssets
                ? Settings.Instance.TargetPath
                : Application.dataPath + (string.IsNullOrEmpty(Settings.Instance.TargetAssetPath) ? "" : '/' + Settings.Instance.TargetAssetPath);
            bankTargetFolder = RuntimeUtils.GetCommonPlatformPath(bankTargetFolder);
            Directory.CreateDirectory(bankTargetFolder);

            string bankTargetExtension =
                Settings.Instance.ImportType == ImportType.StreamingAssets
                ? ".bank"
                : ".bytes";

            string bankSourceFolder =
                Settings.Instance.HasPlatforms
                ? Settings.Instance.SourceBankPath + '/' + platform.BuildDirectory
                : Settings.Instance.SourceBankPath;
            bankSourceFolder = RuntimeUtils.GetCommonPlatformPath(bankSourceFolder);

            if (Path.GetFullPath(bankTargetFolder).TrimEnd('/').ToUpperInvariant() ==
                Path.GetFullPath(bankSourceFolder).TrimEnd('/').ToUpperInvariant())
            {
                return;
            }

            bool madeChanges = false;

            try
            {
                // Clean out any stale .bank files
                string[] existingBankFiles =
                    Directory.GetFiles(bankTargetFolder, "*" + bankTargetExtension, SearchOption.AllDirectories);

                foreach (string bankFilePath in existingBankFiles)
                {
                    string bankName = EditorBankRef.CalculateName(bankFilePath, bankTargetFolder);

                    if (!eventCache.EditorBanks.Exists(x => x.Name == bankName))
                    {
                        string assetPath = bankFilePath.Replace(Application.dataPath, AssetsFolderName);

                        if (AssetHasLabel(assetPath, FMODLabel))
                        {
                            AssetDatabase.MoveAssetToTrash(assetPath);
                            madeChanges = true;
                        }
                    }
                }

                // Copy over any files that don't match timestamp or size or don't exist
                AssetDatabase.StartAssetEditing();
                foreach (var bankRef in eventCache.EditorBanks)
                {
                    string sourcePath = bankSourceFolder + "/" + bankRef.Name + ".bank";
                    string targetPathRelative = bankRef.Name + bankTargetExtension;
                    string targetPathFull = bankTargetFolder + "/" + targetPathRelative;

                    FileInfo sourceInfo = new FileInfo(sourcePath);
                    FileInfo targetInfo = new FileInfo(targetPathFull);

                    if (!targetInfo.Exists ||
                        sourceInfo.Length != targetInfo.Length ||
                        sourceInfo.LastWriteTime != targetInfo.LastWriteTime)
                    {
                        if (targetInfo.Exists)
                        {
                            targetInfo.IsReadOnly = false;
                        }
                        else
                        {
                            EnsureFoldersExist(targetPathRelative, bankTargetFolder);
                        }

                        File.Copy(sourcePath, targetPathFull, true);
                        targetInfo = new FileInfo(targetPathFull);
                        targetInfo.IsReadOnly = false;
                        targetInfo.LastWriteTime = sourceInfo.LastWriteTime;

                        madeChanges = true;

                        string assetString = targetPathFull.Replace(Application.dataPath, "Assets");
                        AssetDatabase.ImportAsset(assetString);
                        newBankPaths.Add(assetString);
                    }
                }

                RemoveEmptyFMODFolders(bankTargetFolder);
            }
            catch (Exception exception)
            {
                RuntimeUtils.DebugLogErrorFormat("FMOD Studio: copy banks for platform {0} : copying banks from {1} to {2}",
                    platform.DisplayName, bankSourceFolder, bankTargetFolder);
                RuntimeUtils.DebugLogException(exception);
                return;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (madeChanges)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                RuntimeUtils.DebugLogFormat("FMOD Studio: copy banks for platform {0} : copying banks from {1} to {2} succeeded",
                    platform.DisplayName, bankSourceFolder, bankTargetFolder);
            }
        }

        public static void UpdateBankStubAssets(BuildTarget buildTarget)
        {
            if (Settings.Instance.ImportType != ImportType.AssetBundle
                || string.IsNullOrEmpty(Settings.Instance.SourceBankPath))
            {
                return;
            }

            Platform platform = EditorSettings.Instance.GetPlatform(buildTarget);

            if (platform == Settings.Instance.DefaultPlatform)
            {
                Debug.LogWarningFormat("FMOD: Updating bank stubs: Unsupported platform {0}", buildTarget);
                return;
            }

            string bankTargetFolder = Application.dataPath;

            if (!string.IsNullOrEmpty(Settings.Instance.TargetAssetPath))
            {
                bankTargetFolder += "/" + Settings.Instance.TargetAssetPath;
            }

            bankTargetFolder = RuntimeUtils.GetCommonPlatformPath(bankTargetFolder);

            string bankSourceFolder = Settings.Instance.SourceBankPath;

            if (Settings.Instance.HasPlatforms)
            {
                bankSourceFolder += "/" + platform.BuildDirectory;
            }

            bankSourceFolder = RuntimeUtils.GetCommonPlatformPath(bankSourceFolder);

            if (Path.GetFullPath(bankTargetFolder).TrimEnd('/').ToUpperInvariant() ==
                Path.GetFullPath(bankSourceFolder).TrimEnd('/').ToUpperInvariant())
            {
                return;
            }

            bool madeChanges = false;

            Directory.CreateDirectory(bankTargetFolder);

            try
            {
                const string BankAssetExtension = ".bytes";

                // Clean out any stale stubs
                string[] existingBankFiles =
                    Directory.GetFiles(bankTargetFolder, "*" + BankAssetExtension, SearchOption.AllDirectories);

                foreach (string bankFilePath in existingBankFiles)
                {
                    string bankName = EditorBankRef.CalculateName(bankFilePath, bankTargetFolder);

                    if (!eventCache.EditorBanks.Exists(x => x.Name == bankName))
                    {
                        string assetPath = bankFilePath.Replace(Application.dataPath, AssetsFolderName);

                        if (AssetHasLabel(assetPath, FMODLabel))
                        {
                            AssetDatabase.MoveAssetToTrash(assetPath);
                            madeChanges = true;
                        }
                    }
                }

                // Create any stubs that don't exist, and ensure any that do exist have the correct data
                AssetDatabase.StartAssetEditing();
                foreach (var bankRef in eventCache.EditorBanks)
                {
                    string sourcePath = bankSourceFolder + "/" + bankRef.Name + ".bank";
                    string targetPathRelative = bankRef.Name + BankAssetExtension;
                    string targetPathFull = bankTargetFolder + "/" + targetPathRelative;

                    EnsureFoldersExist(targetPathRelative, bankTargetFolder);

                    FileInfo targetInfo = new FileInfo(targetPathFull);

                    string stubData = RuntimeManager.BankStubPrefix + bankRef.Name;

                    // Minimise asset database refreshing by only writing the stub if necessary
                    bool writeStub;

                    if (targetInfo.Exists && targetInfo.Length == stubData.Length)
                    {
                        using (StreamReader reader = targetInfo.OpenText())
                        {
                            string contents = reader.ReadToEnd();
                            writeStub = (contents != stubData);
                        }
                    }
                    else
                    {
                        writeStub = true;
                    }

                    if (writeStub)
                    {
                        // Create or update the stub
                        using (StreamWriter writer = targetInfo.CreateText())
                        {
                            writer.Write(stubData);
                        }

                        madeChanges = true;

                        if (!targetInfo.Exists)
                        {
                            string assetPath = targetPathFull.Replace(Application.dataPath, "Assets");
                            AssetDatabase.ImportAsset(assetPath);

                            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                            AssetDatabase.SetLabels(obj, new string[] { FMODLabel });
                        }
                    }
                }
                RemoveEmptyFMODFolders(bankTargetFolder);
            }
            catch (Exception exception)
            {
                Debug.LogErrorFormat("FMOD: Updating bank stubs in {0} to match {1}",
                    bankTargetFolder, bankSourceFolder);
                Debug.LogException(exception);
                return;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (madeChanges)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.LogFormat("FMOD: Updated bank stubs in {0} to match {1}", bankTargetFolder, bankSourceFolder);
            }
        }

        private static void EnsureFoldersExist(string filePath, string basePath)
        {
            string dataPath = Application.dataPath + "/";

            if (!basePath.StartsWith(dataPath))
            {
                throw new ArgumentException(
                    string.Format("Base path {0} is not within the Assets folder", basePath), "basePath");
            }

            int lastSlash = filePath.LastIndexOf('/');

            if (lastSlash == -1)
            {
                // No folders
                return;
            }

            string assetString = filePath.Substring(0, lastSlash);

            string[] folders = assetString.Split('/');
            string parentFolder = "Assets/" + basePath.Substring(dataPath.Length);

            for (int i = 0; i < folders.Length; ++i)
            {
                string folderPath = parentFolder + "/" + folders[i];

                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder(parentFolder, folders[i]);

                    var folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
                    AssetDatabase.SetLabels(folder, new string[] { FMODLabel });
                }

                parentFolder = folderPath;
            }
        }

        private static void BuildTargetChanged()
        {
            RefreshBanks();
#if UNITY_ANDROID
#if UNITY_2023_1_OR_NEWER
            Settings.Instance.AndroidUseOBB = PlayerSettings.Android.splitApplicationBinary;
#else
            Settings.Instance.AndroidUseOBB = PlayerSettings.Android.useAPKExpansionFiles;
#endif //UNITY_2023_1_OR_NEWER
#endif //UNITY_ANDROID
        }

        private static void OnCacheChange()
        {
            List<string> masterBanks = new List<string>();
            List<string> banks = new List<string>();

            var settings = Settings.Instance;
            bool hasChanged = false;

            foreach (EditorBankRef bankRef in eventCache.MasterBanks)
            {
                masterBanks.Add(bankRef.Name);
            }

            if (!CompareLists(masterBanks, settings.MasterBanks))
            {
                settings.MasterBanks.Clear();
                settings.MasterBanks.AddRange(masterBanks);
                hasChanged = true;
            }

            foreach (var bankRef in eventCache.EditorBanks)
            {
                if (!eventCache.MasterBanks.Contains(bankRef) &&
                    !eventCache.StringsBanks.Contains(bankRef))
                {
                    banks.Add(bankRef.Name);
                }
            }
            banks.Sort((a, b) => string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase));

            if (!CompareLists(banks, settings.Banks))
            {
                settings.Banks.Clear();
                settings.Banks.AddRange(banks);
                hasChanged = true;
            }

            if (hasChanged)
            {
                EditorUtility.SetDirty(settings);
            }
        }

        public static DateTime CacheTime
        {
            get
            {
                if (eventCache != null)
                {
                    return eventCache.CacheTime;
                }
                else
                {
                    return DateTime.MinValue;
                }
            }
        }

        public static List<EditorEventRef> Events
        {
            get
            {
                AffirmEventCache();
                return eventCache.EditorEvents;
            }
        }

        public static List<EditorBankRef> Banks
        {
            get
            {
                AffirmEventCache();
                return eventCache.EditorBanks;
            }
        }

        public static List<EditorParamRef> Parameters
        {
            get
            {
                AffirmEventCache();
                return eventCache.EditorParameters;
            }
        }

        public static List<EditorBankRef> MasterBanks
        {
            get
            {
                AffirmEventCache();
                return eventCache.MasterBanks;
            }
        }

        public static bool IsLoaded
        {
            get
            {
                return Settings.Instance.SourceBankPath != null;
            }
        }

        public static bool IsValid
        {
            get
            {
                AffirmEventCache();
                return eventCache.CacheTime != DateTime.MinValue;
            }
        }

        public static bool IsInitialized
        {
            get
            {
                return eventCache != null;
            }
        }

        public static EventLinkage GetEventLinkage(EventReference eventReference)
        {
            if (Settings.Instance.EventLinkage == EventLinkage.Path)
            {
                if (string.IsNullOrEmpty(eventReference.Path) && !eventReference.Guid.IsNull)
                {
                    return EventLinkage.GUID;
                }
                else
                {
                    return EventLinkage.Path;
                }
            }
            else // Assume EventLinkage.GUID
            {
                if (eventReference.Guid.IsNull && !string.IsNullOrEmpty(eventReference.Path))
                {
                    return EventLinkage.Path;
                }
                else
                {
                    return EventLinkage.GUID;
                }
            }
        }

        public static EditorEventRef EventFromPath(string pathOrGuid)
        {
            EditorEventRef eventRef;
            if (pathOrGuid.StartsWith("{"))
            {
                eventRef = EventFromGUID(FMOD.GUID.Parse(pathOrGuid));
            }
            else
            {
                eventRef = EventFromString(pathOrGuid);
            }
            return eventRef;
        }

        public static EditorEventRef EventFromString(string path)
        {
            AffirmEventCache();

            if (eventCache.EditorEventsDict.TryGetValue(path, out int index))
            {
                return eventCache.EditorEvents[index];
            }

            return null;
        }

        public static EditorEventRef EventFromGUID(FMOD.GUID guid)
        {
            AffirmEventCache();
            return eventCache.EditorEvents.Find((x) => x.Guid == guid);
        }

        public static EditorParamRef ParamFromPath(string name)
        {
            AffirmEventCache();
            return eventCache.EditorParameters.Find((x) => x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
        }

        public class ActiveBuildTargetListener : IActiveBuildTargetChanged
        {
            public int callbackOrder{ get { return 0; } }
            public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
            {
                BuildTargetChanged();
            }
        }

        public class PreprocessScene : IProcessSceneWithReport
        {
            public int callbackOrder { get { return 0; } }

            public void OnProcessScene(Scene scene, BuildReport report)
            {
                if (report == null) return;

                ValidateEventReferences(scene);
            }
        }

        private static bool CompareLists(List<string> tempBanks, List<string> banks)
        {
            if (tempBanks.Count != banks.Count)
                return false;

            for (int i = 0; i < tempBanks.Count; i++)
            {
                if (tempBanks[i] != banks[i])
                    return false;
            }
            return true;
        }

        private static bool AssetHasLabel(string assetPath, string label)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            string[] labels = AssetDatabase.GetLabels(asset);

            return labels.Contains(label);
        }

        public static void RemoveBanks(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                return;
            }

            string[] filePaths = Directory.GetFiles(basePath, "*", SearchOption.AllDirectories);

            foreach (string filePath in filePaths)
            {
                if (!filePath.EndsWith(".meta"))
                {
                    string assetPath = filePath.Replace(Application.dataPath, AssetsFolderName);

                    if (AssetHasLabel(assetPath, FMODLabel))
                    {
                        AssetDatabase.MoveAssetToTrash(assetPath);
                    }
                }
            }

            RemoveEmptyFMODFolders(basePath);

            if (Directory.GetFileSystemEntries(basePath).Length == 0)
            {
                string baseFolder = basePath.Replace(Application.dataPath, AssetsFolderName);
                AssetDatabase.MoveAssetToTrash(baseFolder);
            }
        }

        public static void MoveBanks(string from, string to)
        {
            if (!Directory.Exists(from))
            {
                return;
            }

            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }

            string[] oldBankFiles = Directory.GetFiles(from);

            foreach (var oldBankFileName in oldBankFiles)
            {
                if (oldBankFileName.EndsWith(".meta"))
                    continue;
                string assetString = oldBankFileName.Replace(Application.dataPath, "Assets");
                AssetDatabase.ImportAsset(assetString);
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetString);
                string[] labels = AssetDatabase.GetLabels(obj);
                foreach (string label in labels)
                {
                    if (label.Equals("FMOD"))
                    {
                        AssetDatabase.MoveAsset(assetString, to);
                        break;
                    }
                }
            }
            if (Directory.GetFiles(Path.GetDirectoryName(oldBankFiles[0])).Length == 0)
            {
                Directory.Delete(Path.GetDirectoryName(oldBankFiles[0]));
            }
        }

        public static void RemoveEmptyFMODFolders(string basePath)
        {
            string[] folderPaths = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories);

            // Process longest paths first so parent folders are cleared out when we get to them
            Array.Sort(folderPaths, (a, b) => b.Length.CompareTo(a.Length));

            foreach (string folderPath in folderPaths)
            {
                string assetPath = folderPath.Replace(Application.dataPath, AssetsFolderName);

                if (AssetHasLabel(assetPath, FMODLabel) && Directory.GetFileSystemEntries(folderPath).Length == 0)
                {
                    AssetDatabase.MoveAssetToTrash(assetPath);
                }
            }
        }

        private static void ApplyFMODLabel()
        {
            foreach (string assetPath in newBankPaths)
            {
                if (!AssetHasLabel(assetPath, FMODLabel))
                {
                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    AssetDatabase.SetLabels(obj, new string[] { FMODLabel });
                }
            }

            newBankPaths.Clear();
        }
    }
}
