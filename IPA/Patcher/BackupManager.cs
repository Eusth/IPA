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
        public static BackupUnit FindLatestBackup(PatchContext context)
        {
            return new DirectoryInfo(context.BackupPath)
                .GetDirectories()
                .OrderByDescending(p => p.Name)
                .Select(p => BackupUnit.FromDirectory(p, context))
                .FirstOrDefault();
        }

        public static bool HasBackup(PatchContext context)
        {
            return FindLatestBackup(context) != null;
        }
        
        public static bool Restore(PatchContext context)
        {
            var backup = FindLatestBackup(context);
            if(backup != null)
            {
                backup.Restore();
                backup.Delete();
                return true;
            }
            return false;
        }

    }
}
