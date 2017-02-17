using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace IllusionInjector
{
    public class PluginComponent : MonoBehaviour
    {
        private CompositePlugin plugins;
        private bool freshlyLoaded = false;

        public static PluginComponent Create()
        {
            return new GameObject("IPA_PluginManager").AddComponent<PluginComponent>();
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            plugins = new CompositePlugin(PluginManager.Plugins);
            plugins.OnApplicationStart();
        }

        void Start()
        {
            OnLevelWasLoaded(Application.loadedLevel);
        }

        void Update()
        {
            if (freshlyLoaded)
            {
                freshlyLoaded = false;
                plugins.OnLevelWasInitialized(Application.loadedLevel);
            }
            plugins.OnUpdate();
        }

        void LateUpdate()
        {
            plugins.OnLateUpdate();
        }

        void FixedUpdate()
        {
            plugins.OnFixedUpdate();
        }

        void OnDestroy()
        {
            plugins.OnApplicationQuit();

            Create();
        }

        void OnLevelWasLoaded(int level)
        {
            plugins.OnLevelWasLoaded(level);
            freshlyLoaded = true;
        }

    }
}
