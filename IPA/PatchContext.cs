using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IPA
{
    public class PatchContext
    {
        /// <summary>
        /// Gets the filename of the executable.
        /// </summary>
        public string Executable { get; private set; }

        /// <summary>
        /// Gets the path to the launcher executable (in the IPA folder)
        /// </summary>
        public string LauncherPathSrc { get; private set; }
        public string DataPathSrc { get; private set; }
        public string PluginsFolder { get; private set; }
        public string ProjectName { get; private set; }
        public string DataPathDst { get; private set; }
        public string ManagedPath { get; private set; }
        public string EngineFile { get; private set; }
        public string AssemblyFile { get; private set; }
        public string[] Args { get; private set; }
        public string ProjectRoot { get; private set; }
        public string IPARoot { get; private set; }
        public string ShortcutPath { get; private set; }
        public string IPA { get; private set; }
        public string BackupPath { get; private set; }

        private PatchContext() { }

        public static PatchContext Create(String[] args)
        {
            var context = new PatchContext();
            
            context.Args = args;
            context.Executable = args[0];
            context.ProjectRoot = new FileInfo(context.Executable).Directory.FullName;
            context.IPARoot = Path.Combine(context.ProjectRoot, "IPA");
            context.IPA = Assembly.GetExecutingAssembly().Location ?? Path.Combine(context.ProjectRoot, "IPA.exe");
            context.LauncherPathSrc = Path.Combine(context.IPARoot, "Launcher.exe");
            context.DataPathSrc = Path.Combine(context.IPARoot, "Data");
            context.PluginsFolder = Path.Combine(context.ProjectRoot, "Plugins");
            context.ProjectName = Path.GetFileNameWithoutExtension(context.Executable);
            context.DataPathDst = Path.Combine(context.ProjectRoot, context.ProjectName + "_Data");
            context.ManagedPath = Path.Combine(context.DataPathDst, "Managed");
            context.EngineFile =  DetermineEngineFile(context.ManagedPath, "UnityEngine.CoreModule.dll", "UnityEngine.dll");
            context.AssemblyFile = Path.Combine(context.ManagedPath, "Assembly-Csharp.dll");
            context.BackupPath = Path.Combine(Path.Combine(context.IPARoot, "Backups"), context.ProjectName);
            context.ShortcutPath = Path.Combine(context.ProjectRoot, $"{context.ProjectName} (Patch & Launch)") + ".lnk";

            Directory.CreateDirectory(context.BackupPath);

            return context;
        }

        private static string DetermineEngineFile(string basePath, params string[] candidates)
        {
            foreach(var candidate in candidates)
            {
                var fullPath = Path.Combine(basePath, candidate);
                if(File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            throw new FileNotFoundException("Could not find engine DLL!");
        }
    }
}
