// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests.Tools
{
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    [TestCategory("Test cases")]
    public sealed class TestAdapterDiscovererTest
    {
        [TestMethod]
        public void TestAdapterDiscoverer_Discovery_v3()
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
            TestAdapterDiscoverer.Run(new TestDiscoverer_Parameters()
            {
                Sources = new List<string>()
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
                nameof (TestDiscoverer_DiscoveredTests.TestCase.Traits)
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
                        if ((testCase.Traits?.Count ?? 0) > 0)
                        {
                            propertyPassed.Remove(nameof(testCase.Traits));
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
@"TestFramework.Tooling.Tests.NFUnitTest.NonStaticTestClass.Method1 - Method1 [Real hardware]
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
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods - MethodWithNewTestMethods [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithNewTestMethods - MethodWithNewTestMethods [Virtual Device]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits - MethodWithTraits [Real hardware]
TestFramework.Tooling.Tests.NFUnitTest.TestWithNewTestMethodsAttributes.MethodWithTraits - MethodWithTraits [Virtual Device]
".Trim().Replace("\r\n", "\n") + '\n',
                string.Join("\n", testCases) + '\n'
            );
            #endregion

        }
    }
}
