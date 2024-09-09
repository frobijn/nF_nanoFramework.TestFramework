// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests.Tools
{
    [TestClass]
    [TestCategory("Unit test debugger")]
    [TestCategory("MSBuild")]
    public sealed class DebugProjectBuildToolTest
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void DebugProjectBuildTool_GenerateTestCasesSpecificationAndSchema()
        {
            #region Get test cases
            string projectDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);
            string outputDirectory = Path.Combine("bin", "Debug");
            AssemblyHelper.CopyAssemblies(Path.Combine(projectDirectoryPath, outputDirectory), "TestFramework.Tooling.Tests.Execution.v3");

            var actual = new DebugProjectBuildTool(projectDirectoryPath);

            var logger = new LogMessengerMock();
            TestCaseCollection testCases = actual.LoadTestCases(outputDirectory, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsNotNull(testCases);
            Assert.IsTrue(testCases.TestCases.Any());
            #endregion

            actual.GenerateTestCasesSpecificationAndSchema(testCases, "obj/nF");

            AssertExistsAndNotEmpty(Path.Combine(projectDirectoryPath, DebugTestCasesSpecification.SpecificationFileName));
            AssertExistsAndNotEmpty(Path.Combine(projectDirectoryPath, "obj", "nF", Path.ChangeExtension(DebugTestCasesSpecification.SpecificationFileName, ".schema.json")));
        }

        [TestMethod]
        public void DebugProjectBuildTool_GenerateUnitTestLauncherSourceCode()
        {
            #region Get test cases
            string projectDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);
            string outputDirectory = Path.Combine("bin", "Debug");
            AssemblyHelper.CopyAssemblies(Path.Combine(projectDirectoryPath, outputDirectory), "TestFramework.Tooling.Tests.Execution.v3");
            string specificationFilePath = Path.Combine(projectDirectoryPath, DebugTestCasesSpecification.SpecificationFileName);

            var actual = new DebugProjectBuildTool(projectDirectoryPath);

            var logger = new LogMessengerMock();
            TestCaseCollection testCases = actual.LoadTestCases(outputDirectory, logger);
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsNotNull(testCases);
            Assert.IsTrue(testCases.TestCases.Any());
            #endregion

            #region Just after clone: no specification available
            logger = new LogMessengerMock();
            actual.GenerateUnitTestLauncherSourceCode(testCases, "obj/nF", logger);

            logger.AssertEqual(
$@"Error: {specificationFilePath}(0,0): error: No test cases selected; nothing to debug.",
                LoggingLevel.Error);
            Assert.IsFalse(Directory.Exists(Path.Combine(projectDirectoryPath, "obj", "nF")));
            #endregion

            #region Test case specified
            File.WriteAllText(specificationFilePath,
@"{
    ""TestCases"": {
        ""TestFramework.Tooling.Execution.Tests"": {
            ""TestWithMethods"": ""*""
        }
    }
}");

            logger = new LogMessengerMock();
            actual.GenerateUnitTestLauncherSourceCode(testCases, "obj/nF2", logger);

            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsTrue(Directory.Exists(Path.Combine(projectDirectoryPath, "obj", "nF2")));
            Assert.AreEqual(
$@"{projectDirectoryPath}\obj\nF2\UnitTestLauncher.cs
{projectDirectoryPath}\obj\nF2\UnitTestLauncher.RunUnitTests.cs
{projectDirectoryPath}\obj\nF2\UnitTestLauncher.TestClassInitialisation.cs
".Trim().Replace("\r\n", "\n") + '\n',
                string.Join("\n", from f in Directory.EnumerateFiles(Path.Combine(projectDirectoryPath, "obj", "nF2"))
                                  orderby f
                                  select f) + '\n'
            );
            #endregion
        }

        #region Helpers
        private static void AssertExistsAndNotEmpty(string filePath)
        {
            Assert.IsTrue(File.Exists(filePath));
            Assert.IsTrue(!string.IsNullOrWhiteSpace(File.ReadAllText(filePath)));
        }
        #endregion
    }
}
