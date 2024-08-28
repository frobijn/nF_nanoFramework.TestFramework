// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests.Tools
{
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    public sealed class CommunicatorTest
    {
        [TestMethod]
        public void Communicator_Test_MessageExchange()
        {
            #region Setup
            string id = Guid.NewGuid().ToString();
            var message = new TestDiscoverer_Parameters()
            {
                Sources = new List<string>() { id }
            };
            #endregion

            var actual = new TestAdapterTestHostMock(null, null, null);

            #region Send from test adapter to test host
            actual.TestAdapter.SendMessage(message);

            actual.WaitUntilProcessingIsCompleted();
            Assert.AreEqual(message.GetType(), actual.ReceivedByTestHost.FirstOrDefault()?.GetType());
            Assert.AreEqual(id, (actual.ReceivedByTestHost[0] as TestDiscoverer_Parameters).Sources?.FirstOrDefault());
            #endregion

            #region Send from test host to test adapter
            actual = new TestAdapterTestHostMock(null, null, null);
            actual.TestHost.SendMessage(message);

            actual.WaitUntilProcessingIsCompleted();
            Assert.AreEqual(message.GetType(), actual.ReceivedByTestAdapter.FirstOrDefault()?.GetType());
            Assert.AreEqual(id, (actual.ReceivedByTestAdapter[0] as TestDiscoverer_Parameters).Sources?.FirstOrDefault());
            #endregion
        }

        [TestMethod]
        public void Communicator_Test_LogExchange()
        {
            #region Setup
            string id = Guid.NewGuid().ToString();
            #endregion

            #region Exchange all log messages
            var logger = new LogMessengerMock();

            var actual = new TestAdapterTestHostMock((_, testHostLogger, __) =>
            {
                testHostLogger(LoggingLevel.Detailed, id); // Send a log message from host to adapter
                testHostLogger(LoggingLevel.Verbose, id);
                testHostLogger(LoggingLevel.Error, id);
            }, null, logger);

            actual.TestAdapter.SendMessage(new TestDiscoverer_Parameters()
            {
                LogLevel = (int)LoggingLevel.Detailed // instruct test host to use detailed logging
            });

            actual.WaitUntilProcessingIsCompleted();
            logger.AssertEqual(
$@"Detailed: {id}
Verbose: {id}
Error: {id}");
            #endregion

            #region Exchange only error messages
            logger = new LogMessengerMock();

            actual = new TestAdapterTestHostMock((_, testHostLogger, __) =>
            {
                testHostLogger(LoggingLevel.Detailed, id);
                testHostLogger(LoggingLevel.Verbose, id);
                testHostLogger(LoggingLevel.Error, id);
            }, null, logger);

            actual.TestAdapter.SendMessage(new TestDiscoverer_Parameters()
            {
                LogLevel = (int)LoggingLevel.Error
            });

            actual.WaitUntilProcessingIsCompleted();
            logger.AssertEqual($"Error: {id}");
            #endregion
        }

        [TestMethod]
        public void Communicator_Cancel()
        {
            #region Setup
            string id = Guid.NewGuid().ToString();
            var message = new TestDiscoverer_Parameters()
            {
                Sources = new List<string> { id }
            };
            #endregion

            #region Start test host and cancellation
            bool testHostProcessMessageStarted = false;
            TestAdapterTestHostMock actual = null;

            // Simulate processing on the test host
            void testHostProcessMessage(Communicator.IMessage _, LogMessenger __, CancellationToken cancellationToken)
            {
                testHostProcessMessageStarted = true;
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Wait for cancellation
                    Thread.Sleep(100);
                }
                actual.TestHost.SendMessage(message);
            }

            // Start the simulation
            actual = new TestAdapterTestHostMock(testHostProcessMessage, null, null);
            actual.TestAdapter.SendMessage(message);

            // Wait until the test host has started processing the task
            while (!testHostProcessMessageStarted)
            {
                Thread.Sleep(100);
            }

            // Cancel what the test host is doing
            actual.TestAdapter.Cancel();

            // Wait for all processing to be completed
            actual.WaitUntilProcessingIsCompleted();
            #endregion

            #region Asserts
            // The test adapter still processes messages after cancellation
            Assert.AreEqual(message.GetType(), actual.ReceivedByTestAdapter.FirstOrDefault()?.GetType());
            Assert.AreEqual(id, (actual.ReceivedByTestAdapter[0] as TestDiscoverer_Parameters).Sources?.FirstOrDefault());
            #endregion
        }
    }
}
