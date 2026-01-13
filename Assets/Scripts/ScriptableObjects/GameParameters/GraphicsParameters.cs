// TODO: naming convention (resolution and display instead of current_...)
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

    # region Structs
    // custom structs instead of built-in ones because of serializability

    [System.Serializable]
    public struct ResolutionData : IEquatable<ResolutionData>
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

        public bool Equals(ResolutionData other)
        {
            return width == other.width && height == other.height && refresh_rate == other.refresh_rate;
        }

        public override bool Equals(object obj)
        {
            return obj is ResolutionData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(width, height, refresh_rate);
        }

        public override string ToString()
        {
            return $"{width} x {height} ({refresh_rate}Hz)";
        }
    }


    [System.Serializable]
    public struct DisplayData : IEquatable<DisplayData>
    {
        public string name;
        public Vector2Int position;
        public Vector2Int resolution;

        public DisplayData(string name, Vector2Int position, Vector2Int resolution)
        {
            this.name = name;
            this.position = position;
            this.resolution = resolution;
        }

        public bool Equals(DisplayData other)
        {
            return name == other.name && position == other.position && resolution == other.resolution;
        }

        public override bool Equals(object obj)
        {
            return obj is DisplayData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(name, position, resolution);
        }

        public override string ToString()
        {
            return $"{name} ({resolution.x} x {resolution.y})";
        }
    }

    #endregion

    [CreateAssetMenu(
        fileName = "Graphics Parameters",
        menuName = "Scriptable Objects/Graphics Parameters"
    )]
    public class GraphicsParameters : GameParameters
    {
        public GraphicsQuality quality = GraphicsQuality.Medium;
        public ResolutionData current_resolution;
        public DisplayData current_display;

        [Range(0, 100)]
        public int detailsDensity = 100;

        // use lists instead of the default supported enums because the data are dynamically generated
        [HideInInspector]
        public List<ResolutionData> resolutions = new();

        [HideInInspector]
        public List<DisplayData> displays = new();

        public override string GetParametersName => "Graphics";

        public void Initialize()
        {
            EventManager.AddListener("UpdateGameParameter:quality", OnUpdateGraphicsQuality);
            EventManager.AddListener("UpdateGameParameter:current_resolution", OnChangeResolution);
            EventManager.AddListener("UpdateGameParameter:current_display", OnChangeDisplay);
        }

        public void Deinitialize()
        {
            EventManager.RemoveListener("UpdateGameParameter:quality", OnUpdateGraphicsQuality);
            EventManager.RemoveListener("UpdateGameParameter:current_resolution", OnChangeResolution);
            EventManager.RemoveListener("UpdateGameParameter:current_display", OnChangeDisplay);
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
            ApplyDisplay(current_display);
            ApplyResolution(current_resolution);
        }

        #region Helper Functions
        private void ApplyResolution(ResolutionData res)
        {
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refresh_rate);
        }

        private void ApplyDisplay(DisplayData disp)
        {
            List<DisplayInfo> available_displays = new List<DisplayInfo>();
            Screen.GetDisplayLayout(available_displays);

            // find DisplayInfo that matches DisplayData to pass to Screen.MoveMainWindowTo
            DisplayInfo? targetDisplay = null;
            foreach (var d in available_displays)
            {
                if (d.name == disp.name &&
                    d.width == disp.resolution.x &&
                    d.height == disp.resolution.y)
                {
                    targetDisplay = d;
                    break;
                }
            }
            Screen.MoveMainWindowTo(targetDisplay.Value, Vector2Int.zero);
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

            List<DisplayInfo> available_displays = new List<DisplayInfo>();
            Screen.GetDisplayLayout(available_displays);

            foreach (var d in available_displays)
            {
                var displayData = new DisplayData(
                    d.name,
                    new Vector2Int(d.workArea.x, d.workArea.y),
                    new Vector2Int(d.width, d.height)
                );

                if (!displays.Contains(displayData))
                    displays.Add(displayData);
            }

            if (!displays.Contains(current_display))
                current_display = displays[0];
        }

        #endregion
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
