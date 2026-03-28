using System;
using System.Collections.Generic;
using UnityEngine;
namespace BladesCombat.Topple
{
	public class LegCollider : MonoBehaviour
	{
		[SerializeField] private ToppleDirectionCalculator DirectionCalculator;

		[SerializeField] private bool IsLeftLeg;
		[SerializeField] private int LegIndex;
		
		List<GameObject> _parts = new List<GameObject>();
		public bool IsLeft => IsLeftLeg;
		public int Index => LegIndex;

		private void OnDestroy()
		{
			foreach (var part in _parts)
			{
				Destroy(part);
			}
		}

		public void RemoveLeg(GameObject part)
		{
			_parts.Add(part);
			DirectionCalculator.RemoveLeg(this);
		}
	}
}