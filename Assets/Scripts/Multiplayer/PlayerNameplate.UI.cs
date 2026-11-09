using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class PlayerNameplate
    {
private void BuildHealthBarUI()
        {
            if (_canvas == null) return;

            if (_healthBarSlider == null)
            {
                _healthBarSlider = _canvas.GetComponentInChildren<Slider>(true);
            }

            if (_healthBarSlider != null)
            {
                if (_healthFillImage == null && _healthBarSlider.fillRect != null)
                {
                    _healthFillImage = _healthBarSlider.fillRect.GetComponent<Image>();
                }
                return;
            }

            // Adjust name text position slightly higher
            if (_nameText != null)
            {
                var nameRect = _nameText.GetComponent<RectTransform>();
                if (nameRect != null)
                {
                    nameRect.anchoredPosition = new Vector2(0f, 12f);
                    nameRect.sizeDelta = new Vector2(250f, 30f);
                    _nameText.fontSize = 20;
                }
            }

            // Minimal fallback health bar
            GameObject healthBarGo = new GameObject("OverheadHealthBar", typeof(RectTransform), typeof(Slider));
            healthBarGo.transform.SetParent(_canvas.transform, false);
            var hbRect = healthBarGo.GetComponent<RectTransform>();
            hbRect.anchoredPosition = new Vector2(0f, -8f);
            hbRect.sizeDelta = new Vector2(100f, 10f);

            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(healthBarGo.transform, false);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            _healthBgImage = bgGo.GetComponent<Image>();
            _healthBgImage.color = HealthBgColor;

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(healthBarGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            _healthFillImage = fillGo.GetComponent<Image>();
            _healthFillImage.color = RedOpponentColor;

            _healthBarSlider = healthBarGo.GetComponent<Slider>();
            _healthBarSlider.minValue = 0f;
            _healthBarSlider.maxValue = 1f;
            _healthBarSlider.value = 1f;
            _healthBarSlider.interactable = false;
            _healthBarSlider.targetGraphic = _healthFillImage;
            _healthBarSlider.fillRect = fillRect;
            _healthBarSlider.transition = Selectable.Transition.None;
        }

public void SetNameplateColor(Color c)
        {
            if (_nameText != null)
            {
                _nameText.color = c;
            }

            if (_healthFillImage != null)
            {
                _healthFillImage.color = c;
            }
        }

        /// <summary>
        /// Hides or restores the overhead nameplate canvas when the target enters or leaves stealth/invisibility.
        /// </summary>
        public void SetInvisibilityHidden(bool hidden)
        {
            _isInvisibleHidden = hidden;
            if (_isLocalPlayer) return;

            if (_canvas != null)
            {
                bool isDead = (_playerHealth != null && _playerHealth.IsDead) || _isDeadHidden;
                _canvas.enabled = !hidden && !isDead;
            }
        }

        /// <summary>
        /// Hides or restores the overhead nameplate canvas on player death and respawn.
        /// </summary>
        public void SetDeathHidden(bool hidden)
        {
            _isDeadHidden = hidden;
            if (_isLocalPlayer) return;

            if (_canvas != null)
            {
                bool isDead = hidden || (_playerHealth != null && _playerHealth.IsDead);
                _canvas.enabled = !isDead && !_isInvisibleHidden;
            }
        }

        private void UpdateTeamAndColors(bool force)
        {
            if (_isLocalPlayer || _isInvisibleHidden || _isDeadHidden) return;
            if (_playerHealth != null && _playerHealth.IsDead) return;

            int relationship = DetermineRelationship();
            if (force || relationship != _lastRelationship)
            {
                _lastRelationship = relationship;

                // 0: Teammate (Blue), 1: Opponent (Red)
                Color themeColor = (relationship == 0) ? BlueTeammateColor : RedOpponentColor;
                SetNameplateColor(themeColor);
            }
        }

        /// <summary>
        /// Returns 0 for teammate, 1 for opponent.
        /// In TDM: compares target's team with local player's team.
        /// In FFA / other modes: all other players are opponents (1).
        /// </summary>
        private int DetermineRelationship()
        {
            if (GameModeBase.Instance is TDMMode tdm && _networkObject != null)
            {
                ulong localClientId = Unity.Netcode.NetworkManager.Singleton != null ? Unity.Netcode.NetworkManager.Singleton.LocalClientId : 9999;
                int localTeam = tdm.GetPlayerTeam(localClientId);
                int targetTeam = tdm.GetPlayerTeam(_networkObject.OwnerClientId);

                if (localTeam != -1 && targetTeam != -1)
                {
                    return (localTeam == targetTeam) ? 0 : 1;
                }

                // If local team not yet assigned, fallback to target's raw team (0=blue, 1=red)
                if (targetTeam != -1)
                {
                    return targetTeam;
                }
            }

            // In FFA or unassigned mode, all others are opponents (Red)
            return 1;
        }

        private static Transform s_MainCamTransform;

        private void LateUpdate()
        {
            if (_isLocalPlayer || _isInvisibleHidden || _isDeadHidden) return;
            if (_playerHealth != null && _playerHealth.IsDead) return;

            if (s_MainCamTransform == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null) s_MainCamTransform = mainCam.transform;
            }

            if (s_MainCamTransform != null && _canvas != null && _canvas.enabled)
            {
                _canvas.transform.LookAt(_canvas.transform.position + s_MainCamTransform.forward);
            }
        }
    }
}
