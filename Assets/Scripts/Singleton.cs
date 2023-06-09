using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    /// <summary>
    /// A Base Class that allows to use the Singleton Pattern.
    ///
    /// What is a Singleton? A Singleton is a class that can only have one instance.
    /// It is accessible from anywhere in the code, with the static Instance property.
    /// </summary>
    /// <remarks>Avoid using the singleton class pattern at all cost as it creates dependencies in the code. There should only be a few at most in the project.</remarks>
    /// <typeparam name="T">The type of the class that inherits from it</typeparam>
    public abstract class Singleton<T> : Singleton where T : MonoBehaviour
    {
        #region Fields

        [CanBeNull] private static T _instance;

        [NotNull] private static readonly object Lock = new();
        
        [Tooltip("Is it in DontDestroyOnLoad?")]
        [SerializeField] private bool persistent = true;

        #endregion
        
        #region  Properties

        public static bool HasInstance => _instance;
        
        public static T Instance
        {
            get
            {
                if (Quitting)
                {
                    Debug.LogWarning($"[{nameof(Singleton)}<{typeof(T)}>] Instance will not be returned because the application is quitting.");
                    // ReSharper disable once AssignNullToNotNullAttribute
                    return null;
                }
                // lock for thread safety
                lock (Lock)
                {
                    if (_instance)
                        return _instance;
                    var instances = FindObjectsOfType<T>();
                    var count = instances.Length;
                    if (count > 0)
                    {
                        if (count == 1)
                            return _instance = instances[0];
                        Debug.LogWarning($"[{nameof(Singleton)}<{typeof(T)}>] There should never be more than one {nameof(Singleton)} of type {typeof(T)} in the scene, but {count} were found. The first instance found will be used, and all others will be destroyed.");
                        for (var i = 1; i < instances.Length; i++)
                            Destroy(instances[i]);
                        return _instance = instances[0];
                    }

                    Debug.Log($"[{nameof(Singleton)}<{typeof(T)}>] An instance is needed in the scene and no existing instances were found, so a new instance will be created.");
                    return _instance = new GameObject($"({nameof(Singleton)}){typeof(T)}")
                        .AddComponent<T>();
                }
            }
        }
        #endregion

        #region  Methods
        
        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// It set the persistence of the object and call the OnAwake method.
        /// Consider using the <see cref="OnAwake"/> method instead.
        /// </summary>
        private void Awake()
        {
            if (persistent)
                DontDestroyOnLoad(gameObject);
            OnAwake();
        }

        /// <summary>
        /// OnAwake is called when the script instance is being loaded.
        /// It is the basic Awake of Unity
        /// </summary>
        protected virtual void OnAwake() { }
        #endregion
    }
}

public abstract class Singleton : MonoBehaviour
{
    #region  Properties
    public static bool Quitting { get; private set; }
    #endregion

    #region  Methods
    private void OnApplicationQuit()
    {
        Quitting = true;
        OnApplicationQuitting();
    }

    protected virtual void OnApplicationQuitting()
    {
        
    }
    #endregion
}