using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.Serialization;
using System.Runtime.CompilerServices;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

[assembly: InternalsVisibleTo("FMODUnityEditor")]
namespace FMODUnity
{
    [Serializable]
    public enum ImportType
    {
        StreamingAssets,
        AssetBundle,
    }

    [Serializable]
    public enum BankLoadType
    {
        All,
        Specified,
        None
    }

    [Serializable]
    public enum MeterChannelOrderingType
    {
        Standard,
        SeparateLFE,
        Positional
    }

    public enum EventLinkage
    {
        Path,
        GUID,
    }

    public enum TriStateBool
    {
        Disabled,
        Enabled,
        Development,
    }

    public enum ScreenPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        BottomLeft,
        BottomCenter,
        BottomRight,
        Center,
        VR,
    }

    public interface IEditorSettings
    {
#if UNITY_EDITOR
        Settings RuntimeSettings { get; set; }
        bool ForceLoggingBinaries { get; set; }
        Platform CurrentEditorPlatform { get; }
        void Clear();
        void ResetPlatformSettings();
        void ReimportLegacyPlatforms();
        void CreateSettingsAsset(string assetName);
        void AddMissingPlatforms();
        void AddPlatformsToAsset();
        void AddPlatformForBuildTargets(Platform platform);
        void UpdateMigratedPlatform(Platform platform);
        Platform GetPlatform(BuildTarget buildTarget);
        void SetPlatformParent(Platform platform, Platform newParent);
        PlatformGroup AddPlatformGroup(string displayName, int sortOrder);
        void PreprocessBuild(BuildTarget target, Platform.BinaryType binaryType);
        void CleanTemporaryFiles();
        void DeleteTemporaryFile(string assetPath);
        bool CanBuildTarget(BuildTarget target, Platform.BinaryType binaryType, out string error);
        void CheckActiveBuildTarget();
#endif
    }

    // This class stores all of the FMOD for Unity cross-platform settings, as well as a collection
    // of Platform objects that hold the platform-specific settings. The Platform objects are stored
    // in the same asset as the Settings object using AssetDatabase.AddObjectToAsset.
    public class Settings : ScriptableObject
    {
#if UNITY_EDITOR
        [FormerlySerializedAs("SwitchSettingsMigration")]
        [SerializeField]
        private bool switchSettingsMigration = false;
#endif

        internal const string SettingsAssetName = "FMODStudioSettings";

        private static Settings instance = null;
        private static IEditorSettings editorSettings = null;
        private static bool isInitializing = false;

        [SerializeField]
        public bool HasSourceProject = true;

        [SerializeField]
        public bool HasPlatforms = true;

        [SerializeField]
        private string sourceProjectPath;

        [SerializeField]
        private string sourceBankPath;

        [FormerlySerializedAs("SourceBankPathUnformatted")]
        [SerializeField]
        private string sourceBankPathUnformatted; // Kept as to not break existing projects

        [SerializeField]
        public int BankRefreshCooldown = 5;

        [SerializeField]
        public bool ShowBankRefreshWindow = true;

        internal const int BankRefreshPrompt = -1;
        internal const int BankRefreshManual = -2;

        [SerializeField]
        public bool AutomaticEventLoading;

        [SerializeField]
        public BankLoadType BankLoadType;

        [SerializeField]
        public bool AutomaticSampleLoading;

        [SerializeField]
        public string EncryptionKey;

        [SerializeField]
        public ImportType ImportType;

        [SerializeField]
        public string TargetAssetPath = "FMODBanks";

        [SerializeField]
        public string TargetBankFolder = "";

        [SerializeField]
#if FMOD_SERIALIZE_GUID_ONLY
        public EventLinkage EventLinkage = EventLinkage.GUID;
#else
        public EventLinkage EventLinkage = EventLinkage.Path;
#endif

        [SerializeField]
        public bool SerializeGUIDsOnly;

        [SerializeField]
        public FMOD.DEBUG_FLAGS LoggingLevel = FMOD.DEBUG_FLAGS.WARNING;

        [SerializeField]
        internal List<Legacy.PlatformIntSetting> SpeakerModeSettings;

        [SerializeField]
        internal List<Legacy.PlatformIntSetting> SampleRateSettings;

        [SerializeField]
        internal List<Legacy.PlatformBoolSetting> LiveUpdateSettings;

        [SerializeField]
        internal List<Legacy.PlatformBoolSetting> OverlaySettings;

        [SerializeField]
        internal List<Legacy.PlatformStringSetting> BankDirectorySettings;

        [SerializeField]
        internal List<Legacy.PlatformIntSetting> VirtualChannelSettings;

        [SerializeField]
        internal List<Legacy.PlatformIntSetting> RealChannelSettings;

        [SerializeField]
        internal List<string> Plugins = new List<string>();

        [SerializeField]
        public List<string> MasterBanks;

        [SerializeField]
        public List<string> Banks;

        [SerializeField]
        public List<string> BanksToLoad;

        [SerializeField]
        public ushort LiveUpdatePort = 9264;

        [SerializeField]
        public bool EnableMemoryTracking;

        [SerializeField]
        public bool AndroidUseOBB = false;

        [SerializeField]
        public bool AndroidPatchBuild = false;

        [SerializeField]
        public MeterChannelOrderingType MeterChannelOrdering;

        [SerializeField]
        public bool StopEventsOutsideMaxDistance = false;

        [SerializeField]
        internal bool BoltUnitOptionsBuildPending = false;

        [SerializeField]
        public bool EnableErrorCallback = false;

        [SerializeField]
        internal SharedLibraryUpdateStages SharedLibraryUpdateStage = SharedLibraryUpdateStages.Start;

        [SerializeField]
        internal double SharedLibraryTimeSinceStart = 0.0;

        [SerializeField]
        internal int CurrentVersion;

        [SerializeField]
        public bool HideSetupWizard;

        [SerializeField]
        internal int LastEventReferenceScanVersion;

        // This holds all known platforms, but only those that have settings are shown in the UI.
        // It is populated at load time from the Platform objects in the settings asset.
        // It is serializable to facilitate undo support.
        [SerializeField]
        public List<Platform> Platforms = new List<Platform>();

        // This is used to find the platform that matches the current Unity runtime platform.
        internal Dictionary<RuntimePlatform, List<Platform>> PlatformForRuntimePlatform = new Dictionary<RuntimePlatform, List<Platform>>();

        // Default platform settings.
        [NonSerialized]
        public Platform DefaultPlatform;

        // Play In Editor platform settings.
        [NonSerialized]
        public Platform PlayInEditorPlatform;

#if UNITY_EDITOR
        // We store a persistent list so we don't try to re-migrate platforms if the user deletes them.
        [SerializeField]
        internal List<Legacy.Platform> MigratedPlatforms = new List<Legacy.Platform>();
#endif

        // A collection of templates for constructing known platforms.
        internal static List<PlatformTemplate> PlatformTemplates = new List<PlatformTemplate>();

        [NonSerialized]
        private bool hasLoaded = false;

        public static Settings Instance
        {
            get
            {
                if (isInitializing)
                {
                    return null;
                }

                Initialize();

                return instance;
            }
        }

        internal static void Initialize()
        {
            if (instance == null)
            {
                isInitializing = true;

                instance = Resources.Load(SettingsAssetName) as Settings;

                if (instance == null)
                {
                    RuntimeUtils.DebugLog("[FMOD] Cannot find integration settings, creating default settings");
                    instance = CreateInstance<Settings>();
                    instance.name = "FMOD Studio Integration Settings";
                    instance.CurrentVersion = FMOD.VERSION.number;
                    instance.LastEventReferenceScanVersion = FMOD.VERSION.number;

#if UNITY_EDITOR
                    if (editorSettings != null)
                    {
                        editorSettings.CreateSettingsAsset(SettingsAssetName);
                    }
                    else
                    {
                        // editorSettings is populated via the static constructor of FMODUnity.EditorSettings when in the Unity editor.
                        RuntimeUtils.DebugLogError("[FMOD] Attempted to instantiate Settings before EditorSettings was populated. " +
                            "Ensure that Settings.Instance is not being called from an InitializeOnLoad method or class.");
                    }
#endif
                }
                else
                {
#if UNITY_EDITOR
                    if (AssetDatabase.GetAssetPath(instance).StartsWith("Packages"))
                    {
                        RuntimeUtils.DebugLogError($"[FMOD] {SettingsAssetName} initialization failed. {SettingsAssetName} located in \"Packages\" folder. Please delete {SettingsAssetName} in file explorer.");
                        instance = CreateInstance<Settings>();
                    }
#endif
                }

                isInitializing = false;
            }
        }

        internal static bool IsInitialized()
        {
            return !(instance == null || isInitializing);
        }

        internal static IEditorSettings EditorSettings
        {
            get
            {
                return editorSettings;
            }
            set
            {
                editorSettings = value;
            }
        }

        public string SourceProjectPath
        {
            get
            {
                return sourceProjectPath;
            }
            set
            {
                sourceProjectPath = value;
            }
        }

        public string SourceBankPath
        {
            get
            {
                return sourceBankPath;
            }
            set
            {
                sourceBankPath = value;
            }
        }

        internal string TargetPath
        {
            get
            {
                if (ImportType == ImportType.AssetBundle)
                {
                    if (string.IsNullOrEmpty(TargetAssetPath))
                    {
                        return Application.dataPath;
                    }
                    else
                    {
                        return Application.dataPath + "/" + TargetAssetPath;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(TargetBankFolder))
                    {
                        return Application.streamingAssetsPath;
                    }
                    else
                    {
                        return Application.streamingAssetsPath + "/" + TargetBankFolder;
                    }
                }
            }
        }

        public string TargetSubFolder
        {
            get
            {
                if (ImportType == ImportType.AssetBundle)
                {
                    return TargetAssetPath;
                }
                else
                {
                    return TargetBankFolder;
                }
            }
            set
            {
                if (ImportType == ImportType.AssetBundle)
                {
                    TargetAssetPath = value;
                }
                else
                {
                    TargetBankFolder = value;
                }
            }
        }

        internal enum SharedLibraryUpdateStages
        {
            Start = 0,
            DisableExistingLibraries,
            RestartUnity,
            CopyNewLibraries,
        };

        internal Platform FindPlatform(string identifier)
        {
            foreach (Platform platform in Platforms)
            {
                if (platform.Identifier == identifier)
                {
                    return platform;
                }
            }

            return null;
        }

        internal bool PlatformExists(string identifier)
        {
            return FindPlatform(identifier) != null;
        }

        internal void AddPlatform(Platform platform)
        {
            if (PlatformExists(platform.Identifier))
            {
                throw new ArgumentException(string.Format("Duplicate platform identifier: {0}", platform.Identifier));
            }

            Platforms.Add(platform);
        }

        internal void RemovePlatform(string identifier)
        {
            Platforms.RemoveAll(p => p.Identifier == identifier);
        }

        // Links the platform to its parent, and to the BuildTargets and RuntimePlatforms it implements.
        internal void LinkPlatform(Platform platform)
        {
            LinkPlatformToParent(platform);

            platform.DeclareRuntimePlatforms(this);

#if UNITY_EDITOR
            if (editorSettings != null)
            {
                editorSettings.AddPlatformForBuildTargets(platform);
            }
#endif
        }

        internal void DeclareRuntimePlatform(RuntimePlatform runtimePlatform, Platform platform)
        {
            List<Platform> platforms;

            if (!PlatformForRuntimePlatform.TryGetValue(runtimePlatform, out platforms))
            {
                platforms = new List<Platform>();
                PlatformForRuntimePlatform.Add(runtimePlatform, platforms);
            }

            platforms.Add(platform);

            // Highest priority goes first
            platforms.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        // Links the given platform to its parent, if it has one.
        private void LinkPlatformToParent(Platform platform)
        {
            if (!string.IsNullOrEmpty(platform.ParentIdentifier))
            {
                SetPlatformParent(platform, FindPlatform(platform.ParentIdentifier));
            }
        }

        // The highest-priority platform that matches the current environment.
        internal Platform FindCurrentPlatform()
        {
            List<Platform> platforms;

            if (PlatformForRuntimePlatform.TryGetValue(Application.platform, out platforms))
            {
                foreach (Platform platform in platforms)
                {
                    if (platform.MatchesCurrentEnvironment)
                    {
                        return platform;
                    }
                }
            }

            return DefaultPlatform;
        }

        private Settings()
        {
            MasterBanks = new List<string>();
            Banks = new List<string>();
            BanksToLoad = new List<string>();
            RealChannelSettings = new List<Legacy.PlatformIntSetting>();
            VirtualChannelSettings = new List<Legacy.PlatformIntSetting>();
            LiveUpdateSettings = new List<Legacy.PlatformBoolSetting>();
            OverlaySettings = new List<Legacy.PlatformBoolSetting>();
            SampleRateSettings = new List<Legacy.PlatformIntSetting>();
            SpeakerModeSettings = new List<Legacy.PlatformIntSetting>();
            BankDirectorySettings = new List<Legacy.PlatformStringSetting>();

            ImportType = ImportType.StreamingAssets;
            AutomaticEventLoading = true;
            AutomaticSampleLoading = false;
            EnableMemoryTracking = false;
        }

        // Adds properties to a platform, thus revealing it in the UI.
        internal void AddPlatformProperties(Platform platform)
        {
            platform.AffirmProperties();
            LinkPlatformToParent(platform);
        }

#if UNITY_EDITOR
        internal void SetPlatformParent(Platform platform, Platform newParent)
        {
            if (editorSettings != null)
            {
                editorSettings.SetPlatformParent(platform, newParent);
            }
        }
#else
        public void SetPlatformParent(Platform platform, Platform newParent)
        {
            platform.Parent = newParent;
        }
#endif

        // A template for constructing a platform from an identifier.
        internal struct PlatformTemplate
        {
            public string Identifier;
            public Func<Platform> CreateInstance;
        };

        // Adds a platform to the collection of templates. Platforms register themselves by using
        // [InitializeOnLoad] and calling this function from a static constructor.
        internal static void AddPlatformTemplate<T>(string identifier) where T : Platform
        {
            PlatformTemplates.Add(new PlatformTemplate() {
                    Identifier = identifier,
                    CreateInstance = () => CreatePlatformInstance<T>(identifier)
                });
        }

        private static Platform CreatePlatformInstance<T>(string identifier) where T : Platform
        {
            Platform platform = CreateInstance<T>();
            platform.InitializeProperties();
            platform.Identifier = identifier;

            return platform;
        }

        internal void OnEnable()
        {
            if (hasLoaded)
            {
                // Already loaded
                return;
            }

            hasLoaded = true;

#if UNITY_EDITOR
            if (editorSettings != null)
            {
                // Clear the EditorSettings object in case it has not been reloaded (this can happen
                // if the settings asset is modified on disk).
                editorSettings.Clear();

                editorSettings.RuntimeSettings = this;
            }
#endif

            PopulatePlatformsFromAsset();

            DefaultPlatform = Platforms.FirstOrDefault(platform => platform is PlatformDefault);
            PlayInEditorPlatform = Platforms.FirstOrDefault(platform => platform is PlatformPlayInEditor);

#if UNITY_EDITOR
            if (editorSettings != null)
            {
                if (switchSettingsMigration == false)
                {
                    // Create Switch settings from the legacy Mobile settings, if they exist
                    Legacy.CopySetting(LiveUpdateSettings, Legacy.Platform.Mobile, Legacy.Platform.Switch);
                    Legacy.CopySetting(OverlaySettings, Legacy.Platform.Mobile, Legacy.Platform.Switch);

                    Legacy.CopySetting(RealChannelSettings, Legacy.Platform.Mobile, Legacy.Platform.Switch);
                    Legacy.CopySetting(VirtualChannelSettings, Legacy.Platform.Mobile, Legacy.Platform.Switch);
                    Legacy.CopySetting(SampleRateSettings, Legacy.Platform.Mobile, Legacy.Platform.Switch);
                    Legacy.CopySetting(SpeakerModeSettings, Legacy.Platform.Mobile, Legacy.Platform.Switch);
                    switchSettingsMigration = true;
                }

                // Fix up slashes for old settings meta data.
                SourceProjectPath = RuntimeUtils.GetCommonPlatformPath(SourceProjectPath);
                sourceBankPathUnformatted = RuntimeUtils.GetCommonPlatformPath(sourceBankPathUnformatted);

                // Remove the FMODStudioCache if in the old location
                string oldCache = "Assets/Plugins/FMOD/Resources/FMODStudioCache.asset";
                if (File.Exists(oldCache))
                {
                    AssetDatabase.DeleteAsset(oldCache);
                }

                editorSettings.AddMissingPlatforms();

                // Add all known platforms to the settings asset. We can only do this if the Settings
                // object is already in the asset database, which won't be the case if we're inside the
                // CreateInstance call in the Instance accessor above.
                if (AssetDatabase.Contains(this))
                {
                    editorSettings.AddPlatformsToAsset();
                }
            }
#endif

            // Link all known platforms
            Platforms.ForEach(LinkPlatform);
        }

        private void PopulatePlatformsFromAsset()
        {
            Platforms.Clear();

#if UNITY_EDITOR
            string assetPath = AssetDatabase.GetAssetPath(this);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Platform[] assetPlatforms = assets.OfType<Platform>().ToArray();
#else
            Platform[] assetPlatforms = Resources.LoadAll<Platform>(SettingsAssetName);
#endif

            foreach (Platform newPlatform in assetPlatforms)
            {
                Platform existingPlatform = FindPlatform(newPlatform.Identifier);

                if (existingPlatform != null)
                {
                    // Duplicate platform; clean one of them up
                    Platform platformToDestroy;

                    if (newPlatform.Active && !existingPlatform.Active)
                    {
                        RemovePlatform(existingPlatform.Identifier);

                        platformToDestroy = existingPlatform;
                        existingPlatform = null;
                    }
                    else
                    {
                        platformToDestroy = newPlatform;
                    }

                    RuntimeUtils.DebugLogWarningFormat("FMOD: Cleaning up duplicate platform: ID  = {0}, name = '{1}', type = {2}",
                        platformToDestroy.Identifier, platformToDestroy.DisplayName, platformToDestroy.GetType().Name);

                    DestroyImmediate(platformToDestroy, true);
                }

                if (existingPlatform == null)
                {
                    newPlatform.EnsurePropertiesAreValid();
                    AddPlatform(newPlatform);
                }
            }

#if UNITY_EDITOR
            // Remove any invalid child platforms (ie. deprecated platforms).
            foreach (Platform newPlatform in assetPlatforms)
            {
                if (newPlatform.ChildIdentifiers.RemoveAll(x => FindPlatform(x) == null) > 0)
                {
                    EditorUtility.SetDirty(newPlatform);
                }
            }

            if (editorSettings != null)
            {
                Platforms.ForEach(editorSettings.UpdateMigratedPlatform);
            }
#endif
        }
    }

    // This class stores data types and code used for migrating old settings.
    internal static class Legacy
    {
#if UNITY_EDITOR
        private const string RegisterStaticPluginsAssetPathRelative =
            "/Plugins/FMOD/Cache/fmod_register_static_plugins.cpp";
        private const string RegisterStaticPluginsAssetPathFull = "Assets" + RegisterStaticPluginsAssetPathRelative;

        public static void CleanTemporaryChanges()
        {
            CleanIl2CppArgs();
            CleanTemporaryFiles();
        }

        private static IEnumerable<string> AdditionalIl2CppFiles()
        {
            yield return Application.dataPath + RegisterStaticPluginsAssetPathRelative;
            yield return Application.dataPath + "/Plugins/FMOD/src/Runtime/fmod_static_plugin_support.h";
        }

        public static void CleanIl2CppArgs()
        {
            const string Il2CppCommand_AdditionalCpp = "--additional-cpp";

            string arguments = PlayerSettings.GetAdditionalIl2CppArgs();
            string newArguments = arguments;

            foreach (string path in AdditionalIl2CppFiles())
            {
                // Match on basename only in case the temp file location has moved
                string basename = Regex.Escape(Path.GetFileName(path));
                Regex regex = new Regex(Il2CppCommand_AdditionalCpp + "=\"[^\"]*" + basename + "\"");

                for (int startIndex = 0; startIndex < newArguments.Length; )
                {
                    Match match = regex.Match(newArguments, startIndex);

                    if (!match.Success)
                    {
                        break;
                    }

                    RuntimeUtils.DebugLogFormat("FMOD: Removing Il2CPP argument '{0}'", match.Value);

                    int matchStart = match.Index;
                    int matchEnd = match.Index + match.Length;

                    // Consume an adjacent space if there is one
                    if (matchStart > 0 && newArguments[matchStart - 1] == ' ')
                    {
                        --matchStart;
                    }
                    else if (matchEnd < newArguments.Length && newArguments[matchEnd] == ' ')
                    {
                        ++matchEnd;
                    }

                    newArguments = newArguments.Substring(0, matchStart) + newArguments.Substring(matchEnd);
                    startIndex = matchStart;
                }
            }

            if (newArguments != arguments)
            {
                PlayerSettings.SetAdditionalIl2CppArgs(newArguments);
            }
        }

        public static void CleanTemporaryFiles()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Messing with the asset database while entering play mode causes a NullReferenceException
                return;
            }

            string[] TemporaryFiles = {
                RegisterStaticPluginsAssetPathFull,
            };

            foreach (string path in TemporaryFiles)
            {
                if (Settings.EditorSettings != null)
                {
                    Settings.EditorSettings.DeleteTemporaryFile(path);
                }
            }
        }
#endif

        [Serializable]
        public enum Platform
        {
            None,
            PlayInEditor,
            Default,
            Desktop,
            Mobile,
            MobileHigh,
            MobileLow,
            Console,
            Windows,
            Mac,
            Linux,
            iOS,
            Android,
            Deprecated_1,
            XboxOne,
            PS4,
            Deprecated_2,
            Deprecated_3,
            AppleTV,
            UWP,
            Switch,
            WebGL,
            Deprecated_4,
            Reserved_1,
            Reserved_2,
            Reserved_3,
            Count,
        }

        public class PlatformSettingBase
        {
            public Platform Platform;
        }

        public class PlatformSetting<T> : PlatformSettingBase
        {
            public T Value;
        }

        [Serializable]
        public class PlatformIntSetting : PlatformSetting<int>
        {
        }

        [Serializable]
        public class PlatformStringSetting : PlatformSetting<string>
        {
        }

        [Serializable]
        public class PlatformBoolSetting : PlatformSetting<TriStateBool>
        {
        }

        // Copies a setting from one platform to another.
        public static void CopySetting<T, U>(List<T> list, Platform fromPlatform, Platform toPlatform)
            where T : PlatformSetting<U>, new()
        {
            T fromSetting = list.Find((x) => x.Platform == fromPlatform);
            T toSetting = list.Find((x) => x.Platform == toPlatform);

            if (fromSetting != null)
            {
                if (toSetting == null)
                {
                    toSetting = new T() { Platform = toPlatform };
                    list.Add(toSetting);
                }

                toSetting.Value = fromSetting.Value;
            }
            else if (toSetting != null)
            {
                list.Remove(toSetting);
            }
        }

        public static void CopySetting(List<PlatformBoolSetting> list, Platform fromPlatform, Platform toPlatform)
        {
            CopySetting<PlatformBoolSetting, TriStateBool>(list, fromPlatform, toPlatform);
        }

        public static void CopySetting(List<PlatformIntSetting> list, Platform fromPlatform, Platform toPlatform)
        {
            CopySetting<PlatformIntSetting, int>(list, fromPlatform, toPlatform);
        }

        // Returns the UI display name for the given platform.
        public static string DisplayName(Platform platform)
        {
            switch (platform)
            {
                case Platform.Linux:
                    return "Linux";
                case Platform.Desktop:
                    return "Desktop";
                case Platform.Console:
                    return "Console";
                case Platform.iOS:
                    return "iOS";
                case Platform.Mac:
                    return "OSX";
                case Platform.Mobile:
                    return "Mobile";
                case Platform.PS4:
                    return "PS4";
                case Platform.Windows:
                    return "Windows";
                case Platform.UWP:
                    return "UWP";
                case Platform.XboxOne:
                    return "XBox One";
                case Platform.Android:
                    return "Android";
                case Platform.AppleTV:
                    return "Apple TV";
                case Platform.MobileHigh:
                    return "High-End Mobile";
                case Platform.MobileLow:
                    return "Low-End Mobile";
                case Platform.Switch:
                    return "Switch";
                case Platform.WebGL:
                    return "WebGL";
            }
            return "Unknown";
        }

        // Returns the UI sort order for the given platform.
        public static float SortOrder(Platform legacyPlatform)
        {
            switch (legacyPlatform)
            {
                case Platform.Desktop:
                    return 1;
                case Platform.Windows:
                    return 1.1f;
                case Platform.Mac:
                    return 1.2f;
                case Platform.Linux:
                    return 1.3f;
                case Platform.Mobile:
                    return 2;
                case Platform.MobileHigh:
                    return 2.1f;
                case Platform.MobileLow:
                    return 2.2f;
                case Platform.AppleTV:
                    return 2.3f;
                case Platform.Console:
                    return 3;
                case Platform.XboxOne:
                    return 3.1f;
                case Platform.PS4:
                    return 3.2f;
                case Platform.Switch:
                    return 3.3f;
                default:
                    return 0;
            }
        }

        // Returns the parent for the given platform.
        public static Platform Parent(Platform platform)
        {
            switch (platform)
            {
                case Platform.Windows:
                case Platform.Linux:
                case Platform.Mac:
                case Platform.UWP:
                case Platform.WebGL:
                    return Platform.Desktop;
                case Platform.MobileHigh:
                case Platform.MobileLow:
                case Platform.iOS:
                case Platform.Android:
                case Platform.AppleTV:
                    return Platform.Mobile;
                case Platform.Switch:
                case Platform.XboxOne:
                case Platform.PS4:
                case Platform.Reserved_1:
                case Platform.Reserved_2:
                case Platform.Reserved_3:
                    return Platform.Console;
                case Platform.Desktop:
                case Platform.Console:
                case Platform.Mobile:
                    return Platform.Default;
                case Platform.PlayInEditor:
                case Platform.Default:
                default:
                    return Platform.None;
            }
        }

        // Determines whether the given platform is a group
        public static bool IsGroup(Platform platform)
        {
            switch (platform)
            {
                case Platform.Desktop:
                case Platform.Mobile:
                case Platform.Console:
                    return true;
                default:
                    return false;
            }
        }
    }
}
