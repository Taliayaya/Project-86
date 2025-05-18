using System;

namespace Networking.Widgets.Core.Base.ChatService.Interfaces
{
    internal interface IChatMessage
    {
        /// <summary>
        /// The PlayerId of the sender of the message.
        /// </summary>
        public string SenderPlayerId { get; }

        /// <summary>
        /// The DisplayName of the sender of the message.
        /// </summary>
        public string SenderDisplayName { get; }

        /// <summary>
        /// The ChannelName of the channel the message was sent in.
        /// </summary>
        public string ChannelName { get; }

        /// <summary>
        /// The text body of the message that was sent.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Whether or not the message was sent from the user to the channel.
        /// </summary>
        public bool FromSelf { get; }

        /// <summary>
        /// At what time the message was received.
        /// </summary>
        public DateTime ReceivedTime { get; }

        /// <summary>
        /// Unique message id of the text message.
        /// </summary>
        public string Id { get; }

    }
}
