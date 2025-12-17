namespace SoundManagement
{
	public static class SoundEventName
	{
		public const string OnDinosauriaAimed = "OnDinosauriaAimed";
		public const string OnDinosauriaDead = "OnDinosauriaDead";
		
		public static string OnPause => Constants.Events.OnPause;
		public static string OnResume => Constants.Events.OnResume;
		public const string MusicVolumeChanged = "UpdateGameParameter:musicVolume";
		public const string QuestChanged = "QuestChanged";
	}
}