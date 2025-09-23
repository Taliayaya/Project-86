using System;
using System.Collections.Generic;
using System.Linq;
using BladesCombat.Utils;
using BladesCombatTutorial;
using UnityEngine;
namespace BladesCombat
{
    public class BladeArmamentManager : MonoBehaviour
    {
        [SerializeField] private BladeSharedData SharedData;
        [SerializeField] private bool HasSeparateControls;

        private BladeSwitcher _bladeSwitcher = new BladeSwitcher();
        private BladeCollision _bladeCollision = new BladeCollision();

        private BladeComponent[] _bladeComponents;

        private OptionalFeature<BladeComponent> _updateFeature;
        private OptionalFeature<BladeComponent> _triggerFeature;

        private bool _initedComponents = false;
        public bool HasSeparateBladeControls => HasSeparateControls;
        public event Action OnBladeModeChanged;

        public event Action<Collider, TriggerData> OnBladeTriggerEntered;
        public event Action<Collider, TriggerData> OnBladeTriggerExited;
        
        private void OnEnable()
        {
            InitComponents();
            EnableComponents();
        }

        private void OnDisable()
        {
            DisableComponents();
        }

        private void InitComponents()
        {
            if (_initedComponents)
            {
                return;
            }

            _bladeComponents = new BladeComponent[]
            {
                _bladeSwitcher,
                _bladeCollision
            };

            foreach (BladeComponent component in _bladeComponents)
            {
                component.Init(this, SharedData, _bladeComponents);
            }

            PrepareUpdatableComponents();
            PrepareTriggerComponents();
            SubscribeToBladeTriggers();
        }

        private void PrepareUpdatableComponents()
        {
            List<BladeComponent> updatableComponents = null;

            var updatables = _bladeComponents.Where(b => b.UseUpdate);

            if (updatables.Any())
            {
                updatableComponents = updatables.ToList();
            }

            _updateFeature = new OptionalFeature<BladeComponent>(updatableComponents, component => component.Update());
        }

        private void PrepareTriggerComponents()
        {
            List<BladeComponent> triggerComponents = null;

            var triggers = _bladeComponents.Where(b => b.UseTriggers);

            if (triggers.Any())
            {
                triggerComponents = triggers.ToList();
            }

            _triggerFeature = new OptionalFeature<BladeComponent>(triggerComponents);
        }

        private void SubscribeToBladeTriggers()
        {
            _bladeSwitcher.OnBothBladesToggled += BladeModeChanged;
            
            Subscribe(SharedData.LeftTrigger);
            Subscribe(SharedData.RightTrigger);
            return;

            void Subscribe(BladeCollisionTrigger trigger)
            {

                trigger.OnBladeTriggerEnter.AddListener(OnBladeTriggerEnter);
                trigger.OnBladeTriggerStay.AddListener(OnBladeTriggerStay);
                trigger.OnBladeTriggerExit.AddListener(OnBladeTriggerExit);
            }
        }

        private void EnableComponents()
        {
            foreach (BladeComponent component in _bladeComponents)
            {
                component.Enable();
            }
        }

        private void DisableComponents()
        {
            foreach (BladeComponent component in _bladeComponents)
            {
                component.Disable();
            }
        }

        private void Update()
        {


            _updateFeature.Invoke();
        }

        private void OnBladeTriggerEnter(Collider other, bool isLeft)
        {
            TriggerData data = new TriggerData() { IsLeftBlade = isLeft };
            _triggerFeature.Invoke(c => c.OnTriggerEnter(other, data));
            OnBladeTriggerEntered?.Invoke(other, data);
        }

        private void OnBladeTriggerStay(Collider other, bool isLeft)
        {
            TriggerData data = new TriggerData() { IsLeftBlade = isLeft };
            _triggerFeature.Invoke(c => c.OnTriggerStay(other, data));
        }

        private void OnBladeTriggerExit(Collider other, bool isLeft)
        {
            TriggerData data = new TriggerData() { IsLeftBlade = isLeft };
            _triggerFeature.Invoke(c => c.OnTriggerExit(other, data));
            OnBladeTriggerExited?.Invoke(other, data);
        }

        private void BladeModeChanged()
        {
            OnBladeModeChanged?.Invoke();
        }

        public T GetBladeComponent<T>() where T : BladeComponent
        {
            IEnumerable<BladeComponent> components = _bladeComponents.Where(b => b is T);
            if (components.Any())
            {
                return components.First() as T;
            }
            
            return null;
        }
        
        public BladeMode CurrentMode => _bladeSwitcher.IsLeftActive || _bladeSwitcher.IsRightActive ? BladeMode.Side : BladeMode.Forward;
    }
}