using R2API;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    [RequireComponent(typeof(CharacterSelectBarController))]
    public class CharacterSelectBarControllerReplacement : MonoBehaviour
    {
        public GameObject choiceButtonPrefab;
        public GameObject fillButtonPrefab;
        public RectTransform iconContainer;
        public GridLayoutGroup gridLayoutGroup;

        private const int survivorsPerPage = 14;
        private const int buttonSide = 37;
        private const float delayBeforeForcingSurvivor = 0.1f;

        private UIElementAllocator<SurvivorIconController> survivorIconControllers;
        private CharacterSelectBarController characterSelectBar;
        private HGButtonHistory buttonHistory;
        private MPEventSystemLocator eventSystemLocator;

        private int fillerCount;

        private readonly List<SurvivorIndex> survivorIndexList = new List<SurvivorIndex>();
        private readonly List<GameObject> fillerIcons = new List<GameObject>();
        private HGButton previousButtonComponent;
        private HGButton nextButtonComponent;

        public int pageCount { get; private set; } = 1;
        public int currentPageIndex { get; private set; } = 0;
        public bool IsOnFirstPage => currentPageIndex == 0;
        public bool IsOnLastPage => pageCount == currentPageIndex + 1;

        private bool isEclipseRun
        {
            get
            {
                return PreGameController.instance && PreGameController.instance.gameModeIndex == GameModeCatalog.FindGameModeIndex("EclipseRun");
            }
        }

        private bool ShouldDisplaySurvivor(SurvivorDef survivorDef)
        {
            if (!isEclipseRun)
            {
                return survivorDef != null;
            }

            return (SurvivorIndex)EclipseRun.cvEclipseSurvivorIndex.value == survivorDef.survivorIndex;
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

            for (var index = 0; index < fillerCount; index++)
            {
                fillerIcons[index].gameObject.SetActive(IsOnLastPage);
            }

            if (buttonHistory && buttonHistory.lastRememberedGameObject)
            {
                if (eventSystemLocator && eventSystemLocator.eventSystem)
                {
                    if (eventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad)
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
            for (int index = 0; index < SurvivorCatalog.idealSurvivorOrder.Length; ++index)
            {
                SurvivorIndex survivorIndex = SurvivorCatalog.idealSurvivorOrder[index];
                if (ShouldDisplaySurvivor(SurvivorCatalog.GetSurvivorDef(survivorIndex)))
                {
                    survivorIndexList.Add(survivorIndex);
                }
            }

            var survivorMaxCount = survivorIndexList.Count;
            pageCount = survivorMaxCount / survivorsPerPage + (survivorMaxCount % survivorsPerPage > 0 ? 1 : 0);

            fillerCount = pageCount * survivorsPerPage - survivorMaxCount;
        }

        private void GatherCharacterSelectBarInfo()
        {
            choiceButtonPrefab = characterSelectBar.choiceButtonPrefab;
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
            layoutElement.preferredHeight = 156;

            ModifyGridLayout();

            AllocateCells();

            var choiseGridContainer = new GameObject("SurvivorChoiseGridContainer");
            choiseGridContainer.transform.SetParent(transform.parent, false);
            choiseGridContainer.transform.SetSiblingIndex(2);
            transform.SetParent(choiseGridContainer.transform, false);

            var choiseGridHorizontalLayout = choiseGridContainer.AddComponent<HorizontalLayoutGroup>();
            var choiseGridLayout = choiseGridContainer.AddComponent<LayoutElement>();
            choiseGridLayout.preferredHeight = 156;

            var choiseGridContainerContentFitter = choiseGridContainer.AddComponent<ContentSizeFitter>();
            choiseGridContainerContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            choiseGridContainerContentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

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
            survivorIconControllers.AllocateElements(Mathf.Min(survivorsPerPage, survivorIndexList.Count));
            survivorIconControllers.elements[0].GetComponent<MPButton>().defaultFallbackButton = true;
            
            for (var i = 0; i < fillerCount; i++)
            {
                fillerIcons.Add(Instantiate(fillButtonPrefab, iconContainer));
            }
        }

        private void SetupPagingStuff()
        {
            var mpEventSystemLocator = GetComponent<MPEventSystemLocator>();
            var survivorChoiseGrid = transform.parent.gameObject;
            var uiLayerKey = survivorChoiseGrid.GetComponentInParent<UILayerKey>();


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
                buttonContainerLayout.preferredHeight = 156;

                var button = Resources.Load<GameObject>($"prefabs/ui/controls/buttons/{buttonPrefix}Button").InstantiateClone($"{prefix}Page");
                button.transform.SetParent(buttonContainer.transform, false);
                button.layer = 5;

                var buttonComponent = button.GetComponent<HGButton>();
                buttonComponent.onClick = new Button.ButtonClickedEvent();
                buttonComponent.onClick.AddListener(new UnityEngine.Events.UnityAction(action));

                var buttonLayout = button.GetComponent<LayoutElement>();
                buttonLayout.preferredWidth = buttonSide;
                buttonLayout.minWidth = buttonSide;

                //var glyph = subheaderPanel.Find($"GenericGlyph ({buttonPrefix})").gameObject.InstantiateClone($"{prefix}Glyph");
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

                //var glyphText = glyph.transform.Find("Text").gameObject;
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
                inputBindingDisplayController.InvokeMethod("Awake");


                var glyphTextLayout = glyphText.AddComponent<LayoutElement>();
                glyphTextLayout.preferredHeight = buttonSide;
                glyphTextLayout.preferredWidth = buttonSide;

                var tmpBombDropShadows = Resources.Load<TMP_FontAsset>("tmpfonts/bombardier/tmpbombdropshadow.asset");
                var hgTextMeshPro = glyphText.AddComponent<HGTextMeshProUGUI>();
                hgTextMeshPro.raycastTarget = false;
                hgTextMeshPro.text = $"<sprite=\"tmpsprXboxOneGlyphs\" name=\"texXBoxOneGlyphs_{glyphIndex}\">";
                hgTextMeshPro.UpdateFontAsset();
                hgTextMeshPro.fontSize = 24;
                hgTextMeshPro.SetFieldValue("m_fontSizeBase", 16F);
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

                var material = new Material(Shader.Find("TextMeshPro/Sprite"));
                var texture = Resources.Load<Texture>("sprite assets/texXBoxOneGlyphs.png");

                var glyphTMPCanvasRenderer = glyphTMP.AddComponent<CanvasRenderer>();
                glyphTMPCanvasRenderer.SetMaterial(material, texture);

                var glyphTMPSubMesh = glyphTMP.AddComponent<TMP_SubMeshUI>();
                glyphTMPSubMesh.fontAsset = tmpBombDropShadows;
                glyphTMPSubMesh.spriteAsset = Resources.Load<TMP_SpriteAsset>("sprite assets/tmpsprXboxOneGlyphs.asset");
                glyphTMPSubMesh.SetFieldValue("m_TextComponent", hgTextMeshPro);

                var pageEvent = survivorChoiseGrid.AddComponent<HGGamepadInputEvent>();
                pageEvent.requiredTopLayer = uiLayerKey;
                pageEvent.actionName = actionName;
                pageEvent.enabledObjectsIfActive = new GameObject[] { glyph };
                pageEvent.actionEvent = new UnityEngine.Events.UnityEvent();
                pageEvent.actionEvent.AddListener(new UnityEngine.Events.UnityAction(action));

                return buttonComponent;
            }
        }

        private void Start()
        {
            buttonHistory = GetComponent<HGButtonHistory>();
            characterSelectBar = GetComponent<CharacterSelectBarController>();
            eventSystemLocator = GetComponent<MPEventSystemLocator>();
            GatherCharacterSelectBarInfo();
            
            survivorIconControllers = new UIElementAllocator<SurvivorIconController>(iconContainer, choiceButtonPrefab);
            
            StartCoroutine(StartDelayCoroutine());
        }

        private IEnumerator StartDelayCoroutine()
        {
            yield return new WaitForSeconds(delayBeforeForcingSurvivor);

            GatherSurvivorsInfo();

            PrepareContainer();
            SetupPagingStuff();

            RebuildPage();

            if (!isEclipseRun)
            {
                yield break;
            }
            survivorIconControllers.elements[0].PushSurvivorIndexToCharacterSelect();
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

        public void OpenPageWithCharacter(SurvivorIndex survivorIndex)
        {
            var index = survivorIndexList.FindIndex(el => el == survivorIndex);
            if (index == -1)
            {
                return;
            }
            currentPageIndex = index / survivorsPerPage;
            RebuildPage();
        }
    }
}
