using System;
using System.Collections;
using ScriptableObjects.Skins;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using JuggConfigSO = ScriptableObjects.Skins.JuggConfigSO;

namespace Cosmetic
{
    [RequireComponent(typeof(DecalProjector))]
    public class LoadPersonalMark : MonoBehaviour
    {
        private Material _material;
        private DecalProjector _decalProjector;
        private JuggConfigSO _configSo;
        private static readonly int BaseMap = Shader.PropertyToID("Base_Map");

        private void Awake()
        {
            _decalProjector = GetComponent<DecalProjector>();
            
            _material = new Material(_decalProjector.material);
            //_decalProjector.enabled = false;


        }

        private void Start()
        {
            
            _configSo = Resources.Load<JuggConfigSO>("ScriptableObjects/Skins/PersonalMarks/JuggConfig");
            _configSo.SaveToFile();
            _configSo.SaveToFileDefault();

            _configSo.LoadFromFile();
            _material.EnableKeyword("_BASEMAP");
            _material.SetTexture(BaseMap, _configSo.PersonalMark.image);
            _material.mainTexture = _configSo.PersonalMark.image;
            
            _material.name = "Decal_PM_tmp";
            _decalProjector.material = _material;
            _decalProjector.enabled = true;

            //StartCoroutine(DisplayAllPM());
        }

        IEnumerator DisplayAllPM()
        {
            var allPm = Resources.LoadAll<PersonalMarkSO>("ScriptableObjects/Skins/PersonalMarks/");
            while (true)
            {
                foreach (var personalMarkSo in allPm)
                {
                    _material.SetTexture(BaseMap, personalMarkSo.image);
                    yield return new WaitForSeconds(0.3f);
                }
            }
        }
    }
}