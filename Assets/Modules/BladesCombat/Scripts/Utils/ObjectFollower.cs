using System;
using UnityEngine;

namespace BladesCombat.Utils
{
	public class ObjectFollower : MonoBehaviour
	{
		[SerializeField] private Transform Target;
		
		private void Update()
		{
			transform.position = Vector3.Lerp(transform.position, Target.position, Time.deltaTime * 8f);
		}
	}
}