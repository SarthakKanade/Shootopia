using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Shootopia
{
    public partial class MainMenuController
    {
public void OpenModal(GameObject modal, CanvasGroup group = null)
        {
            if (modal == null) return;
            if (modal == modalSettings)
            {
                InitSettingsValues();
            }
            _modalStack.Push(modal);
            UIJuice.PopIn(modal.GetComponent<RectTransform>(), group);
        }

        public void CloseTopModal()
        {
            if (_modalStack.Count > 0)
            {
                var top = _modalStack.Pop();
                var group = top.GetComponent<CanvasGroup>();
                UIJuice.PopOut(top.GetComponent<RectTransform>(), group);
            }
        }

        public void ShowBusy(bool show, string message = "WARPING...")
        {
            _isBusy = show;
            if (busyOverlay != null)
            {
                if (show)
                {
                    if (busyLabel != null) busyLabel.text = message;
                    UIJuice.PopIn(busyOverlay.GetComponent<RectTransform>(), busyGroup);
                }
                else
                {
                    UIJuice.PopOut(busyOverlay.GetComponent<RectTransform>(), busyGroup);
                }
            }
        }

        private async void CancelBusy()
        {
            ShowBusy(false);
            if (SessionManager.Instance != null)
            {
                await SessionManager.Instance.LeaveSession();
            }
        }

        public void ShowError(string error)
        {
            ShowBusy(false);
            string friendlyText = GetFriendlyErrorMessage(error);
            if (errorModal != null)
            {
                if (errorLabel != null) errorLabel.text = friendlyText;
                OpenModal(errorModal, errorGroup);
                if (errorClip != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX2D(errorClip, 1.0f);
                }
            }
            else
            {
                NetworkNotificationUI.ShowToast(friendlyText, isError: true);
            }
        }

        public static string GetFriendlyErrorMessage(string rawError, string action = "connect")
        {
            if (string.IsNullOrWhiteSpace(rawError))
                return "Unable to connect to game servers. Please try again.";

            string lower = rawError.ToLowerInvariant();

            // Null reference / uninitialized state
            if (lower.Contains("object reference") || lower.Contains("nullreference") || lower.Contains("not found!"))
            {
                return "Game services are still warming up. Please tap again in a moment.";
            }

            // Timeouts & connection loss
            if (lower.Contains("timeout") || lower.Contains("timed out") || lower.Contains("interrupted"))
            {
                return "Connection timed out. Please check your internet connection and try again.";
            }

            // Room full
            if (lower.Contains("gameisfull") || lower.Contains("room is full") || lower.Contains("serverfull"))
            {
                return "This room is currently full! Please try another room or create a new one.";
            }

            // Room closed / doesn't exist
            if (lower.Contains("gamenotfound") || lower.Contains("room not found") || lower.Contains("does not exist") || lower.Contains("invalid code") || lower.Contains("gameclosed"))
            {
                return "Room not found or match has already ended. Please double-check the room code.";
            }

            // Socket / Network DNS unreachable
            if (lower.Contains("socket") || lower.Contains("unreachable") || lower.Contains("disconnect") || lower.Contains("dns") || lower.Contains("host not found"))
            {
                return "Unable to reach multiplayer servers. Please check your internet connection.";
            }

            // Technical / internal engine or configuration errors
            if (lower.Contains("incompatibleconfiguration") || lower.Contains("gamemode") || 
                lower.Contains("not allowed in webgl") || lower.Contains("networkprojectconfig") ||
                lower.Contains("shutdownreason") || lower.Contains("<color="))
            {
                return $"Unable to {action} right now. Please try again in a moment.";
            }

            // Clean technical exception prefixes
            string cleaned = rawError;
            string[] technicalPrefixes = new string[]
            {
                "Failed to create room: ",
                "Failed to join room: ",
                "Failed to join: ",
                "Matchmaking Failed: ",
                "CreateSession failed: ",
                "JoinByCode failed: ",
                "LAN Host failed: ",
                "LAN Join failed: "
            };

            foreach (var prefix in technicalPrefixes)
            {
                if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = cleaned.Substring(prefix.Length).Trim();
                }
            }

            // Strip any leftover XML/rich-text color tags
            if (cleaned.Contains("<color=") || cleaned.Contains("</color>"))
            {
                cleaned = Regex.Replace(cleaned, @"<[^>]*>", string.Empty).Trim();
            }

            // Clean fallback for raw C# exceptions
            if (cleaned.Contains("Exception") || cleaned.Contains("at Shootopia") || cleaned.Contains("System.") || 
                cleaned.Contains("NullReference") || (!cleaned.Contains(" ") && cleaned.Length > 8))
            {
                return $"Unable to {action} right now. Please try again in a moment.";
            }

            return cleaned;
        }

        private void OnSessionError(string error)
        {
            ShowError(error);
        }
    }
}
