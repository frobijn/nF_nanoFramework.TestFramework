// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace TestFramework.TestAdapter.Tests.Helpers
{
    public static class AssemblyHelper
    {
        public static List<string> CopyAssemblies(string assemblyDirectoryPath, string projectName)
        {
            return CopyAssembliesAndProjectFile(null, assemblyDirectoryPath, projectName);
        }

        public static List<string> CopyAssembliesAndProjectFile(string projectDirectoryPath, string outputDirectory, string projectName)
        {
            string assemblyDirectoryPath = projectDirectoryPath is null ? outputDirectory : Path.Combine(projectDirectoryPath, outputDirectory);
            Directory.CreateDirectory(assemblyDirectoryPath);

            string sourceProjectFilePath = TestProjectHelper.FindProjectFilePath(projectName);
            if (!(projectDirectoryPath is null))
            {
                File.Copy(sourceProjectFilePath, Path.Combine(projectDirectoryPath, Path.GetFileName(sourceProjectFilePath)));
            }

            string sourceAssemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(sourceProjectFilePath);
            var copyExtensions = new HashSet<string>()
            {
                ".dll", ".pdb", ".pe"
            };
            var nanoFrameworkAssemblies = new List<string>();
            foreach (string file in Directory.EnumerateFiles(Path.GetDirectoryName(sourceAssemblyFilePath)))
            {
                if (copyExtensions.Contains(Path.GetExtension(file)))
                {
                    string filePath = Path.Combine(assemblyDirectoryPath, Path.GetFileName(file));

                    File.Copy(file, filePath);
                    if (Path.GetExtension(file) == ".pe")
                    {
                        nanoFrameworkAssemblies.Add(filePath);
                    }
                }
            }
            return nanoFrameworkAssemblies;
        }
    }
}
