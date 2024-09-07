// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests.Tools
{
    [TestClass]
    [TestCategory("Visual Studio/VSTest")]
    [TestCategory("Test host")]
    public sealed class InterProcessCommunicatorTest
    {
        [TestMethod]
        public void InterProcessCommunicator_Test_MessageExchange()
        {
            #region Setup
            string idParent = Guid.NewGuid().ToString();
            var messageFromParent = new TestDiscoverer_Parameters()
            {
                AssemblyFilePaths = new List<string>() { idParent }
            };
            string idChild = Guid.NewGuid().ToString();
            var messageFromChild = new TestDiscoverer_Parameters()
            {
                AssemblyFilePaths = new List<string>() { idChild }
            };
            #endregion

            #region Send from parent to child
            var actual = new InterProcessParentChildMock(
                (_, sendMessageFromChildToParent, ___, ____) =>
                {
                    sendMessageFromChildToParent(messageFromChild);
                },
                null,
                null);

            actual.Parent.SendMessage(messageFromParent);
            #endregion

            actual.WaitUntilProcessingIsCompleted();
            Assert.AreEqual(messageFromParent.GetType(), actual.ReceivedByChild.FirstOrDefault()?.GetType());
            Assert.AreEqual(idParent, (actual.ReceivedByChild[0] as TestDiscoverer_Parameters).AssemblyFilePaths?.FirstOrDefault());

            Assert.AreEqual(messageFromChild.GetType(), actual.ReceivedByParent.FirstOrDefault()?.GetType());
            Assert.AreEqual(idChild, (actual.ReceivedByParent[0] as TestDiscoverer_Parameters).AssemblyFilePaths?.FirstOrDefault());
        }

        [TestMethod]
        public void InterProcessCommunicator_Test_LogExchange()
        {
            #region Setup
            string id = Guid.NewGuid().ToString();
            #endregion

            #region Exchange all log messages
            var logger = new LogMessengerMock();

            var actual = new InterProcessParentChildMock((_, __, childLogger, ____) =>
            {
                childLogger(LoggingLevel.Detailed, id); // Send a log message from child to parent
                childLogger(LoggingLevel.Verbose, id);
                childLogger(LoggingLevel.Error, id);
            }, null, logger);

            actual.Parent.SendMessage(new TestDiscoverer_Parameters()
            {
                LogLevel = (int)LoggingLevel.Detailed // instruct child to use detailed logging
            });

            actual.WaitUntilProcessingIsCompleted();
            logger.AssertEqual(
$@"Detailed: {id}
Verbose: {id}
Error: {id}");
            #endregion

            #region Exchange only error messages
            logger = new LogMessengerMock();

            actual = new InterProcessParentChildMock((_, __, childLogger, ____) =>
            {
                childLogger(LoggingLevel.Detailed, id);
                childLogger(LoggingLevel.Verbose, id);
                childLogger(LoggingLevel.Error, id);
            }, null, logger);

            actual.Parent.SendMessage(new TestDiscoverer_Parameters()
            {
                LogLevel = (int)LoggingLevel.Error
            });

            actual.WaitUntilProcessingIsCompleted();
            logger.AssertEqual($"Error: {id}");
            #endregion
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void InterProcessCommunicator_Cancel(bool cancelBeforeWait)
        {
            #region Setup
            string id = Guid.NewGuid().ToString();
            var message = new TestDiscoverer_Parameters()
            {
                AssemblyFilePaths = new List<string> { id }
            };
            #endregion

            #region Start child and cancellation
            CancellationTokenSource waitForChildProcessMessageStarted = new CancellationTokenSource();
            InterProcessParentChildMock actual = null;

            // Simulate processing on the child
            void childProcessMessage(InterProcessCommunicator.IMessage _, Action<InterProcessCommunicator.IMessage> sendMessageFromChildToParent, LogMessenger ___, CancellationToken cancellationToken)
            {
                waitForChildProcessMessageStarted.Cancel();
                cancellationToken.WaitHandle.WaitOne();
                sendMessageFromChildToParent(message);
            }

            // Start the simulation
            actual = new InterProcessParentChildMock(childProcessMessage, null, null);
            actual.Parent.SendMessage(message);

            // Wait until the child has started processing the task
            waitForChildProcessMessageStarted.Token.WaitHandle.WaitOne();

            if (cancelBeforeWait)
            {
                // Cancel what the child is doing
                actual.Parent.Cancel();

                // Wait for all processing to be completed
                actual.WaitUntilProcessingIsCompleted();
            }
            else
            {
                var run = Task.Run(() =>
                {
                    // Make sure this is executed after the WaitUntilProcessingIsCompleted
                    Task.Delay(100).GetAwaiter().GetResult();

                    // Cancel what the child is doing
                    actual.Parent.Cancel();
                });
                // Wait for all processing to be completed
                actual.WaitUntilProcessingIsCompleted();
            }

            #endregion

            #region Asserts
            // The parent still processes messages after cancellation
            Assert.AreEqual(message.GetType(), actual.ReceivedByParent.FirstOrDefault()?.GetType());
            Assert.AreEqual(id, (actual.ReceivedByParent[0] as TestDiscoverer_Parameters).AssemblyFilePaths?.FirstOrDefault());
            #endregion
        }
    }
}
