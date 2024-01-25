using System;
using DefaultNamespace.Sound;
using UnityEngine;

namespace Sound
{
    public class MusicEmitter : MonoBehaviour
    {
        [SerializeField] private AudioClip music;
        
        public bool playOnStart = true;
        private void Start()
        {
            if (playOnStart)
                Play();
        }
        
        public void Play()
        {
            SoundManager.ChangeMusic(music);
        }
    }
}