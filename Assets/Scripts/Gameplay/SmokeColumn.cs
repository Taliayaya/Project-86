using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Distant smoke plume behaviour. Grows from startScale to full size over
    /// riseDuration after being enabled (the monument-destroyed swap), then keeps
    /// slowly churning by rotating around its base.
    /// ponytail: one static combined mesh + transform animation, no particle system —
    /// at vista distances individual puffs are invisible and particles would be culled.
    /// </summary>
    public class SmokeColumn : MonoBehaviour
    {
        [SerializeField] private float riseDuration = 45f;
        [SerializeField] private float startScale = 0.25f;
        [SerializeField] private float idleSpinDegPerSec = 1.5f;

        private float _t;

        private void OnEnable()
        {
            _t = 0f;
            if (Application.isPlaying)
                transform.localScale = Vector3.one * startScale;
        }

        private void Update()
        {
            if (!Application.isPlaying)
                return;
            _t += Time.deltaTime;
            float k = Mathf.SmoothStep(startScale, 1f, Mathf.Clamp01(_t / riseDuration));
            transform.localScale = new Vector3(k, k, k);
            transform.Rotate(0f, idleSpinDegPerSec * Time.deltaTime, 0f);
        }
    }
}
