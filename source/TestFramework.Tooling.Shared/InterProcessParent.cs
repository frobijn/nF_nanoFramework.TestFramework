// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Implementation of the <see cref="InterProcessCommunicator"/> for a component
    /// that lives in the parent process.
    /// </summary>
    public sealed class InterProcessParent : InterProcessCommunicator
    {
        #region Fields
        private readonly LogMessenger _logger;
        #endregion

        #region Public interface
        /// <summary>
        /// Start the process that runs the <see cref="InterProcessChild"/>.
        /// </summary>
        /// <param name="argument1">First argument to pass to the child.</param>
        /// <param name="argument2">Second argument to pass to the child.</param>
        /// <param name="argument3">Third argument to pass to the child.</param>
        public delegate void StartChildProcess(string argument1, string argument2, string argument3);

        /// <summary>
        /// Method that is called to processes the messages sent by the child.
        /// </summary>
        /// <param name="message">Message to process.</param>
        /// <param name="logger">Logger to pass process information to the caller.</param>
        /// <param name="cancellationToken">If the token is cancelled, the child has been asked to abort whatever it is doing.</param>
        public delegate void ProcessMessage(IMessage message, LogMessenger logger, CancellationToken cancellationToken);

        /// <summary>
        /// Create a communicator that lives in the parent process.
        /// </summary>
        /// <param name="messageTypes">Message types that should be supported in the communication. The number and order of the
        /// messages must be the same for the parent and child process.</param>
        /// <param name="messageProcessor">Method to process the received messages.</param>
        /// <remarks>
        /// After a call to <see cref="Cancel"/>, incoming messages are still processed using <paramref name="messageProcessor"/>.
        /// The method is started with a cancellation token that indicates that cancel has been requested.
        /// </remarks>
        public static InterProcessParent Start(IEnumerable<Type> messageTypes, StartChildProcess startChildProcess, ProcessMessage messageProcessor, LogMessenger logger)
        {
            var parentToChild = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            var childToParent = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

            var parent = new InterProcessParent(childToParent, parentToChild, messageTypes, messageProcessor, logger);

            string pipeOut = parentToChild.GetClientHandleAsString();
            string pipeIn = childToParent.GetClientHandleAsString();
            try
            {
                startChildProcess(parent.MessageSeparator, pipeOut, pipeIn);
            }
            catch
            {
                parent.Dispose();
                throw;
            }
            parent.StartCommunication();
            return parent;
        }

        /// <summary>
        /// Cancel whatever the child process is doing. It is up to the child process to decide
        /// when to stop executing and end the child process.
        /// </summary>
        public void Cancel()
        {
            DoSendMessage(new ChildProcess_Stop() { Abort = true });
        }
        #endregion

        #region Internal implementation
        private InterProcessParent(AnonymousPipeServerStream input, AnonymousPipeServerStream output, IEnumerable<Type> messageTypes, ProcessMessage messageProcessor, LogMessenger logger)
            : base(input, output, false, messageTypes, $"{Guid.NewGuid():N}:", (c, m, t) => messageProcessor(m, logger, t))
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        protected override void LogMessage(LoggingLevel level, string message)
        {
            _logger?.Invoke(level, message);
        }
        #endregion
    }
}
