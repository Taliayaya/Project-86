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
    
    [CreateAssetMenu(fileName = "Graphics Parameters", menuName = "Scriptable Objects/Graphics Parameters")]
    public class GraphicsParameters : GameParameters
    {
        public GraphicsQuality quality = GraphicsQuality.Medium;
        public override string GetParametersName => "Graphics";

        private void OnEnable()
        {
            EventManager.AddListener("UpdateGameParameter:quality", OnUpdateGraphicsQuality);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("UpdateGameParameter:quality", OnUpdateGraphicsQuality);
        }

        private void OnUpdateGraphicsQuality(object arg0)
        {
            Debug.Log("Changing Graphics Quality to " + arg0);
            QualitySettings.SetQualityLevel((int) quality, true);
        }

        public override void LoadFromFile()
        {
            base.LoadFromFile();
            QualitySettings.SetQualityLevel((int) quality, true);
        }
    }
}