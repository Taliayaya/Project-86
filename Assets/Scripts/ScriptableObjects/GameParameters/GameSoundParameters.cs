using UnityEngine;

namespace ScriptableObjects.GameParameters
{
    [CreateAssetMenu(fileName = "Sound Parameters", menuName = "Scriptable Objects/Game Sound Parameters", order = 1)]
    public class GameSoundParameters : GameParameters
    {
        [DefaultValue(-12), Range(-80, -12)] public int musicVolume = -12;
        [DefaultValue(0), Range(-80, 0)] public int sfxVolume = 0;
        
        public override string GetParametersName => "Sound";
    }
}