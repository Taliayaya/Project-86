using System;
using UnityEngine;

namespace UI.HUD
{
    public class AudioSpectrum : MonoBehaviour
    {
        [SerializeField] protected AudioSource audioSource;
        [SerializeField] protected Transform spectrumLinesTransform;
        [SerializeField] private GameObject audioLine;
        [SerializeField] protected float heightMultiplier = 1000;
        [SerializeField] protected float maxHeight = 60;
        [SerializeField] private int humanSpectrumMax = 64;

        public float[] samples = new float[512];

        private GameObject[] sampleLines;
        // Start is called before the first frame update

        private void InstantiateLines()
        {
            sampleLines = new GameObject[humanSpectrumMax];
            for (int i = 0; i < humanSpectrumMax; ++i)
            {
                sampleLines[i] = Instantiate(audioLine, spectrumLinesTransform);
            }
        }

        private void Awake()
        {
            InstantiateLines();
        }

        // Update is called once per frame
        void Update()
        {
            if (audioSource.isPlaying)
                GetSpectrumAudioSource();
        }

        void GetSpectrumAudioSource()
        {
            audioSource.GetSpectrumData(samples, 0, FFTWindow.Blackman);
            for (int i = 0; i < humanSpectrumMax; ++i)
            {
                RectTransform t = (RectTransform)sampleLines[i].transform;
                float height = Mathf.Clamp(samples[i] * heightMultiplier, 0, maxHeight);
                t.sizeDelta = new Vector2(t.sizeDelta.x, height);
            }
        }
    }
}
