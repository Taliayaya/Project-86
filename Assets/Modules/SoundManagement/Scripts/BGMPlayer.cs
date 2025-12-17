using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Armament.Shared;
using DefaultNamespace.Sound;
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
		[SerializeField] private EventReference ExplorationBGM;
		[SerializeField] private EventReference Combat1BGM;
		[SerializeField] private EventReference Combat2IntenseBGM;
		[SerializeField] private EventReference Combat3DinosauriaBGM;
		[SerializeField] private EventReference Death1BGM;

		[Foldout("Debug")]
		[SerializeField] private bool ShowInfoWindow = true;

		private bool _enabled = false;

		private PlayingBGM _playing = PlayingBGM.Menu;
		private CombatBGMState _combatState = CombatBGMState.Default;

		private bool _isInMenu;
		private BoolCache<EventInstance> _menu = new();
		private BoolCache<EventInstance> _exploration = new();
		private BoolCache<EventInstance> _combat1 = new();
		private BoolCache<EventInstance> _combat2Intense = new();
		private BoolCache<EventInstance> _combat3Dinosauria = new();
		private BoolCache<EventInstance> _death1 = new();

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
			_exploration.Name = ExplorationBGM.GetEventName();
			_combat1.Name = Combat1BGM.GetEventName();
			_combat2Intense.Name = Combat2IntenseBGM.GetEventName();
			_combat3Dinosauria.Name = Combat3DinosauriaBGM.GetEventName();
			_death1.Name = Death1BGM.GetEventName();
		}

		private void ApplyMusicInitializers()
		{
			_menu.SetInitialization(() => CreateBGMInstance(MenuBGM));
			_exploration.SetInitialization(() => CreateBGMInstance(ExplorationBGM));
			_combat1.SetInitialization(() => CreateBGMInstance(Combat1BGM));
			_combat2Intense.SetInitialization(() => CreateBGMInstance(Combat2IntenseBGM));
			_combat3Dinosauria.SetInitialization(() => CreateBGMInstance(Combat3DinosauriaBGM));
			_death1.SetInitialization(() => CreateBGMInstance(Death1BGM));
		}

		private void InitAllMusicsList()
		{
			_associatedMusics = new Dictionary<PlayingBGM, MusicCollection>()
			{
				{ PlayingBGM.Menu, new MusicCollection(_menu) },
				{ PlayingBGM.Exploration, new MusicCollection(_exploration) },
				{ PlayingBGM.Combat, new MusicCollection(_combat1, _combat2Intense, _combat3Dinosauria, _death1) },
			};

			_allMusics = new MusicCollection(
				_menu, _exploration, _combat1, _combat2Intense, _combat3Dinosauria, _death1
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

			_menu.Value.start();

			_menu.FadeIn(true, 2f);

			Debug.Log($"Playing Menu");
		}

		public void PlayExploration()
		{
			if (_playing == PlayingBGM.Exploration) return;

			FadeOutOthers(PlayingBGM.Exploration, GetFadeoutDurationByState());

			_playing = PlayingBGM.Exploration;

			_exploration.TryInitialize();

			_exploration.Value.start();

			_exploration.FadeIn(true, 5f);

			Debug.Log($"Playing exploration");
		}

		public void PlayCombat()
		{
			if (_playing == PlayingBGM.Combat) return;

			FadeOutOthers(PlayingBGM.Combat, GetFadeoutDurationByState(ifNotMenu: 3f));

			_playing = PlayingBGM.Combat;

			BoolCache<EventInstance> combatMusic = GetMusicByCombatState(_combatState);

			combatMusic.TryInitialize();

			combatMusic.Value.start();

			combatMusic.FadeIn(true, 3f);

			Debug.Log($"Playing combat [{combatMusic.Name}]");
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

		public void SetCombatState(CombatBGMState combatState)
		{
			if (_playing == PlayingBGM.Combat)
			{
				if (combatState != _combatState)
				{
					BoolCache<EventInstance> targetMusic = GetMusicByCombatState(combatState);
					var except = _associatedMusics[PlayingBGM.Combat].Except(new[] { targetMusic });

					if (except.Any())
					{
						foreach (BoolCache<EventInstance> music in except)
						{
							music.FadeOut(3f);
						}
					}

					targetMusic.TryInitialize();

					targetMusic.Value.start();

					targetMusic.FadeIn(true, 3f);

					Debug.Log($"Playing combat [{targetMusic.Name}]");
				}
			}

			_combatState = combatState;
		}

		private BoolCache<EventInstance> GetMusicByCombatState(CombatBGMState combatState)
		{
			return combatState switch
			{
				CombatBGMState.Default => _combat1,
				CombatBGMState.Intense => _combat2Intense,
				CombatBGMState.Dinosauria => _combat3Dinosauria,
				CombatBGMState.Death => _death1
			};
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
			if (_playing == PlayingBGM.Combat)
			{
				BoolCache<EventInstance> state = GetMusicByCombatState(_combatState);
				if (state == null) return null;
				
				return state.Readonly;
			}
			else
			{
				BoolCache<EventInstance> state = _associatedMusics[_playing].Collection.First();
				if (state == null) return null;
				
				return state.Readonly;
			}
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