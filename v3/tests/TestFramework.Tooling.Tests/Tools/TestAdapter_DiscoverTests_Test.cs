// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests.Tools
{
    [TestClass]
    [TestCategory("Test host")]
    [TestCategory("Test cases")]
    public sealed class TestAdapter_DiscoverTests_Test
    {
        [TestMethod]
        public void TestAdapter_DiscoverTests_Discovery_v3()
        {
            #region Setup
            string projectFilePath = TestProjectHelper.FindProjectFilePath("TestFramework.Tooling.Tests.Discovery.v3");
            string assemblyFilePath = TestProjectHelper.FindNFUnitTestAssembly(projectFilePath);
            var messagesSent = new List<InterProcessCommunicator.IMessage>();
            void sendMessage(InterProcessCommunicator.IMessage message)
            {
                lock (messagesSent)
                {
                    messagesSent.Add(message);
                }
            }
            var logger = new LogMessengerMock();
            #endregion

            #region Run the discovery method
            new TestAdapter().DiscoverTests(new TestDiscoverer_Parameters()
            {
                AssemblyFilePaths = new List<string>()
                {
                    assemblyFilePath
                }
            }, sendMessage, logger);
            #endregion

            #region Assert the messagages to the test host
            logger.AssertEqual(
$@"Error: {Path.GetDirectoryName(projectFilePath)}\TestWithALotOfErrors.cs(13,17): Error: An argument of the method must be of type 'byte[]', 'int', 'long' or 'string'.
Error: {Path.GetDirectoryName(projectFilePath)}\TestWithALotOfErrors.cs(25,10): Error: A cleanup method cannot have an attribute that implements 'IDeploymentConfiguration' - the attribute is ignored.
Error: {Path.GetDirectoryName(projectFilePath)}\TestWithALotOfErrors.cs(55,10): Error: The number of arguments of the method does not match the number of configuration keys specified by the attribute that implements 'IDeploymentConfiguration'.", LoggingLevel.Error);

            var testCases = new List<string>();
            var propertyPassed = new HashSet<string>()
            {
                nameof (TestDiscoverer_DiscoveredTests.TestCase.CodeFilePath),
                nameof (TestDiscoverer_DiscoveredTests.TestCase.DisplayName),
                nameof (TestDiscoverer_DiscoveredTests.TestCase.FullyQualifiedName),
                nameof (TestDiscoverer_DiscoveredTests.TestCase.LineNumber),
                nameof (TestDiscoverer_DiscoveredTests.TestCase.Categories)
            };
            foreach (InterProcessCommunicator.IMessage message in messagesSent)
            {
                if (message is TestDiscoverer_DiscoveredTests discovered)
                {
                    Assert.AreEqual(assemblyFilePath, discovered.Source);
                    foreach (TestDiscoverer_DiscoveredTests.TestCase testCase in discovered.TestCases)
                    {
                        if (!(testCase.CodeFilePath is null))
                        {
                            propertyPassed.Remove(nameof(testCase.CodeFilePath));
                        }
                        if (!(testCase.DisplayName is null))
                        {
                            propertyPassed.Remove(nameof(testCase.DisplayName));
                        }
                        if (!(testCase.FullyQualifiedName is null))
                        {
                            propertyPassed.Remove(nameof(testCase.FullyQualifiedName));
                        }
                        if (!(testCase.LineNumber is null))
                        {
                            propertyPassed.Remove(nameof(testCase.LineNumber));
                        }
                        if ((testCase.Categories?.Count ?? 0) > 0)
                        {
                            propertyPassed.Remove(nameof(testCase.Categories));
                        }
                        testCases.Add($"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                    }
                }
                else
                {
                    Assert.Fail($"Unexpected message of type {message.GetType()}");
                }
            }
            if (propertyPassed.Count > 0)
            {
                Assert.Fail($"No data passed for property: {string.Join(", ", propertyPassed)}");
            }
            testCases.Sort();
            Assert.AreEqual(
$@"TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1(H) - Method1 [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1(V) - Method1 [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2(H) - Method2 [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method2(V) - Method2 [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method(H) - Method [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.StaticTestClass.Method(V) - Method [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(H) - TestMethod [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod(V) - TestMethod [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,H) - TestMethod1(1,1) [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(0,V) - TestMethod1(1,1) [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,H) - TestMethod1(2,2) [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestAllCurrentAttributes.TestMethod1(1,V) - TestMethod1(2,2) [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile(H) - TestOnDeviceWithSomeFile [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestOnDeviceWithSomeFile(V) - TestOnDeviceWithSomeFile [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray(H) - TestThatIsNowInDisarray [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithFrameworkExtensions.TestThatIsNowInDisarray(V) - TestThatIsNowInDisarray [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(H) - Test [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test(V) - Test [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(H) - Test2 [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithMethods.Test2(V) - Test2 [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(H) - MethodWithCategories [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithCategories(V) - MethodWithCategories [{Constants.VirtualDevice_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods(H) - MethodWithNewTestMethods [{Constants.RealHardware_Description}]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods(V) - MethodWithNewTestMethods [{Constants.VirtualDevice_Description}]
".Trim().Replace("\r\n", "\n") + '\n',
                string.Join("\n", testCases) + '\n'
            );
            #endregion

        }
    }
}
