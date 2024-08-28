// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace nanoFramework.TestFramework.Tooling.Tools
{
    /// <summary>
    /// Implementation of the test adapter's <c>ITestExecutor.RunTests</c> method
    /// for a selection of test cases, as executed in the test host process.
    /// </summary>
    public sealed class TestAdapterTestCasesExecutor : TestCaseExecutionOrchestration
    {
        #region Fields
        private readonly Action<Communicator.IMessage> _sendMessage;
        #endregion

        #region Public interface
        /// <summary>
        /// Find the available test cases
        /// </summary>
        /// <param name="parameters">The parameters to the <c>ITestDiscoverer.DiscoverTests</c> method.</param>
        /// <param name="sendMessage">Method to send messages with the results to the test adapter.</param>
        /// <param name="logger">Logger to pass messages to the test host/test adapter.</param>
        /// <param name="cancellationToken">Cancellation token; when cancelled, the test adapter has requested to abort executing tests.</param>
        public static void Run(TestExecutor_TestCases_Parameters parameters, Action<Communicator.IMessage> sendMessage, LogMessenger logger, CancellationToken cancellationToken)
        {
            var testCaseSelection = new TestCaseCollection(
                from tc in parameters.TestCases
                select (tc.AssemblyFilePath, tc.DisplayName, tc.FullyQualifiedName),
                (a) => ProjectSourceInventory.FindProjectFilePath(a, logger),
                true,
                logger);

            var executor = new TestAdapterTestCasesExecutor(testCaseSelection, sendMessage, logger, cancellationToken);
            executor.Run();
        }
        #endregion

        #region TestCaseExecutionOrchestration implementation
        /// <summary>
        /// Create the executor
        /// </summary>
        /// <param name="selection">Selection of test cases to execute.</param>
        /// <param name="sendMessage">Method to send messages with the results to the test adapter.</param>
        /// <param name="logger">Logger to provide process information to the caller.</param>
        /// <param name="cancellationToken">Cancellation token that indicates whether the execution of tests should be aborted (gracefully).</param>
        private TestAdapterTestCasesExecutor(TestCaseCollection selection, Action<Communicator.IMessage> sendMessage, LogMessenger logger, CancellationToken cancellationToken)
            : base(selection, logger, cancellationToken)
        {
            _sendMessage = sendMessage;
        }

        /// <inheritdoc/>
        protected override void AddTestResults(IEnumerable<TestResult> results, string deviceName)
        {
            _sendMessage(new TestExecutor_TestResults()
            {
                ComputerName = deviceName,
                TestResults = new List<TestExecutor_TestResults.TestResult>(
                                  from tr in results
                                  select new TestExecutor_TestResults.TestResult()
                                  {
                                      DisplayName = tr.DisplayName,
                                      Duration = tr.Duration,
                                      ErrorMessage = tr.ErrorMessage,
                                      ForRealHardware = tr.TestCase.ShouldRunOnRealHardware,
                                      Index = tr.Index,
                                      Messages = tr._messages,
                                      Outcome = (int)tr.Outcome
                                  }
                              )
            });
        }
        #endregion
    }
}
