using ScriptableObjects;
using UnityEditor;
using UnityEngine;
namespace Armament.Shared
{
	[CreateAssetMenu(menuName = "Scriptable Objects/Armament/Config")]
	public class ArmamentConfig : JsonSerializableSO
	{
		[SerializeField] private ArmamentType Armament;

		public ArmamentType CurrentArmament => Armament;
		
		public void SetArmament(ArmamentSO armament)
		{
			Armament = armament.ArmamentType;
		}
	}
}