using UnityEngine;
namespace Armament.Shared
{
	public class ArmamentConfigManager : Singleton<ArmamentConfigManager>
	{
		private ArmamentConfig _config;

		private bool _inited = false;


		public static ArmamentConfig GetConfig()
		{
			if (!Instance._inited) Instance.Init();
			return Instance._config;
		}
		
		private void Init()
		{
			if (_inited) return;
			Load();
		}

		private void Load()
		{
			_config = Resources.Load<ArmamentConfig>("ScriptableObjects/Armament/ArmamentConfig");
			_config.LoadFromFile();
		}

		public void Save()
		{
			_config.SaveToFile();
		}
	}
}