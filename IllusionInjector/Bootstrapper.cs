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
