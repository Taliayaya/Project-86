using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

#if (UNITY_VISUALSCRIPTING_EXIST)
using Unity.VisualScripting;
#elif (UNITY_BOLT_EXIST)
using Ludiq;
using Bolt;
#endif

namespace FMODUnity
{
    public class BoltIntegration : MonoBehaviour
    {
        [MenuItem("FMOD/Generate Visual Scripting Units")]
        public static void GenerateBoltUnitOptions()
        {
#if (UNITY_BOLT_EXIST || UNITY_VISUALSCRIPTING_EXIST)
            BuildBoltUnitOptions();
#else
            TriggerBuild();
#endif
        }

#if !(UNITY_BOLT_EXIST || UNITY_VISUALSCRIPTING_EXIST)
        [MenuItem("FMOD/Generate Visual Scripting Units", true)]
        private static bool IsBoltPresent()
        {
            Assembly ludiqCoreRuntimeAssembly = null;
            Assembly boltFlowEditorAssembly = null;

            try
            {
                ludiqCoreRuntimeAssembly = Assembly.Load("Ludiq.Core.Runtime");
                boltFlowEditorAssembly = Assembly.Load("Bolt.Flow.Editor");
            }
            catch (FileNotFoundException)
            {
                return false;
            }

            return true;
        }

        private static void TriggerBuild()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
#if UNITY_2021_2_OR_NEWER
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(group);
            string previousSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            string previousSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
#endif

            if (!previousSymbols.Contains("UNITY_BOLT_EXIST"))
            {
#if UNITY_2021_2_OR_NEWER
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, previousSymbols + ";UNITY_BOLT_EXIST");
#else
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, previousSymbols + ";UNITY_BOLT_EXIST");
#endif
            }
            Settings.Instance.BoltUnitOptionsBuildPending = true;
            AssetDatabase.Refresh();
        }

#else
        private static void BuildBoltUnitOptions()
        {
#if (UNITY_BOLT_EXIST)
            DictionaryAsset projectSettings = AssetDatabase.LoadAssetAtPath(PathUtility.FromProject(LudiqCore.Paths.projectSettings), typeof(DictionaryAsset)) as DictionaryAsset;
            List<LooseAssemblyName> assemblyOptions = projectSettings.dictionary["assemblyOptions"] as List<LooseAssemblyName>;
#else
            List<LooseAssemblyName> assemblyOptions = BoltCore.Configuration.assemblyOptions;
#endif

            if (!assemblyOptions.Contains("FMODUnity"))
            {
                assemblyOptions.Add("FMODUnity");
            }

            if (!assemblyOptions.Contains("FMODUnityResonance"))
            {
                assemblyOptions.Add("FMODUnityResonance");
            }
#if (UNITY_BOLT_EXIST)
            List<Type> typeOptions = projectSettings.dictionary["typeOptions"] as List<Type>;
#else
            List<Type> typeOptions = BoltCore.Configuration.typeOptions;
#endif
            Assembly fmodUnityAssembly = Assembly.Load("FMODUnity");
            Assembly fmodUnityResonanceAssembly = Assembly.Load("FMODUnityResonance");

            List<Type> allTypes = new List<Type>(GetTypesForNamespace(fmodUnityAssembly, "FMOD"));
            allTypes.AddRange(GetTypesForNamespace(fmodUnityAssembly, "FMOD.Studio"));
            allTypes.AddRange(GetTypesForNamespace(fmodUnityAssembly, "FMODUnity"));
            allTypes.AddRange(GetTypesForNamespace(fmodUnityResonanceAssembly, "FMODUnityResonance"));

            foreach (Type type in allTypes)
            {
                if (!typeOptions.Contains(type))
                {
                    typeOptions.Add(type);
                }
            }

            Codebase.UpdateSettings();
#if (UNITY_BOLT_EXIST)
            UnitBase.Build();
#else
            BoltCore.Configuration.Save();
            UnitBase.Rebuild();
#endif
        }

        private static IEnumerable<Type> GetTypesForNamespace(Assembly assembly, string requestedNamespace)
        {
            return assembly.GetTypes()
                    .Where(t => string.Equals(t.Namespace, requestedNamespace, StringComparison.Ordinal));
        }
#endif

        public static void Startup()
        {
#if (UNITY_BOLT_EXIST || UNITY_VISUALSCRIPTING_EXIST)
            if (Settings.Instance.BoltUnitOptionsBuildPending)
            {
                Settings.Instance.BoltUnitOptionsBuildPending = false;
                BuildBoltUnitOptions();
            }
#endif
        }
    }
}
