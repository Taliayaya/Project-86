using System;
using System.Collections.Generic;
using ScriptableObjects.GameParameters;
using UnityEngine;
using UnityEngine.UI;

namespace UI.HUD
{
    public class Minimap : MonoBehaviour
    {
        public Transform target;
        public MinimapParameters minimapParameters;
        public RawImage grid;
        public Camera minimapCamera;
        
        
        [Header("Markers")]
        [SerializeField] private GameObject markerPrefab;
        [SerializeField] private Transform markerContainer;
        [SerializeField] private RectTransform markerContainerRect;
        [SerializeField] private Vector2 rectMax;
        [SerializeField] private Vector2 rectMin;

        private List<(MinimapMarker marker, Transform transform)> _markers = new List<(MinimapMarker marker, Transform transform)>();

        private void OnEnable()
        {
            EventManager.AddListener("MinimapMarkerVisibilityChanged", OnMinimapMarkerVisibilityChanged);
            EventManager.AddListener("RegisterMinimapTarget", ChangeTarget);
        }

        private void OnDisable()
        {
            EventManager.RemoveListener("RegisterMinimapTarget", ChangeTarget);
            EventManager.RemoveListener("MinimapMarkerVisibilityChanged", OnMinimapMarkerVisibilityChanged);
        }

        private void OnMinimapMarkerVisibilityChanged(object arg0)
        {
            var marker = (MinimapMarker) arg0;
            if (!marker.IsVisible)
            {
                var markerNavigation = Instantiate(markerPrefab, markerContainer).GetComponent<Image>();
                markerNavigation.sprite = marker.icon;
                markerNavigation.name = marker.GetInstanceID().ToString();
                markerNavigation.color = marker.color;
                _markers.Add((marker, markerNavigation.transform));
            }
            else
            {
                var markerNavigation = _markers.Find(x => x.transform.name == marker.GetInstanceID().ToString());
                if (markerNavigation.transform)
                {
                    _markers.Remove(markerNavigation);
                    Destroy(markerNavigation.transform.gameObject);
                }
            }
            
        }

        public void ChangeTarget(object newTarget)
        {
            target = (Transform)newTarget;
        }

        /// <summary>
        /// Each marker is updated: where they face, their position
        /// </summary>
        private void UpdateMarkersPosition()
        {
            foreach ((MinimapMarker marker, Transform transform) tuple in _markers)
            {
                UpdateMarker(tuple.marker, tuple.transform, markerContainerRect.rect);
            }
        }

        private void UpdateMarker(MinimapMarker marker, Transform arrow, Rect containerRect)
        {
            var fromPosition = target.position;
            var toPosition = marker.transform.position;
            fromPosition.y = 0f;
            
            Vector3 dir = (toPosition - fromPosition).normalized;
            
            var offsetAngle = minimapCamera.transform.localEulerAngles.y;
            
            float angle = Vector3.SignedAngle(dir, Vector3.forward, Vector3.up);
            Debug.Log("offsetAngle: " + offsetAngle);
            arrow.localEulerAngles = new Vector3(0, 0, angle);
            var forward = Quaternion.AngleAxis( offsetAngle, Vector3.forward) * arrow.up;
            arrow.localEulerAngles = new Vector3(0, 0, angle + offsetAngle);
            var targetPosition = forward * containerRect.width;
            targetPosition.x =  Mathf.Clamp(targetPosition.x, containerRect.xMin + rectMin.x, containerRect.xMax + rectMax.x);
            targetPosition.y =  Mathf.Clamp(targetPosition.y, containerRect.yMin + rectMin.y, containerRect.yMax + rectMax.y) ;
            arrow.localPosition= targetPosition;
            
            

        }

        private void LateUpdate()
        {
            if (!target)
                return;
            UpdateMarkersPosition();

            if (minimapParameters.rotateWithPlayer)
            {
                transform.rotation = Quaternion.Euler(90, target.eulerAngles.y, 0);
                if (grid)
                    grid.transform.rotation = Quaternion.Euler(0, 0, target.eulerAngles.y);
            }

            if (minimapParameters.lockCameraToPlayer)
            {
                Vector3 newPosition = target.position;
                newPosition.y = transform.position.y;
                transform.position = newPosition;
                if (grid)
                {
                    var x = newPosition.x / minimapCamera.pixelWidth;
                    var y = newPosition.z / minimapCamera.pixelHeight;
                    grid.uvRect = new Rect(x, y, grid.uvRect.width, grid.uvRect.height);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!target)
                return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position, 3f);
        }
    }
}