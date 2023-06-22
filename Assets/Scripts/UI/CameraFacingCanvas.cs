using UnityEngine;

namespace UI
{
    public class CameraFacingCanvas : MonoBehaviour
    {
        private Transform _mainCamera;

        private void Awake()
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").transform;
        }

        private void Update()
        {
            transform.LookAt(_mainCamera);
        }
        
    }
}