using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Gameplay;
using Gameplay.Mecha;
using Gameplay.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class HUDManager : MonoBehaviour
    {
        private UnitState _playedMechaState;
        
        #region Unity Callbacks

        private void Awake()
        {
            var maskableIcon = iconParent.GetComponentsInChildren<MaskableGraphic>();
            //reticleImages.AddRange(maskableIcon);
            FadeZoom(0, 0);
        }

        private void OnEnable()
        {
            //EventManager.AddListener("OnPause", OnPause);
            //EventManager.AddListener("OnResume", OnResume);
            EventManager.AddListener("OnUpdateHealth", OnUpdateHealth);
            EventManager.AddListener("OnUpdatePrimaryAmmoAmount", OnUpdatePrimaryAmmoAmount);
            EventManager.AddListener("OnZoomChange", OnZoomChange);
            EventManager.AddListener("OnUpdateSecondaryAmmoLeftAmount", OnUpdateSecondaryAmmoLeft);
            EventManager.AddListener("OnUpdateSecondaryAmmoRightAmount", OnUpdateSecondaryAmmoRight);
            EventManager.AddListener("OnUpdateCompass", OnUpdateCompass);
            EventManager.AddListener("OnUpdateXRotation", OnUpdateSideBarAngles);
            EventManager.AddListener("OnDeath", OnDeath);
            //EventManager.AddListener("OnRespawn", OnRespawn);
            EventManager.AddListener("OnDistanceForward", OnUpdateDistance);
            EventManager.AddListener("OnMechaStateChange", OnMechaStateChange);
        }

        private void OnDisable()
        {
            //EventManager.RemoveListener("OnPause", OnPause);
            //EventManager.RemoveListener("OnResume", OnResume);
            EventManager.RemoveListener("OnUpdateHealth", OnUpdateHealth);
            EventManager.RemoveListener("OnUpdatePrimaryAmmoAmount", OnUpdatePrimaryAmmoAmount);
            EventManager.RemoveListener("OnZoomChange", OnZoomChange);
            EventManager.RemoveListener("OnUpdateSecondaryAmmoLeftAmount", OnUpdateSecondaryAmmoLeft);
            EventManager.RemoveListener("OnUpdateSecondaryAmmoRightAmount", OnUpdateSecondaryAmmoRight);
            EventManager.RemoveListener("OnUpdateCompass", OnUpdateCompass);
            EventManager.RemoveListener("OnUpdateXRotation", OnUpdateSideBarAngles);
            EventManager.RemoveListener("OnDeath", OnDeath);
            //EventManager.RemoveListener("OnRespawn", OnRespawn);
            EventManager.RemoveListener("OnDistanceForward", OnUpdateDistance);
            EventManager.RemoveListener("OnMechaStateChange", OnMechaStateChange);
        }

        

        #endregion
        private void OnMechaStateChange(object arg0)
        {
            _playedMechaState = (UnitState) arg0;
        }

        #region Reticle UI

        [Header("Reticle UI")] [SerializeField, Obsolete("Use _reticleCanvasGroups instead | migration ongoing")]
        private List<MaskableGraphic> reticleImages;
        private List<CanvasGroup> _reticleCanvasGroups = new List<CanvasGroup>();

        [SerializeField] private Transform iconParent;
        
        [SerializeField] private MaskableGraphic crosshair;

        float _previousAlpha;

        [Obsolete("HUDManager is not responsible for reticle fade anymore | migration ongoing")]
        private void FadeReticle(float alpha, float duration)
        {
            _previousAlpha = reticleImages[0].color.a;
            foreach (var image in reticleImages)
            {
                image.CrossFadeAlpha(alpha, duration, false);
            }

            StartCoroutine(FadeReticleCoroutine(alpha, duration));
        }
        
        private IEnumerator FadeReticleCoroutine(float alpha, float duration)
        {
            float remainingDuration = duration;
            while (remainingDuration > 0)
            {
                remainingDuration -= Time.deltaTime;
                foreach (var canvasGroup in _reticleCanvasGroups)
                {
                    canvasGroup.alpha = Mathf.Lerp(_previousAlpha, alpha, remainingDuration / duration);
                }

                yield return null;
            }
        }

        #endregion

        #region Ammo Update

        [Header("Ammo UI")] [SerializeField] private TextMeshProUGUI primaryAmmoText;

        private void OnUpdatePrimaryAmmoAmount(object amountRemaining)
        {
            int amount = (int)amountRemaining;
            primaryAmmoText.text = amountRemaining.ToString();

            //if (amount <= 2)
            //{
            //    primaryAmmoText.color = Color.red;
            //}
            //else if (amount <= 5)
            //{
            //    primaryAmmoText.color = Color.yellow;
            //}
            //else
            //{
            //    primaryAmmoText.color = Color.white;
            //}


        }



        #endregion

        #region Distance Looking UI
        
        [Header("Distance Looking UI")] [SerializeField] private TextMeshProUGUI distanceText;
        
        private void OnUpdateDistance(object distance)
        {
            if (distance is not float dist)
                return;
            if (float.IsNaN(dist))
                distanceText.text = "OOB";
            else
                distanceText.text = dist.ToString("0000", CultureInfo.InvariantCulture) + "m";
        }

        #endregion

        #region Health UI

        [Header("Health UI")] [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private Image juggImage;

        private void OnUpdateHealth(object healthPercentage)
        {
            var health = 1 - (float)healthPercentage;
            healthText.text = (int)(health * 100) + "%";
            
            if (health < 0.5f)
            {
                juggImage.color = Color.white;
            }
            else if (health < 0.75f)
            {
                juggImage.color = Color.yellow;
            }
            else
            {
                juggImage.color = Color.red;
            }
        }

        #endregion

        #region Zoom UI

        [Header("Zoom UI")] [SerializeField] private Image[] zoomImage;
        [SerializeField] private TextMeshProUGUI zoomText;
        [SerializeField] private Image zoomGrid;
        [SerializeField] private CanvasGroup zoomCanvasGroup;

        private void OnZoomChange(object zoomAmount)
        {
            if (GameManager.GameIsPaused || _playedMechaState != UnitState.Default)
                return;
            MechaController.Zoom zoom = (MechaController.Zoom)zoomAmount;
            switch (zoom)
            {
                case MechaController.Zoom.Default:
                    zoomText.text = "ZOOM   1.0 X";
                    FadeZoom(0, 0.1f);
                    //crosshair.CrossFadeAlpha(1, 0.1f, false);
                    //FadeReticle(1, 0.1f);
                    break;
                case MechaController.Zoom.X2:
                    FadeZoom(1, 0.2f);
                    //crosshair.CrossFadeAlpha(0.9f, 0.2f, false);
                    //FadeReticle(0.2f, 0.2f);
                    zoomGrid.pixelsPerUnitMultiplier = 1;
                    zoomText.text = "ZOOM   2.0 X";
                    break;
                case MechaController.Zoom.X4:
                    zoomGrid.pixelsPerUnitMultiplier = 0.5f;
                    zoomText.text = "ZOOM   4.0 X";
                    break;
                case MechaController.Zoom.X8:
                    zoomGrid.pixelsPerUnitMultiplier = 0.25f;
                    zoomText.text = "ZOOM   8.0 X";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void FadeZoom(float alpha, float duration)
        {
            foreach (var image in zoomImage)
            {
                image.CrossFadeAlpha(alpha, duration, false);
            }

            zoomText.CrossFadeAlpha(alpha, duration, false);
        }


        #endregion

        #region SecondaryAmmo UI

        [Header("Secondary Ammo UI")] [SerializeField]
        private TextMeshProUGUI secondaryAmmoLeftText;
        [SerializeField] private TextMeshProUGUI secondaryAmmoRightText;
        [SerializeField] private GameObject secondaryAmmoPanel;
        
        private void OnUpdateSecondaryAmmoLeft(object amountRemaining)
        {
            secondaryAmmoLeftText.text = amountRemaining.ToString();
        }
        
        private void OnUpdateSecondaryAmmoRight(object amountRemaining)
        {
            secondaryAmmoRightText.text = amountRemaining.ToString();
        }

        #endregion
        
        #region Compass UI
        
        [Header("Compass UI")]
        [SerializeField] private RawImage compassImage;
       
        private void OnUpdateCompass(object playerYRotation)
        {
            if (GameManager.GameIsPaused || _playedMechaState != UnitState.Default)
                return;
            compassImage.uvRect = new Rect((float)playerYRotation / 360, 0, 1, 1);
        }

        

        #endregion

        #region Side bar angles UI
        
        [Header("Side bar angles UI")] [SerializeField]
        private RawImage sideBarAnglesImageLeft;
        [SerializeField] private RawImage sideBarAnglesImageRight;
        [SerializeField] private TextMeshProUGUI topIndicatorText;
        
        private void OnUpdateSideBarAngles(object playerXRotation)
        {
            if (GameManager.GameIsPaused || _playedMechaState != UnitState.Default)
                return;
            float maxAngles = MechaController.MaxXRotation - MechaController.MinXRotation;
            sideBarAnglesImageLeft.uvRect = new Rect( 0,1 - (float)playerXRotation / maxAngles,  1, 1);
            sideBarAnglesImageRight.uvRect = new Rect(0, 1 -(float)playerXRotation / maxAngles,  1, 1);
            topIndicatorText.text = (-(float)playerXRotation).ToString("F1", new CultureInfo("en-US"));
        }
        

        #endregion

        #region OnPause/OnResume

        [Obsolete("Now handled by HUDWindow")]
        private void OnPause()
        {
            FadeReticle(0.1f, 0);
            crosshair.CrossFadeAlpha(0.1f, 0, true);
            Debug.Log("OnPause");
        }

        [Obsolete("Now handled by HUDWindow")]
        private void OnResume()
        {
            FadeReticle(_previousAlpha, 0);
            crosshair.CrossFadeAlpha(1f, 0, true);
        }

        #endregion

        #region Death and Respawn

        public void OnDeath(object data)
        {
            EventManager.TriggerEvent("OnZoomChange", MechaController.Zoom.Default);
        }
        
        [Obsolete("Now handled by HUDWindow")]
        public void OnRespawn(object data)
        {
            FadeReticle(1, 0.3f);
        }

        #endregion

    }


}