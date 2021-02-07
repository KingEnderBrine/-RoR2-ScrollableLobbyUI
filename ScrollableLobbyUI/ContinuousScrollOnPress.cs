using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    public class ContinuousScrollOnPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public float sensitivity = 150;
        public MoveDirection moveDirection = MoveDirection.None;
        public ScrollRect scrollRect;

        private bool mouseDown;
        private HGButton button;

        private void Awake()
        {
            button = GetComponent<HGButton>();
        }

        private void OnEnable()
        {
            mouseDown = false;
        }

        private void Update()
        {
            if (!mouseDown || !button.interactable)
            {
                return;
            }
            var timedSensitivity = sensitivity * Time.deltaTime;
            switch (moveDirection)
            {
                case MoveDirection.Left:
                    scrollRect.content.localPosition -= new Vector3(timedSensitivity, 0);
                    break;
                case MoveDirection.Up:
                    scrollRect.content.localPosition += new Vector3(0, timedSensitivity);
                    break;
                case MoveDirection.Right:
                    scrollRect.content.localPosition += new Vector3(timedSensitivity, 0);
                    break;
                case MoveDirection.Down:
                    scrollRect.content.localPosition -= new Vector3(0, timedSensitivity);
                    break;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            mouseDown = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            mouseDown = false;
        }
    }
}
