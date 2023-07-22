using System;
using ScriptableObjects.GameParameters;
using UnityEngine;

namespace UI.HUD
{
    public class Minimap : MonoBehaviour
    {
        public Transform target;
        public MinimapParameters minimapParameters;

        private void OnEnable()
        {
            EventManager.AddListener("RegisterMinimapTarget", ChangeTarget);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("RegisterMinimapTarget", ChangeTarget);
        }
        
        public void ChangeTarget(object newTarget)
        {
            target = (Transform)newTarget;
        }

        private void LateUpdate()
        {
            if (!target)
                return;
            
            if (minimapParameters.rotateWithPlayer)
                transform.rotation = Quaternion.Euler(90, target.eulerAngles.y, 0);
            
            if (minimapParameters.lockCameraToPlayer)
            {
                Vector3 newPosition = target.position;
                newPosition.y = transform.position.y;
                transform.position = newPosition;
            }
        }
    }
}