using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    public class ButtonsNavigationController : MonoBehaviour
    {
        private bool buttonsWereActive;
        private MPEventSystemLocator eventSystemLocator;
        public UILayerKey requiredTopLayer;
        public LoadoutPanelController loadoutPanel;

        protected MPEventSystem eventSystem
        {
            get
            {
                return eventSystemLocator?.eventSystem;
            }
        }

        public void Awake()
        {
            eventSystemLocator = GetComponent<MPEventSystemLocator>();
            buttonsWereActive = true;
        }

        public void Update()
        {
            var flag = ButtonsShouldBeActive();
            if (buttonsWereActive != flag && loadoutPanel)
            {
                foreach (var buttonComponent in loadoutPanel.GetComponentsInChildren<HGButton>())
                {
                    buttonComponent.navigation = flag ? 
                        new Navigation() { mode = Navigation.Mode.Automatic } : 
                        new Navigation() { mode = Navigation.Mode.None };
                }
            }

            buttonsWereActive = flag;
        }

        protected bool ButtonsShouldBeActive()
        {
            return gameObject.activeInHierarchy && eventSystem && (eventSystem.currentInputSource == MPEventSystem.InputSource.MouseAndKeyboard || ((!requiredTopLayer || requiredTopLayer.representsTopLayer) && eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad));
        }
    }
}
