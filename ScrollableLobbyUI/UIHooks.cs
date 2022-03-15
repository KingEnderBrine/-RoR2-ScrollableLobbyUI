using LeTai.Asset.TranslucentImage;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    internal static class UIHooks
    {
        //Full panel width ~ 310, choice witdh 64, left offset 12, padding 6, padding 12
        private const int ruleFillerWidth = 87;
        private const int ruleFillerHeight = 64;
        private const float ruleScrollDuration = 0.1F;

        private static readonly List<RoR2.UI.HGButton> buttonsWithListeners = new List<RoR2.UI.HGButton>();

        internal static void LoadoutPanelControllerAwake(On.RoR2.UI.LoadoutPanelController.orig_Awake orig, RoR2.UI.LoadoutPanelController self)
        {
            orig(self);

            var uiLayerKey = self.GetComponentInParent<RoR2.UI.UILayerKey>();

            //Disabling buttons navigation if selected right panel,
            //so extremely long rows in loadout will not interfere in artifacts selection
            var loadoutHelper = uiLayerKey.gameObject.AddComponent<ButtonsNavigationController>();
            loadoutHelper.requiredTopLayer = uiLayerKey;
            loadoutHelper.loadoutPanel = self;

            //Adding container on top of LoadoutPanelController
            var loadoutScrollPanel = AddScrollPanel(self.transform, "LoadoutScrollPanel");
            //Adding container on top of SkillPanel
            var skillScrollPanel = AddScrollPanel(self.transform.parent.parent.Find("SkillPanel"), "SkillsScrollPanel");

            //Adding container on top of OverviewPanel
            var overviewPanel = self.transform.parent.parent.Find("OverviewPanel");
            var overviewScrollPanel = AddScrollPanel(overviewPanel, "OverviewScrollPanel");
            var overviewLayoutGroup = overviewPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            overviewLayoutGroup.childControlHeight = true;
            overviewLayoutGroup.childControlWidth = true;
            overviewLayoutGroup.childForceExpandHeight = true;
            overviewLayoutGroup.childForceExpandWidth = true;

            var overviewScrollHelper = overviewScrollPanel.AddComponent<GamepadScrollRectHelper>();
            overviewScrollHelper.requiredTopLayer = uiLayerKey;

            var survivorDescriptionPanel = self.transform.parent.parent.parent.parent.Find("SurvivorInfoPanel, Inactive/DescriptionPanel, Naked");
            survivorDescriptionPanel.gameObject.AddComponent<RectMask2D>();

            //Moving out descriptionPanel, so it will not be hidden by mask
            MoveUpDescription("DescriptionPanel, Loadout", "LoadoutScrollContainer", loadoutScrollPanel);
            MoveUpDescription("DescriptionPanel, Skill", "SkillScrollContainer", skillScrollPanel);

            GameObject AddScrollPanel(Transform panel, string name)
            {
                var scrollPanel = new GameObject(name);
                scrollPanel.layer = 5;
                scrollPanel.transform.SetParent(panel.transform.parent, false);
                panel.transform.SetParent(scrollPanel.transform, false);

                scrollPanel.AddComponent<RoR2.UI.MPEventSystemLocator>();

                var scrollPanelMask = scrollPanel.AddComponent<RectMask2D>();

                var scrollPanelRect = scrollPanel.AddComponent<ConstrainedScrollRect>();
                scrollPanelRect.horizontal = false;
                scrollPanelRect.content = panel.GetComponent<RectTransform>();
                scrollPanelRect.scrollSensitivity = 30;
                scrollPanelRect.movementType = ScrollRect.MovementType.Clamped;
                scrollPanelRect.scrollConstraint = ConstrainedScrollRect.Constraint.OnlyScroll;

                //Adding ContentSizeFilter, otherwise childs would have been wrong size
                var panelContentSizeFilter = panel.gameObject.AddComponent<ContentSizeFitter>();
                panelContentSizeFilter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                panelContentSizeFilter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var scrollPanelRectTransform = scrollPanelRect.GetComponent<RectTransform>();
                scrollPanelRectTransform.pivot = new Vector2(0.5F, 1F);
                scrollPanelRectTransform.anchorMin = new Vector2(0, 0);
                scrollPanelRectTransform.anchorMax = new Vector2(1, 1);
                scrollPanelRectTransform.sizeDelta = new Vector2(0, 0);

                panel.GetComponent<RectTransform>().pivot = new Vector2(0.5F, 1);

                //Enabling Image component, so you can scroll from any point in panel
                var panelImage = panel.GetComponent<Image>();
                if (!panelImage)
                {
                    panelImage = panel.gameObject.AddComponent<Image>();
                }
                panelImage.enabled = true;
                panelImage.color = new Color(0, 0, 0, 0);
                panelImage.raycastTarget = true;

                return scrollPanel;
            }

            void MoveUpDescription(string descriptionPanelName, string parentName, GameObject panel)
            {
                var descriptionPanel = panel.transform.GetChild(0).Find(descriptionPanelName);

                var parentScrollContainer = new GameObject(parentName);
                parentScrollContainer.transform.SetParent(panel.transform.parent, false);
                panel.transform.SetParent(parentScrollContainer.transform, false);
                descriptionPanel.SetParent(parentScrollContainer.transform, false);

                var rect = parentScrollContainer.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0);
                rect.anchorMax = new Vector2(1, 1);
                rect.pivot = new Vector2(0.5F, 1);
                rect.sizeDelta = new Vector2(0, 0);

                var clearTextOnDisable = panel.transform.GetChild(0).gameObject.AddComponent<ClearTextOnDisable>();
                clearTextOnDisable.textObjects = new List<TextMeshProUGUI> { descriptionPanel.GetComponent<RoR2.UI.DisableIfTextIsEmpty>().tmpUGUI };
            }
        }

        internal static void LoadoutPanelControllerOnDestroy(On.RoR2.UI.LoadoutPanelController.orig_OnDestroy orig, RoR2.UI.LoadoutPanelController self)
        {
            orig(self);
            buttonsWithListeners.Clear();
        }

        internal static void LoadoutPanelControllerRowFinishSetup(On.RoR2.UI.LoadoutPanelController.Row.orig_FinishSetup orig, object selfObject, bool addWIPIcons)
        {
            var self = selfObject as RoR2.UI.LoadoutPanelController.Row;

            orig(self, addWIPIcons);

            var rowRectTransform = self.rowPanelTransform;
            var buttonContainerTransform = self.buttonContainerTransform;

            foreach (var button in self.buttons)
            {
                //Scroll to selected row if it's not fully visible
                button.onSelect.AddListener(() =>
                {
                    var buttonsScrollRect = button.GetComponentInParent<ConstrainedScrollRect>();
                    var rowsScrollRect = buttonsScrollRect.redirectConstrained;
                    var eventSystemLocator = rowsScrollRect.GetComponent<RoR2.UI.MPEventSystemLocator>();

                    if (!eventSystemLocator || !eventSystemLocator.eventSystem || eventSystemLocator.eventSystem.currentInputSource != RoR2.UI.MPEventSystem.InputSource.Gamepad)
                    {
                        return;
                    }

                    var rowsContentPanel = rowsScrollRect.content;

                    var rowPosition = (Vector2)rowsScrollRect.transform.InverseTransformPoint(rowRectTransform.position);
                    var rowsScrollHeight = rowsScrollRect.GetComponent<RectTransform>().rect.height;
                    var halfRowHeight = rowRectTransform.rect.height / 2;

                    if (rowPosition.y - halfRowHeight < -rowsScrollHeight)
                    {
                        rowsContentPanel.anchoredPosition = new Vector2(
                            rowsContentPanel.anchoredPosition.x,
                            -rowRectTransform.anchoredPosition.y - rowsScrollHeight + halfRowHeight);
                    }
                    else if (rowPosition.y + halfRowHeight > 0)
                    {
                        rowsContentPanel.anchoredPosition = new Vector2(
                            rowsContentPanel.anchoredPosition.x,
                            -rowRectTransform.anchoredPosition.y - halfRowHeight);
                    }

                    var buttonsContentPanel = buttonsScrollRect.content;
                    var buttonRectTransform = button.GetComponent<RectTransform>();

                    var buttonPosition = (Vector2)buttonsScrollRect.transform.InverseTransformPoint(buttonRectTransform.position);
                    var buttonsScrollWidth = buttonsScrollRect.GetComponent<RectTransform>().rect.width;
                    var buttonWidth = buttonRectTransform.rect.width;
                    var buttonsPadding = 8;

                    if (buttonPosition.x + buttonWidth + buttonsPadding > 0)
                    {
                        buttonsContentPanel.anchoredPosition = new Vector2(
                            -buttonRectTransform.anchoredPosition.x - buttonWidth + buttonsScrollWidth - buttonsPadding - 24,
                            buttonsContentPanel.anchoredPosition.y);
                    }
                    else if (buttonPosition.x - buttonsPadding < -buttonsScrollWidth)
                    {
                        buttonsContentPanel.anchoredPosition = new Vector2(
                            -buttonRectTransform.anchoredPosition.x + buttonsPadding + 24,
                            buttonsContentPanel.anchoredPosition.y);
                    }
                });
            }
        }

        internal static void LoadoutPanelControllerRowCtor(On.RoR2.UI.LoadoutPanelController.Row.orig_ctor orig, object selfObject, RoR2.UI.LoadoutPanelController owner, BodyIndex bodyIndex, string titleToken)
        {
            var self = selfObject as RoR2.UI.LoadoutPanelController.Row;
            
            orig(self, owner, bodyIndex, titleToken);

            //Disabling sorting override because it not work with mask
            var highlightRect = self.choiceHighlightRect;
            highlightRect.GetComponent<RefreshCanvasDrawOrder>().enabled = false;
            highlightRect.GetComponent<Canvas>().overrideSorting = false;

            var buttonContainer = self.buttonContainerTransform;
            var rowPanel = self.rowPanelTransform;

            var rowHorizontalLayout = rowPanel.gameObject.AddComponent<HorizontalLayoutGroup>();

            var panel = rowPanel.Find("Panel");
            var slotLabel = rowPanel.Find("SlotLabel");

            var labelContainer = new GameObject("LabelContainer");
            labelContainer.transform.SetParent(rowPanel, false);
            panel.SetParent(labelContainer.transform, false);
            slotLabel.SetParent(labelContainer.transform, false);

            var slotLabelRect = slotLabel.GetComponent<RectTransform>();
            slotLabelRect.anchoredPosition = new Vector2(0, 0);

            var labelContainerLayout = labelContainer.AddComponent<LayoutElement>();
            labelContainerLayout.minHeight = 0;
            labelContainerLayout.preferredHeight = 96;
            labelContainerLayout.minWidth = 128;

            var labelContainerRect = labelContainer.GetComponent<RectTransform>();
            labelContainerRect.anchorMin = new Vector2(0, 0);
            labelContainerRect.anchorMax = new Vector2(1, 1);
            labelContainerRect.pivot = new Vector2(0, 0F);

            var scrollPanel = new GameObject("RowScrollPanel");
            scrollPanel.transform.SetParent(rowPanel, false);

            var scrollViewport = new GameObject("Viewport");
            scrollViewport.transform.SetParent(scrollPanel.transform, false);

            var scrollViewportRectTransform = scrollViewport.AddComponent<RectTransform>();
            scrollViewportRectTransform.pivot = new Vector2(0.5F, 0.5F);
            scrollViewportRectTransform.anchorMin = new Vector2(0, 0);
            scrollViewportRectTransform.anchorMax = new Vector2(1, 1);
            scrollViewportRectTransform.sizeDelta = new Vector2(0, 0);

            buttonContainer.SetParent(scrollViewport.transform, false);
            highlightRect.SetParent(scrollViewport.transform, false);

            var mask = scrollPanel.AddComponent<RectMask2D>();

            var scrollPanelLayout = scrollPanel.AddComponent<LayoutElement>();
            scrollPanelLayout.preferredWidth = 100000;

            var scrollRect = scrollPanel.AddComponent<ConstrainedScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.viewport = scrollViewportRectTransform;
            scrollRect.content = buttonContainer;
            scrollRect.scrollSensitivity = -30;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollConstraint = ConstrainedScrollRect.Constraint.OnlyDrag;
            scrollRect.redirectConstrained = rowPanel.GetComponentInParent<ConstrainedScrollRect>();

            var scrollPanelRectTransform = scrollPanel.GetComponent<RectTransform>();
            scrollPanelRectTransform.pivot = new Vector2(1, 0.5F);
            scrollPanelRectTransform.anchorMin = new Vector2(0, 0);
            scrollPanelRectTransform.anchorMax = new Vector2(1, 1);

            //Adding ContentSizeFilter, otherwise childs would have been wrong size
            var buttonContainerSizeFilter = buttonContainer.gameObject.AddComponent<ContentSizeFitter>();
            buttonContainerSizeFilter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            buttonContainerSizeFilter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            buttonContainer.pivot = new Vector2(0, 0.5F);

            buttonContainer.Find("Spacer").gameObject.SetActive(false);

            var buttonContainerHorizontalLayout = buttonContainer.GetComponent<HorizontalLayoutGroup>();
            buttonContainerHorizontalLayout.padding = new RectOffset(8, 8, 8, 8);

            var rightButton = SetupButton("Right", scrollPanelRectTransform, MoveDirection.Right, 1);
            var leftButton = SetupButton("Left", scrollPanelRectTransform, MoveDirection.Left, 0);

            var scrollButtonsController = scrollPanel.AddComponent<ScrollButtonsController>();
            scrollButtonsController.left = leftButton;
            scrollButtonsController.right = rightButton;

            GameObject SetupButton(string buttonPrefix, Transform parent, MoveDirection moveDirection, float xNormalized)
            {
                var scrollButton = GameObject.Instantiate(Addressables.LoadAssetAsync<GameObject>($"RoR2/Base/UI/{buttonPrefix}Button.prefab").WaitForCompletion(), parent, false);
                scrollButton.name = $"{buttonPrefix}ScrollButton";
                scrollButton.layer = 5;


                var hgButton = scrollButton.GetComponent<RoR2.UI.HGButton>();

                var arrowObject = new GameObject("Arrow");
                arrowObject.transform.SetParent(scrollButton.transform, false);

                var arrowObjectRectTransform = arrowObject.AddComponent<RectTransform>();
                arrowObjectRectTransform.pivot = new Vector2(0.5F, 0.5F);
                arrowObjectRectTransform.anchorMin = new Vector2(0, 0);
                arrowObjectRectTransform.anchorMax = new Vector2(1, 1);
                arrowObjectRectTransform.sizeDelta = new Vector2(-8, 0);

                var targetGraphic = hgButton.targetGraphic as Image;
                
                var arrowImage = arrowObject.AddComponent<Image>();
                arrowImage.sprite = targetGraphic.sprite;
                
                GameObject.DestroyImmediate(hgButton.targetGraphic);
                var translucentImage = scrollButton.AddComponent<TranslucentImage>();
                translucentImage.material = Addressables.LoadAssetAsync<Material>("TranslucentImage/Default-Translucent.mat").WaitForCompletion();
                translucentImage.color = Color.black;

                hgButton.targetGraphic = arrowImage;

                var scrollOnPress = scrollButton.AddComponent<ContinuousScrollOnPress>();
                scrollOnPress.scrollRect = scrollRect;
                scrollOnPress.sensitivity = -400;
                scrollOnPress.moveDirection = moveDirection;

                GameObject.DestroyImmediate(scrollButton.GetComponent<LayoutElement>());

                var rectTransform = scrollButton.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(xNormalized, 0F);
                rectTransform.anchorMax = new Vector2(xNormalized, 1F);
                rectTransform.pivot = new Vector2(xNormalized, 0.5F);
                rectTransform.sizeDelta = new Vector2(24, 0);

                scrollButton.SetActive(false);

                return scrollButton;
            }
        }

        internal static void CharacterSelectControllerRebuildLocal(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CharacterSelectController>(nameof(CharacterSelectController.skillStripAllocator)),
                x => x.MatchLdfld(out _));

            var instructions = new List<Instruction>();
            for (var i = 0; i < 8; i++)
            {
                instructions.Add(c.Next);
                c.Index += 1;
            }

            c.Index += 1;
            foreach (var instuction in instructions)
            {
                c.Emit(instuction.OpCode, instuction.Operand);
            }

            var fieldInfo = typeof(RoR2.UI.CharacterSelectController.StripDisplayData).GetField("enabled");
            c.Emit(OpCodes.Ldfld, fieldInfo);
            c.EmitDelegate<Action<RectTransform, bool>>((skillStrip, enabled) =>
            {
                if (!enabled)
                {
                    return;
                }

                //Enabling LayoutElement to force skill row size;
                var layoutElement = skillStrip.GetComponent<LayoutElement>();
                layoutElement.enabled = true;
                layoutElement.minHeight = 0;
                layoutElement.preferredHeight = 96;

                var button = skillStrip.GetComponent<RoR2.UI.HGButton>();
                if (buttonsWithListeners.Contains(button))
                {
                    return;
                }

                button.onSelect.AddListener(OnScrollListener);
                buttonsWithListeners.Add(button);

                void OnScrollListener()
                {
                    var rowsScrollRect = button.GetComponentInParent<ConstrainedScrollRect>();
                    var eventSystemLocator = rowsScrollRect.GetComponent<RoR2.UI.MPEventSystemLocator>();

                    if (!eventSystemLocator || !eventSystemLocator.eventSystem || eventSystemLocator.eventSystem.currentInputSource != RoR2.UI.MPEventSystem.InputSource.Gamepad)
                    {
                        return;
                    }

                    var rowsContentPanel = rowsScrollRect.content;
                    var rowRectTransform = button.GetComponent<RectTransform>();

                    var rowPosition = (Vector2)rowsScrollRect.transform.InverseTransformPoint(rowRectTransform.position);
                    var rowsScrollHeight = rowsScrollRect.GetComponent<RectTransform>().rect.height;
                    var halfRowHeight = rowRectTransform.rect.height / 2;

                    if (rowPosition.y - halfRowHeight < -rowsScrollHeight)
                    {
                        rowsContentPanel.anchoredPosition = new Vector2(
                            rowsContentPanel.anchoredPosition.x,
                            -rowRectTransform.anchoredPosition.y - rowsScrollHeight + halfRowHeight);
                    }
                    else if (rowPosition.y + halfRowHeight > 0)
                    {
                        rowsContentPanel.anchoredPosition = new Vector2(
                            rowsContentPanel.anchoredPosition.x,
                            -rowRectTransform.anchoredPosition.y - halfRowHeight);
                    }
                }
            });
        }

        internal static void RuleBookViewerStripUpdate(On.RoR2.UI.RuleBookViewerStrip.orig_Update orig, RoR2.UI.RuleBookViewerStrip self) { }

        internal static void CharacterSelectBarControllerAwake(On.RoR2.CharacterSelectBarController.orig_Awake orig, RoR2.CharacterSelectBarController self)
        {
            orig(self);
            self.gameObject.AddComponent<CharacterSelectBarControllerExtra>();
        }

        internal static void CharacterSelectBarControllerBuild(On.RoR2.CharacterSelectBarController.orig_Build orig, RoR2.CharacterSelectBarController self)
        {
            var extra = self.GetComponent<CharacterSelectBarControllerExtra>();
            extra.Build();
        }

        internal static void CharacterSelectBarControllerEnforceValidChoice(On.RoR2.CharacterSelectBarController.orig_EnforceValidChoice orig, RoR2.CharacterSelectBarController self)
        {
            var extra = self.GetComponent<CharacterSelectBarControllerExtra>();
            extra.EnforceValidChoice();
        }

        internal static void CharacterSelectBarControllerPickIconBySurvivorDef(On.RoR2.CharacterSelectBarController.orig_PickIconBySurvivorDef orig, RoR2.CharacterSelectBarController self, SurvivorDef survivorDef)
        {
            var extra = self.GetComponent<CharacterSelectBarControllerExtra>();
            extra.OpenPageWithCharacter(survivorDef);
            orig(self, survivorDef);
        }

        internal static void RuleCategoryControllerSetData(On.RoR2.UI.RuleCategoryController.orig_SetData orig, RoR2.UI.RuleCategoryController self, RuleCategoryDef categoryDef, RuleChoiceMask availability, RuleBook ruleBook)
        {
            orig(self, categoryDef, availability, ruleBook);

            if (self.ruleCategoryType == RuleCatalog.RuleCategoryType.StripVote)
            {
                var stripContainer = self.transform.Find("StripContainer");
                if (stripContainer.gameObject.activeInHierarchy)
                {
                    stripContainer.Find("FrameContainer").gameObject.SetActive(false);

                    for (var i = 0; i < stripContainer.childCount; i++)
                    {
                        var child = stripContainer.GetChild(i);
                        if (child.gameObject.activeSelf)
                        {
                            SetupStripPrefab(child);
                        }
                    }
                }
            }

            if (self.ruleCategoryType == RuleCatalog.RuleCategoryType.VoteResultGrid)
            {
                foreach (var controller in self.popoutButtonIconAllocator.elements)
                {
                    var button = controller.hgButton;
                    //Scroll to selected row if it's not fully visible
                    button.onSelect.AddListener(() =>
                    {
                        var scrollRect = button.GetComponentInParent<ScrollRect>();
                        var eventSystemLocator = scrollRect.GetComponent<RoR2.UI.MPEventSystemLocator>();

                        if (!eventSystemLocator || !eventSystemLocator.eventSystem || eventSystemLocator.eventSystem.currentInputSource != RoR2.UI.MPEventSystem.InputSource.Gamepad)
                        {
                            return;
                        }

                        var content = scrollRect.content;
                        var cellRectTransform = button.GetComponent<RectTransform>();
                        var scrollRectTransform = scrollRect.GetComponent<RectTransform>();

                        var cellPosition = (Vector2)content.InverseTransformPoint(cellRectTransform.position);
                        var insideScrollPosition = (Vector2)scrollRectTransform.InverseTransformPoint(cellRectTransform.position);

                        var scrollHeight = scrollRectTransform.rect.height;
                        var contentHeight = content.rect.height;
                        var cellHeigth = cellRectTransform.rect.height;
                        var halfCellHeight = cellRectTransform.rect.height / 2;
                        var padding = 12;

                        if (cellPosition.y + halfCellHeight + padding > -content.anchoredPosition.y + scrollHeight)
                        {
                            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Math.Max(0, -cellPosition.y - halfCellHeight - padding + scrollHeight));
                        }
                        else if (-content.anchoredPosition.y > cellPosition.y - halfCellHeight - padding)
                        {
                            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Math.Max(0, -cellPosition.y + halfCellHeight + padding));
                        }
                    });
                }
            }
        }

        private static void SetupStripPrefab(Transform ruleStrip)
        {
            if (ruleStrip.GetComponent<ConstrainedScrollRect>())
            {
                return;
            }

            var ruleBookViewerStrip = ruleStrip.GetComponent<RoR2.UI.RuleBookViewerStrip>();

            var choiceContainer = ruleStrip.Find("ChoiceContainer");
            var fitter = choiceContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scrollRect = ruleStrip.gameObject.AddComponent<ConstrainedScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.scrollConstraint = ConstrainedScrollRect.Constraint.OnlyDrag;
            scrollRect.content = choiceContainer.GetComponent<RectTransform>();
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            foreach (var choiceController in ruleBookViewerStrip.choiceControllers)
            {
                SetupRuleButton(ruleBookViewerStrip, choiceController);
            }

            CreateFiller("LeftFiller", choiceContainer).transform.SetAsFirstSibling();
            CreateFiller("RightFiller", choiceContainer).transform.SetAsLastSibling();

            ruleBookViewerStrip.StartCoroutine(RuleScrollStartDelayCoroutine(ruleBookViewerStrip));
        }

        private static IEnumerator RuleScrollStartDelayCoroutine(RoR2.UI.RuleBookViewerStrip ruleBookViewerStrip)
        {
            yield return new WaitForSeconds(0.1F);

            if (ruleBookViewerStrip.choiceControllers.Count == 0)
            {
                yield break;
            }

            var currentController = ruleBookViewerStrip.choiceControllers[Math.Min(ruleBookViewerStrip.currentDisplayChoiceIndex, ruleBookViewerStrip.choiceControllers.Count - 1)];
            ruleBookViewerStrip.currentPosition = -currentController.transform.localPosition.x;
            ruleBookViewerStrip.UpdatePosition();

            var hgButtonHistory = GameObject.Find("RightHandPanel").GetComponentInChildren<RoR2.UI.HGButtonHistory>(true);
            if (hgButtonHistory.lastRememberedGameObject)
            {
                yield break;
            }

            hgButtonHistory.lastRememberedGameObject = currentController.hgButton.gameObject;
        }

        private static GameObject CreateFiller(string name, Transform parent)
        {
            var filler = new GameObject(name);
            filler.transform.SetParent(parent, false);
            filler.transform.SetAsFirstSibling();

            var rectTransform = filler.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(ruleFillerWidth, ruleFillerHeight);

            var leftLayout = filler.AddComponent<LayoutElement>();
            leftLayout.minWidth = ruleFillerWidth;
            leftLayout.minHeight = ruleFillerHeight;

            return filler;
        }

        private static void SetupRuleButton(RoR2.UI.RuleBookViewerStrip ruleBookViewerStrip, RoR2.UI.RuleChoiceController choiceController)
        {
            choiceController.hgButton.onClick.AddListener(OnClick);
            choiceController.hgButton.onSelect.AddListener(OnSelect);

            foreach (var refreshOrder in choiceController.GetComponentsInChildren<RefreshCanvasDrawOrder>(true))
            {
                refreshOrder.enabled = false;
                refreshOrder.canvas.overrideSorting = false;
            }

            void OnClick()
            {
                if (!choiceController.canVote)
                {
                    return;
                }
                choiceController.StartCoroutine(RuleScrollCoroutine(ruleBookViewerStrip, choiceController));
            }

            void OnSelect()
            {
                var eventSystemLocator = choiceController.hgButton.eventSystemLocator;

                if (!eventSystemLocator || !eventSystemLocator.eventSystem || eventSystemLocator.eventSystem.currentInputSource != RoR2.UI.MPEventSystem.InputSource.Gamepad)
                {
                    return;
                }

                choiceController.StartCoroutine(RuleScrollCoroutine(ruleBookViewerStrip, choiceController));
            }
        }

        private static IEnumerator RuleScrollCoroutine(RoR2.UI.RuleBookViewerStrip ruleBookViewerStrip, RoR2.UI.RuleChoiceController choiceController)
        {
            var localTime = 0F;
            var velocity = 0F;
            var endPosition = -choiceController.transform.localPosition.x;

            ruleBookViewerStrip.currentPosition = ruleBookViewerStrip.choiceContainer.transform.localPosition.x;
            
            while (localTime < ruleScrollDuration)
            {
                ruleBookViewerStrip.currentPosition = Mathf.SmoothDamp(ruleBookViewerStrip.currentPosition, endPosition, ref velocity, ruleScrollDuration);
                ruleBookViewerStrip.UpdatePosition();

                yield return new WaitForEndOfFrame();
                localTime += Time.deltaTime;
            }

            ruleBookViewerStrip.currentPosition = endPosition;
            ruleBookViewerStrip.UpdatePosition();
        }

        internal static void RuleBookViewerAwake(On.RoR2.UI.RuleBookViewer.orig_Awake orig, RoR2.UI.RuleBookViewer self)
        {
            var ruleChoicePrefab = self.transform.Find("RuleChoicePrefab");
            foreach(var rcdo in ruleChoicePrefab.GetComponentsInChildren<RefreshCanvasDrawOrder>(true))
            {
                rcdo.enabled = false;
            }

            var selectedHighlight = GameObject.Instantiate(ruleChoicePrefab.transform.Find("ButtonSelectionHighlight, Checkbox"), ruleChoicePrefab, false);
            selectedHighlight.name = "ButtonSelectionHighlight, Selected";

            var highlight = selectedHighlight.Find("Highlight");
            highlight.GetComponent<Image>().color = Color.green;
            
            var highlightRect = highlight.GetComponent<RectTransform>();
            highlightRect.offsetMin = new Vector2();
            highlightRect.offsetMax = new Vector2();

            selectedHighlight.Find("Checkbox").gameObject.SetActive(false);

            var panelPrefab = self.transform.GetComponentInChildren<RuleCategoryController>(true).popoutPanelPrefab;
            var contentContainer = panelPrefab.transform.Find("Canvas/Main/ContentContainer");
            var contentRectTransform = contentContainer as RectTransform;

            var scrollPanel = new GameObject("ScrollPanel");
            scrollPanel.transform.SetParent(contentContainer.parent, false);
            scrollPanel.layer = LayerIndex.ui.intVal;
            scrollPanel.transform.SetSiblingIndex(5);
            var sprt = scrollPanel.AddComponent<RectTransform>();
            sprt.anchorMin = new Vector2(0.5F, 0);
            sprt.anchorMax = new Vector2(0.5F, 0);
            sprt.pivot = new Vector2(0.5F, 0);
            scrollPanel.AddComponent<LayoutElement>();
            scrollPanel.AddComponent<MPEventSystemLocator>();

            var viewPort = new GameObject("ViewPort");
            viewPort.transform.SetParent(scrollPanel.transform, false);
            viewPort.layer = LayerIndex.ui.intVal;
            var vprt = viewPort.AddComponent<RectTransform>();
            vprt.anchorMin = new Vector2(0F, 0F);
            vprt.anchorMax = new Vector2(1F, 1F);
            vprt.sizeDelta = new Vector2();
            viewPort.AddComponent<RectMask2D>();

            var sr = scrollPanel.AddComponent<ScrollRect>();
            sr.content = contentRectTransform;
            sr.viewport = viewPort.transform as RectTransform;
            sr.scrollSensitivity = 30F;
            sr.horizontal = false;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.inertia = false;

            contentContainer.SetParent(viewPort.transform, false);
            var contentFitter = contentContainer.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            var contentImage = contentContainer.GetComponent<Image>();
            contentImage.enabled = true;
            contentImage.color = Color.clear;
            var grid = contentContainer.GetComponent<GridLayoutGroup>();
            grid.spacing = new Vector2(6, 12);
            grid.childAlignment = TextAnchor.UpperCenter;
            contentRectTransform.pivot = new Vector2(0.5F, 1);
            contentRectTransform.anchorMin = new Vector2(0, 0);
            contentRectTransform.anchorMax = new Vector2(1, 0);

            var fitter = scrollPanel.AddComponent<DynamicContentSizeFitter>();
            fitter.maxHeight = 425;
            fitter.useMaxHeight = true;
            fitter.watchTransform = contentContainer as RectTransform;

            orig(self);
        }

        internal static void RuleBookViewerStripSetData(On.RoR2.UI.RuleBookViewerStrip.orig_SetData orig, RoR2.UI.RuleBookViewerStrip self, List<RuleChoiceDef> newChoices, int choiceIndex)
        {
            var oldDisplayChoiceIndex = self.currentDisplayChoiceIndex;

            orig(self, newChoices, choiceIndex);

            if (self.currentDisplayChoiceIndex == oldDisplayChoiceIndex)
            {
                return;
            }

            RuleSelectedHighlightUpdate(self.choiceControllers[oldDisplayChoiceIndex], false);
            RuleSelectedHighlightUpdate(self.choiceControllers[self.currentDisplayChoiceIndex], true);
        }

        private static void RuleSelectedHighlightUpdate(RoR2.UI.RuleChoiceController choiceController, bool active)
        {
            var selectedHighlight = choiceController.transform.Find("ButtonSelectionHighlight, Selected");
            if (!selectedHighlight)
            {
                return;
            }

            selectedHighlight.gameObject.SetActive(active);
        }
    }
}
