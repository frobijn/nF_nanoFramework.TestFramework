// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using nanoFramework.TestFramework.Tooling;
using nanoFramework.TestFramework.Tooling.Tools;

namespace TestFramework.Tooling.Tests.Helpers
{
    public sealed class InterProcessParentChildMock
    {
        #region Construction
        /// <summary>
        /// Create a mock of a pair of components in the parent/child process
        /// </summary>
        /// <param name="childProcessMessage">Method to process messages received by the child.
        /// Pass <c>null</c> if that is not required.</param>
        /// <param name="parentProcessMessage">Method to process messages received by the parent.
        /// Pass <c>null</c> if that is not required.</param>
        /// <param name="logger"></param>
        /// <param name="messageTypes">Message types that should be supported in the communication. The number and order of the
        /// messages must be the same for the parent and child process. Defaults to <see cref="TestAdapterMessages.Types"/></param>
        public InterProcessParentChildMock(
            Action<InterProcessCommunicator.IMessage, LogMessenger, CancellationToken> childProcessMessage,
            Action<InterProcessCommunicator.IMessage, CancellationToken> parentProcessMessage,
            LogMessenger logger,
            IEnumerable<Type> messageTypes = null
            )
        {
            void ChildProcessMessage(InterProcessCommunicator.IMessage message, Action<InterProcessCommunicator.IMessage> sendMessage, LogMessenger logger2, CancellationToken token)
            {
                lock (ReceivedByTestHost)
                {
                    ReceivedByTestHost.Add(message);
                }
                childProcessMessage?.Invoke(message, logger2, token);
            }
            void ParentProcessMessage(InterProcessCommunicator.IMessage message, LogMessenger logger2, CancellationToken token)
            {
                lock (ReceivedByTestAdapter)
                {
                    ReceivedByTestAdapter.Add(message);
                }
                parentProcessMessage?.Invoke(message, token);
            }

            Parent = new InterProcessParent(messageTypes ?? TestAdapterMessages.Types, ParentProcessMessage, logger);
            Parent.StartChildProcess(
                (a1, a2, a3) => Child = InterProcessChild.Start(a1, a2, a3, messageTypes ?? TestAdapterMessages.Types, ChildProcessMessage)
            );
        }
        #endregion

        #region Properties
        /// <summary>
        /// The communicator in the role of the component in the parent process
        /// </summary>
        public InterProcessParent Parent
        {
            get;
        }

        /// <summary>
        /// The communicator in the role of the component in the child process
        /// </summary>
        public InterProcessChild Child
        {
            get;
            private set;
        }

        /// <summary>
        /// The messages received by the test adapter from the test host
        /// </summary>
        public List<InterProcessCommunicator.IMessage> ReceivedByTestAdapter
        {
            get;
        } = new List<InterProcessCommunicator.IMessage>();

        /// <summary>
        /// The messages received by the test host from the test adapter
        /// </summary>
        public List<InterProcessCommunicator.IMessage> ReceivedByTestHost
        {
            get;
        } = new List<InterProcessCommunicator.IMessage>();
        #endregion

        #region Methods
        /// <summary>
        /// Wait for the completion of all message processing
        /// </summary>
        public void WaitUntilProcessingIsCompleted()
        {
            Parent.WaitUntilProcessingIsCompleted();
        }
        #endregion
    }
}
