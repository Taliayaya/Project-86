using UnityEngine;

namespace Networking.Widgets.Core.Settings
{
    /// <summary>
    /// Settings for the Multiplayer Widgets Package
    /// </summary>
    public class MultiplayerWidgetsSettings : ScriptableObject
    {
        internal const string k_CustomserviceInitializationTooltip = "Enable if service initialization is handled manually.\n\n" +
            "Requires initialization of UnityServices and Vivox (if used) and a signed-in user via Authentication.\n\n" +
            "Call WidgetServiceInitialization.ServicesInitialized after initialization is finished.";
        
        [SerializeField]
        [Tooltip(k_CustomserviceInitializationTooltip)]
        bool m_UseCustomServiceInitialization;
        
        internal bool UseCustomServiceInitialization
        {
            get => m_UseCustomServiceInitialization;
            set => m_UseCustomServiceInitialization = value;
        }
    }
}
