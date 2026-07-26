using System.Collections;
using FMODUnity;
using SoundManagement.Utils;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    /// <summary>
    /// Distant-destruction presentation. Put this on the "destroyed" variant of a
    /// monument: when Monument.onDestroyed activates that variant, this plays a
    /// horizon flash immediately, then the rumble + camera shake arrive delayed by
    /// distance, like a real far-off blast (light first, sound later).
    /// </summary>
    public class MonumentDestructionFX : MonoBehaviour
    {
        [SerializeField] private EventReference rumbleSound;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("Flash")]
        [SerializeField] private Color flashColor = new Color(1f, 0.96f, 0.88f);
        [SerializeField] private float flashAlpha = 0.85f;
        [SerializeField] private float flashDecay = 2.5f;

        [Header("Timing")]
        [Tooltip("Real speed of sound would be distance/343 s; capped for game feel.")]
        [SerializeField] private float maxSoundDelay = 3f;
        [SerializeField] private float shakeForce = 1f;

        private void OnEnable()
        {
            if (Application.isPlaying)
                StartCoroutine(Play());
        }

        private IEnumerator Play()
        {
            var cam = Camera.main;
            float dist = cam ? Vector3.Distance(cam.transform.position, transform.position) : 1000f;
            float delay = Mathf.Min(dist / 343f, maxSoundDelay);

            // full-screen flash overlay, no scene wiring needed
            var flash = new GameObject("MonumentFlash", typeof(Canvas), typeof(Image));
            var canvas = flash.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            var image = flash.GetComponent<Image>();
            image.raycastTarget = false;

            bool boomed = false;
            float t = 0f;
            float total = Mathf.Max(flashDecay, delay + 0.1f);
            while (t < total)
            {
                t += Time.deltaTime;
                if (!boomed && t >= delay)
                {
                    boomed = true;
                    if (!rumbleSound.IsNull)
                        rumbleSound.PlayOneShot(cam ? cam.transform.position : Vector3.zero);
                    if (impulseSource)
                        impulseSource.GenerateImpulse(shakeForce);
                }
                var c = flashColor;
                c.a = flashAlpha * Mathf.Clamp01(1f - t / flashDecay);
                image.color = c;
                yield return null;
            }
            Destroy(flash);
        }
    }
}
