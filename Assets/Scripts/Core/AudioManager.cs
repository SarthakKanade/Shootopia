using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    public enum AudioChannel
    {
        Master,
        Music,
        SFX
    }

    /// <summary>
    /// Centralized, network-aware audio manager singleton.
    /// Handles 2D/3D SFX playback via a pre-warmed AudioSource pool and BGM with crossfading.
    /// Persists Master, Music, and SFX volume settings via PlayerPrefs.
    /// </summary>
    public partial class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<AudioManager>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private const string KEY_MASTER = "AudioManager_MasterVolume";
        private const string KEY_MUSIC = "AudioManager_MusicVolume";
        private const string KEY_SFX = "AudioManager_SFXVolume";

        [Header("Audio Pool Settings")]
        [SerializeField] private int poolSize = 16;
        [SerializeField] private float defaultSpatialMinDistance = 1f;
        [SerializeField] private float defaultSpatialMaxDistance = 35f;

        [Header("Professional Mix Headroom & Sidechain Ducking")]
        [Tooltip("Baseline attenuation for background music relative to full scale SFX to guarantee dynamic headroom.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float musicHeadroomScale = 0.42f;

        [Tooltip("Target volume multiplier when combat ducking is active (0.55 = ducks to ~50-60% volume during other sounds).")]
        [Range(0.2f, 1.0f)]
        [SerializeField] private float defaultDuckLevel = 0.55f;

        [Tooltip("Cutoff frequency for the dynamic low-pass filter during combat ducking.")]
        [SerializeField] private float duckLowPassCutoff = 20000f;

        [Tooltip("How fast music volume ducks down during combat (seconds).")]
        [SerializeField] private float duckAttackTime = 0.10f;

        [Tooltip("How smoothly music volume recovers after combat action (seconds).")]
        [SerializeField] private float duckReleaseTime = 0.60f;

        [Header("Game Soundtrack")]
        [SerializeField] private AudioClip waitingRoomMusicClip;
        [SerializeField] private AudioClip gasStationMusicClip;
        [SerializeField] private AudioClip volcanoMusicClip;
        [SerializeField] private AudioClip menuMusicClip;

        [Header("UI & Game Outcome SFX")]
        [SerializeField] private AudioClip defaultClickClip;
        [SerializeField] private AudioClip defaultHoverClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;

        public AudioClip WaitingRoomMusicClip => waitingRoomMusicClip;
        public AudioClip GasStationMusicClip => gasStationMusicClip;
        public AudioClip VolcanoMusicClip => volcanoMusicClip;
        public AudioClip MenuMusicClip => menuMusicClip != null ? menuMusicClip : waitingRoomMusicClip;
        public AudioClip DefaultClickClip => defaultClickClip;
        public AudioClip DefaultHoverClip => defaultHoverClip;
        public AudioClip VictoryClip => victoryClip;
        public AudioClip DefeatClip => defeatClip;

        private float _masterVolume = 1f;
        private float _musicVolume = 0.8f;
        private float _sfxVolume = 1f;

        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private int _poolIndex = 0;

        private AudioSource _musicSource;
        private AudioLowPassFilter _musicLowPassFilter;
        private Coroutine _musicFadeCoroutine;

        // Sidechain Ducking Envelope State
        private float _combatDuckTimer = 0f;
        private float _targetDuckLevel = 1f;
        private float _currentDuckMultiplier = 1f;
        private float _targetLowPassCutoff = 22000f;
        private float _currentLowPassCutoff = 22000f;
        private bool _isSustainedDucking = false;
        private float _sustainedDuckLevel = 0.55f;
        private bool _isLowHealthMuffled = false;
        private float _lowHealthCutoff = 1000f;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;

            LoadVolumeSettings();
            InitializeAudioPool();
            InitializeMusicSource();

            Debug.Log("[AudioManager] Initialized successfully.");
        }

        private void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
            _musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC, 0.65f);
            _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        }

        public void SetChannelVolume(AudioChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);

            switch (channel)
            {
                case AudioChannel.Master:
                    _masterVolume = volume;
                    PlayerPrefs.SetFloat(KEY_MASTER, _masterVolume);
                    break;
                case AudioChannel.Music:
                    _musicVolume = volume;
                    PlayerPrefs.SetFloat(KEY_MUSIC, _musicVolume);
                    if (_musicSource != null && _musicSource.isPlaying && _musicFadeCoroutine == null)
                    {
                        _musicSource.volume = _masterVolume * (_musicVolume * musicHeadroomScale) * _currentDuckMultiplier;
                    }
                    break;
                case AudioChannel.SFX:
                    _sfxVolume = volume;
                    PlayerPrefs.SetFloat(KEY_SFX, _sfxVolume);
                    break;
            }

            PlayerPrefs.Save();
            Debug.Log($"[AudioManager] {channel} volume set to {volume:F2}");
        }

        /// <summary>
        /// Gets current volume for specified channel (0.0 to 1.0).
        /// </summary>
        public float GetChannelVolume(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Master: return _masterVolume;
                case AudioChannel.Music: return _musicVolume;
                case AudioChannel.SFX: return _sfxVolume;
                default: return 1f;
            }
        }
    }
}
