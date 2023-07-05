using System;
using System.Reflection;
using ScriptableObjects.GameParameters;
using UnityEngine;
using UnityEngine.Audio;

namespace DefaultNamespace.Sound
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private GameSoundParameters soundParameters;
        
        [Header("Mixers")]
        [SerializeField] private AudioMixerSnapshot paused;
        [SerializeField] private AudioMixerSnapshot unpaused;
        [SerializeField] private AudioMixer masterMixer;

        private void OnEnable()
        {
            EventManager.AddListener("OnPause", OnPauseGame );
            EventManager.AddListener("OnResume", OnResumeGame);
            EventManager.AddListener("UpdateGameParameter:musicVolume",OnUpdateMusicVolume);
            EventManager.AddListener("UpdateGameParameter:sfxVolume", OnUpdateSfxVolume);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("OnPause", OnPauseGame );
            EventManager.RemoveListener("OnResume", OnResumeGame);
            EventManager.RemoveListener("UpdateGameParameter:musicVolume",OnUpdateMusicVolume);
            EventManager.RemoveListener("UpdateGameParameter:sfxVolume", OnUpdateSfxVolume);
        }

        private void OnPauseGame()
        {
            paused.TransitionTo(0.01f);
        }

        private void OnResumeGame()
        {
            unpaused.TransitionTo(0.01f);
        }

        private void OnUpdateMusicVolume(object data)
        {
            Debug.Log("Called Event");
            float volume;
            if (data is float volf)
                volume = volf;
            else if (data is int voli)
                volume = voli;
            else
                throw new ArgumentException("data is not float or int");
            masterMixer.SetFloat("musicVol", volume);
        }

        private void OnUpdateSfxVolume(object data)
        {
            float volume;
            if (data is float volf)
                volume = volf;
            else if (data is int voli)
                volume = voli;
            else
                throw new ArgumentException("data is not float or int");
            masterMixer.SetFloat("sfxVol", volume);
        }
    }
}