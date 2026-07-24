using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Procedural ring of distant low-poly hills surrounding the playable map, hiding
    /// the void past the edges. Flat at the inner edge (to meet the map), rolling hills
    /// toward the horizon. Builds its mesh on Awake; use the component context menu
    /// "Rebuild" to preview in the editor after tweaking values. No collider, no shadows.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class VistaRing : MonoBehaviour
    {
        public enum InnerShape { Circle, Square }

        [Header("Shape (meters)")]
        [Tooltip("Square: hole matches a square map, innerRadius = half the map side length.")]
        public InnerShape innerShape = InnerShape.Square;
        public float innerRadius = 1500f;
        public float outerRadius = 6000f;

        [Header("Hills")]
        public float hillHeight = 400f;
        [Tooltip("Smaller = wider hills. 0.0008 gives ~1km-wide features.")]
        public float noiseScale = 0.0008f;
        public int seed = 86;

        [Header("Resolution")]
        [Range(16, 512)] public int segments = 192;
        [Range(2, 32)] public int rings = 10;

        private void Awake() => Build();

        [ContextMenu("Rebuild")]
        public void Build()
        {
            var mf = GetComponent<MeshFilter>();
            var mesh = mf.sharedMesh && mf.sharedMesh.name == "VistaRing" ? mf.sharedMesh : new Mesh { name = "VistaRing" };
            mesh.Clear();

            int vertsPerRing = segments + 1;
            var verts = new Vector3[vertsPerRing * (rings + 1)];
            var uvs = new Vector2[verts.Length];
            var offset = new Vector2(seed * 13.37f, seed * 7.77f);

            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings;
                for (int s = 0; s <= segments; s++)
                {
                    float angle = s / (float)segments * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                    // square inner edge: distance to the square boundary at this angle,
                    // blending back to the circular outer edge across the ring
                    float inner = innerShape == InnerShape.Square
                        ? innerRadius / Mathf.Max(Mathf.Abs(cos), Mathf.Abs(sin))
                        : innerRadius;
                    float radius = Mathf.Lerp(inner, outerRadius, t);
                    float x = cos * radius;
                    float z = sin * radius;
                    // two noise octaves, faded in from the inner edge so the ring meets the map flat
                    float n = Mathf.PerlinNoise(x * noiseScale + offset.x, z * noiseScale + offset.y)
                              + 0.4f * Mathf.PerlinNoise(x * noiseScale * 3f + offset.y, z * noiseScale * 3f + offset.x);
                    float y = n * hillHeight * Mathf.SmoothStep(0f, 1f, t);
                    verts[r * vertsPerRing + s] = new Vector3(x, y, z);
                    uvs[r * vertsPerRing + s] = new Vector2(s / (float)segments, t);
                }
            }

            var tris = new int[segments * rings * 6];
            int i = 0;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = r * vertsPerRing + s;
                    int b = a + vertsPerRing;
                    tris[i++] = a; tris[i++] = a + 1; tris[i++] = b;
                    tris[i++] = a + 1; tris[i++] = b + 1; tris[i++] = b;
                }
            }

            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            var renderer = GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
