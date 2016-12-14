using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Threading;

namespace QuietLauncher
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
                if (fileName.IndexOf("VR") == -1 && fileName.IndexOf("_") == -1) return;
                
                bool vrMode = fileName.IndexOf("VR") > 0;
                bool directMode = Application.ExecutablePath.EndsWith("_DirectToRift.exe");
                string baseName = execPath.Substring(0, vrMode
                                                        ? execPath.LastIndexOf("VR")
                                                        : execPath.LastIndexOf("_"));
    
                string executable = baseName + ".exe";
                var file = new FileInfo(executable);
                if (file.Exists)
                {
                    var args = Environment.GetCommandLineArgs().ToList();
                    bool created = false;
    
                    var dataFolder = Path.Combine(file.DirectoryName, Path.GetFileNameWithoutExtension(file.Name) + "_Data");
                    var assemblyPath = Path.Combine(Path.Combine(dataFolder, "Managed"), "Assembly-CSharp.dll");
                    var enginePath = Path.Combine(Path.Combine(dataFolder, "Managed"), "UnityEngine.dll");
                    var directToRiftPath = baseName + "_DirectToRift.exe";
    
                    try
                    {
                        if (directMode)
                        {
                            //args[Array.IndexOf(args, "--direct")] = "-force-d3d11";
    
    
                            if (!File.Exists(directToRiftPath))
                            {
                                File.WriteAllBytes(directToRiftPath, Resources.DirectToRift);
                                created = true;
                            }
    
                            file = new FileInfo(directToRiftPath);
                        }
    
    
                        if (vrMode) args.Add("--vr");
                        var arguments = string.Join(" ", args.ToArray());
    
                        try
                        {
                            Patch(assemblyPath, enginePath);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
    
                        Process.Start(file.FullName, arguments);
    
                    }
                    finally
                    {
                        if (created && directMode)
                        {
                            var thread = new Thread(new ThreadStart(delegate
                            {
                                int attempts = 0;
                                while (File.Exists(directToRiftPath) && attempts++ < 20)
                                {
                                    Thread.Sleep(1000);
                                    try
                                    {
                                        File.Delete(directToRiftPath);
                                    }
                                    catch (Exception ex)
                                    {
    
                                    }
                                }
                            }));
                            thread.Start();
                            thread.Join();
                            // Clean up
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Could not find: " + file.FullName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            } catch(Exception globalException) {
                MessageBox.Show(globalException.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        static string PrepareBackup(FileInfo file)
        {
            string backup = file.FullName + ".Original";

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

        static void Patch(string assemblyFile, string engineFile)
        {

            var input = new FileInfo(assemblyFile);
            var engineInput = new FileInfo(engineFile);
            string assemblyBackup = PrepareBackup(input);
            string engineBackup = PrepareBackup(engineInput);

            if (!input.Exists) Fail("File does not exist.");
            

            var directory = input.DirectoryName;
            var injectorPath = Path.Combine(directory, "IllusionInjector.dll");

            if (!File.Exists(injectorPath)) Fail("You're missing IllusionInjector.dll. Please make sure to extract all files correctly.");

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(directory);

            var parameters = new ReaderParameters
            {
                //   SymbolReaderProvider = GetSymbolReaderProvider(),
                AssemblyResolver = resolver,
            };

            var assemblyModule = ModuleDefinition.ReadModule(input.FullName, parameters);
            var engineModule = ModuleDefinition.ReadModule(engineInput.FullName, parameters);

            if (!IsPatched(engineModule)) //|| !isVirtualized)
            {
                // Make backup
                input.CopyTo(engineBackup);
                // First, let's add the reference
                var nameReference = new AssemblyNameReference("IllusionInjector", new Version(1, 0, 0, 0));
                engineModule.AssemblyReferences.Add(nameReference);
                var targetType = FindEntryType(engineModule);

                if (targetType == null) Fail("Couldn't find entry class. Aborting.");

                var awakeMethod = targetType.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);
                if (awakeMethod == null)
                {
                    Fail("Couldn't find awake method. Aborting.");
                }

                var injector = ModuleDefinition.ReadModule(injectorPath);
                var methodReference = engineModule.Import(injector.GetType("IllusionInjector.Injector").Methods.First(m => m.Name == "Inject"));
                //var methodReference = module.GetMemberReferences().FirstOrDefault(r => r.FullName == "IllusionInjector.Injector");

                awakeMethod.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, methodReference));
                engineModule.Write(engineInput.FullName);
            }
            if(!IsVirtualized(assemblyModule))
            {
                input.CopyTo(assemblyBackup);
                Virtualize(assemblyModule);
                assemblyModule.Write(input.FullName);
            }
        }


        /// <summary>
        /// The forbidden deed of the gods -- make ALL methods virtual and public
        /// </summary>
        /// <param name="module"></param>
        private static void Virtualize(ModuleDefinition module)
        {
            foreach (var type in module.Types)
            {
                VirtualizeType(type);
            }
        }

        private static void VirtualizeType(TypeDefinition type)
        {
            if (type.IsSealed) return;
            if (type.IsInterface) return;
            if (type.IsAbstract) return;

            // These two don't seem to work.
            if (type.Name == "SceneControl" || type.Name == "ConfigUI") return;

            //if (type.FullName.Contains("RootMotion")) return;
            //if (type.Methods.Any(m => m.Body != null && m.Body.Variables.Any(v => v.VariableType.FullName.Contains("<")))) return;
            //if (!type.FullName.Contains("H_VoiceControl")) return;
            //if (!type.FullName.Contains("Human")) return;
            //if (type.Namespace.Length > 1) return;
            

            // Take care of sub types
            foreach (var subType in type.NestedTypes)
            {
                VirtualizeType(subType);
            }

            foreach (var method in type.Methods)
            {
                Console.WriteLine(method.Name);
                if (method.IsManaged
                    && !TABOO_NAMES.Contains(method.Name)
                    && method.IsIL
                    && !method.IsStatic 
                    && !method.IsVirtual
                    && !method.IsAbstract 
                    && !method.IsAddOn 
                    && !method.IsConstructor 
                    && !method.IsSpecialName 
                    && !method.IsGenericInstance 
                    && !method.HasOverrides)
                {
                    method.IsVirtual = true;
                    method.IsPublic = true;
                    method.IsPrivate = false;
                    method.IsNewSlot = true;
                    method.IsHideBySig = true;
                }
            }

            foreach (var field in type.Fields)
            {
                if (field.IsPrivate) field.IsFamily = true;
                //field.IsPublic = true;
            }

            //foreach (var property in type.Properties)
            //{
            //    property.GetMethod.IsVirtual = true;
            //    property.GetMethod.IsPublic = true;
            //    property.SetMethod.IsVirtual = true;
            //    property.SetMethod.IsPublic = true;
            //}

        }

        private static bool IsPatched(ModuleDefinition module)
        {
            foreach (var @ref in module.AssemblyReferences)
            {
                if (@ref.Name == "IllusionInjector") return true;
            }
            return false;
        }

        private static bool IsVirtualized(ModuleDefinition module)
        {
            return module.GetTypes().SelectMany(t => t.Methods.Where(m => m.Name == "Awake")).All(m => m.IsVirtual);
        }

        private static void Fail(string reason) {
            throw new Exception(reason);
        }

        private static TypeDefinition FindEntryType(ModuleDefinition module)
        {
            return module.GetTypes().FirstOrDefault(IsEntryType);
        }

        private static bool IsEntryType(TypeDefinition type)
        {
            return ENTRY_TYPES.Contains(type.Name);
        }

    }
}
