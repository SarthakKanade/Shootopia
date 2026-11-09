using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class MainMenuController
    {
private void EnsureLanRoomToggleInitialized()
        {
            if (togglePublicRoom == null || togglePublicRoom.transform.parent == null) return;

            GameObject publicRow = togglePublicRoom.transform.parent.gameObject;
            Transform container = publicRow.transform.parent; // TabContent_Create
            if (container == null) return;

            var vlg = container.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 10f;
                vlg.childForceExpandHeight = false;
            }

            publicRow.name = "Row_PublicRoom";
            var pubHlg = publicRow.GetComponent<HorizontalLayoutGroup>();
            if (pubHlg != null)
            {
                pubHlg.spacing = 14f;
                pubHlg.childAlignment = TextAnchor.MiddleLeft;
                pubHlg.childForceExpandWidth = false;
                pubHlg.childForceExpandHeight = false;
                pubHlg.childControlWidth = false;
                pubHlg.childControlHeight = false;
            }

            if (_publicRoomLabel == null)
            {
                _publicRoomLabel = publicRow.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (_publicRoomLabel != null)
            {
                _publicRoomLabel.text = "PUBLIC ARENA (VISIBLE IN BROWSER)";
                _publicRoomLabel.fontSize = 14f;
                _publicRoomLabel.margin = Vector4.zero;
                _publicRoomLabel.alignment = TextAlignmentOptions.MidlineLeft;
            }

            // Find or instantiate Row 2: LAN Room
            GameObject lanRow = null;
            Transform existingLanRow = container.Find("Row_LanRoom");
            if (existingLanRow != null)
            {
                lanRow = existingLanRow.gameObject;
            }
            else
            {
                lanRow = Instantiate(publicRow, container);
                lanRow.name = "Row_LanRoom";
            }

            lanRow.transform.SetSiblingIndex(publicRow.transform.GetSiblingIndex() + 1);

            toggleLanRoom = lanRow.GetComponentInChildren<Toggle>();
            if (toggleLanRoom != null)
            {
                toggleLanRoom.name = "Toggle_Lan";
                toggleLanRoom.onValueChanged.RemoveAllListeners();
                toggleLanRoom.SetIsOnWithoutNotify(false);
                toggleLanRoom.onValueChanged.AddListener(OnLanRoomToggleChanged);
            }

            _lanRoomLabel = lanRow.GetComponentInChildren<TextMeshProUGUI>();
            if (_lanRoomLabel != null)
            {
                _lanRoomLabel.name = "Label_Lan";
                _lanRoomLabel.text = "LAN ROOM (LOCAL WI-FI)";
                _lanRoomLabel.fontSize = 14f;
                _lanRoomLabel.margin = Vector4.zero;
                _lanRoomLabel.alignment = TextAlignmentOptions.MidlineLeft;
            }

            if (togglePublicRoom != null)
            {
                togglePublicRoom.SetIsOnWithoutNotify(true);
            }

            OnLanRoomToggleChanged(false);
        }

        private void OnLanRoomToggleChanged(bool isLan)
        {
            if (togglePublicRoom != null)
            {
                togglePublicRoom.interactable = !isLan;
                var graphics = togglePublicRoom.GetComponentsInChildren<Graphic>(true);
                foreach (var g in graphics)
                {
                    var c = g.color;
                    c.a = isLan ? 0.35f : 1.0f;
                    g.color = c;
                }
            }

            if (_publicRoomLabel != null)
            {
                var c = _publicRoomLabel.color;
                c.a = isLan ? 0.35f : 1.0f;
                _publicRoomLabel.color = c;
            }

            if (inputCreateRoomName != null)
            {
                string text = inputCreateRoomName.text ?? string.Empty;
                if (isLan)
                {
                    if (!text.StartsWith("LAN_", StringComparison.OrdinalIgnoreCase))
                    {
                        if (text.StartsWith("LAN", StringComparison.OrdinalIgnoreCase))
                        {
                            text = "LAN_" + text.Substring(3).TrimStart('_', ' ');
                        }
                        else if (string.IsNullOrWhiteSpace(text))
                        {
                            string hostName = PlayerProfileManager.Instance != null ? PlayerProfileManager.Instance.DisplayName : "Agent";
                            text = $"LAN_{hostName}'s Arena";
                        }
                        else
                        {
                            text = "LAN_" + text;
                        }
                        inputCreateRoomName.text = text;
                    }
                    inputCreateRoomName.caretPosition = inputCreateRoomName.text.Length;
                }
                else
                {
                    if (text.StartsWith("LAN_", StringComparison.OrdinalIgnoreCase))
                    {
                        inputCreateRoomName.text = text.Substring(4);
                    }
                }
            }
        }
    }
}
