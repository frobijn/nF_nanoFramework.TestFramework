// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Some information about a nanoFramework assembly.
    /// </summary>
    public sealed class AssemblyMetadata
    {
        #region Construction
        /// <summary>
        /// Get the information about the nanoFramework assembly.
        /// </summary>
        /// <param name="assemblyFilePath">The path to the assembly. Can be the path to a *.pe, *.dll or *.exe file.
        /// if it is the path to a *.pe file, the *.dll/*.exe file should reside in the same directory.</param>
        public AssemblyMetadata(string assemblyFilePath)
        {
            AssemblyFilePath = assemblyFilePath;
            NanoFrameworkAssemblyFilePath = assemblyFilePath;
            if (Path.GetExtension(assemblyFilePath).ToLower() == ".pe")
            {
                string tryPath = Path.ChangeExtension(assemblyFilePath, ".dll");
                if (File.Exists(tryPath))
                {
                    AssemblyFilePath = tryPath;
                }
                else
                {
                    tryPath = Path.ChangeExtension(assemblyFilePath, ".exe");
                    if (File.Exists(tryPath))
                    {
                        AssemblyFilePath = tryPath;
                    }
                }
            }
            else
            {
                NanoFrameworkAssemblyFilePath = Path.ChangeExtension(assemblyFilePath, ".pe");
            }

            if (File.Exists(AssemblyFilePath))
            {
                var decompiler = new CSharpDecompiler(AssemblyFilePath, new DecompilerSettings
                {
                    LoadInMemory = false,
                    ThrowOnAssemblyResolveErrors = false
                });
                string assemblyProperties = decompiler.DecompileModuleAndAssemblyAttributesToString();

                // AssemblyVersion
                string pattern = @"(?<=AssemblyVersion\("")(.*)(?=\""\)])";
                MatchCollection match = Regex.Matches(assemblyProperties, pattern, RegexOptions.IgnoreCase);
                Version = match[0].Value;

                // AssemblyNativeVersion
                pattern = @"(?<=AssemblyNativeVersion\("")(.*)(?=\""\)])";
                match = Regex.Matches(assemblyProperties, pattern, RegexOptions.IgnoreCase);

                // only class libs have this attribute, therefore sanity check is required
                if (match.Count == 1)
                {
                    NativeVersion = match[0].Value;
                }
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Path to the EXE or DLL file.
        /// </summary>
        public string AssemblyFilePath
        {
            get;
        }

        /// <summary>
        /// Path to the PE file.
        /// </summary>
        public string NanoFrameworkAssemblyFilePath
        {
            get;
        }

        /// <summary>
        /// Assembly version of the EXE or DLL. Is <c>null</c> if the assembly does not exist.
        /// </summary>
        public string Version
        {
            get;
        }

        /// <summary>
        /// Required version of the native implementation of the class library.
        /// Only used in class libraries. Can be <c>null</c> on the core library and user EXE and DLLs.
        /// </summary>
        public string NativeVersion
        {
            get;
        }
        #endregion
    }
}
