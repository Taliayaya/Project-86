using UnityEngine;

namespace ScriptableObjects.GameParameters
{
    [CreateAssetMenu(fileName = "Sound Parameters", menuName = "Scriptable Objects/Game Sound Parameters", order = 1)]
    public class GameSoundParameters : GameParameters
    {
        [DefaultValue(-12), Range(0, 100)] public int musicVolume = 100;
        [DefaultValue(0), Range(0, 100)] public int sfxVolume = 100;
        
        public override string GetParametersName => "Sound";
    }
}