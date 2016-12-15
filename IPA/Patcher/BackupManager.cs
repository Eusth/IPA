using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

    }
}
