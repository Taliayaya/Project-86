using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.Mecha;
using ScriptableObjects.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace UI.HUD.HUDWindowSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HUDWindow : MonoBehaviour
    {
        public enum HUDWindowState
        {
            Edit,
            Play
        }
        [SerializeField] private RectTransform childTransform;
        [SerializeField] private List<GameObject> childObjects;
    
        private CanvasGroup _canvasGroup;
    
        public HUDWindowSettings settings;
        private Vector2 _defaultSize;

        private HUDWindowState _windowState = HUDWindowState.Play;
    
        [Header("Prefabs")]
        [SerializeField] private GameObject windowEditPrefab;
    
        private GameObject windowEditInstance;
    
        public HUDWindow.HUDWindowState WindowState
        {
            get => _windowState;
            set
            {
                if (value == HUDWindowState.Edit)
                {
                    windowEditInstance = Instantiate(windowEditPrefab, childTransform);
                    windowEditInstance.GetComponent<HUDWindowEdit>().Init(this);
                    _canvasGroup.alpha = settings.activeOpacity;
                }
                else
                {
                    Destroy(windowEditInstance);
                }
                if (WindowState == HUDWindowState.Edit && value == HUDWindowState.Play)
                    settings.SaveToFile(name);
                _windowState = value;
                onWindowModeChanged.Invoke(this);
            }
        }


        public UnityEvent<HUDWindow> onWindowModeChanged = new UnityEvent<HUDWindow>();

        #region Properties

        public bool IsActivated
        {
            get => settings.isActive;
            set
            {
                childObjects.ForEach(child => child.SetActive(value));
                settings.isActive = value;
            }
        }

        public Vector2 Position
        {
            get => settings.position;
            set
            {
                settings.position = value;
                childTransform.anchoredPosition = value;
            }
        }

        public float SizeMultiplier
        {
            get => settings.sizeMultiplier;
            set
            {
                value = Mathf.Clamp(value, 0.25f, 4f);
                Debug.Log("SizeMultiplier: " + value, this);
                settings.sizeMultiplier = value;
                childTransform.sizeDelta = _defaultSize * value;
            }
        }

        // Opacity
        public float Opacity
        {
            get => _canvasGroup.alpha;
            set
            {
                _canvasGroup.alpha = value;
            }
        }

        #endregion
    
        public void Fade(float alpha, float duration)
        {
            StartCoroutine(FadeCoroutine(alpha, duration));
        }
    
        private IEnumerator FadeCoroutine(float alpha, float duration)
        {
            float startAlpha = Opacity;
            float time = 0;
            while (time < duration)
            {
                Opacity = Mathf.Lerp(startAlpha, alpha, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
            Opacity = alpha;
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _defaultSize = childTransform.sizeDelta;
            settings.SaveToFileDefault(name);
            settings.LoadFromFile(name);
            UsePreset(settings);
        }

        private void OnEnable()
        {
            EventManager.AddListener("OnPause", OnPause);
            EventManager.AddListener("OnResume", OnResume);
            EventManager.AddListener("OnDeath", OnDeath);
            EventManager.AddListener("OnRespawn", OnRespawn);
            EventManager.AddListener("OnZoomChange", OnZoomChange);
            EventManager.AddListener("OnHUDEdit", OnHUDEdit);
        }

        private void OnZoomChange(object zoomType)
        {
            MechaController.Zoom zoom = (MechaController.Zoom) zoomType;
            switch (zoom)
            {
                case MechaController.Zoom.Default:
                    Opacity = settings.activeOpacity;
                    break;
                case MechaController.Zoom.X2:
                    Opacity = settings.zoomOpacity;
                    break;
                case MechaController.Zoom.X4:
                    break;
                case MechaController.Zoom.X8:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("OnPause", OnPause);
            EventManager.RemoveListener("OnResume", OnResume);
            EventManager.RemoveListener("OnDeath", OnDeath);
            EventManager.RemoveListener("OnRespawn", OnRespawn);
            EventManager.RemoveListener("OnZoomChange", OnZoomChange);
            EventManager.RemoveListener("OnHUDEdit", OnHUDEdit);
        }

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        #region Event Callbacks

        private void OnDeath()
        {
            if (IsActivated)
                Fade(0, 0.3f);
        }
    
        private void OnRespawn()
        {
            if (IsActivated)
                Fade(settings.activeOpacity, 0.3f);
        }
    
        private void OnPause()
        {
            if (IsActivated)
                Fade(0.1f, 0f);
        }
    
        private void OnResume()
        {
            if (IsActivated)
                Fade(settings.activeOpacity, 0f);
        }
    

        #endregion
    
        public void UsePreset(HUDWindowSettings preset)
        {
            settings = preset;
            IsActivated = settings.isActive;
            Position = settings.position;
            SizeMultiplier = settings.sizeMultiplier;
            Opacity = settings.activeOpacity;
            transform.SetSiblingIndex(settings.orderInLayer);
        }

#if UNITY_EDITOR
        public void SetPreset()
        {
            if (!childTransform)
                childTransform = transform.GetChild(0).GetComponent<RectTransform>();
            settings.position = childTransform.anchoredPosition;
            settings.activeOpacity = GetComponent<CanvasGroup>().alpha;
            settings.sizeMultiplier = 1;
            settings.orderInLayer = transform.GetSiblingIndex();
            AssetDatabase.SaveAssets();
        }
#endif

        public void DeleteSave()
        {
            settings.DeleteSave(name);
        }
    
        public void OnHUDEdit(object isEditing)
        {
            if (!settings.isEditable)
                return;
            WindowState = (bool)isEditing ? HUDWindowState.Edit : HUDWindowState.Play;
        }

        public void ResetSettings()
        {
            DeleteSave();
            settings.ResetToDefault(name);
            Debug.Log("Resetting settings to default" + settings.activeOpacity, this);
            UsePreset(settings);
        }

    }
}
