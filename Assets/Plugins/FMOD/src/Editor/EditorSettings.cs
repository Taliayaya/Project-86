using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FMODUnity
{
    [InitializeOnLoad]
    public class EditorSettings : IEditorSettings
    {
        static EditorSettings()
        {
            Settings.EditorSettings = new EditorSettings();
        }

        public const string DownloadURL = "https://www.fmod.com/download";

        // This is used to find the platform that implements the current Unity build target.
        private Dictionary<BuildTarget, Platform> PlatformForBuildTarget = new Dictionary<BuildTarget, Platform>();

        private static string FMODFolderFull => RuntimeUtils.PluginBasePath;

        private const string CacheFolderName = "Cache";
        private static string CacheFolderRelative => $"{RuntimeUtils.PluginBasePath}/{CacheFolderName}";
        private static string CacheFolderFull => $"{FMODFolderFull}/{CacheFolderName}";

        private const string RegisterStaticPluginsFile = "RegisterStaticPlugins.cs";
        private static string RegisterStaticPluginsAssetPathRelative => $"{CacheFolderRelative}/{RegisterStaticPluginsFile}";
        private static string RegisterStaticPluginsAssetPathFull => $"{CacheFolderFull}/{RegisterStaticPluginsFile}";

        [NonSerialized]
        private Dictionary<string, bool> binaryCompatibilitiesBeforeBuild;

        public static EditorSettings Instance
        {
            get
            {
                return Settings.EditorSettings as EditorSettings;
            }
        }

        public Settings RuntimeSettings { get; set; }

        [MenuItem("FMOD/Edit Settings", priority = 0)]
        public static void EditSettings()
        {
            Selection.activeObject = Settings.Instance;
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        public void Clear()
        {
            PlatformForBuildTarget.Clear();
            binaryCompatibilitiesBeforeBuild = null;
        }

        public void CreateSettingsAsset(string assetName)
        {
            string resourcesPath = $"{FMODFolderFull}/Resources";

            bool inPackagesFolder = resourcesPath.StartsWith("Packages/");
            if (inPackagesFolder)
            {
                resourcesPath = "Assets/Plugins/FMOD/Resources";
            }
            EditorUtils.EnsureFolderExists(resourcesPath);
            AssetDatabase.CreateAsset(RuntimeSettings, $"{resourcesPath}/{assetName}.asset");

            AddPlatformsToAsset();
        }

        public void AddPlatformForBuildTargets(Platform platform)
        {
            foreach (BuildTarget buildTarget in platform.GetBuildTargets())
            {
                if (buildTarget != BuildTarget.NoTarget)
                {
                    try
                    {
                        PlatformForBuildTarget.Add(buildTarget, platform);
                    }
                    catch (Exception e)
                    {
                        RuntimeUtils.DebugLogWarningFormat("FMOD: Error platform {0} already added to build targets. : {1}", buildTarget, e.Message);
                    }
                }
            }
        }

        // Adds a new platform group to the set of platforms.
        public PlatformGroup AddPlatformGroup(string displayName, int sortOrder)
        {
            PlatformGroup group = PlatformGroup.Create(displayName, Legacy.Platform.None);
            group.DisplaySortOrder = sortOrder;

            RuntimeSettings.AddPlatform(group);
            AssetDatabase.AddObjectToAsset(group, RuntimeSettings);

            RuntimeSettings.LinkPlatform(group);

            return group;
        }

        private void ClearPlatformSettings()
        {
            RemovePlatformFromAsset(RuntimeSettings.DefaultPlatform);
            RemovePlatformFromAsset(RuntimeSettings.PlayInEditorPlatform);

            RuntimeSettings.Platforms.ForEach(RemovePlatformFromAsset);

            foreach (Platform platform in Resources.LoadAll<Platform>(Settings.SettingsAssetName))
            {
                RemovePlatformFromAsset(platform);
            }

            RuntimeSettings.DefaultPlatform = null;
            RuntimeSettings.PlayInEditorPlatform = null;

            RuntimeSettings.Platforms.Clear();
            PlatformForBuildTarget.Clear();
            RuntimeSettings.PlatformForRuntimePlatform.Clear();
        }

        // Testing function: Resets all platform settings.
        public void ResetPlatformSettings()
        {
            ClearPlatformSettings();
            RuntimeSettings.OnEnable();
        }

        // Testing function: Reimports legacy platform settings.
        public void ReimportLegacyPlatforms()
        {
            ClearPlatformSettings();
            RuntimeSettings.MigratedPlatforms.Clear();
            RuntimeSettings.OnEnable();
        }

        public void UpdateMigratedPlatform(Platform platform)
        {
            if (!RuntimeSettings.MigratedPlatforms.Contains(platform.LegacyIdentifier))
            {
                RuntimeSettings.MigratedPlatforms.Add(platform.LegacyIdentifier);
            }
        }

        // Adds any missing platforms:
        // * From the template collection
        // * From the legacy settings
        public void AddMissingPlatforms()
        {
            var newPlatforms = new List<Platform>();

            foreach (Settings.PlatformTemplate template in Settings.PlatformTemplates)
            {
                if (!RuntimeSettings.PlatformExists(template.Identifier))
                {
                    newPlatforms.Add(template.CreateInstance());
                }
            }

            // Ensure that the default platform exists
            if (!RuntimeSettings.DefaultPlatform)
            {
                RuntimeSettings.DefaultPlatform = ScriptableObject.CreateInstance<PlatformDefault>();
                newPlatforms.Add(RuntimeSettings.DefaultPlatform);
            }

            // Ensure that the Play In Editor platform exists
            if (!RuntimeSettings.PlayInEditorPlatform)
            {
                RuntimeSettings.PlayInEditorPlatform = ScriptableObject.CreateInstance<PlatformPlayInEditor>();
                newPlatforms.Add(RuntimeSettings.PlayInEditorPlatform);
            }

            // Ensure that the default and Play In Editor platforms have properties
            AffirmPlatformProperties(RuntimeSettings.DefaultPlatform);
            AffirmPlatformProperties(RuntimeSettings.PlayInEditorPlatform);

            // Migrate plugins if necessary
            var PluginsProperty = Platform.PropertyAccessors.Plugins;

            if (!RuntimeSettings.MigratedPlatforms.Contains(RuntimeSettings.DefaultPlatform.LegacyIdentifier))
            {
                PluginsProperty.Set(RuntimeSettings.DefaultPlatform, RuntimeSettings.Plugins);
            }
            else if (!PluginsProperty.HasValue(RuntimeSettings.DefaultPlatform))
            {
                PluginsProperty.Set(RuntimeSettings.DefaultPlatform, new List<string>());
            }

            // Migrate LiveUpdatePort
            if (!Platform.PropertyAccessors.LiveUpdatePort.HasValue(RuntimeSettings.DefaultPlatform))
            {
                Platform.PropertyAccessors.LiveUpdatePort.Set(RuntimeSettings.DefaultPlatform, RuntimeSettings.LiveUpdatePort);
            }

            // Create a map for migrating legacy settings
            var platformMap = new Dictionary<Legacy.Platform, Platform>();

            foreach (Platform platform in RuntimeSettings.Platforms.Concat(newPlatforms))
            {
                if (platform.LegacyIdentifier != Legacy.Platform.None)
                {
                    platformMap.Add(platform.LegacyIdentifier, platform);
                }
            }

            Func<Legacy.Platform, Platform> AffirmPlatform = null;

            // Ensures that all of the platform's ancestors exist.
            Action<Platform> AffirmAncestors = (platform) =>
            {
                Legacy.Platform legacyParent = Legacy.Parent(platform.LegacyIdentifier);

                if (legacyParent != Legacy.Platform.None)
                {
                    platform.ParentIdentifier = AffirmPlatform(legacyParent).Identifier;
                }
            };

            // Gets the platform corresponding to legacyPlatform (or creates it if it is a group),
            // and ensures that it has properties and all of its ancestors exist.
            // Returns null if legacyPlatform is unknown.
            AffirmPlatform = (legacyPlatform) =>
            {
                Platform platform;

                if (platformMap.TryGetValue(legacyPlatform, out platform))
                {
                    platform.AffirmProperties();
                }
                else if (Legacy.IsGroup(legacyPlatform))
                {
                    PlatformGroup group = PlatformGroup.Create(Legacy.DisplayName(legacyPlatform), legacyPlatform);
                    platformMap.Add(legacyPlatform, group);
                    newPlatforms.Add(group);

                    platform = group;
                }
                else
                {
                    // This is an unknown platform
                    return null;
                }

                AffirmAncestors(platform);

                return platform;
            };

            // Gets the target plaform to use when migrating settings from legacyPlatform.
            // Returns null if legacyPlatform is unknown or has already been migrated.
            Func<Legacy.Platform, Platform> getMigrationTarget = (legacyPlatform) =>
            {
                if (RuntimeSettings.MigratedPlatforms.Contains(legacyPlatform))
                {
                    // Already migrated
                    return null;
                }

                return AffirmPlatform(legacyPlatform);
            };

            var speakerModeSettings = RuntimeSettings.SpeakerModeSettings.ConvertAll(
                setting => new Legacy.PlatformSetting<FMOD.SPEAKERMODE>()
                {
                    Value = (FMOD.SPEAKERMODE)setting.Value,
                    Platform = setting.Platform
                }
                );

            // Migrate all the legacy settings, creating platforms as we need them via AffirmPlatform
            MigrateLegacyPlatforms(speakerModeSettings, Platform.PropertyAccessors.SpeakerMode, getMigrationTarget);
            MigrateLegacyPlatforms(RuntimeSettings.SampleRateSettings, Platform.PropertyAccessors.SampleRate, getMigrationTarget);
            MigrateLegacyPlatforms(RuntimeSettings.LiveUpdateSettings, Platform.PropertyAccessors.LiveUpdate, getMigrationTarget);
            MigrateLegacyPlatforms(RuntimeSettings.OverlaySettings, Platform.PropertyAccessors.Overlay, getMigrationTarget);
            MigrateLegacyPlatforms(RuntimeSettings.BankDirectorySettings, Platform.PropertyAccessors.BuildDirectory, getMigrationTarget);
            MigrateLegacyPlatforms(RuntimeSettings.VirtualChannelSettings, Platform.PropertyAccessors.VirtualChannelCount, getMigrationTarget);
            MigrateLegacyPlatforms(RuntimeSettings.RealChannelSettings, Platform.PropertyAccessors.RealChannelCount, getMigrationTarget);

            // Now we ensure that if a legacy group has settings, all of its descendants exist
            // and inherit from it (even if they have no settings of their own), so that the
            // inheritance structure matches the old system.
            // We look at all groups (not just newly created ones), because a newly created platform
            // may need to inherit from a preexisting group.
            var groupsToProcess = new Queue<Platform>(platformMap.Values.Where(
                platform => platform is PlatformGroup
                    && platform.LegacyIdentifier != Legacy.Platform.None
                    && platform.HasAnyOverriddenProperties));

            while (groupsToProcess.Count > 0)
            {
                Platform group = groupsToProcess.Dequeue();

                // Ensure that all descendants exist
                foreach (var child in platformMap.Values)
                {
                    if (child.Active)
                    {
                        // Don't overwrite existing settings
                        continue;
                    }

                    var legacyPlatform = child.LegacyIdentifier;

                    if (legacyPlatform == Legacy.Platform.iOS || legacyPlatform == Legacy.Platform.Android)
                    {
                        // These platforms were overridden by MobileHigh and MobileLow in the old system
                        continue;
                    }

                    if (RuntimeSettings.MigratedPlatforms.Contains(legacyPlatform))
                    {
                        // The user may have deleted this platform since migration, so don't mess with it
                        continue;
                    }

                    if (Legacy.Parent(legacyPlatform) == group.LegacyIdentifier)
                    {
                        child.AffirmProperties();
                        child.ParentIdentifier = group.Identifier;

                        if (child is PlatformGroup)
                        {
                            groupsToProcess.Enqueue(child as PlatformGroup);
                        }
                    }
                }
            }

            // Add all of the new platforms to the set of known platforms
            foreach (Platform platform in newPlatforms)
            {
                RuntimeSettings.AddPlatform(platform);
            }

            RuntimeSettings.Platforms.ForEach(UpdateMigratedPlatform);
        }

        private void MigrateLegacyPlatforms<TValue, TSetting>(List<TSetting> settings,
            Platform.PropertyAccessor<TValue> property, Func<Legacy.Platform, Platform> getMigrationTarget)
            where TSetting : Legacy.PlatformSetting<TValue>
        {
            foreach (TSetting setting in settings)
            {
                Platform platform = getMigrationTarget(setting.Platform);

                if (platform != null)
                {
                    property.Set(platform, setting.Value);
                }
            }
        }

        // The platform that implements the current Unity build target.
        public Platform CurrentEditorPlatform
        {
            get
            {
                return GetPlatform(EditorUserBuildSettings.activeBuildTarget);
            }
        }

        public Platform GetPlatform(BuildTarget buildTarget)
        {
            if (PlatformForBuildTarget.ContainsKey(buildTarget))
            {
                return PlatformForBuildTarget[buildTarget];
            }
            else
            {
                return RuntimeSettings.DefaultPlatform;
            }
        }

        public void SetPlatformParent(Platform platform, Platform newParent)
        {
            Platform oldParent = RuntimeSettings.FindPlatform(platform.ParentIdentifier);

            if (oldParent != null)
            {
                oldParent.ChildIdentifiers.Remove(platform.Identifier);
            }

            if (newParent != null)
            {
                platform.ParentIdentifier = newParent.Identifier;

                newParent.ChildIdentifiers.Add(platform.Identifier);
                SortPlatformChildren(newParent);
            }
            else
            {
                platform.ParentIdentifier = null;
            }
        }

        public void SetPlatformSortOrder(Platform platform, float sortOrder)
        {
            if (platform.DisplaySortOrder != sortOrder)
            {
                platform.DisplaySortOrder = sortOrder;

                if (platform.Parent != null)
                {
                    SortPlatformChildren(platform.Parent);
                }
            }
        }

        public void SortPlatformChildren(Platform platform)
        {
            platform.ChildIdentifiers.Sort((a, b) => {
                Platform platformA = RuntimeSettings.FindPlatform(a);
                Platform platformB = RuntimeSettings.FindPlatform(b);

                return platformA.DisplaySortOrder.CompareTo(platformB.DisplaySortOrder);
            });
        }

        // Ensures that the given platform has valid properties.
        private void AffirmPlatformProperties(Platform platform)
        {
            if (!platform.Active)
            {
                RuntimeUtils.DebugLogFormat("[FMOD] Cannot find properties for platform {0}, creating default properties", platform.Identifier);
                RuntimeSettings.AddPlatformProperties(platform);
            }
        }

        private void RemovePlatformFromAsset(Platform platform)
        {
            if (AssetDatabase.Contains(platform))
            {
                UnityEngine.Object.DestroyImmediate(platform, true);
            }
        }

        public bool CanBuildTarget(BuildTarget target, Platform.BinaryType binaryType, out string error)
        {
            if (Settings.Instance == null)
            {
                error = "Settings instance has not been initialized. Unable to continue build.";
                return false;
            }

            Platform platform;

            if (!PlatformForBuildTarget.TryGetValue(target, out platform))
            {
                error = string.Format("No FMOD platform found for build target {0}. " +
                            "You may need to install a platform specific integration package from {1}.",
                            target, DownloadURL);
                return false;
            }

            IEnumerable<string> missingPathsQuery = platform.GetBinaryFilePaths(target, binaryType)
                .Where(path => !File.Exists(path) && !Directory.Exists(path));

            if (missingPathsQuery.Any())
            {
                string[] missingPaths = missingPathsQuery.Select(path => "- " + path).ToArray();

                string summary;

                if (missingPaths.Length == 1)
                {
                    summary = string.Format("There is an FMOD binary missing for build target {0}", target);
                }
                else
                {
                    summary = string.Format("There are {0} FMOD binaries missing for build target {1}",
                        missingPaths.Length, target);
                }

                if (binaryType == Platform.BinaryType.Logging)
                {
                    summary += " (development build)";
                }

                error = string.Format(
                    "{0}:\n" +
                    "{1}\n" +
                    "Please run the {2} menu command.\n",
                    summary, string.Join("\n", missingPaths), FileReorganizer.ReorganizerMenuItemPath);
                return false;
            }

            error = null;
            return true;
        }

        public void PreprocessBuild(BuildTarget target, Platform.BinaryType binaryType)
        {
            Platform platform = PlatformForBuildTarget[target];

            PreprocessStaticPlugins(platform, target);

            SelectBinaries(platform, target, binaryType);
        }

        private void PostprocessBuild(BuildTarget target)
        {
            foreach(string path in binaryCompatibilitiesBeforeBuild.Keys)
            {
                PluginImporter importer = AssetImporter.GetAtPath(path) as PluginImporter;

                if (importer != null)
                {
                    importer.SetCompatibleWithPlatform(target, binaryCompatibilitiesBeforeBuild[path]);
                }
            }
        }

        private void PreprocessStaticPlugins(Platform platform, BuildTarget target)
        {
            // Ensure we don't have leftover temporary changes from a previous build.
            CleanTemporaryFiles();

            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            ScriptingImplementation scriptingBackend = PlayerSettings.GetScriptingBackend(namedBuildTarget);

            if (platform.StaticPlugins.Count > 0)
            {
                if (scriptingBackend == ScriptingImplementation.IL2CPP)
                {
                    Action<string> reportError = message => {
                        RuntimeUtils.DebugLogWarningFormat("FMOD: Error processing static plugins for platform {0}: {1}",
                            platform.DisplayName, message);
                    };

                    if (!AssetDatabase.IsValidFolder(CacheFolderFull))
                    {
                        RuntimeUtils.DebugLogFormat("Creating {0}", CacheFolderFull);
                        AssetDatabase.CreateFolder(FMODFolderFull, CacheFolderName);
                    }

                    // Generate registration code and import it so it's included in the build.
                    RuntimeUtils.DebugLogFormat("FMOD: Generating static plugin registration code in {0}", RegisterStaticPluginsAssetPathFull);

                    string filePath = RegisterStaticPluginsAssetPathRelative.Replace("Assets", Application.dataPath);
                    CodeGeneration.GenerateStaticPluginRegistration(filePath, platform, reportError);
                    AssetDatabase.ImportAsset(RegisterStaticPluginsAssetPathFull);
                }
                else
                {
                    RuntimeUtils.DebugLogWarningFormat(
                        "FMOD: Platform {0} has {1} static plugins specified, " +
                        "but static plugins are only supported on the IL2CPP scripting backend",
                        platform.DisplayName, platform.StaticPlugins.Count);
                }
            }
        }

        public void CleanTemporaryFiles()
        {
            DeleteTemporaryFile(RegisterStaticPluginsAssetPathFull);
        }

        public void DeleteTemporaryFile(string assetPath)
        {
            bool assetExists = !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath));

            if (assetExists && AssetDatabase.DeleteAsset(assetPath))
            {
                RuntimeUtils.DebugLogFormat("FMOD: Removed temporary file {0}", assetPath);
            }
        }

        private static void SelectBinaries(Platform platform, BuildTarget target, Platform.BinaryType binaryType)
        {
            string message = string.Format("FMOD: Selected binaries for platform {0}{1}:", target,
                (binaryType == Platform.BinaryType.Logging) ? " (development build)" : string.Empty);

            Instance.binaryCompatibilitiesBeforeBuild = new Dictionary<string, bool>();

            HashSet<string> enabledPaths = new HashSet<string>();

            foreach (string path in platform.GetBinaryAssetPaths(target, binaryType | Platform.BinaryType.Optional))
            {
                PluginImporter importer = AssetImporter.GetAtPath(path) as PluginImporter;

                if (importer is PluginImporter)
                {
                    Instance.binaryCompatibilitiesBeforeBuild.Add(path, importer.GetCompatibleWithPlatform(target));

                    importer.SetCompatibleWithPlatform(target, true);

                    enabledPaths.Add(path);

                    message += string.Format("\n- Enabled {0}", path);
                }
            }

            foreach (string path in platform.GetBinaryAssetPaths(target, Platform.BinaryType.All))
            {
                if (!enabledPaths.Contains(path))
                {
                    PluginImporter importer = AssetImporter.GetAtPath(path) as PluginImporter;

                    if (importer is PluginImporter)
                    {
                        Instance.binaryCompatibilitiesBeforeBuild.Add(path, importer.GetCompatibleWithPlatform(target));

                        importer.SetCompatibleWithPlatform(target, false);

                        message += string.Format("\n- Disabled {0}", path);
                    }
                }
            }

            RuntimeUtils.DebugLog(message);
        }

        public bool ForceLoggingBinaries { get; set; } = false;

        public class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
        {
            public int callbackOrder { get { return 0; } }

            public void OnPreprocessBuild(BuildReport report)
            {
                Platform.BinaryType binaryType;

                if ((report.summary.options & BuildOptions.Development) == BuildOptions.Development
                    || EditorSettings.Instance.ForceLoggingBinaries)
                {
                    binaryType = Platform.BinaryType.Logging;
                }
                else
                {
                    binaryType = Platform.BinaryType.Release;
                }

                string error;
                if (!EditorSettings.Instance.CanBuildTarget(report.summary.platform, binaryType, out error))
                {
                    throw new BuildFailedException(error);
                }

                bool androidPatchBuildPrevious = Settings.Instance.AndroidPatchBuild;
                if ((report.summary.options & BuildOptions.PatchPackage) == BuildOptions.PatchPackage)
                {
                    Settings.Instance.AndroidPatchBuild = true;
                }
                else
                {
                    Settings.Instance.AndroidPatchBuild = false;
                }
                if (androidPatchBuildPrevious != Settings.Instance.AndroidPatchBuild)
                {
                    EditorUtility.SetDirty(Settings.Instance);
                }

                EditorSettings.Instance.PreprocessBuild(report.summary.platform, binaryType);
            }

            public void OnPostprocessBuild(BuildReport report)
            {
                Instance.PostprocessBuild(report.summary.platform);
                Settings.Instance.AndroidPatchBuild = false;
            }
        }

        public void CheckActiveBuildTarget()
        {
            Settings.EditorSettings.CleanTemporaryFiles();

            Platform.BinaryType binaryType = EditorUserBuildSettings.development
                ? Platform.BinaryType.Logging
                : Platform.BinaryType.Release;

            string error;
            if (!CanBuildTarget(EditorUserBuildSettings.activeBuildTarget, binaryType, out error))
            {
                RuntimeUtils.DebugLogWarning(error);

                if (EditorWindow.HasOpenInstances<BuildPlayerWindow>())
                {
                    GUIContent message =
                        new GUIContent("FMOD detected issues with this platform!\nSee the Console for details.");
                    EditorWindow.GetWindow<BuildPlayerWindow>().ShowNotification(message, 10);
                }
            }
        }

        // Adds all platforms to the settings asset, so they get stored in the same file as the main
        // Settings object.
        public void AddPlatformsToAsset()
        {
            RuntimeSettings.Platforms.ForEach(AddPlatformToAsset);
        }

        private void AddPlatformToAsset(Platform platform)
        {
            if (!AssetDatabase.Contains(platform))
            {
                platform.name = "FMODStudioSettingsPlatform";
                AssetDatabase.AddObjectToAsset(platform, RuntimeSettings);
            }
        }
    }
}
