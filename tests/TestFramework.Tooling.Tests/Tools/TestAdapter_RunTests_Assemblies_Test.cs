// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests.Tools
{
    [TestClass]
    [TestCategory("Test host")]
    public sealed class TestAdapter_RunTests_Assemblies_Test
    {
        /// <summary>
        /// The purpose of this test is to verify that the sequence of calls is working correctly:
        /// <see cref="TestAdapter.RunTests(TestExecutor_Sources_Parameters, System.Action{InterProcessCommunicator.IMessage}, LogMessenger)"/>
        /// followed by <see cref="TestAdapter.RunAllTests(CancellationToken)"/>.
        /// For tests run on the virtual device.
        /// The correct working of running tests on a Virtual Device is already tested
        /// by <see cref="TestsRunnerExecutionTest"/>, and the correct working of sending test results
        /// from the test host to the adapter in <see cref="TestAdapter_RunTests_TestCases_Test.TestAdapter_RunTests_TestCases_VirtualDeviceOnly(bool)"/>.
        /// </summary>
        [TestMethod]
        [TestCategory(Constants.VirtualDevice_TestCategory)]
        public void TestAdapter_RunTests_Assemblies_VirtualDeviceOnly_AllTestCases()
        {
            #region Set up the configurations and create the parameters
            var configuration = new TestFrameworkConfiguration()
            {
                AllowRealHardware = false
            };
            (TestExecutor_Sources_Parameters parameters, string testDirectoryPath, TestResultCollection testResults) = CreateParameters(
                ("TestFramework.Tooling.Tests.Execution.v3", configuration)
            );
            #endregion

            var logger = new LogMessengerMock();

            using (var cancellationToken = new CancellationTokenSource())
            {
                using (var waitForAllTestCases = new CancellationTokenSource())
                {
                    #region Execute TestAdapter().RunTests(Assemblies)
                    var testAdapter = new TestAdapter();
                    testAdapter.RunTests(
                        parameters,
                        (message) =>
                        {
                            lock (testResults)
                            {
                                if (message is TestDiscoverer_DiscoveredTests discoveredTests)
                                {
                                    if (testResults.AllTestCasesPresent(discoveredTests))
                                    {
                                        waitForAllTestCases.Cancel();
                                    }
                                }
                                else if (message is TestExecutor_Sources_Done total)
                                {
                                    if (testResults.AllTestCasesPresent(total, logger))
                                    {
                                        waitForAllTestCases.Cancel();
                                    }
                                }
                                else if (message is TestExecutor_TestResults results)
                                {
                                    testResults.AddTestResults(results, logger);
                                }
                                else
                                {
                                    ((LogMessenger)logger)(LoggingLevel.Error, $"Unexpected message type: {message.GetType()}");
                                }
                            }
                        },
                        logger);

                    waitForAllTestCases.Token.WaitHandle.WaitOne();
                    #endregion

                    #region Asserts for RunTests(Assemblies)
                    Assert.AreEqual(
                        string.Join("", from t in testResults.ExpectedTestCases
                                        orderby t.FullyQualifiedName, t.DisplayName
                                        select $"{t.FullyQualifiedName} - {t.DisplayName} - {t.AssemblyFilePath}\n"),
                        string.Join("", from t in testResults.ReceivedTestCases
                                        orderby t.fqn, t.displayName
                                        select $"{t.fqn} - {t.displayName} - {t.assemblyFilePath}\n")
                    );
                    #endregion

                    #region Run all tests
                    testResults.SelectedTestCases.AddRange(testResults.ExpectedTestCases);

                    testAdapter.RunAllTests(cancellationToken.Token);
                    #endregion
                }
            }

            #region Asserts for RunAllTests
            logger.AssertEqual("", LoggingLevel.Warning);

            // We do not need to assert the proper working of TestsRunner
            // or its implementation in TestAdapter.
            // Just make sure all test cases have been used in the test execution and have a test result.
            foreach (TestCase testCase in testResults.ExpectedTestCases)
            {
                Assert.IsTrue(testResults.TestResults.ContainsKey(testCase), $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
            }
            #endregion
        }

        /// <summary>
        /// The purpose of this test is to verify that the sequence of calls is working correctly:
        /// <see cref="TestAdapter.RunTests(TestExecutor_Sources_Parameters, System.Action{InterProcessCommunicator.IMessage}, LogMessenger)"/>
        /// followed by <see cref="TestAdapter.RunTests(TestExecutor_TestCases_Parameters, System.Action{InterProcessCommunicator.IMessage}, LogMessenger, CancellationToken)"/>.
        /// For tests run on the virtual device.
        /// The correct working of running tests on a Virtual Device is already tested
        /// by <see cref="TestsRunnerExecutionTest"/>, and the correct working of sending test results
        /// from the test host to the adapter in <see cref="TestAdapter_RunTests_TestCases_Test.TestAdapter_RunTests_TestCases_VirtualDeviceOnly(bool)"/>.
        /// </summary>
        [TestMethod]
        [TestCategory(Constants.VirtualDevice_TestCategory)]
        public void TestAdapter_RunTests_Assemblies_VirtualDeviceOnly_SelectedTestCases()
        {
            #region Set up the configurations and create the parameters
            var configuration = new TestFrameworkConfiguration()
            {
                AllowRealHardware = false
            };
            (TestExecutor_Sources_Parameters parameters, string testDirectoryPath, TestResultCollection testResults) = CreateParameters(
                ("TestFramework.Tooling.Tests.Execution.v3", configuration)
            );
            #endregion

            var logger = new LogMessengerMock();

            bool includeTestCase(string displayName)
            {
                return displayName.Contains(Constants.VirtualDevice_Description)
                    && displayName.Contains("Method");
            }

            using (var cancellationToken = new CancellationTokenSource())
            {
                using (var waitForAllTestCases = new CancellationTokenSource())
                {
                    #region Execute TestAdapter().RunTests(Assemblies)
                    var testAdapter = new TestAdapter();
                    testAdapter.RunTests(
                        parameters,
                        (message) =>
                        {
                            lock (testResults)
                            {
                                if (message is TestDiscoverer_DiscoveredTests discoveredTests)
                                {
                                    if (testResults.AllTestCasesPresent(discoveredTests))
                                    {
                                        waitForAllTestCases.Cancel();
                                    }
                                }
                                else if (message is TestExecutor_Sources_Done total)
                                {
                                    if (testResults.AllTestCasesPresent(total, logger))
                                    {
                                        waitForAllTestCases.Cancel();
                                    }
                                }
                                else
                                {
                                    ((LogMessenger)logger)(LoggingLevel.Error, $"Unexpected message type: {message.GetType()}");
                                }
                            }
                        },
                        logger);

                    waitForAllTestCases.Token.WaitHandle.WaitOne();
                    #endregion

                    #region Asserts for RunTests(Assemblies)
                    Assert.AreEqual(
                        string.Join("", from t in testResults.ExpectedTestCases
                                        orderby t.FullyQualifiedName, t.DisplayName
                                        select $"{t.FullyQualifiedName} - {t.DisplayName} - {t.AssemblyFilePath}\n"),
                        string.Join("", from t in testResults.ReceivedTestCases
                                        orderby t.fqn, t.displayName
                                        select $"{t.fqn} - {t.displayName} - {t.assemblyFilePath}\n")
                    );
                    #endregion

                    #region Run a selection of tests
                    testResults.SelectedTestCases.AddRange(from tc in testResults.ExpectedTestCases
                                                           where includeTestCase(tc.DisplayName)
                                                           select tc);
                    if (testResults.SelectedTestCases.Count == 0)
                    {
                        Assert.Inconclusive("No test cases satisfy the selection criteria!");
                    }

                    testAdapter.RunTests(
                        new TestExecutor_TestCases_Parameters()
                        {
                            LogLevel = (int)LoggingLevel.Detailed,
                            TestCases = new List<TestExecutor_TestCases_Parameters.TestCase>(
                                from t in testResults.SelectedTestCases
                                select new TestExecutor_TestCases_Parameters.TestCase()
                                {
                                    AssemblyFilePath = t.AssemblyFilePath,
                                    DisplayName = t.DisplayName,
                                    FullyQualifiedName = t.FullyQualifiedName
                                }
                            )
                        },
                        (message) =>
                        {
                            lock (testResults)
                            {
                                if (message is TestExecutor_TestResults results)
                                {
                                    testResults.AddTestResults(results, logger);
                                }
                                else
                                {
                                    ((LogMessenger)logger)(LoggingLevel.Error, $"Unexpected message type: {message.GetType()}");
                                }
                            }
                        },
                        logger,
                        cancellationToken.Token
                    );
                    #endregion
                }
            }

            #region Asserts for RunAllTests
            logger.AssertEqual("", LoggingLevel.Warning);

            // We do not need to assert the proper working of TestsRunner
            // or its implementation in TestAdapter.
            // Just make sure only the selected test cases have a test result.
            foreach (TestCase testCase in testResults.ExpectedTestCases)
            {
                Assert.AreEqual(includeTestCase(testCase.DisplayName), testResults.TestResults.ContainsKey(testCase), $"{testCase.FullyQualifiedName} - {testCase.DisplayName}");
            }
            #endregion
        }

        #region Helpers
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Collector of test results.
        /// </summary>
        private sealed class TestResultCollection
        {
            public List<TestCase> ExpectedTestCases
            {
                get;
            } = new List<TestCase>();

            public List<(string assemblyFilePath, string fqn, string displayName)> ReceivedTestCases
            {
                get;
            } = new List<(string, string, string)>();


            public bool AllTestCasesPresent(TestDiscoverer_DiscoveredTests discoveredTests)
            {
                lock (this)
                {
                    ReceivedTestCases.AddRange(from tc in discoveredTests.TestCases
                                               select (discoveredTests.Source, tc.FullyQualifiedName, tc.DisplayName));
                    return ReceivedTestCases.Count == _expectedToReceive;
                }
            }

            public bool AllTestCasesPresent(TestExecutor_Sources_Done totalTestCases, LogMessenger logger)
            {
                lock (this)
                {
                    _expectedToReceive = totalTestCases.NumberOfTestCases;
                    if (_expectedToReceive != ExpectedTestCases.Count)
                    {
                        logger(LoggingLevel.Error, $"Expected to receive {ReceivedTestCases.Count} test cases, but {nameof(TestExecutor_Sources_Done)}.{nameof(TestExecutor_Sources_Done.NumberOfTestCases)} = {totalTestCases.NumberOfTestCases}");
                    }
                    return ReceivedTestCases.Count == _expectedToReceive;
                }
            }
            private int _expectedToReceive = -1;

            public List<TestCase> SelectedTestCases
            {
                get;
            } = new List<TestCase>();

            public Dictionary<TestCase, List<(string computerName, TestExecutor_TestResults.TestResult testResult)>> TestResults
            {
                get;
            } = new Dictionary<TestCase, List<(string, TestExecutor_TestResults.TestResult)>>();

            public void AddTestResults(TestExecutor_TestResults results, LogMessenger logger)
            {
                foreach (TestExecutor_TestResults.TestResult result in results.TestResults)
                {
                    if (result.Index < 0 || result.Index >= ReceivedTestCases.Count)
                    {
                        logger(LoggingLevel.Error, $"Invalid index {result.Index} for test result '{result.DisplayName}' => {result.ErrorMessage}");
                    }
                    else
                    {
                        TestCase testCase = SelectedTestCases[result.Index];
                        if (!TestResults.TryGetValue(testCase, out List<(string computerName, TestExecutor_TestResults.TestResult)> list))
                        {
                            TestResults[testCase] = list = new List<(string computerName, TestExecutor_TestResults.TestResult)>();
                        }
                        list.Add((results.ComputerName, result));
                    }
                }
            }
        }

        private (TestExecutor_Sources_Parameters parameters, string testDirectoryPath, TestResultCollection results) CreateParameters(params (string projectName, TestFrameworkConfiguration configuration)[] projectNameAndConfiguration)
        {
            #region Copy the assembles and project files and get all test cases
            // ... as the test needs a copy of the project structure to create the unit test launcher and custom test framework configurations.
            string testDirectoryPath = TestDirectoryHelper.GetTestDirectory(TestContext);

            var setupLogger = new LogMessengerMock();
            var testAssemblies = new List<string>();
            foreach ((string projectName, TestFrameworkConfiguration configuration) in projectNameAndConfiguration)
            {
                string projectDirectoryPath = Path.Combine(testDirectoryPath, projectName);

                (configuration ?? new TestFrameworkConfiguration()).SaveEffectiveSettings(projectDirectoryPath, setupLogger);

                testAssemblies.Add((from a in AssemblyHelper.CopyAssembliesAndProjectFile(projectDirectoryPath, "bin", projectName)
                                    where Path.GetFileNameWithoutExtension(a) == projectName
                                    select Path.ChangeExtension(a, ".dll")).First());
            }

            var testCases = new TestCaseCollection(testAssemblies, (a) => ProjectSourceInventory.FindProjectFilePath(a, setupLogger), setupLogger);
            setupLogger.AssertEqual("", LoggingLevel.Error);
            #endregion

            var results = new TestResultCollection();
            results.ExpectedTestCases.AddRange(testCases.TestCases);

            var parameters = new TestExecutor_Sources_Parameters()
            {
                LogLevel = (int)LoggingLevel.Detailed,
                AssemblyFilePaths = testAssemblies
            };
            return (parameters, testDirectoryPath, results);
        }
        #endregion
    }
}
