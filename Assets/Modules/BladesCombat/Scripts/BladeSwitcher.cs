using System;
using Unity.Netcode.Components;
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

        public bool IsEmitting;
        public bool IsListening;

        public event Action OnBothBladesToggled;
        
        protected override void OnEnabled()
        {
            if (!IsListening)
            {
                _enabledLeftBlade = true;
                _enabledRightBlade = true;
                return;
            }

            if (Manager.HasSeparateBladeControls)
            {
                EventManager.AddListener("OnToggleLeftBlade", OnLeftToggle);
                EventManager.AddListener("OnToggleRightBlade", OnRightToggle);
            }
            else
            {
                EventManager.AddListener("OnSecondaryFire", OnBothToggle);
                if (IsEmitting)
                    EventManager.TriggerEvent(Constants.Events.BladeClosed);
            }
        }

        protected override void OnDisabled()
        {
            if (!IsListening)
            {
                _enabledLeftBlade = false;
                _enabledRightBlade = false;
                return;
            }
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

        private void OpenBlade(NetworkAnimator animator)
        {
            if (IsEmitting)
                EventManager.TriggerEvent(Constants.Events.BladeOpened);
            animator.SetTrigger("Open");
        }

        private void CloseBlade(NetworkAnimator animator)
        {
            if (IsEmitting)
                EventManager.TriggerEvent(Constants.Events.BladeClosed);
            Debug.Log("Close blade");
            animator.SetTrigger("Close");
        }
    }
}