using System;
using System.Globalization;
using Gameplay.Mecha;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HUDManager : MonoBehaviour
    {
        #region Unity Callbacks

        private void Awake()
        {
            FadeZoom(0, 0);
        }

        private void OnEnable()
        {
            EventManager.AddListener("OnPause", OnPause);
            EventManager.AddListener("OnResume", OnResume);
            EventManager.AddListener("OnUpdateHealth", OnUpdateHealth);
            EventManager.AddListener("OnUpdatePrimaryAmmoAmount", OnUpdatePrimaryAmmoAmount);
            EventManager.AddListener("OnZoomChange", OnZoomChange);
            EventManager.AddListener("OnUpdateSecondaryAmmoLeftAmount", OnUpdateSecondaryAmmoLeft);
            EventManager.AddListener("OnUpdateSecondaryAmmoRightAmount", OnUpdateSecondaryAmmoRight);
            EventManager.AddListener("OnUpdateCompass", OnUpdateCompass);
            EventManager.AddListener("OnUpdateXRotation", OnUpdateSideBarAngles);
            EventManager.AddListener("OnDeath", OnDeath);
            EventManager.AddListener("OnRespawn", OnRespawn);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("OnPause", OnPause);
            EventManager.RemoveListener("OnResume", OnResume);
            EventManager.RemoveListener("OnUpdateHealth", OnUpdateHealth);
            EventManager.RemoveListener("OnUpdatePrimaryAmmoAmount", OnUpdatePrimaryAmmoAmount);
            EventManager.RemoveListener("OnZoomChange", OnZoomChange);
            EventManager.RemoveListener("OnUpdateSecondaryAmmoLeftAmount", OnUpdateSecondaryAmmoLeft);
            EventManager.RemoveListener("OnUpdateSecondaryAmmoRightAmount", OnUpdateSecondaryAmmoRight);
            EventManager.RemoveListener("OnUpdateCompass", OnUpdateCompass);
            EventManager.RemoveListener("OnUpdateXRotation", OnUpdateSideBarAngles);
            EventManager.RemoveListener("OnDeath", OnDeath);
            EventManager.RemoveListener("OnRespawn", OnRespawn);
        }

        #endregion

        #region Reticle UI

        [Header("Reticle UI")] [SerializeField]
        private MaskableGraphic[] reticleImages;
        
        [SerializeField] private MaskableGraphic crosshair;


        private void FadeReticle(float alpha, float duration)
        {
            _previousAlpha = reticleImages[0].color.a;
            foreach (var image in reticleImages)
            {
                image.CrossFadeAlpha(alpha, duration, false);
            }
        }

        #endregion

        #region Ammo Update

        [Header("Ammo UI")] [SerializeField] private TextMeshProUGUI primaryAmmoText;

        private void OnUpdatePrimaryAmmoAmount(object amountRemaining)
        {
            int amount = (int)amountRemaining;
            primaryAmmoText.text = amountRemaining.ToString();

            if (amount <= 2)
            {
                primaryAmmoText.color = Color.red;
            }
            else if (amount <= 5)
            {
                primaryAmmoText.color = Color.yellow;
            }
            else
            {
                primaryAmmoText.color = Color.white;
            }


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

        private void OnZoomChange(object zoomAmount)
        {
            MechaController.Zoom zoom = (MechaController.Zoom)zoomAmount;
            switch (zoom)
            {
                case MechaController.Zoom.Default:
                    zoomText.text = "ZOOM   1.0 X";
                    FadeZoom(0, 0.1f);
                    crosshair.CrossFadeAlpha(1, 0.1f, false);
                    FadeReticle(1, 0.1f);
                    break;
                case MechaController.Zoom.X2:
                    FadeZoom(1, 0.2f);
                    crosshair.CrossFadeAlpha(0.9f, 0.2f, false);
                    FadeReticle(0.2f, 0.2f);
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
            float maxAngles = MechaController.MaxXRotation - MechaController.MinXRotation;
            sideBarAnglesImageLeft.uvRect = new Rect( 0,1 - (float)playerXRotation / maxAngles,  1, 1);
            sideBarAnglesImageRight.uvRect = new Rect(0, 1 -(float)playerXRotation / maxAngles,  1, 1);
            topIndicatorText.text = (-(float)playerXRotation).ToString("F1", new CultureInfo("en-US"));
        }
        

        #endregion

        #region OnPause/OnResume

        private float _previousAlpha;
        private void OnPause()
        {
            FadeReticle(0.1f, 0);
            crosshair.CrossFadeAlpha(0.1f, 0, true);
            Debug.Log("OnPause");
        }

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
            FadeReticle(0, 0.3f);
        }
        
        public void OnRespawn(object data)
        {
            FadeReticle(1, 0.3f);
        }

        #endregion
    }


}