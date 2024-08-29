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
    public sealed class UnitTestLauncherGeneratorTest_Discovery_v3 : TestUsingTestFrameworkToolingTestsDiscovery_v3
    {
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void UnitTestLauncher_GeneratedCode(bool communicateByNames)
        {
            var logger = new LogMessengerMock();
            var actual = new UnitTestLauncherGenerator(TestSelection, CreateDeploymentConfiguration(), communicateByNames, logger);

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
                typeof(global::TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes), 1,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.Setup), null);
                },
                (rcm) =>
                {
                    rcm(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.Cleanup));
                },
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod), null);
                    rdr(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1), null, 0, 1);
                }
            );
            ForClass(
                typeof(global::TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions), 1,
                (rsm) =>
                {
                    rsm(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.Setup), new object[] { CFG_1, CFG_2, -1 });
                },
                null,
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile), null);
                }
            );
            ForClass(
                typeof(global::TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods), 1,
                null,
                null,
                (rtm, rdr) =>
                {
                    rtm(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test), null);
                    rtm(nameof(global::TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2), new object[] { s_cfg_3 });
                }
            );
        }
#region Deployment configuration data
        /// <summary>Value for deployment configuration key 'xyzzy'</summary>
        private const string CFG_1 = ""Value\r\nfor\r\nxyzzy"";
        /// <summary>Value for deployment configuration key 'Device ID'</summary>
        private const long CFG_2 = 42;
        /// <summary>Value for deployment configuration key 'Make and model'</summary>
        private static readonly byte[] s_cfg_3 = new byte[] {
            3,1,4,1,5,
        };
#endregion
    }
}
".Replace("\r\n", "\n") + '\n',
                actual.SourceFiles["UnitTestLauncher.RunUnitTests.cs"].Replace("\r\n", "\n") + '\n'
            );
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
        [TestCategory("Virtual device")]
        public void UnitTestLauncher_GeneratedApplication_RunWithNanoCLRHelper(bool communicateByNames)
        {
            (UnitTestLauncherGenerator generator, UnitTestLauncherGenerator.Application actual) = UnitTestLauncher_GenerateApplication(communicateByNames);

            #region Assert the generated code runs on the Virtual Device
            var logger = new LogMessengerMock();
            var nanoClr = new NanoCLRHelper(null, null, false, logger);
            logger.AssertEqual("", LoggingLevel.Error);

            logger = new LogMessengerMock();
            var outputCollector = new StringBuilder();
            bool result = nanoClr.RunAssembliesAsync(actual.Assemblies, null, LoggingLevel.Detailed, (o) => outputCollector.AppendLine(o), logger, null)
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
Outcome     : Passed
ErrorMessage: ''
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0
DisplayName : 'TestMethod1(1,1) - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1
DisplayName : 'TestMethod1(2,2) - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
DisplayName : 'Test - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
DisplayName : 'Test2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
DisplayName : 'TestOnDeviceWithSomeFile - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------", false);
            #endregion
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        [TestCategory("Virtual device")]
        public void UnitTestLauncher_GeneratedApplication_RunWithNanoCLRHelper_NoDeploymentConfiguration(bool communicateByNames)
        {
            (UnitTestLauncherGenerator generator, UnitTestLauncherGenerator.Application actual) = UnitTestLauncher_GenerateApplication(communicateByNames, false);

            #region Assert the generated code runs on the Virtual Device
            var logger = new LogMessengerMock();
            var nanoClr = new NanoCLRHelper(null, null, false, logger);
            logger.AssertEqual("", LoggingLevel.Error);

            logger = new LogMessengerMock();
            var outputCollector = new StringBuilder();
            bool result = nanoClr.RunAssembliesAsync(actual.Assemblies, null, LoggingLevel.Detailed, (o) => outputCollector.AppendLine(o), logger, null)
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
Outcome     : Passed
ErrorMessage: ''
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#0
DisplayName : 'TestMethod1(1,1) - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1#1
DisplayName : 'TestMethod1(2,2) - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test
DisplayName : 'Test - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2
DisplayName : 'Test2 - Test failed'
Outcome     : Failed
ErrorMessage: 'Test failed'
----------------------------------------
    
    
----------------------------------------
Test        : TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile
DisplayName : 'TestOnDeviceWithSomeFile - Setup failed'
Outcome     : Failed
ErrorMessage: 'Setup failed'
----------------------------------------", false);
            #endregion
        }

        private (UnitTestLauncherGenerator generator, UnitTestLauncherGenerator.Application application) UnitTestLauncher_GenerateApplication(bool communicateByNames, bool withDeploymentConfiguration = true)
        {
            #region Prepare assembly directory and unit test selection
            string applicationAssemblyDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);
            List<string> expectedAssemblies = AssemblyFilePaths.ToList();
            string assemblyFilePath = Path.Combine(applicationAssemblyDirectoryPath, "nanoFramework.UnitTestLauncher.pe");
            expectedAssemblies.Insert(0, assemblyFilePath);
            File.WriteAllText(assemblyFilePath, "This is an old version of the assembly");
            File.WriteAllText(Path.ChangeExtension(assemblyFilePath, ".dll"), "This is an old version of the assembly");

            var logger = new LogMessengerMock();
            var generator = new UnitTestLauncherGenerator(TestSelection, withDeploymentConfiguration ? CreateDeploymentConfiguration() : null, communicateByNames, logger);
            logger.AssertEqual("");
            #endregion

            #region Create the application
            logger = new LogMessengerMock();
            UnitTestLauncherGenerator.Application application = generator.GenerateAsApplication(applicationAssemblyDirectoryPath, logger);
            #endregion

            #region Assert it is created as expected
            logger.AssertEqual("");
            Assert.IsTrue(File.Exists(assemblyFilePath));

            if (withDeploymentConfiguration)
            {
                Assert.AreEqual(
@"TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile - TestOnDeviceWithSomeFile: Address
".Trim().Replace("\r\n", "\n") + '\n',
                    string.Join("\n", from tc in application.MissingDeploymentConfigurationKeys
                                      orderby tc.Key.FullyQualifiedName, tc.Key.DisplayName
                                      select $"{tc.Key.FullyQualifiedName} - {tc.Key.DisplayName}: {string.Join(";", from key in tc.Value orderby key select key)}"
                                ) + '\n'
                );
            }
            else
            {
                Assert.AreEqual(
@"TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile - TestOnDeviceWithSomeFile: Address;Device ID;xyzzy
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 - Test2: Make and model
".Trim().Replace("\r\n", "\n") + '\n',
                    string.Join("\n", from tc in application.MissingDeploymentConfigurationKeys
                                      orderby tc.Key.FullyQualifiedName, tc.Key.DisplayName
                                      select $"{tc.Key.FullyQualifiedName} - {tc.Key.DisplayName}: {string.Join(";", from key in tc.Value orderby key select key)}"
                                ) + '\n'
                );
            }
            #endregion



            return (generator, application);
        }

    }
}
