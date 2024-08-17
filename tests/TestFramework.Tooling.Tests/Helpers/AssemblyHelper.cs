// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace TestFramework.Tooling.Tests.Helpers
{
    public static class AssemblyHelper
    {
        public static List<string> CopyAssemblies(string assemblyDirectoryPath, string projectName)
        {
            string projectFilePathUT = TestProjectHelper.FindProjectFilePath(projectName);
            string assemblyFilePathUT = TestProjectHelper.FindNFUnitTestAssembly(projectFilePathUT);
            var copyExtensions = new HashSet<string>()
            {
                ".dll", ".pdb", ".pe"
            };
            var expectedAssemblies = new List<string>();
            foreach (string file in Directory.EnumerateFiles(Path.GetDirectoryName(assemblyFilePathUT)))
            {
                if (copyExtensions.Contains(Path.GetExtension(file)))
                {
                    string filePath = Path.Combine(assemblyDirectoryPath, Path.GetFileName(file));

                    File.Copy(file, filePath);
                    if (Path.GetExtension(file) == ".pe")
                    {
                        expectedAssemblies.Add(filePath);
                    }
                }
            }
            return expectedAssemblies;
        }
    }
}
