// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Unit test debugger")]
    [TestCategory("MSBuild")]
    public sealed class DebugTestCasesSpecificationTest
    {
        public TestContext TestContext { get; set; }

        #region Json (de)serialization
        [TestMethod]
        public void DebugTestCases_Serialization()
        {
            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string specificationFilePath = Path.Combine(testDirectory, "SelectUnitTests.json");

            #region Empty specification
            AssertSpecification(specificationFilePath,
                    new DebugTestCasesSpecification()
                    {
                    }
                    , @"{}");

            AssertSpecification(specificationFilePath,
                new DebugTestCasesSpecification()
                {
                }
                , @"{ ""TestCases"": { } }");
            #endregion

            #region Test case specification
            AssertSpecification(specificationFilePath,
                new DebugTestCasesSpecification()
                {
                    TestCases = new Dictionary<string, Dictionary<string, DebugTestCasesSpecification.TestMethodList>>()
                    {
                        { "Name.Space", new Dictionary<string, DebugTestCasesSpecification.TestMethodList> ()
                            {
                                { "TestClass", new DebugTestCasesSpecification.TestMethodList ()
                                    {
                                        TestMethods = new List<DebugTestCasesSpecification.TestMethodSpecification>()
                                        {
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "TestMethod" },
                                        }
                                    }
                                },
                            }
                        },
                    }
                }
                , @"{
                ""TestCases"": {
                    ""Name.Space"": {
                        ""TestClass"": [
                            ""TestMethod""
                        ]
                    }
                }
            }");

            AssertSpecification(specificationFilePath,
                new DebugTestCasesSpecification()
                {
                    TestCases = new Dictionary<string, Dictionary<string, DebugTestCasesSpecification.TestMethodList>>()
                    {
                        { "Name.Space", new Dictionary<string, DebugTestCasesSpecification.TestMethodList> ()
                            {
                                { "TestClass", new DebugTestCasesSpecification.TestMethodList ()
                                    {
                                        TestMethods = new List<DebugTestCasesSpecification.TestMethodSpecification>()
                                        {
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "DataRowMethod", DataRowAttributes = new List<int> () { 0, 2 } },
                                       }
                                    }
                                }
                            }
                        }
                    }
                }
                , @"{
                ""TestCases"": {
                    ""Name.Space"": {
                        ""TestClass"": [
                            { ""DataRowMethod"": [0, 2] }
                        ]
                    }
                }
            }");

            AssertSpecification(specificationFilePath,
                new DebugTestCasesSpecification()
                {
                    TestCases = new Dictionary<string, Dictionary<string, DebugTestCasesSpecification.TestMethodList>>()
                    {
                        { "Name.Space", new Dictionary<string, DebugTestCasesSpecification.TestMethodList> ()
                            {
                                { "TestClass", new DebugTestCasesSpecification.TestMethodList ()
                                    {
                                        TestMethods = new List<DebugTestCasesSpecification.TestMethodSpecification>()
                                        {
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "DataRowMethod", AllDataRows = true },
                                       }
                                    }
                                }
                            }
                        }
                    }
                }
                , @"{
    ""TestCases"": {
        ""Name.Space"": {
            ""TestClass"": [
                { ""DataRowMethod"": ""*"" }
            ]
        }
    }
}");

            AssertSpecification(specificationFilePath,
                new DebugTestCasesSpecification()
                {
                    TestCases = new Dictionary<string, Dictionary<string, DebugTestCasesSpecification.TestMethodList>>()
                    {
                        { "Name.Space", new Dictionary<string, DebugTestCasesSpecification.TestMethodList> ()
                            {
                                { "TestClass", new DebugTestCasesSpecification.TestMethodList ()
                                    {
                                        AllMethods = true
                                    }
                                }
                            }
                        }
                    }
                }
                , @"{
    ""TestCases"": {
        ""Name.Space"": {
            ""TestClass"": ""*""
        }
    }
}");

            AssertSpecification(specificationFilePath,
                new DebugTestCasesSpecification()
                {
                    TestCases = new Dictionary<string, Dictionary<string, DebugTestCasesSpecification.TestMethodList>>()
                    {
                        { "", new Dictionary<string, DebugTestCasesSpecification.TestMethodList> ()
                            {
                                { "TestClassWithoutNamespace", new DebugTestCasesSpecification.TestMethodList (){ AllMethods = true } }
                            }
                        }
                    }
                }
                , @"{
    ""TestCases"": {
        """": {
            ""TestClassWithoutNamespace"": ""*""
        }
    }
}");

            AssertSpecification(specificationFilePath,
                new DebugTestCasesSpecification()
                {
                    TestCases = new Dictionary<string, Dictionary<string, DebugTestCasesSpecification.TestMethodList>>()
                    {
                        { "Name.Space", new Dictionary<string, DebugTestCasesSpecification.TestMethodList> ()
                            {
                                { "TestClass1", new DebugTestCasesSpecification.TestMethodList ()
                                    {
                                        TestMethods = new List<DebugTestCasesSpecification.TestMethodSpecification>()
                                        {
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "TestMethod1" },
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "TestMethod2" },
                                        }
                                    }
                                },
                                { "TestClass2", new DebugTestCasesSpecification.TestMethodList ()
                                    {
                                        TestMethods = new List<DebugTestCasesSpecification.TestMethodSpecification>()
                                        {
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "DataRowMethod1", AllDataRows = true },
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "DataRowMethod2", DataRowAttributes = new List<int> () { 0, 2 } },
                                       }
                                    }
                                }
                            }
                        },
                        { "Other.Name.Space", new Dictionary<string, DebugTestCasesSpecification.TestMethodList> ()
                            {
                                { "TestClass1", new DebugTestCasesSpecification.TestMethodList ()
                                    {
                                        TestMethods = new List<DebugTestCasesSpecification.TestMethodSpecification>()
                                        {
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "TestMethod1" },
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "TestMethod2" },
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "DataRowMethod1", AllDataRows = true },
                                            new DebugTestCasesSpecification.TestMethodSpecification () { MethodName=  "DataRowMethod2", DataRowAttributes = new List<int> () { 0, 1 } },
                                        }
                                    }
                                }
                            }
                        },
                        { "", new Dictionary<string, DebugTestCasesSpecification.TestMethodList> ()
                            {
                                { "TestClassWithoutNamespace", new DebugTestCasesSpecification.TestMethodList (){ AllMethods = true } }
                            }
                        }
                    }
                }
                , @"{
    ""TestCases"": {
        ""Name.Space"": {
            ""TestClass1"": [
                ""TestMethod1"",
                ""TestMethod2""
            ],
            ""TestClass2"": [
                { ""DataRowMethod1"": ""*"" },
                { ""DataRowMethod2"": [0, 2] }
            ]
        },
        ""Other.Name.Space"": {
            ""TestClass1"": [
                ""TestMethod1"",
                ""TestMethod2"",
                { ""DataRowMethod1"": ""*"" },
                { ""DataRowMethod2"": [0, 1] }
            ]
        },
        """": {
            ""TestClassWithoutNamespace"": ""*""
        }
    }
}");
            #endregion

            #region DeployConfiguration
            AssertSpecification(specificationFilePath,
                new DebugTestCasesSpecification()
                {
                    DeploymentConfigurationFilePath = Path.GetFullPath(Path.Combine(testDirectory, "..", "config", "DevBoard.json"))
                }
                ,
                @"{ ""DeploymentConfiguration"": ""../config/DevBoard.json"" }"
            );
            #endregion
        }

        private static void AssertSpecification(string specificationFilePath, DebugTestCasesSpecification expected, string actualJson)
        {
            File.WriteAllText(specificationFilePath, actualJson);
            var actual = DebugTestCasesSpecification.Parse(specificationFilePath);
            AssertSpecification(expected, actual);
        }

        private static void AssertSpecification(DebugTestCasesSpecification expected, DebugTestCasesSpecification actual)
        {
            Assert.AreEqual(expected.DeploymentConfigurationFilePath, actual.DeploymentConfigurationFilePath);

            string ListTestCases(Dictionary<string, Dictionary<string, DebugTestCasesSpecification.TestMethodList>> testCases)
            {
                var result = new List<string>();
                if (!(testCases is null))
                {
                    foreach (KeyValuePair<string, Dictionary<string, DebugTestCasesSpecification.TestMethodList>> ns in testCases)
                    {
                        foreach (KeyValuePair<string, DebugTestCasesSpecification.TestMethodList> tc in ns.Value)
                        {
                            if (tc.Value.AllMethods)
                            {
                                result.Add($"{ns.Key}:{tc.Key}:*");
                            }
                            else
                            {
                                foreach (DebugTestCasesSpecification.TestMethodSpecification tm in tc.Value.TestMethods)
                                {
                                    if (tm.AllDataRows)
                                    {
                                        result.Add($"{ns.Key}:{tc.Key}:{tm.MethodName}.*");
                                    }
                                    else if (tm.DataRowAttributes is null)
                                    {
                                        result.Add($"{ns.Key}:{tc.Key}:{tm.MethodName}");
                                    }
                                    else
                                    {
                                        result.Add($"{ns.Key}:{tc.Key}:{tm.MethodName}[{string.Join(",", from i in tm.DataRowAttributes orderby i select i)}]");
                                    }
                                }
                            }
                        }
                    }
                }
                result.Sort();
                return string.Join("\n", result) + '\n';
            }
            Assert.AreEqual(ListTestCases(expected.TestCases), ListTestCases(actual.TestCases));
        }
        #endregion

        #region Json schema
        /// <summary>
        /// This test asserts that the schema is unchanged. To really
        /// test the schema, save it to a .json file, e.g., "Test.schema.json"
        /// and create another json file with content <c>{ "$schema": "Test.schema.json" }</c>.
        /// Open "Test.schema.json" in Visual Studio to check whether it is valid,
        /// open the other json file and start typing to see whether the schema does
        /// what it should do.
        /// </summary>
        [TestMethod]
        public void DebugTestCases_JsonSchema()
        {
            #region Get a bunch of test cases
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v2");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath2 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath2);
            string projectFilePath3 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Execution.v3");
            string assemblyFilePath3 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath3);
            var logger = new LogMessengerMock();
            var testCases = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath2, assemblyFilePath3 },
                (a) => ProjectSourceInventory.FindProjectFilePath(a, logger),
                true, logger);
            logger.AssertEqual(
$@"Error: {Path.GetDirectoryName(projectFilePath2)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]', 'int', 'long' or 'string'.
Error: {Path.GetDirectoryName(projectFilePath2)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.
Error: {Path.GetDirectoryName(projectFilePath2)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(55,10): Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.",
                LoggingLevel.Error);
            #endregion

            string actual = DebugTestCasesSpecification.GenerateJsonSchema(testCases);
            Assert.AreEqual(
@"{
    ""$schema"": ""http://json-schema.org/draft-07/schema"",
    ""type"": ""object"",
    ""description"": ""Specification of the test cases to be run."",
    ""properties"": {
        ""DeploymentConfiguration"": {
            ""description"": ""The path to the file that contains the deployment configuration. The path can be absolute or relative to the directory this specification file resides in."",
            ""type"": ""string""
        },
        ""TestCases"": {
            ""$ref"": ""#/definitions/Namespaces_0""
        }
    },
    ""required"": [ ""TestCases"" ],
    ""definitions"": {
        ""Namespaces_0"": {
            ""type"": ""object"",
            ""description"": ""The selection of test cases, grouped by namespace and test class name."",
            ""properties"": {
                ""TestFramework.Tooling.Execution.Tests"": { ""$ref"": ""#/definitions/Namespace_3"" },
                ""TestFramework.Tooling.Tests.NFUnitTest"": { ""$ref"": ""#/definitions/Namespace_5"" }
            }
        },
        ""TestMethod_1"": {
            ""type"": ""object"",
            ""properties"": {
                ""Test1"": {
                    ""description"": ""Specify which data row attributes should be included. The first data row attribute in the code is 0, the next one is 1, etc. There are 2 attributes, so the numbers must be between 0 and 1. To select all, specify \""*\"" instead of an array of indices."",
                    ""type"": ""array"", ""items"": { ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 1 } ] }
                }
            }
        },,
        ""TestMethod_2"": {
            ""type"": ""object"",
            ""properties"": {
                ""MethodToRunOnRealHardwareWithData"": {
                    ""description"": ""Specify which data row attributes should be included. The first data row attribute in the code is 0, the next one is 1, etc. There are 1 attributes, so the numbers must be between 0 and 0. To select all, specify \""*\"" instead of an array of indices."",
                    ""type"": ""array"", ""items"": { ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 0 } ] }
                }
            }
        },,
        ""Namespace_3"": {
            ""type"": ""object"",
            ""description"": ""Test cases for classes within the namespace. Select the classes to include."",
            ""properties"": {
                ""CleanupFailedInTest"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""FailInCleanUp"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""FailInConstructor"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""FailInDispose"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""FailInFirstCleanUp"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""FailInFirstSetup"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""FailInSetup"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""FailInTest"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""NonFailingTest"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""NonStaticTestClass"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] } ]
                },,
                ""NonStaticTestClassInstancePerMethod"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] } ]
                },,
                ""NonStaticTestClassSetupCleanupPerMethod"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] } ]
                },,
                ""SkippedInConstructor"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""SkippedInSetup"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""SkippedInTest"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""StaticTestClass"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] } ]
                },,
                ""StaticTestClassSetupCleanupPerMethod"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] } ]
                },,
                ""TestClassWithMultipleSetupCleanup"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"" ] } ]
                },,
                ""TestWithFrameworkExtensions"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""TestDeviceWithSomeFile"" ] } ]
                },,
                ""TestWithMethods"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""anyOf"": [
                        { ""type"": ""string"", ""enum"": [ ""Test2"" ] },
                        { ""$ref"": ""#/definitions/TestMethod_1"" }
                    ] } ]
                },,
                ""TestWithNewTestMethodsAttributes"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""anyOf"": [
                        { ""type"": ""string"", ""enum"": [ ""MethodToRunOnRealHardware"", ""MethodToRunOnRealHardwareWithData"", ""MethodWithTraits"" ] },
                        { ""$ref"": ""#/definitions/TestMethod_2"" }
                    ] } ]
                }
            }
        },
        ""TestMethod_4"": {
            ""type"": ""object"",
            ""properties"": {
                ""TestMethod1"": {
                    ""description"": ""Specify which data row attributes should be included. The first data row attribute in the code is 0, the next one is 1, etc. There are 2 attributes, so the numbers must be between 0 and 1. To select all, specify \""*\"" instead of an array of indices."",
                    ""type"": ""array"", ""items"": { ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 1 } ] }
                }
            }
        },,
        ""Namespace_5"": {
            ""type"": ""object"",
            ""description"": ""Test cases for classes within the namespace. Select the classes to include."",
            ""properties"": {
                ""NonStaticTestClass"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] } ]
                },,
                ""StaticTestClass"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Method"" ] } ]
                },,
                ""TestAllCurrentAttributes"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""anyOf"": [
                        { ""type"": ""string"", ""enum"": [ ""TestMethod"" ] },
                        { ""$ref"": ""#/definitions/TestMethod_4"" }
                    ] } ]
                },,
                ""TestWithFrameworkExtensions"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""TestOnDeviceWithSomeFile"", ""TestThatIsNowInDisarray"" ] } ]
                },,
                ""TestWithMethods"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""Test"", ""Test2"" ] } ]
                },,
                ""TestWithNewTestMethodsAttributes"": {
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ { ""type"": ""string"", ""enum"": [ ""*"" ] }, { ""type"": ""array"", ""items"": { ""type"": ""string"", ""enum"": [ ""MethodWithNewTestMethods"", ""MethodWithTraits"" ] } ]
                }
            }
        }
    }
}
".Replace("\r\n", "\n"),
                actual.Replace("\r\n", "\n") + '\n');
        }
        #endregion

        #region Test case selection
        [TestMethod]
        public void DebugTestCases_Selection()
        {
            #region Get a bunch of test cases
            string projectFilePath1 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath1 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath1);
            string projectFilePath2 = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Execution.v3");
            string assemblyFilePath2 = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath2);
            var logger = new LogMessengerMock();
            var testCases = new TestCaseCollection(new string[] { assemblyFilePath1, assemblyFilePath2 },
                (a) => ProjectSourceInventory.FindProjectFilePath(a, logger),
                true, logger);
            logger.AssertEqual(
$@"Error: {Path.GetDirectoryName(projectFilePath1)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]', 'int', 'long' or 'string'.
Error: {Path.GetDirectoryName(projectFilePath1)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.
Error: {Path.GetDirectoryName(projectFilePath1)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(55,10): Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.",
                LoggingLevel.Error);
            #endregion

            string testDirectory = TestDirectoryHelper.GetTestDirectory(TestContext);
            string specificationFilePath = Path.Combine(testDirectory, "SelectUnitTests.json");

            #region Empty selection
            AssertSelection(specificationFilePath, testCases, "", "", @"{}");
            #endregion

            #region Each of the basic selection methods
            AssertSelection(specificationFilePath, testCases, "",
@"TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile",
@"{
    ""TestCases"": {
        ""TestFramework.Tooling.Tests.NFUnitTest"": {
            ""TestWithFrameworkExtensions"": [
                ""TestOnDeviceWithSomeFile""
            ]
        }
    }
}");

            AssertSelection(specificationFilePath, testCases, "",
@"TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=1",
@"{
    ""TestCases"": {
        ""TestFramework.Tooling.Tests.NFUnitTest"": {
            ""TestAllCurrentAttributes"": [
                { ""TestMethod1"": [1] }
            ]
        }
    }
}");
            AssertSelection(specificationFilePath, testCases, "",
@"TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=0
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=1",
@"{
    ""TestCases"": {
        ""TestFramework.Tooling.Tests.NFUnitTest"": {
            ""TestAllCurrentAttributes"": [
                { ""TestMethod1"": ""*"" }
            ]
        }
    }
}");
            AssertSelection(specificationFilePath, testCases, "",
@"TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=0
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=1",
@"{
    ""TestCases"": {
        ""TestFramework.Tooling.Tests.NFUnitTest"": {
            ""TestAllCurrentAttributes"": ""*""
        }
    }
}");
            #endregion

            #region Selection (partly) not found
            AssertSelection(specificationFilePath, testCases,
@"Error: No test cases found for test class 'NoSuchTestClass2' in namespace 'TestFramework.Tooling.Tests.NFUnitTest'.
Error: No test case found for test method 'TestMethod' of test class 'NoSuchTestClass1' in namespace 'TestFramework.Tooling.Tests.NFUnitTest'.
Error: No test case found for test method 'NoSuchTestMethod1' of test class 'TestAllCurrentAttributes' in namespace 'TestFramework.Tooling.Tests.NFUnitTest'.
Error: No test case found for test method 'NoSuchTestMethod2' of test class 'TestAllCurrentAttributes' in namespace 'TestFramework.Tooling.Tests.NFUnitTest'.
Error: No test case found for data attributes #0, #1 of test method 'NoSuchTestMethod3' of test class 'TestAllCurrentAttributes' in namespace 'TestFramework.Tooling.Tests.NFUnitTest'.
Error: No test case found for data attributes #2 of test method 'TestMethod1' of test class 'TestAllCurrentAttributes' in namespace 'TestFramework.Tooling.Tests.NFUnitTest'.",
@"TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=0",
    @"{
    ""TestCases"": {
        ""TestFramework.Tooling.Tests.NFUnitTest"": {
            ""TestAllCurrentAttributes"": [
                ""TestMethod"",
                { ""TestMethod1"": [0, 2] },
                ""NoSuchTestMethod1"",
                { ""NoSuchTestMethod2"": ""*"" },
                { ""NoSuchTestMethod3"": [0, 1] },
            ],
            ""NoSuchTestClass1"": [
                ""TestMethod""
            ],
            ""NoSuchTestClass2"": ""*""
        }
    }
}");
            #endregion

            #region Select from multiple assemblies
            AssertSelection(specificationFilePath, testCases, "",
@"TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile
TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1 DR=0
TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1 DR=1
TestFramework.Tooling.Execution.Tests.TestWithMethods.Test2
TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware
TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=0",
@"{
  ""TestCases"": {
    ""TestFramework.Tooling.Execution.Tests"": {
      ""TestWithNewTestMethodsAttributes"": [
        ""MethodToRunOnRealHardware""
      ],
      ""TestWithMethods"": [
        ""Test1"",
        ""Test2""
      ],
      ""TestWithFrameworkExtensions"": [
        ""TestDeviceWithSomeFile""
      ]
    },
    ""TestFramework.Tooling.Tests.NFUnitTest"": {
      ""StaticTestClass"": [ ""Method"" ],
      ""TestAllCurrentAttributes"": [
        ""TestMethod"",
        { ""TestMethod1"": [ 0 ] }
      ]
    }
  }
}");
            #endregion
        }

        private static void AssertSelection(string specificationFilePath, TestCaseCollection testCases, string expectedErrors, string expectedTestCases, string actualJson)
        {
            File.WriteAllText(specificationFilePath, actualJson);
            var specification = DebugTestCasesSpecification.Parse(specificationFilePath);

            var logger = new LogMessengerMock();
            var actual = specification.SelectTestCases(testCases, logger, false).ToList();
            logger.AssertEqual(expectedErrors, LoggingLevel.Error);

            var actualList = new List<TestCase>();
            foreach (TestCaseSelection selection in actual)
            {
                actualList.AddRange(from tc in selection.TestCases
                                    where tc.selectionIndex >= 0
                                    select tc.testCase);
            }

            Assert.AreEqual(
                expectedTestCases.Trim().Replace("\r\n", "\n") + '\n',
                string.Join("\n", from tc in actualList
                                  orderby tc.FullyQualifiedName, tc.DataRowIndex
                                  select tc.DataRowIndex < 0 ? tc.FullyQualifiedName : $"{tc.FullyQualifiedName} DR={tc.DataRowIndex}"
                ) + '\n'
            );

            if (!string.IsNullOrEmpty(expectedErrors))
            {
                // Same for MSBuild style errors
                expectedErrors = string.Join("\n", from e in expectedErrors.Trim().Replace("\r\n", "\n").Split('\n')
                                                   select $"Error: {specificationFilePath}(0,0): {e}");

                logger = new LogMessengerMock();
                actual = specification.SelectTestCases(testCases, logger, true).ToList();
                logger.AssertEqual(expectedErrors, LoggingLevel.Error);
            }

            // No exception without logger
            specification.SelectTestCases(testCases, null, false).ToList();
        }
        #endregion
    }
}
