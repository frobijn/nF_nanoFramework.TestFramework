// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;

namespace TestFramework.Tooling.Tests.Helpers
{
    public sealed class TestAdapterTestHostMock
    {
        #region Construction
        /// <summary>
        /// Create a mock of a pair test adapter / test host
        /// </summary>
        /// <param name="testHostProcessMessage">Method to process messages received by the test host.
        /// Pass <c>null</c> if that is not required.</param>
        /// <param name="testAdapterProcessMessage">Method to process messages received by the test adapter.
        /// Pass <c>null</c> if that is not required.</param>
        /// <param name="logger"></param>
        public TestAdapterTestHostMock(
            Action<Communicator.IMessage, LogMessenger, CancellationToken> testHostProcessMessage,
            Action<Communicator.IMessage, CancellationToken> testAdapterProcessMessage,
            LogMessenger logger
            )
        {
            void TestHostProcessMessage(Communicator.IMessage message, Action<Communicator.IMessage> sendMessage, LogMessenger logger2, CancellationToken token)
            {
                lock (ReceivedByTestHost)
                {
                    ReceivedByTestHost.Add(message);
                }
                testHostProcessMessage?.Invoke(message, logger2, token);
            }
            void TestAdapterProcessMessage(Communicator.IMessage message, LogMessenger logger2, CancellationToken token)
            {
                lock (ReceivedByTestAdapter)
                {
                    ReceivedByTestAdapter.Add(message);
                }
                testAdapterProcessMessage?.Invoke(message, token);
            }

            TestAdapter = new TestAdapterCommunicator(TestAdapterProcessMessage, logger);
            TestAdapter.StartTestHost(
                (a1, a2, a3) => TestHost = TestHostCommunicator.Start(a1, a2, a3, TestHostProcessMessage)
            );
        }
        #endregion

        #region Properties
        /// <summary>
        /// The communicator in the role of the test adapter
        /// </summary>
        public TestAdapterCommunicator TestAdapter
        {
            get;
        }

        /// <summary>
        /// The communicator in the role of the test host
        /// </summary>
        public TestHostCommunicator TestHost
        {
            get;
            private set;
        }

        /// <summary>
        /// The messages received by the test adapter from the test host
        /// </summary>
        public List<Communicator.IMessage> ReceivedByTestAdapter
        {
            get;
        } = new List<Communicator.IMessage>();

        /// <summary>
        /// The messages received by the test host from the test adapter
        /// </summary>
        public List<Communicator.IMessage> ReceivedByTestHost
        {
            get;
        } = new List<Communicator.IMessage>();
        #endregion

        #region Methods
        /// <summary>
        /// Wait for the completion of all message processing
        /// </summary>
        public void WaitUntilProcessingIsCompleted()
        {
            TestAdapter.WaitUntilProcessingIsCompleted();
        }
        #endregion
    }
}
