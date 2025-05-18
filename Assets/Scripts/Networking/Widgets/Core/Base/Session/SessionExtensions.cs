using Networking.Widgets.Core.Base.Session;
using Unity.Services.Multiplayer;

namespace Unity.Multiplayer.Widgets
{
    internal static class SessionExtensions
    {
        internal static string GetPlayerName(this ISession session, string playerId)
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
    }
}
