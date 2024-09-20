using System;
using ScriptableObjects.Skins;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public class PersonalMarkCell : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private JuggConfigSO _juggConfigSo;
        private PersonalMarkSO _personalMarkSo;

        [SerializeField] private bool isSelected;
        [SerializeField] private bool isHover;
        [SerializeField] private GameObject selectedBorder;
        [SerializeField] private GameObject hoverBorder;
        
        private const int PrefabImageID = 2;
        private const int PrefabNameId = 3;

        private void OnEnable()
        {
            EventManager.AddListener(Constants.TypedEvents.OnChangedPersonalMark, OnChangedPersonalMark);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.TypedEvents.OnChangedPersonalMark, OnChangedPersonalMark);
        }

        private void OnChangedPersonalMark(object value)
        {
            IsSelected = _personalMarkSo.name == _juggConfigSo.PersonalMark.name;
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                isSelected = value;
                selectedBorder.SetActive(value);
                
            }
        }

        public bool IsHover
        {
            get => isHover;
            set
            {
                isHover = value;
                hoverBorder.SetActive(value);
            }
        }

        public void Init(JuggConfigSO juggConfigSo, PersonalMarkSO personalMarkSo)
        {
            Debug.Log(personalMarkSo.name);
            Debug.Log(juggConfigSo.name);
            _juggConfigSo = juggConfigSo;
            _personalMarkSo = personalMarkSo;

            IsSelected = juggConfigSo.PersonalMark != null && _juggConfigSo.PersonalMark.name == personalMarkSo.name;
            transform.GetChild(PrefabImageID).GetComponent<RawImage>().texture = personalMarkSo.image;
            transform.GetChild(PrefabNameId).GetComponent<TMP_Text>().text = personalMarkSo.pmName;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsSelected)
                return;
            _juggConfigSo.PersonalMark = _personalMarkSo;
            _juggConfigSo.SaveToFile();
            IsSelected = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsHover = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsHover = false;
        }
    }
}