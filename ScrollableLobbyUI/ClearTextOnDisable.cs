using System;
using System.Collections.Generic;
using RoR2.UI;
using UnityEngine;

namespace ScrollableLobbyUI
{
    public class ClearTextOnDisable : MonoBehaviour
    {
        public List<LanguageTextMeshController> textObjects;

        private void OnDisable()
        {
            if (textObjects is null)
            {
                return;
            }

            foreach (var textObj in textObjects)
            {
                if (textObj)
                {
                    textObj.token = String.Empty;
                }
            }
        }
    }
}
