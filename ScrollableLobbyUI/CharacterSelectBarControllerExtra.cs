using RoR2;
using RoR2.UI;
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
        private int SurvivorsPerRow { get; set; } = 1;
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
        private HGButton previousButtonComponent;
        private HGButton nextButtonComponent;

        public int PageCount { get; private set; } = 1;
        public int CurrentPageIndex { get; private set; } = 0;
        public bool IsOnFirstPage => CurrentPageIndex == 0;
        public bool IsOnLastPage => PageCount == CurrentPageIndex + 1;

        private void RebuildPage()
        {
            characterSelectBar.pickedIcon = null;
            
            var survivorDefs = survivorDefList.Skip(CurrentPageIndex * SurvivorsPerPage).Take(SurvivorsPerPage).ToArray();
            var elements = SurvivorIconControllers.elements;

            for (var index = 0; index < elements.Count; ++index)
            {
                var element = elements[index];
                if (index >= survivorDefs.Length)
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

            if (buttonHistory && buttonHistory.lastRememberedGameObject)
            {
                if (EventSystemLocator && EventSystemLocator.eventSystem)
                {
                    if (EventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad)
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
            }

            previousButtonComponent.interactable = !IsOnFirstPage;
            nextButtonComponent.interactable = !IsOnLastPage;
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

            previousButtonComponent = SetupPagingButton("Previous", "Left", SelectPreviousPage, nameof(RewiredConsts.Action.UITabLeft), 0, 2);
            nextButtonComponent = SetupPagingButton("Next", "Right", SelectNextPage, nameof(RewiredConsts.Action.UITabRight), 10, 6);

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
        }

        private void Awake()
        {
            buttonHistory = GetComponent<HGButtonHistory>();
            characterSelectBar = GetComponent<CharacterSelectBarController>();
            characterSelectBar.onSurvivorPicked.AddListener((survivorInfo) => pickedSurvivor = survivorInfo.pickedSurvivor);
            
            PrepareContainer();
            SetupPagingStuff();
            UpdateHeights();
        }

        private void OnEnable()
        {
            ScrollableLobbyUIPlugin.CharacterSelectRows.SettingChanged += CharacterSelectRowsChanged;
        }

        private void OnDisable()
        {
            ScrollableLobbyUIPlugin.CharacterSelectRows.SettingChanged -= CharacterSelectRowsChanged;
        }

        private void CharacterSelectRowsChanged(object sender, EventArgs e)
        {
            Build();
        }

        private void Update()
        {
            var containerWidth = (IconContainerGrid.transform.parent.parent.parent as RectTransform).rect.width;
            var newSurvivorsPerRow = Math.Max(1, (int)(containerWidth + iconSpacing - iconPadding * 2) / (iconSize + iconSpacing));
            if (newSurvivorsPerRow * SurvivorRows <= survivorDefList.Count)
            {
                previousButtonComponent.transform.parent.gameObject.SetActive(true);
                nextButtonComponent.transform.parent.gameObject.SetActive(true);
                newSurvivorsPerRow = Math.Max(1, newSurvivorsPerRow - 1);
            }
            else
            {
                previousButtonComponent.transform.parent.gameObject.SetActive(false);
                nextButtonComponent.transform.parent.gameObject.SetActive(false);
            }

            if (newSurvivorsPerRow != SurvivorsPerRow)
            {
                var previousFirstIconIndex = CurrentPageIndex * SurvivorsPerPage;
                SurvivorsPerRow = newSurvivorsPerRow;
                CurrentPageIndex = previousFirstIconIndex / SurvivorsPerPage;

                Build();
            }
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
            GatherSurvivorsInfo();

            var survivorMaxCount = survivorDefList.Count;
            PageCount = survivorMaxCount / SurvivorsPerPage + (survivorMaxCount % SurvivorsPerPage > 0 ? 1 : 0);
            fillerCount = PageCount * SurvivorsPerPage - survivorMaxCount;
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
            CurrentPageIndex = index / SurvivorsPerPage;
            RebuildPage();
        }
    }
}
