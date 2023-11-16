using System;
using ScriptableObjects.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public class RegionPoint : MonoBehaviour
    {
        public RegionPointsSO regionPointsSO;

        private void SetAngle(Vector3 delta)
        {
            Debug.Log($"Angle before {delta} " + transform.eulerAngles + $" {delta + transform.eulerAngles}");
            transform.Rotate(delta);
            Debug.Log("Angle after" + transform.eulerAngles);
            foreach (Transform child in transform)
            {
                child.eulerAngles += delta;
            }
        }

        private void ApplyAngle()
        {
            switch (regionPointsSO.rotation)
            {
                case RegionPointsSO.RotationPoint.Down:
                    SetAngle(new Vector3(180, 0, 0));
                    break;
                case RegionPointsSO.RotationPoint.Up:
                    break;
                case RegionPointsSO.RotationPoint.ReverseDown:
                    SetAngle(new Vector3(180, 0, 180));
                    break;
                case RegionPointsSO.RotationPoint.ReverseUp:
                    SetAngle(new Vector3(0, 180, 0));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void SetText()
        {
            GetComponentInChildren<TMP_Text>().text = regionPointsSO.regionName;
        }

        public void Init(RegionPointsSO regionPoints)
        {
            regionPointsSO = regionPoints;
            //ApplyAngle();
            SetText();
            // TODO open a window with description and button to start the game
        }

        public void SetClick(Action<RegionPointsSO> onClick)
        {
            GetComponentInChildren<Button>().onClick.AddListener(() => onClick(regionPointsSO));
        }
    }
}