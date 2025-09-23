using System;
using System.Collections.Generic;
using System.Linq;
using Armament.Shared;
using UnityEngine;
namespace Utility
{
	[Serializable]
	public class BlendData<T>
	{
		public bool PersistentOnSceneLoad = false;
		public List<BlendPoint<T>> Points = new List<BlendPoint<T>>();
		private static List<BlendPoint<T>> PersistentPoints;
		
		public void Init()
		{
			if (PersistentOnSceneLoad)
			{
				if (PersistentPoints == null)
				{
					PersistentPoints = Points;
				}
				else
				{
					Points = PersistentPoints;
				}
			}
		}
		
		public T GetNearestData(Vector2 point)
		{
			if (Points.Count == 0) throw new InvalidOperationException($"Points are null. Add a point");

			if (point.x.Abs() + point.y.Abs() > 0.001f) // is not zero
				point = point.normalized;
			return Points.OrderBy(p => Vector2.Distance(p.Point, point)).First().Value;
		}
	}

	[Serializable]
	public struct BlendPoint<T>
	{
		public Vector2 Point;
		public Color Color;
		public T Value;
	}
}