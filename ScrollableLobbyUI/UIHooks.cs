using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    internal static class UIHooks
    {
        internal static void LoadoutPanelControllerAwake(On.RoR2.UI.LoadoutPanelController.orig_Awake orig, LoadoutPanelController self)
        {
            orig(self);

            //Adding container on top of LoadoutPanelController
            AddScrollPanel(self.transform, "LoadoutScrollPanel");
            //Adding container on top of SkillPanel
            var skillScrollPanel = AddScrollPanel(self.transform.parent.parent.Find("SkillPanel"), "LoadoutScrollPanel");

            //Adding scrolling with stick for skills overview
            var scrollHelper = skillScrollPanel.AddComponent<GamepadScrollRectHelper>();
            scrollHelper.requiredTopLayer = skillScrollPanel.transform.parent.parent.GetComponent<UILayerKey>();

            GameObject AddScrollPanel(Transform panel, string name)
            {
                var scrollPanel = new GameObject(name);
                scrollPanel.layer = 5;
                scrollPanel.transform.SetParent(panel.transform.parent, false);
                panel.transform.SetParent(scrollPanel.transform, false);

                scrollPanel.AddComponent<MPEventSystemLocator>();

                var scrollPanelImage = scrollPanel.AddComponent<Image>();
                scrollPanelImage.raycastTarget = false;

                var scrollPanelMask = scrollPanel.AddComponent<Mask>();
                scrollPanelMask.showMaskGraphic = false;

                var scrollPanelRect = scrollPanel.AddComponent<ScrollRect>();
                scrollPanelRect.horizontal = false;
                scrollPanelRect.content = panel.GetComponent<RectTransform>();
                scrollPanelRect.scrollSensitivity = 30;
                scrollPanelRect.movementType = ScrollRect.MovementType.Clamped;

                //Adding ContentSizeFilter, otherwise childs would have been wrong size
                var panelContentSizeFilter = panel.gameObject.AddComponent<ContentSizeFitter>();
                panelContentSizeFilter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                panelContentSizeFilter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var scrollPanelRectTransform = scrollPanelRect.GetComponent<RectTransform>();
                scrollPanelRectTransform.pivot = new Vector2(0.5F, 1F);
                scrollPanelRectTransform.anchorMin = new Vector2(0, 0);
                scrollPanelRectTransform.anchorMax = new Vector2(1, 1);
                scrollPanelRectTransform.offsetMin = new Vector2(4, 4);
                scrollPanelRectTransform.offsetMax = new Vector2(4, 4);

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
        }
        
        internal static void LoadoutPanelControllerRowFinishSetup(On.RoR2.UI.LoadoutPanelController.Row.orig_FinishSetup orig, object self, bool addWIPIcons)
        {
            orig(self, addWIPIcons);
            var buttonContainerTransform = self.GetFieldValue<RectTransform>("buttonContainerTransform");
            foreach (var button in buttonContainerTransform.GetComponentsInChildren<HGButton>())
            {
                //Scroll to selected row if it's not fully visible
                button.onSelect.AddListener(new UnityEngine.Events.UnityAction(() =>
                {
                    var scrollRect = button.GetComponentInParent<ScrollRect>();
                    var eventSystemLocator = scrollRect.GetComponent<MPEventSystemLocator>();

                    if (!eventSystemLocator || !eventSystemLocator.eventSystem || eventSystemLocator.eventSystem.currentInputSource != MPEventSystem.InputSource.Gamepad)
                    {
                        return;
                    }

                    var contentPanel = scrollRect.content;
                    var rectTransform = button.transform.parent.parent.GetComponent<RectTransform>();

                    var rowPosition = (Vector2)scrollRect.transform.InverseTransformPoint(rectTransform.position);
                    var scrollHeight = scrollRect.GetComponent<RectTransform>().rect.height;
                    var halfRowHeight = rectTransform.rect.height / 2;

                    if (rowPosition.y - halfRowHeight < -scrollHeight)
                    {
                        contentPanel.anchoredPosition = new Vector2(contentPanel.anchoredPosition.x,
                            -rectTransform.anchoredPosition.y - scrollHeight + halfRowHeight);
                    }

                    if (rowPosition.y + halfRowHeight > 0)
                    {
                        contentPanel.anchoredPosition = new Vector2(contentPanel.anchoredPosition.x,
                            -rectTransform.anchoredPosition.y - halfRowHeight);
                    }
                }));
            }
        }

        internal static void LoadoutPanelControllerRowCtor(On.RoR2.UI.LoadoutPanelController.Row.orig_ctor orig, object self, LoadoutPanelController owner, int bodyIndex, string titleToken)
        {
            orig(self, owner, bodyIndex, titleToken);

            //Disabling sorting override
            var highlightRect = self.GetFieldValue<RectTransform>("choiceHighlightRect");
            highlightRect.GetComponent<RefreshCanvasDrawOrder>().enabled = false;
            highlightRect.GetComponent<Canvas>().overrideSorting = false;
        }

        internal static void CharacterSelectControllerRebuildLocal(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(23),
                x => x.MatchBr(out var label),
                x => x.MatchLdarg(0),
                x => x.MatchLdarg(0));
            c.Index += 4;
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

            var fieldInfo = typeof(CharacterSelectController).GetNestedType("StripDisplayData", BindingFlags.NonPublic).GetField("enabled");
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
            });
        }
    }
}
