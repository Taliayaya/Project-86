using UnityEngine;

namespace Utility
{
    public class DeactivatesWith : MonoBehaviour
    {
        [SerializeField] private GameObject[] linkedObjects;

        private void OnDisable()
        {
            foreach (var obj in linkedObjects)
                if (obj) obj.SetActive(false);
        }

        private void OnEnable()
        {
            foreach (var obj in linkedObjects)
                if (obj) obj.SetActive(true);
        }
    }
}
