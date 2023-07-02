using TMPro;
using UnityEngine;

namespace UI.HUD
{
    public struct AmmoPopUpData
    {
        public string GunName;
        public float Duration;
        
        public AmmoPopUpData(string gunName, float duration)
        {
            GunName = gunName;
            Duration = duration;
        }
    }
    public class NoAmmoPopUp : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject noAmmoPopUp;
        [SerializeField] private TextMeshProUGUI noAmmoText;
        [SerializeField] private TextMeshProUGUI gunNameText;
        // Start is called before the first frame update

        private bool _isOn = false;

        private void OnEnable()
        {
            EventManager.AddListener("OnNoAmmo", OnNoAmmo);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("OnNoAmmo", OnNoAmmo);
        }

        public void TurnOnPopUp()
        {
            noAmmoPopUp.SetActive(true);
            _isOn = true;
        }
        
        public void TurnOffPopUp()
        {
            noAmmoPopUp.SetActive(false);
            _isOn = false;
        }
        
        public void SetAmmoInfo(string gunName)
        {
            gunNameText.text = gunName;
        }
        
        public void OnNoAmmo(object arg0)
        {
            if (_isOn)
                return;
            Debug.Log("No Ammo");
            var data = (AmmoPopUpData) arg0;
            SetAmmoInfo(data.GunName);
            TurnOnPopUp();
            Invoke(nameof(TurnOffPopUp), data.Duration);
        }
        
    }
}
