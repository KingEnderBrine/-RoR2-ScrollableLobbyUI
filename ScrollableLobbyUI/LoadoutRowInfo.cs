using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    public class LoadoutRowInfo : MonoBehaviour
    {
        public LoadoutPanelController.Row row;
        private GameObject skillGrid;
        private bool couldAcceptInput;
        private bool wasSelected;
        private MPEventSystemLocator locator;
        private GameObject glyph;
        private UILayerKey requiredTopLayer;
        public static LoadoutRowInfo selectedRow;

        private void Start()
        {
            MakeButton();
            SkillCountForGridButton_SettingChanged(null, null);
            ScrollableLobbyUIPlugin.SkillCountForGridButton.SettingChanged += SkillCountForGridButton_SettingChanged;
        }

        private void MakeButton()
        {
            skillGrid = new GameObject("SkillGridButton");
            skillGrid.transform.SetParent(transform, false);

            var skillListButtonRect = skillGrid.AddComponent<RectTransform>();
            skillListButtonRect.anchoredPosition = new Vector2(0, 0);
            skillListButtonRect.anchorMin = new Vector2(0, 0);
            skillListButtonRect.anchorMax = new Vector2(0, 1);
            skillListButtonRect.pivot = new Vector2(0, 0.5F);
            skillListButtonRect.sizeDelta = new Vector2(20, 0);

            var verticalLayout = skillGrid.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(4, 0, 8, 8);
            verticalLayout.spacing = 8;

            var button = new GameObject("Button");
            button.transform.SetParent(skillGrid.transform, false);
            var skillListImage = button.AddComponent<Image>();
            skillListImage.color = Color.clear;

            var skillListLayout = button.AddComponent<LayoutElement>();
            skillListLayout.minWidth = 24;
            skillListLayout.flexibleHeight = 10000;

            var skillListButton = button.AddComponent<HGButton>();
            skillListButton.onClick.AddListener(() => LoadoutSkillGridController.Instance?.Show(row));
            skillListButton.targetGraphic = skillListImage;

            var buttonText = new GameObject("Text");
            buttonText.transform.SetParent(button.transform, false);

            var buttonTextRect = buttonText.AddComponent<RectTransform>();
            buttonTextRect.anchorMin = new Vector2(0, 0);
            buttonTextRect.anchorMax = new Vector2(1, 1);
            buttonTextRect.offsetMin = new Vector2(0, 0);
            buttonTextRect.offsetMax = new Vector2(0, 0);
            buttonTextRect.pivot = new Vector2(0.5f, 0.5f);
            buttonTextRect.Rotate(Vector3.forward, 270);

            var buttonTextText = buttonText.AddComponent<HGTextMeshProUGUI>();
            buttonTextText.text = "···";
            buttonTextText.alignment = TextAlignmentOptions.Center;
            buttonTextText.enableAutoSizing = true;
            buttonTextText.fontSizeMin = 3;
            buttonTextText.fontSizeMax = 72;
            buttonTextText.fontWeight = FontWeight.Bold;
            buttonTextText.fontStyle = FontStyles.Bold;

            var outline = new GameObject("BaseOutline");
            outline.transform.SetParent(button.transform, false);

            var outlineRect = outline.AddComponent<RectTransform>();
            outlineRect.anchorMin = new Vector2(0, 0);
            outlineRect.anchorMax = new Vector2(1, 1);
            outlineRect.offsetMin = new Vector2(0, 0);
            outlineRect.offsetMax = new Vector2(0, 0);
            outlineRect.pivot = new Vector2(0.5f, 0.5f);

            var outlineImage = outline.AddComponent<Image>();
            outlineImage.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIOutlineOnly.png").WaitForCompletion();
            outlineImage.type = Image.Type.Sliced;

            var hover = new GameObject("HoverOutline");
            hover.transform.SetParent(button.transform, false);

            var hoverRect = hover.AddComponent<RectTransform>();
            hoverRect.anchorMin = new Vector2(0, 0);
            hoverRect.anchorMax = new Vector2(1, 1);
            hoverRect.offsetMin = new Vector2(0, -8);
            hoverRect.offsetMax = new Vector2(8, 0);
            hoverRect.pivot = new Vector2(0.5f, 0.5f);

            var hoverImage = hover.AddComponent<Image>();
            hoverImage.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIHighlightBoxOutlineThick.png").WaitForCompletion();
            hoverImage.type = Image.Type.Sliced;

            skillListButton.imageOnHover = hoverImage;
            skillListButton.showImageOnHover = true;

            glyph = new GameObject("Glyph");
            glyph.transform.SetParent(skillGrid.transform, false);
            glyph.layer = 5;
            glyph.transform.SetSiblingIndex(0);
            glyph.SetActive(false);

            var glyphTransform = glyph.AddComponent<RectTransform>();
            glyphTransform.anchoredPosition3D = new Vector3(0, 0, 0);
            glyphTransform.localScale = new Vector3(1, 1, 1);
            glyphTransform.sizeDelta = new Vector2(0, 0);

            var glyphImage = glyph.AddComponent<Image>();
            glyphImage.color = Color.white;
            glyphImage.enabled = false;

            var glyphLayout = glyph.AddComponent<LayoutElement>();
            glyphLayout.minWidth = 24;
            glyphLayout.minHeight = 24;

            var glyphText = new GameObject($"Text");
            glyphText.transform.SetParent(glyph.transform);
            glyphText.layer = 5;

            var glyphTextTransform = glyphText.AddComponent<RectTransform>();
            glyphTextTransform.anchorMin = new Vector2(0, 0);
            glyphTextTransform.anchorMax = new Vector2(1, 1);
            glyphTextTransform.anchoredPosition3D = new Vector3(0, 0, 0);
            glyphTextTransform.localScale = new Vector3(1, 1, 1);
            glyphTextTransform.sizeDelta = new Vector2(24, 24);

            glyphText.AddComponent<MPEventSystemLocator>();

            var inputBindingDisplayController = glyphText.AddComponent<InputBindingDisplayController>();
            inputBindingDisplayController.actionName = "Sprint";
            inputBindingDisplayController.axisRange = Rewired.AxisRange.Full;
            inputBindingDisplayController.useExplicitInputSource = true;
            inputBindingDisplayController.explicitInputSource = MPEventSystem.InputSource.Gamepad;
            inputBindingDisplayController.Awake();

            var glyphTextLayout = glyphText.AddComponent<LayoutElement>();
            glyphTextLayout.preferredHeight = 24;
            glyphTextLayout.preferredWidth = 24;

            var tmpBombDropShadows = Addressables.LoadAssetAsync<TMP_FontAsset>("RoR2/Base/Common/Fonts/Bombardier/tmpbombdropshadow.asset").WaitForCompletion();
            var hgTextMeshPro = glyphText.AddComponent<HGTextMeshProUGUI>();
            hgTextMeshPro.raycastTarget = false;
            hgTextMeshPro.UpdateFontAsset();
            hgTextMeshPro.fontSize = 24;
            hgTextMeshPro.fontSizeMin = 18;
            hgTextMeshPro.fontSizeMax = 72;
            hgTextMeshPro.fontWeight = FontWeight.Regular;
            hgTextMeshPro.alignment = TextAlignmentOptions.Center;
            hgTextMeshPro.wordWrappingRatios = 0.4F;
            hgTextMeshPro.overflowMode = TextOverflowModes.Overflow;
            hgTextMeshPro.enableKerning = true;
            hgTextMeshPro.richText = true;
            hgTextMeshPro.parseCtrlCharacters = true;
            hgTextMeshPro.isOrthographic = true;

            var glyphTMP = new GameObject("TMP SubMeshUI");
            glyphTMP.transform.SetParent(glyphText.transform);
            glyphTMP.layer = 5;

            var glyphTMPTransform = glyphTMP.AddComponent<RectTransform>();
            glyphTMPTransform.anchoredPosition3D = new Vector3(0, 0, 0);
            glyphTMPTransform.localScale = new Vector3(1, 1, 1);
            glyphTMPTransform.sizeDelta = new Vector2(0, 0);

            var material = new Material(Addressables.LoadAssetAsync<Shader>("TextMesh Pro/FormerResources/Shaders/TMP_Sprite.shader").WaitForCompletion());
            var texture = Addressables.LoadAssetAsync<Texture>("TextMesh Pro/FormerResources/Sprite Assets/texXBoxOneGlyphs.png").WaitForCompletion();

            var glyphTMPCanvasRenderer = glyphTMP.AddComponent<CanvasRenderer>();
            glyphTMPCanvasRenderer.SetMaterial(material, texture);

            var glyphTMPSubMesh = glyphTMP.AddComponent<TMP_SubMeshUI>();
            glyphTMPSubMesh.fontAsset = tmpBombDropShadows;
            glyphTMPSubMesh.spriteAsset = Addressables.LoadAssetAsync<TMP_SpriteAsset>("TextMesh Pro/FormerResources/Sprite Assets/tmpsprXboxOneGlyphs.asset").WaitForCompletion();
            glyphTMPSubMesh.m_TextComponent = hgTextMeshPro;

            requiredTopLayer = GetComponentInParent<UILayerKey>();
            if (!TryGetComponent(out locator))
            {
                locator = gameObject.AddComponent<MPEventSystemLocator>();
            }
        }

        private void Update()
        {
            var canAcceptInput = CanAcceptInput();
            var isSelected = this == selectedRow;
            if (couldAcceptInput != canAcceptInput || isSelected != wasSelected)
            {
                glyph.SetActive(canAcceptInput && isSelected);
            }
            couldAcceptInput = canAcceptInput;
            wasSelected = isSelected;
        }

        public bool CanShowGrid()
        {
            return skillGrid.activeSelf;
        }

        protected bool CanAcceptInput()
        {
            if (gameObject.activeInHierarchy && (!requiredTopLayer || requiredTopLayer.representsTopLayer))
            {
                var eventSystem = locator.eventSystem;
                return eventSystem && eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad;
            }

            return false;
        }

        private void OnDestroy()
        {
            ScrollableLobbyUIPlugin.SkillCountForGridButton.SettingChanged -= SkillCountForGridButton_SettingChanged;
            if (selectedRow == this)
            {
                selectedRow = null;
            }
        }

        private void SkillCountForGridButton_SettingChanged(object sender, EventArgs e)
        {
            if (skillGrid)
            {
                skillGrid.SetActive(row.rowData.Count >= ScrollableLobbyUIPlugin.SkillCountForGridButton.Value);
            }
        }
    }
}
