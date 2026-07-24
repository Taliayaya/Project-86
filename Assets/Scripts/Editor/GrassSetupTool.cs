using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EEditor
{

    /// <summary>
    /// Populates every Terrain in the open scene with GPU-instanced low-poly grass
    /// (a generated 6-tri crossed-quad card), sprayed only where a grass-surfaced
    /// terrain layer dominates and the slope is walkable.
    /// Knobs live on the GrassSettings asset (Assets/PermResources/Grass/GrassSettings.asset),
    /// which also has Populate/Clear buttons. Menu: Tools/Grass/*.
    /// </summary>
    public static class GrassSetupTool
    {
        const string DefaultGrassTexturePath = "Assets/Additional Assets/Bootcamp Map/Earth/grass.png";
        const string OutputFolder = "Assets/PermResources/Grass";
        // Matched against DetailPrototype.prototype.name — NOT the mesh asset's name,
        // which Unity renames to "GrassCard.mesh" after the file
        const string PrototypeName = "GrassCard";

        [MenuItem("Tools/Grass/Populate Terrain Grass")]
        public static void Populate()
        {
            var settings = GetOrCreateSettings();
            var mesh = GetOrCreateCardMesh();
            var material = GetOrCreateMaterial(settings);
            int painted = 0, skipped = 0;

            foreach (var terrain in Object.FindObjectsByType<Terrain>())
            {
                var data = terrain.terrainData;
                var grassLayers = GrassLayerIndices(data);
                if (grassLayers.Count == 0)
                {
                    Debug.Log($"[GrassSetup] {terrain.name}: no grass-named terrain layer, skipped.");
                    skipped++;
                    continue;
                }

                int protoIndex = EnsurePrototype(data, mesh, material, settings);
                PaintLayer(data, protoIndex, grassLayers, settings);

                terrain.detailObjectDistance = settings.detailDistance;
                // Terrain caches detail patches — without this, edits render stale
                data.RefreshPrototypes();
                terrain.Flush();
                EditorUtility.SetDirty(terrain);
                EditorUtility.SetDirty(data);
                painted++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GrassSetup] Done. Painted {painted} terrains, skipped {skipped}.");
        }

        [MenuItem("Tools/Grass/Clear Terrain Grass")]
        public static void Clear()
        {
            foreach (var terrain in Object.FindObjectsByType<Terrain>())
            {
                var data = terrain.terrainData;
                var prototypes = new List<DetailPrototype>(data.detailPrototypes);
                if (prototypes.RemoveAll(p => p.prototype && p.prototype.name == PrototypeName) == 0)
                    continue;
                data.detailPrototypes = prototypes.ToArray();
                data.RefreshPrototypes();
                terrain.Flush();
                EditorUtility.SetDirty(terrain);
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[GrassSetup] Grass prototype removed from all terrains.");
        }

        [MenuItem("Tools/Grass/Select Grass Settings")]
        public static void SelectSettings() => Selection.activeObject = GetOrCreateSettings();

        static GrassSettings GetOrCreateSettings()
        {
            EnsureFolder();
            string path = $"{OutputFolder}/GrassSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<GrassSettings>(path);
            if (settings) return settings;

            settings = ScriptableObject.CreateInstance<GrassSettings>();
            settings.grassTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultGrassTexturePath);
            AssetDatabase.CreateAsset(settings, path);
            return settings;
        }

        static List<int> GrassLayerIndices(TerrainData data)
        {
            var result = new List<int>();
            for (int i = 0; i < data.terrainLayers.Length; i++)
            {
                var layer = data.terrainLayers[i];
                if (!layer) continue;
                // Gaia layers have opaque names (Gaia_-2026..._1); the texture name identifies the surface
                bool isGrass = layer.name.ToLowerInvariant().Contains("grass")
                    || (layer.diffuseTexture && layer.diffuseTexture.name.ToLowerInvariant().Contains("grass"));
                if (isGrass)
                    result.Add(i);
            }
            return result;
        }

        static int EnsurePrototype(TerrainData data, Mesh mesh, Material material, GrassSettings s)
        {
            var prototypes = new List<DetailPrototype>(data.detailPrototypes);
            int idx = prototypes.FindIndex(p => p.prototype && p.prototype.name == PrototypeName);
            if (idx < 0)
            {
                prototypes.Add(new DetailPrototype());
                idx = prototypes.Count - 1;
            }

            // Always rewrite so settings edits apply on re-run
            var proto = prototypes[idx];
            proto.prototype = GetOrCreateCardPrefab(mesh, material);
            proto.usePrototypeMesh = true;
            proto.renderMode = DetailRenderMode.VertexLit;
            proto.useInstancing = true;
            proto.minWidth = s.minWidth;
            proto.maxWidth = s.maxWidth;
            proto.minHeight = s.minHeight;
            proto.maxHeight = s.maxHeight;
            proto.noiseSpread = 0.2f;
            proto.healthyColor = s.healthyColor;
            proto.dryColor = s.dryColor;
            prototypes[idx] = proto;
            data.detailPrototypes = prototypes.ToArray();
            return idx;
        }

        static void PaintLayer(TerrainData data, int protoIndex, List<int> grassLayers, GrassSettings s)
        {
            int res = data.detailResolution;
            int alphaRes = data.alphamapResolution;
            float[,,] splats = data.GetAlphamaps(0, 0, alphaRes, alphaRes);
            int[,] layer = new int[res, res];
            int cellValue = data.detailScatterMode == DetailScatterMode.CoverageMode
                ? s.coverageValue
                : s.densityPerCell;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float u = (x + 0.5f) / res;
                    float v = (y + 0.5f) / res;
                    if (data.GetSteepness(u, v) > s.maxSlopeDegrees) continue;

                    int ax = Mathf.Min((int)(u * alphaRes), alphaRes - 1);
                    int ay = Mathf.Min((int)(v * alphaRes), alphaRes - 1);
                    float grassWeight = 0f;
                    foreach (int g in grassLayers)
                        grassWeight += splats[ay, ax, g];
                    if (grassWeight >= s.minGrassWeight)
                        layer[y, x] = cellValue;
                }
            }
            data.SetDetailLayer(0, 0, protoIndex, layer);
        }

        static Mesh GetOrCreateCardMesh()
        {
            EnsureFolder();
            string path = $"{OutputFolder}/GrassCard.mesh.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh) return mesh;

            // 3 quads crossed at 60°, pivot at ground, 1m x 1m. 6 tris total.
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                var rot = Quaternion.Euler(0f, 60f * i, 0f);
                int b = verts.Count;
                verts.Add(rot * new Vector3(-0.5f, 0f, 0f));
                verts.Add(rot * new Vector3(0.5f, 0f, 0f));
                verts.Add(rot * new Vector3(-0.5f, 1f, 0f));
                verts.Add(rot * new Vector3(0.5f, 1f, 0f));
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1));
                // Up normals so lighting matches the terrain instead of shading each card as a wall
                for (int n = 0; n < 4; n++) normals.Add(Vector3.up);
                tris.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
            }

            mesh = new Mesh { name = "GrassCard" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.SetTriangles(tris, 0);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static Material GetOrCreateMaterial(GrassSettings s)
        {
            EnsureFolder();
            string path = $"{OutputFolder}/GrassCard.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }

            // Always rewrite so settings edits apply on re-run.
            // Tint goes on the material: instanced details ignore healthy/dry colors.
            mat.SetTexture("_BaseMap", s.grassTexture);
            mat.SetColor("_BaseColor", s.healthyColor);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", s.alphaCutoff);
            mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            mat.SetFloat("_Smoothness", 0f);
            mat.EnableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static GameObject GetOrCreateCardPrefab(Mesh mesh, Material material)
        {
            string path = $"{OutputFolder}/GrassCard.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab) return prefab;

            var go = new GameObject("GrassCard");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/PermResources", "Grass");
        }
    }
}
