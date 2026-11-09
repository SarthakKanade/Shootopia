using System;
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
            if (errorModal != null)
            {
                if (errorLabel != null) errorLabel.text = error;
                OpenModal(errorModal, errorGroup);
                if (errorClip != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX2D(errorClip, 1.0f);
                }
            }
            else
            {
                NetworkNotificationUI.ShowToast(error, isError: true);
            }
        }

        private void OnSessionError(string error)
        {
            ShowError(error);
        }
    }
}
