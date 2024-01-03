using RoR2;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    [RequireComponent(typeof(CharacterSelectBarController))]
    public class CharacterSelectBarControllerExtra : MonoBehaviour
    {
        private const int iconSize = 70;
        private const int iconSpacing = 4;
        private const int iconPadding = 6;
        private const int buttonSide = 37;

        private int SurvivorRows => ScrollableLobbyUIPlugin.CharacterSelectRows.Value;
        private int SurvivorsPerRow { get; set; } = !ScrollableLobbyUIPlugin.InlinePageArrows.Value ? 1 :
            ScrollableLobbyUIPlugin.CharacterSelectRows.Value switch
            {
                1 => 3,
                2 => 2,
                _ => 1
            };
        private int SurvivorsPerPage => SurvivorsPerRow * SurvivorRows;
        private int ContainerHeight => (iconSize + iconSpacing) * SurvivorRows - iconSpacing + iconPadding * 2;

        private CharacterSelectBarController characterSelectBar;
        private HGButtonHistory buttonHistory;
        private GridLayoutGroup IconContainerGrid => characterSelectBar.iconContainerGrid;
        private UIElementAllocator<SurvivorIconController> SurvivorIconControllers => characterSelectBar.survivorIconControllers;
        private UIElementAllocator<RectTransform> FillerIconControllers => characterSelectBar.fillerIcons;
        private MPEventSystemLocator EventSystemLocator => characterSelectBar.eventSystemLocator;
        private LocalUser LocalUser => (EventSystem.current as MPEventSystem)?.localUser;

        private int fillerCount;
        private SurvivorDef pickedSurvivor;

        private readonly List<LayoutElement> trackingHeightElements = new List<LayoutElement>();
        private readonly List<SurvivorDef> survivorDefList = new List<SurvivorDef>();

        private HGButton arrowPreviousButtonComponent;
        private HGButton arrowNextButtonComponent;

        private HGButton inlinePreviousButtonComponent;
        private HGButton inlineNextButtonComponent;

        public int PageCount { get; private set; } = 1;
        public int CurrentPageIndex { get; private set; } = 0;
        public bool IsOnFirstPage => CurrentPageIndex == 0;
        public bool IsOnLastPage => PageCount == CurrentPageIndex + 1;

        private void RebuildPage()
        {
            characterSelectBar.pickedIcon = null;
            IList<SurvivorDef> survivorDefs;

            if (ScrollableLobbyUIPlugin.InlinePageArrows.Value)
            {
                if (PageCount == 1)
                {
                    survivorDefs = survivorDefList;
                    inlinePreviousButtonComponent.gameObject.SetActive(false);
                    inlineNextButtonComponent.gameObject.SetActive(false);
                }
                else if (IsOnFirstPage)
                {
                    survivorDefs = survivorDefList.Take(SurvivorsPerPage - 1).ToArray();

                    inlinePreviousButtonComponent.gameObject.SetActive(false);
                    inlineNextButtonComponent.gameObject.SetActive(true);
                }
                else
                {
                    inlinePreviousButtonComponent.gameObject.SetActive(true);
                    inlineNextButtonComponent.gameObject.SetActive(!IsOnLastPage);

                    var page = SurvivorsPerPage - 2;
                    survivorDefs = survivorDefList.Skip((SurvivorsPerPage - 1) + page * (CurrentPageIndex - 1)).Take(IsOnLastPage ? page + 1 : page).ToArray();
                }

                inlinePreviousButtonComponent.transform.SetSiblingIndex(0);

                var newSiblingIngex = FillerIconControllers.elements.FirstOrDefault()?.GetSiblingIndex() ?? inlineNextButtonComponent.transform.parent.childCount;
                inlineNextButtonComponent.transform.SetSiblingIndex(newSiblingIngex);
            }
            else
            {
                survivorDefs = survivorDefList.Skip(CurrentPageIndex * SurvivorsPerPage).Take(SurvivorsPerPage).ToArray();
                if (arrowPreviousButtonComponent && arrowNextButtonComponent)
                {
                    arrowPreviousButtonComponent.interactable = !IsOnFirstPage;
                    arrowNextButtonComponent.interactable = !IsOnLastPage;
                }
            }

            var elements = SurvivorIconControllers.elements;
            for (var index = 0; index < elements.Count; ++index)
            {
                var element = elements[index];

                if (index >= survivorDefs.Count)
                {
                    element.gameObject.SetActive(false);
                    continue;
                }

                element.gameObject.SetActive(true);

                var survivorDef = survivorDefs[index];
                element.survivorDef = survivorDef;

                if (pickedSurvivor == survivorDef)
                {
                    characterSelectBar.pickedIcon = element;
                }
            }

            foreach (var fillerIcon in FillerIconControllers.elements)
            {
                fillerIcon.gameObject.SetActive(IsOnLastPage);
            }

            if (buttonHistory &&
                buttonHistory.lastRememberedGameObject &&
                EventSystemLocator &&
                EventSystemLocator.eventSystem)
            {
                if (buttonHistory.lastRememberedGameObject.activeInHierarchy)
                {
                    buttonHistory.lastRememberedGameObject.GetComponent<HGButton>().OnSelect(new BaseEventData(EventSystem.current));
                }
                else
                {
                    elements.LastOrDefault(el => el.gameObject.activeInHierarchy)?.GetComponent<HGButton>().Select();
                }
            }
        }

        private void GatherSurvivorsInfo()
        {
            if (survivorDefList.Count > 0)
            {
                return;
            }

            foreach (var survivorDef in SurvivorCatalog.orderedSurvivorDefs)
            {
                if (characterSelectBar.ShouldDisplaySurvivor(survivorDef))
                {
                    survivorDefList.Add(survivorDef);
                }
            }
        }

        private void PrepareContainer()
        {
            //Removing this component because of strictly defined cell size
            DestroyImmediate(GetComponent<AdjustGridLayoutCellSize>());

            //Updating layout
            var layoutElement = GetComponent<LayoutElement>();
            trackingHeightElements.Add(layoutElement);
            layoutElement.preferredWidth = float.MaxValue;

            var choiceContainerLayout = transform.parent.GetComponent<LayoutElement>() ?? transform.parent.gameObject.AddComponent<LayoutElement>();
            trackingHeightElements.Add(choiceContainerLayout);

            var choiceContainerContentFitter = transform.parent.gameObject.AddComponent<ContentSizeFitter>();
            choiceContainerContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            choiceContainerContentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var choiceGridContainer = new GameObject("SurvivorChoiceGridContainer");
            choiceGridContainer.transform.SetParent(transform.parent, false);
            choiceGridContainer.transform.SetSiblingIndex(2);
            transform.SetParent(choiceGridContainer.transform, false);

            var choiceGridHorizontalLayout = choiceGridContainer.AddComponent<HorizontalLayoutGroup>();
            var choiceGridLayout = choiceGridContainer.AddComponent<LayoutElement>();
            trackingHeightElements.Add(choiceGridLayout);

            //Define cell size (previous value: (-5, 70))
            IconContainerGrid.cellSize = new Vector2(iconSize, iconSize);
            IconContainerGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            IconContainerGrid.childAlignment = TextAnchor.MiddleCenter;

            var rightPanel = transform.parent?.parent?.parent;
            var rightPanelVerticalLayout = rightPanel.GetComponent<VerticalLayoutGroup>();
            if (rightPanelVerticalLayout)
            {
                var padding = rightPanelVerticalLayout.padding;
                rightPanelVerticalLayout.padding = new RectOffset(padding.left, padding.right, 0, padding.bottom);
            }
        }

        private void AllocateCells()
        {
            SurvivorIconControllers.AllocateElements(Mathf.Min(SurvivorsPerPage, survivorDefList.Count));
            var first = SurvivorIconControllers.elements.FirstOrDefault();
            if (first)
            {
                first.GetComponent<MPButton>().defaultFallbackButton = true;
            }
            FillerIconControllers.AllocateElements(fillerCount);
            FillerIconControllers.MoveElementsToContainerEnd();
        }

        private void SetupPagingStuff()
        {
            var mpEventSystemLocator = GetComponent<MPEventSystemLocator>();
            var survivorChoiceGrid = transform.parent.gameObject;
            var uiLayerKey = survivorChoiceGrid.GetComponentInParent<UILayerKey>();

            arrowPreviousButtonComponent = SetupPagingButton("Previous", "Left", SelectPreviousPage, nameof(RewiredConsts.Action.UITabLeft), 0, 2);
            arrowNextButtonComponent = SetupPagingButton("Next", "Right", SelectNextPage, nameof(RewiredConsts.Action.UITabRight), 10, 6);

            inlinePreviousButtonComponent = SetupPagingButtonCharacterSlot("Previous", SelectPreviousPage, nameof(RewiredConsts.Action.UITabLeft), 0, 2, mpEventSystemLocator, survivorChoiceGrid, uiLayerKey);
            inlineNextButtonComponent = SetupPagingButtonCharacterSlot("Next", SelectNextPage, nameof(RewiredConsts.Action.UITabRight), 10, 6, mpEventSystemLocator, survivorChoiceGrid, uiLayerKey);

            HGButton SetupPagingButton(string prefix, string buttonPrefix, System.Action action, string actionName, int siblingIndex, int glyphIndex)
            {
                var buttonContainer = new GameObject($"{prefix}ButtonContainer");
                buttonContainer.transform.SetParent(transform.parent, false);
                buttonContainer.transform.SetSiblingIndex(siblingIndex);
                buttonContainer.layer = 5;

                var buttonVerticalLayout = buttonContainer.AddComponent<VerticalLayoutGroup>();
                buttonVerticalLayout.childControlHeight = true;
                buttonVerticalLayout.childControlWidth = true;

                var buttonContainerLayout = buttonContainer.AddComponent<LayoutElement>();
                trackingHeightElements.Add(buttonContainerLayout);

                var button = Instantiate(Addressables.LoadAssetAsync<GameObject>($"RoR2/Base/UI/{buttonPrefix}Button.prefab").WaitForCompletion(), buttonContainer.transform, false);
                button.name = $"{prefix}Page";
                button.layer = 5;

                var buttonComponent = button.GetComponent<HGButton>();
                buttonComponent.onClick = new Button.ButtonClickedEvent();
                buttonComponent.onClick.AddListener(new UnityEngine.Events.UnityAction(action));

                var buttonLayout = button.GetComponent<LayoutElement>();
                buttonLayout.preferredWidth = buttonSide;
                buttonLayout.minWidth = buttonSide;

                var glyph = new GameObject($"{prefix}Glyph");
                glyph.transform.SetParent(buttonContainer.transform, false);
                glyph.layer = 5;
                glyph.transform.SetSiblingIndex(0);
                glyph.SetActive(mpEventSystemLocator.eventSystem && mpEventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad);

                var glyphTransform = glyph.AddComponent<RectTransform>();
                glyphTransform.anchoredPosition3D = new Vector3(0, 0, 0);
                glyphTransform.localScale = new Vector3(1, 1, 1);
                glyphTransform.sizeDelta = new Vector2(0, 0);

                var glyphImage = glyph.AddComponent<Image>();
                glyphImage.color = Color.white;
                glyphImage.enabled = false;

                var glyphLayout = glyph.AddComponent<LayoutElement>();
                glyphLayout.minWidth = buttonSide;

                var glyphText = new GameObject($"Text");
                glyphText.transform.SetParent(glyph.transform);
                glyphText.layer = 5;

                var glyphTextTransform = glyphText.AddComponent<RectTransform>();
                glyphTextTransform.anchorMin = new Vector2(0, 0);
                glyphTextTransform.anchorMax = new Vector2(1, 1);
                glyphTextTransform.anchoredPosition3D = new Vector3(0, 0, 0);
                glyphTextTransform.localScale = new Vector3(1, 1, 1);
                glyphTextTransform.sizeDelta = new Vector2(48, 48);

                glyphText.AddComponent<MPEventSystemLocator>();

                var inputBindingDisplayController = glyphText.AddComponent<InputBindingDisplayController>();
                inputBindingDisplayController.actionName = actionName;
                inputBindingDisplayController.axisRange = Rewired.AxisRange.Full;
                inputBindingDisplayController.useExplicitInputSource = true;
                inputBindingDisplayController.explicitInputSource = MPEventSystem.InputSource.Gamepad;
                inputBindingDisplayController.Awake();


                var glyphTextLayout = glyphText.AddComponent<LayoutElement>();
                glyphTextLayout.preferredHeight = buttonSide;
                glyphTextLayout.preferredWidth = buttonSide;

                var tmpBombDropShadows = Addressables.LoadAssetAsync<TMP_FontAsset>("RoR2/Base/Common/Fonts/Bombardier/tmpbombdropshadow.asset").WaitForCompletion();
                var hgTextMeshPro = glyphText.AddComponent<HGTextMeshProUGUI>();
                hgTextMeshPro.raycastTarget = false;
                hgTextMeshPro.text = $"<sprite=\"tmpsprXboxOneGlyphs\" name=\"texXBoxOneGlyphs_{glyphIndex}\">";
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

                var material = new Material(Addressables.LoadAssetAsync<Shader>("TextMesh Pro/TMP_Sprite.shader").WaitForCompletion());
                var texture = Addressables.LoadAssetAsync<Texture>("TextMesh Pro/texXBoxOneGlyphs.png").WaitForCompletion();

                var glyphTMPCanvasRenderer = glyphTMP.AddComponent<CanvasRenderer>();
                glyphTMPCanvasRenderer.SetMaterial(material, texture);

                var glyphTMPSubMesh = glyphTMP.AddComponent<TMP_SubMeshUI>();
                glyphTMPSubMesh.fontAsset = tmpBombDropShadows;
                glyphTMPSubMesh.spriteAsset = Addressables.LoadAssetAsync<TMP_SpriteAsset>("TextMesh Pro/tmpsprXboxOneGlyphs.asset").WaitForCompletion();
                glyphTMPSubMesh.m_TextComponent = hgTextMeshPro;

                var pageEvent = survivorChoiceGrid.AddComponent<HGGamepadInputEvent>();
                pageEvent.requiredTopLayer = uiLayerKey;
                pageEvent.actionName = actionName;
                pageEvent.enabledObjectsIfActive = new GameObject[] { glyph };
                pageEvent.actionEvent = new UnityEngine.Events.UnityEvent();
                pageEvent.actionEvent.AddListener(new UnityEngine.Events.UnityAction(action));

                return buttonComponent;
            }

            HGButton SetupPagingButtonCharacterSlot(string prefix, System.Action action, string actionName, int siblingIndex, int glyphIndex, MPEventSystemLocator mpEventSystemLocator, GameObject survivorChoiceGrid, UILayerKey uiLayerKey)
            {
                GameObject buttonHolder = new GameObject($"{prefix}ButtonContainer");
                buttonHolder.transform.SetParent(transform, false);
                buttonHolder.transform.SetSiblingIndex(siblingIndex);
                buttonHolder.layer = 5;

                var holderRectTransform = buttonHolder.AddComponent<RectTransform>();
                holderRectTransform.sizeDelta = new Vector2(48, 48);
                buttonHolder.AddComponent<CanvasRenderer>();

                var holderImage = buttonHolder.AddComponent<Image>();
                holderImage.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUICleanButton.png").WaitForCompletion();
                holderImage.overrideSprite = holderImage.sprite;
                holderImage.type = Image.Type.Sliced;
                holderImage.fillMethod = Image.FillMethod.Radial360;

                var button = buttonHolder.AddComponent<HGButton>();
                button.image = holderImage;
                button.targetGraphic = holderImage;

                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(new UnityEngine.Events.UnityAction(action));

                button.interactable = true;

                var buttonAnimationTriggers = button.animationTriggers;
                buttonAnimationTriggers.normalTrigger = "Normal";
                buttonAnimationTriggers.highlightedTrigger = "Highlighted";
                buttonAnimationTriggers.pressedTrigger = "Pressed";
                buttonAnimationTriggers.selectedTrigger = "Highlighted";
                buttonAnimationTriggers.disabledTrigger = "Disabled";

                var buttonSkinController = buttonHolder.AddComponent<ButtonSkinController>();
                buttonSkinController.useRecommendedImage = true;
                buttonSkinController.useRecommendedMaterial = true;
                buttonSkinController.useRecommendedAlignment = true;
                buttonSkinController.useRecommendedLabel = true;
                buttonSkinController.useRecommendedButtonHeight = false;
                buttonSkinController.useRecommendedButtonWidth = false;
                buttonSkinController.skinData = Addressables.LoadAssetAsync<UISkinData>("RoR2/Base/UI/skinCleanButton.asset").WaitForCompletion();

                var interactableHighlight = new GameObject("InteractableHighlight");
                interactableHighlight.transform.SetParent(buttonHolder.transform, false);
                interactableHighlight.layer = 5;

                var interactableHighlighRectTransform = interactableHighlight.AddComponent<RectTransform>();
                interactableHighlighRectTransform.sizeDelta = new Vector2(72, 72);
                interactableHighlight.AddComponent<CanvasRenderer>();

                var interactableHighlightImage = interactableHighlight.AddComponent<Image>();
                interactableHighlightImage.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIOutlineOnly.png").WaitForCompletion();
                interactableHighlightImage.overrideSprite = interactableHighlightImage.sprite;
                interactableHighlightImage.type = Image.Type.Sliced;
                interactableHighlightImage.fillMethod = Image.FillMethod.Radial360;

                var hoverHighlight = new GameObject("HoverHighlight");
                hoverHighlight.transform.SetParent(buttonHolder.transform, false);
                hoverHighlight.layer = 5;

                var hoverHighlightRectTransform = hoverHighlight.AddComponent<RectTransform>();
                hoverHighlightRectTransform.sizeDelta = new Vector2(8, 8);
                hoverHighlightRectTransform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                hoverHighlightRectTransform.anchoredPosition = new Vector2(4, -4);
                hoverHighlightRectTransform.anchorMin = Vector2.zeroVector;
                hoverHighlightRectTransform.anchorMax = Vector2.oneVector;
                hoverHighlightRectTransform.offsetMin = new Vector2(0, -8);
                hoverHighlightRectTransform.offsetMax = new Vector2(8, 0);

                hoverHighlight.AddComponent<Canvas>();
                var refreshCanvasDrawOrder = hoverHighlight.AddComponent<RefreshCanvasDrawOrder>();
                refreshCanvasDrawOrder.canvasSortingOrderDelta = 1;

                hoverHighlight.AddComponent<CanvasRenderer>();

                var hoverHighlightImage = hoverHighlight.AddComponent<Image>();
                hoverHighlightImage.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIHighlightBoxOutlineThick.png").WaitForCompletion();
                hoverHighlightImage.overrideSprite = hoverHighlightImage.sprite;
                hoverHighlightImage.type = Image.Type.Sliced;
                hoverHighlightImage.fillMethod = Image.FillMethod.Radial360;

                var text = new GameObject("ArrowText");
                text.transform.SetParent(buttonHolder.transform, false);
                text.layer = 5;

                var textRectTransform = text.AddComponent<RectTransform>();
                textRectTransform.anchoredPosition = new Vector2(0, 2);

                text.AddComponent<CanvasRenderer>();

                var textMeshProUGUI = text.AddComponent<HGTextMeshProUGUI>();
                if (prefix == "Next")
                {
                    textMeshProUGUI.text = ">";
                }
                else
                {
                    textMeshProUGUI.text = "<";
                }

                textMeshProUGUI.fontSize = 56;
                textMeshProUGUI.fontSizeMin = 55;
                textMeshProUGUI.fontSizeMax = 57;
                textMeshProUGUI.enableKerning = true;
                textMeshProUGUI.enableAutoSizing = true;

                textMeshProUGUI.raycastTarget = false;

                button.imageOnHover = hoverHighlightImage;
                button.imageOnInteractable = interactableHighlightImage;
                button.allowAllEventSystems = true;
                button.submitOnPointerUp = true;

                var colors = button.colors;
                colors.normalColor = new Color32(83, 103, 120, 255);
                colors.highlightedColor = new Color32(252, 255, 177, 187);
                colors.pressedColor = new Color32(189, 192, 113, 251);
                colors.selectedColor = new Color32(252, 255, 177, 187);
                colors.disabledColor = new Color32(65, 51, 51, 182);
                colors.colorMultiplier = 1;
                colors.fadeDuration = 0;
                button.colors = colors;

                button.showImageOnHover = true;

                var glyph = new GameObject($"{prefix}Glyph");
                glyph.transform.SetParent(buttonHolder.transform, false);
                glyph.layer = 5;
                glyph.transform.SetSiblingIndex(0);
                glyph.SetActive(mpEventSystemLocator.eventSystem && mpEventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad);

                var glyphTransform = glyph.AddComponent<RectTransform>();
                glyphTransform.anchoredPosition3D = new Vector3(0, 0, 0);
                glyphTransform.localScale = new Vector3(1, 1, 1);
                glyphTransform.sizeDelta = new Vector2(0, 0);

                var glyphImage = glyph.AddComponent<Image>();
                glyphImage.color = Color.white;
                glyphImage.enabled = false;

                var glyphLayout = glyph.AddComponent<LayoutElement>();
                glyphLayout.minWidth = buttonSide;

                var glyphText = new GameObject($"Text");
                glyphText.transform.SetParent(glyph.transform);
                glyphText.layer = 5;

                var glyphTextTransform = glyphText.AddComponent<RectTransform>();
                glyphTextTransform.anchorMin = new Vector2(0, 0);
                glyphTextTransform.anchorMax = new Vector2(1, 1);
                glyphTextTransform.anchoredPosition3D = new Vector3(0, 0, 0);
                glyphTextTransform.localScale = new Vector3(1, 1, 1);
                glyphTextTransform.sizeDelta = new Vector2(48, 48);

                glyphText.AddComponent<MPEventSystemLocator>();

                var inputBindingDisplayController = glyphText.AddComponent<InputBindingDisplayController>();
                inputBindingDisplayController.actionName = actionName;
                inputBindingDisplayController.axisRange = Rewired.AxisRange.Full;
                inputBindingDisplayController.useExplicitInputSource = true;
                inputBindingDisplayController.explicitInputSource = MPEventSystem.InputSource.Gamepad;
                inputBindingDisplayController.Awake();


                var glyphTextLayout = glyphText.AddComponent<LayoutElement>();
                glyphTextLayout.preferredHeight = buttonSide;
                glyphTextLayout.preferredWidth = buttonSide;

                var tmpBombDropShadows = Addressables.LoadAssetAsync<TMP_FontAsset>("RoR2/Base/Common/Fonts/Bombardier/tmpbombdropshadow.asset").WaitForCompletion();
                var hgTextMeshPro = glyphText.AddComponent<HGTextMeshProUGUI>();
                hgTextMeshPro.raycastTarget = false;
                hgTextMeshPro.text = $"<sprite=\"tmpsprXboxOneGlyphs\" name=\"texXBoxOneGlyphs_{glyphIndex}\">";
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

                var material = new Material(Addressables.LoadAssetAsync<Shader>("TextMesh Pro/TMP_Sprite.shader").WaitForCompletion());
                var texture = Addressables.LoadAssetAsync<Texture>("TextMesh Pro/texXBoxOneGlyphs.png").WaitForCompletion();

                var glyphTMPCanvasRenderer = glyphTMP.AddComponent<CanvasRenderer>();
                glyphTMPCanvasRenderer.SetMaterial(material, texture);

                var glyphTMPSubMesh = glyphTMP.AddComponent<TMP_SubMeshUI>();
                glyphTMPSubMesh.fontAsset = tmpBombDropShadows;
                glyphTMPSubMesh.spriteAsset = Addressables.LoadAssetAsync<TMP_SpriteAsset>("TextMesh Pro/tmpsprXboxOneGlyphs.asset").WaitForCompletion();
                glyphTMPSubMesh.m_TextComponent = hgTextMeshPro;

                var pageEvent = survivorChoiceGrid.AddComponent<HGGamepadInputEvent>();
                pageEvent.requiredTopLayer = uiLayerKey;
                pageEvent.actionName = actionName;
                pageEvent.enabledObjectsIfActive = new GameObject[] { glyph };
                pageEvent.actionEvent = new UnityEngine.Events.UnityEvent();

                var vat = glyph.AddComponent<VariantArrowToggler>();
                vat.arrow = text;

                return button;
            }
        }

        private void Awake()
        {
            buttonHistory = GetComponent<HGButtonHistory>();
            characterSelectBar = GetComponent<CharacterSelectBarController>();

            characterSelectBar.onSurvivorPicked.AddListener((survivorInfo) => pickedSurvivor = survivorInfo.pickedSurvivor);

            PrepareContainer();
            SetupPagingStuff();
            UpdateHeights();

            SurvivorsPerRow = CalculateSurvivorsPerRow();
        }

        private void OnEnable()
        {
            ScrollableLobbyUIPlugin.CharacterSelectRows.SettingChanged += OnCharacterSelectRowsSettingsChanged;
            ScrollableLobbyUIPlugin.InlinePageArrows.SettingChanged += OnInlinePageArrowsSettingsChanged;
        }

        private void OnDisable()
        {
            ScrollableLobbyUIPlugin.CharacterSelectRows.SettingChanged -= OnCharacterSelectRowsSettingsChanged;
            ScrollableLobbyUIPlugin.InlinePageArrows.SettingChanged -= OnInlinePageArrowsSettingsChanged;
        }

        private void OnCharacterSelectRowsSettingsChanged(object sender, EventArgs e)
        {
            var survivorDef = SurvivorIconControllers.elements.FirstOrDefault()?.survivorDef;
            var index = survivorDefList.IndexOf(survivorDef);

            if (index == -1)
            {
                CurrentPageIndex = 0;
            }
            else
            {
                var firstIconIndex = RecalculateSurvivorIndexWithInlineButtons(index);
                CurrentPageIndex = firstIconIndex / SurvivorsPerPage;
            }

            Build();
        }

        private void OnInlinePageArrowsSettingsChanged(object sender, EventArgs e)
        {
            int newSurvivorsPerRow = CalculateSurvivorsPerRow();

            var previousFirstIconIndex = CurrentPageIndex * SurvivorsPerPage;
            SurvivorsPerRow = newSurvivorsPerRow;
            CurrentPageIndex = previousFirstIconIndex / SurvivorsPerPage;

            Build();
        }

        private void Update()
        {
            int newSurvivorsPerRow = CalculateSurvivorsPerRow();

            if (newSurvivorsPerRow != SurvivorsPerRow)
            {
                var previousFirstIconIndex = CurrentPageIndex * SurvivorsPerPage;
                SurvivorsPerRow = newSurvivorsPerRow;
                CurrentPageIndex = previousFirstIconIndex / SurvivorsPerPage;

                Build();
            }
        }

        private int CalculateSurvivorsPerRow()
        {
            var containerWidth = (IconContainerGrid.transform.parent.parent.parent as RectTransform).rect.width;
            var tempPadding = ScrollableLobbyUIPlugin.InlinePageArrows.Value ? 0 : iconPadding * 2;
            var newSurvivorsPerRow = Math.Max(1, (int)(containerWidth + iconSpacing - tempPadding) / (iconSize + iconSpacing));

            if (!ScrollableLobbyUIPlugin.InlinePageArrows.Value)
            {
                if (newSurvivorsPerRow * SurvivorRows < survivorDefList.Count)
                {
                    arrowPreviousButtonComponent.transform.parent.gameObject.SetActive(true);
                    arrowNextButtonComponent.transform.parent.gameObject.SetActive(true);
                    newSurvivorsPerRow = Math.Max(1, newSurvivorsPerRow - 1);
                }
                else
                {
                    arrowPreviousButtonComponent.transform.parent.gameObject.SetActive(false);
                    arrowNextButtonComponent.transform.parent.gameObject.SetActive(false);
                }
            }
            else
            {
                if (newSurvivorsPerRow * SurvivorRows < survivorDefList.Count)
                {
                    var minPerRow = SurvivorRows switch
                    {
                        1 => 3,
                        2 => 2,
                        _ => 1
                    };
                    newSurvivorsPerRow = Math.Max(minPerRow, newSurvivorsPerRow);
                }
            }

            return newSurvivorsPerRow;
        }

        private void UpdateHeights()
        {
            foreach (var element in trackingHeightElements)
            {
                element.minHeight = ContainerHeight;
                element.preferredHeight = ContainerHeight;
            }
        }

        internal void Build()
        {
            inlinePreviousButtonComponent.gameObject.SetActive(ScrollableLobbyUIPlugin.InlinePageArrows.Value);
            inlineNextButtonComponent.gameObject.SetActive(ScrollableLobbyUIPlugin.InlinePageArrows.Value);

            arrowPreviousButtonComponent.transform.parent.gameObject.SetActive(!ScrollableLobbyUIPlugin.InlinePageArrows.Value);
            arrowNextButtonComponent.transform.parent.gameObject.SetActive(!ScrollableLobbyUIPlugin.InlinePageArrows.Value);

            GatherSurvivorsInfo();

            if (ScrollableLobbyUIPlugin.InlinePageArrows.Value)
            {
                var survivorMaxCount = survivorDefList.Count;
                var middlePagesSurvivorsCount = survivorDefList.Count - (SurvivorsPerPage - 1) * 2; 
                var buttonsCount = survivorDefList.Count switch
                {
                   var _ when survivorDefList.Count <= SurvivorsPerPage => 0,
                    _ => 2 + (middlePagesSurvivorsCount / (SurvivorsPerPage - 2) + (middlePagesSurvivorsCount % (SurvivorsPerPage - 2) > 0 ? 1 : 0)) * 2
                };

                survivorMaxCount += buttonsCount;
                PageCount = survivorMaxCount / SurvivorsPerPage + (survivorMaxCount % SurvivorsPerPage > 0 ? 1 : 0);
                fillerCount = PageCount * SurvivorsPerPage - survivorMaxCount;
            }
            else
            {
                var survivorMaxCount = survivorDefList.Count;
                PageCount = survivorMaxCount / SurvivorsPerPage + (survivorMaxCount % SurvivorsPerPage > 0 ? 1 : 0);
                fillerCount = PageCount * SurvivorsPerPage - survivorMaxCount;
            }

            CurrentPageIndex = Mathf.Clamp(CurrentPageIndex, 0, PageCount - 1);
            IconContainerGrid.constraintCount = SurvivorsPerRow;

            UpdateHeights();
            AllocateCells();
            RebuildPage();
        }

        internal void EnforceValidChoice()
        {
            if ((characterSelectBar.pickedIcon && characterSelectBar.pickedIcon.survivorIsAvailable) || SurvivorIsAvailable(pickedSurvivor))
            {
                return;
            }

            var survivorDefIndex = survivorDefList.IndexOf(pickedSurvivor);
            for (var offset = -1; offset < survivorDefList.Count; offset *= -1)
            {
                var index = survivorDefIndex + offset;
                if (0 <= index && index < survivorDefList.Count)
                {
                    var survivorDef = survivorDefList[index];
                    if (SurvivorIsAvailable(survivorDef))
                    {
                        characterSelectBar.PickIconBySurvivorDef(survivorDef);
                        break;
                    }
                }
                if (offset >= 0)
                {
                    offset++;
                }
            }
        }

        private bool SurvivorIsAvailable(SurvivorDef survivorDef)
        {
            return
                survivorDef &&
                SurvivorCatalog.SurvivorIsUnlockedOnThisClient(survivorDef.survivorIndex) &&
                survivorDef.CheckRequiredExpansionEnabled() &&
                survivorDef.CheckUserHasRequiredEntitlement(LocalUser);
        }

        public void SelectNextPage()
        {
            if (IsOnLastPage)
            {
                return;
            }
            CurrentPageIndex++;
            RebuildPage();
        }

        public void SelectPreviousPage()
        {
            if (IsOnFirstPage)
            {
                return;
            }
            CurrentPageIndex--;
            RebuildPage();
        }

        public void OpenPageWithCharacter(SurvivorIndex survivorIndex) => OpenPageWithCharacter(SurvivorCatalog.GetSurvivorDef(survivorIndex));
        public void OpenPageWithCharacter(SurvivorDef survivorDef)
        {
            var index = survivorDefList.FindIndex(el => el == survivorDef);
            if (index == -1)
            {
                return;
            }

            CurrentPageIndex = RecalculateSurvivorIndexWithInlineButtons(index) / SurvivorsPerPage;

            RebuildPage();
        }

        private int RecalculateSurvivorIndexWithInlineButtons(int index)
        {
            if (!ScrollableLobbyUIPlugin.InlinePageArrows.Value)
            {
                return index;
            }

            if (PageCount == 1)
            {
                return index;
            }

            if (index == survivorDefList.Count)
            {
                return index + 2 + (PageCount - 2) * 2;
            }

            if (index < SurvivorsPerPage - 1)
            {
                return index;
            }

            var newIndex = SurvivorsPerPage - 2;
            index -= SurvivorsPerPage - 1;
            
            while (index > 0)
            {
                newIndex += 2;
                index -= SurvivorsPerPage - 2;
                if (index > 0)
                {
                    newIndex += SurvivorsPerPage - 2;
                }
                else
                {
                    newIndex += SurvivorsPerPage - 2 + index;
                }
            }

            return newIndex;
        }

        public class VariantArrowToggler : MonoBehaviour
        {
            public GameObject arrow;

            private void Start()
            {
                if (arrow)
                {
                    arrow.SetActive(false);
                }
            }

            void OnEnable()
            {
                if (arrow)
                {
                    arrow.SetActive(false);
                }
            }

            void OnDisable()
            {
                if (arrow)
                {
                    arrow.SetActive(true);
                }
            }
        }

    }
}
