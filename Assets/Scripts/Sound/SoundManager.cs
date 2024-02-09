using System;
using System.Collections;
using System.Reflection;
using ScriptableObjects.GameParameters;
using UnityEngine;
using UnityEngine.Audio;

namespace DefaultNamespace.Sound
{
    public class SoundManager : Singleton<SoundManager>
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
            EventManager.AddListener("LoadingLoadingScene", OnLoadingScene);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("OnPause", OnPauseGame );
            EventManager.RemoveListener("OnResume", OnResumeGame);
            EventManager.RemoveListener("UpdateGameParameter:musicVolume",OnUpdateMusicVolume);
            EventManager.RemoveListener("UpdateGameParameter:sfxVolume", OnUpdateSfxVolume);
            EventManager.RemoveListener("LoadingLoadingScene", OnLoadingScene);
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
        
        
        private IEnumerator FadeOutMusic()
        {
            for (int i = 1; i <= 60; i++)
            {
                audioSource.volume = Mathf.Lerp(audioSource.volume, 0f, (float)i / 60);
                yield return new WaitForSeconds(0.1f);
            }
            audioSource.Stop();
            audioSource.volume = 1f;
        }
        
        public static void ChangeMusic(AudioClip clip)
        {
            Instance.audioSource.clip = clip;
            Instance.audioSource.Play();
            Debug.Log("Changed Music");
        }
        
        public static void PlayOneShot(AudioClip clip)
        {
            Instance.audioSource.PlayOneShot(clip);
        }
        
        public static void StopMusic(float fadeTime)
        {
            Instance.StartCoroutine(Instance.FadeOutMusic());
        }
        
        public static void StopMusicLoop()
        {
            Instance.audioSource.loop = false;
        }
        
        private void OnLoadingScene()
        {
            Debug.Log("Loading Scene");
            StartCoroutine(FadeOutMusic());
        }

    }
}