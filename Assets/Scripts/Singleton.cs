using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

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

    [NonSerialized] [CanBeNull] protected static T _instance;

    [NotNull] protected static readonly object Lock = new();
    
    [Tooltip("Is it in DontDestroyOnLoad?")]
    [SerializeField] private bool persistent = true;

    #endregion
    
    #region  Properties
    
    protected static bool AllowAutoCreation => true;

    public static bool HasInstance => _instance != null;
    
    public static T Instance
    {
        get
        {
            // lock for thread safety
            lock (Lock)
            {
                if (_instance)
                    return _instance;

                if (!AllowAutoCreation)
                    return null;
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
        if (_instance != null)
        {
            Debug.LogWarning($"[{nameof(Singleton)}<{typeof(T)}>] Attempted to create a {nameof(Singleton)} of" +
                             $" type {typeof(T)} when one already existed, destroying");
            Destroy(this);
            return;
        }
        _instance = this as T;
        if (persistent)
            DontDestroyOnLoad(_instance!.gameObject);
        if (!Quitting)
            OnAwake();
    }

    /// <summary>
    /// OnAwake is called when the script instance is being loaded.
    /// It is the basic Awake of Unity
    /// </summary>
    protected virtual void OnAwake() { }

    protected void OnDestroy()
    {
        if (_instance == this)
        {
            Debug.Log($"[{nameof(Singleton)}<{typeof(T)}>] Instance was destroyed, setting _instance to null");
            _instance = null;
        }
    }

    #endregion
}

public abstract class Singleton : MonoBehaviour
{
    public bool Quitting { get; private set; }
    #region  Methods
    private void OnApplicationQuit()
    {
        OnApplicationQuitting();
    }

    protected virtual void OnApplicationQuitting()
    {
        Quitting = true;
    }

    #endregion
}