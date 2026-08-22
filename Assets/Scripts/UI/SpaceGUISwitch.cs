using System;
using UnityEngine;
using UnityEngine.UI;

namespace Shootopia
{
    /// <summary>
    /// Component managing the authentic Space Exploration GUI Kit ON/OFF active/inactive buttons.
    /// </summary>
    public class SpaceGUISwitch : MonoBehaviour
    {
        [SerializeField] private Button btnOnActive;
        [SerializeField] private Button btnOnInactive;
        [SerializeField] private Button btnOffActive;
        [SerializeField] private Button btnOffInactive;

        public bool IsOn { get; private set; } = true;
        public event Action<bool> OnValueChanged;

        private void Awake()
        {
            AutoWire();
            RegisterListeners();
        }

        public void AutoWire()
        {
            if (btnOnActive == null) btnOnActive = transform.Find("ON(active)")?.GetComponent<Button>();
            if (btnOnInactive == null) btnOnInactive = transform.Find("ON(inactive)")?.GetComponent<Button>();
            if (btnOffActive == null) btnOffActive = transform.Find("Off(active)")?.GetComponent<Button>();
            if (btnOffInactive == null) btnOffInactive = transform.Find("Off(inactive)")?.GetComponent<Button>();
        }

        private void RegisterListeners()
        {
            if (btnOnActive != null) btnOnActive.onClick.AddListener(() => SetState(true, true));
            if (btnOnInactive != null) btnOnInactive.onClick.AddListener(() => SetState(true, true));
            if (btnOffActive != null) btnOffActive.onClick.AddListener(() => SetState(false, true));
            if (btnOffInactive != null) btnOffInactive.onClick.AddListener(() => SetState(false, true));
        }

        public void SetState(bool state, bool triggerCallback = false)
        {
            IsOn = state;
            if (btnOnActive != null) btnOnActive.gameObject.SetActive(IsOn);
            if (btnOnInactive != null) btnOnInactive.gameObject.SetActive(!IsOn);
            if (btnOffActive != null) btnOffActive.gameObject.SetActive(!IsOn);
            if (btnOffInactive != null) btnOffInactive.gameObject.SetActive(IsOn);

            if (triggerCallback)
            {
                OnValueChanged?.Invoke(IsOn);
            }
        }
    }
}
