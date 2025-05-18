using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace Networking.Widgets.Core.Base.Session
{
    /// <summary>
    /// Base Class to write a custom NetworkHandler for Multiplayer Services Package.
    /// </summary>
    public abstract class CustomWidgetsNetworkHandler : ScriptableObject, INetworkHandler
    {
        /// <summary>
        /// Called when connection is created.
        ///
        /// Can be used to do Setup Steps during Starting the connection.
        /// </summary>
        /// <param name="configuration">The Network Configuration</param>
        /// <returns>Task</returns>
        public abstract Task StartAsync(NetworkConfiguration configuration);

        /// <summary>
        /// Called when connection is terminated.
        ///
        /// Can be used for Tear Down Steps during stopping the connection.
        /// </summary>
        /// <returns>Task</returns>
        public abstract Task StopAsync();
    }
}
