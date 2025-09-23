using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
namespace BladesCombat
{
    public abstract class BladeComponent
    {
        protected BladeArmamentManager Manager;
        protected BladeSharedData SharedData;

        protected BladeSwitcher Switcher;
        protected BladeCollision Collision;

        public virtual bool UseUpdate => false;
        public virtual bool UseTriggers => false;

        public void Init(BladeArmamentManager manager, BladeSharedData sharedData, BladeComponent[] bladeComponents)
        {
            SharedData = sharedData;
            Manager = manager;
            PopulateComponents(bladeComponents);

            OnInited();
        }

        private void PopulateComponents(BladeComponent[] bladeComponents)
        {
            Switcher = bladeComponents.FirstOrDefault(b => b.GetType() == typeof(BladeSwitcher)) as BladeSwitcher;
            Assert.IsNotNull(Switcher);

            Collision = bladeComponents.FirstOrDefault(b => b.GetType() == typeof(BladeCollision)) as BladeCollision;
            Assert.IsNotNull(Collision);

        }

        protected virtual void OnInited()
        {
        }

        public void Enable()
        {
            OnEnabled();
        }

        protected virtual void OnEnabled()
        {
        }

        public virtual void Update()
        {
        }
        
        public void Disable()
        {
            OnDisabled();
        }

        protected virtual void OnDisabled()
        {
        }

        public virtual void OnTriggerEnter(Collider other, object additionalData = null)
        {
        }

        public virtual void OnTriggerStay(Collider other, object additionalData = null)
        {
        }
        
        public virtual void OnTriggerExit(Collider other, object additionalData = null)
        {
        }
    }
}