using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Launcher
{
    static class Program
    {
        private static string[] TABOO_NAMES = {
            //"Start",
            //"Update",
            //"Awake",
            //"OnDestroy"
        };
        private static string[] ENTRY_TYPES = { "Display" };
       
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try {
                var execPath = Application.ExecutablePath;
                var fileName = Path.GetFileNameWithoutExtension(execPath);
                if (fileName.IndexOf("VR") == -1 && fileName.IndexOf("_") == -1)
                {
                    Fail("File not named correctly!");
                }
                
                bool vrMode = fileName.IndexOf("VR") > 0;
                string baseName = execPath.Substring(0, vrMode
                                                        ? execPath.LastIndexOf("VR")
                                                        : execPath.LastIndexOf("_"));
    
                string executable = baseName + ".exe";
                var file = new FileInfo(executable);
                if (file.Exists)
                {
                    var args = Environment.GetCommandLineArgs().ToList();
                    if (vrMode) args.Add("--vr");
                    EnsureIPA(executable);
                    StartGame(executable, args.ToArray());
                }
                else
                {
                    MessageBox.Show("Could not find: " + file.FullName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } catch(Exception globalException) {
                MessageBox.Show(globalException.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        private static void EnsureIPA(string executable)
        {
            var processStart = new ProcessStartInfo("IPA.exe", EncodeParameterArgument(executable) + " --nowait");
            processStart.UseShellExecute = false;
            processStart.CreateNoWindow = true;
            processStart.RedirectStandardError = true;

            var process = Process.Start(processStart);
            process.WaitForExit();
            if(process.ExitCode != 0)
            {
                Fail(process.StandardError.ReadToEnd());
            }
        }

        private static void StartGame(string executable, string[] args)
        {
            var arguments = string.Join(" ", args.ToArray());
            Process.Start(executable, arguments);
        }

        private static void Fail(string reason) {
            throw new Exception(reason);
        }

        /// <summary>
        /// Encodes an argument for passing into a program
        /// </summary>
        /// <param name="original">The value that should be received by the program</param>
        /// <returns>The value which needs to be passed to the program for the original value 
        /// to come through</returns>
        private static string EncodeParameterArgument(string original)
        {
            if (string.IsNullOrEmpty(original))
                return original;
            string value = Regex.Replace(original, @"(\\*)" + "\"", @"$1\$0");
            value = Regex.Replace(value, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");
            return value;
        }

    }
}
