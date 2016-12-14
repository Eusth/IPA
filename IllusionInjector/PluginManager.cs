using IllusionPlugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace IllusionInjector
{
    public static class PluginManager
    {
        private static List<IPlugin> _Plugins = null;

        /// <summary>
        /// Gets the list of loaded plugins and loads them if necessary.
        /// </summary>
        public static IEnumerable<IPlugin> Plugins
        {
            get
            {
                if(_Plugins == null)
                {
                    LoadPlugins();
                }
                return _Plugins;
            }
        }


        private static void LoadPlugins()
        {
            string pluginDirectory = Path.Combine(Environment.CurrentDirectory, "Plugins");

            // Process.GetCurrentProcess().MainModule crashes the game and Assembly.GetEntryAssembly() is NULL,
            // so we need to resort to P/Invoke
            string exeName = Path.GetFileNameWithoutExtension(AppInfo.StartupPath);
            Console.WriteLine(exeName);
            _Plugins = new List<IPlugin>();

            if (!Directory.Exists(pluginDirectory)) return;
            
            String[] files = Directory.GetFiles(pluginDirectory, "*.dll");
            foreach (var s in files)
            {
                _Plugins.AddRange(LoadPluginsFromFile(Path.Combine(pluginDirectory, s), exeName));
            }
            

            // DEBUG
            Console.WriteLine("-----------------------------");
            Console.WriteLine("Loading plugins from {0} and found {1}", pluginDirectory, _Plugins.Count);
            Console.WriteLine("-----------------------------");
            foreach (var plugin in _Plugins)
            {

                Console.WriteLine(" {0}: {1}", plugin.Name, plugin.Version);
            }
            Console.WriteLine("-----------------------------");
 
        }

        private static IEnumerable<IPlugin> LoadPluginsFromFile(string file, string exeName)
        {
            List<IPlugin> plugins = new List<IPlugin>();

            if (!File.Exists(file) || !file.EndsWith(".dll", true, null))
                return plugins;

            try
            {
                Assembly assembly = Assembly.LoadFrom(file);

                foreach (Type t in assembly.GetTypes())
                {
                    if (t.GetInterface("IPlugin") != null)
                    {
                        try
                        {

                            IPlugin pluginInstance = Activator.CreateInstance(t) as IPlugin;
                            string[] filter = null;

                            if (pluginInstance is IEnhancedPlugin)
                            {
                                filter = ((IEnhancedPlugin)pluginInstance).Filter;
                            }
                            
                            if(filter == null || Enumerable.Contains(filter, exeName, StringComparer.OrdinalIgnoreCase))
                                plugins.Add(pluginInstance);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

            }
            catch (Exception)
            {
            }

            return plugins;
        }

        public class AppInfo
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, ExactSpelling = false)]
            private static extern int GetModuleFileName(HandleRef hModule, StringBuilder buffer, int length);
            private static HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);
            public static string StartupPath
            {
                get
                {
                    StringBuilder stringBuilder = new StringBuilder(260);
                    GetModuleFileName(NullHandleRef, stringBuilder, stringBuilder.Capacity);
                    return stringBuilder.ToString();
                }
            }
        }

    }
}
