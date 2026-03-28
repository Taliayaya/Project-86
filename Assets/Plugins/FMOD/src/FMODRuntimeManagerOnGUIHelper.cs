using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FMODUnity
{
    public class FMODRuntimeManagerOnGUIHelper : MonoBehaviour
    {
        public RuntimeManager TargetRuntimeManager = null;

        private void OnGUI()
        {
            if (TargetRuntimeManager)
            {
                TargetRuntimeManager.ExecuteOnGUI();
            }
        }
    }
}
