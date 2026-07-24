using UnityEditor;
using UnityEngine;

namespace EEditor
{

    /// <summary>
    /// Inspector-editable settings for GrassSetupTool. Lives in the Editor assembly
    /// on purpose — this never ships in a build. The tool auto-creates one at
    /// Assets/PermResources/Grass/GrassSettings.asset.
    /// </summary>
    public class GrassSettings : ScriptableObject
    {
        [Header("Placement")]
        [Range(0f, 60f)] public float maxSlopeDegrees = 30f;
        [Tooltip("Grass-named terrain layers must hold this much of the splat weight")]
        [Range(0f, 1f)] public float minGrassWeight = 0.4f;
        [Tooltip("Instances per detail cell (InstanceCount scatter mode)")]
        public int densityPerCell = 2;
        [Tooltip("0-255 coverage (Coverage scatter mode)")]
        [Range(0, 255)] public int coverageValue = 160;

        [Header("Rendering")]
        [Tooltip("Detail draw distance — the main performance lever")]
        public float detailDistance = 120f;
        public Texture2D grassTexture;
        [Range(0f, 1f)] public float alphaCutoff = 0.45f;

        [Header("Card size & tint")]
        public float minWidth = 0.8f;
        public float maxWidth = 1.6f;
        public float minHeight = 0.5f;
        public float maxHeight = 1.1f;
        public Color healthyColor = new(0.45f, 0.55f, 0.25f);
        public Color dryColor = new(0.55f, 0.5f, 0.3f);
    }

    [CustomEditor(typeof(GrassSettings))]
    public class GrassSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            if (GUILayout.Button("Populate Terrain Grass"))
                GrassSetupTool.Populate();
            if (GUILayout.Button("Clear Terrain Grass"))
                GrassSetupTool.Clear();
        }
    }
}
