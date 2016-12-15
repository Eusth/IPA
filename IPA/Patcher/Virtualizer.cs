using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IPA.Patcher
{
    class VirtualizedModule
    {
        private const string ENTRY_TYPE = "Display";

        private FileInfo _File;
        private ModuleDefinition _Module;

        public static VirtualizedModule Load(string engineFile)
        {
            return new VirtualizedModule(engineFile);
        }

        private VirtualizedModule(string assemblyFile)
        {
            _File = new FileInfo(assemblyFile);

            LoadModules();
        }

        private void LoadModules()
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(_File.DirectoryName);

            var parameters = new ReaderParameters
            {
                AssemblyResolver = resolver,
            };

            _Module = ModuleDefinition.ReadModule(_File.FullName, parameters);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="module"></param>
        public void Virtualize()
        {
            foreach (var type in _Module.Types)
            {
                VirtualizeType(type);
            }
        }

        private void VirtualizeType(TypeDefinition type)
        {
            if (type.IsSealed) return;
            if (type.IsInterface) return;
            if (type.IsAbstract) return;

            // These two don't seem to work.
            if (type.Name == "SceneControl" || type.Name == "ConfigUI") return;

            // Take care of sub types
            foreach (var subType in type.NestedTypes)
            {
                VirtualizeType(subType);
            }

            foreach (var method in type.Methods)
            {
                if (method.IsManaged
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
            }
        }

        public bool IsVirtualized
        {
            get
            {
                return _Module.GetTypes().SelectMany(t => t.Methods.Where(m => m.Name == "Awake")).All(m => m.IsVirtual);
            }
        }
    }
}
