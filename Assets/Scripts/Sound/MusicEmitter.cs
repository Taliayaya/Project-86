using System;
using DefaultNamespace.Sound;
using UnityEngine;

namespace Sound
{
    public class MusicEmitter : MonoBehaviour
    {
        [SerializeField] private AudioClip music;
        private void Start()
        {
            SoundManager.ChangeMusic(music);
        }
    }
}