using System;
using System.Collections.Generic;
using ScriptableObjects.Skins;
using UnityEngine;

namespace Cosmetic
{
    [Serializable]
    public struct MeshRendererGroup
    {
        public string name;
        public List<MeshRenderer> meshRenderers;
    }
    public class ChangeSkin : MonoBehaviour
    {
        [SerializeField] private SkinSO defaultSkin;
        [SerializeField] private List<MeshRendererGroup> meshRenderers;
        
        private void Start()
        {
            ChangeMaterials(defaultSkin);
        }

        public void ChangeMaterials(SkinSO skin)
        {
            if (skin.rendererMaterials.Count != meshRenderers.Count)
            {
                Debug.LogError("Skin materials count does not match mesh renderers count");
                return;
            }
            for (int i = 0; i < meshRenderers.Count; i++)
            {
                var meshRendererGroup = meshRenderers[i];
                var materials = skin.rendererMaterials[i].materials;
                foreach (var meshRenderer in meshRendererGroup.meshRenderers)
                {
                    meshRenderer.materials = materials;
                }
            }
        }
        
        public void ChangeSkinToDefault()
        {
            ChangeMaterials(defaultSkin);
        }
        
    }
}