using DG.Tweening;
using FIMSpace.FEditor;
using FIMSpace.FProceduralAnimation;
#if UNITY_EDITOR
using UnityEditor;
  #endif
using UnityEngine;
namespace Gameplay.Units
{
	// [CreateAssetMenu(fileName = "LAM_CustomHips", menuName = "Scriptable Objects/Legs Animator/LAM_CustomHips", order = 2)]
	public class CustomHipsController:  LegsAnimatorControlModuleBase
	{
		private LegsAnimator.Variable _hipPositionOffset;
		private LegsAnimator.Variable _hipRotationOffset;

		private Vector3 _prevRotationAngle;
		private Quaternion _prevRotation;

		private Tween _prevPositionAnimation;
		private Tween _prevRotationAnimation;
		
		public override void OnInit(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
		{
			_hipPositionOffset = helper.RequestVariable("_hipPositionOffset", Vector3.zero);
			_hipRotationOffset = helper.RequestVariable("_hipRotationOffset", Vector3.zero);
			_prevRotationAngle = _hipRotationOffset.GetVector3();
			_prevRotation = Quaternion.Euler(_prevRotationAngle);
		}

		public override void OnLateUpdatePreApply(LegsAnimator.LegsAnimatorCustomModuleHelper helper)
		{
			LA.Hips.localPosition = _hipPositionOffset.GetVector3();

			var rotationAngle = _hipRotationOffset.GetVector3();
			if (rotationAngle != _prevRotationAngle)
			{
				_prevRotation = Quaternion.Euler(rotationAngle);
				_prevRotationAngle = rotationAngle;
			}

			LA.Hips.localRotation = _prevRotation;
		}

		public void AnimateHips(Vector3 endPosition, Vector3 endRotation, float duration)
		{
			StopPreviousAnimation();
			
			_prevPositionAnimation = DOVirtual.Vector3(_hipPositionOffset.GetVector3(), endPosition, duration, value =>
			{
				_hipPositionOffset.SetValue(value);
			}).SetEase(Ease.OutBack).OnComplete(() =>
			{
				_prevPositionAnimation = null;
			});
			
			_prevRotationAnimation = DOVirtual.Vector3(_hipRotationOffset.GetVector3(), endRotation, duration * 0.5f, value =>
			{
				_hipRotationOffset.SetValue(value);
			}).SetEase(Ease.InQuad).OnComplete(() =>
			{
				_prevRotationAnimation = null;
			});

		}
		private void StopPreviousAnimation()
		{
			if (_prevPositionAnimation != null)
			{
				_prevPositionAnimation.Kill();
				_prevPositionAnimation = null;
			}
			if (_prevRotationAnimation != null)
			{
				_prevRotationAnimation.Kill();
				_prevRotationAnimation = null;
			}
		}

#if UNITY_EDITOR

		public override void Editor_InspectorGUI(LegsAnimator legsAnimator, LegsAnimator.LegsAnimatorCustomModuleHelper helper)
		{
			EditorGUILayout.HelpBox("Setting custom position / rotation to hips", MessageType.Info);
			GUILayout.Space(3);
			
			var hipPositionOffset = helper.RequestVariable("_hipPositionOffset", Vector3.zero);
			hipPositionOffset.AssignTooltip("Offset for position (local)");
			hipPositionOffset.Editor_DisplayVariableGUI();

			var hipRotationOffset = helper.RequestVariable("_hipRotationOffset", Vector3.zero);
			hipRotationOffset.AssignTooltip("Offset for rotation (local)");
			hipRotationOffset.Editor_DisplayVariableGUI();
		}

#endif
		public Vector3 GetPositionOffset()
		{
			return _hipPositionOffset.GetVector3();
		}

		public Vector3 GetRotationOffset()
		{
			return _hipRotationOffset.GetVector3();
		}
	}
}