using System;
using Assets.Scripts;
using UnityEngine;

/// <summary>
/// Code from <a href="https://www.youtube.com/watch?v=BVCNDUcnE1o" />
/// </summary>
public class MeshSlicing
{
	private Transform _bladeTip;
	private Transform _bladeBase;
	private Vector3 _triggerEnterTipPosition;
	private Vector3 _triggerEnterBasePosition;
	private Vector3 _triggerExitTipPosition;
	private float _forceAppliedToCut;

	public event Action<GameObject> OnSliced; 

	public MeshSlicing(Transform bladeTip, Transform bladeBase, float forceAppliedToCut)
	{
		_bladeTip = bladeTip;
		_bladeBase = bladeBase;
		_forceAppliedToCut = forceAppliedToCut;
	}

	public void OnTriggerEnter(Collider other)
	{
		_triggerEnterTipPosition = _bladeTip.position;
		_triggerEnterBasePosition = _bladeBase.position;
	}

	public void OnTriggerExit(Collider other)
	{
		if (!other.gameObject.TryGetComponent(out Sliceable sliceable) || !sliceable.CanSlice)
		{
			/*if (sliceable == null)
			{
				Debug.LogError($"Doesn't have slicable", other.gameObject);
			}
			else
			{
				Debug.LogError($"slicablle disabled", other.gameObject);
			}*/
			return;
		}
		
		_triggerExitTipPosition = _bladeTip.position;

		Vector3 worldScale = other.transform.lossyScale;
		
		//Create a triangle between the tip and base so that we can get the normal
		Vector3 side1 = _triggerExitTipPosition - _triggerEnterTipPosition;
		Vector3 side2 = _triggerExitTipPosition - _triggerEnterBasePosition;

		//Get the point perpendicular to the triangle above which is the normal
		//https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
		Vector3 normal = Vector3.Cross(side1, side2).normalized;

		//Transform the normal so that it is aligned with the object we are slicing's transform.
		Vector3 transformedNormal = ((Vector3)(other.gameObject.transform.localToWorldMatrix.transpose * normal)).normalized;

		//Get the enter position relative to the object we're cutting's local transform
		Vector3 transformedStartingPoint = other.gameObject.transform.InverseTransformPoint(_triggerEnterTipPosition);

		Plane plane = new Plane();

		plane.SetNormalAndPosition(
			transformedNormal,
			transformedStartingPoint);

		var direction = Vector3.Dot(Vector3.up, transformedNormal);

		//Flip the plane so that we always know which side the positive mesh is on
		if (direction < 0)
		{
			plane = plane.flipped;
		}

		GameObject newPart = Slicer.SliceNewPart(plane, sliceable);

		Rigidbody rigidbody = newPart.GetComponent<Rigidbody>();
		Vector3 newNormal = transformedNormal + Vector3.up * _forceAppliedToCut;
		rigidbody.AddForce(newNormal, ForceMode.Impulse);

		newPart.transform.localScale = worldScale;

		OnSliced?.Invoke(other.gameObject);
	}
}