using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class DeathHUD : MonoBehaviour
    {
        [Header("Death UI")] [SerializeField] private List<MaskableGraphic> deathImages;
        [SerializeField] private GameObject deathPanel;


        private void Awake()
        {
            deathImages.ForEach((d) => d.CrossFadeAlpha(0, 0, true));
            deathPanel.SetActive(false);
        }

        private void OnEnable()
        {
            EventManager.AddListener("OnDeath", OnDeath);
            EventManager.AddListener("OnRespawn", OnRespawn);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener("OnDeath", OnDeath);
            EventManager.RemoveListener("OnRespawn", OnRespawn);
        }
        
        

        private void OnRespawn(object arg0)
        {
            StopAllCoroutines();
            StartCoroutine(SetDeathPanel(false));
        }

        private void OnDeath(object arg0)
        {
            var deathData = (DeathData) arg0;
            StartCoroutine(SetDeathPanel(true));
        }

        IEnumerator SetDeathPanel(bool on)
        {
            if (on)
            {
                deathPanel.SetActive(true);
                deathImages.ForEach((d) => d.CrossFadeAlpha(1, 0.2f, true));
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                deathImages.ForEach((d) => d.CrossFadeAlpha(0, 0.2f, true));
                yield return new WaitForSeconds(0.2f);
                deathPanel.SetActive(false);
            }
        }
    }
}
