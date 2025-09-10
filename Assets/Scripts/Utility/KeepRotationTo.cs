using Gameplay.Units;
using UnityEngine;

namespace Utility
{
    public class KeepRotationTo : MonoBehaviour
    {
        public Vector3 rotation;
        public Transform targetRotation;

        private void Update()
        {
            transform.rotation = targetRotation ? targetRotation.rotation : Quaternion.Euler(rotation);
        }
    }
}
