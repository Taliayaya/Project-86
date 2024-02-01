using UnityEngine;

namespace Gameplay
{
    public class LegSynchroniseToEnd : MonoBehaviour
    {
        public Transform[] legTargets;
        public Transform[] legEnds;
        
        public void Synchronise()
        {
            for (int i = 0; i < legTargets.Length; i++)
            {
                Transform parent = legTargets[i].parent;
                legTargets[i].parent = legEnds[i];
                legTargets[i].localPosition = Vector3.zero;
                legTargets[i].parent = parent;
            }
        }
    }
}