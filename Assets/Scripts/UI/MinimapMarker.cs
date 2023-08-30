using System;
using UnityEngine;
using UnityEngine.Events;

namespace UI
{
    public class MinimapMarker : MonoBehaviour
    {
        private Renderer _renderer;
        
        public bool IsVisible => _renderer.isVisible;
        public UnityEvent<bool> onVisibilityChanged; 
        public Sprite icon;
        public Color color;
        
        private void Awake()
        {
           _renderer = GetComponent<Renderer>(); 
        }

        private void Start()
        {
            EventManager.TriggerEvent("MinimapMarkerVisibilityChanged", this);
        }

        private void OnBecameInvisible()
        {
            EventManager.TriggerEvent("MinimapMarkerVisibilityChanged", this);
            Debug.Log("Marker became invisible");
            onVisibilityChanged?.Invoke(false);
        }
        
        private void OnBecameVisible()
        {
            EventManager.TriggerEvent("MinimapMarkerVisibilityChanged", this);
            Debug.Log("Marker became visible");
            onVisibilityChanged?.Invoke(true);
        }
    }
}