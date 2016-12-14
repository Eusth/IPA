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

            // Sanitizing
            if (!File.Exists(launcherSrc)) Fail("Couldn't find launcher! Make sure you extracted all contents of the release archive.");
            if (!File.Exists(launcherSrc)) Fail("Couldn't find DLLs! Make sure you extracted all contents of the release archive.");

            // Copying
            try
            {
                string projectName = Path.GetFileNameWithoutExtension(args[0]);
                string launcherPath = Path.Combine(Environment.CurrentDirectory, projectName + "_Patched.exe");
                string dataPath = Path.Combine(Path.Combine(Environment.CurrentDirectory, projectName + "_Data"), "Managed");

                File.Copy(launcherSrc, launcherPath, true);
                CopyAll(new DirectoryInfo(managedFolder), new DirectoryInfo(dataPath));

                Console.WriteLine("Successfully copied files!");
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
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
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
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("{0,80}", "");
            Console.Write("{0,-80}", "   "+message);
            Console.Write("{0,80}", "");
            
            Console.ReadLine();
            Environment.Exit(1);
        }
    }
}
