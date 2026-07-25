using UnityEditor;
using UnityEngine;

namespace Project86.Editor
{
    // ponytail: one-shot manual tool (menu item), not a build step or asset postprocessor.
    // Re-run after adding new textures if you want them crunched too.
    public static class BatchCrunchCompression
    {
        private static readonly string[] ExcludePathContains = { "juggernaut", "reginleif" };
        private const int CrunchQuality = 50;

        [MenuItem("Tools/Textures/Enable Crunch Compression (Skip Hero Units)")]
        public static void Run()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D");
            int changed = 0, skipped = 0, unsupported = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("Enabling crunch compression", path, (float)i / guids.Length);

                    var lowerPath = path.ToLowerInvariant();
                    if (ContainsAny(lowerPath, ExcludePathContains))
                    {
                        skipped++;
                        continue;
                    }

                    if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                        continue;

                    // crunch only applies to Default/NormalMap/Sprite textures compressed as DXT/ETC
                    if (importer.textureType != TextureImporterType.Default &&
                        importer.textureType != TextureImporterType.NormalMap &&
                        importer.textureType != TextureImporterType.Sprite)
                    {
                        unsupported++;
                        continue;
                    }

                    if (importer.crunchedCompression && importer.compressionQuality == CrunchQuality)
                        continue;

                    importer.crunchedCompression = true;
                    importer.compressionQuality = CrunchQuality;
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"[BatchCrunchCompression] Crunched {changed} textures. Skipped {skipped} hero-unit textures, {unsupported} unsupported types (cubemap/lightmap/cursor/etc).");
        }

        private static bool ContainsAny(string haystack, string[] needles)
        {
            foreach (var needle in needles)
            {
                if (haystack.Contains(needle))
                    return true;
            }

            return false;
        }
    }
}
