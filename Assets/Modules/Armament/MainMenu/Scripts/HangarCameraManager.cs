using System;
using System.Collections.Generic;
using System.Linq;
using Armament.Shared;
using Cinemachine;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
namespace Armament.MainMenu
{

	public class HangarCameraManager : MonoBehaviour
	{
		public static HangarCameraManager Instance;

		[SerializeField] private CinemachineVirtualCamera HangarCamera;
		[SerializeField] private CinemachineVirtualCamera PersonalMarkCamera;
		[SerializeField] private CinemachineVirtualCamera ArmamentLeftCamera;
		[SerializeField] private CinemachineVirtualCamera ArmamentRightCamera;
		[SerializeField] private Transform JuggernautVisual;

		private HangarCameraBlendConfigurer _blendConfigurer;

		private void Awake()
		{
			Instance = this;
			_blendConfigurer = new HangarCameraBlendConfigurer(HangarCamera, ArmamentLeftCamera, ArmamentRightCamera, PersonalMarkCamera);
		}

		private void OnDestroy()
		{
			Instance = null;
		}

		private void Start()
		{
			MenuEvents.Instance.OnOpenedHangar += OpenedHangar;
			MenuEvents.Instance.OnClosedHangar += OnClosedHangar;
			MenuEvents.Instance.OnTabChanged += ChangeCameraByTab;
			MenuEvents.Instance.OnChangedArmament += SetArmamentCamera;
			_blendConfigurer.SetupCameraBlends();
		}
		

		private void OnDisable()
		{
			_blendConfigurer.OnDisable();

		}
		private void ChangeCameraByTab(TabSwitcher currentTab)
		{
			if (currentTab.Type == TabType.Armament)
			{
				SetHangarCamera();
			}
			else if (currentTab.Type == TabType.PersonalMark)
			{
				SetPersonalMarkCamera();
			}
		}

		public void OpenedHangar()
		{
			SetHangarCamera();

		}
		private void OnClosedHangar()
		{
			const float wait1FrameIn60FPS = 1 / 60f;
			DOVirtual.DelayedCall(wait1FrameIn60FPS, SetHangarCamera, false);
		}

		private void SetHangarCamera()
		{
			SetCameraStatus(hangarState: true, armamentState: false, personalMarkState: false);
		}

		private void SetPersonalMarkCamera()
		{
			SetCameraStatus(hangarState: false, armamentState: false, personalMarkState: true);
		}

		private void SetArmamentCamera()
		{
			SetCameraStatus(hangarState: false, armamentState: true, personalMarkState: false);
		}

		private void SetCameraStatus(bool hangarState, bool armamentState, bool personalMarkState)
		{

			bool isLeftArmamentActive = false;
			bool isRightArmamentActive = false;
			if (armamentState)
			{
				// in world position -z is x
				isLeftArmamentActive = _blendConfigurer.CinemachineBrain.transform.position.z > JuggernautVisual.position.z;
				isRightArmamentActive = !isLeftArmamentActive;

			}
			
			HangarCamera.gameObject.SetActive(hangarState);
			PersonalMarkCamera.gameObject.SetActive(personalMarkState);
			ArmamentLeftCamera.gameObject.SetActive(isLeftArmamentActive);
			ArmamentRightCamera.gameObject.SetActive(isRightArmamentActive);
		}
	}
}