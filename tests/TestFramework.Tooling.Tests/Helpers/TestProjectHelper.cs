// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;

namespace TestFramework.Tooling.Tests.Helpers
{
    /// <summary>
    /// Helper class to read the source code of one of the test projects
    /// </summary>
    internal static class TestProjectHelper
    {
        /// <summary>
        /// Read the source files from one of the test projects and return the declaration of a class that must exist in the source
        /// </summary>
        /// <param name="classType">The type of the class to get the declaration of</param>
        /// <param name="projectName">Name of the project to find; pass <c>null</c> for the source of this project</param>
        /// <param name="failOnError">The test should fail if the source cannot be read; otherwise it should be marked as inconclusive</param>
        /// <returns>The class declaration</returns>
        internal static ProjectSourceInventory.ClassDeclaration FindClassDeclaration(Type classType, string projectName = null, bool failOnError = false)
        {
            (ProjectSourceInventory source, string _) = FindAndCreateProjectSource(projectName, failOnError);
            ProjectSourceInventory.ClassDeclaration result = source.TryGet(classType.FullName);
            if (result is null)
            {
                Assert.Inconclusive($"Cannot find the class '{classType.FullName}' in the source of the test project '{projectName}'");
            }
            return result;
        }

        /// <summary>
        /// Read the source files from one of the test projects and return the declaration of a class that must exist in the source
        /// </summary>
        /// <param name="projectName">Name of the project to find; pass <c>null</c> for the source of this project</param>
        /// <param name="failOnError">The test should fail if the source cannot be read; otherwise it should be marked as inconclusive</param>
        /// <returns>The class declaration</returns>
        internal static ProjectSourceInventory.MethodDeclaration FindMethodDeclaration(Type classType, string methodName, string projectName = null, bool failOnError = false)
        {
            ProjectSourceInventory.ClassDeclaration classDeclaration = FindClassDeclaration(classType, projectName, failOnError);
            ProjectSourceInventory.MethodDeclaration result = (from m in classDeclaration.Methods
                                                               where m.Name == methodName
                                                               select m).FirstOrDefault();
            if (result is null)
            {
                Assert.Inconclusive($"Cannot find the method '{methodName}' of class '{classType.FullName}' in the source of the test project '{projectName}'");
            }
            return result;
        }

        /// <summary>
        /// Read the source files from one of the test projects
        /// </summary>
        /// <param name="projectName">Name of the project to find; pass <c>null</c> for the source of this project</param>
        /// <returns>The project source, and the directory it resides in</returns>
        internal static string FindProjectFilePath(string projectName = null)
        {
            if (projectName is null)
            {
                projectName = "TestFramework.Tooling.Tests";
            }

            string sharedRoot = Path.GetDirectoryName(typeof(TestProjectHelper).Assembly.Location);
            while (true)
            {
                string projectFilePath = Path.Combine(sharedRoot, projectName, $"{projectName}.nfproj");
                if (!File.Exists(projectFilePath))
                {
                    projectFilePath = Path.Combine(sharedRoot, projectName, $"{projectName}.csproj");
                }
                if (File.Exists(projectFilePath))
                {
                    return projectFilePath;
                }

                string newRoot = Path.GetDirectoryName(sharedRoot);
                if (string.IsNullOrEmpty(newRoot) || newRoot == sharedRoot)
                {
                    Assert.Inconclusive($"Cannot find the source of the test project '{projectName}'");
                }
                sharedRoot = newRoot;
            }
        }

        internal static string FindNFUnitTestAssembly(string projectFilePath)
        {
            foreach (string version in new string[] { "Debug", "Release" })
            {
                string assemblyFilePath = Path.Combine(Path.GetDirectoryName(projectFilePath), "bin", version, "NFUnitTest.dll");
                if (File.Exists(assemblyFilePath))
                {
                    return assemblyFilePath;
                }
            }
            Assert.Inconclusive($"Cannot find the assembly of the test project '{Path.GetFileNameWithoutExtension(projectFilePath)}'");
            return null;
        }

        /// <summary>
        /// Read the source files from one of the test projects
        /// </summary>
        /// <param name="projectName">Name of the project to find; pass <c>null</c> for the source of this project</param>
        /// <param name="failOnError">The test should fail if the source cannot be read; otherwise it should be marked as inconclusive</param>
        /// <returns>The project source, and the directory it resides in</returns>
        internal static (ProjectSourceInventory actual, string projectFilePrefix) FindAndCreateProjectSource(string projectName = null, bool failOnError = false)
        {
            string projectFilePath = FindProjectFilePath(projectName);
            var logger = new LogMessengerMock();
            var actual = new ProjectSourceInventory(projectFilePath, logger);

            if (failOnError)
            {
                logger.AssertEqual("");
                Assert.IsNotNull(actual);
            }
            else if (logger.Messages.Count > 0)
            {
                Assert.Inconclusive($"Cannot read the source of the test project '{Path.GetFileNameWithoutExtension(projectFilePath)}':\n{string.Join("\n", from m in logger.Messages select $"{m.level}: {m.message}")}");
            }

            return (actual, Path.GetDirectoryName(projectFilePath) + Path.DirectorySeparatorChar);
        }
    }
}
