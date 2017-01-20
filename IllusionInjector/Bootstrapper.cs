using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IllusionInjector
{
    class Bootstrapper : MonoBehaviour
    {
        public event Action Destroyed = delegate {};
        
        void Awake()
        {
            if (Environment.CommandLine.Contains("--verbose") && !Screen.fullScreen)
            {
                Windows.GuiConsole.CreateConsole();
            }
        }

        void Start()
        {
            Destroy(gameObject);
        }
        void OnDestroy()
        {
            Destroyed();
        }
    }
}
