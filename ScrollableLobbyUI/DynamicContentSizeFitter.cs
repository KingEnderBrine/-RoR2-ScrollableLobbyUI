using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ScrollableLobbyUI
{
    [RequireComponent(typeof(LayoutElement))]
    public class DynamicContentSizeFitter : MonoBehaviour
    {
        private LayoutElement layoutElement;
        public RectTransform watchTransform;
        public float maxWidth = -1;
        public float maxHeight = -1;
        public bool useMaxHeight = false;
        public bool useMaxWidth = false;

        public float MaxHeight => maxHeight < 0 ? float.MaxValue : maxHeight;
        public float MaxWidth => maxWidth < 0 ? float.MaxValue : maxWidth;

        private void Start()
        {
            layoutElement = GetComponent<LayoutElement>();
        }

        private void OnGUI()
        {
            var watchRect = watchTransform.rect;
            if (useMaxWidth)
            {
                layoutElement.preferredWidth = Mathf.Min(watchRect.width, MaxWidth);
            }
            if (useMaxHeight)
            {
                layoutElement.preferredHeight = Mathf.Min(watchRect.height, MaxHeight);
            }
        }
    }
}
