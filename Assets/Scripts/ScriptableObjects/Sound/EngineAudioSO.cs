using UnityEngine;

namespace ScriptableObjects.Sound
{
    [CreateAssetMenu(fileName = "EngineAudio", menuName = "Scriptable Objects/Sound/EngineAudio")]
    public class EngineAudioSO : ScriptableObject
    {
        public AudioClip engineIdle;
        public AudioClip engineWalking;
        public AudioClip engineSpeeding;
        public AudioClip engineAcceleration;
        public AudioClip engineDeceleration;

    }
}