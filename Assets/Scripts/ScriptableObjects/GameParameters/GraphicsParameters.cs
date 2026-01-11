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
            return $"{width}x{height}@{refresh_rate}";
        }
    }

    [CreateAssetMenu(
        fileName = "Graphics Parameters",
        menuName = "Scriptable Objects/Graphics Parameters"
    )]
    public class GraphicsParameters : GameParameters
    {
        [HideInInspector]
        public List<ResolutionData> resolutions = new List<ResolutionData>();
        public ResolutionData current_resolution;

        public GraphicsQuality quality = GraphicsQuality.Medium;

        [Range(0, 100)]
        public int detailsDensity = 100;

        public override string GetParametersName => "Graphics";

        private void OnEnable()
        {
            // ERROR: not getting triggered for some reason?
            Debug.Log("Adding listeners for resolution and quality");
            EventManager.AddListener("UpdateGameParameter:quality", OnUpdateGraphicsQuality);
            EventManager.AddListener("UpdateGameParameter:resolution", OnResolutionChanged);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("UpdateGameParameter:quality", OnUpdateGraphicsQuality);
            EventManager.RemoveListener("UpdateGameParameter:resolution", OnResolutionChanged);
        }

        private void OnUpdateGraphicsQuality(object _)
        {
            QualitySettings.SetQualityLevel((int)quality, true);
        }

        private void OnResolutionChanged(object _)
        {
            Debug.Log("applying new resolution");
            ApplyResolution(current_resolution);
        }

        [Obsolete]
        private void ApplyResolution(ResolutionData res)
        {
            Screen.SetResolution(
                res.width,
                res.height,
                Screen.fullScreenMode,
                res.refresh_rate
            );
        }

        private void BuildResolutionList()
        {
            resolutions.Clear();

            foreach (var r in Screen.resolutions)
            {
                int refreshRate = Mathf.RoundToInt((float)r.refreshRateRatio.value);

                var data = new ResolutionData(
                    r.width,
                    r.height,
                    refreshRate
                );

                if (!resolutions.Contains(data))
                {
                    resolutions.Add(data);
                }
            }

            int initial_index = resolutions.Count - 1;
            current_resolution = resolutions.Count > 0 ? resolutions[initial_index] : default;
        }

        public override void LoadFromFile()
        {
            base.LoadFromFile();

            BuildResolutionList();
            ApplyResolution(current_resolution);

            QualitySettings.SetQualityLevel((int)quality, true);
        }
    }
}
