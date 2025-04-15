using Networking.Widgets.Core.Base;
using Networking.Widgets.Session.Widgets.Base;
using Unity.Multiplayer.Widgets;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace Networking.Widgets.Session.Widgets
{
    [RequireComponent(typeof(Button))]
    internal class QuickJoinSession : EnterSessionBase
    {
        [Header("Quick Join Options")]
        [Tooltip("If true, the widget will automatically create a session if one does not exist. If false, the widget will only attempt to join existing sessions.")]
        [SerializeField]
        bool m_AutoCreateSession = true;
        
        protected override EnterSessionData GetSessionData()
        {
            return new EnterSessionData
            {
                SessionAction = SessionAction.QuickJoin,
                WidgetConfiguration = WidgetConfiguration,
                AdditionalOptions = new AdditionalOptions
                {
                    AutoCreateSession = m_AutoCreateSession
                }
            };
        }
    }
}
