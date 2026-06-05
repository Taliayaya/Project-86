using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/MorphoStunned")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "MorphoStunned", message: "[Morpho] is stunned for [StunDuration]", category: "Events", id: "a0bb307f5577a820dcd96a0b9645f38a")]
public sealed partial class MorphoStunned : EventChannel<GameObject, float> { }

