#if VIVOX_AVAILABLE
using System;
using Unity.Services.Vivox;

namespace Unity.Multiplayer.Widgets
{
    internal class VivoxChatMessage : IChatMessage
    {
        VivoxMessage m_VivoxMessage;
        
        public VivoxChatMessage(VivoxMessage vivoxTextMessage)
        {
            m_VivoxMessage = vivoxTextMessage;
        }

        public string SenderPlayerId => m_VivoxMessage.SenderPlayerId;
        public string SenderDisplayName => m_VivoxMessage.SenderDisplayName;
        public string ChannelName => m_VivoxMessage.ChannelName;
        public string Text => m_VivoxMessage.MessageText;
        public bool FromSelf => m_VivoxMessage.FromSelf;
        public DateTime ReceivedTime => m_VivoxMessage.ReceivedTime;
        public string Id => m_VivoxMessage.MessageId;
    }
}
#endif