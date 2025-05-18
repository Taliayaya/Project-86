using System;

namespace Networking.Widgets.Core.Base.ChatService.Interfaces
{
    internal interface IChatParticipant
    {
        /// <summary>
        /// Called when the MuteState of the Player changes.
        /// </summary>
        event Action OnMuteStateChanged;

        /// <summary>
        /// Called when the speech is detected from the Player.
        /// </summary>
        event Action OnSpeechDetected;

        /// <summary>
        /// Called when the audio energy of the Player changes.
        /// </summary>
        event Action OnAudioEnergyChanged;
        
        /// <summary>
        /// The ID of the Player.
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// The ID of the Chat the Player is assigned to.
        /// </summary>
        string ChatId { get; }
        
        /// <summary>
        /// Mute state of the Player.
        /// </summary>
        bool IsMuted { get; }
        
        /// <summary>
        /// The audio energy of the Player.
        /// </summary>
        double AudioEnergy { get; }
        
        /// <summary>
        /// Whether speech is detected from the Player.
        /// </summary>
        bool SpeechDetected { get; }
        
        /// <summary>
        /// Mute the player for the local player.
        ///
        /// Calls <see cref="OnMuteStateChanged"/> when the mute state changes.
        /// </summary>
        /// <param name="mute">Should be muted.</param>
        public void MuteLocally(bool mute);
    }
}
