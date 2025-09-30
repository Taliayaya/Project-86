using System;
using System.Collections.Generic;
using System.Linq;
using Armament.Shared;
using Gameplay.Mecha;
using UnityEngine;
namespace BladesCombat.Topple
{
	public class ToppleDirectionCalculator : MonoBehaviour
	{
		[SerializeField] private LoweTopple ToppleController;
		[SerializeField] private LegCollider[] Colliders;

		private List<int> _removedLeftLegs = new List<int>();
		private List<int> _removedRightLegs = new List<int>();
		private Dictionary<int, Vector2> _indexPoints = new Dictionary<int, Vector2>()
		{
			{0, Vector2.down},
			{1, Vector2.zero},
			{2, Vector2.zero},
			{3, Vector2.up},
		};

		public void RemoveLeg(LegCollider leg)
		{
			if (ToppleController == null) return;
			List<int> removedLegs = leg.IsLeft ? _removedLeftLegs : _removedRightLegs;
			removedLegs.Add(leg.Index);
			Debug.Log($"Removed {(leg.IsLeft ? "Left" : "Right")} leg, removed {removedLegs.Count} legs");
			
			if (removedLegs.Count >= 3)
			{
				Vector2 direction = GetToppleDirection();
				Debug.Log($"Toppling in {direction} direction");
				ToppleController.Topple(direction);
			}
		}
		private Vector2 GetToppleDirection()
		{
			Vector2 direction;

			if (_removedLeftLegs.Count == 4 && _removedRightLegs.Count == 4)
			{
				direction = Vector2.zero; // central
			}else if (_removedLeftLegs.Count == 4)
			{
				direction = Vector2.left;
			}else if (_removedRightLegs.Count == 4)
			{
				direction = Vector2.right;
			}
			else if (_removedLeftLegs.Count == 3 && _removedRightLegs.Count == 3)
			{
				int notRemovedLeft = NotRemovedLeg(_removedLeftLegs);
				int notRemovedRight = NotRemovedLeg(_removedRightLegs);

				if (notRemovedLeft == notRemovedRight)
				{
					direction = _indexPoints[notRemovedLeft];
				}else
				{
					Vector2 leftDirection = _indexPoints[notRemovedLeft];
					Vector2 rightDirection = _indexPoints[notRemovedRight];
					
					if (leftDirection.y.Abs() > 0.1f)
					{
						direction = new Vector2(-1, leftDirection.y).normalized;
					}else if (rightDirection.y.Abs() > 0.1f)
					{
						direction = new Vector2(1, rightDirection.y).normalized;
					}
					else
					{
						direction = new Vector2(0, new int[] { -1, 1 }.Random()); // either fall back or forward
					}
				}
			}
			else
			{
				var isLeft = _removedLeftLegs.Count == 3;
				List<int> legsWithMoreRemoved = isLeft ? _removedLeftLegs : _removedRightLegs;

				int notRemoved = NotRemovedLeg(legsWithMoreRemoved);
				Vector2 fallDirection = _indexPoints[notRemoved];
				float y = 0;
				
				if (fallDirection.y.Abs() > 0.1f) // had either
				{
					int inverted = 3 - notRemoved;
					y = _indexPoints[inverted].y;
				}
				else
				{
					bool leftOnlyBottomCenterLeg = notRemoved == 1;
					y = leftOnlyBottomCenterLeg ? 1 : -1;
				}
				direction = new Vector2(isLeft ? -1 : 1, y).normalized;
			}
			
			return direction;
		}

		private int NotRemovedLeg(List<int> removedLegs)
		{
			return _indexPoints.Keys.Except(removedLegs).FirstOrDefault();
		}
	}
}