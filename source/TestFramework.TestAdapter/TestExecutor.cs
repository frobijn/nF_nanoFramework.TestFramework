// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;

namespace nanoFramework.TestFramework.TestAdapter
{
    [ExtensionUri(NanoExecutor)]
    public sealed class TestExecutor : ITestExecutor
    {
        #region Fields and constants
        /// <summary>
        /// Executor name for nanoFramework.
        /// Different than the v2 name to make sure Visual Studio does not confuse the two.
        /// </summary>
        public const string NanoExecutor = "https://nanoFramework.net/TestFramework/Executor";

        private TestHost _testHost;
        private static readonly Dictionary<string, Func<TestCase, object>> s_testCaseProperties = new Dictionary<string, Func<TestCase, object>>()
        {
            { "FullyQualifiedName", (tc) => tc.FullyQualifiedName },
            { "Name", (tc) => tc.FullyQualifiedName.Substring (tc.FullyQualifiedName.LastIndexOf ('.')+1) },
            { "ClassName", (tc) => tc.FullyQualifiedName.Substring (0, tc.FullyQualifiedName.LastIndexOf ('.')) },
            { "DisplayName", (tc) => tc.DisplayName },
            { "TestCategory", (tc) => (from t in tc.Traits
                                       select t.Name).ToArray ()},
        };
        #endregion

        #region Run all tests in the assemblies that pass the optional filter
        /// <inheritdoc/>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
#if DEBUG
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
#endif
            var logMessenger = new TestAdapterLogger(frameworkHandle);
            if (runContext.IsBeingDebugged)
            {
                logMessenger.LogMessage(LoggingLevel.Error, "Debugging tests is not supported. Use a Debug Test Project instead.");
                return;
            }

            // Find out if a filter is present
            // Do that here, to abort early if there is a problem with the filter.
            ITestCaseFilterExpression filter;
            try
            {
                filter = runContext.GetTestCaseFilter(s_testCaseProperties.Keys, (p) => null);
            }
            catch (TestPlatformException ex)
            {
                logMessenger.LogMessage(LoggingLevel.Error, $"No tests are executed because the test case filter has errors: {ex.Message}");
                return;
            }

            #region Discover all test cases
            var testCases = new List<TestCase>();
            var filterProperties = new HashSet<string>(s_testCaseProperties.Keys);

            int totalNumberOfTestCases = -1;
            var testSelection = new List<TestCase>();
            var waitForReceptionOfTestCases = new CancellationTokenSource();

            void ProcessSourcesDone(InterProcessCommunicator.IMessage message)
            {
                if (message is TestExecutor_Sources_Done done)
                {
                    totalNumberOfTestCases = done.NumberOfTestCases;
                }
                if (totalNumberOfTestCases == testCases.Count)
                {
                    waitForReceptionOfTestCases.Cancel();
                }
            }

            _testHost = TestHost.Start(
                new TestExecutor_Sources_Parameters()
                {
                    AssemblyFilePaths = sources.ToList(),
                },
                (m, l, c) =>
                {
                    TestDiscoverer.ProcessTestHostMessage(m, testCase =>
                    {
                        lock (testCases)
                        {
                            testCases.Add(testCase);
                            filterProperties.UnionWith(from t in testCase.Traits
                                                       select t.Name);
                        }
                    });
                    ProcessSourcesDone(m);
                    ProcessTestCaseResults(m, testSelection, frameworkHandle);
                },
                new TestAdapterLogger(frameworkHandle).LogMessage
            );

            waitForReceptionOfTestCases.Token.WaitHandle.WaitOne();
            #endregion

            #region Select the test cases to run, and start the execution of the selection
            try
            {
                // All possible traits/categories have to be added as potential property,
                // as the VSTest/dotnet test filter mechanism doesn't allow the test adapter
                // to return a range of values - just one. If a test has more categories,
                filter = runContext.GetTestCaseFilter(s_testCaseProperties.Keys, (p) => null);
            }
            catch (TestPlatformException ex)
            {
                logMessenger.LogMessage(LoggingLevel.Error, $"No tests are executed because the test case filter has errors: {ex.Message}");
                _testHost?.WaitUnitCompleted();
                return;
            }

            if (filter is null)
            {
                testSelection = testCases;
                _testHost.SendMessage(new TestExecutor_Sources_RunAll());
            }
            else
            {
                bool IncludeTestCase(TestCase testCase)
                {
                    return filter.MatchTestCase(testCase, (property) =>
                    {
                        if (s_testCaseProperties.TryGetValue(property, out Func<TestCase, object> getValue))
                        {
                            return getValue(testCase);
                        }
                        return null;
                    });
                }
                testSelection.AddRange(from tc in testCases
                                       where IncludeTestCase(tc)
                                       select tc);

                _testHost.SendMessage(CreateTestExecutorTestCasesParameters(testSelection));
            }
            #endregion

            _testHost?.WaitUnitCompleted();
        }
        #endregion

        #region Run a selection of tests
        /// <inheritdoc/>
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
#if DEBUG
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
#endif
            var logMessenger = new TestAdapterLogger(frameworkHandle);
            if (runContext.IsBeingDebugged)
            {
                logMessenger.LogMessage(LoggingLevel.Error, "Debugging tests is not supported. Use a Debug Test Project instead.");
                return;
            }

            var testSelection = tests.ToList();

            _testHost = TestHost.Start(
                CreateTestExecutorTestCasesParameters(testSelection),
                (m, l, c) => ProcessTestCaseResults(m, testSelection, frameworkHandle),
                new TestAdapterLogger(frameworkHandle).LogMessage
            );

            _testHost?.WaitUnitCompleted();
        }
        #endregion

        #region Cancel running the tests
        /// <summary>
        /// Indicates whether the test execution has been cancelled.
        /// For test purposes; not part of the <see cref="ITestExecutor"/> interface.
        /// </summary>
        public bool IsCancelled
        {
            get;
            private set;
        }

        public void Cancel()
        {
            if (!(_testHost is null) && !IsCancelled)
            {
                IsCancelled = true;
                _testHost.Cancel();
            }
        }
        #endregion

        #region Internal implementation
        private static TestExecutor_TestCases_Parameters CreateTestExecutorTestCasesParameters(List<TestCase> testSelection)
        {
            return new TestExecutor_TestCases_Parameters()
            {
                TestCases = new List<TestExecutor_TestCases_Parameters.TestCase>(from tc in testSelection
                                                                                 select new TestExecutor_TestCases_Parameters.TestCase()
                                                                                 {
                                                                                     AssemblyFilePath = tc.Source,
                                                                                     DisplayName = tc.DisplayName,
                                                                                     FullyQualifiedName = tc.FullyQualifiedName
                                                                                 }),
            };
        }

        private static void ProcessTestCaseResults(InterProcessCommunicator.IMessage message, List<TestCase> selection, ITestExecutionRecorder testRecorder)
        {
            if (message is TestExecutor_TestResults testResults)
            {
                if (testResults.TestResults is null)
                {
                    return;
                }

                foreach (TestExecutor_TestResults.TestResult testResultData in testResults.TestResults)
                {
                    if (testResultData.Index < 0 || testResultData.Index >= selection.Count)
                    {
                        continue;
                    }

                    var result = new TestResult(selection[testResultData.Index])
                    {
                        DisplayName = testResultData.DisplayName,
                        Duration = testResultData.Duration,
                        ErrorMessage = testResultData.ErrorMessage,
                        Outcome = (TestOutcome)testResultData.Outcome,
                        ComputerName = !testResultData.ForRealHardware.HasValue ? null
                                    : testResultData.ForRealHardware.Value
                                        ? Constants.RealHardware_Description
                                        : Constants.VirtualDevice_Description
                    };
                    if (!(testResultData.Messages is null))
                    {
                        foreach (string msg in testResultData.Messages)
                        {
                            result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, msg));
                        }
                    }
                    testRecorder.RecordResult(result);
                }
            }
        }
        #endregion

    }
}
