using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Random = UnityEngine.Random;

namespace Animations
{
    public class RandomDecal : MonoBehaviour
    {
        public Vector2[] offsets = {
            new Vector2(0.0f, 0.5f), // H1
            new Vector2(0.5f, 0.5f), // H2
            new Vector2(0.0f, 0.0f), // H3
            new Vector2(0.5f, 0.0f)  // H4
        };
        
        public DecalProjector projector;

        private void OnEnable()
        {
            RandomizeDecal();
        }

        [ContextMenu("Randomize Decal")]
        public void RandomizeDecal()
        {
            int i = Random.Range(0, offsets.Length);

            projector.material.SetTextureScale("_BaseMap", new Vector2(0.5f, 0.5f));
            projector.material.SetTextureOffset("_BaseMap", offsets[i]);   
        }
    }
}