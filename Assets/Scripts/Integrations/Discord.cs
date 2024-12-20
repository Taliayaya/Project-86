using System;
using UnityEngine;
#if ENABLE_INTEGRATION_DISCORD
using System.Collections;
using Discord;
using Managers;
#endif

namespace Integrations
{
    public class Discord : IDisposable
    {
#if ENABLE_INTEGRATION_DISCORD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap() => Discord.Start();

        internal static Discord Instance;
        
        private readonly global::Discord.Discord _discord;
        private readonly ActivityManager _activityManager;
        private readonly long _appLaunchTime;

        private readonly Task updater;
        
        protected static class GameState
        {
            public static bool isPaused;
            public static bool isDead;
        }
        
#endif

        public static void Start()
        {
#if ENABLE_INTEGRATION_DISCORD
            Instance = new Discord();
#endif
        }

        /// <summary>
        /// Creates the link to Discord RPC
        /// </summary>
        /// <param name="autoStart">Whether to start throwing calls to Discord immediately</param>
        /// <exception cref="ApplicationException">If there is already a discord RPC link</exception>
        private Discord(bool autoStart = true)
        {
#if ENABLE_INTEGRATION_DISCORD
            if (Instance is not null)
                throw new ApplicationException();

            _discord = new global::Discord.Discord(Constants.Integrations.Discord.AppID, (ulong)CreateFlags.NoRequireDiscord);
            _activityManager = _discord.GetActivityManager();
            
            Application.wantsToQuit += OnApplicationQuit;

            RegisterEvents();

            _appLaunchTime = DateTimeOffset.Now.ToUnixTimeSeconds();
            
            updater = new Task(UpdateIntegration(), autoStart);
#else
            Debug.LogError("Attempted to start Discord RPC integration when it has not been enabled at build");
#endif
        }

#if ENABLE_INTEGRATION_DISCORD
        private bool OnApplicationQuit()
        {
            this.Dispose();
            return true;
        }

        private IEnumerator UpdateIntegration()
        {
            while (Application.isPlaying)
            {
                yield return new WaitForSecondsRealtime(.25f);
                _activityManager.UpdateActivity(new Activity
                {
                    Type = Constants.Integrations.Discord.ActivityType,
                    ApplicationId = Constants.Integrations.Discord.AppID,
                    Name = "Project 86",
                    State = GetCurrentState(),
                    Details = GetCurrentDetails(),
                    Timestamps = GetTimestamps(),
                    Assets = GetAssets(),
                    Party = GetParty(),
                    Secrets = GetSecrets(),
                    Instance = false // TODO
                }, result => {
                    if (result != Result.Ok)
                    {
                        Debug.LogError($"Discord returned result {result}, Stopping Integration");
                        Dispose();
                    }
                });
                _discord.RunCallbacks();
            }
        }
#endif
        
        public void Dispose()
        {
#if ENABLE_INTEGRATION_DISCORD
            if (Instance != this)
                throw new ApplicationException();
            Instance = null;
            _activityManager.ClearActivity(_ => { });
            updater.Stop();
            _discord?.Dispose();
#endif
        }

        #region Value Getters

#if ENABLE_INTEGRATION_DISCORD
        /// <summary>
        /// Returns the "Secrets" or login codes to join another user's server directly
        /// </summary>
        /// <remarks>See https://bit.ly/47SxszQ for example</remarks>
        /// <returns>ActivitySecrets the secrets</returns>
        private ActivitySecrets GetSecrets()
        {
            // TODO
            return new ActivitySecrets();
        }

        /// <summary>
        /// Gets the status of the current multiplayer party/session
        /// </summary>
        /// <remarks>See https://bit.ly/47SxszQ for example</remarks>
        /// <returns>ActivityParty the details about the session</returns>
        private ActivityParty GetParty()
        {
            // TODO: Actually get current multiplayer party
            return new ActivityParty
            {
                Id = "",
                Size = default
            };
        }

        /// <summary>
        /// Gets the timestamps for how long the user has been playing / how long is left on a match...
        /// </summary>
        /// <remarks>See https://bit.ly/47SxszQ for example</remarks>
        /// <returns>ActivityTimestamps the details about playtime</returns>
        private ActivityTimestamps GetTimestamps()
        {
            // TODO: see if we want to do wacky things here
            // TODO-QUESTION: are matches gonna be timed? if so, do we want to display "XX:XX left" in discord?
            return new ActivityTimestamps { Start = _appLaunchTime };
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <remarks>See https://bit.ly/47SxszQ for example</remarks>
        /// <returns></returns>
        private ActivityAssets GetAssets()
        {
            // TODO: Get proper art on discord
            return new ActivityAssets
            {
                LargeImage = null,
                LargeText = "LargeTextTemp",
                SmallImage = null,
                SmallText = "SmallTestTemp"
            };
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <remarks>See https://bit.ly/47SxszQ for example</remarks>
        /// <returns></returns>
        private string GetCurrentDetails()
        {
            // TODO
            return GameState.isPaused ? "Navigating the pause menu" : (GameState.isDead ? "Died" : "Playing with mechs");
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <remarks>See https://bit.ly/47SxszQ for example</remarks>
        /// <returns></returns>
        private string GetCurrentState()
        {
            // TODO
            return "state not yet implemented, likely in singleplayer";
        }
#endif
        
        #endregion

        #region Events
        
#if ENABLE_INTEGRATION_DISCORD
        private void RegisterEvents()
        {
            EventManager.AddListener(Constants.Events.OnPause, () => GameState.isPaused = true);
            EventManager.AddListener(Constants.Events.OnResume, () => GameState.isPaused = false);
            EventManager.AddListener(Constants.Events.OnDeath, _ => GameState.isDead = true);
            EventManager.AddListener(Constants.Events.OnRespawn, _ => GameState.isDead = false);
        }
#endif
        
        #endregion
    }
}
