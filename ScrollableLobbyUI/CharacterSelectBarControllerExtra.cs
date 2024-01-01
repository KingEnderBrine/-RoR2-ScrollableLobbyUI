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

        private ColorBlock colors;
        private int SurvivorRows => ScrollableLobbyUIPlugin.CharacterSelectRows.Value;
        private int SurvivorsPerRow { get; set; } = 1;
        private int SurvivorsPerPage => SurvivorsPerRow * SurvivorRows;
        private int ContainerHeight => (iconSize + iconSpacing) * SurvivorRows - iconSpacing + iconPadding * 2;

        private bool hasRebuiltOnce = false;
        private int previousTarget = -1;

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

            if (ScrollableLobbyUIPlugin.PagingVariant.Value)
            {
                SurvivorDef[] survivorDefs;

                if(SurvivorsPerPage == 2 && survivorDefList.Count > 2)
                {
                    
                    var survivorDefs2 = survivorDefList.Skip(CurrentPageIndex * SurvivorsPerPage).Take(SurvivorsPerPage).ToArray();
                    var elements2 = SurvivorIconControllers.elements;


                    for (var index = 0; index < elements2.Count; ++index){
                        var element = elements2[index];

                        if (index >= survivorDefs2.Length){
                            element.gameObject.SetActive(false);
                            continue;
                        }

                        element.gameObject.SetActive(true);
                        //Debug.Log(survivorDefs2[index].displayNameToken + " FOUND YOU");

                        var survivorDef = survivorDefs2[index];
                        element.survivorDef = survivorDef;

                        if (pickedSurvivor == survivorDef){
                            characterSelectBar.pickedIcon = element;
                        }
                    }
                    return;
                }

                if (IsOnFirstPage)
                {
                    if (SurvivorsPerPage == survivorDefList.Count){
                        survivorDefs = survivorDefList.Skip(CurrentPageIndex * SurvivorsPerRow).Take(SurvivorsPerPage).ToArray();
                    }else{
                        survivorDefs = survivorDefList.Skip((CurrentPageIndex * SurvivorsPerRow) - 1).Take(SurvivorsPerPage).ToArray();
                    }


                    if (previousButtonComponent){
                        previousButtonComponent.gameObject.SetActive(false);
                    }

                    if (nextButtonComponent){
                        nextButtonComponent.gameObject.SetActive(true);
                    }
                       
                }else{

                    if (previousButtonComponent){
                        previousButtonComponent.gameObject.SetActive(true);
                        //previousButtonComponent.transform.SetSiblingIndex(-999);
                    }

                    if (!IsOnLastPage){
                        nextButtonComponent.gameObject.SetActive(true);
                        //nextButtonComponent.transform.SetSiblingIndex(999);
                    }else{
                        nextButtonComponent.gameObject.SetActive(false);
                    }

                    var page = SurvivorsPerPage - 2;

                    //Debug.Log("first part " + ((SurvivorsPerRow - 1) * SurvivorRows) + " | | " + (survivorDefList.Count - SurvivorsPerPage - 1) + " | " + survivorDefList.Count);
                    //Debug.Log("page: " + (((SurvivorsPerPage - 1) + page * (CurrentPageIndex - 1)) + page - survivorDefList.Count));

                    var workingCount = survivorDefList.Count;
                    var equation = ((SurvivorsPerPage - 1) + page * (CurrentPageIndex - 1)) + page - survivorDefList.Count;
                    if (equation == -1){
                         survivorDefs = survivorDefList.Skip((SurvivorsPerPage - 1) + page * (CurrentPageIndex - 1)).Take(SurvivorsPerPage - 1).ToArray();
                    }else{
                        survivorDefs = survivorDefList.Skip((SurvivorsPerPage - 1) + page * (CurrentPageIndex - 1)).Take(page).ToArray();
                    }
                }


                var elements = SurvivorIconControllers.elements;

                GameObject lastButton = null;
                for (int index = 0; index < elements.Count; ++index)
                {
                    var element = elements[index];

                    if (index == ((CurrentPageIndex + 1) * SurvivorsPerPage) - 1){
                        if (IsOnFirstPage){
                            if(SurvivorsPerPage == survivorDefList.Count){
                                var survivorDef2 = survivorDefs[index];
                                element.survivorDef = survivorDef2;
                                element.gameObject.SetActive(true);
                                lastButton = element.gameObject;
                                continue;
                            }
                            element.gameObject.SetActive(false);
                            continue;
                        }
                    }
                    if (index >= survivorDefs.Length){
                        //Debug.Log(element.GetComponentInChildren<RawImage>().mainTexture.name + " is inactive");
                        element.gameObject.SetActive(false);
                        continue;
                    }

                    //Debug.Log(survivorDefs[index].displayNameToken + " FOUND YOU");
                    element.gameObject.SetActive(true);
                    lastButton = element.gameObject;

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

                if (nextButtonComponent){
                    nextButtonComponent.transform.SetSiblingIndex(SurvivorsPerPage + 1);
                    nextButtonComponent.transform.Find("ArrowText").GetComponent<RectTransform>().localPosition = new Vector3(0, 2, 0);
                }

                if (previousButtonComponent){
                    previousButtonComponent.transform.SetSiblingIndex(0);
                    previousButtonComponent.transform.Find("ArrowText").GetComponent<RectTransform>().localPosition = new Vector3(-2, 2, 0);
                }

                if (buttonHistory && buttonHistory.lastRememberedGameObject){
                    if (EventSystemLocator && EventSystemLocator.eventSystem){
                        if (EventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad){
                            if (buttonHistory){
                                if (buttonHistory.lastRememberedGameObject.activeInHierarchy){
                                    buttonHistory.lastRememberedGameObject.GetComponent<HGButton>().OnSelect(new BaseEventData(EventSystem.current));
                                    StartCoroutine(DelayedControllerFix(previousButtonComponent)); //yeah
                                    StartCoroutine(DelayedControllerFix(nextButtonComponent));
                                    if (lastButton){
                                        StartCoroutine(DelayedControllerFix(lastButton.GetComponent<HGButton>()));
                                    }
                                }else{
                                    elements.LastOrDefault(el => el.gameObject.activeInHierarchy)?.GetComponent<HGButton>().Select();
                                    StartCoroutine(DelayedControllerFix(previousButtonComponent));
                                    StartCoroutine(DelayedControllerFix(nextButtonComponent));
                                    if (lastButton){
                                        StartCoroutine(DelayedControllerFix(lastButton.GetComponent<HGButton>()));
                                    }
                                }
                            }
                        }
                    }
                }

                if(survivorDefList.Count <= SurvivorsPerPage){
                    nextButtonComponent.gameObject.SetActive(false);
                    foreach (var fillerIcon in FillerIconControllers.elements){
                        fillerIcon.gameObject.SetActive(true);
                    }
                }

            }
            else
            {
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
                    //Debug.Log(survivorDefs[index].displayNameToken + " FOUND YOU");
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
                            if (buttonHistory)
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
                }
                if (previousButtonComponent && nextButtonComponent && !ScrollableLobbyUIPlugin.PagingVariant.Value)
                {
                    previousButtonComponent.interactable = !IsOnFirstPage;
                    nextButtonComponent.interactable = !IsOnLastPage;
                }
            }

        }

        IEnumerator DelayedControllerFix(HGButton button)
        {
            button.interactable = false;
            yield return .05f;
            button.interactable = true;

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

            if (!ScrollableLobbyUIPlugin.PagingVariant.Value)
            {
                previousButtonComponent = SetupPagingButton("Previous", "Left", SelectPreviousPage, nameof(RewiredConsts.Action.UITabLeft), 0, 2);
                nextButtonComponent = SetupPagingButton("Next", "Right", SelectNextPage, nameof(RewiredConsts.Action.UITabRight), 10, 6);
            }
            else
            {
                previousButtonComponent = SetupPagingButtonCharacterSlot("Previous", "Left", SelectPreviousPage, nameof(RewiredConsts.Action.UITabLeft), 0, 2, mpEventSystemLocator, survivorChoiceGrid, uiLayerKey);
                nextButtonComponent = SetupPagingButtonCharacterSlot("Next", "Right", SelectNextPage, nameof(RewiredConsts.Action.UITabRight), 10, 6, mpEventSystemLocator, survivorChoiceGrid, uiLayerKey);
            }
            


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

        HGButton SetupPagingButtonCharacterSlot(string prefix, string buttonPrefix, System.Action action, string actionName, int siblingIndex, int glyphIndex, MPEventSystemLocator mpEventSystemLocator, GameObject survivorChoiceGrid, UILayerKey uiLayerKey)
        {

            GameObject buttonHolder = new GameObject($"{prefix}ButtonContainer");
            buttonHolder.transform.SetParent(transform, false);
            buttonHolder.transform.SetSiblingIndex(siblingIndex);
            buttonHolder.layer = 5;

            var rt = buttonHolder.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48, 48);
            buttonHolder.AddComponent<CanvasRenderer>();

            var img1 = buttonHolder.AddComponent<Image>();
            img1.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUICleanButton.png").WaitForCompletion();
            img1.overrideSprite = img1.sprite;
            img1.type = Image.Type.Sliced;
            img1.fillMethod = Image.FillMethod.Radial360;

            var btn = buttonHolder.AddComponent<HGButton>();
            btn.image = img1;
            btn.targetGraphic = img1; //come back to this

            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(new UnityEngine.Events.UnityAction(action));

            btn.interactable = true;

            var tgrs = btn.animationTriggers;
            tgrs.normalTrigger = "Normal";
            tgrs.highlightedTrigger = "Highlighted";
            tgrs.pressedTrigger = "Pressed";
            tgrs.selectedTrigger = "Highlighted";
            tgrs.disabledTrigger = "Disabled";

            var bsc = buttonHolder.AddComponent<ButtonSkinController>();
            bsc.useRecommendedImage = true;
            bsc.useRecommendedMaterial = true;
            bsc.useRecommendedAlignment = true;
            bsc.useRecommendedLabel = true;
            bsc.useRecommendedButtonHeight = false;
            bsc.useRecommendedButtonWidth = false;
            bsc.skinData = Addressables.LoadAssetAsync<UISkinData>("RoR2/Base/UI/skinCleanButton.asset").WaitForCompletion();

            var inter = new GameObject("InteractableHighlight");
            inter.transform.SetParent(buttonHolder.transform, false);
            inter.layer = 5;

            var rti = inter.AddComponent<RectTransform>();
            rti.sizeDelta = new Vector2(72, 72);
            inter.AddComponent<CanvasRenderer>();

            var img2 = inter.AddComponent<Image>();
            img2.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIOutlineOnly.png").WaitForCompletion();
            img2.overrideSprite = img2.sprite;
            img2.type = Image.Type.Sliced;
            img2.fillMethod = Image.FillMethod.Radial360;

            var hover = new GameObject("HoverHighlight");
            hover.transform.SetParent(buttonHolder.transform, false);
            hover.layer = 5;

            var rt2 = hover.AddComponent<RectTransform>();
            rt2.sizeDelta = new Vector2(72, 72);
            rt2.localScale = new Vector3(1.1f, 1.1f, 1.1f);

            hover.AddComponent<CanvasRenderer>();

            var img3 = hover.AddComponent<Image>();
            img3.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texUIHighlightBoxOutline.png").WaitForCompletion();
            img3.overrideSprite = img3.sprite;
            img3.type = Image.Type.Sliced;
            img3.fillMethod = Image.FillMethod.Radial360;

            var text = new GameObject("ArrowText");
            text.transform.SetParent(buttonHolder.transform, false);
            text.layer = 5;

            var rtt = text.AddComponent<RectTransform>();
            text.AddComponent<CanvasRenderer>();

            var stupid = text.AddComponent<HGTextMeshProUGUI>();
            if (prefix == "Next"){
                stupid.text = ">";
            }else{
                stupid.text = "<";
            }

            stupid.fontSize = 56;
            stupid.fontSizeMin = 55;
            stupid.fontSizeMax = 57;
            stupid.enableKerning = true;
            stupid.enableAutoSizing = true;

            stupid.raycastTarget = false;

            btn.imageOnHover = img3;
            btn.imageOnInteractable = img2;

            colors = btn.colors;
            colors.normalColor = new Color(.325f, .404f, .471f, 1); //83, 103, 120, 1
            colors.highlightedColor = new Color(.989f, 1, .694f, .733f); //252, 255, 177, 187
            colors.pressedColor = new Color(.741f, .753f, .443f, .984f); //189, 192, 113, 251
            colors.selectedColor = new Color(.989f, 1, .694f, .733f); //252, 255, 177, 187
            colors.disabledColor = new Color(.255f, .2f, .2f, .714f); //65, 51, 51, 182
            colors.colorMultiplier = 1;
            colors.fadeDuration = 0;
            btn.colors = colors;

            btn.showImageOnHover = true;

            var glyph = new GameObject($"{prefix}Glyph");
            glyph.transform.SetParent(buttonHolder.transform, false);
            glyph.layer = 5;
            glyph.transform.SetSiblingIndex(0);
            glyph.SetActive(mpEventSystemLocator.eventSystem && mpEventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad);

            var glyphTransform = glyph.AddComponent<RectTransform>();
            glyphTransform.anchoredPosition3D = new Vector3(0, 0, 0);
            glyphTransform.localScale = new Vector3(1, 1, 1);
            glyphTransform.sizeDelta = new Vector2(0, 0);
            //glyphTransform.position = new Vector3(0, -4, 0);
            //glyphTransform.eulerAngles = new Vector3(0, 0, -.05f);

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
            //glyphTextTransform.position = new Vector3(0, offset, 0);

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

            var vat = glyph.AddComponent<VariantArrowToggler>();
            vat.arrow = text;


            return btn;
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
            var tempPadding = ScrollableLobbyUIPlugin.PagingVariant.Value ? 0 : iconPadding * 2;
            var newSurvivorsPerRow = Math.Max(1, (int)(containerWidth + iconSpacing - tempPadding) / (iconSize + iconSpacing));
            //Debug.Log("newSurvivorsPerRow: " + newSurvivorsPerRow); 

            if (newSurvivorsPerRow * SurvivorRows < survivorDefList.Count)
            {
                if(previousButtonComponent && nextButtonComponent && !ScrollableLobbyUIPlugin.PagingVariant.Value)
                {
                    previousButtonComponent.transform.parent.gameObject.SetActive(true);
                    nextButtonComponent.transform.parent.gameObject.SetActive(true);
                    newSurvivorsPerRow = Math.Max(1, newSurvivorsPerRow - 1);
                }
            }
            else
            {
                if (previousButtonComponent && nextButtonComponent && !ScrollableLobbyUIPlugin.PagingVariant.Value)
                {
                    previousButtonComponent.transform.parent.gameObject.SetActive(false);
                    nextButtonComponent.transform.parent.gameObject.SetActive(false);
                }
            }

            if (newSurvivorsPerRow != SurvivorsPerRow)
            {
                var previousFirstIconIndex = CurrentPageIndex * SurvivorsPerPage;
                SurvivorsPerRow = newSurvivorsPerRow;

                if (ScrollableLobbyUIPlugin.PagingVariant.Value)
                {
                    if(previousTarget != -1)
                    {
                        var workingTarget = previousTarget;
                        if(workingTarget < (SurvivorsPerPage - 1))
                        {
                            CurrentPageIndex = 0;
                        }
                        else
                        {
                            workingTarget -= (SurvivorsPerPage - 1);
                            workingTarget /= (SurvivorsPerPage - 2);
                            //Debug.Log("wokring target: " + workingTarget + " | " + previousTarget);
                            CurrentPageIndex = workingTarget + 1;
                            previousTarget = -1;
                        }

                    }
                }
                else
                {
                    CurrentPageIndex = previousFirstIconIndex / SurvivorsPerPage;
                }

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
            //CreateEmpties();
            if (ScrollableLobbyUIPlugin.PagingVariant.Value)
            {
                var survivorMaxCount = survivorDefList.Count;
                PageCount = survivorMaxCount / SurvivorsPerPage + (survivorMaxCount % SurvivorsPerPage > 0 ? 1 : 0);
                //fillerCount = PageCount * SurvivorsPerPage - survivorMaxCount;
                if (SurvivorsPerPage == 2 && survivorDefList.Count > 2)
                {
                    hasRebuiltOnce = false;
                }
                fillerCount = 0;
                //Debug.Log("survivorMaxCount: " + survivorMaxCount);
                //Debug.Log("PageCount: " + PageCount + " | " + survivorMaxCount / SurvivorsPerPage + " | " + (survivorMaxCount % SurvivorsPerPage > 0 ? 1 : 0));
                //Debug.Log("fillercount: " + fillerCount + " | " + PageCount * SurvivorsPerPage + " | " + survivorMaxCount);
                

                if(hasRebuiltOnce) //PageCount > 1 && hasRebuiltOnce
                {
                    survivorMaxCount -= (SurvivorsPerPage - 1);

                    var mod = survivorMaxCount % (SurvivorsPerPage - 2);

                    survivorMaxCount -= mod;

                    survivorMaxCount /= (SurvivorsPerPage - 2);
                    PageCount = 1 + survivorMaxCount + (mod > 1 ? 1 : 0);

                    //var workingCount = survivorDefList.Count - (SurvivorsPerPage - 1);
                    //var previous = workingCount;

                    if(mod == 1){
                        fillerCount = 0;
                    }else if(PageCount > 1){
                        fillerCount = (SurvivorsPerPage - 1) - mod;
                    }
                    else if(survivorDefList.Count <= SurvivorsPerPage)
                    {
                        fillerCount = SurvivorsPerPage - survivorDefList.Count;
                    }
                }
            }
            else
            {
                var survivorMaxCount = survivorDefList.Count;
                PageCount = survivorMaxCount / SurvivorsPerPage + (survivorMaxCount % SurvivorsPerPage > 0 ? 1 : 0);
                fillerCount = PageCount * SurvivorsPerPage - survivorMaxCount;
            }

            //Debug.Log("page index : " + CurrentPageIndex + " | " + PageCount);
            CurrentPageIndex = Mathf.Clamp(CurrentPageIndex, 0, PageCount - 1);
            IconContainerGrid.constraintCount = SurvivorsPerRow;

            UpdateHeights();
            AllocateCells();
            RebuildPage();
            hasRebuiltOnce = true;
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
            //Debug.Log("Yeah");
            var index = survivorDefList.FindIndex(el => el == survivorDef);
            previousTarget = index;
            if (index == -1)
            {
                return;
            }

            if(SurvivorsPerPage == 2 || !ScrollableLobbyUIPlugin.PagingVariant.Value)
            {
                CurrentPageIndex = index / SurvivorsPerPage;
            }
            else
            {
                var tempIndex = index;
                if (index < (SurvivorsPerPage - 1))
                {
                    CurrentPageIndex = 0;
                }
                else
                {
                    tempIndex -= (SurvivorsPerPage - 1);
                    tempIndex /= (SurvivorsPerPage - 2);
                    CurrentPageIndex = tempIndex + 1; //?
                    //Debug.Log("CurrentPageIndex: " + CurrentPageIndex + " || " + index + " || " + tempIndex);
                }
            }
            //Debug.Log("CurrentPageIndex: " + CurrentPageIndex + " || " + index);
            RebuildPage();
        }

        public class VariantArrowToggler : MonoBehaviour
        {
            public GameObject arrow;

            void OnEnable()
            {
                arrow.SetActive(false);
            }

            void OnDisable()
            {
                arrow.SetActive(true);
            }
        }

    }
}
