using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace ScriptableObjects.UI
{
    [CreateAssetMenu(fileName = "HUDWindowSettings", menuName = "Scriptable Objects/UI/HUD Window Settings")]
    public class HUDWindowSettings : BinarySerializableSO
    {
        [DefaultValue] public string title;
        [DefaultValue] public Vector2 position;
        [DefaultValue] public float sizeMultiplier = 1;
                      // opacity
        [DefaultValue] public float inactiveOpacity;
        [DefaultValue] public float activeOpacity;
        [DefaultValue] public float zoomOpacity;

        [DefaultValue] public bool isActive = true;
        [DefaultValue] public int orderInLayer;
        
        [DefaultValue] public bool showBorder = false;
        
        [DefaultValue, Header("Rights")] public bool isDraggable = true;
        [DefaultValue] public bool isEditable = false;
        [DefaultValue] public bool isResizable = true;
        [DefaultValue] public bool isClosable = true;
        [DefaultValue] public bool isOpacityEditable = true;

    }
}