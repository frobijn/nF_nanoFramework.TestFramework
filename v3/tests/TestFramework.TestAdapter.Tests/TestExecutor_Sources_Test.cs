// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.TestAdapter;
using nanoFramework.TestFramework.Tooling;
using TestFramework.TestAdapter.Tests.Helpers;

using nfTest = nanoFramework.TestFramework;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace TestFramework.TestAdapter.Tests
{
    /// <summary>
    /// The purpose of these tests is to verify that the implementation of the interface
    /// exposed to Visual Studio/VSTest is working correctly. This is a smoke test;
    /// a fully functional test is available in <c>TestAdapterRunTests_Assemblies_Test</c>
    /// and the tests for <c>TestsRunner</c>.
    /// These tests use both the test host and a Virtual nanoDevice as external processes.
    /// </summary>
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    public sealed class TestExecutor_Sources_Test
    {
        [TestMethod]
        [TestCategory(nfTest.Constants.VirtualDevice_TestCategory)]
        [TestCategory("@Test host")]
        public void TestAdapter_ITestExecutor_Sources_NoFilter()
        {
            // Setup
            TestResultCollection testResults = GetTestAssemblies("TestFramework.Tooling.Tests.Execution.v3");

            // Test
            var actual = new TestExecutor();
            actual.RunTests(testResults.Sources, testResults, testResults);

            // Asserts
            testResults.Logger.AssertEqual("", TestMessageLevel.Warning);

            // We do not need to assert the proper working of TestsRunner etc.
            // Just make sure test results have been received for all test cases.
            foreach (TestCase testCase in testResults.ExpectedTestCases)
            {
                List<TestResult> results = (from tr in testResults.TestResults
                                            where tr.Key.FullyQualifiedName == testCase.FullyQualifiedName
                                               && tr.Key.DisplayName == testCase.DisplayName
                                            select tr.Value).FirstOrDefault();

                Assert.AreEqual(1, results?.Count, $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
            }
        }

        [TestMethod]
        [TestCategory(nfTest.Constants.VirtualDevice_TestCategory)]
        [TestCategory("@Test host")]
        public void TestAdapter_ITestExecutor_Sources_Filter()
        {
            // Setup
            TestResultCollection testResults = GetTestAssemblies("TestFramework.Tooling.Tests.Execution.v3");
            string fqn = "TestFramework.Tooling.Execution.Tests.StaticTestClass.Method1";
            testResults.FilterTerms["FullyQualifiedName"] = fqn;
            string name = "TestDeviceWithSomeFile";
            testResults.FilterTerms["Name"] = name;
            string className = "TestFramework.Tooling.Execution.Tests.TestClassWithMultipleSetupCleanup";
            testResults.FilterTerms["ClassName"] = className;
            string displayName = "Test1(42)";
            testResults.FilterTerms["DisplayName"] = displayName;
            string category = "@ESP32";
            testResults.FilterTerms["TestCategory"] = category;

            // Test
            var actual = new TestExecutor();
            actual.RunTests(testResults.Sources, testResults, testResults);

            // Asserts
            testResults.Logger.AssertEqual("", TestMessageLevel.Warning);

            // We do not need to assert the proper working of TestsRunner etc.
            // Just make sure test results have been received for all selected test cases.
            var notSelectedBy = new HashSet<string>(testResults.FilterTerms.Keys);
            foreach (TestCase testCase in testResults.ExpectedTestCases)
            {
                string expectedBy = null;
                if (testCase.FullyQualifiedName == fqn)
                {
                    expectedBy = "FullyQualifiedName";
                }
                else if (testCase.FullyQualifiedName.EndsWith($".{name}"))
                {
                    expectedBy = "Name";
                }
                else if (testCase.FullyQualifiedName.StartsWith($"{className}."))
                {
                    expectedBy = "ClassName";
                }
                else if (testCase.DisplayName == displayName)
                {
                    expectedBy = "DisplayName";
                }
                else if ((from t in testCase.Traits
                          where t.Name == category
                          select t).Any())
                {
                    expectedBy = "TestCategory";
                }
                if (!(expectedBy is null))
                {
                    notSelectedBy.Remove(expectedBy);
                }

                List<TestResult> results = (from tr in testResults.TestResults
                                            where tr.Key.FullyQualifiedName == testCase.FullyQualifiedName
                                               && tr.Key.DisplayName == testCase.DisplayName
                                            select tr.Value).FirstOrDefault();

                Assert.AreEqual(expectedBy is null ? 0 : 1, results?.Count ?? 0, $"{expectedBy} / {testCase.FullyQualifiedName} - {testCase.DisplayName}");
            }
            if (notSelectedBy.Count > 0)
            {
                Assert.Inconclusive($"No test cases selected by: {string.Join(", ", notSelectedBy)}");
            }
        }

        [TestMethod]
        public void TestAdapter_ITestExecutor_Sources_IsBeingDebugged()
        {
            // Setup
            var testResults = new TestResultCollection();
            testResults.Sources.Add("some.dll");
            testResults.IsBeingDebugged = true;

            // Test
            var actual = new TestExecutor();
            actual.RunTests(testResults.Sources, testResults, testResults);

            // Asserts
            testResults.Logger.AssertEqual(
                "Error: Debugging tests is not supported. Use a Debug Test Project instead.",
                TestMessageLevel.Warning
            );
            Assert.AreEqual(0, testResults.TestResults.Count);
        }

        #region Helpers
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Collector of test results.
        /// </summary>
        private sealed class TestResultCollection : IRunContext, IFrameworkHandle, ITestCaseFilterExpression
        {
            #region Test assemblies
            public List<string> Sources
            {
                get;
            } = new List<string>();

            public List<TestCase> ExpectedTestCases
            {
                get; set;
            }
            #endregion

            #region Filter
            public Dictionary<string, string> FilterTerms
            {
                get;
            } = new Dictionary<string, string>();

            public string TestCaseFilterValue
                => string.Join("|", from t in FilterTerms
                                    select $"{t.Key}={t.Value}");

            /// <summary>
            /// Matched test case with test case filtering criteria.
            /// </summary>
            bool ITestCaseFilterExpression.MatchTestCase(TestCase testCase, Func<string, object> propertyValueProvider)
            {
                if (testCase is null)
                {
                    (Logger as IMessageLogger).SendMessage(TestMessageLevel.Error, $"{nameof(testCase)} is null in {nameof(ITestCaseFilterExpression.MatchTestCase)}");
                }
                if (propertyValueProvider is null)
                {
                    (Logger as IMessageLogger).SendMessage(TestMessageLevel.Error, $"{nameof(propertyValueProvider)} is null in {nameof(ITestCaseFilterExpression.MatchTestCase)}");
                    return false;
                }
                foreach (KeyValuePair<string, string> term in FilterTerms)
                {
                    object value = propertyValueProvider(term.Key);
                    if (!(value is null))
                    {
                        if (value is string stringValue)
                        {
                            if (term.Value == stringValue)
                            {
                                return true;
                            }
                        }
                        else if (value is string[] stringArray)
                        {
                            if (stringArray.Contains(term.Value))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            (Logger as IMessageLogger).SendMessage(TestMessageLevel.Error, $"{nameof(propertyValueProvider)}({term.Key}) returns unsupported type {value.GetType().FullName}");
                        }
                    }
                }
                return false;
            }

            ITestCaseFilterExpression IRunContext.GetTestCaseFilter(IEnumerable<string> supportedProperties, Func<string, TestProperty> propertyProvider)
                    => FilterTerms.Count == 0 ? null : this;
            #endregion

            #region Logged messages
            public MessageLoggerMock Logger
            {
                get;
            } = new MessageLoggerMock();

            void IMessageLogger.SendMessage(TestMessageLevel testMessageLevel, string message)
            {
                (Logger as IMessageLogger).SendMessage(testMessageLevel, message);
            }
            #endregion

            #region Test results
            public Dictionary<TestCase, List<TestResult>> TestResults
            {
                get;
            } = new Dictionary<TestCase, List<TestResult>>();

            void ITestExecutionRecorder.RecordResult(TestResult testResult)
            {
                lock (TestResults)
                {
                    if (!TestResults.TryGetValue(testResult.TestCase, out List<TestResult> list))
                    {
                        TestResults[testResult.TestCase] = list = new List<TestResult>();
                    }
                    list.Add(testResult);
                }
            }
            #endregion

            #region Test support
            public bool IsBeingDebugged
            {
                get; set;
            }
            #endregion

            #region Unsupported IRunContext properties
            bool IRunContext.KeepAlive => throw new NotImplementedException();

            bool IRunContext.InIsolation => throw new NotImplementedException();

            bool IRunContext.IsDataCollectionEnabled => throw new NotImplementedException();

            string IRunContext.TestRunDirectory => throw new NotImplementedException();

            string IRunContext.SolutionDirectory => throw new NotImplementedException();

            IRunSettings IDiscoveryContext.RunSettings => throw new NotImplementedException();
            #endregion

            #region Unsupported IFrameworkHandle properties and methods
            int IFrameworkHandle.LaunchProcessWithDebuggerAttached(string filePath, string workingDirectory, string arguments, IDictionary<string, string> environmentVariables) => throw new NotImplementedException();
            void ITestExecutionRecorder.RecordStart(TestCase testCase) => throw new NotImplementedException();
            void ITestExecutionRecorder.RecordEnd(TestCase testCase, TestOutcome outcome) => throw new NotImplementedException();
            void ITestExecutionRecorder.RecordAttachments(IList<AttachmentSet> attachmentSets) => throw new NotImplementedException();
            bool IFrameworkHandle.EnableShutdownAfterTestRun { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            #endregion
        }

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

        private TestResultCollection GetTestAssemblies(params string[] projectNames)
        {
            var results = new TestResultCollection();

            #region Copy the assembles and project files and get all test cases
            // ... as the test needs a copy of the project structure to create the unit test launcher and custom test framework configurations.
            string testDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);

            foreach (string projectName in projectNames)
            {
                string projectDirectoryPath = Path.Combine(testDirectoryPath, projectName);

                var configuration = new TestFrameworkConfiguration()
                {
                    AllowRealHardware = false
                };
                configuration.SaveEffectiveSettings(projectDirectoryPath, null);

                results.Sources.Add((from a in AssemblyHelper.CopyAssembliesAndProjectFile(projectDirectoryPath, "bin", projectName)
                                     where Path.GetFileNameWithoutExtension(a) == projectName || Path.GetFileNameWithoutExtension(a) == "NFUnitTest"
                                     select Path.ChangeExtension(a, ".dll")).First());
            }
            #endregion

            #region Run the discovery method to get all test cases
            var logger = new MessageLoggerMock();
            var sink = new TestCaseDiscoverySinkMock();
            var discovery = new TestDiscoverer();
            discovery.DiscoverTests(
                results.Sources,
                new DiscoveryContextMock(),
                logger,
                sink
            );
            results.ExpectedTestCases = sink.TestCases;
            #endregion

            if (results.ExpectedTestCases.Count == 0)
            {
                Assert.Inconclusive("There are no test cases in the assemblies!");
            }

            return results;
        }
        #endregion
    }
}
