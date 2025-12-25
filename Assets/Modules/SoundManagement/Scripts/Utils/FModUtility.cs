using DG.Tweening;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace SoundManagement.Utils
{
	public static class FModUtility
	{
		public static EventInstance CreateInstance(this EventReference reference)
		{
			return RuntimeManager.CreateInstance(reference);
		}

		public static EventInstance CreateInstance(this EventReference reference, float volume)
		{
			EventInstance instance = reference.CreateInstance();
			
			instance.setVolume(volume);
			
			return instance;
		}
		
		public static void PlayOneShot(this EventReference reference, Vector3 position = default(Vector3))
		{
			RuntimeManager.PlayOneShot(reference, position);
		}
		
		public static void FadeIn(this EventInstance reference, bool startFromZero, float duration = 1f)
		{
			if (startFromZero)
				reference.setParameterByName("fade_volume", 0);

			DOVirtual.Float(0f, 1f, duration, volume =>
			{
				reference.setParameterByName("fade_volume", volume);
			});
		}

		public static void FadeOut(this EventInstance reference, float duration = 1f)
		{
			DOVirtual.Float(1f, 0f, duration, volume =>
			{
				reference.setParameterByName("fade_volume", volume);
			}).OnComplete(() =>
			{
				reference.stop(STOP_MODE.IMMEDIATE);
			});
		}

		public static void FadeIn(this BoolCache<EventInstance> reference, bool startFromZero = true, float duration = 1f)
		{
			reference.Value.FadeIn(startFromZero, duration);
		}

		public static void FadeOut(this BoolCache<EventInstance> reference, float duration = 1f)
		{
			if (!reference.HasValue) return;
			if (!reference.Value.IsPlaying()) return;
			reference.Value.FadeOut(duration);
		}

		public static bool IsPlaying(this EventInstance reference)
		{
			if (reference.getPlaybackState(out var menuState) == RESULT.OK && menuState == PLAYBACK_STATE.PLAYING)
				return true;
			
			return false;
		}
		
		public static bool IsPlaying(this BoolCache<EventInstance> reference)
		{
			if (!reference.HasValue) return false;

			return reference.Value.IsPlaying();
		}

		public static string GetEventName(this EventReference reference)
		{
			string eventName = reference.Path.Substring(reference.Path.LastIndexOf('/') + 1);
			return eventName;
		}
		public static string GetEventName(this EventInstance reference)
		{
			if (reference.getDescription(out EventDescription description) == RESULT.OK && description.getPath(out string path) == RESULT.OK)
			{
				return path.Substring(path.LastIndexOf('/') + 1);
			}else
			{
				return "ERROR, COULDN'T GET NAME";
			}
		}
	}
}