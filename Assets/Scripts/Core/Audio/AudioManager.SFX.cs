using System.Collections.Generic;
using UnityEngine;

namespace Shootopia
{
    public partial class AudioManager
    {
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
                src.priority = 32; // Elevated priority so combat SFX never get culled

                _sfxPool.Add(src);
            }
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
        public void PlaySFX2D(AudioClip clip, float volumeScale = 1f, bool isCombatPriority = false)
        {
            if (clip == null) return;

            AudioSource source = GetNextAvailableSource();
            if (source == null) return;

            source.transform.position = transform.position;
            source.spatialBlend = 0f;
            source.priority = isCombatPriority ? 16 : 48;
            source.volume = Mathf.Clamp01(_masterVolume * _sfxVolume * volumeScale);
            source.clip = clip;
            source.Play();

            if (isCombatPriority)
            {
                TriggerCombatDucking(defaultDuckLevel, 0.6f, true);
            }
        }

        /// <summary>
        /// Plays a 3D spatialized SFX at a specific position in the world.
        /// </summary>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f, bool isCombatPriority = true)
        {
            if (clip == null) return;

            AudioSource source = GetNextAvailableSource();
            if (source == null) return;

            source.transform.position = position;
            source.spatialBlend = 1f; // Full 3D spatialization
            source.priority = isCombatPriority ? 24 : 64;
            source.volume = Mathf.Clamp01(_masterVolume * _sfxVolume * volumeScale);
            source.clip = clip;
            source.Play();

            if (isCombatPriority)
            {
                TriggerCombatDucking(defaultDuckLevel, 0.7f, true);
            }
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
            source.priority = 16;
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
    }
}
