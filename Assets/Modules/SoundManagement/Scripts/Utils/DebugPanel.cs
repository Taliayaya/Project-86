using FMOD.Studio;
using UnityEngine;

namespace SoundManagement.Utils
{
	public class DebugPanel
	{
		private GUIStyle _richText;
		private bool _inited = false;
		private BGMPlayer _player;
		private Rect _rectPosition = new Rect(40, 130, 270, 90);

		public void Init(BGMPlayer bgmPlayer)
		{
			_player = bgmPlayer;
		}
		
		public void OnGUI()
		{
			if (!_inited)
			{
				_inited = true;
				_richText = new GUIStyle(GUI.skin.label)
				{
					richText = true,
					fontSize = 20
				};
			}


			_rectPosition = GUI.Window(1999, _rectPosition, DrawMutedText, "BGM Player Debug Panel");
		}

		private void DrawMutedText(int id)
		{
			if (_player == null) return;
			
			ReadonlyBoolCache<EventInstance> currentPlaying = _player.GetCurrentPlaying();

			string playingBGMName = currentPlaying.Name;

			if (_player.PlayingMusic == PlayingBGM.Game)
			{
				playingBGMName = _player.IsCombat
					? _player.CombatState.ToString()
					: CombatBGMState.Exploration.ToString();

			}

			GUILayout.Label($"Playing <b>{playingBGMName}</b>", _richText);
			
			bool isMuted = UnityEditor.EditorUtility.audioMasterMute;
			
			if (isMuted)
				GUILayout.Label($"Audio is <b><color=red>DISABLED</color></b>", _richText);

			GUI.DragWindow();
		}
	}
}