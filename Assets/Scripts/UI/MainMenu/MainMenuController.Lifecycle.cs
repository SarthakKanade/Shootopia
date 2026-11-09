using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shootopia
{
    public partial class MainMenuController
    {
private void OnEnable()
        {
            CursorManager.PushUnlock("MainMenu");
            RefreshProfileDisplay();

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionError.AddListener(OnSessionError);
            }

            RegisterCallbacks();
        }

        private async void Start()
        {
            if (menuMusicClip == null && AudioManager.Instance != null)
            {
                menuMusicClip = AudioManager.Instance.MenuMusicClip;
            }
            if (menuMusicClip != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMusic(menuMusicClip, 0.8f);
            }

            InitSettingsValues();
            UpdateChipVisuals();

            InitRoomCreationDefaults();
                        EnsureLanRoomToggleInitialized();
            await MultiplayerAutoInitializer.EnsureInitializedAsync();
        }

        private void OnDisable()
        {
            CursorManager.PopUnlock("MainMenu");

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnSessionError.RemoveListener(OnSessionError);
            }

            UnregisterCallbacks();
        }

        private void Update()
        {
            if (_isBusy && spinnerIcon != null)
            {
                spinnerIcon.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime);
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.oKey.wasPressedThisFrame)
                {
                    if (modalCustomize != null)
                    {
                        if (modalCustomize.activeSelf) CloseTopModal();
                        else OpenModal(modalCustomize, customizeGroup);
                    }
                }
                else if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    if (_modalStack.Count > 0)
                    {
                        CloseTopModal();
                    }
                }
            }
        }

        // ──────────────────────── CALLBACK REGISTRATION ────────────────────────

        // ──────────────────────── SETTINGS ────────────────────────
    }
}
