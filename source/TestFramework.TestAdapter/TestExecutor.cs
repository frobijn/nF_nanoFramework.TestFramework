// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
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
        #endregion

        #region Run all tests in the assemblies that pass the optional filter
        /// <inheritdoc/>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            if (runContext.IsBeingDebugged)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, $"Debugging tests is not supported. Use a Debug Test Project instead.");
                return;
            }
            // TODO: First discovery, then filter, then TestExecutor_TestCases_Parameters.
            // Filter: see xunit TestCaseFilter.cs
            throw new NotImplementedException();
        }
        #endregion

        #region Run a selection of tests
        /// <inheritdoc/>
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            if (runContext.IsBeingDebugged)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, $"Debugging tests is not supported. Use a Debug Test Project instead.");
                return;
            }

            var testSelection = tests.ToList();
            int selectionIndex = 0;

            var logMessenger = new TestAdapterLogger(frameworkHandle);
            _testHost = TestHost.Start(
                new TestExecutor_TestCases_Parameters()
                {
                    TestCases = (from tc in testSelection
                                 select (tc.Source, new TestExecutor_TestCases_Parameters.TestCase()
                                 {
                                     FullyQualifiedName = tc.FullyQualifiedName,
                                     DisplayName = tc.DisplayName,
                                     Index = selectionIndex++
                                 }))
                                 .GroupBy((tc) => tc.Source)
                                 .ToDictionary(
                                    g => g.Key,
                                    g => (from i in g
                                          select i.Item2).ToList()
                                 )
                },
                (m, l, c) => ProcessTestCaseResults(m, testSelection, frameworkHandle),
                new TestAdapterLogger(frameworkHandle).LogMessage
            );
            _testHost?.WaitUnitCompleted();
        }

        private static void ProcessTestCaseResults(Communicator.IMessage message, List<TestCase> selection, ITestExecutionRecorder testRecorder)
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
                                        ? "Hardware nanoDevice"
                                        : "Virtual nanoDevice"
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

        #region Cancel running the tests
        public void Cancel()
        {
            _testHost?.Cancel();
        }
        #endregion
    }
}
