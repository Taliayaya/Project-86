using Networking.Widgets.Core.Base.ChatService.Interfaces;
using TMPro;
using Unity.Multiplayer.Widgets;
using UnityEngine;

namespace Networking.Widgets.Vivox.Widgets.Text_Chat
{
    internal class TextMessage : MonoBehaviour
    {
        [SerializeField]
        TMP_Text m_Text;
        
        public void Init(IChatMessage message, string displayName)
        {
            m_Text.text = $"[{displayName}]: {message.Text}";
            m_Text.alignment = message.FromSelf ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
        }
    }    
}