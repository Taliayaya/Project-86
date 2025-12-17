using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FMODUnity
{
    public enum ParameterType
    {
        Continuous,
        Discrete,
        Labeled,
    }

    public class EditorParamRef : ScriptableObject
    {
        [SerializeField]
        public string Name;
        [SerializeField]
        public string StudioPath;
        [SerializeField]
        public float Min;
        [SerializeField]
        public float Max;
        [SerializeField]
        public float Default;
        [SerializeField]
        public ParameterID ID;
        [SerializeField]
        public ParameterType Type;
        [SerializeField]
        public bool IsGlobal;
        [SerializeField]
        public string[] Labels = { };

        public bool Exists;

        [Serializable]
        public struct ParameterID
        {
            public static implicit operator ParameterID(FMOD.Studio.PARAMETER_ID source)
            {
                return new ParameterID {
                    data1 = source.data1,
                    data2 = source.data2,
                };
            }

            public static implicit operator FMOD.Studio.PARAMETER_ID(ParameterID source)
            {
                return new FMOD.Studio.PARAMETER_ID {
                    data1 = source.data1,
                    data2 = source.data2,
                };
            }

            public bool Equals(FMOD.Studio.PARAMETER_ID other)
            {
                return data1 == other.data1 && data2 == other.data2;
            }

            public uint data1;
            public uint data2;
        }
    }
}
