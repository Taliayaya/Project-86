using UnityEngine;
using UnityEngine.Events;
namespace BladesCombat
{
	public class BladeHitReceiver : MonoBehaviour
	{

		public UnityEvent<StabType> OnReceiveDamage;
		
		public void TakeDamage(StabType stabType)
		{
			OnReceiveDamage?.Invoke(stabType);
		}
	}
}