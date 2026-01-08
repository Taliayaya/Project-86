using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace ScriptableObjects.GameParameters
{
    public enum GraphicsQuality
    {
        VeryLow,
        Low,
        Medium,
        High
    }

    public enum ResolutionOption
    {
        R_1280x720,
        R_1920x1080,
        R_2560x1440
    }

    [CreateAssetMenu(fileName = "Graphics Parameters", menuName = "Scriptable Objects/Graphics Parameters")]
    public class GraphicsParameters : GameParameters
    {
        public GraphicsQuality quality = GraphicsQuality.Medium;
        [Range(0, 100)] public int detailsDensity = 100;
        public override string GetParametersName => "Graphics";
        public ResolutionOption resolution = ResolutionOption.R_1920x1080;

        private void OnEnable()
        {
            EventManager.AddListener("UpdateGameParameter:quality", OnUpdateGraphicsQuality);
            EventManager.AddListener("UpdateGameParameter:resolution", OnResolutionChanged);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("UpdateGameParameter:quality", OnUpdateGraphicsQuality);
            EventManager.RemoveListener("UpdateGameParameter:resolution", OnResolutionChanged);
        }

        private void OnUpdateGraphicsQuality(object arg0)
        {
            Debug.Log("Changing Graphics Quality to " + arg0);
            QualitySettings.SetQualityLevel((int) quality, true);
        }
        private void OnResolutionChanged(object arg0)
        {
            resolution = (ResolutionOption)arg0;

            switch (resolution)
            {
                case ResolutionOption.R_1280x720:
                    Screen.SetResolution(1280, 720, Screen.fullScreen);
                    break;

                case ResolutionOption.R_1920x1080:
                    Screen.SetResolution(1920, 1080, Screen.fullScreen);
                    break;

                case ResolutionOption.R_2560x1440:
                    Screen.SetResolution(2560, 1440, Screen.fullScreen);
                    break;
            }
        }

        public override void LoadFromFile()
        {
            base.LoadFromFile();
            QualitySettings.SetQualityLevel((int) quality, true);
            OnResolutionChanged(resolution);
        }
    }
}
