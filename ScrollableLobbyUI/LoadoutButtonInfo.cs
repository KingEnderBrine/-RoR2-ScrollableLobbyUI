using System;
using System.Collections.Generic;
using System.Text;
using RoR2.UI;
using UnityEngine;

namespace ScrollableLobbyUI
{
    public class LoadoutButtonInfo : MonoBehaviour
    {
        public LoadoutRowInfo row;

        private void Start()
        {
            var button = GetComponent<HGButton>();
            button.onSelect.AddListener(() =>
            {
                LoadoutSkillGridController.Instance.selectedButton = this;
                LoadoutRowInfo.selectedRow = row;
            });
        }
    }
}
