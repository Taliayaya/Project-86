using System;
using System.Collections.Generic;
using NaughtyAttributes;
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
        public bool PreserveAspectRatio;

        public bool HasSecondaryContent;

        [AllowNesting, ShowIf(nameof(HasSecondaryContent))] public SecondaryInfo SecondaryInfo;
    }

    [Serializable]
    public struct SecondaryInfo
    {
        [TextArea(1, 20)] 
        public string Description;

        public Sprite Image;
        public bool PreserveAspectRatio;
    }

    [CreateAssetMenu(fileName = "Tutorial", menuName = "Scriptable Objects/Tutorial")]
    public class TutorialSO : ScriptableObject
    {
        public List<Tutorial> tutorials = new List<Tutorial>(3);
    }
}