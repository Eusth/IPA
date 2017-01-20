using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IPA.Patcher
{
    public class BackupManager
    {
        public static void MakeBackup(string file)
        {
            File.Copy(file, GetBackupName(file));
        }
        
        private static string GetBackupName(string file)
        {
            string backup = file + ".Original";

            if (File.Exists(backup))
            {
                int i = 1;
                string backupBase = backup;
                while (File.Exists(backup))
                {
                    backup = backupBase + i++;
                }
            }
            return backup;
        }

        public static string FindLatestBackup(string file)
        {
            var directory = Path.GetDirectoryName(file);
            var filename = Path.GetFileName(file);

            var regex = new Regex(String.Format(@"^{0}\.Original\d*$", Regex.Escape(filename)));
            var extractNumRegex = new Regex(@"\d+$");

            string latestFile = null;
            int latestNum = -1;
            foreach(var f in Directory.GetFiles(directory))
            {
                if(regex.IsMatch(Path.GetFileName(f)))
                {
                    var match = extractNumRegex.Match(f);
                    int number = match.Success ? int.Parse(match.Value) : 0;
                    if(number > latestNum)
                    {
                        latestNum = number;
                        latestFile = f;
                    }
                }
            }

            return latestFile;
        }
        
        public static bool Restore(string file)
        {
            var backup = FindLatestBackup(file);
            if(backup != null)
            {
                File.Delete(file);
                File.Move(backup, file);
                return true;
            }
            return false;
        }

    }
}
