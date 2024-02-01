using System;
using System.Collections;
using UnityEngine;

namespace Utility
{
    [RequireComponent(typeof(Light))]
    public class LightController : MonoBehaviour
    {
        private Light _light;
        private float _intensity;
        
        public float duration = 1;
        private void Awake()
        {
           _light = GetComponent<Light>(); 
           _intensity = _light.intensity;
        }
        
        public void SetIntensity(float intensity)
        {
            _light.intensity = intensity;
        }

        public void ResetIntensity()
        {
            _light.intensity = _intensity;
        }
        
        public void ResetIntensityTransition()
        {
            StartCoroutine(IntensityTransition(_intensity));
        }

        public void SetIntensityTransition(float intensity)
        {
            StartCoroutine(IntensityTransition(intensity));
        }
        
        
        private IEnumerator IntensityTransition(float intensity)
        {
            float t = 0;
            float startIntensity = _light.intensity;
            while (t < duration)
            {
                t += Time.deltaTime;
                _light.intensity = Mathf.Lerp(startIntensity, intensity, t / duration);
                yield return null;
            }
        }
        
        
    }
}