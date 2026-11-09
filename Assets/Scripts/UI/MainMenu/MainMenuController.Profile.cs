using UnityEngine;

namespace Shootopia
{
    public partial class MainMenuController
    {
public void RefreshProfileDisplay()
        {
            string pName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
            if (playerNameLabel != null) playerNameLabel.text = pName;
            if (inputDisplayName != null) inputDisplayName.text = pName;
        }

        private void SaveProfile()
        {
            if (inputDisplayName != null && !string.IsNullOrWhiteSpace(inputDisplayName.text))
            {
                if (PlayerProfileManager.Instance != null)
                {
                    PlayerProfileManager.Instance.SetDisplayName(inputDisplayName.text.Trim());
                }
                RefreshProfileDisplay();
            }
            CloseTopModal();
        }

        private void ToggleSound()
        {
            _isMuted = !_isMuted;
            AudioListener.pause = _isMuted;
            if (soundIconImage != null)
            {
                soundIconImage.sprite = _isMuted ? soundOffSprite : soundOnSprite;
            }
        }

private void SelectSkin(int skinIndex)
        {
            _selectedSkin = Mathf.Clamp(skinIndex, 0, 1);
            if (PlayerProfileManager.Instance != null)
            {
                PlayerProfileManager.Instance.SetSelectedModel(_selectedSkin);
            }
            else
            {
                PlayerPrefs.SetInt("Shootopia_SelectedModelIndex", _selectedSkin);
                PlayerPrefs.Save();
            }

            if (AvatarStageController.Instance != null)
            {
                AvatarStageController.Instance.SetSkin(_selectedSkin);
            }
            UpdateChipVisuals();
        }

private void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
    }
}
