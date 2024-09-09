// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestFramework.TestAdapter.Tests.Helpers
{
    /// <summary>
    /// Helper class to find the assemblies for the nanoFramework test projects that are used
    /// as input for unit tests.
    /// </summary>
    internal static class TestProjectHelper
    {
        /// <summary>
        /// Read the source files from one of the test projects
        /// </summary>
        /// <param name="projectName">Name of the project to find; pass <c>null</c> for the source of this project</param>
        /// <returns>The project source, and the directory it resides in</returns>
        internal static string FindProjectFilePath(string projectName)
        {
            string sharedRoot = Path.GetDirectoryName(typeof(TestProjectHelper).Assembly.Location);
            while (true)
            {
                string projectFilePath = Path.Combine(sharedRoot, projectName, $"{projectName}.nfproj");
                if (File.Exists(projectFilePath))
                {
                    return projectFilePath;
                }

                string newRoot = Path.GetDirectoryName(sharedRoot);
                if (string.IsNullOrEmpty(newRoot) || newRoot == sharedRoot)
                {
                    Assert.Inconclusive($"Cannot find the directory of the test project '{projectName}'");
                }
                sharedRoot = newRoot;
            }
        }

        internal static string FindNFUnitTestAssembly(string projectFilePath)
        {
            foreach (string version in new string[] { "Debug", "Release" })
            {
                string outputDirectory = Path.Combine(Path.GetDirectoryName(projectFilePath), "bin", version);
                if (Directory.Exists(outputDirectory))
                {
                    foreach (string assemblyFilePath in Directory.EnumerateFiles(outputDirectory, "*.dll"))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(assemblyFilePath);
                        if (fileName == "NFUnitTest" || fileName.StartsWith("TestFramework.Tooling.Tests."))
                        {
                            return assemblyFilePath;
                        }
                    }
                }
            }
            Assert.Inconclusive($"Cannot find the assembly of the test project '{Path.GetFileNameWithoutExtension(projectFilePath)}'");
            return null;
        }
    }
}
