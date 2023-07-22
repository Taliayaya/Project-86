using System;
using JetBrains.Annotations;
using UnityEngine;

namespace ScriptableObjects.UI
{
    [CreateAssetMenu(fileName = "Cursor", menuName = "Scriptable Objects/UI/Cursor")]
    public class CursorSO : ScriptableObject
    {
        [CanBeNull] public Texture2D defaultTexture;
        [CanBeNull] public Texture2D hoverTexture;
        [CanBeNull] public Texture2D dragTexture;
        [CanBeNull] public Texture2D resizeTexture;

        private void OnEnable()
        {
            Cursor.SetCursor(defaultTexture, Vector2.zero, CursorMode.Auto);
        }
    }
}