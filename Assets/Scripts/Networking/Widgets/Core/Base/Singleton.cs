using UnityEngine;
using Object = UnityEngine.Object;

namespace Networking.Widgets.Core.Base
{
    /// <summary>
    /// Lazy singleton pattern.
    /// </summary>
    /// <typeparam name="T">MonoBehaviour that will be added to an instantiated GameObject.</typeparam>
    internal abstract class LazySingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        // We don't have to reset this static field to make it work with disabled DomainReload, as we check the gameObject in the Instance property.
        static T s_Instance;
        
        public static T Instance
        {
            get
            {
                if (s_Instance == (Object)null || s_Instance.gameObject == null)
                    CreateInstance();
                return s_Instance;
            }
        }
        
        static void CreateInstance()
        {
            var gameObject = new GameObject($"{typeof(T).Name}");
            s_Instance = gameObject.AddComponent<T>();
            DontDestroyOnLoad(gameObject);
        }
    }
}


