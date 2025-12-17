using System.Threading.Tasks;
using Networking.Widgets.Core.Base.ChatService.Interfaces;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Networking.Widgets.Core.Base
{
    internal class WidgetDependencies
    {
        /// <summary>
        /// Reset when domain reload is disabled.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            s_Instance = null;
        }
        
        static WidgetDependencies s_Instance;
        
        /// <summary>
        /// Access the WidgetDependencies instance.
        /// </summary>
        public static WidgetDependencies Instance => s_Instance ??= new WidgetDependencies();
        
        IAuthenticationService m_AuthenticationService;
        
        /// <summary>
        /// Get the AuthenticationService.
        ///
        /// Defaults to the Unity.Services.Authentication.AuthenticationService.Instance.
        /// </summary>
        public IAuthenticationService AuthenticationService
        {
            get { return m_AuthenticationService ??= Unity.Services.Authentication.AuthenticationService.Instance; }
            set => m_AuthenticationService = value;
        }
        
        IServiceInitialization m_ServiceInitialization;
        public IServiceInitialization ServiceInitialization
        {
            get { return m_ServiceInitialization ??= new WidgetServiceInitializationInternal(); }
            set => m_ServiceInitialization = value;
        }

        IMultiplayerService m_MultiplayerService;
        
        /// <summary>
        /// Get the MultiplayerService.
        ///
        /// Defaults to the Unity.Services.Multiplayer.MultiplayerService.Instance.
        /// </summary>
        public IMultiplayerService MultiplayerService
        {
            get { return m_MultiplayerService ??= Unity.Services.Multiplayer.MultiplayerService.Instance; }
            set => m_MultiplayerService = value;
        }

        IChatService m_ChatService;
        
        /// <summary>
        /// Get the ChatService.
        ///
        /// If the ChatService is not set, it will return null.
        ///
        /// The ChatService will only be set automatically when the Vivox package is available.
        /// </summary>
        public IChatService ChatService
        {
#if VIVOX_AVAILABLE
            get {return m_ChatService ??= new VivoxChatService();}
#else
            get { return null; }
#endif
            set => m_ChatService = value;
        }
    }
    
    /// <summary>
    /// Used to initialize the services.
    /// </summary>
    internal interface IServiceInitialization
    {
        /// <summary>
        /// The following services are expected to be initialized by the implementation:
        /// - UnityServices
        /// - AuthenticationService
        /// - (required when Vivox package is installed) VivoxService
        ///
        /// After all services are initialized the <see cref="WidgetServiceInitialization.ServicesInitialized"/> method needs to be called.
        /// </summary>
        Task InitializeAsync();
    }
}
