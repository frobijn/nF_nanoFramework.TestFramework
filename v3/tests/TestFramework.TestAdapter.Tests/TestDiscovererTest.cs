﻿// Licensed to the .NET Foundation under one or more agreements.
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

using nfTest = nanoFramework.TestFramework;

namespace TestFramework.TestAdapter.Tests
{
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    [TestCategory("@Test host")]
    public class TestDiscovererTest
    {
        [TestMethod]
        public void TestAdapter_ITestDiscoverer_Discovery_v3()
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
$@"TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1(H) - Method1 [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1(V) - Method1 [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2(H) - Method2 [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2(V) - Method2 [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method(H) - Method [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method(V) - Method [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(H) - TestMethod [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(V) - TestMethod [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,H) - TestMethod1(1,1) [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,V) - TestMethod1(1,1) [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,H) - TestMethod1(2,2) [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,V) - TestMethod1(2,2) [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile(H) - TestOnDeviceWithSomeFile [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile(V) - TestOnDeviceWithSomeFile [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray(H) - TestThatIsNowInDisarray [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray(V) - TestThatIsNowInDisarray [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(H) - Test [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(V) - Test [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(H) - Test2 [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(V) - Test2 [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(H) - MethodWithCategories [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(V) - MethodWithCategories [{nfTest.Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods(H) - MethodWithNewTestMethods [{nfTest.Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods(V) - MethodWithNewTestMethods [{nfTest.Constants.VirtualDevice_Description}]
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
