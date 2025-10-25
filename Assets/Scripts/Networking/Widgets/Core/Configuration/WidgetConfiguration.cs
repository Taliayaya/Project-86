using UnityEngine;
using UnityEngine.Serialization;

namespace Networking.Widgets.Core.Configuration
{
    /// <summary>
    /// ConnectionType is the type of connection to use when creating a session.
    /// </summary>
    public enum ConnectionType
    {
        /// <summary>
        /// Session only, no netcode.
        /// </summary>
        None,
        /// <summary>
        /// A direct connection that requires the IP address and port. Also requires either Netcode for GameObjects or Netcode for Entities.
        /// </summary>
        Direct,
        /// <summary>
        /// A Relay connection via Unity Multiplayer Services. Requires either Netcode for GameObjects or Netcode for Entities.
        /// </summary>
        Relay,
#if true || NGO_2_AVAILABLE            
        /// <summary>
        /// A connection via Distributed Authority that requires Netcode for GameObjects 2.0.0 or higher and Unity Multiplayer Services.
        /// </summary>
        DistributedAuthority,
#endif
        /// <summary>
        /// A hidden type that is used as a fallback.
        /// </summary>
        [InspectorName(null)] // Hide the value in the inspector. It is a fallback when NGO is downgraded by the user.
        Unsupported
    }

    /// <summary>
    /// ConnectionMode is used to differentiate between a local (Listen) or public (Publish) connection when direct
    /// connection as the <see cref="ConnectionType"/> is used.
    /// </summary>
    public enum ConnectionMode
    {
        /// <summary>
        /// Local connection. This is helpful for testing on the same machine or a local network.
        ///
        /// ListenIp and PublishIp are the same. They default to "127.0.0.1".
        /// </summary>
        Listen,
        
        /// <summary>
        /// Specify the listenIp and publishIp.
        ///
        /// To listen on all interfaces, use 0.0.0.0 as the listenIp and specify the external/public IP address
        /// that clients should sue as the publishIp.
        /// </summary>
        Publish
    }
    
    /// <summary>
    /// The WidgetConfiguration is a ScriptableObject that contains the configuration for a session.
    /// </summary>
    [CreateAssetMenu(fileName = "WidgetConfiguration", menuName = "Networks/Widgets/WidgetConfiguration")]
    public class WidgetConfiguration : ScriptableObject
    {
        /// <summary>
        /// The name of the session to create or join.
        /// </summary>
        public string Name;
        
        /// <summary>
        /// The type of connection to use when creating or joining a session.
        /// </summary>
        [FormerlySerializedAs("connectionType")]
        [Header("Multiplayer Services Settings")]
        [Header("Connection Settings")]
        public ConnectionType ConnectionType = ConnectionType.Relay;
        
        /// <summary>
        /// Choose between a local (Listen) or public (Publish) connection.
        /// </summary>
        [FormerlySerializedAs("connectionMode")]
        [Header("Direct Connection Settings")]
        public ConnectionMode ConnectionMode = ConnectionMode.Listen;
        
        /// <summary>
        /// Listen for incoming connection at this address.
        /// </summary>
        [FormerlySerializedAs("IpAddress")]
        [Tooltip("Listen for incoming connection at this address. This is the local IP address that the host should use. " +
            "To listen on all interfaces Use 0.0.0.0 when using ConnectionMode.Publish. " +
            "For a Listen connection the default allows for local testing.")]
        public string ListenIpAddress = "127.0.0.1";
        
        /// <summary>
        /// Address that clients should use when connecting.
        /// </summary>
        [Tooltip("Address that clients should use when connecting. This is the external/public IP address that clients " +
            "should use as the publish IP.")]
        public string PublishIpAddress = "127.0.0.1";
        
        /// <summary>
        /// The port number defaults to 0 which selects a randomly available port on the machine and uses the chosen
        /// value as the publish port. If a non-zero value is used, the port number applies to both listen and publish
        /// addresses.
        /// </summary>
        [Tooltip("0 selects a randomly available port on the machine and uses the chosen value as the publish port. " +
            "If a non-zero value is used, the port number applies to both listen and publish addresses.")]
        public int Port = 0;
        
        /// <summary>
        /// Custom NetworkHandler that is used during Session Creation.
        ///
        /// Can be null. If null, the default NetworkHandler from the Multiplayer Service Package is used.
        /// </summary>
        [Tooltip("Custom NetworkHandler that is used during Session Creation. Can be null. If null, the default NetworkHandler from the Multiplayer Service Package is used.")]
        public global::Networking.Widgets.Core.Base.Session.CustomWidgetsNetworkHandler NetworkHandler;
        
        /// <summary>
        /// The maximum number of players that can join the session.
        /// </summary>
        [Header("Session Settings")]
        public int MaxPlayers = 4;
        
        /// <summary>
        /// A flag to determine if the player should join the voice channel when joining the session.
        /// </summary>
        [FormerlySerializedAs("JoinVoiceChannel")]
        [Header("Voice Settings")]
        [Tooltip("Automatically join a voice channel for a session when joining the session.")]
        public bool EnableVoiceChat;
        
        void OnValidate()
        {
            var changed = false;
            
#if !true && !NGO_2_AVAILABLE && !NETCODE_FOR_ENTITIES_AVAILABLE 
            if (ConnectionType == ConnectionType.Relay || ConnectionType == ConnectionType.Direct)
            {
                Debug.LogWarning($"{this.name} (WidgetConfiguration) uses the connection type {ConnectionType} but the necessary packages are not installed." +
                    $"Either install Netcode for Gameobjects or Netcode for Entities to use Relay or Direct connection. Resetting the connection type to {ConnectionType.None}.");
                ConnectionType = ConnectionType.None;

                changed = true;
            }
#endif
            
            if (ConnectionType == ConnectionType.Unsupported)
            {
#if true || NGO_2_AVAILABLE
                ConnectionType = ConnectionType.DistributedAuthority;
#else
                ConnectionType = ConnectionType.Relay;
#endif
                changed = true;
            }
            
            if (changed)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
            }
        }
    }
}
