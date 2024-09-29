using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScriptableObjects.Skins
{
    
    [CreateAssetMenu(fileName = "PersonalMark", menuName = "Scriptable Objects/Cosmetic/PersonalMark", order = 1)]
    public class PersonalMarkSO : ScriptableObject
    {
        public string pmName;
        [TextArea]
        public string description;

        public string note;
        public string author = "";
        public string discord_name = "";
        public string twitter_name = "";

        public Texture2D image;
    }
}