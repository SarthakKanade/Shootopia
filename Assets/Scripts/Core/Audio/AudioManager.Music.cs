using System.Collections;
using UnityEngine;

namespace Shootopia
{
    public partial class AudioManager
    {
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
            _musicSource.priority = 128;
            _musicSource.clip = AudioClip.Create("SilentInit", 1, 1, 44100, false);

            // Attach dynamic low-pass filter for frequency carving during intense SFX bursts
            _musicLowPassFilter = musicGo.AddComponent<AudioLowPassFilter>();
            _musicLowPassFilter.cutoffFrequency = 22000f;
            _musicLowPassFilter.enabled = false;
            _currentLowPassCutoff = 22000f;
            _targetLowPassCutoff = 22000f;
        }

public void PlayMusic(AudioClip clip, float fadeSeconds = 1.5f)
        {
            if (clip == null) return;
            if (_musicSource == null) InitializeMusicSource();
            if (_musicSource == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }

            _musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(clip, fadeSeconds));
        }

        public void StopMusic(float fadeSeconds = 1.0f)
        {
            if (_musicSource == null || !_musicSource.isPlaying) return;

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }

            _musicFadeCoroutine = StartCoroutine(FadeOutMusicRoutine(fadeSeconds));
        }

        private IEnumerator FadeOutMusicRoutine(float fadeDuration)
        {
            if (_musicSource != null && _musicSource.isPlaying && fadeDuration > 0.05f)
            {
                float startVolume = _musicSource.volume;
                float timer = 0f;
                while (timer < fadeDuration)
                {
                    timer += Time.deltaTime;
                    _musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
                    yield return null;
                }
                _musicSource.Stop();
                _musicSource.clip = null;
            }
            else if (_musicSource != null)
            {
                _musicSource.Stop();
                _musicSource.clip = null;
            }

            _musicFadeCoroutine = null;
        }

        public void PlayWaitingRoomMusic(float fadeSeconds = 1.0f)
        {
            if (waitingRoomMusicClip != null)
            {
                PlayMusic(waitingRoomMusicClip, fadeSeconds);
            }
        }

        public void PlayMapMusic(string mapOrSceneName = null, float fadeSeconds = 1.0f)
        {
            if (string.IsNullOrEmpty(mapOrSceneName))
            {
                mapOrSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            }

            AudioClip targetClip = null;
            if (string.Equals(mapOrSceneName, "GasStationMap", System.StringComparison.OrdinalIgnoreCase) ||
                mapOrSceneName.IndexOf("GasStation", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                targetClip = gasStationMusicClip;
            }
            else // Map1, Sector Alpha, GameScene, Shootopia_Sandbox, Volcano
            {
                targetClip = volcanoMusicClip;
            }

            if (targetClip != null)
            {
                PlayMusic(targetClip, fadeSeconds);
            }
        }



        private IEnumerator FadeMusicRoutine(AudioClip newClip, float fadeDuration)
        {
            float targetVolume = Mathf.Clamp01(_masterVolume * (_musicVolume * musicHeadroomScale));

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
            _musicSource.loop = true;

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
    }
}
