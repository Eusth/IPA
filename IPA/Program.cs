using IPA.Patcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IPA
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 1 || !args[0].EndsWith(".exe"))
            {
                Fail("Drag an (executable) file on the exe!");
            }

            string launcherSrc = Path.Combine("IPA", "Launcher.exe");
            string managedFolder = Path.Combine("IPA", "Managed");
            string pluginsFolder = "Plugins";
            string projectName = Path.GetFileNameWithoutExtension(args[0]);
            string dataPath = Path.Combine(Path.Combine(Environment.CurrentDirectory, projectName + "_Data"), "Managed");
            string engineFile = Path.Combine(dataPath, "UnityEngine.dll");
            string assemblyFile = Path.Combine(dataPath, "Assembly-Csharp.dll");


            // Sanitizing
            if (!File.Exists(launcherSrc)) Fail("Couldn't find DLLs! Make sure you extracted all contents of the release archive.");
            if(!Directory.Exists(dataPath) || !File.Exists(engineFile) || !File.Exists(assemblyFile))
            {
                Fail("Game does not seem to be a Unity project. Could not find the libraries to patch.");
            } 

            try
            {
                // Copying
                CopyAll(new DirectoryInfo(managedFolder), new DirectoryInfo(dataPath));
                Console.WriteLine("Successfully copied files!");

                if(!Directory.Exists(pluginsFolder))
                {
                    Directory.CreateDirectory(pluginsFolder);
                }

                // Patching
                var patchedModule = PatchedModule.Load(engineFile);
                if(!patchedModule.IsPatched)
                {
                    BackupManager.MakeBackup(engineFile);
                    patchedModule.Patch();
                }

                // Virtualizing
                var virtualizedModule = VirtualizedModule.Load(assemblyFile);
                if(!virtualizedModule.IsVirtualized)
                {
                    BackupManager.MakeBackup(assemblyFile);
                    virtualizedModule.Virtualize();
                }
            } catch(Exception e)
            {
                Fail("Oops! This should not have happened.\n\n" + e);
            }
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                string targetFile = Path.Combine(target.FullName, fi.Name);
                if (!File.Exists(targetFile) || File.GetLastWriteTimeUtc(targetFile) < fi.LastWriteTimeUtc)
                {
                    Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                    fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                }
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }


        static void Fail(string message)
        {
            Console.Error.Write("ERROR: " + message);
            if (!Environment.CommandLine.Contains("--nowait"))
            {
                Console.WriteLine("\n\n[Press any key to quit]");
                Console.ReadKey();
            }
            Environment.Exit(1);
        }
    }
}
