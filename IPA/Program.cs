using IPA.Patcher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IPA
{
  
    public class Program
    {

        static void Main(string[] args)
        {
            if(args.Length < 1 || !args[0].EndsWith(".exe"))
            {
                Fail("Drag an (executable) file on the exe!");
            }

            try
            {
                var context = PatchContext.Create(args);
                bool isRevert = args.Contains("--revert") || Keyboard.IsKeyDown(Keys.LMenu);
                // Sanitizing
                Validate(context);

                if (isRevert)
                {
                    Revert(context);
                }
                else
                {
                    Install(context);
                    StartIfNeedBe(context);
                }
            } catch(Exception e)
            {
                Fail(e.Message);
            }
        }

        private static void Validate(PatchContext c)
        {
            if (!File.Exists(c.LauncherPathSrc)) Fail("Couldn't find DLLs! Make sure you extracted all contents of the release archive.");
            if (!Directory.Exists(c.DataPathDst) || !File.Exists(c.EngineFile) || !File.Exists(c.AssemblyFile))
            {
                Fail("Game does not seem to be a Unity project. Could not find the libraries to patch.");
            }
        }

        private static void Install(PatchContext context)
        {
            try
            {
                // Copying
                Console.WriteLine("Updating files... ");
                var nativePluginFolder = Path.Combine(context.DataPathDst, "Plugins");
                bool isFlat = Directory.Exists(nativePluginFolder) && Directory.GetFiles(nativePluginFolder).Any(f => f.EndsWith(".dll"));
                CopyAll(new DirectoryInfo(context.DataPathSrc), new DirectoryInfo(context.DataPathDst), (from, to) => NativePluginInterceptor(from, to, new DirectoryInfo(nativePluginFolder), isFlat) );

                Console.WriteLine("Successfully updated files!");

                if (!Directory.Exists(context.PluginsFolder))
                {
                    Console.WriteLine("Creating plugins folder... ");
                    Directory.CreateDirectory(context.PluginsFolder);
                }

                // Patching
                var patchedModule = PatchedModule.Load(context.EngineFile);
                if (!patchedModule.IsPatched)
                {
                    Console.Write("Patching UnityEngine.dll... ");
                    BackupManager.MakeBackup(context.EngineFile);
                    patchedModule.Patch();
                    Console.WriteLine("Done!");
                }

                // Virtualizing
                var virtualizedModule = VirtualizedModule.Load(context.AssemblyFile);
                if (!virtualizedModule.IsVirtualized)
                {
                    Console.Write("Virtualizing Assembly-Csharp.dll... ");
                    BackupManager.MakeBackup(context.AssemblyFile);
                    virtualizedModule.Virtualize();
                    Console.WriteLine("Done!");
                }

                // Creating shortcut
                if(!File.Exists(context.ShortcutPath))
                {
                    Console.Write("Creating shortcut to IPA ({0})... ",  context.IPA);
                    Shortcut.Create(
                        fileName: context.ShortcutPath, 
                        targetPath: context.IPA, 
                        arguments: Args(context.Executable, "--launch"),
                        workingDirectory: context.ProjectRoot,
                        description: "Launches the game and makes sure it's in a patched state",
                        hotkey: "",
                        iconPath: context.Executable
                    );
                    
                    Console.WriteLine("Created");
                }
            }
            catch (Exception e)
            {
                Fail("Oops! This should not have happened.\n\n" + e);
            }

            Console.WriteLine("Finished!");

        }

        private static void Revert(PatchContext context)
        {
            Console.Write("Restoring game assembly... ");
            if(BackupManager.Restore(context.AssemblyFile))
            {
                Console.WriteLine("Done!");
            } else
            {
                Console.WriteLine("Already vanilla!");
            }

            Console.Write("Restoring unity engine... ");
            if(BackupManager.Restore(context.EngineFile))
            {
                Console.WriteLine("Done!");
            }
            else
            {
                Console.WriteLine("Already vanilla!");
            }

            if (File.Exists(context.ShortcutPath))
            {
                Console.WriteLine("Deleting shortcut...");
                File.Delete(context.ShortcutPath);
            }

            Console.WriteLine("");
            Console.WriteLine("--- Done reverting ---");

            if (!Environment.CommandLine.Contains("--nowait"))
            {
                Console.WriteLine("\n\n[Press any key to quit]");
                Console.ReadKey();
            }
        }

        private static void StartIfNeedBe(PatchContext context)
        {
            var argList = context.Args.ToList();
            bool launch = argList.Remove("--launch");

            argList.RemoveAt(0);

            if(launch)
            {
                Process.Start(context.Executable, Args(argList.ToArray()));
            }
        }

        public static IEnumerable<FileInfo> NativePluginInterceptor(FileInfo from, FileInfo to, DirectoryInfo nativePluginFolder, bool isFlat)
        {
            if (to.FullName.StartsWith(nativePluginFolder.FullName))
            {
                var relevantBit = to.FullName.Substring(nativePluginFolder.FullName.Length + 1);
                // Goes into the plugin folder!
                bool isFileFlat = !relevantBit.StartsWith("x86");
                if (isFlat && !isFileFlat)
                {
                    // Flatten structure
                    if (relevantBit.StartsWith("x86_64"))
                    {
                        yield return new FileInfo(Path.Combine(nativePluginFolder.FullName, relevantBit.Substring("x86_64".Length + 1)));
                    }
                    else
                    {
                        // Throw away
                        yield break;
                    }
                }
                else if (!isFlat && isFileFlat)
                {
                    // Deepen structure
                    yield return new FileInfo(Path.Combine(Path.Combine(nativePluginFolder.FullName, "x86"), relevantBit));
                    yield return new FileInfo(Path.Combine(Path.Combine(nativePluginFolder.FullName, "x86_64"), relevantBit));
                }
                else
                {
                    yield return to;
                }
            }
            else
            {
                yield return to;
            }
        }
        private static IEnumerable<FileInfo> PassThroughInterceptor(FileInfo from, FileInfo to)
        {
            yield return to;
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target, Func<FileInfo, FileInfo, IEnumerable<FileInfo>> interceptor = null)
        {
            if(interceptor == null)
            {
                interceptor = PassThroughInterceptor;
            }

            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                foreach(var targetFile in interceptor(fi, new FileInfo(Path.Combine(target.FullName, fi.Name)))) {
                    if (!targetFile.Exists || targetFile.LastWriteTimeUtc < fi.LastWriteTimeUtc)
                    {
                        Console.WriteLine(@"Copying {0}", targetFile.FullName);
                        fi.CopyTo(targetFile.FullName, true);
                    }
                }
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir, interceptor);
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

        public static string Args(params string[] args)
        {
            return string.Join(" ", args.Select(EncodeParameterArgument).ToArray());
        }

        /// <summary>
        /// Encodes an argument for passing into a program
        /// </summary>
        /// <param name="original">The value that should be received by the program</param>
        /// <returns>The value which needs to be passed to the program for the original value 
        /// to come through</returns>
        public static string EncodeParameterArgument(string original)
        {
            if (string.IsNullOrEmpty(original))
                return original;
            string value = Regex.Replace(original, @"(\\*)" + "\"", @"$1\$0");
            value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");
            return value;
        }


        public abstract class Keyboard
        {
            [Flags]
            private enum KeyStates
            {
                None = 0,
                Down = 1,
                Toggled = 2
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
            private static extern short GetKeyState(int keyCode);

            private static KeyStates GetKeyState(Keys key)
            {
                KeyStates state = KeyStates.None;

                short retVal = GetKeyState((int)key);

                //If the high-order bit is 1, the key is down
                //otherwise, it is up.
                if ((retVal & 0x8000) == 0x8000)
                    state |= KeyStates.Down;

                //If the low-order bit is 1, the key is toggled.
                if ((retVal & 1) == 1)
                    state |= KeyStates.Toggled;

                return state;
            }

            public static bool IsKeyDown(Keys key)
            {
                return KeyStates.Down == (GetKeyState(key) & KeyStates.Down);
            }

            public static bool IsKeyToggled(Keys key)
            {
                return KeyStates.Toggled == (GetKeyState(key) & KeyStates.Toggled);
            }
        }
    }
}
