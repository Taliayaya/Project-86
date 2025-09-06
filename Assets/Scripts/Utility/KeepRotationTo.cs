using Gameplay.Units;
using UnityEngine;

namespace Utility
{
    public class KeepRotationTo : MonoBehaviour
    {
        public Vector3 rotation;

        private void Update()
        {
            transform.rotation = Quaternion.Euler(rotation);
        }
    }
}
