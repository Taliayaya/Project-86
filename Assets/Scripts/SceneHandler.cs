using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefaultNamespace
{
    public class SceneHandler : Singleton<SceneHandler>
    {
        public static void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            EventManager.TriggerEvent("OnResume");
        }
    }
}