
public static class Constants
{
    /// <summary>
    /// All the untyped event names
    /// </summary>
    /// <remarks>
    /// All these event names were pulled straight from the list generated, so I can not comment on all these
    /// </remarks>
    public static class Events
    {
        public const string OnPause = "OnPause";
        public const string OnResume = "OnResume";
        public const string OnPrimaryFire = "OnPrimaryFire";
        public const string OnSecondaryFire = "OnSecondaryFire";
        public const string OnCallScavenger = "OnCallScavenger";
        public const string OnOrderScavenger = "OnOrderScavenger";
        public const string OnOrderSubmitScavenger = "OnOrderSubmitScavenger";
        public const string OnStopScavenger = "OnStopScavenger";
        public const string OnReload = "OnReload";
        /// <summary>
        /// FIXME: This event seems to only be called in their typed counterpart
        /// </summary>
        public const string OnDeath = "OnDeath";
        /// <summary>
        /// FIXME: This event seems to only be called in their typed counterpart
        /// </summary>
        public const string OnRespawn = "OnRespawn";

        public const string OnAskDataCollectionAgreement = "OnAskDataCollectionAgreement";
        public const string OnDataCollectionAgreement = "OnDataCollectionAgreement";

        public static class Inputs
        {
            public const string OnChangeView = "OnChangeView";
        }
        
        public static class Analytics
        {
            public const string LevelFinished = "AnalyticsLevelFinished";
            public const string QuestCompleted = "AnalyticsQuestCompleted";
            
        }
    }
    /// <summary>
    /// All the typed event names
    /// </summary>
    /// <remarks>
    /// All these event names were pulled straight from the list generated, so I can not comment on all these
    /// </remarks>
    public static class TypedEvents
    {
        public static class Inputs
        {
            public const string OnFreeLook = "OnFreeLook";
            public const string OnLookAround = "OnLookAround";
            public const string OnToggleMap = "OnToggleMap";
            public const string OnToggleObjective = "OnToggleObjective";

            // Fox's events, unorganised

            public const string OnJump = "OnJump";
        }

        public const string OnDash = "OnDash";
        public const string OnChangedPersonalMark = "OnChangedPersonalMark";

        public const string OnSceneLoadingCompleted = "OnSceneLoadingCompleted";

        // /!\ this is used as NAME:keyName
        public const string RebindKey = "RebindKey:";
        
        
        public const string OnToggleCockpitView = "OnToggleCockpitView";
        // TODO : Add all the typed event names cause i couldnt be bothered -nemo

    }
    
    public static class Integrations
    {
        public static class Discord
        {
            public const long AppID = 2 * 0b1011 * 0x1F * 0b101111101110001100001100000110001111111101110010111;
            public const global::Discord.ActivityType ActivityType = 0;
        }
    }
}