// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    [TestCategory("Unit test launcher")]
    public sealed class UnitTestLauncherGeneratorTest : TestUsingTestFrameworkToolingTestsDiscovery_v3
    {
        #region Test context and helper
        public TestContext TestContext { get; set; }

        private List<string> CopyAssemblies(string assemblyDirectoryPath, string projectName)
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
        #endregion

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void UnitTestLauncher_GeneratedCode(bool communicateByNames)
        {
            var logger = new LogMessengerMock();
            var actual = new UnitTestLauncherGenerator(TestSelection, communicateByNames, logger);

            logger.AssertEqual("");

            Assert.IsTrue(actual.SourceFiles.ContainsKey("UnitTestLauncher.RunUnitTests.cs"));
            Assert.AreEqual(
@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//======================================================================
//
// This file is generated. Changes to the code will be lost.
//
//======================================================================

namespace nanoFramework.TestFramework.Tools
{
    public partial class UnitTestLauncher
    {
        private partial void RunUnitTests()
        {
            ForClass(
                typeof(global::TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes), true,
                nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.Setup),
                nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.Cleanup),
                (frm, fdr) =>
                {
                    frm(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod));
                    fdr(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1), 0, 1);
                }
            );
            ForClass(
                typeof(global::TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods), true,
                null,
                null,
                (frm, fdr) =>
                {
                    frm(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test));
                    frm(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2));
                }
            );
        }
    }
}
".Replace("\r\n", "\n") + '\n',
                actual.SourceFiles["UnitTestLauncher.RunUnitTests.cs"].Replace("\r\n", "\n") + '\n'
            );
        }

        [TestMethod]
        public void UnitTestLauncher_GeneratedApplication_MismatchNFUnitTestAndTestCases()
        {
            #region Prepare assembly directory and unit test selection
            string assemblyDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);
            CopyAssemblies(assemblyDirectoryPath, "TestFramework.Tooling.Tests.Execution.v3"); // Wrong project!
            var logger = new LogMessengerMock();
            var actual = new UnitTestLauncherGenerator(TestSelection, false, logger);
            logger.AssertEqual("");
            #endregion

            logger = new LogMessengerMock();
            actual.GenerateAsApplication(assemblyDirectoryPath, logger);

            logger.AssertEqual(
@"Error: CS0234 The type or namespace name 'Tests' does not exist in the namespace 'TestFramework.Tooling' (are you missing an assembly reference?) @ SourceFile([591..596))
Error: CS0234 The type or namespace name 'Tests' does not exist in the namespace 'TestFramework.Tooling' (are you missing an assembly reference?) @ SourceFile([665..700))
Error: CS0234 The type or namespace name 'Tests' does not exist in the namespace 'TestFramework.Tooling' (are you missing an assembly reference?) @ SourceFile([769..804))
Error: CS0234 The type or namespace name 'Tests' does not exist in the namespace 'TestFramework.Tooling' (are you missing an assembly reference?) @ SourceFile([933..968))
Error: CS0234 The type or namespace name 'Tests' does not exist in the namespace 'TestFramework.Tooling' (are you missing an assembly reference?) @ SourceFile([1051..1086))
Error: CS0234 The type or namespace name 'Tests' does not exist in the namespace 'TestFramework.Tooling' (are you missing an assembly reference?) @ SourceFile([1256..1261))
Error: CS0234 The type or namespace name 'Tests' does not exist in the namespace 'TestFramework.Tooling' (are you missing an assembly reference?) @ SourceFile([1425..1460))
Error: CS0234 The type or namespace name 'Tests' does not exist in the namespace 'TestFramework.Tooling' (are you missing an assembly reference?) @ SourceFile([1528..1563))");
        }

        [TestMethod]
        public void UnitTestLauncher_GeneratedApplication()
        {
            (UnitTestLauncherGenerator _, UnitTestLauncherGenerator.Application actual) = UnitTestLauncher_GenerateApplication(true);

            Assert.IsNotNull(actual?.ReportPrefix);
            Assert.AreNotEqual(0, actual.Assemblies.Count);
            Assert.AreEqual("nanoFramework.UnitTestLauncher.pe", Path.GetFileName(actual.Assemblies[0]));
            Assert.IsTrue((from a in actual.Assemblies
                           where Path.GetFileName(a) == "nanoFramework.TestFramework.pe"
                           select Path.GetFileName(a)).Any());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void UnitTestLauncher_GeneratedApplication_RunWithNanoCLRHelper(bool communicateByNames)
        {
            (UnitTestLauncherGenerator generator, UnitTestLauncherGenerator.Application actual) = UnitTestLauncher_GenerateApplication(communicateByNames);

            #region Assert the generated code runs on the Virtual Device
            var logger = new LogMessengerMock();
            var nanoClr = new NanoCLRHelper(null, null, false, logger);
            logger.AssertEqual("", LoggingLevel.Error);

            logger = new LogMessengerMock();
            var outputCollector = new StringBuilder();
            bool result = nanoClr.RunAssembliesAsync(actual.Assemblies, null, null, LoggingLevel.Detailed, (o) => outputCollector.AppendLine(o), logger)
                                .GetAwaiter().GetResult();
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsTrue(result);
            #endregion

            #region Assert the output is correct
            var testResults = new List<nanoFramework.TestFramework.Tooling.TestResult>();
            var parser = new UnitTestsOutputParser(TestSelection, null, actual.ReportPrefix, (t) => testResults.AddRange(t));
            parser.AddOutput(outputCollector.ToString());
            parser.Flush();

            testResults.AssertResults(TestSelection,
@"----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
DisplayName : 'TestMethod - Passed'
Duration    : 0 ticks
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0
DisplayName : 'TestMethod1(1,1) - Passed'
Duration    : 0 ticks
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1
DisplayName : 'TestMethod1(2,2) - Passed'
Duration    : 0 ticks
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
DisplayName : 'Test - Passed'
Duration    : 0 ticks
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
DisplayName : 'Test2 - Passed'
Duration    : 0 ticks
Outcome     : Passed
ErrorMessage: ''
----------------------------------------", false);
            #endregion
        }

        private (UnitTestLauncherGenerator generator, UnitTestLauncherGenerator.Application application) UnitTestLauncher_GenerateApplication(bool communicateByNames)
        {
            #region Prepare assembly directory and unit test selection
            string assemblyDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);
            List<string> expectedAssemblies = CopyAssemblies(assemblyDirectoryPath, "TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath = Path.Combine(assemblyDirectoryPath, "nanoFramework.UnitTestLauncher.pe");
            expectedAssemblies.Insert(0, assemblyFilePath);
            File.WriteAllText(assemblyFilePath, "This is an old version of the assembly");
            File.WriteAllText(Path.ChangeExtension(assemblyFilePath, ".dll"), "This is an old version of the assembly");

            var logger = new LogMessengerMock();
            var generator = new UnitTestLauncherGenerator(TestSelection, communicateByNames, logger);
            logger.AssertEqual("");
            #endregion

            logger = new LogMessengerMock();
            UnitTestLauncherGenerator.Application application = generator.GenerateAsApplication(assemblyDirectoryPath, logger);

            logger.AssertEqual("");
            Assert.IsTrue(File.Exists(assemblyFilePath));
            return (generator, application);
        }

    }
}
