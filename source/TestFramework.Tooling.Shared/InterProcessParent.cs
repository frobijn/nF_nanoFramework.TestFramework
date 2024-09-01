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
        private AnonymousPipeServerStream _parentToChild;
        private AnonymousPipeServerStream _childToParent;
        private readonly LogMessenger _logger;
        #endregion

        #region Construction
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
        public InterProcessParent(IEnumerable<Type> messageTypes, ProcessMessage messageProcessor, LogMessenger logger)
            : base(messageTypes, $"{Guid.NewGuid():N}:", (c, m, t) => messageProcessor(m, logger, t))
        {
            _logger = logger;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Start the child process
        /// </summary>
        /// <param name="startChildProcess">Method that actually starts the process. The method receives three arguments
        /// that should be passed to the child process.</param>
        public void StartChildProcess(Action<string, string, string> startChildProcess)
        {
            _parentToChild = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            _childToParent = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

            string pipeOut = _parentToChild.GetClientHandleAsString();
            string pipeIn = _childToParent.GetClientHandleAsString();
            try
            {
                startChildProcess(MessageSeparator, pipeOut, pipeIn);
            }
            catch
            {
                DisposeOfPipes();
                throw;
            }
            StartCommunication(_childToParent, _parentToChild);
        }

        /// <summary>
        /// Cancel whatever the child process is doing. It is up to the child process to decide
        /// when to stop executing and end the child process.
        /// </summary>
        public void Cancel()
        {
            SendMessage(new ChildProcess_Stop() { Abort = true });
        }

        /// <inheritdoc/>
        public override void WaitUntilProcessingIsCompleted()
        {
            if (!(_parentToChild is null))
            {
                SendMessage(new ChildProcess_Stop() { Abort = false });
                try
                {
                    _parentToChild.WaitForPipeDrain();
                }
                catch (IOException)
                {
                    // Child may no longer be running
                }
            }
            WaitUntilInputProcessingIsCompleted();
            DisposeOfPipes();
        }
        #endregion

        #region Internal implementation

        /// <inheritdoc/>
        protected override void LogMessage(LoggingLevel level, string message)
        {
            _logger?.Invoke(level, message);
        }

        /// <inheritdoc/>
        protected override void Stop(bool abort)
        {
            // Confirmation that the child process has stopped processing inputs
        }

        /// <inheritdoc/>
        protected override void DisposeOfPipes()
        {
            _parentToChild?.Dispose();
            _parentToChild = null;
            _childToParent?.Dispose();
            _childToParent = null;
        }
        #endregion
    }
}
