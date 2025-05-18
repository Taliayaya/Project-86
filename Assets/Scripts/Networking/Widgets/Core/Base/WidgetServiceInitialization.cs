using System.Threading.Tasks;
using Networking.Widgets.Core.Base.Widget;
using Unity.Multiplayer.Widgets;
using Unity.Services.Core;
using UnityEngine;

namespace Networking.Widgets.Core.Base
{
    internal class WidgetServiceInitializationInternal : IServiceInitialization
    {
        /// <summary>
        /// UnityServices, AuthenticationService and VivoxService (if installed) are initialized.
        ///
        /// Register an event to SessionEventDispatcher.Instance.OnInitializationDone to be notified when the initialization is done.
        /// </summary>
        public async Task InitializeAsync()
        {
            var widgetSettings = Resources.Load<global::Networking.Widgets.Core.Settings.MultiplayerWidgetsSettings>(nameof(global::Networking.Widgets.Core.Settings.MultiplayerWidgetsSettings));

            if (widgetSettings != null && widgetSettings.UseCustomServiceInitialization)
                return;

            var widgetDependencies = WidgetDependencies.Instance;
            
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                Debug.Log("Initialized Unity Services");
            }

            //if (!widgetDependencies.AuthenticationService.IsSignedIn)
            //{
            //    await widgetDependencies.AuthenticationService.SignInAnonymouslyAsync();
            //    var name = await widgetDependencies.AuthenticationService.GetPlayerNameAsync();
            //    Debug.Log($"Signed in anonymously. Name: {name}. ID: {widgetDependencies.AuthenticationService.PlayerId}");
            //}

            var chatService = WidgetDependencies.Instance.ChatService;
            if (chatService != null)
                await chatService.InitializeAsync();

            WidgetServiceInitialization.ServicesInitialized();
        }
    }
    
    /// <summary>
    /// Initialization class for all services.
    ///
    /// By default, UnityServices are initialized and an anonymous sign in is done. Vivox is also initialized when installed.
    ///
    /// To handle initialization manually enable the <see cref="Networking.Widgets.Core.Settings.MultiplayerWidgetsSettings.UseCustomServiceInitialization"/> setting in
    /// the project settings.
    ///
    /// Call <see cref="WidgetServiceInitialization.ServicesInitialized"/> when initialization is done.
    /// </summary>
    public static class WidgetServiceInitialization
    {
        // Reset IsInitialized to support domain reload disabled. 
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            IsInitialized = false;
        }
        
        /// <summary>
        /// True if UnityServices, AuthenticationService and VivoxService (if installed) are initialized.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// Called after all services are initialized.
        /// </summary>
        public static void ServicesInitialized()
        {
            Debug.Log("Services Initialized");
            IsInitialized = true;
            WidgetEventDispatcher.Instance.OnServicesInitialized();
        }
    }
}
