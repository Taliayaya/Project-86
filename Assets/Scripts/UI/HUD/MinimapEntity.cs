using System;
using UnityEngine;

namespace UI.HUD
{
    public class MinimapEntity : MonoBehaviour
    {
        private void Update()
        {
            transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }
}