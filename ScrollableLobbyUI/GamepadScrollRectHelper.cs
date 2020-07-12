using RoR2.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    internal class GamepadScrollRectHelper : MonoBehaviour
    {
        public UILayerKey requiredTopLayer;
        public float stickScale = 3000;
        private ScrollRect scrollRect;
        private MPEventSystemLocator eventSystemLocator;
        private bool hasInitialized;

        private void Start()
        {
            this.Initialize();
        }

        private void Initialize()
        {
            if (hasInitialized)
                return;
            hasInitialized = true;
            scrollRect = GetComponent<ScrollRect>();
            eventSystemLocator = GetComponent<MPEventSystemLocator>();
        }

        private bool GamepadIsCurrentInputSource()
        {
            return hasInitialized && eventSystemLocator.eventSystem && eventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad;
        }

        private bool CanAcceptInput()
        {
            return !requiredTopLayer || requiredTopLayer.representsTopLayer;
        }

        private void Update()
        {
            this.Initialize();
            if (!this.GamepadIsCurrentInputSource())
                return;
            if (eventSystemLocator && eventSystemLocator.eventSystem && CanAcceptInput())
            {
                float height = scrollRect.content.rect.height;
                float axis1 = eventSystemLocator.eventSystem.player.GetAxis(13);
                scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + axis1 * stickScale * Time.unscaledDeltaTime / height);
            }
        }
    }
}
