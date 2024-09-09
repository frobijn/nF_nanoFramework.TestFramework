// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;

namespace nanoFramework.TestFramework.TestAdapter
{
    /// <summary>
    /// The test adapter delegates most of the work to a separate process
    /// that is running the nanoFramework.TestFramework.TestHost.
    /// That is necessary as the test host that is running the test adapter
    /// does not support using some of the required NuGet packages and there
    /// is at this moment no way to configure the required support.
    /// </summary>
    internal sealed class TestHost
    {
        #region Fields
        private readonly InterProcessParent _testAdapter;
        private readonly Process _testHostProcess;
        #endregion

        #region Delegate to the test host
        /// <summary>
        /// Run the test host, start executing the command and wait until
        /// all results have been processed.
        /// </summary>
        /// <param name="processMessage">Method to process the results from the test host.</param>
        /// <param name="logger">Logger to pass information over the process (including from the test host) to the caller.</param>
        /// <returns>The test host, or <c>null</c> if the test host executable is not found.</returns>
        /// <remarks>
        /// After a call to <see cref="Cancel"/>, incoming messages are still processed using <paramref name="processMessage"/>.
        /// The method is started with a cancellation token that indicates that cancel has been requested.
        /// </remarks>
        public static TestHost Start(
            InterProcessCommunicator.IMessage parameters,
            InterProcessParent.ProcessMessage processMessage,
            LogMessenger logger)
        {
            // Find the test host
            string testHostApplication = Path.Combine(Path.GetDirectoryName(typeof(TestHost).Assembly.Location), "TestHost", "nanoFramework.TestFramework.TestHost.exe");
            if (!File.Exists(testHostApplication))
            {
                logger(LoggingLevel.Verbose, $"Using test adapter: '{typeof(TestHost).Assembly.Location}'");
                logger(LoggingLevel.Verbose, $"Using test host: '{testHostApplication}'");
                logger(LoggingLevel.Error, "**PANIC** Cannot find the test host - test platform is unavailable.");
                return null;
            }

            Process testHostProcess = null;
            var testAdapter = InterProcessParent.Start
            (
                TestAdapterMessages.Types,
                (a1, a2, a3) =>
                {
                    testHostProcess = Process.Start(
                        new ProcessStartInfo(testHostApplication)
                        {
                            Arguments = $"{a1} {a2} {a3}",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        }
                    );
                },
                processMessage,
                logger
            );
            testAdapter.SendMessage(parameters);
            return new TestHost(testAdapter, testHostProcess);
        }
        #endregion

        #region Construction
        /// <summary>
        /// Create the communicator
        /// </summary>
        private TestHost(InterProcessParent testAdapter, Process testHostProcess)
        {
            _testAdapter = testAdapter;
            _testHostProcess = testHostProcess;
        }

        #endregion

        #region Methods
        /// <summary>
        /// Send a message to the test host
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(InterProcessCommunicator.IMessage message)
        {
            _testAdapter.SendMessage(message);
        }

        /// <summary>
        /// Cancel whatever the test host is doing.
        /// </summary>
        public void Cancel()
        {
            _testAdapter.Cancel();
        }

        public void WaitUnitCompleted()
        {
            // Wait for all communication to complete
            _testAdapter.WaitUntilProcessingIsCompleted();

            // The test host process should have stopped by now
            // Just to be sure...
            if (!_testHostProcess.HasExited)
            {
                try
                {
                    _testHostProcess.Kill();
                }
                catch
                {
                }
            }
        }
        #endregion
    }
}
