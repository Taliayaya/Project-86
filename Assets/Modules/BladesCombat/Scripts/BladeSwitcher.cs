using System;
using UnityEngine;
namespace BladesCombat
{
    public class BladeSwitcher : BladeComponent
    {
        private bool _enabledLeftBlade = false;
        private bool _enabledRightBlade = false;
        public bool IsLeftActive => _enabledLeftBlade;
        public bool IsRightActive => _enabledRightBlade;

        private bool _isHeld = false;

        public event Action OnBothBladesToggled;
        
        protected override void OnEnabled()
        {
            if (Manager.HasSeparateBladeControls)
            {
                EventManager.AddListener("OnToggleLeftBlade", OnLeftToggle);
                EventManager.AddListener("OnToggleRightBlade", OnRightToggle);
            }
            else
            {
                EventManager.AddListener("OnSecondaryFire", OnBothToggle);
            }
        }

        protected override void OnDisabled()
        {
            if (Manager.HasSeparateBladeControls)
            {
                EventManager.RemoveListener("OnToggleLeftBlade", OnLeftToggle);
                EventManager.RemoveListener("OnToggleRightBlade", OnRightToggle);
            }
            else
            {
                EventManager.RemoveListener("OnSecondaryFire", OnBothToggle);
            }
        }

        private void OnBothToggle()
        {
            _isHeld = !_isHeld;
            if (!_isHeld) return;
            _enabledLeftBlade = !_enabledLeftBlade;
            _enabledRightBlade = !_enabledRightBlade;
            
            SharedData.LeftTrigger.IsActive = _enabledLeftBlade;
            SharedData.RightTrigger.IsActive = _enabledRightBlade;
            if (_enabledLeftBlade)
            {
                OpenBlade(SharedData.BothBlade);
            }
            else
            {
                CloseBlade(SharedData.BothBlade);
            }
            OnBothBladesToggled?.Invoke();
        }

        private void OnLeftToggle()
        {
            _enabledLeftBlade = !_enabledLeftBlade;
            SharedData.LeftTrigger.IsActive = _enabledLeftBlade;
            if (_enabledLeftBlade)
            {
                OpenBlade(SharedData.LeftBlade);
            }
            else
            {
                CloseBlade(SharedData.LeftBlade);
            }
        }

        private void OnRightToggle()
        {
            _enabledRightBlade = !_enabledRightBlade;
            SharedData.RightTrigger.IsActive = _enabledRightBlade;
            if (_enabledRightBlade)
            {
                OpenBlade(SharedData.RightBlade);
            }
            else
            {
                CloseBlade(SharedData.RightBlade);
            }
        }

        private void OpenBlade(Animator animator)
        {
            animator.SetTrigger("Open");
        }

        private void CloseBlade(Animator animator)
        {
            animator.SetTrigger("Close");
        }
    }
}