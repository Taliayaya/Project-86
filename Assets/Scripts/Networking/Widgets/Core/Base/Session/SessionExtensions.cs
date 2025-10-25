using Networking.Widgets.Core.Base.Session;
using Unity.Services.Multiplayer;

namespace Unity.Multiplayer.Widgets
{
    public static class SessionExtensions
    {
        public static string GetPlayerName(this ISession session, string playerId)
        {
            foreach (var player in session.Players)
            {
                if (player.Id == playerId)
                {
                    return player.Properties[SessionConstants.playerNamePropertyKey].Value;
                }
            }

            return null;
        }
        
        public static IReadOnlyPlayer GetPlayer(this ISession session, string playerId)
        {
            foreach (var player in session.Players)
            {
                if (player.Id == playerId)
                {
                    return player;
                }
            }

            return null;
        }
    }
}
