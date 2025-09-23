using UnityEngine;
namespace BladesCombat
{
	/// <summary>
	/// Script that have reference to <see cref="BladeArmamentManager"/>
	/// </summary>
	public class BladeManagerReference : MonoBehaviour
	{
		[SerializeField] private BladeArmamentManager ArmamentManager;
		public BladeArmamentManager BladeArmamentManager => ArmamentManager;
	}
}