// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.TestAdapter;
using TestFramework.TestAdapter.Tests.Helpers;

namespace TestFramework.TestAdapter.Tests
{
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    public class TestDiscovererTest
    {
        [TestMethod]
        public void TestAdapter_TestDiscoverer_Discovery_v3()
        {
            #region Setup
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string projectDirectory = Path.GetDirectoryName(projectFilePath);
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            var logger = new MessageLoggerMock();
            var sink = new TestCaseDiscoverySinkMock();
            #endregion

            #region Run the test adapter method
            var actual = new TestDiscoverer();
            actual.DiscoverTests(
                new string[] { assemblyFilePath },
                new DiscoveryContextMock(),
                logger,
                sink
            );
            #endregion

            #region Asserts
            logger.AssertEqual($@"Error: {projectDirectory}\TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]', 'int', 'long' or 'string'.
Error: {projectDirectory}\TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.
Error: {projectDirectory}\TestWithALotOfErrors.cs(55,10): Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.",
                        TestMessageLevel.Error);

            Assert.AreEqual(
$@"TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 - Method1 [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 - Method1 [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 - Method2 [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2 - Method2 [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method - Method [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method - Method [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod - TestMethod [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod - TestMethod [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 - TestMethod1(1,1) [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 - TestMethod1(1,1) [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 - TestMethod1(2,2) [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1 - TestMethod1(2,2) [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile - TestOnDeviceWithSomeFile [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile - TestOnDeviceWithSomeFile [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray - TestThatIsNowInDisarray [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray - TestThatIsNowInDisarray [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test - Test [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test - Test [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 - Test2 [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2 - Test2 [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories - MethodWithCategories [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories - MethodWithCategories [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods - MethodWithNewTestMethods [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods - MethodWithNewTestMethods [Virtual Device]
".Trim().Replace("\r\n", "\n") + '\n',
                string.Join("\n", from tc in sink.TestCases
                                  orderby tc.FullyQualifiedName, tc.DisplayName
                                  select $"{tc.FullyQualifiedName} - {tc.DisplayName}"
                            ) + '\n'
            );
            #endregion

        }

        #region Helpers
        private sealed class TestCaseDiscoverySinkMock : ITestCaseDiscoverySink
        {
            public List<TestCase> TestCases
            {
                get;
            } = new List<TestCase>();

            void ITestCaseDiscoverySink.SendTestCase(TestCase discoveredTest)
            {
                lock (TestCases)
                {
                    TestCases.Add(discoveredTest);
                }
            }
        }
        #endregion
    }
}
