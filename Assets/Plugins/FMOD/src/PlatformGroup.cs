using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMODUnity
{
    public class PlatformGroup : Platform
    {
        [SerializeField]
        private string displayName;

        [SerializeField]
        private Legacy.Platform legacyIdentifier;

        internal override string DisplayName { get { return displayName; } }
        internal override void DeclareRuntimePlatforms(Settings settings) { }
#if UNITY_EDITOR
        internal override IEnumerable<BuildTarget> GetBuildTargets()
        {
            yield break;
        }

        internal override Legacy.Platform LegacyIdentifier { get { return legacyIdentifier; } }

        internal static PlatformGroup Create(string displayName, Legacy.Platform legacyIdentifier)
        {
            PlatformGroup group = CreateInstance<PlatformGroup>();
            group.Identifier = GUID.Generate().ToString();
            group.displayName = displayName;
            group.legacyIdentifier = legacyIdentifier;
            group.AffirmProperties();

            return group;
        }

        protected override BinaryAssetFolderInfo GetBinaryAssetFolder(BuildTarget buildTarget)
        {
            return null;
        }

        protected override IEnumerable<FileRecord> GetBinaryFiles(BuildTarget buildTarget, bool allVariants, string suffix)
        {
            yield break;
        }

        internal override OutputType[] ValidOutputTypes { get { return null; } }
#endif
    }
}
