using System;
using System.Collections;
using System.Globalization;
using Gameplay.Mecha;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class ReloadingPopUp : MonoBehaviour
    {
        
        [Header("References")]
        [SerializeField] private GameObject reloadPanel; 
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private TextMeshProUGUI reloadingCooldownText;
        [SerializeField] private RawImage ammoImage;
        [SerializeField] private Texture2D mainAmmoImage;
        [SerializeField] private Texture2D secondaryAmmoImage;

        #region Unity Callbacks

        private void OnEnable()
        {
            EventManager.AddListener("OnReloadWeaponModule", OnReloadModule);
        }
        
        private void OnDisable()
        {
            EventManager.RemoveListener("OnReloadWeaponModule", OnReloadModule);
        }

        #endregion
        
        #region Weapon Reload

        private Coroutine _reloadCoroutine = null;
        private void OnReloadModule(object data)
        {
            ReloadModuleData reloadModuleData = (ReloadModuleData) data;
            reloadPanel.SetActive(true);
            if (_reloadCoroutine != null)
                StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = StartCoroutine(ReloadCoroutine(reloadModuleData));

        }

        private IEnumerator ReloadCoroutine(ReloadModuleData reloadModuleData)
        {
            float timeRemaining = reloadModuleData.ReloadTime;
            weaponNameText.text = reloadModuleData.WeaponName;
            ammoImage.texture = reloadModuleData.Type == WeaponModule.WeaponType.Primary
                ? mainAmmoImage
                : secondaryAmmoImage;
            
            while (timeRemaining > 0)
            {
                string secondsText = timeRemaining.ToString("F1", new CultureInfo("en-US"));
                reloadingCooldownText.text = secondsText;
                timeRemaining -= Time.deltaTime;
                yield return null;
            }
            reloadPanel.SetActive(false);
            _reloadCoroutine = null;
        }

        #endregion
    
    }
}