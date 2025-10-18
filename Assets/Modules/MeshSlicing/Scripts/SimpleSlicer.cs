using System;
using UnityEngine;
namespace Scripts
{
	public class SimpleSlicer : MonoBehaviour
	{
		[SerializeField] private Transform Tip;
		[SerializeField] private Transform Base;
		[SerializeField] private float ForceApplication;
		[SerializeField] private float lifetime = 30f;

		private MeshSlicing _slicing;
		
		private void Start()
		{

			_slicing = new MeshSlicing(Tip, Base, ForceApplication, lifetime);
		}

		private void OnTriggerEnter(Collider other)
		{
			_slicing.OnTriggerEnter(other);
		}

		private void OnTriggerExit(Collider other)
		{
			_slicing.OnTriggerExit(other);
		}
	}
}