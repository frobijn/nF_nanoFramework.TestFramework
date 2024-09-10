// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    public sealed class AssemblyMetadataTest
    {
        [TestMethod]
        [TestCategory("Test execution")]
        public void AssemblyMetadata_Test()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);

            foreach (string filePath in Directory.EnumerateFiles(Path.GetDirectoryName(assemblyFilePath), "*.pe"))
            {
                var actual = new AssemblyMetadata(filePath);

                Assert.AreEqual(filePath, actual.NanoFrameworkAssemblyFilePath);
                Assert.IsNotNull(actual.Version);
                if (Path.GetFileNameWithoutExtension(filePath) == "mscorlib")
                {
                    Assert.IsNotNull(actual.NativeVersion);
                }

                Assert.IsNotNull(actual.AssemblyFilePath);
                Assert.AreNotEqual(actual.AssemblyFilePath, actual.NanoFrameworkAssemblyFilePath);

                var actual2 = new AssemblyMetadata(actual.AssemblyFilePath);
                Assert.AreEqual(filePath, actual2.NanoFrameworkAssemblyFilePath);
                Assert.AreEqual(actual.AssemblyFilePath, actual2.AssemblyFilePath);
                Assert.AreEqual(actual.Version, actual2.Version);
                Assert.AreEqual(actual.NativeVersion, actual2.NativeVersion);
            }
        }
    }
}
