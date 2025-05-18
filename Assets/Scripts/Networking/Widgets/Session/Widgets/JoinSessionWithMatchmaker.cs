using Networking.Widgets.Core.Base;
using Networking.Widgets.Session.Widgets.Base;
using Unity.Multiplayer.Widgets;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Widgets.Session.Widgets
{
    [RequireComponent(typeof(Button))]
    internal class JoinSessionWithMatchmaker : EnterSessionBase
    {
        [Header("Matchmaker Options")]
        [Tooltip("The user will initiate the Matchmaking in this Queue.")]
        [SerializeField]
        string m_QueueName = "default";

        protected override EnterSessionData GetSessionData()
        {
            return new EnterSessionData
            {
                SessionAction = SessionAction.StartMatchmaking,
                WidgetConfiguration = WidgetConfiguration,
                AdditionalOptions = new AdditionalOptions
                {
                    MatchmakerOptions = new MatchmakerOptions
                    {
                        QueueName = m_QueueName
                    }
                }
            };
        }
    }
}
