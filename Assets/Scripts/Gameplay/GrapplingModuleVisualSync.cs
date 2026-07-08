using System;
using DG.Tweening;
using Gameplay.Mecha;
using UI.HUD;
using UnityEngine;

namespace Gameplay
{
	public class GrapplingModuleVisualSync : MonoBehaviour
	{
		[SerializeField] private GrapplingModule Grappling;
		[SerializeField] private LineRenderer GrapplingRenderer;
		[SerializeField] private Transform VisualBone;

		private Vector3 _defaultBonePos;
		private bool _active = false;
		private int _linePosCount;
		
		private void Start()
		{
			_defaultBonePos = VisualBone.localPosition;
			_linePosCount = GrapplingRenderer.positionCount - 1;
			EventManager.AddListener("GrapplingModule", ChangedState);
		}

		private void OnDestroy()
		{
			EventManager.RemoveListener("GrapplingModule", ChangedState);
		}

		private void Reset()
		{
			Grappling = GetComponent<GrapplingModule>();
			GrapplingRenderer = GetComponent<LineRenderer>();
		}

		private void LateUpdate()
		{
			if (!_active) return;

			VisualBone.position = GrapplingRenderer.GetPosition(_linePosCount);
		}

		/// <param name="rawData">Type is <see cref="UI.HUD.ModuleData"/> </param>
		private void ChangedState(object rawData)
		{
			if (rawData is not ModuleData data) return;
			
			bool newState = data.status == ModuleStatus.Active;
			if (_active == newState) return;
			
			_active = newState;
			
			if (!_active)
			{
				VisualBone.DOLocalMove(_defaultBonePos, 0.3f);
			}
		}
	}
}