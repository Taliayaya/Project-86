using Unity.Multiplayer.Widgets;
using Unity.Services.Multiplayer;
using WidgetConfiguration = Networking.Widgets.Core.Configuration.WidgetConfiguration;

namespace Networking.Widgets.Core.Base
{
    /// <summary>
    /// The different types of SessionActions.
    /// </summary>
    internal enum SessionAction
    {
        Invalid,
        Create,
        StartMatchmaking,
        QuickJoin,
        JoinByCode,
        JoinById
    }
    
    /// <summary>
    /// Data to enter a session.
    /// </summary>
    internal struct EnterSessionData
    {
        public SessionAction SessionAction;
        public string SessionName;
        public string JoinCode;
        public string Id;
        public WidgetConfiguration WidgetConfiguration;
        public AdditionalOptions AdditionalOptions;
    }

    /// <summary>
    /// Additional data to enter specific session types.
    /// </summary>
    internal struct AdditionalOptions
    {
        public MatchmakerOptions MatchmakerOptions;
        public bool AutoCreateSession;
    }
}


