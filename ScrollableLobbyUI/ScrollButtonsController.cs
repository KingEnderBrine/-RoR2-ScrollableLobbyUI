using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    public class ScrollButtonsController : MonoBehaviour
    {
        public GameObject left;
        public GameObject right;
        public float deadzone = 0.0001F;

        private ScrollRect scrollRect;
        private RectTransform rectTransform;

        private bool _contentOutOfRect;
        public bool ContentOutOfRect
        {
            get => _contentOutOfRect;
            set
            {
                if (_contentOutOfRect != value)
                {
                    _contentOutOfRect = value;
                    OnContentOutOfRectChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ContentOutOfRect)));
                }
            }
        }

        public event PropertyChangedEventHandler OnContentOutOfRectChanged;

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            ContentOutOfRect = rectTransform.rect.width < scrollRect.content.rect.width;

            left.SetActive(ContentOutOfRect && scrollRect.horizontalNormalizedPosition > deadzone);
            right.SetActive(ContentOutOfRect && scrollRect.horizontalNormalizedPosition < 1 - deadzone);

        }
    }
}
