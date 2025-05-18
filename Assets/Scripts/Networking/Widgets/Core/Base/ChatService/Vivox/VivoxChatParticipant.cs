#if VIVOX_AVAILABLE
using System;
using Unity.Services.Vivox;

namespace Unity.Multiplayer.Widgets
{
    internal class VivoxChatParticipant : IChatParticipant
    {
        VivoxParticipant m_Participant;
        
        internal VivoxChatParticipant(VivoxParticipant vivoxParticipant)
        {
            m_Participant = vivoxParticipant;
            m_Participant.ParticipantMuteStateChanged += () =>
            {
                OnMuteStateChanged?.Invoke();
            };
            
            m_Participant.ParticipantSpeechDetected += () =>
            {
                OnSpeechDetected?.Invoke();
            };
            
            m_Participant.ParticipantAudioEnergyChanged += () =>
            {
                OnAudioEnergyChanged?.Invoke();
            };
        }

        public event Action OnMuteStateChanged;
        public event Action OnSpeechDetected;
        public event Action OnAudioEnergyChanged;
        public string Id => m_Participant.PlayerId;

        public string ChatId => m_Participant.ChannelName;
        public bool IsMuted => m_Participant.IsMuted;
        public double AudioEnergy => m_Participant.AudioEnergy;
        public bool SpeechDetected => m_Participant.SpeechDetected;
        public void MuteLocally(bool mute)
        {
            if(mute)
                m_Participant.MutePlayerLocally();
            else
                m_Participant.UnmutePlayerLocally();
        }
    }
}
#endif
