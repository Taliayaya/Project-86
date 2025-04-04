using System.Text;
using TMPro;
using UnityEngine;

namespace Networking.Communication.ChatService
{
    public class ChatMessageUI : MonoBehaviour
    {
        public void Init(ChatMessage message)
        {
            StringBuilder textMessage = new StringBuilder();
            switch (message.type)
            {
                case ChatMessageType.System:
                    textMessage.Append("<color=red><System>: ")
                        .Append(message.message).Append("</color>");
                    break;
                case ChatMessageType.Player:
                    textMessage.Append("<color=green>")
                        .Append(message.sender).Append(": ")
                        .Append(message.message).Append("</color>");
                    
                    break;
                default:
                    break;
            }

            GetComponentInChildren<TMP_Text>().text = textMessage.ToString();
        }
    }
}