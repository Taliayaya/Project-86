using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if !UNITY_EDITOR
namespace FMOD
{
    public partial class VERSION
    {
#if UNITY_STANDALONE_WIN
        public const string dll = "fmodstudio" + suffix;
#elif UNITY_WSA
        public const string dll = "fmod" + suffix;
#endif
    }
}

namespace FMOD.Studio
{
    public partial class STUDIO_VERSION
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA
        public const string dll = "fmodstudio" + VERSION.suffix;
#endif
    }
}
#endif

namespace FMODUnity
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class PlatformWindows : Platform
    {
        static PlatformWindows()
        {
            Settings.AddPlatformTemplate<PlatformWindows>("2c5177b11d81d824dbb064f9ac8527da");
        }

        internal override string DisplayName { get { return "Windows"; } }
        internal override void DeclareRuntimePlatforms(Settings settings)
        {
            settings.DeclareRuntimePlatform(RuntimePlatform.WindowsPlayer, this);
            settings.DeclareRuntimePlatform(RuntimePlatform.WSAPlayerX86, this);
            settings.DeclareRuntimePlatform(RuntimePlatform.WSAPlayerX64, this);
            settings.DeclareRuntimePlatform(RuntimePlatform.WSAPlayerARM, this);
        }

#if UNITY_EDITOR
        internal override IEnumerable<BuildTarget> GetBuildTargets()
        {
            yield return BuildTarget.StandaloneWindows;
            yield return BuildTarget.StandaloneWindows64;
            yield return BuildTarget.WSAPlayer;
        }

        internal override Legacy.Platform LegacyIdentifier { get { return Legacy.Platform.Windows; } }
#endif

#if UNITY_WINRT_8_1 || UNITY_WSA_10_0
        internal override string GetBankFolder()
        {
            return "ms-appx:///Data/StreamingAssets";
        }
#endif

#if UNITY_EDITOR
        protected override BinaryAssetFolderInfo GetBinaryAssetFolder(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return new BinaryAssetFolderInfo("win", "Plugins");
                case BuildTarget.WSAPlayer:
                    return new BinaryAssetFolderInfo("uwp", "Plugins/UWP");
                default:
                    throw new System.ArgumentException("Unrecognised build target: " + buildTarget);
            }
        }

        protected override IEnumerable<FileRecord> GetBinaryFiles(BuildTarget buildTarget, bool allVariants, string suffix)
        {
            string dllSuffix = suffix + ".dll";

            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                    yield return new FileRecord("x86/fmodstudio" + dllSuffix);
                    break;
                case BuildTarget.StandaloneWindows64:
#if UNITY_2023_1_OR_NEWER
                    yield return new FileRecord("arm64/fmodstudio" + dllSuffix);
#endif
                    yield return new FileRecord("x86_64/fmodstudio" + dllSuffix);
                    break;
                case BuildTarget.WSAPlayer:
                    foreach (string architecture in new[] { "arm", "x64", "x86" })
                    {
                        yield return new FileRecord(string.Format("{0}/fmod{1}", architecture, dllSuffix));
                        yield return new FileRecord(string.Format("{0}/fmodstudio{1}", architecture, dllSuffix));
                    }
                    break;
                default:
                    throw new System.NotSupportedException("Unrecognised Build Target");
            }
        }

        protected override IEnumerable<FileRecord> GetOptionalBinaryFiles(BuildTarget buildTarget, bool allVariants)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                    yield return new FileRecord("x86/gvraudio.dll");
                    yield return new FileRecord("x86/resonanceaudio.dll");
                    break;
                case BuildTarget.StandaloneWindows64:
                    yield return new FileRecord("x86_64/gvraudio.dll");
                    yield return new FileRecord("x86_64/resonanceaudio.dll");
                    break;
                case BuildTarget.WSAPlayer:
                    yield break;
                default:
                    throw new System.NotSupportedException("Unrecognised Build Target");
            }
        }

        internal override bool SupportsAdditionalCPP(BuildTarget target)
        {
            return target != BuildTarget.WSAPlayer;
        }
#endif

        internal override string GetPluginPath(string pluginName)
        {
#if UNITY_STANDALONE_WIN
            return string.Format("{0}/{1}/{2}.dll", GetPluginBasePath(), RuntimeUtils.GetPluginArchitectureFolder(), pluginName);
#else // UNITY_WSA
            return string.Format("{0}.dll", pluginName);
#endif
        }
#if UNITY_EDITOR
        internal override OutputType[] ValidOutputTypes
        {
            get
            {
                return sValidOutputTypes;
            }
        }

        private static OutputType[] sValidOutputTypes = {
           new OutputType() { displayName = "Windows Audio Session API", outputType = FMOD.OUTPUTTYPE.WASAPI },
           new OutputType() { displayName = "Windows Sonic", outputType = FMOD.OUTPUTTYPE.WINSONIC },
        };

        internal override int CoreCount { get { return MaximumCoreCount; } }
#endif

        internal override List<CodecChannelCount> DefaultCodecChannels { get { return staticCodecChannels; } }

        private static List<CodecChannelCount> staticCodecChannels = new List<CodecChannelCount>()
        {
            new CodecChannelCount { format = CodecType.FADPCM, channels = 0 },
            new CodecChannelCount { format = CodecType.Vorbis, channels = 32 },
        };
    }
}
