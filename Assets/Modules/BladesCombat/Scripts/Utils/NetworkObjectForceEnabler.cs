using System;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
namespace BladesCombat.Utils
{
	public class NetworkObjectForceEnabler : MonoBehaviour
	{
		[SerializeField] private NetworkObject ToEnableObject;
		[SerializeField] private NetworkBehaviour[] ToEnable;
		[SerializeField] private Behaviour[] EnableAfter;

		private void Start()
		{

			PropertyInfo clientId = typeof(NetworkBehaviour).GetProperty("OwnerClientId", BindingFlags.Public | BindingFlags.Instance);
			
			foreach (NetworkBehaviour behaviour in ToEnable)
			{
				clientId.SetValue(behaviour, NetworkManager.Singleton.LocalClientId);
				behaviour.enabled = true;
			}
			
			if (ToEnableObject != null)
			{
				PropertyInfo objectClientId = typeof(NetworkObject).GetProperty("OwnerClientId", BindingFlags.Public | BindingFlags.Instance);
				objectClientId.SetValue(ToEnableObject, NetworkManager.Singleton.LocalClientId);
				ToEnableObject.enabled = true;
			}
			
			foreach (Behaviour behaviour in EnableAfter)
			{
				behaviour.enabled = true;
			}
		}
	}
}