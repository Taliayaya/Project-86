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
        
        private void OnDisable()
        {
            EventManager.RemoveListener(Constants.Events.Inputs.Juggernaut.OnPhotoMode, OnPhotoMode);
        }
        
        private void OnPhotoMode()
        {
            Instantiate(freeCamera, transform.position, transform.rotation);
        }
        
    }
}