using System;
using BladesCombat.Topple;
using UnityEngine;
namespace BladesCombat
{
	public class MeshSlicer : MonoBehaviour
	{
		[SerializeField] private Transform LeftTip;
		[SerializeField] private Transform LeftBase;
		[SerializeField] private Transform RightTip;
		[SerializeField] private Transform RightBase;

		[SerializeField] private float ForceApplication;
		
		[SerializeField] private BladeArmamentManager BladeManager;

		private MeshSlicing _leftSlicer;
		private MeshSlicing _rightSlicer;
		
		private void Start()
		{
			BladeManager.OnBladeTriggerEntered += TriggerEntered;
			BladeManager.OnBladeTriggerExited += TriggerExited;
			_leftSlicer = new MeshSlicing(LeftTip, LeftBase, ForceApplication);
			_rightSlicer = new MeshSlicing(RightTip, RightBase, ForceApplication);
			
			_leftSlicer.OnSliced += Sliced;
			_rightSlicer.OnSliced += Sliced;
		}
		private void Sliced(GameObject slicedObject)
		{
			if (slicedObject.TryGetComponent(out LegCollider legCollider))
			{
				legCollider.RemoveLeg();
			}
		}
		private void TriggerEntered(Collider other, TriggerData data)
		{
			MeshSlicing slicer = data.IsLeftBlade ? _leftSlicer : _rightSlicer;
			Debug.LogError($"{(data.IsLeftBlade ? "Left" : "Right")} trigger entered: {other.name}");
			slicer.OnTriggerEnter(other);
		}
		private void TriggerExited(Collider other, TriggerData data)
		{
			MeshSlicing slicer = data.IsLeftBlade ? _leftSlicer : _rightSlicer;
			Debug.LogError($"{(data.IsLeftBlade ? "Left" : "Right")} trigger exited: {other.name}");
			slicer.OnTriggerExit(other);
		}
	}
}