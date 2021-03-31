using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ScrollableLobbyUI
{
    public class ClearTextOnDisable : MonoBehaviour
    {
        public List<TextMeshProUGUI> textObjects;

        private void OnDisable()
        {
            textObjects?.ForEach(el => el.SetText(String.Empty));
        }
    }
}
