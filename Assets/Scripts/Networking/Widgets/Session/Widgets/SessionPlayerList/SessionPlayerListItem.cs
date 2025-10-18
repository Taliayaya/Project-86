using System;
using Armament.Shared;
using Networking.Widgets.Core.Base.ChatService.Interfaces;
using Networking.Widgets.Core.Base.Widget.Interfaces;
using Networking.Widgets.Session.Session;
using ScriptableObjects.Skins;
using TMPro;
using Unity.Multiplayer.Widgets;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace Networking.Widgets.Session.Widgets.SessionPlayerList
{
    internal class SessionPlayerListItem : WidgetBehaviour, IChatParticipantEvents, IChatEvents, ISessionEvents, ISessionProvider
    {
        public ISession Session { get; set; }
        
        internal struct Configuration
        {
            public bool HostCanKickPlayers;
            public bool PlayersCanBeMuted;
        }

        public string PlayerId { get; set; }
        public IChatParticipant ChatParticipant { get; set; }
        
        bool m_IsLocalPlayer;
        Configuration m_Configuration;
        
        /// <summary>
        /// The text element that will display the player's name.
        /// </summary>
        public TMP_Text PlayerNameText;
        
        /// <summary>
        /// The button that will kick the player from the session.
        /// </summary>
        public Button KickButton;
        
        /// <summary>
        /// The button that will toggle the mute state of a player in the session.
        /// </summary>
        public Button MuteButton;

        public Image ArmamentType;
        public RawImage PersonalMark;
        
        /// <summary>
        /// The icon that will show speech activity and mute states.
        /// </summary>
        public Image VoiceIndicator;

        /// <summary>
        /// The icon that will show when the player is not speaking.
        /// </summary>
        [Header("Voice Indicator Icons")]
        public Sprite VoiceIndicatorNoSound;
        /// <summary>
        /// The icon that will show when the player is speaking with low volume.
        /// </summary>
        public Sprite VoiceIndicatorLowSound;
        /// <summary>
        /// The icon that will show when the player is speaking with high volume.
        /// </summary>
        public Sprite VoiceIndicatorHighSound;
        /// <summary>
        /// The icon that will show when there is no voice audio available.
        /// </summary>
        public Sprite VoiceIndicatorNoAudio;
        /// <summary>
        /// The icon that will show when the player is muted.
        /// </summary>
        public Sprite VoiceIndicatorMuted;
        
        internal void Init(string playerName, string playerId, Configuration configuration, string armament, string personalMark)
        {
            PlayerNameText.text = playerName;
            PlayerId = playerId;
            m_Configuration = configuration;
            
            m_IsLocalPlayer = playerId == Session.CurrentPlayer.Id;
            ShowKickButtonIfConditionsAreMet();
            
            MuteButton.gameObject.SetActive(m_Configuration.PlayersCanBeMuted);
            SetMuteButtonInteractable(false);

            SetVoiceIndicatorEnabled(false);
            
            SetArmamentType(armament);
            SetPersonalMark(personalMark);
            
            KickButton.onClick.AddListener(OnKickButtonClicked);
            MuteButton.onClick.AddListener(OnMuteButtonClicked);
        }

        internal void SetArmamentType(string armamentFile)
        {
            ArmamentSO armament = Resources.Load<ArmamentSO>($"ScriptableObjects/Armament/{armamentFile}");
            ArmamentType.sprite = armament.Icon;
        }

        internal void SetPersonalMark(string personalMarkFile)
        {
            if (personalMarkFile == String.Empty)
                return;
            PersonalMarkSO personalMark = Resources.Load<PersonalMarkSO>($"ScriptableObjects/Skins/PersonalMarks/{personalMarkFile}");
            PersonalMark.texture = personalMark.image;
        }

        internal void Reset()
        {
            KickButton.onClick.RemoveListener(OnKickButtonClicked);
            MuteButton.onClick.RemoveListener(OnMuteButtonClicked);

            PlayerId = null;
            ChatParticipant = null;
        }
        
        void ShowKickButtonIfConditionsAreMet()
        {
            KickButton.gameObject.SetActive(m_Configuration.HostCanKickPlayers && Session.IsHost && !m_IsLocalPlayer);
        }

        public void OnSessionChanged()
        {
            // The host might have changed.
            ShowKickButtonIfConditionsAreMet();
        }

        public void OnChatJoined(string chatId)
        {
            SetVoiceIndicatorEnabled(false);
        }
        
        public void OnChatLeft(string chatId)
        {
            SetVoiceIndicatorEnabled(false);
        }

        internal void SetMuteButtonInteractable(bool isInteractable)
        {
            MuteButton.interactable = isInteractable;
        }
        
        internal void SetVoiceIndicatorEnabled(bool isEnabled)
        {
            VoiceIndicator.enabled = isEnabled;
        }
        
        void OnKickButtonClicked()
        {
            SessionManager.Instance.KickPlayer(PlayerId);
        }

        void OnMuteButtonClicked()
        {
            if (ChatParticipant == null)
            {
                Debug.LogWarning("No Player is assigned to this PlayerListItem (ChatParticipant == null). Cannot mute player.");
                return;
            }

            var muteState = ChatParticipant?.IsMuted ?? false;
            
            ChatParticipant?.MuteLocally(!muteState);
            OnPlayerMuteStateChanged(!muteState);
        }

        public void OnPlayerMuteStateChanged(bool isMuted)
        {
            MuteButton.GetComponentInChildren<TMP_Text>().text = isMuted ? "Unmute" : "Mute";
            VoiceIndicator.sprite = isMuted ? VoiceIndicatorMuted : VoiceIndicatorNoSound;
        }
        
        public void OnPlayerSpeechDetected()
        {
            if (ChatParticipant.IsMuted)
                return;
            
            VoiceIndicator.sprite = VoiceIndicatorHighSound;
        }
        
        public void OnPlayerAudioEnergyChanged()
        {
            if (ChatParticipant.IsMuted)
                return;
            
            if (!ChatParticipant.SpeechDetected)
            {
                VoiceIndicator.sprite = VoiceIndicatorNoSound;
                return;
            }
            
            var audioEnergy = RemapAudioEnergy(ChatParticipant.AudioEnergy);
            if (audioEnergy >= 0.1)
                VoiceIndicator.sprite =  VoiceIndicatorHighSound;
        }

        /// <summary>
        /// The audio energy provided by Vivox typically ranges from 0.4 to 0.6.
        /// We remap it here to a range from 0 to 1 for better usability.
        /// </summary>
        static double RemapAudioEnergy(double audioEnergy)
        {
            return (audioEnergy - 0.4) / (0.6 - 0.4);
        }
    }
}
