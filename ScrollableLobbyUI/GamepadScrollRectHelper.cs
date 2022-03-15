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

        private void Start()
        {
            scrollRect = GetComponent<ScrollRect>();
            eventSystemLocator = GetComponent<MPEventSystemLocator>();
        }

        private bool GamepadIsCurrentInputSource()
        {
            return eventSystemLocator && eventSystemLocator.eventSystem && eventSystemLocator.eventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad;
        }

        private bool CanAcceptInput()
        {
            return !requiredTopLayer || requiredTopLayer.representsTopLayer;
        }

        private void Update()
        {
            if (!this.GamepadIsCurrentInputSource() || !CanAcceptInput())
                return;

            var height = scrollRect.content.rect.height;
            var axis = eventSystemLocator.eventSystem.player.GetAxis(13);
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + axis * stickScale * Time.unscaledDeltaTime / height);
        }
    }
}
