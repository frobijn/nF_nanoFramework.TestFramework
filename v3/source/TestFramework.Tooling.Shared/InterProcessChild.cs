// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Implementation of the <see cref="InterProcessCommunicator"/> for the component
    /// that lives in the child process.
    /// </summary>
    public sealed class InterProcessChild : InterProcessCommunicator
    {
        #region Public interface
        /// <summary>
        /// Method that is called to processes the messages sent by the parent.
        /// </summary>
        /// <param name="message">Message to process.</param>
        /// <param name="sendMessageToParent">Method to send response messages to the parent</param>
        /// <param name="logger">Logger to pass process information to the parent</param>
        /// <param name="cancellationToken">If the token is cancelled, processing of the message should be aborted.</param>
        public delegate void ProcessMessage(IMessage message, Action<IMessage> sendMessageToParent, LogMessenger logger, CancellationToken cancellationToken);

        /// <summary>
        /// Start the communicator in the child process.
        /// </summary>
        /// <param name="argument1">The first argument passed to the child process; see <see cref="InterProcessParent.StartChildProcess"/></param>
        /// <param name="argument2">The second argument passed to the child process; see <see cref="InterProcessParent.StartChildProcess"/></param>
        /// <param name="argument3">The third argument passed to the child process; see <see cref="InterProcessParent.StartChildProcess"/></param>
        /// <param name="messageTypes">Message types that should be supported in the communication. The number and order of the
        /// messages must be the same for the parent and child process.</param>
        /// <param name="messageProcessor">Method to process the received messages.</param>
        /// <returns></returns>
        public static InterProcessChild Start(string argument1, string argument2, string argument3, IEnumerable<Type> messageTypes, ProcessMessage messageProcessor)
        {
            void LogMessenger(InterProcessChild child, LoggingLevel level, string message)
            {
                child.DoSendMessage(new ChildProcess_Message()
                {
                    Level = (int)level,
                    Text = message
                });
            }

            var childProcess = new InterProcessChild(
                    new AnonymousPipeClientStream(PipeDirection.In, argument2),
                    new AnonymousPipeClientStream(PipeDirection.Out, argument3),
                    messageTypes,
                    argument1,
                    (t, m, c) => messageProcessor(
                                    m,
                                    (rm) => t.SendMessage(rm),
                                    (ll, lm) => LogMessenger(t as InterProcessChild, ll, lm),
                                    c
                                )
                );
            childProcess.StartCommunication();

            return childProcess;
        }
        #endregion

        #region Internal implementation
        private InterProcessChild(
            AnonymousPipeClientStream input, AnonymousPipeClientStream output,
            IEnumerable<Type> messageTypes, string messageSeparator,
            Action<InterProcessCommunicator, IMessage, CancellationToken> messageProcessor)
            : base(input, output, true, messageTypes, messageSeparator, messageProcessor)
        {
        }
        #endregion
    }
}
