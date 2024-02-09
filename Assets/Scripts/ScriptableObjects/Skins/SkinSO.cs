using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.Skins
{
    
    [CreateAssetMenu(fileName = "Skin", menuName = "Scriptable Objects/Skin", order = 1)]
    public class SkinSO : ScriptableObject
    {
        public string skinName;
        [Serializable]
        public struct RendererMaterials
        {
            public string name;
            public Material[] materials;
        }
        public List<RendererMaterials> rendererMaterials;
    }
}