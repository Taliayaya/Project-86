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

        /// <summary>
        /// This function is really messy and I'm sorry for whoever have to fix it.
        /// Basically, it takes the marker and the arrow, and update the arrow position and rotation
        /// First we want to get the direction from the player to the marker.
        /// From this direction, we get the angle between the direction and the forward vector, this will let us decided where the arrow should be facing. (South, West...)
        /// Then, we want to place the arrow on the edge of the minimap, so we push it forward outbound and clamp if it goes out to the limit.
        /// This is enough for the non rotating camera. However, if the camera rotates, we want to make the direction rotate with it.
        /// So we rotate the direction, do the step where we push outbound and clamp, and then we rotate the arrow to face the direction again.
        /// (If we did it before, it would have been rotated twice, and the arrow would be facing the wrong direction)
        /// </summary>
        /// <param name="marker"></param>
        /// <param name="arrow"></param>
        /// <param name="containerRect"></param>
        private void UpdateMarker(MinimapMarker marker, Transform arrow, Rect containerRect)
        {
            var fromPosition = target.position;
            var toPosition = marker.transform.position;
            fromPosition.y = 0f;
            
            // we get the direction from the player to the marker
            Vector3 dir = (toPosition - fromPosition).normalized;
            
            // this is the angle of the minimap if rotate with player is true. It is the y axis for some reasons but on the inspector it is the Z axis.
            var offsetAngle = minimapCamera.transform.localEulerAngles.y;
            
            // this is the angle between the direction and the forward vector
            float angle = Vector3.SignedAngle(dir, Vector3.forward, Vector3.up);
            // we first set the arrow to face the direction
            arrow.localEulerAngles = new Vector3(0, 0, angle);
            // we rotate the direction with the offset angle (if not rotating with camera, offset will be 0 hence no rotation)
            var forward = Quaternion.AngleAxis( offsetAngle, Vector3.forward) * arrow.up;
            // because we no longer need arrow.up,
            // we can put the arrow back to face the direction + the offset angle if rotating with camera (if not, offset will be 0)
            arrow.localEulerAngles = new Vector3(0, 0, angle + offsetAngle);
            
            //we push it forward outbound
            var targetPosition = forward * containerRect.width;
            // we clamp the position to the limit of the minimap
            targetPosition.x =  Mathf.Clamp(targetPosition.x, containerRect.xMin + rectMin.x, containerRect.xMax + rectMax.x);
            targetPosition.y =  Mathf.Clamp(targetPosition.y, containerRect.yMin + rectMin.y, containerRect.yMax + rectMax.y) ;
            // we set the arrow position
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