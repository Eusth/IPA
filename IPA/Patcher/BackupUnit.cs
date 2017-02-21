using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace IPA.Patcher
{
    /// <summary>
    /// A unit for backup. WIP.
    /// </summary>
    public class BackupUnit
    {
        public string Name { get; private set; }

        private DirectoryInfo _BackupPath;
        private PatchContext _Context;
        private List<string> _Files = new List<string>();



        public BackupUnit(PatchContext context) : this(context, DateTime.Now.ToString("yyyy-MM-dd_h-mm-ss"))
        {
        }

        private BackupUnit(PatchContext context, string name)
        {
            Name = name;
            _Context = context;
            _BackupPath = new DirectoryInfo(Path.Combine(_Context.BackupPath, Name));
        }
        
        public static BackupUnit FromDirectory(DirectoryInfo directory, PatchContext context)
        {
            var unit = new BackupUnit(context, directory.Name);

            // Parse directory
            foreach(var file in directory.GetFiles("*", SearchOption.AllDirectories)) {
                var relativePath = file.FullName.Substring(directory.FullName.Length + 1);
                unit._Files.Add(relativePath);
            }

            return unit;
        }

        public void Add(string file)
        {
            Add(new FileInfo(file));
        }

        internal void Delete()
        {
            _BackupPath.Delete(true);
        }

        /// <summary>
        /// Adds a file to the list of changed files and backups it.
        /// </summary>
        /// <param name="path"></param>
        public void Add(FileInfo file)
        {
            if(!file.FullName.StartsWith(_Context.ProjectRoot))
            {
                Console.Error.WriteLine("Invalid file path for backup! {0}", file);
                return;
            }

            var relativePath = file.FullName.Substring(_Context.ProjectRoot.Length + 1);
            var backupPath = new FileInfo(Path.Combine(_BackupPath.FullName, relativePath));
            
            if(_Files.Contains(relativePath))
            {
                Console.WriteLine("Skipping backup of {0}", relativePath);
                return;
            }


            // Copy over
            backupPath.Directory.Create();
            if (file.Exists)
            {
                file.CopyTo(backupPath.FullName);
            } else
            {
                // Make empty file
                backupPath.Create().Close();
            }

            // Add to list
            _Files.Add(relativePath);
        }

        /// <summary>
        /// Reverts the changes made in this unit.
        /// </summary>
        public void Restore()
        {
            foreach(var relativePath in _Files)
            {
                Console.WriteLine("Restoring {0}", relativePath);
                // Original version
                var backupFile = new FileInfo(Path.Combine(_BackupPath.FullName, relativePath));
                var target = new FileInfo(Path.Combine(_Context.ProjectRoot, relativePath));

                if (backupFile.Exists)
                {
                    if (backupFile.Length > 0)
                    {
                        Console.WriteLine("  {0} => {1}", backupFile.FullName, target.FullName);
                        target.Directory.Create();
                        backupFile.CopyTo(target.FullName, true);
                    } else
                    {
                        Console.WriteLine("  x {0}", target.FullName);
                        if(target.Exists)
                        {
                            target.Delete();
                        }
                    }
                } else {
                    Console.Error.WriteLine("Backup not found!");
                }
            }
        }

    }
}
