using Cosmetic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(ChangeSkin))]
    public class ChangeSkinEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Change Skin"))
            {
                var changeSkin = target as Cosmetic.ChangeSkin;
                if (changeSkin != null) changeSkin.ChangeSkinToDefault();
            }
        }
    }
}