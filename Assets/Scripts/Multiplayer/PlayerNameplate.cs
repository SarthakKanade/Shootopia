using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Collections;

namespace Shootopia
{
    public class PlayerNameplate : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Canvas _canvas;

        private PlayerNameDisplay _nameDisplay;
        private Transform _camTransform;

        private void Start()
        {
            _nameDisplay = GetComponentInParent<PlayerNameDisplay>();
            if (_nameDisplay != null)
            {
                _nameDisplay.DisplayName.OnValueChanged += OnNameChanged;
                UpdateNameplateText(_nameDisplay.DisplayName.Value);
            }

            var mainCam = Camera.main;
            if (mainCam != null)
            {
                _camTransform = mainCam.transform;
            }
        }

        private void OnDestroy()
        {
            if (_nameDisplay != null)
            {
                _nameDisplay.DisplayName.OnValueChanged -= OnNameChanged;
            }
        }

        private void OnNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
        {
            UpdateNameplateText(newValue);
        }

        private void UpdateNameplateText(FixedString64Bytes name)
        {
            string displayName = name.ToString();
            if (string.IsNullOrEmpty(displayName))
            {
                var no = GetComponentInParent<NetworkObject>();
                if (no != null)
                {
                    displayName = PlayerNameRegistry.GetName(no.OwnerClientId);
                }
                else
                {
                    displayName = "Player";
                }
            }

            if (_nameText != null)
            {
                _nameText.text = displayName;
            }

            Debug.Log("[PlayerNameplate] Set nameplate for: " + displayName);
        }

        private void LateUpdate()
        {
            if (_camTransform != null && _canvas != null)
            {
                // Billboard: face the camera directly
                _canvas.transform.LookAt(_canvas.transform.position + _camTransform.forward);
            }
            else if (_camTransform == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    _camTransform = mainCam.transform;
                }
            }
        }

        // Hook for Phase 4.7 (Team Colors)
        public void SetNameplateColor(Color c)
        {
            if (_nameText != null)
            {
                _nameText.color = c;
            }
        }
    }
}
