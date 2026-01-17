using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Armament.Shared;
using DefaultNamespace.Sound;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using NaughtyAttributes;
using SoundManagement.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SoundManagement
{
	public class BGMPlayer : Singleton<BGMPlayer>
	{
		[SerializeField] private EventReference MenuBGM;
		[SerializeField] private EventReference GameBGM;
		[SerializeField] private EventReference DeathBGM;

		[Foldout("Debug")]
		[SerializeField] private bool ShowInfoWindow = true;

		private bool _enabled = false;

		private PlayingBGM _playing = PlayingBGM.Menu;
		private CombatBGMState _combatState = CombatBGMState.Default;

		private bool _isInMenu;
		private BoolCache<EventInstance> _menu = new();
		private BoolCache<EventInstance> _game = new();
		private BoolCache<EventInstance> _death = new();

		[SerializeField] private bool _isInCombat;
		public bool IsInCombat
		{
			get => _isInCombat;
			set
			{
				if (_isInCombat == value) return;
				_isInCombat = value;
				_game.Value.setParameterByName("IsCombat", value ? 1f : 0f);
			}
		}

		[SerializeField] private CombatBGMState _intensity;

		public CombatBGMState Intensity
		{
			get => _intensity;
			set
			{
				if (_intensity == value) return;
				_intensity = value;
				_game.Value.setParameterByName("Intensity", (int) value);
			}
		}

		private MusicCollection _allMusics;
		private Dictionary<PlayingBGM, MusicCollection> _associatedMusics = new();

		private List<EventInstance> _pausedInstances = new();

		private bool _isPaused = false;
		private bool _internalPause = false;
#if UNITY_EDITOR
		private DebugPanel _debugPanel = new();
#endif

		public bool IsPaused => _isPaused || _internalPause;
		public CombatBGMState CombatState => _combatState;

		private IEnumerator Start()
		{
			InitAllMusicsList();
			SetMusicHandlersNames();
			ApplyMusicInitializers();
			yield return null;

			_enabled = true;

			EventManager.AddListener(SoundEventName.OnPause, OnPause);
			EventManager.AddListener(SoundEventName.OnResume, OnResume);
			EventManager.AddListener(SoundEventName.MusicVolumeChanged, UpdateBGMVolume);

#if UNITY_EDITOR
			_debugPanel.Init(this);
#endif
		}

		private void SetMusicHandlersNames()
		{
			_menu.Name = MenuBGM.GetEventName();
			_game.Name = GameBGM.GetEventName();
			_death.Name = DeathBGM.GetEventName();
		}

		private void ApplyMusicInitializers()
		{
			_menu.SetInitialization(() => CreateBGMInstance(MenuBGM));
			_game.SetInitialization(() => CreateBGMInstance(GameBGM));
			_death.SetInitialization(() => CreateBGMInstance(DeathBGM));
		}

		private void InitAllMusicsList()
		{
			_associatedMusics = new Dictionary<PlayingBGM, MusicCollection>()
			{
				{ PlayingBGM.Menu, new MusicCollection(_menu) },
				{ PlayingBGM.Game, new MusicCollection(_game, _death) },
			};
			

			_allMusics = new MusicCollection(
				_menu, _game, _death
			);
		}

		private void OnDisable()
		{
			if (!_enabled) return;

			Debug.LogError($"Unsubscribed from UpdateGameParameter:musicVolume");
			EventManager.RemoveListener(SoundEventName.OnPause, OnPause);
			EventManager.RemoveListener(SoundEventName.OnResume, OnResume);
			EventManager.RemoveListener(SoundEventName.MusicVolumeChanged, UpdateBGMVolume);
		}

		private void UpdateBGMVolume(object data)
		{
			float volume = GetVolume();

			foreach (BoolCache<EventInstance> music in _allMusics)
			{
				if (music.HasValue)
				{
					music.Value.setVolume(volume);
				}
			}
		}

		private void OnPause()
		{
			if (_internalPause) return;
			_internalPause = true;
			HandlePause();
		}

		private void OnResume()
		{
			if (!_internalPause) return;
			_internalPause = false;
			HandlePause();
		}

		public void CheckInMenu(bool isInMenu, bool isInCombat = false)
		{
			_isInMenu = isInMenu;
			if (_isInMenu)
			{
				PlayMenuMusic();
			}
			else
			{
				PlayGameplayMusic(isInCombat);
			}
		}

		private void PlayGameplayMusic(bool isInCombat)
		{
			if (_playing != PlayingBGM.Menu)
			{
				Debug.Log($"Skipped playing exploration because current playing is: {_playing}");
				return;
			}

			if (isInCombat)
				PlayCombat();
			else
				PlayExploration();
		}

		private void PlayMenuMusic()
		{
			FadeOutOthers(PlayingBGM.Menu, 2f);

			_playing = PlayingBGM.Menu;
			
			_menu.TryInitialize();

			// This was failing because the music can take some time to load the file
			// and not be ready in time which ends in a failure. So we restart it after a little while.
			var result = _menu.Value.start();
			if (result != RESULT.OK)
			{
				Debug.LogError($"Failed to start Menu BGM: {result}");
				Invoke(nameof(PlayMenuMusic), 0.5f);
				return;
			}

			_menu.FadeIn(true, 2f);

			Debug.Log($"Playing Menu ({result})");
		}

		public void PlayGame()
		{
			if (_playing == PlayingBGM.Game) return;
			FadeOutOthers(PlayingBGM.Game, GetFadeoutDurationByState());
			_game.TryInitialize();
			var result = _game.Value.start();
			if (result != RESULT.OK)
			{
				Debug.LogError($"Failed to start Game BGM: {result}");
				Invoke(nameof(PlayGame), 0.5f);
				return;
			}
			_playing = PlayingBGM.Game;
			_game.FadeIn(true, 5f);
			Debug.Log($"Playing Game");
		}

		public void PlayExploration()
		{
			PlayGame();
			IsInCombat = false;
		}

		public void PlayCombat()
		{
			PlayGame();
			IsInCombat = true;
		}

		private float GetFadeoutDurationByState(float ifNotMenu = 5f, float ifMenu = 2f)
		{
			return _playing == PlayingBGM.Menu ? ifMenu : ifNotMenu;
		}

		private void OnApplicationPause(bool pauseStatus)
		{
			if (_isPaused == pauseStatus) return;

			_isPaused = pauseStatus;
			HandlePause();
		}

		private void HandlePause()
		{
			if (IsPaused)
			{
				_pausedInstances.Clear();
				foreach (BoolCache<EventInstance> music in _allMusics)
				{
					if (music.IsPlaying())
					{
						music.Value.setPaused(true);
						_pausedInstances.Add(music.Value);
					}
				}
			}
			else
			{
				foreach (EventInstance pausedInstance in _pausedInstances)
				{
					pausedInstance.setPaused(false);
				}

				_pausedInstances.Clear();
			}
		}

		private void FadeOutOthers(PlayingBGM except, float duration = 4f)
		{
			foreach (var (key, value) in _associatedMusics)
			{
				if (key == except) continue;

				foreach (BoolCache<EventInstance> boolCache in value)
				{
					if (!boolCache.IsPlaying()) continue;

					boolCache.FadeOut(duration);
				}
			}
		}

		private EventInstance CreateBGMInstance(EventReference reference)
		{
			return reference.CreateInstance(GetVolume());
		}

		private static float GetVolume()
		{
			float volume = SoundManager.Instance.MusicVolume.Clamp(0, 100);
			if (volume > 0)
			{
				volume /= 100f;
			}

			Debug.Log($"Setting volume {volume} (from: {SoundManager.Instance.MusicVolume.Clamp(0, 100)})");
			return volume;
		}

		public ReadonlyBoolCache<EventInstance> GetCurrentPlaying()
		{
			BoolCache<EventInstance> state = _associatedMusics[_playing].Collection.First();
			if (state == null) return null;

			return state.Readonly;
		}

#if UNITY_EDITOR

		private void OnGUI()
		{
			if (!ShowInfoWindow) return;
			
			_debugPanel.OnGUI();
		}

#endif
	}
}