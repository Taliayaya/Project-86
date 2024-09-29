using System;
using ScriptableObjects.Skins;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public class PersonalMarksMenu : MonoBehaviour
    {
        [SerializeField] private Transform container;
        [SerializeField] private GameObject pmPrefab;

        private JuggConfigSO _juggConfigSo;



        private void Awake()
        {
            

        }

        private void CreateCell(PersonalMarkSO personalMarkSo)
        {
            Instantiate(pmPrefab, container).GetComponent<PersonalMarkCell>().Init(_juggConfigSo, personalMarkSo);
        }

        public void Open()
        {
             _juggConfigSo = Resources.Load<JuggConfigSO>("ScriptableObjects/Skins/PersonalMarks/JuggConfig");
             _juggConfigSo.LoadFromFile();
             
            var personalMarkSos = Resources.LoadAll<PersonalMarkSO>("ScriptableObjects/Skins/PersonalMarks/");

            foreach (var personalMarkSo in personalMarkSos)
                CreateCell(personalMarkSo);
            gameObject.SetActive(true);
            
           
        }

        public void Close()
        {
            gameObject.SetActive(false);
            foreach (Transform child in container.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}