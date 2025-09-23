using System;
using System.Collections.Generic;
using System.Linq;
using Armament.Shared;
using UnityEngine;
namespace Utility
{
	[Serializable]
	public class TransformData
	{
		public List<LinkedTransforms> Directions = new List<LinkedTransforms>();
		
		public GameObject[] GetNearestData(Vector2 point)
		{
			if (Directions.Count == 0) throw new InvalidOperationException($"Points are null. Add a point");

			if (point.x.Abs() + point.y.Abs() > 0.001f) // is not zero
				point = point.normalized;
			return Directions.OrderBy(p => Vector2.Distance(p.Point, point)).First().ToHide;
		}
	}

	[Serializable]
	public struct LinkedTransforms
	{
		public Vector2 Point;
		public GameObject[] ToHide;
	}
}