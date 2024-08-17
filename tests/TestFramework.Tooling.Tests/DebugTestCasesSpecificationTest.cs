// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Unit test debugger")]
    public sealed class DebugTestCasesSpecificationTest
    {
        #region Json (de)serialization
        [DataRow(@"{
  ""TestCases"": {
    ""TestFramework.Tooling.Tests"": {
      ""DebugTestCasesSpecificationTest"": [
        ""DebugTestCasesSerialization""
      ]
    }
  }
}")]
        [DataRow(@"{
  ""ToBeRunOnVirtualDevice"": false,
  ""ToBeRunOnRealHardware"": true,
  ""TestCases"": {
    ""TestFramework.Tooling.Tests"": {
      ""DebugTestCasesSpecificationTest"": [
        ""DebugTestCasesSerialization"",
        {
          ""TestMethodWithDataRows"": [
            0,
            1
          ]
        }
      ]
    }
  }
}")]
        [DataRow(@"{
  ""$schema"": ""obj/TestCaseSelection.schema.json"",
  ""TestCases"": {
    ""TestFramework.Tooling.Tests"": {
      ""DebugTestCasesSpecificationTest"": [
        ""DebugTestCasesSerialization""
      ]
    }
  }
}")]

        public void DebugTestCases_Serialization(string json)
        {
            var actual = DebugTestCasesSpecification.Parse(json);
            string actualJson = actual.ToJson();
            Assert.AreEqual(json, actualJson);
        }
        #endregion

        #region Json schema
        /// <summary>
        /// This test asserts that the schema is unchanged. To really
        /// test the schema, save it to a .json file, e.g., "test.schema.json"
        /// and create another json file with content <c>{ "$schema": "test.schema.json" }</c>.
        /// Open "test.schema.json" in Visual Studio to check whether it is valid,
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
$@"Error: {Path.GetDirectoryName(projectFilePath2)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]' or 'string'.
Error: {Path.GetDirectoryName(projectFilePath2)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.",
                LoggingLevel.Error);
            #endregion

            string actual = DebugTestCasesSpecification.GenerateJsonSchema(testCases);
            Assert.AreEqual(
@"{
    ""$schema"": ""http://json-schema.org/draft-07/schema"",
    ""type"": ""object"",
    ""description"": ""Specification of the test cases to be run."",
    ""properties"": {
        ""ToBeRunOnVirtualDevice"": {
            ""description"": ""Indicates whether to include tests that should be run on the virtual device. If omitted, the tests are included."",
            ""type"": ""boolean""
        },
        ""ToBeRunOnRealHardware"": {
            ""description"": ""Indicates whether to include tests that should be run on real hardware. If omitted, the tests are included."",
            ""type"": ""boolean""
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
                    ""type"": ""array"", ""items"": { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 1 },
                    ""description"": ""Specify which data row attributes should be included. The first data row attribute in the code is 0, the next one is 1, etc. There are 2 attributes, so the numbers must be between 0 and 1. To select all, list the method name instead of this object."",
                }
            }
        },,
        ""TestMethod_2"": {
            ""type"": ""object"",
            ""properties"": {
                ""MethodToRunOnRealHardwareWithData"": {
                    ""type"": ""array"", ""items"": { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 0 },
                    ""description"": ""Specify which data row attributes should be included. The first data row attribute in the code is 0, the next one is 1, etc. There are 1 attributes, so the numbers must be between 0 and 0. To select all, list the method name instead of this object."",
                }
            }
        },,
        ""Namespace_3"": {
            ""type"": ""object"",
            ""description"": ""Test cases for classes within the namespace. Select the classes to include."",
            ""properties"": {
                ""CleanupFailedInTest"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Test"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""FailInCleanUp"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Test"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""FailInConstructor"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Test"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""FailInDispose"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Test"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""FailInSetup"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Test"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""FailInTest"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Test"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""InconclusiveInTest"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Test"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""NonFailingTest"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Test"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""NonStaticTestClass"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""NonStaticTestClassInstancePerMethod"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""NonStaticTestClassSetupCleanupPerMethod"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""StaticTestClass"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""StaticTestClassSetupCleanupPerMethod"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""TestWithFrameworkExtensions"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Setup"", ""TestDeviceWithSomeFile"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },,
                ""TestWithMethods"": {
                    ""type"": ""array"", ""items"": { ""anyOf"": [
                        { ""type"": ""string"", ""enum"": [ ""Test1"", ""Test2"" ] },
                        { ""$ref"": ""#/definitions/TestMethod_1"" }
                    ] },
                    ""description"": ""Specify which test methods should be included."",
                },,
                ""TestWithNewTestMethodsAttributes"": {
                    ""type"": ""array"", ""items"": { ""anyOf"": [
                        { ""type"": ""string"", ""enum"": [ ""MethodToRunOnRealHardware"", ""MethodToRunOnRealHardwareWithData"", ""MethodWithTraits"" ] },
                        { ""$ref"": ""#/definitions/TestMethod_2"" }
                    ] },
                    ""description"": ""Specify which test methods should be included."",
                }
            }
        },
        ""TestMethod_4"": {
            ""type"": ""object"",
            ""properties"": {
                ""TestMethod1"": {
                    ""type"": ""array"", ""items"": { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 1 },
                    ""description"": ""Specify which data row attributes should be included. The first data row attribute in the code is 0, the next one is 1, etc. There are 2 attributes, so the numbers must be between 0 and 1. To select all, list the method name instead of this object."",
                }
            }
        },,
        ""Namespace_5"": {
            ""type"": ""object"",
            ""description"": ""Test cases for classes within the namespace. Select the classes to include."",
            ""properties"": {
                ""NonStaticTestClass"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Method1"", ""Method2"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""StaticTestClass"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Method"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },,
                ""TestAllCurrentAttributes"": {
                    ""type"": ""array"", ""items"": { ""anyOf"": [
                        { ""type"": ""string"", ""enum"": [ ""TestMethod"", ""TestMethod1"" ] },
                        { ""$ref"": ""#/definitions/TestMethod_4"" }
                    ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""TestWithFrameworkExtensions"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""TestOnDeviceWithSomeFile"", ""TestThatIsNowInDisarray"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""TestWithMethods"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""Test"", ""Test2"" ] },
                    ""description"": ""Specify which test methods should be included."",
                },
                ""TestWithNewTestMethodsAttributes"": {
                    ""type"": ""array"", ""items"":
                        { ""type"": ""string"", ""enum"": [ ""MethodWithNewTestMethods"", ""MethodWithTraits"" ] },
                    ""description"": ""Specify which test methods should be included."",
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
        public void DebugTestCases_Selection_AnyDevice()
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
$@"Error: {Path.GetDirectoryName(projectFilePath2)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]' or 'string'.
Error: {Path.GetDirectoryName(projectFilePath2)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.",
                LoggingLevel.Error);
            #endregion

            var selection = DebugTestCasesSpecification.Parse(
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
        {
          ""TestMethod1"": [ 0 ]
        }
      ]
    }
  }
}");

            var actual = selection.SelectTestCases(testCases).ToList();

            Assert.AreEqual(3, actual.Count);

            Assert.AreEqual(
@"TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod DR=-1
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=0
".Replace("\r\n", "\n"),
                string.Join("\n", from tc in
                                    (from s in actual
                                     where s.AssemblyFilePath.Contains("Discovery.v2")
                                     select s.TestCases).First()
                                  orderby tc.testCase.FullyQualifiedName, tc.testCase.DataRowIndex
                                  select $"{tc.testCase.FullyQualifiedName} DR={tc.testCase.DataRowIndex}"
                ) + '\n'
            );

            Assert.AreEqual(
@"TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method DR=-1
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod DR=-1
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=0
".Replace("\r\n", "\n"),
                string.Join("\n", from tc in
                                    (from s in actual
                                     where s.AssemblyFilePath.Contains("Discovery.v3")
                                     select s.TestCases).First()
                                  orderby tc.testCase.FullyQualifiedName, tc.testCase.DataRowIndex
                                  select $"{tc.testCase.FullyQualifiedName} DR={tc.testCase.DataRowIndex}"
                ) + '\n'
            );

            Assert.AreEqual(
@"TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile DR=-1
TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1 DR=0
TestFramework.Tooling.Execution.Tests.TestWithMethods.Test1 DR=1
TestFramework.Tooling.Execution.Tests.TestWithMethods.Test2 DR=-1
TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware DR=-1
".Replace("\r\n", "\n"),
                string.Join("\n", from tc in
                                    (from s in actual
                                     where s.AssemblyFilePath.Contains("Execution.v3")
                                     select s.TestCases).First()
                                  orderby tc.testCase.FullyQualifiedName, tc.testCase.DataRowIndex
                                  select $"{tc.testCase.FullyQualifiedName} DR={tc.testCase.DataRowIndex}"
                ) + '\n'
            );
        }

        [TestMethod]
        public void DebugTestCases_Selection_RealHardware()
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
$@"Error: {Path.GetDirectoryName(projectFilePath2)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]' or 'string'.
Error: {Path.GetDirectoryName(projectFilePath2)}{Path.DirectorySeparatorChar}TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.",
                LoggingLevel.Error);
            #endregion

            var selection = DebugTestCasesSpecification.Parse(
@"{
  ""ToBeRunOnRealHardware"": true,
  ""ToBeRunOnVirtualDevice"": false,
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
        {
          ""TestMethod1"": [ 0 ]
        }
      ]
    }
  }
}");

            var actual = selection.SelectTestCases(testCases).ToList();

            Assert.AreEqual(3, actual.Count);

            Assert.AreEqual(
@"TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod DR=-1
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=0
".Replace("\r\n", "\n"),
                string.Join("\n", from tc in
                                    (from s in actual
                                     where s.AssemblyFilePath.Contains("Discovery.v2")
                                     select s.TestCases).First()
                                  orderby tc.testCase.FullyQualifiedName, tc.testCase.DataRowIndex
                                  select $"{tc.testCase.FullyQualifiedName} DR={tc.testCase.DataRowIndex}"
                ) + '\n'
            );

            Assert.AreEqual(
@"TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method DR=-1
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod DR=-1
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 DR=0
".Replace("\r\n", "\n"),
                string.Join("\n", from tc in
                                    (from s in actual
                                     where s.AssemblyFilePath.Contains("Discovery.v3")
                                     select s.TestCases).First()
                                  orderby tc.testCase.FullyQualifiedName, tc.testCase.DataRowIndex
                                  select $"{tc.testCase.FullyQualifiedName} DR={tc.testCase.DataRowIndex}"
                ) + '\n'
            );

            Assert.AreEqual(
@"TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile DR=-1
TestFramework.Tooling.Execution.Tests.TestWithNewTestMethodsAttributes.MethodToRunOnRealHardware DR=-1
".Replace("\r\n", "\n"),
                string.Join("\n", from tc in
                                    (from s in actual
                                     where s.AssemblyFilePath.Contains("Execution.v3")
                                     select s.TestCases).First()
                                  orderby tc.testCase.FullyQualifiedName, tc.testCase.DataRowIndex
                                  select $"{tc.testCase.FullyQualifiedName} DR={tc.testCase.DataRowIndex}"
                ) + '\n'
            );
        }
        #endregion
    }
}
