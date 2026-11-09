using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Shootopia
{
    public partial class MainMenuController
    {
public void OpenControlsModal()
        {
            EnsureControlsModalUI();
            if (modalControls != null)
            {
                OpenModal(modalControls, controlsGroup);
            }
        }

        private void EnsureControlsModalUI()
        {
            if (modalControls != null) return;

            Transform canvasTr = modalSettings != null ? modalSettings.transform.parent : transform;
            Transform existing = canvasTr.Find("ModalControlsSheet");
            if (existing != null)
            {
                modalControls = existing.gameObject;
                controlsGroup = modalControls.GetComponent<CanvasGroup>();
                btnCloseControls = modalControls.GetComponentInChildren<Button>();
                return;
            }

            var modalObj = new GameObject("ModalControlsSheet", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            modalObj.transform.SetParent(canvasTr, false);
            modalObj.transform.SetAsLastSibling();

            var mRt = modalObj.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(0.5f, 0.5f);
            mRt.anchorMax = new Vector2(0.5f, 0.5f);
            mRt.sizeDelta = new Vector2(1060f, 660f);
            mRt.anchoredPosition = Vector2.zero;

            var mImg = modalObj.GetComponent<Image>();
            mImg.color = new Color(0.18f, 0.72f, 1.0f, 0.45f);

            controlsGroup = modalObj.GetComponent<CanvasGroup>();

            var backdropObj = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdropObj.transform.SetParent(modalObj.transform, false);
            var bRt = backdropObj.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.sizeDelta = new Vector2(-16f, -16f);
            bRt.anchoredPosition = Vector2.zero;
            var bImg = backdropObj.GetComponent<Image>();
            bImg.color = new Color(0.04f, 0.07f, 0.14f, 0.96f);
            bImg.raycastTarget = false;

            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(modalObj.transform, false);
            var tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 1f);
            tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.sizeDelta = new Vector2(980f, 40f);
            tRt.anchoredPosition = new Vector2(0f, -22f);

            var tTmp = titleObj.GetComponent<TextMeshProUGUI>();
            tTmp.text = "CONTROLS & KEYBINDINGS";
            tTmp.fontSize = 28;
            tTmp.fontStyle = FontStyles.Bold;
            tTmp.color = new Color(0.92f, 0.97f, 1f, 1f);
            tTmp.alignment = TextAlignmentOptions.Center;

            var subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(modalObj.transform, false);
            var sRt = subObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 1f);
            sRt.anchorMax = new Vector2(0.5f, 1f);
            sRt.pivot = new Vector2(0.5f, 1f);
            sRt.sizeDelta = new Vector2(980f, 24f);
            sRt.anchoredPosition = new Vector2(0f, -60f);

            var sTmp = subObj.GetComponent<TextMeshProUGUI>();
            sTmp.text = "FULL INPUT SPECIFICATION • KEYBOARD / MOUSE & GAMEPAD";
            sTmp.fontSize = 14;
            sTmp.fontStyle = FontStyles.Normal;
            sTmp.color = new Color(0.6f, 0.78f, 0.95f, 1f);
            sTmp.alignment = TextAlignmentOptions.Center;

            var sheetContainer = new GameObject("SheetContainer", typeof(RectTransform));
            sheetContainer.transform.SetParent(modalObj.transform, false);
            var scRt = sheetContainer.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.5f, 0.5f);
            scRt.anchorMax = new Vector2(0.5f, 0.5f);
            scRt.sizeDelta = new Vector2(1000f, 480f);
            scRt.anchoredPosition = new Vector2(0f, -10f);

            TacticalControlsSheetBuilder.BuildControlsSheetView(sheetContainer.transform, tTmp.font);

            var closeBtnObj = new GameObject("BtnCloseControls", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIJuice));
            closeBtnObj.transform.SetParent(modalObj.transform, false);
            var cbRt = closeBtnObj.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.5f, 0f);
            cbRt.anchorMax = new Vector2(0.5f, 0f);
            cbRt.pivot = new Vector2(0.5f, 0f);
            cbRt.sizeDelta = new Vector2(220f, 48f);
            cbRt.anchoredPosition = new Vector2(0f, 20f);

            var cbImg = closeBtnObj.GetComponent<Image>();
            cbImg.color = new Color(0.08f, 0.12f, 0.22f, 0.95f);

            btnCloseControls = closeBtnObj.GetComponent<Button>();
            btnCloseControls.onClick.AddListener(CloseTopModal);

            var cbTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            cbTxtObj.transform.SetParent(closeBtnObj.transform, false);
            var cbtRt = cbTxtObj.GetComponent<RectTransform>();
            cbtRt.anchorMin = Vector2.zero;
            cbtRt.anchorMax = Vector2.one;
            cbtRt.sizeDelta = Vector2.zero;

            var cbtTmp = cbTxtObj.GetComponent<TextMeshProUGUI>();
            cbtTmp.text = "BACK TO SETTINGS";
            cbtTmp.fontSize = 16;
            cbtTmp.fontStyle = FontStyles.Bold;
            cbtTmp.color = new Color(0.2f, 0.85f, 1.0f, 1f);
            cbtTmp.alignment = TextAlignmentOptions.Center;

            modalControls = modalObj;
            modalControls.SetActive(false);
        }
    }
}
