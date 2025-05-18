using Networking.Widgets.Core.Base;
using Networking.Widgets.Session.Widgets.Base;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Networking.Widgets.Session.Widgets
{
    internal class CreateSession : EnterSessionBase
    {
        TMP_InputField m_InputField;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnEnable()
        {
            m_InputField = GetComponentInChildren<TMP_InputField>();
            base.OnEnable();
        }

        public override void OnServicesInitialized()
        {
            m_InputField.onEndEdit.AddListener(value =>
            {
                if (Input.GetKeyDown(KeyCode.Return) && !string.IsNullOrEmpty(value))
                {
                    EnterSession();
                }
            });
            m_InputField.onValueChanged.AddListener(value =>
            {
                m_EnterSessionButton.interactable = !string.IsNullOrEmpty(value) && Session == null;
            });
        }

        protected override EnterSessionData GetSessionData()
        {
            return new EnterSessionData
            {
                SessionAction = SessionAction.Create,
                SessionName = m_InputField.text,
                WidgetConfiguration = WidgetConfiguration,
            };
        }
    }
}
