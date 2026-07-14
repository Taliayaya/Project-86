using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMODUnity
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class PlatformMobileLow : Platform
    {
        static PlatformMobileLow()
        {
            Settings.AddPlatformTemplate<PlatformMobileLow>("c88d16e5272a4e241b0ef0ac2e53b73d");
        }

        internal override string DisplayName { get { return "Low-End Mobile"; } }
        internal override void DeclareRuntimePlatforms(Settings settings)
        {
            settings.DeclareRuntimePlatform(RuntimePlatform.IPhonePlayer, this);
            settings.DeclareRuntimePlatform(RuntimePlatform.Android, this);
        }

#if UNITY_EDITOR
        internal override IEnumerable<BuildTarget> GetBuildTargets()
        {
            yield break;
        }

        internal override Legacy.Platform LegacyIdentifier { get { return Legacy.Platform.MobileLow; } }

        protected override BinaryAssetFolderInfo GetBinaryAssetFolder(BuildTarget buildTarget)
        {
            return null;
        }

        protected override IEnumerable<FileRecord> GetBinaryFiles(BuildTarget buildTarget, bool allVariants, string suffix)
        {
            yield break;
        }

        internal override bool SupportsAdditionalCPP(BuildTarget target)
        {
            if (target == BuildTarget.iOS)
            {
                return PlatformIOS.StaticSupportsAdditionalCpp();
            }
            else
            {
                return base.SupportsAdditionalCPP(target);
            }
        }
#endif

        internal override float Priority { get { return DefaultPriority + 1; } }

        internal override bool MatchesCurrentEnvironment
        {
            get
            {
                return Active;
            }
        }

#if UNITY_IOS
        internal override void LoadPlugins(FMOD.System coreSystem, Action<FMOD.RESULT, string> reportResult)
        {
            PlatformIOS.StaticLoadPlugins(this, coreSystem, reportResult);
        }
#elif UNITY_ANDROID
        internal override string GetBankFolder()
        {
            return PlatformAndroid.StaticGetBankFolder();
        }

        internal override string GetPluginPath(string pluginName)
        {
            return PlatformAndroid.StaticGetPluginPath(pluginName);
        }
#endif

#if UNITY_EDITOR
        internal override OutputType[] ValidOutputTypes { get { return null; } }
#endif
    }
}
