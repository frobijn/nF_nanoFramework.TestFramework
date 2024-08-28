// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace nanoFramework.TestFramework.Tooling.Tools
{
    /// <summary>
    /// Implementation of the test adapter's <c>ITestExecutor.RunTests</c> method
    /// for a selection of test cases, as executed in the test host process.
    /// </summary>
    public static class TestAdapterTestCasesExecutor
    {
        /// <summary>
        /// Find the available test cases
        /// </summary>
        /// <param name="parameters">The parameters to the <c>ITestDiscoverer.DiscoverTests</c> method.</param>
        /// <param name="sendMessage">Method to send messages with the results to the test adapter.</param>
        /// <param name="logger">Logger to pass messages to the test host/test adapter.</param>
        /// <param name="cancellationToken">Cancellation token; when cancelled, the test adapter has requested to abort executing tests.</param>
        public static void Run(TestExecutor_TestCases_Parameters parameters, Action<Communicator.IMessage> sendMessage, LogMessenger logger, CancellationToken cancellationToken)
        {
            // TODO
        }
    }
}
