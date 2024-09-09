// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace nanoFramework.TestFramework.Tooling.Tools
{
    /// <summary>
    /// Implementation of the test adapter's <c>ITestDiscoverer.DiscoverTests</c> method,
    /// as executed in the test host process.
    /// </summary>
    public sealed class TestAdapter
    {
        #region Fields
        private readonly CancellationTokenSource _waitForLastMessageProcessed = new CancellationTokenSource();
        private Action<InterProcessCommunicator.IMessage> _sendMessage;
        private LogMessenger _logger;
        private TestCaseCollection _testCases;
        #endregion

        #region Test host support
        /// <summary>
        /// Start the test adapter in the test host child process. For each call to one of the test adapter's methods
        /// (except <c>Cancel</c>), a new test host process should be created.
        /// </summary>
        /// <param name="argument1">The first argument passed to the child process; see <see cref="InterProcessParent.StartChildProcess"/></param>
        /// <param name="argument2">The second argument passed to the child process; see <see cref="InterProcessParent.StartChildProcess"/></param>
        /// <param name="argument3">The third argument passed to the child process; see <see cref="InterProcessParent.StartChildProcess"/></param>
        public static void Run(string argument1, string argument2, string argument3)
        {
            var testAdapter = new TestAdapter();
            var testHost = InterProcessChild.Start(argument1, argument2, argument3, TestAdapterMessages.Types, testAdapter.Process);

            testAdapter._waitForLastMessageProcessed.Token.WaitHandle.WaitOne();

            testHost.WaitUntilProcessingIsCompleted();
        }
        #endregion

        #region TestAdapter methods
        /// <summary>
        /// Find the available test cases
        /// </summary>
        /// <param name="parameters">The parameters to the <c>ITestDiscoverer.DiscoverTests</c> method.</param>
        /// <param name="sendMessage">Method to send messages with the results to the test adapter.</param>
        /// <param name="logger">Logger to pass messages to the test host/test adapter</param>
        public void DiscoverTests(TestDiscoverer_Parameters parameters, Action<InterProcessCommunicator.IMessage> sendMessage, LogMessenger logger)
        {
            _sendMessage = sendMessage;
            _logger = logger;

            // Discover the test cases and send them to the adapter
            DiscoverAndSendTestCases(parameters.AssemblyFilePaths);

            // Done
            _waitForLastMessageProcessed.Cancel();
        }

        /// <summary>
        /// Find the available test cases in test assemblies, and run the ones selected by the test adapter.
        /// </summary>
        /// <param name="parameters">The parameters passed to the <c>TestExecutor.RunTests</c> method.</param>
        /// <param name="sendMessage">Method to send messages with the results to the test adapter.</param>
        /// <param name="logger">Logger to pass messages to the test host/test adapter.</param>
        /// <param name="cancellationToken">Cancellation token; when cancelled, the test adapter has requested to abort executing tests.</param>
        public void RunTests(TestExecutor_Sources_Parameters parameters, Action<InterProcessCommunicator.IMessage> sendMessage, LogMessenger logger)
        {
            _sendMessage = sendMessage;
            _logger = logger;

            // Discover the test cases and send them to the test adapter
            DiscoverAndSendTestCases(parameters.AssemblyFilePaths);

            // Send the total number to the test adapter
            var totalAmount = new TestExecutor_Sources_Done();

            foreach (TestCaseSelection selection in _testCases.TestOnVirtualDevice)
            {
                totalAmount.NumberOfTestCases += selection.TestCases.Count;
            }
            foreach (TestCaseSelection selection in _testCases.TestOnRealHardware)
            {
                totalAmount.NumberOfTestCases += selection.TestCases.Count;
            }
            _sendMessage(totalAmount);

            // The test adapter will continue with a message that starts either
            // RunAllTests or the other RunTests method.
        }

        /// <summary>
        /// Run all tests previously discovered via the <see cref="RunTests(TestExecutor_Sources_Parameters, Action{InterProcessCommunicator.IMessage}, LogMessenger)"/>
        /// method in the same test host process.
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void RunAllTests(CancellationToken cancellationToken)
        {
            // Select all test cases
            _testCases.SelectAllTestCases();

            // Run the tests
            var executor = new TestCaseCollectionRunner(_testCases, _sendMessage, _logger, cancellationToken);
            executor.Run();

            // Done
            _waitForLastMessageProcessed.Cancel();
        }

        /// <summary>
        /// Run the selected test cases. The test cases have previously been discovered via the <see cref="DiscoverTests"/>
        /// (in a different test host process) or the other <see cref="RunTests(TestExecutor_Sources_Parameters, Action{InterProcessCommunicator.IMessage}, LogMessenger)"/>
        /// method in the same test host process.
        /// </summary>
        /// <param name="parameters">The parameters passed to the <c>TestExecutor.RunTests</c> method.</param>
        /// <param name="sendMessage">Method to send messages with the results to the test adapter.</param>
        /// <param name="logger">Logger to pass messages to the test host/test adapter.</param>
        /// <param name="cancellationToken">Cancellation token; when cancelled, the test adapter has requested to abort executing tests.</param>
        public void RunTests(TestExecutor_TestCases_Parameters parameters, Action<InterProcessCommunicator.IMessage> sendMessage, LogMessenger logger, CancellationToken cancellationToken)
        {
            // Select the test cases
            if (_testCases is null)
            {
                // This is the first message sent by the test adapter
                _sendMessage = sendMessage;
                _logger = logger;

                _testCases = new TestCaseCollection(
                    from tc in parameters.TestCases
                    select (tc.AssemblyFilePath, tc.FullyQualifiedName),
                    (a) => ProjectSourceInventory.FindProjectFilePath(a, logger),
                    logger);
            }
            else
            {
                // This is a subsequent message
                _testCases.SelectTestCases(from tc in parameters.TestCases
                                           select (tc.AssemblyFilePath, tc.FullyQualifiedName),
                                           logger);
            }

            // Run the tests
            var executor = new TestCaseCollectionRunner(_testCases, sendMessage, logger, cancellationToken);
            executor.Run();

            // Done
            _waitForLastMessageProcessed.Cancel();
        }
        #endregion

        #region Internal implementation
        /// <summary>
        /// Process messages received from the test adapter
        /// </summary>
        /// <param name="message">Message received from the test adapter.</param>
        /// <param name="sendMessage">Method to send a message to the test adapter.</param>
        /// <param name="logger">Logger to pass process information to the test adapter.</param>
        /// <param name="cancellationToken">Cancellation token that is cancelled when the test host should stop doing whatever it is doing.</param>
        private void Process(InterProcessCommunicator.IMessage message, Action<InterProcessCommunicator.IMessage> sendMessage, LogMessenger logger, CancellationToken cancellationToken)
        {
            #region Main test adapter methods
            if (message is TestDiscoverer_Parameters discoverer)
            {
                DiscoverTests(discoverer, sendMessage, logger);
            }
            else if (message is TestExecutor_Sources_Parameters executeAssemblies)
            {
                RunTests(executeAssemblies, sendMessage, logger);
            }
            else if (message is TestExecutor_TestCases_Parameters executeTestCases)
            {
                RunTests(executeTestCases, sendMessage, logger, cancellationToken);
            }
            #endregion
            #region Intermediate steps
            else if (message is TestExecutor_Sources_RunAll)
            {
                RunAllTests(cancellationToken);
            }
            #endregion
        }

        /// <summary>
        /// Send the discovered test cases to the test adapter.
        /// </summary>
        /// <param name="assemblyFilePaths">The paths of the assemblies that might contain test cases.</param>
        private void DiscoverAndSendTestCases(List<string> assemblyFilePaths)
        {
            // Discover all tests
            _testCases = new TestCaseCollection(
                assemblyFilePaths,
                (a) => ProjectSourceInventory.FindProjectFilePath(a, _logger),
                _logger);

            // Send the results
            foreach (TestCaseSelection testSelection in _testCases.TestCasesPerAssemblyAndDeviceType)
            {
                var testCasesData = new TestDiscoverer_DiscoveredTests()
                {
                    Source = testSelection.AssemblyFilePath,
                    TestCases = new List<TestDiscoverer_DiscoveredTests.TestCase>
                        (
                            from tc in testSelection.TestCases
                            select new TestDiscoverer_DiscoveredTests.TestCase()
                            {
                                CodeFilePath = tc.testCase.TestMethodSourceCodeLocation?.SourceFilePath,
                                DataRowIndex = tc.testCase.DataRowIndex >= 0 ? tc.testCase.DataRowIndex : (int?)null,
                                DisplayName = tc.testCase.DisplayName,
                                FullyQualifiedName = tc.testCase.FullyQualifiedName,
                                LineNumber = tc.testCase.TestMethodSourceCodeLocation?.LineNumber ?? 0,
                                Categories = tc.testCase.Categories.ToList()
                            }
                        )
                };
                _sendMessage(testCasesData);
            }
        }

        /// <summary>
        /// Runner for a selection of test cases
        /// </summary>
        private sealed class TestCaseCollectionRunner : TestsRunner
        {
            #region Fields
            private readonly Action<InterProcessCommunicator.IMessage> _sendMessage;
            #endregion

            #region TestCaseRunner implementation
            /// <summary>
            /// Create the executor
            /// </summary>
            /// <param name="selection">Selection of test cases to execute.</param>
            /// <param name="sendMessage">Method to send messages with the results to the test adapter.</param>
            /// <param name="logger">Logger to provide process information to the caller.</param>
            /// <param name="cancellationToken">Cancellation token that indicates whether the execution of tests should be aborted (gracefully).</param>
            internal TestCaseCollectionRunner(TestCaseCollection selection, Action<InterProcessCommunicator.IMessage> sendMessage, LogMessenger logger, CancellationToken cancellationToken)
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
        #endregion
    }
}
