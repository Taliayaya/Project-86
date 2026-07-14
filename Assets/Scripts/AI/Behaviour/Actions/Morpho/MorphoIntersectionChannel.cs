using System;
using System.Collections.Generic;
using AI;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;

#if UNITY_EDITOR
[CreateAssetMenu(menuName = "Behavior/Event Channels/MorphoIntersectionChannel")]
#endif
[Serializable, GeneratePropertyBag]
[EventChannelDescription(name: "MorphoIntersection", message: "Morpho is at a junction [BranchList]", category: "Events", id: "a0bb307f5577a820dcd96a0b9645f38b")]
public sealed partial class MorphoIntersectionChannel : EventChannel<List<Intersection.Branch>> { }

