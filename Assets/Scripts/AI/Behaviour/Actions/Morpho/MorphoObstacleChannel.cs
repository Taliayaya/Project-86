using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/MorphoObstacleChannel")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "MorphoObstacle", message: "Morpho met [Obstacle]", category: "Events", id: "a0bb307f5577a820dcd96a0b9645f38b")]
public sealed partial class MorphoObstacleChannel : EventChannel<Transform> { }

