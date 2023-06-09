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
            EventManager.AddListener("PauseGame", OnPauseGame );
            EventManager.AddListener("ResumeGame", OnResumeGame);
            EventManager.AddListener("UpdateGameParameter:musicVolume",OnUpdateMusicVolume);
            EventManager.AddListener("UpdateGameParameter:sfxVolume", OnUpdateSfxVolume);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("PauseGame", OnPauseGame );
            EventManager.RemoveListener("ResumeGame", OnResumeGame);
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
            float volume = (float)data;
            masterMixer.SetFloat("musicVol", volume);
        }

        private void OnUpdateSfxVolume(object data)
        {
            float volume = (float)data;
            masterMixer.SetFloat("sfxVol", volume);
        }
    }
}