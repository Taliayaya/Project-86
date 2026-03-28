using System;

namespace FMODUnity
{
    [Serializable]
    public class ParamRef
    {
        public string Name;
        public float Value;
        public FMOD.Studio.PARAMETER_ID ID;
    }
}
