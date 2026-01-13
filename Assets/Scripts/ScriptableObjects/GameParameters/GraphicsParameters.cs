using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.GameParameters
{
    public enum GraphicsQuality
    {
        VeryLow,
        Low,
        Medium,
        High
    }

    [System.Serializable]
    public struct ResolutionData
    {
        public int width;
        public int height;
        public int refresh_rate;

        public ResolutionData(int width, int height, int refresh_rate)
        {
            this.width = width;
            this.height = height;
            this.refresh_rate = refresh_rate;
        }

        public override string ToString()
        {
            return $"{width} x {height} ({refresh_rate}Hz)";
        }
    }

    [CreateAssetMenu(
        fileName = "Graphics Parameters",
        menuName = "Scriptable Objects/Graphics Parameters"
    )]
    public class GraphicsParameters : GameParameters
    {
        public GraphicsQuality quality = GraphicsQuality.Medium;
        public ResolutionData current_resolution;
        public DisplayInfo current_display;

        [Range(0, 100)]
        public int detailsDensity = 100;

        // use lists instead of the default supported enums because the data are dynamically generated
        [HideInInspector]
        public List<ResolutionData> resolutions = new List<ResolutionData>();

        [HideInInspector]
        public List<DisplayInfo> displays = new List<DisplayInfo>();

        public override string GetParametersName => "Graphics";

        public void Initialize()
        {
            EventManager.AddListener("UpdateGameParameter:quality", OnUpdateGraphicsQuality);
            EventManager.AddListener("UpdateGameParameter:resolution", OnChangeResolution);
            EventManager.AddListener("UpdateGameParameter:display", OnChangeDisplay);
        }

        public void Deinitialize()
        {
            EventManager.RemoveListener("UpdateGameParameter:quality", OnUpdateGraphicsQuality);
            EventManager.RemoveListener("UpdateGameParameter:resolution", OnChangeResolution);
            EventManager.RemoveListener("UpdateGameParameter:display", OnChangeDisplay);
        }

        private void OnUpdateGraphicsQuality(object _)
        {
            Debug.Log("Changing Graphics Quality to " + quality);
            QualitySettings.SetQualityLevel((int)quality, true);
        }

        private void OnChangeResolution(object _)
        {
            Debug.Log("Changing resolution to " + current_resolution);
            ApplyResolution(current_resolution);
        }

        private void OnChangeDisplay(object _)
        {
            Debug.Log("Changing display to " + current_display.name);
            Screen.MoveMainWindowTo(current_display, Vector2Int.zero);
            ApplyResolution(current_resolution);
        }

        private void ApplyResolution(ResolutionData res)
        {
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refresh_rate);
        }

        private void BuildResolutionList()
        {
            resolutions.Clear();
            foreach (var r in Screen.resolutions)
            {
                int refreshRate = Mathf.RoundToInt((float)r.refreshRateRatio.value);
                var data = new ResolutionData(r.width, r.height, refreshRate);
                if (!resolutions.Contains(data))
                    resolutions.Add(data);
            }
            if (!resolutions.Contains(current_resolution))
                current_resolution = resolutions[^1];
        }

        private void BuildDisplayList()
        {
            displays.Clear();
            Screen.GetDisplayLayout(displays);
            if (!displays.Contains(current_display))
                current_display = displays[0];
        }

        public override void LoadFromFile()
        {
            base.LoadFromFile();
            BuildDisplayList();
            BuildResolutionList();
            ApplyResolution(current_resolution);
            QualitySettings.SetQualityLevel((int)quality, true);
        }
    }
}
