using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefaultNamespace
{
    public class SceneHandler : Singleton<SceneHandler>
    {
        public static void ReloadScene()
        {
            WindowManager.CloseAll();
            EventManager.TriggerEvent("OnResume");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}