using System;
using UnityEngine;

namespace Gameplay
{
	public class ObjectTransformSync : MonoBehaviour
	{
		[SerializeField] private Transform Target;
		private Transform _transformCache;

		private void Start()
		{
			_transformCache = transform;
		}

		private void LateUpdate()
		{
			_transformCache.position = Target.position;
			_transformCache.rotation = Target.rotation;
		}
	}
}