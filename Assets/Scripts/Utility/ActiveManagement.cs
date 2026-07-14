using UnityEngine;

namespace Utility
{
    public class ActiveManagement : MonoBehaviour
    {
       [SerializeField] private Collider component;
       public void DisableAfterSeconds(float seconds) => Invoke(nameof(Disable), seconds);
       public void Disable() => component.enabled = false;
       public void EnableThenDisableAfterSeconds(float seconds)
       {
           component.enabled = true;
           DisableAfterSeconds(seconds);
       }
    }
}