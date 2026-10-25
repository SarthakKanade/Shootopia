using System;
using UnityEngine;

namespace Shootopia
{
    /// <summary>
    /// Manages persistent player profile settings (Display Name, Level/XP, Sensitivity, Invert Y, etc.)
    /// via PlayerPrefs. Emits events when settings change to keep UI synchronized.
    /// </summary>
    public class PlayerProfileManager : MonoBehaviour
    {
        private static PlayerProfileManager _instance;
        public static PlayerProfileManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<PlayerProfileManager>();
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private const string KEY_PLAYER_NAME = "Shootopia_PlayerDisplayName";
        private const string KEY_MOUSE_SENS = "Shootopia_MouseSensitivity";
        private const string KEY_INVERT_Y = "Shootopia_InvertY";
        private const string KEY_CROSSHAIR_STYLE = "Shootopia_CrosshairStyle";

        private const string KEY_MODEL_INDEX = "Shootopia_SelectedModelIndex";

        public static event Action<string> OnPlayerNameChanged;
        public static event Action OnSettingsChanged;
        public static event Action<int> OnModelChanged;

        private string _displayName;
        private float _mouseSensitivity = 1.0f;
        private bool _invertY = false;
        private int _crosshairStyle = 0;
        private int _selectedModelIndex = 0; // 0 = Vanguard, 1 = Valkyrie

        public string DisplayName => _displayName;
        public float MouseSensitivity => _mouseSensitivity;
        public bool InvertY => _invertY;
        public int CrosshairStyle => _crosshairStyle;
        public int SelectedModelIndex => _selectedModelIndex;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;

            LoadProfile();
        }

        private void LoadProfile()
        {
            _displayName = PlayerPrefs.GetString(KEY_PLAYER_NAME, string.Empty);
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                // Generate a cool default name
                int randomId = UnityEngine.Random.Range(1000, 9999);
                _displayName = $"Agent_{randomId}";
                PlayerPrefs.SetString(KEY_PLAYER_NAME, _displayName);
                PlayerPrefs.Save();
            }

            _mouseSensitivity = PlayerPrefs.GetFloat(KEY_MOUSE_SENS, 1.0f);
            _invertY = PlayerPrefs.GetInt(KEY_INVERT_Y, 0) == 1;
            _crosshairStyle = PlayerPrefs.GetInt(KEY_CROSSHAIR_STYLE, 0);
            _selectedModelIndex = PlayerPrefs.GetInt(KEY_MODEL_INDEX, 0);

            Debug.Log($"[PlayerProfileManager] Loaded profile for: {_displayName} (Sens: {_mouseSensitivity}, InvertY: {_invertY}, Model: {_selectedModelIndex})");
        }

        public void SetSelectedModel(int index)
        {
            _selectedModelIndex = Mathf.Clamp(index, 0, 1);
            PlayerPrefs.SetInt(KEY_MODEL_INDEX, _selectedModelIndex);
            PlayerPrefs.Save();
            OnModelChanged?.Invoke(_selectedModelIndex);
        }

        public void SetDisplayName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;

            string trimmed = newName.Trim();
            if (trimmed.Length > 20)
            {
                trimmed = trimmed.Substring(0, 20);
            }

            _displayName = trimmed;
            PlayerPrefs.SetString(KEY_PLAYER_NAME, _displayName);
            PlayerPrefs.Save();

            Debug.Log($"[PlayerProfileManager] Display name updated to: {_displayName}");
            OnPlayerNameChanged?.Invoke(_displayName);
        }

        public void SetMouseSensitivity(float value)
        {
            _mouseSensitivity = Mathf.Clamp(value, 0.1f, 5.0f);
            PlayerPrefs.SetFloat(KEY_MOUSE_SENS, _mouseSensitivity);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public void SetInvertY(bool invert)
        {
            _invertY = invert;
            PlayerPrefs.SetInt(KEY_INVERT_Y, _invertY ? 1 : 0);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }

        public void SetCrosshairStyle(int styleIndex)
        {
            _crosshairStyle = styleIndex;
            PlayerPrefs.SetInt(KEY_CROSSHAIR_STYLE, _crosshairStyle);
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }
}
