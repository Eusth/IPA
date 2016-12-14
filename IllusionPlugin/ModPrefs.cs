using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IllusionPlugin
{
    /// <summary>
    /// Allows to get and set preferences for your mod. 
    /// </summary>
    public static class ModPrefs
    {
        private static IniFile _instance;
        private static IniFile Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new IniFile(Path.Combine(Environment.CurrentDirectory, "UserData/modprefs.ini"));
                }
                return _instance;
            }
        }


        /// <summary>
        /// Gets a string from the ini.
        /// </summary>
        /// <param name="section">Section of the key.</param>
        /// <param name="name">Name of the key.</param>
        /// <param name="defaultValue">Value that should be used when no value is found.</param>
        /// <param name="autoSave">Whether or not the default value should be written if no value is found.</param>
        /// <returns></returns>
        public static string GetString(string section, string name, string defaultValue = "", bool autoSave = false)
        {
            string value = Instance.IniReadValue(section, name);
            if (value != "")
                return value;
            else if (autoSave)
                SetString(section, name, defaultValue);

            return defaultValue;
        }

        /// <summary>
        /// Gets an int from the ini.
        /// </summary>
        /// <param name="section">Section of the key.</param>
        /// <param name="name">Name of the key.</param>
        /// <param name="defaultValue">Value that should be used when no value is found.</param>
        /// <param name="autoSave">Whether or not the default value should be written if no value is found.</param>
        /// <returns></returns>
        public static int GetInt(string section, string name, int defaultValue = 0, bool autoSave = false)
        {
            int value;
            if (int.TryParse(Instance.IniReadValue(section, name), out value))
                return value;
            else if (autoSave)
                SetInt(section, name, defaultValue);
                
            return defaultValue;
        }


        /// <summary>
        /// Gets a float from the ini.
        /// </summary>
        /// <param name="section">Section of the key.</param>
        /// <param name="name">Name of the key.</param>
        /// <param name="defaultValue">Value that should be used when no value is found.</param>
        /// <param name="autoSave">Whether or not the default value should be written if no value is found.</param>
        /// <returns></returns>
        public static float GetFloat(string section, string name, float defaultValue = 0f, bool autoSave = false)
        {
            float value;
            if (float.TryParse(Instance.IniReadValue(section, name), out value))
                return value;
            else if (autoSave)
                SetFloat(section, name, defaultValue);

            return defaultValue;
        }

        /// <summary>
        /// Gets a bool from the ini.
        /// </summary>
        /// <param name="section">Section of the key.</param>
        /// <param name="name">Name of the key.</param>
        /// <param name="defaultValue">Value that should be used when no value is found.</param>
        /// <param name="autoSave">Whether or not the default value should be written if no value is found.</param>
        /// <returns></returns>
        public static bool GetBool(string section, string name, bool defaultValue = false, bool autoSave = false)
        {
            string sVal = GetString(section, name, null);
            if (sVal == "1" || sVal == "0")
            {
                return sVal == "1";
            } else if (autoSave)
            {
                SetBool(section, name, defaultValue);
            }

            return defaultValue;
        }


        /// <summary>
        /// Checks whether or not a key exists in the ini.
        /// </summary>
        /// <param name="section">Section of the key.</param>
        /// <param name="name">Name of the key.</param>
        /// <returns></returns>
        public static bool HasKey(string section, string name)
        {
            return Instance.IniReadValue(section, name) != null;
        }

        /// <summary>
        /// Sets a float in the ini.
        /// </summary>
        /// <param name="section">Section of the key.</param>
        /// <param name="name">Name of the key.</param>
        /// <param name="value">Value that should be written.</param>
        public static void SetFloat(string section, string name, float value)
        {
            Instance.IniWriteValue(section, name, value.ToString());
        }

        /// <summary>
        /// Sets an int in the ini.
        /// </summary>
        /// <param name="section">Section of the key.</param>
        /// <param name="name">Name of the key.</param>
        /// <param name="value">Value that should be written.</param>
        public static void SetInt(string section, string name, int value)
        {
            Instance.IniWriteValue(section, name, value.ToString());

        }

        /// <summary>
        /// Sets a string in the ini.
        /// </summary>
        /// <param name="section">Section of the key.</param>
        /// <param name="name">Name of the key.</param>
        /// <param name="value">Value that should be written.</param>
        public static void SetString(string section, string name, string value)
        {
            Instance.IniWriteValue(section, name, value);

        }

        /// <summary>
        /// Sets a bool in the ini.
        /// </summary>
        /// <param name="section">Section of the key.</param>
        /// <param name="name">Name of the key.</param>
        /// <param name="value">Value that should be written.</param>
        public static void SetBool(string section, string name, bool value)
        {
            Instance.IniWriteValue(section, name, value ? "1" : "0");

        }
    }
}
