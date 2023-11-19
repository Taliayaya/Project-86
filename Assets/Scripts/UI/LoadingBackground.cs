using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingBackground : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private int imageChangeInterval = 5;
    [SerializeField] private int imageNumber = 2;
    [SerializeField] private float fadeTime = 0.3f;

    private void Awake()
    {
        StartCoroutine(Start());
    }
    
    private IEnumerator Start()
    {
        background.CrossFadeAlpha(0, 0, false);
        int prevI = -1;
        while (true)
        {
            int i;
            do
            {
                i = UnityEngine.Random.Range(0, imageNumber);
            } while (prevI == i && imageNumber > 1);
            prevI = i;
            
            background.sprite = Resources.Load<Sprite>("Sprites/UI/LoadingScreen/Background" + i);
            background.CrossFadeAlpha(1, fadeTime, false);
            yield return new WaitForSeconds(fadeTime + 0.2f);
            yield return new WaitForSeconds(imageChangeInterval);
            background.CrossFadeAlpha(0, fadeTime, false);
            yield return new WaitForSeconds(fadeTime + 0.2f);
        }
    }
}
