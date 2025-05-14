using System;
using System.Collections.Generic;
using System.Text;
using LeTai.Asset.TranslucentImage;
using Rewired.InputManagers;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    public class LoadoutSkillGridController : MonoBehaviour
    {
        private GameObject scrollPanel;
        private GameObject panel;
        public GameObject infoPanel;
        private UIElementAllocator<SkillGridButton> buttons;
        private GameObject buttonPrefab;
        private LoadoutPanelController.Row currentRow;
        private LoadoutButtonInfo currentButton;
        private Transform choiceHighlightRect;
        private MPEventSystemLocator locator;
        private LanguageTextMeshController headerLanguageController;
        public LoadoutButtonInfo selectedButton;

        public static LoadoutSkillGridController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            UserProfile.onLoadoutChangedGlobal += OnLoadoutChangedGlobal;
        }

        public void Start()
        {
            transform.SetAsLastSibling();

            locator = gameObject.AddComponent<MPEventSystemLocator>();

            var controller = infoPanel.GetComponentInParent<CharacterSelectController>();
            var popoutPanelPrefab = controller.transform.Find("SafeArea/RightHandPanel/PopoutPanelContainer/PopoutPanelPrefab");

            scrollPanel = new GameObject("ScrollPanel");
            scrollPanel.SetActive(false);
            scrollPanel.transform.SetParent(transform, false);

            var rect = scrollPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(0, 0);
            rect.pivot = new Vector2(0.5f, 0);

            var verticalLayout = scrollPanel.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(12, 12, 4, 4);
            verticalLayout.spacing = 4;
            verticalLayout.childForceExpandHeight = false;

            var header = new GameObject("Header");
            header.transform.SetParent(scrollPanel.transform, false);
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.sizeDelta = new Vector2(0, 0);
            headerRect.pivot = new Vector2(0.5f, 0);

            var headerLayout = header.AddComponent<LayoutElement>();
            var headerGroup = header.AddComponent<HorizontalLayoutGroup>();
            headerGroup.padding = new RectOffset(80, 80, 0, 0);

            var headerTextObj = new GameObject("Text");
            headerTextObj.transform.SetParent(header.transform, false);
            var headerText = headerTextObj.AddComponent<HGTextMeshProUGUI>();
            headerText.fontSizeMin = 12;
            headerText.fontSizeMax = 24;
            headerText.alignment = TextAlignmentOptions.Center;
            
            headerTextObj.SetActive(false);
            headerLanguageController = headerTextObj.AddComponent<LanguageTextMeshController>();
            headerLanguageController.token = "";
            headerLanguageController.textMeshPro = headerText;
            headerTextObj.SetActive(true);

            var headerButtonObj = Instantiate(popoutPanelPrefab.Find("Canvas/Main/CancelButton").gameObject, header.transform, false);
            headerButtonObj.transform.SetParent(header.transform, false);
            var headerButton = headerButtonObj.GetComponent<HGButton>();
            headerButton.onClick = new Button.ButtonClickedEvent();
            headerButton.onClick.AddListener(Hide);

            var viewPort = new GameObject("ViewPort");
            viewPort.transform.SetParent(scrollPanel.transform, false);

            var viewRect = viewPort.AddComponent<RectTransform>();
            viewRect.anchorMin = new Vector2(0, 0);
            viewRect.anchorMax = new Vector2(1, 1);
            viewRect.sizeDelta = new Vector2(0, 0);
            viewRect.pivot = new Vector2(0.5f, 0);
            viewRect.offsetMin = new Vector2(12, 12);
            viewRect.offsetMax = new Vector2(-12, -12);

            var viewPortLayout = viewPort.AddComponent<LayoutElement>();
            viewPortLayout.flexibleHeight = 10000;

            panel = new GameObject("Panel");
            panel.transform.SetParent(viewPort.transform, false);

            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.sizeDelta = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0.5f, 1);

            var background = scrollPanel.AddComponent<TranslucentImage>();
            background.color = Color.black;
            background.flatten = 0.2f;
            background.material = Addressables.LoadAssetAsync<Material>("TranslucentImage/Default-Translucent.mat").WaitForCompletion();

            var mask = viewPort.AddComponent<RectMask2D>();
            var scroll = scrollPanel.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.content = panelRect;
            scroll.viewport = viewRect;
            scroll.scrollSensitivity = 30;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var layer = ScriptableObject.CreateInstance<UILayer>();
            layer.priority = 20;

            var loadoutPanel = infoPanel.GetComponentInChildren<LoadoutPanelController>();
            var descriptionText = loadoutPanel.hoverTextDescription.transform.parent.gameObject;
            controller.characterSelectBarController.onSurvivorPicked.AddListener((c) => Hide());
            var onDisable = loadoutPanel.gameObject.AddComponent<OnDisableEvent>();
            onDisable.action = new UnityEvent();
            onDisable.action.AddListener(Hide);

            var layerKey = panel.AddComponent<UILayerKey>();
            layerKey.layer = layer;
            layerKey.onBeginRepresentTopLayer = new UnityEvent();
            layerKey.onBeginRepresentTopLayer.AddListener(() =>
            {
                descriptionText.SetActive(true);
            });
            layerKey.onEndRepresentTopLayer = new UnityEvent();
            layerKey.onEndRepresentTopLayer.AddListener(() =>
            {
                descriptionText.SetActive(false);
            });

            var disableButtonObject = new GameObject();
            disableButtonObject.SetActive(false);
            disableButtonObject.transform.SetParent(scrollPanel.transform, false);
            var onDisableButton = disableButtonObject.AddComponent<OnDisableEvent>();
            onDisableButton.action = new UnityEvent();
            onDisableButton.action.AddListener(() => headerButtonObj.SetActive(true));
            var onEnableButton = disableButtonObject.AddComponent<OnEnableEvent>();
            onEnableButton.action = new UnityEvent();
            onEnableButton.action.AddListener(() => headerButtonObj.SetActive(false));

            var grid = panel.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 9;
            grid.cellSize = new Vector2(70, 70);
            grid.spacing = new Vector2(4, 8);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.childAlignment = TextAnchor.UpperCenter;

            var cancelEvent = panel.AddComponent<HGGamepadInputEvent>();
            cancelEvent.actionName = "UICancel";
            cancelEvent.actionEvent = new UnityEvent();
            cancelEvent.actionEvent.AddListener(Hide);
            cancelEvent.requiredTopLayer = layerKey;
            cancelEvent.enabledObjectsIfActive = [disableButtonObject];

            var openEvent = gameObject.AddComponent<HGGamepadInputEvent>();
            openEvent.actionName = "Sprint";
            openEvent.actionEvent = new UnityEvent();
            openEvent.actionEvent.AddListener(() => {
                if (selectedButton && selectedButton.row.CanShowGrid())
                {
                    currentButton = selectedButton;
                    Show(selectedButton.row.row);
                }
            });
            openEvent.requiredTopLayer = infoPanel.GetComponent<UILayerKey>();
            openEvent.enabledObjectsIfActive = [];

            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var image = panel.AddComponent<Image>();
            image.color = Color.clear;

            buttonPrefab = MakeButtonPrefab();
            buttons = new UIElementAllocator<SkillGridButton>(panelRect, buttonPrefab);
        }

        private GameObject MakeButtonPrefab()
        {
            var buttonPrefab = new GameObject("LoadoutButton");
            buttonPrefab.SetActive(false);
            buttonPrefab.transform.SetParent(transform, false);

            var rect = buttonPrefab.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchorMin = rect.pivot;
            rect.anchorMax = rect.pivot;
            rect.sizeDelta = new Vector2(64, 64);

            var layout = buttonPrefab.AddComponent<LayoutElement>();
            layout.preferredHeight = 70;
            layout.preferredWidth = 70;

            var loadoutButton = Instantiate(LoadoutPanelController.loadoutButtonPrefab, buttonPrefab.transform, false);
            var buttonRect = loadoutButton.GetComponent<RectTransform>();
            buttonRect.pivot = new Vector2(0f, 0f);
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.offsetMin = new Vector2(4, 4);
            buttonRect.offsetMax = new Vector2(-4, -4);

            var skillGridButton = buttonPrefab.AddComponent<SkillGridButton>();
            skillGridButton.button = loadoutButton.GetComponent<HGButton>();

            return buttonPrefab;
        }

        public void Hide()
        {
            scrollPanel.SetActive(false);
            if (locator.eventSystem && locator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad)
            {
                if (currentButton)
                {
                    locator.eventSystem.SetSelectedGameObject(currentButton.gameObject);
                    currentButton.GetComponent<HGButton>().OnSelect(null);
                }
                else
                {
                    var button = currentRow.rowData[0].button;
                    locator.eventSystem.SetSelectedGameObject(button.gameObject);
                    button.OnSelect(null);
                }
            }
            currentRow = null;
            currentButton = null;
            buttons.AllocateElements(0);
        }

        public void Show(LoadoutPanelController.Row self)
        {
            if (self == currentRow || scrollPanel.activeSelf)
            {
                return;
            }

            currentRow = self;
            var layer = panel.GetComponent<UILayerKey>();
            var gridLayout = panel.GetComponent<GridLayoutGroup>();
            gridLayout.constraintCount = (int)(GetComponent<RectTransform>().rect.width / (gridLayout.cellSize.x + gridLayout.spacing.x));
            buttons.AllocateElements(self.rowData.Count);
            scrollPanel.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
            for (var i = 0; i < buttons.elements.Count; i++)
            {
                var rowData = self.rowData[i];
                var element = buttons.elements[i];
                element.defIndex = rowData.defIndex;

                var button = element.button;
                var sourceButton = rowData.button as HGButton;

                button.requiredTopLayer = layer;
                (button.targetGraphic as Image).sprite = (sourceButton.targetGraphic as Image).sprite;
                button.interactable = sourceButton.interactable;
                button.disableGamepadClick = sourceButton.disableGamepadClick;
                button.disablePointerClick = sourceButton.disablePointerClick;
                button.hoverToken = sourceButton.hoverToken;
                button.updateTextOnHover = sourceButton.updateTextOnHover;
                button.hoverLanguageTextMeshController = sourceButton.hoverLanguageTextMeshController;
                button.onSelect.AddListener(() =>
                {
                    var panelScrollRect = button.GetComponentInParent<ScrollRect>(true);
                    var buttonRect = button.transform.parent.GetComponent<RectTransform>();

                    if (!locator || !locator.eventSystem || locator.eventSystem.currentInputSource != RoR2.UI.MPEventSystem.InputSource.Gamepad)
                    {
                        return;
                    }

                    var rowsContentPanel = panelScrollRect.content;

                    var rowPosition = (Vector2)panelScrollRect.viewport.InverseTransformPoint(buttonRect.position);
                    var rowsScrollHeight = panelScrollRect.viewport.GetComponent<RectTransform>().rect.height;
                    var halfRowHeight = gridLayout.cellSize.y / 2;

                    if (rowPosition.y - halfRowHeight < 0)
                    {
                        rowsContentPanel.anchoredPosition = new Vector2(
                            rowsContentPanel.anchoredPosition.x,
                            -buttonRect.anchoredPosition.y - rowsScrollHeight + halfRowHeight);
                    }
                    else if (rowPosition.y + halfRowHeight > rowsScrollHeight)
                    {
                        rowsContentPanel.anchoredPosition = new Vector2(
                            rowsContentPanel.anchoredPosition.x,
                            -buttonRect.anchoredPosition.y - halfRowHeight);
                    }
                });
                button.onClick.AddListener(() =>
                {
                    if (sourceButton)
                    {
                        sourceButton.InvokeClick();
                    }
                });

                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                int index = (i + buttons.elements.Count - 1) % buttons.elements.Count;
                navigation.selectOnLeft = buttons.elements[index].button;
                int index2 = (i + buttons.elements.Count + 1) % buttons.elements.Count;
                navigation.selectOnRight = buttons.elements[index2].button;
                if (i - gridLayout.constraintCount >= 0)
                {
                    navigation.selectOnUp = buttons.elements[i - gridLayout.constraintCount].button;
                }
                if (i + gridLayout.constraintCount < buttons.elements.Count)
                {
                    navigation.selectOnDown = buttons.elements[i + gridLayout.constraintCount].button;
                }
                else if (i / gridLayout.constraintCount < buttons.elements.Count / gridLayout.constraintCount)
                {
                    navigation.selectOnDown = buttons.elements[^1].button;
                }
                button.navigation = navigation;

                var tag = button.GetComponent<ViewableTag>();
                var sourceTag = sourceButton.GetComponent<ViewableTag>();
                tag.viewableName = sourceTag.viewableName;
                tag.Refresh();

                var tooltip = button.GetComponent<TooltipProvider>();
                var sourceTooltip = sourceButton.GetComponent<TooltipProvider>();
                tooltip.overrideTitleText = sourceTooltip.overrideTitleText;
                tooltip.overrideBodyText = sourceTooltip.overrideBodyText;
                tooltip.titleColor = sourceTooltip.titleColor;
            }

            if (choiceHighlightRect)
            {
                Destroy(choiceHighlightRect);
            }
            choiceHighlightRect = Instantiate(LoadoutPanelController.rowPrefab.transform.Find("ButtonSelectionHighlight, Checkbox").gameObject, panel.transform).transform;
            Destroy(choiceHighlightRect.GetComponent<RefreshCanvasDrawOrder>());
            UpdateHighlightedChoice();
			var slotLabel = (RectTransform)self.rowPanelTransform.Find("LabelContainer/SlotLabel");
			headerLanguageController.token = slotLabel.GetComponent<LanguageTextMeshController>().token;
            scrollPanel.SetActive(true);
        }

		private void OnLoadoutChangedGlobal(UserProfile userProfile)
		{
            if (!scrollPanel || !scrollPanel.activeInHierarchy)
            {
                return;
            }

			if (userProfile == this.currentRow?.userProfile)
			{
				UpdateHighlightedChoice();
			}
		}

		private void UpdateHighlightedChoice()
		{
			var num = currentRow.findCurrentChoice(currentRow.userProfile.loadout);
            foreach (var button in buttons.elements)
            {
                var colors = button.button.colors;
                if (num == button.defIndex)
                {
                    colors.colorMultiplier = 1f;
                    button.button.onSelect.Invoke();
                    choiceHighlightRect.SetParent(button.transform, false);
                }
                else
                {
                    colors.colorMultiplier = 0.5f;
                }
                button.button.colors = colors;
            }
		}

        private void OnDestroy()
        {
            Instance = null;
        }
    }
}
