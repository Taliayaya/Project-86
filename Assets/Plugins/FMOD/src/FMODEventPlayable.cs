#if UNITY_TIMELINE_EXIST

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.Serialization;

namespace FMODUnity
{
    [System.Serializable]
    public class FMODEventPlayable : PlayableAsset, ITimelineClipAsset
    {
        [FormerlySerializedAs("template")]
        public FMODEventPlayableBehavior Template = new FMODEventPlayableBehavior();

        [FormerlySerializedAs("eventLength")]
        public float EventLength; //In seconds.

        [Obsolete("Use the eventReference field instead")]
        [SerializeField]
        public string eventName;

        [FormerlySerializedAs("eventReference")]
        [SerializeField]
        public EventReference EventReference;

        [FormerlySerializedAs("stopType")]
        [SerializeField]
        public STOP_MODE StopType;

        [FormerlySerializedAs("parameters")]
        [SerializeField]
        public ParamRef[] Parameters = new ParamRef[0];

        [NonSerialized]
        public bool CachedParameters = false;

        public static event System.EventHandler<EventArgs> OnCreatePlayable;

        private FMODEventPlayableBehavior behavior;

        public GameObject TrackTargetObject { get; set; }

        public override double duration
        {
            get
            {
                if (EventReference.IsNull)
                {
                    return base.duration;
                }
                else
                {
                    return EventLength;
                }
            }
        }

        public ClipCaps clipCaps
        {
            get { return ClipCaps.None; }
        }

        public TimelineClip OwningClip { get; set; }

        public void LinkParameters(FMOD.Studio.EventDescription eventDescription)
        {
#if UNITY_EDITOR
            if (!EventReference.IsNull)
#else
            if (!CachedParameters && !EventReference.IsNull)
#endif
            {
                for (int i = 0; i < Parameters.Length; i++)
                {
                    FMOD.Studio.PARAMETER_DESCRIPTION parameterDescription;
                    eventDescription.getParameterDescriptionByName(Parameters[i].Name, out parameterDescription);
                    Parameters[i].ID = parameterDescription.id;
                }

                List<ParameterAutomationLink> parameterLinks = Template.ParameterLinks;

                for (int i = 0; i < parameterLinks.Count; i++)
                {
                    FMOD.Studio.PARAMETER_DESCRIPTION parameterDescription;
                    eventDescription.getParameterDescriptionByName(parameterLinks[i].Name, out parameterDescription);
                    parameterLinks[i].ID = parameterDescription.id;
                }

                CachedParameters = true;
            }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            if (Application.isPlaying)
            {
                LinkParameters(RuntimeManager.GetEventDescription(EventReference));
            }
            else
            {
                // Handled by the editor auditioning system.
                EventArgs args = new EventArgs();
                OnCreatePlayable.Invoke(this, args);
            }

            var playable = ScriptPlayable<FMODEventPlayableBehavior>.Create(graph, Template);
            behavior = playable.GetBehaviour();

            behavior.TrackTargetObject = TrackTargetObject;
            behavior.EventReference = EventReference;
            behavior.StopType = StopType;
            behavior.Parameters = Parameters;
            behavior.OwningClip = OwningClip;

            return playable;
        }

#if UNITY_EDITOR
        public void UpdateEventDuration(float duration)
        {
            EventLength = duration / 1000f;
        }

        public void OnValidate()
        {
            if (OwningClip != null)
            {
                if (EventReference.IsNull)
                {
                    OwningClip.displayName = "FMODEventPlayable";
                }
                else
                {
                    int index = EventReference.Path.LastIndexOf("/");
                    OwningClip.displayName = EventReference.Path.Substring(index + 1);
                }
            }
            if (behavior != null)
            {
                behavior.EventReference = EventReference;
            }
        }
#endif //UNITY_EDITOR
    }

    public enum STOP_MODE : int
    {
        AllowFadeout,
        Immediate,
        None
    }

    [Serializable]
    public class ParameterAutomationLink
    {
        public string Name;
        public FMOD.Studio.PARAMETER_ID ID;
        public int Slot;
    }

    [Serializable]
    public class FMODEventPlayableBehavior : PlayableBehaviour
    {
        public FMODEventPlayableBehavior()
        {
            CurrentVolume = 1;
        }

        public class EventArgs : System.EventArgs
        {
            public FMOD.Studio.EventInstance eventInstance { get; set; }
        }

        public static event System.EventHandler<EventArgs> Enter;
        public static event System.EventHandler<EventArgs> Exit;
        public static event System.EventHandler<EventArgs> GraphStop;

        [FormerlySerializedAs("eventReference")]
        public EventReference EventReference;

        [FormerlySerializedAs("stopType")]
        public STOP_MODE StopType = STOP_MODE.AllowFadeout;

        [FormerlySerializedAs("parameters")]
        [NotKeyable]
        public ParamRef[] Parameters = new ParamRef[0];

        [FormerlySerializedAs("parameterLinks")]
        public List<ParameterAutomationLink> ParameterLinks = new List<ParameterAutomationLink>();

        [NonSerialized]
        public GameObject TrackTargetObject;

        [NonSerialized]
        public TimelineClip OwningClip;

        [FormerlySerializedAs("parameterAutomation")]
        public AutomatableSlots ParameterAutomation;

        private bool isPlayheadInside = false;

        private FMOD.Studio.EventInstance eventInstance;

        public float ClipStartTime { get; private set; } = 0.0f;

        public float CurrentVolume { get; private set; }

        protected void PlayEvent()
        {
            if (!EventReference.IsNull)
            {
                eventInstance = RuntimeManager.CreateInstance(EventReference);

                // Only attach to object if the game is actually playing, not auditioning.
                if (Application.isPlaying && TrackTargetObject)
                {
#if UNITY_PHYSICS_EXIST
                    if (TrackTargetObject.GetComponent<Rigidbody>())
                    {
                        RuntimeManager.AttachInstanceToGameObject(eventInstance, TrackTargetObject, TrackTargetObject.GetComponent<Rigidbody>());
                    }
                    else
#endif
#if UNITY_PHYSICS2D_EXIST
                    if (TrackTargetObject.GetComponent<Rigidbody2D>())
                    {
                        RuntimeManager.AttachInstanceToGameObject(eventInstance, TrackTargetObject, TrackTargetObject.GetComponent<Rigidbody2D>());
                    }
                    else
#endif
                    {
                        RuntimeManager.AttachInstanceToGameObject(eventInstance, TrackTargetObject);
                    }
                }
                else
                {
                    eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(Vector3.zero));
                }

                foreach (var param in Parameters)
                {
                    eventInstance.setParameterByID(param.ID, param.Value);
                }

                eventInstance.setVolume(CurrentVolume);
                eventInstance.setTimelinePosition((int)(ClipStartTime * 1000.0f));
                eventInstance.start();
            }
        }

        protected virtual void OnEnter()
        {
            if (!isPlayheadInside)
            {
                isPlayheadInside = true;

                if (Application.isPlaying)
                {
                    PlayEvent();
                }
                else
                {
                    // Handled by the editor auditioning system.
                    EventArgs args = new EventArgs();
                    Enter.Invoke(this, args);
                    eventInstance = args.eventInstance;
                }
            }
        }

        protected virtual void OnExit()
        {
            if (isPlayheadInside)
            {
                isPlayheadInside = false;

                if (Application.isPlaying)
                {
                    if (eventInstance.isValid())
                    {
                        if (StopType != STOP_MODE.None)
                        {
                            eventInstance.stop(StopType == STOP_MODE.Immediate ? FMOD.Studio.STOP_MODE.IMMEDIATE : FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                        }
                        eventInstance.release();
                        eventInstance.clearHandle();
                    }
                }
                else
                {
                    // Handled by the editor auditioning system.
                    EventArgs args = new EventArgs();
                    args.eventInstance = eventInstance;
                    Exit.Invoke(this, args);
                }
            }
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (eventInstance.isValid())
            {
                foreach (ParameterAutomationLink link in ParameterLinks)
                {
                    float value = ParameterAutomation.GetValue(link.Slot);
                    eventInstance.setParameterByID(link.ID, value);
                }
            }
        }

        public void UpdateBehavior(float time, float volume)
        {
            if (volume != CurrentVolume)
            {
                CurrentVolume = volume;

                if (eventInstance.isValid())
                {
                    eventInstance.setVolume(volume);
                }
            }

            if ((time >= OwningClip.start) && (time < OwningClip.end))
            {
                ClipStartTime = time - (float)OwningClip.start;
                OnEnter();
            }
            else
            {
                OnExit();
            }
        }

        public override void OnGraphStop(Playable playable)
        {
            isPlayheadInside = false;

            if (Application.isPlaying)
            {
                if (eventInstance.isValid())
                {
                    eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    eventInstance.release();
                    RuntimeManager.StudioSystem.update();
                }
            }
            else
            {
                // Handled by the editor auditioning system.
                EventArgs args = new EventArgs();
                args.eventInstance = eventInstance;
                GraphStop.Invoke(this, args);
            }
        }
    }
}
#endif
