// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    [TestCategory("Unit test launcher")]
    [TestCategory(Constants.VirtualDevice_TestCategory)]
    public class UnitTestLauncherGeneratorTest_Execution_v3
    {
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void UnitTestLauncher_GeneratedApplication_RunWithNanoCLRHelper_Execution_v3_WithConfiguration(bool communicateByNames)
        {
            UnitTestLauncher_GenerateApplication_RunWithNanoCLRHelper(communicateByNames, true,
$@"----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardwareWithData(0,V)
DisplayName : 'MethodToRunOnRealHardwareWithData [{Constants.VirtualDevice_Description}] - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware(V)
DisplayName : 'MethodToRunOnRealHardware [{Constants.VirtualDevice_Description}] - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodWithCategories(V)
DisplayName : 'MethodWithCategories - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithMethods.Test2(V)
DisplayName : 'Test2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1(1,V)
DisplayName : 'Test1(123) - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1(0,V)
DisplayName : 'Test1(42) - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_AreDevicesEqual(V)
DisplayName : 'TestOnDeviceWithProgrammingError_AreDevicesEqual [{Constants.VirtualDevice_Description}] - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_ShouldTestOnDevice(V)
DisplayName : 'TestOnDeviceWithProgrammingError_ShouldTestOnDevice [{Constants.VirtualDevice_Description}] - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile(V)
DisplayName : 'TestDeviceWithSomeFile [{Constants.VirtualDevice_Description}] - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestClassWithMultipleSetupCleanup.Test(V)
DisplayName : 'Test - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Method2(V)
DisplayName : 'Method2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Method1(V)
DisplayName : 'Method1 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Method2(V)
DisplayName : 'Method2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Method1(V)
DisplayName : 'Method1 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Method2(V)
DisplayName : 'Method2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Method1(V)
DisplayName : 'Method1 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Method2(V)
DisplayName : 'Method2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Method1(V)
DisplayName : 'Method1 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.StaticTestClass.Method2(V)
DisplayName : 'Method2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.StaticTestClass.Method1(V)
DisplayName : 'Method1 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp.Test(V)
DisplayName : 'Test - Cleanup failed'
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInFirstSetup.Test(V)
DisplayName : 'Test - Setup failed'
Outcome     : Failed
ErrorMessage: 'Setup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.SkippedInSetup.Test(V)
DisplayName : 'Test - Test has not been run'
Outcome     : Skipped
ErrorMessage: 'Test has not been run'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.SkippedInConstructor.Test(V)
DisplayName : 'Test - Test has not been run'
Outcome     : Skipped
ErrorMessage: 'Test has not been run'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonFailingTest.Test(V)
DisplayName : 'Test - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInDispose.Test(V)
DisplayName : 'Test - Cleanup failed'
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInCleanUp.Test(V)
DisplayName : 'Test - Cleanup failed'
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.CleanupFailedInTest.Test(V)
DisplayName : 'Test - Cleanup failed'
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.SkippedInTest.Test(V)
DisplayName : 'Test - Test skipped'
Outcome     : Skipped
ErrorMessage: 'Test skipped'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInTest.Test(V)
DisplayName : 'Test - Test failed'
Outcome     : Failed
ErrorMessage: 'Test failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInSetup.Test(V)
DisplayName : 'Test - Setup failed'
Outcome     : Failed
ErrorMessage: 'Setup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInConstructor.Test(V)
DisplayName : 'Test - Setup failed'
Outcome     : Failed
ErrorMessage: 'Setup failed'
----------------------------------------");
        }

        /// <summary>
        /// Some tests fail because they require deployment configuration. This test verifies that the deployment configuration
        /// is actually passed to the setup/test methods. Hypothetically the tests in <see cref="UnitTestLauncher_GeneratedApplication_RunWithNanoCLRHelper_Execution_v3_WithConfiguration"/>
        /// could pass because the methods are not run, rather that they did not receive the deployment configuration.
        /// </summary>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void UnitTestLauncher_GeneratedApplication_RunWithNanoCLRHelper_Execution_v3_NoConfiguration(bool communicateByNames)
        {
            UnitTestLauncher_GenerateApplication_RunWithNanoCLRHelper(communicateByNames, false,
$@"----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardwareWithData(0,V)
DisplayName : 'MethodToRunOnRealHardwareWithData [{Constants.VirtualDevice_Description}] - Test failed'
Outcome     : Failed
ErrorMessage: 'Test failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware(V)
DisplayName : 'MethodToRunOnRealHardware [{Constants.VirtualDevice_Description}] - Test failed'
Outcome     : Failed
ErrorMessage: 'Test failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodWithCategories(V)
DisplayName : 'MethodWithCategories - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithMethods.Test2(V)
DisplayName : 'Test2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1(1,V)
DisplayName : 'Test1(123) - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1(0,V)
DisplayName : 'Test1(42) - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_AreDevicesEqual(V)
DisplayName : 'TestOnDeviceWithProgrammingError_AreDevicesEqual [{Constants.VirtualDevice_Description}] - Setup failed'
Outcome     : Failed
ErrorMessage: 'Setup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestOnDeviceWithProgrammingError_ShouldTestOnDevice(V)
DisplayName : 'TestOnDeviceWithProgrammingError_ShouldTestOnDevice [{Constants.VirtualDevice_Description}] - Setup failed'
Outcome     : Failed
ErrorMessage: 'Setup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile(V)
DisplayName : 'TestDeviceWithSomeFile [{Constants.VirtualDevice_Description}] - Setup failed'
Outcome     : Failed
ErrorMessage: 'Setup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.TestClassWithMultipleSetupCleanup.Test(V)
DisplayName : 'Test - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Method2(V)
DisplayName : 'Method2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClassInstancePerMethod.Method1(V)
DisplayName : 'Method1 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Method2(V)
DisplayName : 'Method2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClassSetupCleanupPerMethod.Method1(V)
DisplayName : 'Method1 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Method2(V)
DisplayName : 'Method2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonStaticTestClass.Method1(V)
DisplayName : 'Method1 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Method2(V)
DisplayName : 'Method2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.StaticTestClassSetupCleanupPerMethod.Method1(V)
DisplayName : 'Method1 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.StaticTestClass.Method2(V)
DisplayName : 'Method2 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.StaticTestClass.Method1(V)
DisplayName : 'Method1 - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInFirstCleanUp.Test(V)
DisplayName : 'Test - Cleanup failed'
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInFirstSetup.Test(V)
DisplayName : 'Test - Setup failed'
Outcome     : Failed
ErrorMessage: 'Setup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.SkippedInSetup.Test(V)
DisplayName : 'Test - Test has not been run'
Outcome     : Skipped
ErrorMessage: 'Test has not been run'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.SkippedInConstructor.Test(V)
DisplayName : 'Test - Test has not been run'
Outcome     : Skipped
ErrorMessage: 'Test has not been run'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.NonFailingTest.Test(V)
DisplayName : 'Test - Passed'
Outcome     : Passed
ErrorMessage: ''
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInDispose.Test(V)
DisplayName : 'Test - Cleanup failed'
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInCleanUp.Test(V)
DisplayName : 'Test - Cleanup failed'
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.CleanupFailedInTest.Test(V)
DisplayName : 'Test - Cleanup failed'
Outcome     : Failed
ErrorMessage: 'Cleanup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.SkippedInTest.Test(V)
DisplayName : 'Test - Test skipped'
Outcome     : Skipped
ErrorMessage: 'Test skipped'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInTest.Test(V)
DisplayName : 'Test - Test failed'
Outcome     : Failed
ErrorMessage: 'Test failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInSetup.Test(V)
DisplayName : 'Test - Setup failed'
Outcome     : Failed
ErrorMessage: 'Setup failed'
----------------------------------------


----------------------------------------
Test        : TestFramework.Tooling.Execution.Tests.FailInConstructor.Test(V)
DisplayName : 'Test - Setup failed'
Outcome     : Failed
ErrorMessage: 'Setup failed'
----------------------------------------");
        }

        #region Helpers
        public TestContext TestContext { get; set; }

        private void UnitTestLauncher_GenerateApplication_RunWithNanoCLRHelper(bool communicateByNames, bool withDeploymentConfiguration, string expectedTestResults)
        {
            #region Get the test cases
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Execution.v3");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);

            string pathPrefix = Path.GetDirectoryName(projectFilePath) + Path.DirectorySeparatorChar;
            var logger = new LogMessengerMock();
            var testCases = new TestCaseCollection(assemblyFilePath, projectFilePath, logger);
            logger.AssertEqual("");
            TestCaseSelection testSelection = testCases.TestOnVirtualDevice.First();
            #endregion

            #region Prepare assembly directory and unit test selection
            string testDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);

            logger = new LogMessengerMock();
            var generator = new UnitTestLauncherGenerator(testSelection, withDeploymentConfiguration ? CreateDeploymentConfiguration(testDirectoryPath) : null, communicateByNames, logger);
            logger.AssertEqual("");
            #endregion

            #region Generate the application
            logger = new LogMessengerMock();
            UnitTestLauncherGenerator.Application application = generator.GenerateAsApplication(testDirectoryPath, logger);

            logger.AssertEqual("");
            Assert.IsTrue(File.Exists(Path.Combine(testDirectoryPath, "nanoFramework.UnitTestLauncher.pe")));
            #endregion

            #region Assert the generated code runs on the Virtual Device
            logger = new LogMessengerMock();
            var nanoClr = new NanoCLRHelper(null, null, false, logger);
            logger.AssertEqual("", LoggingLevel.Error);

            logger = new LogMessengerMock();
            var outputCollector = new StringBuilder();
            bool result = nanoClr.RunAssembliesAsync(application.Assemblies, null, LoggingLevel.Detailed, (o) => outputCollector.AppendLine(o), logger, null)
                                .GetAwaiter().GetResult();
            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsTrue(result);
            #endregion

            #region Assert the output is correct
            var testResults = new List<nanoFramework.TestFramework.Tooling.TestResult>();
            var parser = new UnitTestsOutputParser(testSelection, null, application.ReportPrefix, (t) => testResults.AddRange(t));
            parser.AddOutput(outputCollector.ToString());
            parser.Flush();

            testResults.AssertResults(testSelection, expectedTestResults, false);
            #endregion
        }

        private DeploymentConfiguration CreateDeploymentConfiguration(string configDirectoryPath)
        {
            File.WriteAllText(Path.Combine(configDirectoryPath, "config_data.txt"), @"Textual value
for
data.txt");
            File.WriteAllBytes(Path.Combine(configDirectoryPath, "config_data.bin"), new byte[] { 3, 1, 4, 1, 5 });

            string specificationFile = Path.Combine(configDirectoryPath, "deployment.json");
            File.WriteAllText(specificationFile, $@"{{
    ""DisplayName"": ""{GetType().Name}"",
    ""Configuration"":{{
        ""data.txt"": {{ ""File"": ""config_data.txt"" }},
        ""data.bin"": {{ ""File"": ""config_data.bin"" }},
        ""RGB LED pin"": 42,
    }}
}}");
            return DeploymentConfiguration.Parse(specificationFile);
        }
        #endregion
    }
}
