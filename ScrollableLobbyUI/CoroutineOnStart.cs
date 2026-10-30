using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ScrollableLobbyUI
{
    public class CoroutineOnStart : MonoBehaviour
    {
        public IEnumerator coroutine;

        public void Start()
        {
            if (coroutine != null)
            {
                StartCoroutine(coroutine);
                coroutine = null;
            }
        }
    }
}
