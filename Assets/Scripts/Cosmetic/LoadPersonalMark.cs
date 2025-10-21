using System;
using System.Collections;
using Armament.Shared;
using Networking;
using Networking.Widgets.Session.Session;
using ScriptableObjects.Skins;
using Unity.Multiplayer.Widgets;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using JuggConfigSO = ScriptableObjects.Skins.JuggConfigSO;

namespace Cosmetic
{
    [RequireComponent(typeof(DecalProjector))]
    public class LoadPersonalMark : NetworkBehaviour
    {
        private Material _material;
        private DecalProjector _decalProjector;
        private JuggConfigSO _configSo;
        PersonalMarkSO _personalMarkSo;
        private static readonly int BaseMap = Shader.PropertyToID("Base_Map");

        private void Awake()
        {
            _decalProjector = GetComponent<DecalProjector>();
            
            _material = new Material(_decalProjector.material);
            //_decalProjector.enabled = false;
        }

        public static JuggConfigSO LoadConfig()
        {
            var config = Resources.Load<JuggConfigSO>("ScriptableObjects/Skins/PersonalMarks/JuggConfig");
            config.SaveToFile();
            config.SaveToFileDefault();

            config.LoadFromFile();
            return config;
        }

        private void Start()
        {
            _configSo = LoadConfig();
           
            _material.EnableKeyword("_BASEMAP");
            if (!_personalMarkSo)
            {
                Debug.Log("Using config PM");
                if (NetworkManager.Singleton.IsConnectedClient && !IsOwner)
                    LoadRemotePM();
                else
                    _personalMarkSo = _configSo.PersonalMark;
            }

            _material.SetTexture(BaseMap, _personalMarkSo.image);
            _material.mainTexture = _personalMarkSo.image;
            
            _material.name = "Decal_PM_tmp";
            _decalProjector.material = _material;
            _decalProjector.enabled = true;

            //StartCoroutine(DisplayAllPM());
        }

        private void LoadRemotePM()
        {
            MissionManager.Instance.GetPlayerByNetworkId(OwnerClientId, out var playerInfo);
            IReadOnlyPlayer player = SessionManager.Instance.ActiveSession.GetPlayer(playerInfo.Value.PlayerId.Value);
            Load(player.Properties[Constants.Properties.Session.PersonalMark].Value);
        }
        
        private void OnEnable()
        {
            EventManager.AddListener(Constants.TypedEvents.OnChangedPersonalMark, OnChangedPersonalMark);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.TypedEvents.OnChangedPersonalMark, OnChangedPersonalMark);
        }

        public void Load(string personalMarkFileName)
        {
            _personalMarkSo = Resources.Load<PersonalMarkSO>("ScriptableObjects/Skins/PersonalMarks/" + personalMarkFileName);
            if (_personalMarkSo == null)
                Debug.LogError("PersonalMarkSO not found");
            // EventManager.TriggerEvent(Constants.TypedEvents.OnChangedPersonalMark, mark);
        }
        
        private void OnChangedPersonalMark(object mark)
        {
            PersonalMarkSO markSo;
            if (mark is PersonalMarkSO personalMarkSo && personalMarkSo != null)
            {
                markSo = personalMarkSo;
            }else
            {
                markSo = _configSo.PersonalMark;
            }
            
            _material.SetTexture(BaseMap, markSo.image);
            _material.mainTexture = markSo.image;
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