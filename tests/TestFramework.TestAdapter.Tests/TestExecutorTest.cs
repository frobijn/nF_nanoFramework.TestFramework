// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.TestAdapter;
using nanoFramework.TestFramework.Tooling;
using TestFramework.TestAdapter.Tests.Helpers;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace TestFramework.TestAdapter.Tests
{
    /// <summary>
    /// These test only runs tests on the Virtual Device.
    /// A fully functional test is available in <c>TestAdapterTestCasesExecutorTest</c>
    /// and the tests for <c>TestsRunner</c>.
    /// </summary>
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    public sealed class TestExecutorTest
    {
        [TestMethod]
        public void TestExecutor_TestCases()
        {
            // Setup
            TestResultCollection testResults = GetTestCases("TestFramework.Tooling.Tests.Execution.v3");

            // Test
            var actual = new TestExecutor();
            actual.RunTests(testResults.TestCases, testResults, testResults);

            // Asserts
            testResults.Logger.AssertEqual("", TestMessageLevel.Warning);

            // We do not need to assert the proper working of TestsRunner etc.
            // Just make sure every test has a result.
            foreach (TestCase testCase in testResults.TestCases)
            {
                Assert.IsTrue(testResults.TestResults.ContainsKey(testCase), $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
                Assert.AreEqual(1, testResults.TestResults[testCase].Count);

                if (testCase.FullyQualifiedName.StartsWith("TestFramework.Tooling.Execution.Tests.TestWithMethods."))
                {
                    // Just check that all properties are assigned
                    TestResult testResult = testResults.TestResults[testCase][0];
                    Assert.IsNotNull(testResult.ComputerName);
                    Assert.IsNotNull(testResult.DisplayName);
                    Assert.IsTrue(testResult.Duration.TotalMilliseconds > 0);
                    Assert.IsNull(testResult.ErrorMessage);
                    Assert.IsNotNull(testResult.Messages);
                    Assert.AreNotEqual(0, testResult.Messages.Count);
                    Assert.AreEqual(TestOutcome.Passed, testResult.Outcome);
                    Assert.AreEqual(testCase, testResult.TestCase);
                }
                else if (testCase.FullyQualifiedName == "TestFramework.Tooling.Execution.Tests.TestWithFrameworkExtensions.TestDeviceWithSomeFile")
                {
                    // This method is is not run (real hardware) or fails (virtual machine) due to missing deployment configuration
                    TestResult testResult = testResults.TestResults[testCase][0];
                    Assert.IsNotNull(testResult.ErrorMessage);
                }
            }
        }

        [TestMethod]
        public void TestExecutor_TestCases_Cancel()
        {
            // Setup
            TestResultCollection testResults = GetTestCases("TestFramework.Tooling.Tests.Execution.v3");

            // Test
            var actual = new TestExecutor();
            var run = Task.Run(() =>
            {
                actual.RunTests(testResults.TestCases, testResults, testResults);
            });
            while (!actual.IsCancelled)
            {
                actual.Cancel();
            }
            run.GetAwaiter().GetResult();

            // Asserts
            testResults.Logger.AssertEqual("", TestMessageLevel.Warning);

            // Assumption: the test runner should be cancelled before the Virtual Device has had an opportunity to run.
            Assert.IsTrue(testResults.TestResults.Count < testResults.TestCases.Count);
            // There are only results fo skipped hardware files.
            Assert.IsFalse((from tc in testResults.TestResults
                            where (from tr in tc.Value
                                   where tr.Outcome != TestOutcome.Skipped
                                   select tr).Any()
                            select tc).Any());
        }

        [TestMethod]
        public void TestExecutor_TestCases_IsBeingDebugged()
        {
            // Setup
            TestResultCollection testResults = GetTestCases("TestFramework.Tooling.Tests.Execution.v3");
            testResults.IsBeingDebugged = true;

            // Test
            var actual = new TestExecutor();
            actual.RunTests(testResults.TestCases, testResults, testResults);

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
        private sealed class TestResultCollection : ITestCaseDiscoverySink, IRunContext, IFrameworkHandle
        {
            #region Test cases
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
            public ITestCaseFilterExpression GetTestCaseFilter(IEnumerable<string> supportedProperties, Func<string, TestProperty> propertyProvider)
                => throw new NotImplementedException();

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

        private TestResultCollection GetTestCases(params string[] projectNames)
        {
            #region Copy the assembles and project files and get all test cases
            // ... as the test needs a copy of the project structure to create the unit test launcher and custom test framework configurations.
            string testDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);

            var testAssemblies = new List<string>();
            foreach (string projectName in projectNames)
            {
                string projectDirectoryPath = Path.Combine(testDirectoryPath, projectName);

                var configuration = new TestFrameworkConfiguration()
                {
                    AllowRealHardware = false
                };
                configuration.SaveEffectiveSettings(projectDirectoryPath, null);

                testAssemblies.Add((from a in AssemblyHelper.CopyAssembliesAndProjectFile(projectDirectoryPath, "bin", projectName)
                                    where Path.GetFileNameWithoutExtension(a) == projectName || Path.GetFileNameWithoutExtension(a) == "NFUnitTest"
                                    select Path.ChangeExtension(a, ".dll")).First());
            }
            #endregion

            #region Get the test cases
            var results = new TestResultCollection();
            var setupLogger = new MessageLoggerMock();
            var actual = new TestDiscoverer();
            actual.DiscoverTests(
                testAssemblies,
                new DiscoveryContextMock(),
                setupLogger,
                results
            );
            #endregion

            if (results.TestCases.Count == 0)
            {
                Assert.Inconclusive("Could not get the test cases");
            }

            return results;
        }
        #endregion
    }
}
