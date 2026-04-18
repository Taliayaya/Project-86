using System.Collections.Generic;
using UnityEngine;

namespace FMODUnity
{
    [AddComponentMenu("FMOD Studio/FMOD Studio Listener")]
    public class StudioListener : MonoBehaviour
    {
        [SerializeField]
        private bool nonRigidbodyVelocity = false;

        [SerializeField]
        private GameObject attenuationObject = null;

        private Vector3 lastFramePosition = Vector3.zero;

#if UNITY_PHYSICS_EXIST
        private Rigidbody rigidBody;
#endif
#if UNITY_PHYSICS2D_EXIST
        private Rigidbody2D rigidBody2D;
#endif
        private static List<StudioListener> listeners = new List<StudioListener>();

        public static int ListenerCount
        {
            get
            {
                return listeners.Count;
            }
        }

        public int ListenerNumber
        {
            get
            {
                return listeners.IndexOf(this);
            }
        }

        public static float DistanceToNearestListener(Vector3 position)
        {
            float result = float.MaxValue;
            for (int i = 0; i < listeners.Count; i++)
            {
                if (listeners[i].attenuationObject == null)
                {
                    result = Mathf.Min(result, Vector3.Distance(position, listeners[i].transform.position));
                }
                else
                {
                    result = Mathf.Min(result, Vector3.Distance(position, listeners[i].attenuationObject.transform.position));
                }
            }
            return result;
        }

        public static float DistanceSquaredToNearestListener(Vector3 position)
        {
            float result = float.MaxValue;
            for (int i = 0; i < listeners.Count; i++)
            {
                if (listeners[i].attenuationObject == null)
                {
                    result = Mathf.Min(result, (position - listeners[i].transform.position).sqrMagnitude);
                }
                else
                {
                    result = Mathf.Min(result, (position - listeners[i].attenuationObject.transform.position).sqrMagnitude);
                }
            }
            return result;
        }

        private static void AddListener(StudioListener listener)
        {
            // Is the listener already in the list?
            if (listeners.Contains(listener))
            {
                Debug.LogWarning(string.Format(("[FMOD] Listener has already been added at index {0}."), listener.ListenerNumber));
                return;
            }

            // If already at the max numListeners
            if (listeners.Count >= FMOD.CONSTANTS.MAX_LISTENERS)
            {
                Debug.LogWarning(string.Format(("[FMOD] Max number of listeners reached : {0}."), FMOD.CONSTANTS.MAX_LISTENERS));
            }

            listeners.Add(listener);
            RuntimeManager.StudioSystem.setNumListeners(Mathf.Clamp(listeners.Count, 1, FMOD.CONSTANTS.MAX_LISTENERS));
        }

        private static void RemoveListener(StudioListener listener)
        {
            listeners.Remove(listener);
            RuntimeManager.StudioSystem.setNumListeners(Mathf.Clamp(listeners.Count, 1, FMOD.CONSTANTS.MAX_LISTENERS));
        }

        private void OnEnable()
        {
            RuntimeUtils.EnforceLibraryOrder();
#if UNITY_PHYSICS_EXIST
            rigidBody = gameObject.GetComponent<Rigidbody>();

            if (nonRigidbodyVelocity && rigidBody)
            {
                Debug.LogWarning(string.Format("[FMOD] Non-Rigidbody Velocity is enabled on Listener attached to GameObject \"{0}\", which also has a Rigidbody component attached - this will be disabled in favor of velocity from Rigidbody component.", this.name));
                nonRigidbodyVelocity = false;
            }
#endif
#if UNITY_PHYSICS2D_EXIST
            rigidBody2D = gameObject.GetComponent<Rigidbody2D>();

            if (nonRigidbodyVelocity && rigidBody2D)
            {
                Debug.LogWarning(string.Format("[FMOD] Non-Rigidbody Velocity is enabled on Listener attached to GameObject \"{0}\", which also has a Rigidbody2D component attached - this will be disabled in favor of velocity from Rigidbody2D component.", this.name));
                nonRigidbodyVelocity = false;
            }
#endif
            AddListener(this);

            lastFramePosition = transform.position;
        }

        private void OnDisable()
        {
            RemoveListener(this);
        }

        private void Update()
        {
            if (ListenerNumber < 0 || ListenerNumber >= FMOD.CONSTANTS.MAX_LISTENERS)
            {
                return;
            }

            if (nonRigidbodyVelocity)
            {
                var velocity = Vector3.zero;
                var position = transform.position;

                if (Time.deltaTime != 0)
                {
                    velocity = (position - lastFramePosition) / Time.deltaTime;
                    velocity = Vector3.ClampMagnitude(velocity, 20.0f);
                }

                lastFramePosition = position;

                RuntimeManager.SetListenerLocation(ListenerNumber, gameObject, attenuationObject, velocity);
            }
            else
            {
#if UNITY_PHYSICS_EXIST
                if (rigidBody)
                {
                    RuntimeManager.SetListenerLocation(ListenerNumber, gameObject, rigidBody, attenuationObject);
                }
                else
#endif
#if UNITY_PHYSICS2D_EXIST
                if (rigidBody2D)
                {
                    RuntimeManager.SetListenerLocation(ListenerNumber, gameObject, rigidBody2D, attenuationObject);
                }
                else
#endif
                {
                    RuntimeManager.SetListenerLocation(ListenerNumber, gameObject, attenuationObject);
                }
            }
        }
    }
}
