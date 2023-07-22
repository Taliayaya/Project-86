using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.Tutorial
{
    [Serializable]
    public struct Tutorial
    {
        public string title;
        [TextArea(1, 20)]
        public string description;
        public Sprite image;
    }
    [CreateAssetMenu(fileName = "Tutorial", menuName = "Scriptable Objects/Tutorial")]
    public class TutorialSO : ScriptableObject
    {
        public List<Tutorial> tutorials = new List<Tutorial>(3);
    }
}