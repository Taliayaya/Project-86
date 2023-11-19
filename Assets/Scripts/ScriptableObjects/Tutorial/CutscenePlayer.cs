using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ScriptableObjects.Tutorial
{
    [RequireComponent(typeof(PlayableDirector))]
    public class CutscenePlayer : MonoBehaviour
    {
        public int id;
        public TimelineAsset playableAsset;
        private PlayableDirector _director;
        
        public bool playOnStart = false;
        public bool playOncePerSession = false;

        private void Awake()
        {
            _director = GetComponent<PlayableDirector>();
        }

        void Play()
        {
            if (playOncePerSession && CutsceneManager.HasPlayedCutscene(id)) return;
            CutsceneManager.AddPlayedCutscene(id);
            _director.Play(playableAsset);
        }

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }
    }
}