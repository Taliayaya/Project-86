using Gameplay.Units;
using UnityEngine;

namespace Utility
{
    public class UnitColliderEvent : ColliderEvent
    {
        public UnitType unitFlag;

        protected override void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent(out Unit unit) && unitFlag.HasFlag(unit.unitType))
                base.OnCollisionEnter(collision);
        }

        protected override void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.TryGetComponent(out Unit unit) && unitFlag.HasFlag(unit.unitType))
                base.OnCollisionExit(collision);
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out Unit unit) && unitFlag.HasFlag(unit.unitType))
                base.OnTriggerEnter(other);
        }

        protected override void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out Unit unit) && unit.unitType == unitFlag)
                base.OnTriggerExit(other);
        }
    }
}