using System.Linq;
using Armament.Shared;
using Cinemachine;
using UnityEngine;
namespace Armament.MainMenu
{
	using CustomBlend = CinemachineBlenderSettings.CustomBlend;
	using BlendDefinition = CinemachineBlendDefinition;

	public class HangarCameraBlendConfigurer
	{
		private CinemachineVirtualCamera _hangarCamera;
		private CinemachineVirtualCamera _personalMarkCamera;
		private CinemachineVirtualCamera _armamentLeftCamera;
		private CinemachineVirtualCamera _armamentRightCamera;
		private CustomBlend[] _defaultBlends;
		private CinemachineBrain _cinemachineBrain;
		public CinemachineBrain CinemachineBrain
		{
			get => _cinemachineBrain;
		}

		public HangarCameraBlendConfigurer(CinemachineVirtualCamera hangarCamera, CinemachineVirtualCamera armamentLeftCamera, CinemachineVirtualCamera armamentRightCamera, CinemachineVirtualCamera personalMarkCamera)
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
				_cinemachineBrain.m_CustomBlends.m_CustomBlends = _defaultBlends;
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

			_defaultBlends = _cinemachineBrain.m_CustomBlends.m_CustomBlends;

			TempList<CustomBlend> newBlends = _defaultBlends.Create();

			if (HasHangarBlends())
			{
				ClearHangarBlends(newBlends);
			}

			InsertImmediateBlends(newBlends);

			AddSmoothBlends(newBlends);

			_cinemachineBrain.m_CustomBlends.m_CustomBlends = newBlends.ToArray();

			newBlends.Dispose();
		}
		private bool HasHangarBlends()
		{
			if (_defaultBlends.Length == 0) return false;

			TempArray<string> hangarCameraNames = GetHangarCameraNames();

			bool hasHangarBlends = _defaultBlends.Any(d => hangarCameraNames.Contains(d.m_From) || hangarCameraNames.Contains(d.m_To));

			hangarCameraNames.Dispose();

			return hasHangarBlends;
		}

		private void ClearHangarBlends(TempList<CustomBlend> newBlends)
		{
			TempArray<string> hangarCameraNames = GetHangarCameraNames();

			TempList<CustomBlend> otherCameras = newBlends.Where(b => !hangarCameraNames.Contains(b.m_From) && !hangarCameraNames.Contains(b.m_To)).Create();
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

			InsertCustomBlend(newBlends, "**ANY CAMERA**", _hangarCamera.name, BlendDefinition.Style.Cut);
			InsertCustomBlend(newBlends, "**ANY CAMERA**", _armamentLeftCamera.name, BlendDefinition.Style.Cut);
			InsertCustomBlend(newBlends, "**ANY CAMERA**", _armamentRightCamera.name, BlendDefinition.Style.Cut);
			InsertCustomBlend(newBlends, "**ANY CAMERA**", _personalMarkCamera.name, BlendDefinition.Style.Cut);
			
			InsertCustomBlend(newBlends, _hangarCamera.name, "**ANY CAMERA**", BlendDefinition.Style.Cut);
			InsertCustomBlend(newBlends, _armamentLeftCamera.name, "**ANY CAMERA**", BlendDefinition.Style.Cut);
			InsertCustomBlend(newBlends, _armamentRightCamera.name, "**ANY CAMERA**", BlendDefinition.Style.Cut);
			InsertCustomBlend(newBlends, _personalMarkCamera.name, "**ANY CAMERA**", BlendDefinition.Style.Cut);
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

		private void InsertCustomBlend(TempList<CustomBlend> settings, string from, string to, BlendDefinition.Style tweenType = BlendDefinition.Style.EaseInOut, float duration = 1f)
		{
			settings.Insert(0, new CustomBlend()
			{
				m_Blend = new BlendDefinition() { m_Style = tweenType, m_Time = duration },
				m_From = from,
				m_To = to
			});
		}

	}
}