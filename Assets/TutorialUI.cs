using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects.Tutorial;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUI : MonoBehaviour
{
    [Header("Tutorials")] [SerializeField] private List<TutorialSO> tutorials;
    
    [Header("Tutorial References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image image;
    
    [Header("Menu References")]
    [SerializeField] private RectTransform menuContentParent;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject menuButtonPrefab;
    
    private int _currentTutorialIndex = 0;

    private void Awake()
    {
        AddTutorialsMenu();
    }

    private void AddTutorialsMenu()
    {
        for (int i = 0; i < tutorials.Count; i++)
        {
            AddTutorialMenu(tutorials[i]);
        }
        SelectTutorial(tutorials[0].tutorials[0], 0);
    }

    private void AddTutorialMenu(TutorialSO tutorialSo, int index = 0)
    {
        var menuButton = Instantiate(menuButtonPrefab, menuContentParent);
        menuButton.GetComponentInChildren<TMP_Text>().text = tutorialSo.tutorials[0].title;
        menuButton.GetComponentInChildren<Button>().onClick.AddListener(() => SelectTutorial(tutorialSo.tutorials[0], index));
    }

    private void SelectTutorial(Tutorial tutorial, int index)
    {
        descriptionText.text = tutorial.description;
        titleText.text = tutorial.title;
        image.sprite = tutorial.image;
        _currentTutorialIndex = index;
    }
    
    
    private void OpenTutorial()
    {
        tutorialPanel.SetActive(true);
    }
    
    private void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
    }

    public void Open()
    {
        WindowManager.Open(OpenTutorial, CloseTutorial);
    }

    public void Close()
    {
        WindowManager.Close();
    }
}
