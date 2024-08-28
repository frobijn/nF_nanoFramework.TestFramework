// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
$@"Error: {pathPrefix}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]', 'int', 'long' or 'string'.
Error: {pathPrefix}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.
Error: {pathPrefix}TestWithALotOfErrors.cs(55,10): Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.", LoggingLevel.Error);
            TestSelection = testCases.TestOnVirtualDevice.First();

            foreach (string file in Directory.EnumerateFiles(Path.GetDirectoryName(assemblyFilePath), "*.dll"))
            {
                AssemblyFilePaths.Add(Path.ChangeExtension(file, ".pe"));
            }
        }

        public DeploymentConfiguration CreateDeploymentConfiguration()
        {
            string configDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);
            File.WriteAllText(Path.Combine(configDirectoryPath, "xyzzy.txt"), TestWithFrameworkExtensions_ConfigurationValue);
            File.WriteAllBytes(Path.Combine(configDirectoryPath, "MakeAndModel.bin"), TestClassTwoMethods_Method2_ConfigurationValue);

            string specificationFile = Path.Combine(configDirectoryPath, "deployment.json");
            File.WriteAllText(specificationFile, $@"{{
    ""DisplayName"": ""{GetType().Name}"",
    ""Configuration"":{{
        ""{TestWithFrameworkExtensions_ConfigurationKey}"": {{ ""File"": ""xyzzy.txt"" }},
        ""{TestWithFrameworkExtensions_ConfigurationKey2}"": 42,
        ""{TestClassTwoMethods_Method2_ConfigurationKey}"": {{ ""File"": ""MakeAndModel.bin"" }}
    }}
}}");
            return DeploymentConfiguration.Parse(specificationFile);
        }
        #endregion

        #region Test context properties
        /// <summary>
        /// The selection of test cases.
        /// </summary>
        public TestCaseSelection TestSelection
        {
            get;
            private set;
        }

        /// <summary>
        /// The assemblies in the test project's directory (*.pe)
        /// </summary>
        public List<string> AssemblyFilePaths
        {
            get;
        } = new List<string>();

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
        public const string TestWithFrameworkExtensions_ConfigurationKey2 = "Device ID";
        public const string TestWithFrameworkExtensions_ConfigurationKey3 = "Address";
        public const string TestWithFrameworkExtensions_ConfigurationValue = @"Value
for
xyzzy";
        #endregion
    }
}
