using System;
using System.Linq;
using FIMSpace.FProceduralAnimation;
using Gameplay.Units;
using UnityEngine;
using UnityEngine.AI;
using Utility;
namespace Gameplay.Mecha
{
	public class LoweTopple : MonoBehaviour
	{
		[SerializeField] private NavMeshAgent Agent;

		[SerializeField] private float Duration = 0.5f;
		[SerializeField] private LegsAnimator LegsAnimator;

		[SerializeField] private TransformData LegData;
		[SerializeField, Tooltip("Body offset in for topple directions")] private BlendData<ToppleData> Data;
		
		private CustomHipsController _hipsController;
		private Vector3 _defaultPosition;
		private Vector3 _defaultRotation;

		private void Start()
		{
			_hipsController = LegsAnimator.GetModule<CustomHipsController>();
			_defaultPosition = _hipsController.GetPositionOffset();
			_defaultRotation = _hipsController.GetRotationOffset();
			Data.Init();
		}

		/*
		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Keypad1))
			{
				Topple(new Vector2(-1, -1));
			}
			if (Input.GetKeyDown(KeyCode.Keypad4))
			{
				Topple(new Vector2(-1, 0));
			}
			if (Input.GetKeyDown(KeyCode.Keypad7))
			{
				Topple(new Vector2(-1, 1));
			}
			
			
			if (Input.GetKeyDown(KeyCode.Keypad3))
			{
				Topple(new Vector2(1, -1));
			}
			if (Input.GetKeyDown(KeyCode.Keypad6))
			{
				Topple(new Vector2(1, 0));
			}
			if (Input.GetKeyDown(KeyCode.Keypad9))
			{
				Topple(new Vector2(1, 1));
			}
			
			if (Input.GetKeyDown(KeyCode.Keypad2))
			{
				Topple(new Vector2(0, -1));
			}
			
			if (Input.GetKeyDown(KeyCode.Keypad8))
			{
				Topple(new Vector2(0, 1));
			}
			
			if (Input.GetKeyDown(KeyCode.Keypad5))
			{
				Topple(new Vector2(0, 0));
			}
			
			if (Input.GetKeyDown(KeyCode.Keypad0))
			{
				EnableAgent();
				EnableAllLegs();
				Topple(new ToppleData() { LocalPosition = _defaultPosition, LocalRotation = _defaultRotation });
			}
		}
		*/
		
		
		
		public void Topple(Vector2 direction)
		{
			DisableAgent();
			// EnableAllLegs();
			Topple(Data.GetNearestData(direction));

			GameObject[] nearest = LegData.GetNearestData(direction);
			foreach (GameObject toHide in nearest)
			{
				toHide.SetActive(false);
			}
		}

		private void EnableAllLegs()
		{
			LegsAnimator.Leg leg = LegsAnimator.Legs.First();

			while (leg != null)
			{
				leg.BoneMid.gameObject.SetActive(true);
				leg = leg.NextLeg;
			}
		}
		
		private void EnableAgent()
		{
			if (!Agent.enabled)
			{
				Agent.enabled = true;
				Agent.isStopped = false;
			}			
		}
		
		private void DisableAgent()
		{
			if (Agent.enabled)
			{
				Agent.isStopped = true;
				Agent.enabled = false;
			}
		}

		private void Topple(ToppleData data)
		{
			_hipsController.AnimateHips(data.LocalPosition, data.LocalRotation, Duration);
		}
		
	}

	[Serializable]
	public struct ToppleData
	{
		public Vector3 LocalPosition;
		public Vector3 LocalRotation;
	}
}