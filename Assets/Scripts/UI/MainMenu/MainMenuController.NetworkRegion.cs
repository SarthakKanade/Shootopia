using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class MainMenuController
    {
private void UpdateChipVisuals()
        {
            SetChipActive(chipQuickAll, _quickMatchMode == "ALL");
            SetChipActive(chipQuickFFA, _quickMatchMode == "FFA");
            SetChipActive(chipQuickTDM, _quickMatchMode == "TDM");

            SetChipActive(chipQuickMapAll, _quickMatchMap == "ALL");
            SetChipActive(chipQuickMapSectorAlpha, _quickMatchMap == "Map1");
            SetChipActive(chipQuickMapGasStation, _quickMatchMap == "GasStationMap");

            SetChipActive(chipModeFFA, _selectedCreateMode == "FFA");
            SetChipActive(chipModeTDM, _selectedCreateMode == "TDM");

            SetChipActive(chipMapSectorAlpha, _selectedCreateMap == "Map1");
            SetChipActive(chipMapGasStation, _selectedCreateMap == "GasStationMap");

            SetChipActive(chipFilterAll, _browserModeFilter == "ALL");
            SetChipActive(chipFilterFFA, _browserModeFilter == "FFA");
            SetChipActive(chipFilterTDM, _browserModeFilter == "TDM");

            SetChipActive(btnSkinAgent, _selectedSkin == 0);
            SetChipActive(btnSkinRobot, _selectedSkin == 1);

            if (btnCustomize != null)
            {
                var custTxt = btnCustomize.GetComponentInChildren<TextMeshProUGUI>();
                if (custTxt != null)
                {
                    string opName = _selectedSkin == 1 ? "VALKYRIE" : "VANGUARD";
                    custTxt.text = $"{opName} [C]";
                }
            }

            // Server Region Visuals
            string currentPref = PlayerPrefs.GetString("Shootopia_PreferredRegion", "in");
            bool isAuto = !string.IsNullOrEmpty(currentPref) && currentPref.Equals("auto", StringComparison.OrdinalIgnoreCase);
            SetChipActive(chipRegionAuto, isAuto);
            SetChipActive(chipRegionIndia, !isAuto && currentPref.Equals("in", StringComparison.OrdinalIgnoreCase));
            SetChipActive(chipRegionAsia, !isAuto && currentPref.Equals("asia", StringComparison.OrdinalIgnoreCase));
            SetChipActive(chipRegionEurope, !isAuto && currentPref.Equals("eu", StringComparison.OrdinalIgnoreCase));
            SetChipActive(chipRegionUSEast, !isAuto && currentPref.Equals("us", StringComparison.OrdinalIgnoreCase));

            string displayRegion = "INDIA";
            if (isAuto)
            {
                displayRegion = "AUTO";
            }
            else
            {
                if (currentPref.Equals("in", StringComparison.OrdinalIgnoreCase)) displayRegion = "INDIA";
                else if (currentPref.Equals("asia", StringComparison.OrdinalIgnoreCase)) displayRegion = "ASIA";
                else if (currentPref.Equals("eu", StringComparison.OrdinalIgnoreCase)) displayRegion = "EUROPE";
                else if (currentPref.Equals("us", StringComparison.OrdinalIgnoreCase)) displayRegion = "US EAST";
            }

            if (topRegionLabel != null)
            {
                topRegionLabel.text = displayRegion;
            }

            if (regionStatusText != null)
            {
                if (isAuto)
                {
                    regionStatusText.text = "CURRENT: AUTO (LOWEST LATENCY)";
                }
                else if (currentPref.Equals("in", StringComparison.OrdinalIgnoreCase))
                {
                    regionStatusText.text = "CURRENT: INDIA (MUMBAI - ASIA-SOUTH1)";
                }
                else if (currentPref.Equals("asia", StringComparison.OrdinalIgnoreCase))
                {
                    regionStatusText.text = "CURRENT: ASIA (SINGAPORE / EAST)";
                }
                else if (currentPref.Equals("eu", StringComparison.OrdinalIgnoreCase))
                {
                    regionStatusText.text = "CURRENT: EUROPE (CENTRAL / WEST)";
                }
                else if (currentPref.Equals("us", StringComparison.OrdinalIgnoreCase))
                {
                    regionStatusText.text = "CURRENT: US EAST (VIRGINIA)";
                }
            }
        }

        public void SelectRegion(string code)
        {
            PlayerPrefs.SetString("Shootopia_PreferredRegion", code);
            PlayerPrefs.Save();

            UpdateChipVisuals();
            PlayClickSFX();
            CloseTopModal();
            Debug.Log($"[MainMenu] Selected Preferred Server Region: '{(string.IsNullOrEmpty(code) ? "AUTO" : code.ToUpper())}'");
        }

        private void PlayClickSFX()
        {
            if (AudioManager.Instance != null && buttonClickClip != null)
            {
                AudioManager.Instance.PlaySFX2D(buttonClickClip, 1.0f);
            }
        }

        private void SetChipActive(Button chip, bool isActive)
        {
            if (chip == null) return;

            // Check for Rich Map Card elements (card background border, checkmark badge, title)
            var checkmark = chip.transform.Find("CheckmarkBadge");
            var title = chip.transform.Find("MaskContainer/TextContainer/TitleText")?.GetComponent<TextMeshProUGUI>();

            if (checkmark != null || title != null)
            {
                var cardBg = chip.GetComponent<Image>();
                if (cardBg != null)
                {
                    cardBg.color = isActive ? new Color(0.0f, 0.95f, 1.0f, 1.0f) : new Color(0.22f, 0.26f, 0.38f, 0.7f);
                }
                if (checkmark != null) checkmark.gameObject.SetActive(isActive);
                if (title != null) title.color = isActive ? new Color(1.0f, 0.92f, 0.4f, 1.0f) : Color.white;
                return;
            }

            var img = chip.GetComponent<Image>();
            if (img != null)
            {
                img.color = isActive ? new Color(1f, 0.9f, 0.4f, 1f) : new Color(0.7f, 0.7f, 0.8f, 0.85f);
            }
        }
    }
}
