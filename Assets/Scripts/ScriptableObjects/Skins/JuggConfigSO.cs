using System;
using UnityEngine;

namespace ScriptableObjects.Skins
{
    [CreateAssetMenu(fileName = "JuggConfig", menuName = "Scriptable Objects/Cosmetic/JuggConfig", order = 1)]
    public class JuggConfigSO : BinarySerializableSO
    {
        public string personalMarkFileName;

        private PersonalMarkSO _personalMarkSo;
        
        public PersonalMarkSO PersonalMark
        {
            get => _personalMarkSo;
            set
            {
                _personalMarkSo = value;
                personalMarkFileName = _personalMarkSo.name;
                EventManager.TriggerEvent(Constants.TypedEvents.OnChangedPersonalMark, value);
            }
        }

        private void OnEnable()
        {
            _personalMarkSo =
                Resources.Load<PersonalMarkSO>($"ScriptableObjects/Skins/PersonalMarks/{personalMarkFileName}");
        }

        public override void LoadFromFile()
        {
            base.LoadFromFile();
            _personalMarkSo =
                Resources.Load<PersonalMarkSO>($"ScriptableObjects/Skins/PersonalMarks/{personalMarkFileName}");
        }
    }
}