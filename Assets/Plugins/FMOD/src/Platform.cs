using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMOD
{
    public partial class VERSION
    {
#if DEVELOPMENT_BUILD
        public const string dllSuffix = "L";
#else
        public const string dllSuffix = "";
#endif
    }
}

namespace FMOD.Studio
{
    public partial class STUDIO_VERSION
    {
#if DEVELOPMENT_BUILD
        public const string dllSuffix = "L";
#else
        public const string dllSuffix = "";
#endif
    }
}

namespace FMODUnity
{
    public class PlatformCallbackHandler : ScriptableObject
    {
        // A hook for custom initialization logic. RuntimeManager.Initialize calls this
        // just before calling system.Initialize.
        // Call reportResult() with the result of each FMOD call to use FMOD's error handling logic.
        public virtual void PreInitialize(FMOD.Studio.System system, Action<FMOD.RESULT, string> reportResult)
        {
        }
    }

    // This class holds per-platform settings and provides hooks for platform-specific behaviour.
    // Each platform has a parent platform, forming a hierarchy that is rooted at PlatformDefault.
    // By default a platform inherits all of its properties from its parent platform; this behaviour
    // can be overridden for each property.
    //
    // There is at least one concrete derived class for each supported platform; these classes use
    // [InitializeOnLoad] and a static constructor to register themselves as supported platforms by
    // calling Settings.AddPlatformTemplate. The user can also create instances of the PlatformGroup
    // class and use them to group platforms that have settings in common.
    public abstract class Platform : ScriptableObject
    {
        internal const float DefaultPriority = 0;

#if UNITY_EDITOR
        internal const int MaximumCoreCount = 16;

        internal static readonly FileLayout[] OldFileLayouts = {
            FileLayout.Release_1_10,
            FileLayout.Release_2_0,
            FileLayout.Release_2_1,
        };
#endif

        // These need to match the function called by LoadStaticPlugins
        internal const string RegisterStaticPluginsClassName = "StaticPluginManager";
        internal const string RegisterStaticPluginsFunctionName = "Register";

        // This is a persistent identifier. It is used:
        // * To link platforms together at load time
        // * To avoid creating duplicate platforms from templates (in Settings.OnEnable)
        // * As a key for SettingsEditor UI state
        // It should be kept stable for concrete platforms (like PlatformWindows) to support
        // settings migration in the future.
        [SerializeField]
        private string identifier;

        [SerializeField]
        private string parentIdentifier;

        [SerializeField]
        private bool active = false;

        [SerializeField]
        protected PropertyStorage Properties = new PropertyStorage();

        [SerializeField]
        [FormerlySerializedAs("outputType")]
        internal string OutputTypeName;

        private static List<ThreadAffinityGroup> StaticThreadAffinities = new List<ThreadAffinityGroup>();

        [SerializeField]
        private PropertyThreadAffinityList threadAffinities = new PropertyThreadAffinityList();

#if UNITY_EDITOR
        [SerializeField]
        private float displaySortOrder;

        [SerializeField]
        private List<string> childIdentifiers = new List<string>();
#else
        // The parent platform from which this platform inherits its property values.
        [NonSerialized]
        public Platform Parent;
#endif

        internal string Identifier
        {
            get
            {
                return identifier;
            }

            set
            {
                identifier = value;
            }
        }

        // The display name to show for this platform in the UI.
        internal abstract string DisplayName { get; }

        // Declares the Unity RuntimePlatforms this platform implements.
        internal abstract void DeclareRuntimePlatforms(Settings settings);

#if UNITY_EDITOR
        // The Unity BuildTargets this platform implements.
        // Returns BuildTarget.NoTarget if the correct value is not defined, as some BuildTarget
        // values are only defined in specific circumstances (e.g. Stadia required Unity 2019.3).
        internal abstract IEnumerable<BuildTarget> GetBuildTargets();

        // The old FMOD platform identifier that this platform corresponds to, for settings migration.
        internal abstract Legacy.Platform LegacyIdentifier { get; }
#endif

        // The priority to use when finding a platform to support the current Unity runtime
        // platform (higher priorities are tried first).
        internal virtual float Priority { get { return DefaultPriority; } }

        // Determines whether this platform matches the current environment. When more than one
        // platform implements the current Unity runtime platform, FMOD for Unity will use the
        // highest-priority platform that returns true from MatchesCurrentEnvironment.
        internal virtual bool MatchesCurrentEnvironment { get { return true; } }

        // Whether this platform is a fixed part of the FMOD for Unity settings, or can be
        // added/removed by the user.
        internal virtual bool IsIntrinsic { get { return false; } }

        // A hook for platform-specific initialization logic. RuntimeManager.Initialize calls this
        // before calling FMOD.Studio.System.create.
        internal virtual void PreSystemCreate(Action<FMOD.RESULT, string> reportResult)
        {
        }

        // A hook for platform-specific initialization logic. RuntimeManager.Initialize calls this
        // just before calling studioSystem.Initialize.
        internal virtual void PreInitialize(FMOD.Studio.System studioSystem)
        {
        }

        // The folder in which FMOD .bank files are stored. Used when loading banks.
        internal virtual string GetBankFolder()
        {
            return Application.streamingAssetsPath;
        }

#if UNITY_EDITOR
        [Flags]
        public enum BinaryType
        {
            Release = 1,
            Logging = 2,
            Optional = 4,
            AllVariants = 8,
            All = Release | Logging | Optional | AllVariants
        }

        protected virtual IEnumerable<string> GetBinaryPaths(BuildTarget buildTarget, BinaryType binaryType, string prefix)
        {
            foreach (BinaryFileInfo info in GetBinaryFileInfo(buildTarget, binaryType))
            {
                yield return info.LatestLocation();
            }
        }

        internal abstract class FileInfo
        {
            public FileInfo(FileRecord fileRecord, BinaryType type)
            {
                this.fileRecord = fileRecord;
                this.type = type;
            }

            public readonly BinaryType type;

            private readonly FileRecord fileRecord;

            public string LatestLocation()
            {
                return GetLocation(FileLayout.Latest);
            }

            public IEnumerable<string> OldLocations()
            {
                foreach (FileLayout layout in OldFileLayouts)
                {
                    string location = GetLocation(layout);

                    if (location != null)
                    {
                        yield return location;
                    }
                }
            }

            private string GetLocation(FileLayout layout)
            {
                string basePath = GetBasePath(layout);

                if (basePath == null)
                {
                    return null;
                }

                bool absolute;
                string path;
                fileRecord.GetPath(layout, out path, out absolute);

                if (absolute)
                {
                    return path;
                }
                else
                {
                    return string.Format("{0}/{1}", basePath, path);
                }
            }

            protected abstract string GetBasePath(FileLayout layout);
        }

        internal class BinaryFileInfo : FileInfo
        {
            public BinaryFileInfo(Platform platform, FileRecord fileRecord, BuildTarget buildTarget, BinaryType type)
                : base(fileRecord, type)
            {
                this.platform = platform;
                this.buildTarget = buildTarget;
            }

            private readonly Platform platform;
            private readonly BuildTarget buildTarget;

            protected override string GetBasePath(FileLayout layout)
            {
                BinaryAssetFolderInfo info = platform.GetBinaryAssetFolder(buildTarget);

                if (layout < info.oldestLayout)
                {
                    return null;
                }

                switch (layout)
                {
                    case FileLayout.Release_1_10:
                        return info.path_1_10;
                    case FileLayout.Release_2_0:
                        return string.Format("Assets/Plugins/FMOD/lib/{0}", info.baseName);
                    case FileLayout.Release_2_1:
                    case FileLayout.Release_2_2:
                        return $"{RuntimeUtils.PluginBasePath}/platforms/{info.baseName}/lib";
                    default:
                        throw new ArgumentException("Unrecognised file layout: " + layout);
                }
            }
        }

        public struct FileRecord
        {
            public FileRecord(string latestPath)
            {
                this.latestPath = latestPath;
                pathVersions = null;
            }

            public FileRecord WithAbsoluteVersion(FileLayout layout, string path)
            {
                AddVersion(layout, path, true);
                return this;
            }

            public FileRecord WithRelativeVersion(FileLayout layout, string path)
            {
                AddVersion(layout, path, false);
                return this;
            }

            private void AddVersion(FileLayout layout, string path, bool absolute)
            {
                if (pathVersions == null)
                {
                    pathVersions = new Dictionary<FileLayout, PathInfo>();
                }

                pathVersions.Add(layout, new PathInfo() { path = path, absolute = absolute });
            }

            public void GetPath(FileLayout layout, out string path, out bool absolute)
            {
                if (pathVersions != null)
                {
                    PathInfo pathForLayout;

                    if (pathVersions.TryGetValue(layout, out pathForLayout))
                    {
                        absolute = pathForLayout.absolute;
                        path = pathForLayout.path;
                        return;
                    }
                }

                absolute = false;
                path = latestPath;
            }

            private struct PathInfo
            {
                public string path;
                public bool absolute;
            }

            private readonly string latestPath;
            private Dictionary<FileLayout, PathInfo> pathVersions;
        }

        internal IEnumerable<BinaryFileInfo> GetBinaryFileInfo(BuildTarget buildTarget, BinaryType binaryType)
        {
            bool allVariants = (binaryType & BinaryType.AllVariants) == BinaryType.AllVariants;

            if ((binaryType & BinaryType.Release) == BinaryType.Release)
            {
                foreach (FileRecord record in GetBinaryFiles(buildTarget, allVariants, ""))
                {
                    yield return CreateFileInfo(record, buildTarget, BinaryType.Release);
                }
            }

            if ((binaryType & BinaryType.Logging) == BinaryType.Logging)
            {
                foreach (FileRecord record in GetBinaryFiles(buildTarget, allVariants, "L"))
                {
                    yield return CreateFileInfo(record, buildTarget, BinaryType.Logging);
                }
            }

            if ((binaryType & BinaryType.Optional) == BinaryType.Optional)
            {
                foreach (FileRecord record in GetOptionalBinaryFiles(buildTarget, allVariants))
                {
                    yield return CreateFileInfo(record, buildTarget, BinaryType.Optional);
                }
            }
        }

        internal class SourceFileInfo : FileInfo
        {
            public SourceFileInfo(Platform platform, FileRecord fileRecord)
                : base(fileRecord, BinaryType.Optional)
            {
                this.platform = platform;
            }

            private readonly Platform platform;

            protected override string GetBasePath(FileLayout layout)
            {
                BinaryAssetFolderInfo info = platform.GetBinaryAssetFolder(platform.GetBuildTargets().First());

                if (layout < info.oldestLayout)
                {
                    return null;
                }

                switch (layout)
                {
                    case FileLayout.Release_1_10:
                        return "Plugins/FMOD/Wrapper";
                    case FileLayout.Release_2_0:
                        return "Plugins/FMOD/src/Runtime/wrapper";
                    case FileLayout.Release_2_1:
                    case FileLayout.Release_2_2:
                        return $"{RuntimeUtils.PluginBasePath}/platforms/{info.baseName}/src";
                    default:
                        throw new ArgumentException("Unrecognised file layout: " + layout);
                }
            }
        }

        internal IEnumerable<SourceFileInfo> GetSourceFileInfo()
        {
            foreach (FileRecord record in GetSourceFiles())
            {
                yield return new SourceFileInfo(this, record);
            }
        }

        internal BinaryFileInfo CreateFileInfo(FileRecord record, BuildTarget buildTarget, BinaryType binaryType)
        {
            return new BinaryFileInfo(this, record, buildTarget, binaryType);
        }

        internal virtual IEnumerable<string> GetObsoleteAssetPaths()
        {
            foreach (string path in GetObsoleteFiles())
            {
                yield return $"{RuntimeUtils.PluginBasePath}/{path}";
                yield return $"{RuntimeUtils.PluginBasePathDefault}/{path}";
            }
        }

        // Called by Settings.CanBuildTarget to get the required binaries for the current
        // build target and logging state.
        internal virtual IEnumerable<string> GetBinaryFilePaths(BuildTarget buildTarget, BinaryType binaryType)
        {
            return GetBinaryPaths(buildTarget, binaryType, RuntimeUtils.PluginBasePath);
        }

        // Called by Settings.SelectBinaries to get:
        // * The required and optional binaries for the current build target and logging state;
        //   these get enabled.
        // * All binaries; any that weren't enabled in the previous step get disabled.
        internal virtual IEnumerable<string> GetBinaryAssetPaths(BuildTarget buildTarget, BinaryType binaryType)
        {
            return GetBinaryPaths(buildTarget, binaryType, RuntimeUtils.PluginBasePath);
        }

        public enum FileLayout : uint
        {
            Release_1_10,
            Release_2_0,
            Release_2_1,
            Release_2_2,
            Latest = Release_2_2,
        }

        protected class BinaryAssetFolderInfo
        {
            public BinaryAssetFolderInfo(string baseName, string path_1_10)
            {
                this.baseName = baseName;
                this.path_1_10 = path_1_10;
                this.oldestLayout = FileLayout.Release_1_10;
            }

            public BinaryAssetFolderInfo(string baseName, FileLayout oldestLayout)
            {
                this.baseName = baseName;
                this.path_1_10 = null;
                this.oldestLayout = oldestLayout;
            }

            public string baseName { get; private set; }
            public string path_1_10 { get; private set; }
            public FileLayout oldestLayout { get; private set; }
        }

        protected abstract BinaryAssetFolderInfo GetBinaryAssetFolder(BuildTarget buildTarget);

        protected abstract IEnumerable<FileRecord> GetBinaryFiles(BuildTarget buildTarget, bool allVariants, string suffix);

        protected virtual IEnumerable<FileRecord> GetOptionalBinaryFiles(BuildTarget buildTarget, bool allVariants)
        {
            yield break;
        }

        protected virtual IEnumerable<FileRecord> GetSourceFiles()
        {
            yield break;
        }

        protected virtual IEnumerable<string> GetObsoleteFiles()
        {
            yield break;
        }

        internal virtual bool IsFMODStaticallyLinked { get { return false; } }

        internal virtual bool SupportsAdditionalCPP(BuildTarget target)
        {
            return true;
        }
#endif

        // The base path for FMOD plugins when in a standalone player.
        protected virtual string GetPluginBasePath()
        {
            return string.Format("{0}/Plugins", Application.dataPath);
        }

        // Returns the full path for an FMOD plugin.
        internal virtual string GetPluginPath(string pluginName)
        {
            throw new NotImplementedException(string.Format("Plugins are not implemented on platform {0}", Identifier));
        }

        // Loads static and dynamic FMOD plugins for this platform.
        internal virtual void LoadPlugins(FMOD.System coreSystem, Action<FMOD.RESULT, string> reportResult)
        {
            LoadDynamicPlugins(coreSystem, reportResult);
            LoadStaticPlugins(coreSystem, reportResult);
        }

        // Loads dynamic FMOD plugins for this platform.
        internal virtual void LoadDynamicPlugins(FMOD.System coreSystem, Action<FMOD.RESULT, string> reportResult)
        {
            List<string> pluginNames = Plugins;

            if (pluginNames == null)
            {
                return;
            }

            foreach (string pluginName in pluginNames)
            {
                if (string.IsNullOrEmpty(pluginName))
                {
                    continue;
                }

                string pluginPath = GetPluginPath(pluginName);
                uint handle;

                FMOD.RESULT result = coreSystem.loadPlugin(pluginPath, out handle);

#if UNITY_64 || UNITY_EDITOR_64
                // Add a "64" suffix and try again
                if (result == FMOD.RESULT.ERR_FILE_BAD || result == FMOD.RESULT.ERR_FILE_NOTFOUND)
                {
                    string pluginPath64 = GetPluginPath(pluginName + "64");
                    result = coreSystem.loadPlugin(pluginPath64, out handle);
                }
#endif

                reportResult(result, string.Format("Loading plugin '{0}' from '{1}'", pluginName, pluginPath));
            }
        }

        // Loads static FMOD plugins for this platform.
        internal virtual void LoadStaticPlugins(FMOD.System coreSystem, Action<FMOD.RESULT, string> reportResult)
        {
            if (StaticPlugins.Count > 0)
            {
#if !UNITY_EDITOR && ENABLE_IL2CPP
                // We use reflection here to avoid compile errors if the plugin registration code doesn't exist.
                // It should be generated by Settings.PreprocessStaticPlugins(), which is called from
                // IPreprocessBuildWithReport.OnPreprocessBuild(). However, some compilation scenarios
                // (such as AddressableAssetSettings.BuildPlayerContent()) don't call OnPreprocessBuild(),
                // so we can't generate the plugin registration code.

                string className = string.Format("FMODUnity.{0}", RegisterStaticPluginsClassName);
                Type type = Type.GetType(className);

                if (type == null)
                {
                    RuntimeUtils.DebugLogWarningFormat(
                        "FMOD: {0} static plugins specified, but the {1} class was not found.",
                        StaticPlugins.Count, className);
                    return;
                }

                MethodInfo method = type.GetMethod(RegisterStaticPluginsFunctionName,
                    BindingFlags.Public | BindingFlags.Static);

                if (method == null)
                {
                    RuntimeUtils.DebugLogWarningFormat(
                        "FMOD: {0} static plugins specified, but the {1}.{2} method was not found.",
                        StaticPlugins.Count, className, RegisterStaticPluginsFunctionName);
                    return;
                }

                method.Invoke(null, new object[] { coreSystem, reportResult });
#else
                RuntimeUtils.DebugLogWarningFormat(
                    "FMOD: {0} static plugins specified, but static plugins are only supported on the IL2CPP scripting backend",
                    StaticPlugins.Count);
#endif
            }
        }

        // Ensures that this platform has properties.
        internal void AffirmProperties()
        {
            if (!active)
            {
                Properties = new PropertyStorage();
                InitializeProperties();
                active = true;
            }
        }

        // Clears this platform's properties.
        internal void ClearProperties()
        {
            if (active)
            {
                Properties = new PropertyStorage();
                active = false;
#if UNITY_EDITOR
                DisplaySortOrder = 0;
#endif
            }
        }

        // Initializes this platform's properties to their default values.
        internal virtual void InitializeProperties()
        {
            if (!IsIntrinsic)
            {
                ParentIdentifier = PlatformDefault.ConstIdentifier;
            }
        }

        // Ensures that this platform's properties are valid after loading from file.
        internal virtual void EnsurePropertiesAreValid()
        {
            if (!IsIntrinsic && string.IsNullOrEmpty(ParentIdentifier))
            {
                ParentIdentifier = PlatformDefault.ConstIdentifier;
            }
        }

        internal string ParentIdentifier
        {
            get
            {
                return parentIdentifier;
            }

            set
            {
                parentIdentifier = value;
            }
        }

#if UNITY_EDITOR
        internal float DisplaySortOrder
        {
            get
            {
                return displaySortOrder;
            }

            set
            {
                displaySortOrder = value;
            }
        }
#endif

        internal bool IsLiveUpdateEnabled
        {
            get
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                return LiveUpdate != TriStateBool.Disabled;
#else
                return LiveUpdate == TriStateBool.Enabled;
#endif
            }
        }

        internal bool IsOverlayEnabled
        {
            get
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                return Overlay != TriStateBool.Disabled;
#else
                return Overlay == TriStateBool.Enabled;
#endif
            }
        }

        // A property value that can be inherited from the parent or overridden.
        public class Property<T>
        {
            public T Value;
            public bool HasValue;
        }

        // These stub classes are needed because Unity can't serialize generic classes
        [Serializable]
        public class PropertyBool : Property<TriStateBool>
        {
        }

        [Serializable]
        public class PropertyScreenPosition : Property<ScreenPosition>
        {
        }

        [Serializable]
        public class PropertyInt : Property<int>
        {
        }

        [Serializable]
        public class PropertySpeakerMode : Property<FMOD.SPEAKERMODE>
        {
        }

        [Serializable]
        public class PropertyString : Property<string>
        {
        }

        [Serializable]
        public class PropertyStringList : Property<List<string>>
        {
        }

        [Serializable]
        public class PropertyCallbackHandler : Property<PlatformCallbackHandler>
        {
        }

        internal interface PropertyOverrideControl
        {
            bool HasValue(Platform platform);
            void Clear(Platform platform);
        }

        // This class provides access to a specific property on any Platform object; the property to
        // operate on is determined by the Getter function. This allows client code to operate on
        // platform properties in a generic manner.
        internal struct PropertyAccessor<T> : PropertyOverrideControl
        {
            private readonly Func<PropertyStorage, Property<T>> Getter;
            private readonly T DefaultValue;

            public PropertyAccessor(Func<PropertyStorage, Property<T>> getter, T defaultValue)
            {
                Getter = getter;
                DefaultValue = defaultValue;
            }

            // Determine whether the property has a value in the given platform, or is inherited
            // from the parent.
            public bool HasValue(Platform platform)
            {
                return platform.Active && Getter(platform.Properties).HasValue;
            }

            // Get the (possibly inherited) value of the property for the given platform.
            public T Get(Platform platform)
            {
                for (Platform current = platform; current != null; current = current.Parent)
                {
                    if (current.Active)
                    {
                        Property<T> property = Getter(current.Properties);

                        if (property.HasValue)
                        {
                            return property.Value;
                        }
                    }
                }

#if UNITY_EDITOR
                if (platform is PlatformPlayInEditor)
                {
                    return Get(Settings.EditorSettings.CurrentEditorPlatform);
                }
#endif

                return DefaultValue;
            }

            // Set the value of the property in the given platform, so it is not inherited from the
            // platform's parent.
            public void Set(Platform platform, T value)
            {
                Property<T> property = Getter(platform.Properties);

                property.Value = value;
                property.HasValue = true;
            }

            // Clear the value of the property in the given platform, so it is inherited from the
            // platform's parent.
            public void Clear(Platform platform)
            {
                Getter(platform.Properties).HasValue = false;
            }
        }

        // This class stores all of the inheritable properties for a platform.
        [Serializable]
        public class PropertyStorage
        {
            public PropertyBool LiveUpdate = new PropertyBool();
            public PropertyInt LiveUpdatePort = new PropertyInt();
            public PropertyBool Overlay = new PropertyBool();
            public PropertyScreenPosition OverlayPosition = new PropertyScreenPosition();
            public PropertyInt OverlayFontSize = new PropertyInt();
            public PropertyBool Logging = new PropertyBool();
            public PropertyInt SampleRate = new PropertyInt();
            public PropertyString BuildDirectory = new PropertyString();
            public PropertySpeakerMode SpeakerMode = new PropertySpeakerMode();
            public PropertyInt VirtualChannelCount = new PropertyInt();
            public PropertyInt RealChannelCount = new PropertyInt();
            public PropertyInt DSPBufferLength = new PropertyInt();
            public PropertyInt DSPBufferCount = new PropertyInt();
            public PropertyStringList Plugins = new PropertyStringList();
            public PropertyStringList StaticPlugins = new PropertyStringList();
            public PropertyCallbackHandler CallbackHandler = new PropertyCallbackHandler();
        }

        // Whether this platform is active in the settings UI.
        internal bool Active { get { return active; } }

        // Whether this platform has any properties that are not inherited from the parent.
        internal bool HasAnyOverriddenProperties
        {
            get
            {
                return active &&
                    (
                        Properties.LiveUpdate.HasValue
                        || Properties.LiveUpdatePort.HasValue
                        || Properties.Overlay.HasValue
                        || Properties.OverlayPosition.HasValue
                        || Properties.OverlayFontSize.HasValue
                        || Properties.Logging.HasValue
                        || Properties.SampleRate.HasValue
                        || Properties.BuildDirectory.HasValue
                        || Properties.SpeakerMode.HasValue
                        || Properties.VirtualChannelCount.HasValue
                        || Properties.RealChannelCount.HasValue
                        || Properties.DSPBufferLength.HasValue
                        || Properties.DSPBufferCount.HasValue
                        || Properties.Plugins.HasValue
                        || Properties.StaticPlugins.HasValue
                    );
            }
        }

        // These accessors provide (possibly inherited) property values.
        public TriStateBool LiveUpdate { get { return PropertyAccessors.LiveUpdate.Get(this); } }
        public int LiveUpdatePort { get { return PropertyAccessors.LiveUpdatePort.Get(this); } }
        public TriStateBool Overlay { get { return PropertyAccessors.Overlay.Get(this); } }
        public ScreenPosition OverlayRect { get { return PropertyAccessors.OverlayPosition.Get(this); } }
        public int OverlayFontSize { get { return PropertyAccessors.OverlayFontSize.Get(this); } }
        public void SetOverlayFontSize(int size) { PropertyAccessors.OverlayFontSize.Set(this, size); }
        public TriStateBool Logging { get { return PropertyAccessors.Logging.Get(this); } }
        public int SampleRate { get { return PropertyAccessors.SampleRate.Get(this); } }
        public string BuildDirectory { get { return PropertyAccessors.BuildDirectory.Get(this); } }
        public FMOD.SPEAKERMODE SpeakerMode { get { return PropertyAccessors.SpeakerMode.Get(this); } }
        public int VirtualChannelCount { get { return PropertyAccessors.VirtualChannelCount.Get(this); } }
        public int RealChannelCount { get { return PropertyAccessors.RealChannelCount.Get(this); } }
        public int DSPBufferLength { get { return PropertyAccessors.DSPBufferLength.Get(this); } }
        public int DSPBufferCount { get { return PropertyAccessors.DSPBufferCount.Get(this); } }
        public List<string> Plugins { get { return PropertyAccessors.Plugins.Get(this); } }
        public List<string> StaticPlugins { get { return PropertyAccessors.StaticPlugins.Get(this); } }
        public PlatformCallbackHandler CallbackHandler { get { return PropertyAccessors.CallbackHandler.Get(this); } }

        // These accessors provide full access to properties.
        internal static class PropertyAccessors
        {
            public static readonly PropertyAccessor<TriStateBool> LiveUpdate
                    = new PropertyAccessor<TriStateBool>(properties => properties.LiveUpdate, TriStateBool.Disabled);

            public static readonly PropertyAccessor<int> LiveUpdatePort
                    = new PropertyAccessor<int>(properties => properties.LiveUpdatePort, 9264);

            public static readonly PropertyAccessor<TriStateBool> Overlay
                    = new PropertyAccessor<TriStateBool>(properties => properties.Overlay, TriStateBool.Disabled);

            public static readonly PropertyAccessor<ScreenPosition> OverlayPosition = new PropertyAccessor<ScreenPosition>(properties => properties.OverlayPosition, ScreenPosition.TopLeft);

            public static readonly PropertyAccessor<int> OverlayFontSize = new PropertyAccessor<int>(properties => properties.OverlayFontSize, 14);

            public static readonly PropertyAccessor<TriStateBool> Logging
                    = new PropertyAccessor<TriStateBool>(properties => properties.Logging, TriStateBool.Disabled);

            public static readonly PropertyAccessor<int> SampleRate
                    = new PropertyAccessor<int>(properties => properties.SampleRate, 0);

            public static readonly PropertyAccessor<string> BuildDirectory
                    = new PropertyAccessor<string>(properties => properties.BuildDirectory, "Desktop");

            public static readonly PropertyAccessor<FMOD.SPEAKERMODE> SpeakerMode
                    = new PropertyAccessor<FMOD.SPEAKERMODE>(properties => properties.SpeakerMode, FMOD.SPEAKERMODE.STEREO);

            public static readonly PropertyAccessor<int> VirtualChannelCount
                    = new PropertyAccessor<int>(properties => properties.VirtualChannelCount, 128);

            public static readonly PropertyAccessor<int> RealChannelCount
                    = new PropertyAccessor<int>(properties => properties.RealChannelCount, 32);

            public static readonly PropertyAccessor<int> DSPBufferLength
                    = new PropertyAccessor<int>(properties => properties.DSPBufferLength, 0);

            public static readonly PropertyAccessor<int> DSPBufferCount
                    = new PropertyAccessor<int>(properties => properties.DSPBufferCount, 0);

            public static readonly PropertyAccessor<List<string>> Plugins
                    = new PropertyAccessor<List<string>>(properties => properties.Plugins, null);

            public static readonly PropertyAccessor<List<string>> StaticPlugins
                    = new PropertyAccessor<List<string>>(properties => properties.StaticPlugins, null);

            public static readonly PropertyAccessor<PlatformCallbackHandler> CallbackHandler
                    = new PropertyAccessor<PlatformCallbackHandler>(properties => properties.CallbackHandler, null);
        }

#if UNITY_EDITOR
        // The parent platform from which this platform inherits its property values.
        internal Platform Parent
        {
            get
            {
                return (ParentIdentifier != null) ? Settings.Instance.FindPlatform(ParentIdentifier) : null;
            }
        }

        // The platforms which inherit their property values from this platform.
        internal List<string> ChildIdentifiers { get { return childIdentifiers; } }
#endif

        // Checks whether this platform inherits from the given platform, so we can avoid creating
        // inheritance loops.
        internal bool InheritsFrom(Platform platform)
        {
            if (platform == this)
            {
                return true;
            }
            else if (Parent != null)
            {
                return Parent.InheritsFrom(platform);
            }
            else
            {
                return false;
            }
        }

        internal FMOD.OUTPUTTYPE GetOutputType()
        {
            if (Enum.IsDefined(typeof(FMOD.OUTPUTTYPE), OutputTypeName))
            {
                return (FMOD.OUTPUTTYPE)Enum.Parse(typeof(FMOD.OUTPUTTYPE), OutputTypeName);
            }
            return FMOD.OUTPUTTYPE.AUTODETECT;
        }

#if UNITY_EDITOR
        public struct OutputType
        {
            public string displayName;
            public FMOD.OUTPUTTYPE outputType;
        }

        internal abstract OutputType[] ValidOutputTypes { get; }

        internal virtual int CoreCount { get { return 0; } }
#endif

        internal virtual List<ThreadAffinityGroup> DefaultThreadAffinities { get { return StaticThreadAffinities; } }

        [Serializable]
        public class PropertyThreadAffinityList : Property<List<ThreadAffinityGroup>>
        {
        }

        public IEnumerable<ThreadAffinityGroup> ThreadAffinities
        {
            get
            {
                if (threadAffinities.HasValue)
                {
                    return threadAffinities.Value;
                }
                else
                {
                    return DefaultThreadAffinities;
                }
            }
        }

        internal PropertyThreadAffinityList ThreadAffinitiesProperty { get { return threadAffinities; } }

        internal virtual List<CodecChannelCount> DefaultCodecChannels { get { return staticCodecChannels; } }

        private static List<CodecChannelCount> staticCodecChannels = new List<CodecChannelCount>()
        {
            new CodecChannelCount { format = CodecType.FADPCM, channels = 32 },
            new CodecChannelCount { format = CodecType.Vorbis, channels = 0 },
        };

        [Serializable]
        internal class PropertyCodecChannels : Property<List<CodecChannelCount>>
        {
        }

        [SerializeField]
        private PropertyCodecChannels codecChannels = new PropertyCodecChannels();

        internal List<CodecChannelCount> CodecChannels
        {
            get
            {
                if (codecChannels.HasValue)
                {
                    return codecChannels.Value;
                }
                else
                {
                    return DefaultCodecChannels;
                }
            }
        }

        internal PropertyCodecChannels CodecChannelsProperty { get { return codecChannels; } }
    }
}
