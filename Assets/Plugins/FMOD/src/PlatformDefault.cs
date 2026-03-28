using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FMODUnity
{
    public class PlatformDefault : Platform
    {
        public const string ConstIdentifier = "default";

        public PlatformDefault()
        {
            Identifier = ConstIdentifier;
        }

        internal override string DisplayName { get { return "Default"; } }
        internal override void DeclareRuntimePlatforms(Settings settings) { }
#if UNITY_EDITOR
        internal override IEnumerable<BuildTarget> GetBuildTargets()
        {
            yield break;
        }

        internal override Legacy.Platform LegacyIdentifier { get { return Legacy.Platform.Default; } }

        protected override BinaryAssetFolderInfo GetBinaryAssetFolder(BuildTarget buildTarget)
        {
            return null;
        }

        protected override IEnumerable<FileRecord> GetBinaryFiles(BuildTarget buildTarget, bool allVariants, string suffix)
        {
            yield break;
        }
#endif

        internal override bool IsIntrinsic { get { return true; } }

        internal override void InitializeProperties()
        {
            base.InitializeProperties();

            PropertyAccessors.Plugins.Set(this, new List<string>());
            PropertyAccessors.StaticPlugins.Set(this, new List<string>());
        }

        internal override void EnsurePropertiesAreValid()
        {
            base.EnsurePropertiesAreValid();

            if (StaticPlugins == null)
            {
                PropertyAccessors.StaticPlugins.Set(this, new List<string>());
            }
        }

        // null means no valid output types - don't display the field in the UI
#if UNITY_EDITOR
        internal override OutputType[] ValidOutputTypes { get { return null; } }
#endif
    }
}
