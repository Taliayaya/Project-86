using System;
using ScriptableObjects.Tutorial;
using TMPro;
using UnityEngine;

namespace UI.HUD
{
    public class TutorialWindow : MonoBehaviour
    {
        [SerializeField] private GameObject _tutorialWindow;
        [SerializeField] private TMP_Text _tutorialText;
        [SerializeField] private TMP_Text _tutorialTitle;

        private void Awake()
        {
            Close();
        }

        public void Set(string title, string text)
        {
            Open();
            _tutorialTitle.text = title;
            _tutorialText.text = text;
        }
        
        public void Set(GameplayTutorialSO tutorial)
        {
            Open();
            _tutorialTitle.text = tutorial.title;
            _tutorialText.text = tutorial.text;
        }
        
        public void Close()
        {
            _tutorialWindow.SetActive(false);
        }
        
        public void Open()
        {
            _tutorialWindow.SetActive(true);
        }
        
    }
}