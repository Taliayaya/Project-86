using System;
using UnityEngine;

namespace Gameplay
{
    public class FreeCameraInvoker : MonoBehaviour
    {
        [SerializeField] private GameObject freeCamera;
        private void OnEnable()
        {
            EventManager.AddListener(Constants.Events.Inputs.Juggernaut.OnPhotoMode, OnPhotoMode);
        }

        private void Awake()
        {
            if (!freeCamera)
            {
                freeCamera = Resources.Load<GameObject>("Prefabs/PhotoMode Camera");
            }
        }

        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.Events.Inputs.Juggernaut.OnPhotoMode, OnPhotoMode);
        }
        
        private void OnPhotoMode()
        {
            var go = Instantiate(freeCamera, transform.position, transform.rotation);
            go.SetActive(true);
        }
        
    }
}