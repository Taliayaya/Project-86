using UnityEngine;
namespace BladesCombat.Topple
{
	public class LegCollider : MonoBehaviour
	{
		[SerializeField] private ToppleDirectionCalculator DirectionCalculator;

		[SerializeField] private bool IsLeftLeg;
		[SerializeField] private int LegIndex;
		public bool IsLeft => IsLeftLeg;
		public int Index => LegIndex;

		public void RemoveLeg()
		{
			DirectionCalculator.RemoveLeg(this);
		}
	}
}