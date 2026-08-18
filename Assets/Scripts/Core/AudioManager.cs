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
    public class AudioManager : MonoBehaviour
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

        private float _masterVolume = 1f;
        private float _musicVolume = 0.8f;
        private float _sfxVolume = 1f;

        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private int _poolIndex = 0;

        private AudioSource _musicSource;
        private Coroutine _musicFadeCoroutine;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            LoadVolumeSettings();
            InitializeAudioPool();
            InitializeMusicSource();

            Debug.Log("[AudioManager] Initialized successfully.");
        }

        private void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
            _musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC, 0.8f);
            _sfxVolume = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        }

        private void InitializeAudioPool()
        {
            _sfxPool.Clear();
            GameObject poolContainer = new GameObject("SFX_Pool");
            poolContainer.transform.SetParent(transform);

            for (int i = 0; i < poolSize; i++)
            {
                GameObject srcGo = new GameObject($"SFX_Source_{i}");
                srcGo.transform.SetParent(poolContainer.transform);
                AudioSource src = srcGo.AddComponent<AudioSource>();

                src.playOnAwake = false;
                src.spatialBlend = 0f; // Default 2D
                src.minDistance = defaultSpatialMinDistance;
                src.maxDistance = defaultSpatialMaxDistance;
                src.rolloffMode = AudioRolloffMode.Logarithmic;

                _sfxPool.Add(src);
            }
        }

        private void InitializeMusicSource()
        {
            GameObject musicGo = new GameObject("Music_Source");
            musicGo.transform.SetParent(transform);
            musicGo.SetActive(true);
            _musicSource = musicGo.AddComponent<AudioSource>();
            _musicSource.enabled = true;
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f; // 2D Music
        }

        private AudioSource GetNextAvailableSource()
        {
            // Search for an unused source first
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                int idx = (_poolIndex + i) % _sfxPool.Count;
                if (!_sfxPool[idx].isPlaying)
                {
                    _poolIndex = (idx + 1) % _sfxPool.Count;
                    return _sfxPool[idx];
                }
            }

            // Fallback: round-robin override if all sources are active
            AudioSource overrideSource = _sfxPool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % _sfxPool.Count;
            overrideSource.Stop();
            return overrideSource;
        }

        /// <summary>
        /// Plays a 2D SFX (UI sound, local feedback) at global SFX volume.
        /// </summary>
        public void PlaySFX2D(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetNextAvailableSource();
            if (source == null) return;

            source.transform.position = transform.position;
            source.spatialBlend = 0f;
            source.volume = Mathf.Clamp01(_masterVolume * _sfxVolume * volumeScale);
            source.clip = clip;
            source.Play();
        }

        /// <summary>
        /// Plays a 3D spatialized SFX at a specific position in the world.
        /// </summary>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            if (clip == null) return;

            AudioSource source = GetNextAvailableSource();
            if (source == null) return;

            source.transform.position = position;
            source.spatialBlend = 1f; // Full 3D spatialization
            source.volume = Mathf.Clamp01(_masterVolume * _sfxVolume * volumeScale);
            source.clip = clip;
            source.Play();
        }

        /// <summary>
        /// Plays a looping or sustained SFX attached to a target transform with configurable pitch and volume.
        /// </summary>
        public AudioSource PlayLoopingSFX(AudioClip clip, Transform targetTransform, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null) return null;

            AudioSource source = GetNextAvailableSource();
            if (source == null) return null;

            if (targetTransform != null)
            {
                source.transform.position = targetTransform.position;
                source.transform.SetParent(targetTransform);
            }

            source.spatialBlend = 1f;
            source.loop = true;
            source.pitch = pitch;
            source.volume = Mathf.Clamp01(_masterVolume * _sfxVolume * volumeScale);
            source.clip = clip;
            source.Play();
            return source;
        }

        /// <summary>
        /// Stops a looping SFX source and resets it back to the audio pool container.
        /// </summary>
        public void StopLoopingSFX(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
                source.loop = false;
                source.pitch = 1f;
                source.transform.SetParent(transform);
            }
        }

        /// <summary>
        /// Plays BGM music with an optional linear crossfade transition.
        /// </summary>
        public void PlayMusic(AudioClip clip, float fadeSeconds = 1.5f)
        {
            if (clip == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }

            _musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(clip, fadeSeconds));
        }

        private IEnumerator FadeMusicRoutine(AudioClip newClip, float fadeDuration)
        {
            float targetVolume = Mathf.Clamp01(_masterVolume * _musicVolume);

            if (_musicSource.isPlaying && fadeDuration > 0.05f)
            {
                float startVolume = _musicSource.volume;
                float timer = 0f;

                // Fade out current track
                while (timer < fadeDuration / 2f)
                {
                    timer += Time.deltaTime;
                    _musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / (fadeDuration / 2f));
                    yield return null;
                }
            }

            _musicSource.Stop();
            _musicSource.clip = newClip;
            _musicSource.volume = 0f;

            if (newClip != null && _musicSource != null)
            {
                _musicSource.enabled = true;
                if (_musicSource.isActiveAndEnabled)
                {
                    _musicSource.Play();
                }

                if (fadeDuration > 0.05f)
                {
                    float timer = 0f;
                    while (timer < fadeDuration / 2f)
                    {
                        timer += Time.deltaTime;
                        _musicSource.volume = Mathf.Lerp(0f, targetVolume, timer / (fadeDuration / 2f));
                        yield return null;
                    }
                }
            }

            _musicSource.volume = targetVolume;
            _musicFadeCoroutine = null;
        }

        /// <summary>
        /// Sets channel volume (0.0 to 1.0) and persists via PlayerPrefs.
        /// </summary>
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
                        _musicSource.volume = _masterVolume * _musicVolume;
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
