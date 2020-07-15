using R2API;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using RoR2.UI.SkinControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    [RequireComponent(typeof(CharacterSelectBarController))]
    public class CharacterSelectBarControllerReplacement : MonoBehaviour
    {
        public GameObject choiceButtonPrefab;
        public GameObject WIPButtonPrefab;
        public GameObject fillButtonPrefab;
        public RectTransform iconContainer;
        public GridLayoutGroup gridLayoutGroup;

        private const int survivorsPerPage = 14;
        private const int buttonSide = 37;

        private UIElementAllocator<SurvivorIconController> survivorIconControllers;
        private CharacterSelectBarController characterSelectBar;
        private HGButtonHistory buttonHistory;

        private int wipSurvivorsCount;
        private int fillerCount;

        private readonly List<SurvivorIndex> survivorIndexList = new List<SurvivorIndex>();
        private readonly List<GameObject> wipSurvivorIcons = new List<GameObject>();
        private readonly List<GameObject> fillerIcons = new List<GameObject>();
        private HGButton previousButtonComponent;
        private HGButton nextButtonComponent;

        public int pageCount { get; private set; } = 1;
        public int currentPageIndex { get; private set; } = 0;
        public bool IsOnFirstPage => currentPageIndex == 0;
        public bool IsOnLastPage => pageCount == currentPageIndex + 1;

        private bool ShouldDisplaySurvivor(SurvivorDef survivorDef)
        {
            return survivorDef != null;
        }

        private void RebuildPage()
        {
            var survivorIndicies = survivorIndexList.Skip(currentPageIndex * survivorsPerPage).Take(survivorsPerPage).ToArray();

            var elements = survivorIconControllers.elements;

            for (var index = 0; index < elements.Count; ++index)
            {
                var element = elements[index];
                if (index >= survivorIndicies.Length)
                {
                    element.gameObject.SetActive(false);
                    continue;
                }
                element.gameObject.SetActive(true);

                var survivorIndex = survivorIndicies[index];
                var survivorDef = SurvivorCatalog.GetSurvivorDef(survivorIndex);
                element.survivorIndex = survivorIndex;
                var buttonComponent = element.GetComponent<HGButton>();

                var survivorDescription = Language.GetString(survivorDef.descriptionToken);
                var length = survivorDescription.IndexOf(Environment.NewLine);
                if (length != -1)
                {
                    survivorDescription = survivorDescription.Substring(0, length);
                }

                var unlockableDef = UnlockableCatalog.GetUnlockableDef(survivorDef.unlockableName);
                if (unlockableDef != null)
                {
                    if (SurvivorCatalog.SurvivorIsUnlockedOnThisClient(survivorIndex))
                    {
                        buttonComponent.hoverToken = Language.GetStringFormatted("CHARACTER_DESCRIPTION_AND_UNLOCK_FORMAT", survivorDescription, unlockableDef.getUnlockedString());
                    }
                    else
                    {
                        buttonComponent.hoverToken = Language.GetStringFormatted("CHARACTER_DESCRIPTION_AND_UNLOCK_FORMAT", Language.GetString("UNIDENTIFIED"), unlockableDef.getHowToUnlockString());
                    }
                }
                else
                {
                    buttonComponent.hoverToken = survivorDescription;
                }
                element.SetFieldValue("shouldRebuild", true);
            }

            int wipCountOnPage;
            if (IsOnLastPage)
            {
                wipCountOnPage = wipSurvivorsCount % survivorsPerPage;
            }
            else
            {
                wipCountOnPage = Mathf.Clamp((currentPageIndex + 1) * survivorsPerPage - survivorIndexList.Count, 0, survivorsPerPage);
            }

            for (var index = 0; index < wipSurvivorsCount; ++index)
            {
                var wipIcon = wipSurvivorIcons[index];
                if (index >= wipCountOnPage)
                {
                    wipIcon.SetActive(false);
                    continue;
                }
                wipIcon.SetActive(true);

                //For some reason colors were changing after reanabling object
                //using this to restore color
                wipIcon.GetComponent<HGButton>().colors = new ColorBlock();
                wipIcon.GetComponent<ButtonSkinController>().InvokeMethod("OnSkinUI");
            }

            for (var index = 0; index < fillerCount; index++)
            {
                fillerIcons[index].gameObject.SetActive(IsOnLastPage);
            }

            if (buttonHistory && buttonHistory.lastRememberedGameObject)
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

            previousButtonComponent.interactable = !IsOnFirstPage;
            nextButtonComponent.interactable = !IsOnLastPage;
        }

        private void GatherSurvivorsInfo()
        {
            var survivorMaxCount = SurvivorCatalog.survivorMaxCount;
            pageCount = survivorMaxCount / survivorsPerPage + (survivorMaxCount % survivorsPerPage > 0 ? 1 : 0);
            for (int index = 0; index < SurvivorCatalog.idealSurvivorOrder.Length; ++index)
            {
                SurvivorIndex survivorIndex = SurvivorCatalog.idealSurvivorOrder[index];
                if (ShouldDisplaySurvivor(SurvivorCatalog.GetSurvivorDef(survivorIndex)))
                {
                    survivorIndexList.Add(survivorIndex);
                }
            }
            wipSurvivorsCount = survivorMaxCount - survivorIndexList.Count;
            fillerCount = pageCount * survivorsPerPage - survivorMaxCount;
        }

        private void GatherCharacterSelectBarInfo()
        {
            choiceButtonPrefab = characterSelectBar.choiceButtonPrefab;
            WIPButtonPrefab = characterSelectBar.WIPButtonPrefab;
            fillButtonPrefab = characterSelectBar.fillButtonPrefab;
            iconContainer = characterSelectBar.iconContainer;
            gridLayoutGroup = characterSelectBar.gridLayoutGroup;
        }

        private void ModifyGridLayout()
        {
            //Define cell size (previous value: (-5, 70))
            gridLayoutGroup.cellSize = new Vector2(70, 70);
            //Allow only 7 characters per row (without constraint it's 8)
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = 7;
        }

        private void PrepareContainer()
        {
            //Removing this component because of strictly defined cell size
            DestroyImmediate(GetComponent<AdjustGridLayoutCellSize>());
            
            //Updating layout
            var layoutElement = GetComponent<LayoutElement>();
            layoutElement.preferredWidth = -1;
            layoutElement.preferredHeight = layoutElement.minHeight;

            ModifyGridLayout();

            AllocateCells();
        }

        private void AllocateCells()
        {
            survivorIconControllers.AllocateElements(Mathf.Min(survivorsPerPage, survivorIndexList.Count));
            survivorIconControllers.elements[0].GetComponent<MPButton>().defaultFallbackButton = true;
            
            for (var i = 0; i < Mathf.Min(wipSurvivorsCount, survivorsPerPage); i++)
            {
                var wipIcon = Instantiate(WIPButtonPrefab, iconContainer);
                wipSurvivorIcons.Add(wipIcon);
            }

            for (var i = 0; i < fillerCount; i++)
            {
                fillerIcons.Add(Instantiate(fillButtonPrefab, iconContainer));
            }
        }

        private void SetupPagingStuff()
        {
            var mpEventSystemLocator = GetComponent<MPEventSystemLocator>();
            var survivorChoiseGrid = transform.parent.gameObject;
            var leftHandPanel = survivorChoiseGrid.transform.parent.gameObject;
            var subheaderPanel = leftHandPanel.transform.Find("SurvivorInfoPanel, Active (Layer: Secondary)").Find("SubheaderPanel (Overview, Skills, Loadout)");
            var uiLayerKey = leftHandPanel.GetComponent<UILayerKey>();

            previousButtonComponent = SetupPagingButton("Previous", "Left", SelectPreviousPage, nameof(RewiredConsts.Action.UITabLeft), 0);
            nextButtonComponent = SetupPagingButton("Next", "Right", SelectNextPage, nameof(RewiredConsts.Action.UITabRight), 2);

            HGButton SetupPagingButton(string prefix, string buttonPrefix, System.Action action, string actionName, int siblingIndex)
            {
                var buttonContainer = new GameObject($"{prefix}ButtonContainer");
                buttonContainer.transform.SetParent(transform.parent, false);
                buttonContainer.transform.SetSiblingIndex(siblingIndex);

                var buttonVerticalLayout = buttonContainer.AddComponent<VerticalLayoutGroup>();
                buttonVerticalLayout.childControlHeight = true;
                buttonVerticalLayout.childControlWidth = true;

                var button = Resources.Load<GameObject>($"prefabs/ui/controls/buttons/{buttonPrefix}Button").InstantiateClone($"{prefix}Page");
                button.transform.SetParent(buttonContainer.transform, false);

                var buttonComponent = button.GetComponent<HGButton>();
                buttonComponent.onClick = new Button.ButtonClickedEvent();
                buttonComponent.onClick.AddListener(new UnityEngine.Events.UnityAction(action));

                var buttonLayout = button.GetComponent<LayoutElement>();
                buttonLayout.preferredWidth = buttonSide;
                buttonLayout.minWidth = buttonSide;

                var glyph = subheaderPanel.Find($"GenericGlyph ({buttonPrefix})").gameObject.InstantiateClone($"{prefix}Glyph");
                glyph.transform.SetParent(buttonContainer.transform, false);
                glyph.transform.SetSiblingIndex(0);
                glyph.SetActive(mpEventSystemLocator.eventSystem && mpEventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad);

                var glyphLayout = glyph.GetComponent<LayoutElement>();
                glyphLayout.minWidth = buttonSide;

                var glyphText = glyph.transform.Find("Text").gameObject;

                var glyphTextLayout = glyphText.GetComponent<LayoutElement>();
                glyphTextLayout.preferredHeight = buttonSide;
                glyphTextLayout.preferredWidth = buttonSide;

                var pageEvent = survivorChoiseGrid.AddComponent<HGGamepadInputEvent>();
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
            GatherCharacterSelectBarInfo();
            
            survivorIconControllers = new UIElementAllocator<SurvivorIconController>(iconContainer, choiceButtonPrefab);
            
            GatherSurvivorsInfo();

            SetupPagingStuff();
            PrepareContainer();

            RebuildPage();
        }

        public void SelectNextPage()
        {
            if (IsOnLastPage)
            {
                return;
            }
            currentPageIndex++;
            RebuildPage();
        }

        public void SelectPreviousPage()
        {
            if (IsOnFirstPage)
            {
                return;
            }
            currentPageIndex--;
            RebuildPage();
        }
    }
}
