using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shootopia
{
    public partial class WaitingRoomController
    {
private string GetCurrentMapName()
        {
            if (SessionManager.Instance != null)
            {
                string sessionMap = SessionManager.Instance.GetSessionMapName();
                if (!string.IsNullOrEmpty(sessionMap)) return sessionMap;
            }
            return SceneManager.GetActiveScene().name;
        }

        private void PlayWaitingRoomMusic()
        {
            if (AudioManager.Instance != null)
            {
                if (waitingRoomBgm != null)
                {
                    AudioManager.Instance.PlayMusic(waitingRoomBgm, 1.0f);
                }
                else
                {
                    AudioManager.Instance.PlayWaitingRoomMusic(1.0f);
                }
            }
            else
            {
                StartCoroutine(DeferredPlayWaitingRoomMusic());
            }
        }

        private IEnumerator DeferredPlayWaitingRoomMusic()
        {
            float elapsed = 0f;
            while (AudioManager.Instance == null && elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!_matchStarted && !_isGameOverMode && AudioManager.Instance != null)
            {
                if (waitingRoomBgm != null)
                {
                    AudioManager.Instance.PlayMusic(waitingRoomBgm, 1.0f);
                }
                else
                {
                    AudioManager.Instance.PlayWaitingRoomMusic(1.0f);
                }
            }
        }

        private void PlayMapBattleMusic()
        {
            string mapName = GetCurrentMapName();
            AudioClip directClip = null;
            if (string.Equals(mapName, "GasStationMap", System.StringComparison.OrdinalIgnoreCase) ||
                mapName.IndexOf("GasStation", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                directClip = gasStationBgm;
            }
            else
            {
                directClip = volcanoBgm;
            }

            if (AudioManager.Instance != null)
            {
                if (directClip != null)
                {
                    AudioManager.Instance.PlayMusic(directClip, 1.2f);
                }
                else
                {
                    AudioManager.Instance.PlayMapMusic(mapName, 1.2f);
                }
            }
            else
            {
                StartCoroutine(DeferredPlayMapBattleMusic(mapName, directClip));
            }
        }

        private IEnumerator DeferredPlayMapBattleMusic(string mapName, AudioClip directClip)
        {
            float elapsed = 0f;
            while (AudioManager.Instance == null && elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (AudioManager.Instance != null)
            {
                if (directClip != null)
                {
                    AudioManager.Instance.PlayMusic(directClip, 1.2f);
                }
                else
                {
                    AudioManager.Instance.PlayMapMusic(mapName, 1.2f);
                }
            }
        }
    }
}
