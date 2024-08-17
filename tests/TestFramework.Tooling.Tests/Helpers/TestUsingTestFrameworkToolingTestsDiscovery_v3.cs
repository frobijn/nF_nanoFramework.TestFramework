// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;

namespace TestFramework.Tooling.Tests.Helpers
{
    public abstract class TestUsingTestFrameworkToolingTestsDiscovery_v3
    {
        #region Initialisation and helpers
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void CreateTestCases()
        {
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            string pathPrefix = Path.GetDirectoryName(projectFilePath) + Path.DirectorySeparatorChar;
            var logger = new LogMessengerMock();
            var testCases = new TestCaseCollection(
                new (string, string, string)[]
                {
                    (assemblyFilePath, TestClassWithSetupCleanup_TestMethodName, $"{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_TestMethodName}"),
                    (assemblyFilePath, $"{TestClassWithSetupCleanup_DataRowMethodName}(1,1)", $"{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}"),
                    (assemblyFilePath, $"{TestClassWithSetupCleanup_DataRowMethodName}(2,2)", $"{TestClassWithSetupCleanup_FQN}.{TestClassWithSetupCleanup_DataRowMethodName}"),
                    (assemblyFilePath, TestClassTwoMethods_Method1Name, $"{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method1Name}"),
                    (assemblyFilePath, TestClassTwoMethods_Method2Name, $"{TestClassTwoMethods_FQN}.{TestClassTwoMethods_Method2Name}"),
                    (assemblyFilePath, TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName, $"{TestWithFrameworkExtensions_FQN}.{TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName}"),
                },
                (f) => ProjectSourceInventory.FindProjectFilePath(f, logger),
                false,
                logger);
            logger.AssertEqual(
$@"Error: {pathPrefix}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]' or 'string'.
Error: {pathPrefix}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.", LoggingLevel.Error);
            TestSelection = testCases.TestOnVirtualDevice.First();
        }

        public DeploymentConfiguration CreateDeploymentConfiguration()
        {
            string configDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);
            File.WriteAllText(Path.Combine(configDirectoryPath, "xyzzy.txt"), TestWithFrameworkExtensions_ConfigurationValue);
            File.WriteAllBytes(Path.Combine(configDirectoryPath, "MakeAndModel.bin"), TestClassTwoMethods_Method2_ConfigurationValue);

            return DeploymentConfiguration.Parse($@"{{
    ""DisplayName"": ""{GetType().Name}"",
    ""Configuration"":{{
        ""{TestWithFrameworkExtensions_ConfigurationKey}"": {{ ""File"": ""xyzzy.txt"" }},
        ""{TestClassTwoMethods_Method2_ConfigurationKey}"": {{ ""File"": ""MakeAndModel.bin"" }}
    }}
}}
", configDirectoryPath, null);
        }
        #endregion

        #region Test context properties
        public TestCaseSelection TestSelection
        {
            get;
            private set;
        }

        public string ReportPrefix
        {
            get;
        } = Guid.NewGuid().ToString("N");

        public const string TestClassWithSetupCleanup_FQN = "TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes";
        public const string TestClassWithSetupCleanup_TestMethodName = "TestMethod";
        public const string TestClassWithSetupCleanup_DataRowMethodName = "TestMethod1";

        public const string TestClassTwoMethods_FQN = "TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods";
        public const string TestClassTwoMethods_Method1Name = "Test";
        public const string TestClassTwoMethods_Method2Name = "Test2";
        public const string TestClassTwoMethods_Method2_ConfigurationKey = "Make and model";
        public static readonly byte[] TestClassTwoMethods_Method2_ConfigurationValue = new byte[] { 3, 1, 4, 1, 5 };

        public const string TestWithFrameworkExtensions_FQN = "TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions";
        public const string TestWithFrameworkExtensions_TestOnDeviceWithSomeFileName = "TestOnDeviceWithSomeFile";
        public const string TestWithFrameworkExtensions_ConfigurationKey = "xyzzy";
        public const string TestWithFrameworkExtensions_ConfigurationValue = @"Value
for
xyzzy";
        #endregion
    }
}
