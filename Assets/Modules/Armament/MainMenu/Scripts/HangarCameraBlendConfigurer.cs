using System.Linq;
using Armament.Shared;
using Unity.Cinemachine;
using UnityEngine;
namespace Armament.MainMenu
{
	using CustomBlend = CinemachineBlenderSettings.CustomBlend;
	using BlendDefinition = CinemachineBlendDefinition;

	public class HangarCameraBlendConfigurer
	{
		private CinemachineCamera _hangarCamera;
		private CinemachineCamera _personalMarkCamera;
		private CinemachineCamera _armamentLeftCamera;
		private CinemachineCamera _armamentRightCamera;
		private CustomBlend[] _defaultBlends;
		private CinemachineBrain _cinemachineBrain;
		public CinemachineBrain CinemachineBrain
		{
			get => _cinemachineBrain;
		}

		public HangarCameraBlendConfigurer(CinemachineCamera hangarCamera, CinemachineCamera armamentLeftCamera, CinemachineCamera armamentRightCamera, CinemachineCamera personalMarkCamera)
		{
			_hangarCamera = hangarCamera;
			_personalMarkCamera = personalMarkCamera;
			_armamentLeftCamera = armamentLeftCamera;
			_armamentRightCamera = armamentRightCamera;
		}

		public void OnDisable()
		{
			if (_cinemachineBrain != null)
			{
				_cinemachineBrain.CustomBlends.CustomBlends = _defaultBlends;
			}
		}

		public void SetupCameraBlends()
		{
			Camera mainCamera = Camera.main;
			if (mainCamera == null)
			{
				Debug.LogError($"Main Camera is not found. Make sure Camera's tag is set MainCamera");
				return;
			}

			_cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
			if (_cinemachineBrain == null)
			{
				Debug.LogError($"Cinemachine Brain not found in {mainCamera.name}. Setup blend is terminated");
				return;
			}

			_defaultBlends = _cinemachineBrain.CustomBlends.CustomBlends;

			TempList<CustomBlend> newBlends = _defaultBlends.Create();

			if (HasHangarBlends())
			{
				ClearHangarBlends(newBlends);
			}

			InsertImmediateBlends(newBlends);

			AddSmoothBlends(newBlends);

			_cinemachineBrain.CustomBlends.CustomBlends = newBlends.ToArray();

			newBlends.Dispose();
		}
		private bool HasHangarBlends()
		{
			if (_defaultBlends.Length == 0) return false;

			TempArray<string> hangarCameraNames = GetHangarCameraNames();

			bool hasHangarBlends = _defaultBlends.Any(d => hangarCameraNames.Contains(d.From) || hangarCameraNames.Contains(d.To));

			hangarCameraNames.Dispose();

			return hasHangarBlends;
		}

		private void ClearHangarBlends(TempList<CustomBlend> newBlends)
		{
			TempArray<string> hangarCameraNames = GetHangarCameraNames();

			TempList<CustomBlend> otherCameras = newBlends.Where(b => !hangarCameraNames.Contains(b.From) && !hangarCameraNames.Contains(b.To)).Create();
			if (otherCameras.Count != newBlends.Count)
			{
				newBlends.Clear();
				if (otherCameras.Count > 0)
					newBlends.AddRange(otherCameras);
			}

			hangarCameraNames.Dispose();
			otherCameras.Dispose();
		}

		private TempArray<string> GetHangarCameraNames()
		{

			TempArray<string> hangarCameraNames = new TempArray<string>(4);
			hangarCameraNames[0] = _hangarCamera.name;
			hangarCameraNames[1] = _armamentRightCamera.name;
			hangarCameraNames[2] = _personalMarkCamera.name;
			hangarCameraNames[3] = _armamentLeftCamera.name;
			return hangarCameraNames;
		}


		private void InsertImmediateBlends(TempList<CustomBlend> newBlends)
		{

			InsertCustomBlend(newBlends, "**ANY CAMERA**", _hangarCamera.name, BlendDefinition.Styles.Cut);
			InsertCustomBlend(newBlends, "**ANY CAMERA**", _armamentLeftCamera.name, BlendDefinition.Styles.Cut);
			InsertCustomBlend(newBlends, "**ANY CAMERA**", _armamentRightCamera.name, BlendDefinition.Styles.Cut);
			InsertCustomBlend(newBlends, "**ANY CAMERA**", _personalMarkCamera.name, BlendDefinition.Styles.Cut);
			
			InsertCustomBlend(newBlends, _hangarCamera.name, "**ANY CAMERA**", BlendDefinition.Styles.Cut);
			InsertCustomBlend(newBlends, _armamentLeftCamera.name, "**ANY CAMERA**", BlendDefinition.Styles.Cut);
			InsertCustomBlend(newBlends, _armamentRightCamera.name, "**ANY CAMERA**", BlendDefinition.Styles.Cut);
			InsertCustomBlend(newBlends, _personalMarkCamera.name, "**ANY CAMERA**", BlendDefinition.Styles.Cut);
		}
		private void AddSmoothBlends(TempList<CustomBlend> newBlends)
		{

			InsertCustomBlend(newBlends, _armamentLeftCamera.name, _hangarCamera.name);
			InsertCustomBlend(newBlends, _armamentRightCamera.name, _hangarCamera.name);
			InsertCustomBlend(newBlends, _personalMarkCamera.name, _hangarCamera.name);

			InsertCustomBlend(newBlends, _hangarCamera.name, _armamentLeftCamera.name);
			InsertCustomBlend(newBlends, _armamentRightCamera.name, _armamentLeftCamera.name);
			InsertCustomBlend(newBlends, _personalMarkCamera.name, _armamentLeftCamera.name);

			InsertCustomBlend(newBlends, _hangarCamera.name, _armamentRightCamera.name);
			InsertCustomBlend(newBlends, _armamentLeftCamera.name, _armamentRightCamera.name);
			InsertCustomBlend(newBlends, _personalMarkCamera.name, _armamentRightCamera.name);

			InsertCustomBlend(newBlends, _hangarCamera.name, _personalMarkCamera.name);
			InsertCustomBlend(newBlends, _armamentLeftCamera.name, _personalMarkCamera.name);
			InsertCustomBlend(newBlends, _armamentRightCamera.name, _personalMarkCamera.name);
		}

		private void InsertCustomBlend(TempList<CustomBlend> settings, string from, string to, BlendDefinition.Styles tweenType = BlendDefinition.Styles.EaseInOut, float duration = 1f)
		{
			settings.Insert(0, new CustomBlend()
			{
				Blend = new BlendDefinition() { Style = tweenType, Time = duration },
				From = from,
				To = to
			});
		}

	}
}