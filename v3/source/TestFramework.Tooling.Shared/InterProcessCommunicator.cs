// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace nanoFramework.TestFramework.Tooling
{
    #region Task execution
    public abstract class ChildProcess_Parameters
    {

        /// <summary>
        /// Level of logging that is required for the task.
        /// Must be one of the <see cref="LoggingLevel"/> values.
        /// </summary>
        [JsonProperty("L")]
        public int LogLevel
        {
            get; set;
        }
    }

    /// <summary>
    /// Message from the child to the parent about the execution of its tasks
    /// </summary>
    internal sealed class ChildProcess_Message : InterProcessCommunicator.IMessage
    {
        /// <summary>
        /// Get or set the level of the message; one of <see cref="LoggingLevel"/> numerical values.
        /// </summary>

        [JsonProperty("L")]
        public int Level
        {
            get; set;
        }

        /// <summary>
        /// Get or set the text of the message.
        /// </summary>

        [JsonProperty("M")]
        public string Text
        {
            get; set;
        }

    }

    /// <summary>
    /// Message sent by the parent to let the child know that the next message will
    /// be a <see cref="ChildProcess_Stop"/>.
    /// </summary>
    internal sealed class ChildProcess_AboutToStop : InterProcessCommunicator.IMessage
    {
    }

    /// <summary>
    /// Message from the parent to instruct the child to stop what it is doing,
    /// and from the child to inform the parent it has completed all its tasks.
    /// </summary>
    internal sealed class ChildProcess_Stop : InterProcessCommunicator.IMessage
    {
        /// <summary>
        /// Indicates whether the child should stop immediately what it is doing.
        /// </summary>

        [JsonProperty("A")]
        public bool Abort
        {
            get; set;
        }
    }

    #endregion

    /// <summary>
    /// Base class for the communication between two components, with one component in an executable
    /// that is run in a child process of the parent process where the other lives. The class uses anonymous
    /// pipes to exchange JSON messages. Logging in the child process is translated to logging in the
    /// parent process. The communication supports graceful cancellation: the parent can ask for cancellation,
    /// the child process decides when to stop.
    /// <para>
    /// Although it is designed to support communications between processes, starting the child process is not
    /// part of this class. It can be used within the same process (for testing purposes).
    /// </para>
    /// </summary>
    public abstract class InterProcessCommunicator : IDisposable
    {
        #region Fields
        private readonly bool _isChild;
        private PipeStream _input;
        private PipeStream _output;
        private readonly Action<InterProcessCommunicator, IMessage, CancellationToken> _messageProcessor;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private LoggingLevel _loggingLevel = LoggingLevel.None;
        private bool _otherProcessHasStoppedListening;
        private string _messageJson;
        private StreamWriter _outStream;
        private Task _inputReadingTask;
        private readonly List<Type> _messageTypes = new List<Type>
        {
            typeof(ChildProcess_Message),
            typeof(ChildProcess_AboutToStop),
            typeof(ChildProcess_Stop)
        };
        #endregion

        #region Construction / destruction
        /// <summary>
        /// Create the communicator.
        /// </summary>
        /// <param name="input">The pipe to read inputs from.</param>
        /// <param name="output">The pipe to send outputs to.</param>
        /// <param name="isChild">Indicates that this instance is the child rather than the parent.</param>
        /// <param name="messageTypes">Message types that should be supported in the communication.</param>
        /// <param name="messageSeparator">Separator that is used between messages.</param>
        /// <param name="messageProcessor">Method to process the received messages.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments is <c>null</c>.</exception>
        protected InterProcessCommunicator(PipeStream input, PipeStream output, bool isChild, IEnumerable<Type> messageTypes, string messageSeparator, Action<InterProcessCommunicator, IMessage, CancellationToken> messageProcessor)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _isChild = isChild;
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
            MessageSeparator = messageSeparator ?? throw new ArgumentNullException(nameof(messageSeparator));
            _messageTypes.AddRange(messageTypes ?? throw new ArgumentNullException(nameof(messageTypes)));
        }

        public void Dispose()
        {
            SendStopAndDisposeOfPipes();
            _cancellationTokenSource.Dispose();
        }
        #endregion

        #region Public interface
        /// <summary>
        /// Marker interface to recognize messages
        /// </summary>
        public interface IMessage
        {
        }

        /// <summary>
        /// Send a message
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <exception cref="ArgumentException">Message is not one of the registered types.</exception>
        public void SendMessage(IMessage message)
        {
            lock (this)
            {
                if (_input is null)
                {
                    // Processing has already been stopped
                    return;
                }
            }
            if (message is ChildProcess_AboutToStop
                || message is ChildProcess_Message
                || message is ChildProcess_Stop)
            {
                throw new ArgumentException($"Messages of type '{message.GetType().FullName}' cannot be sent", nameof(message));
            }
            DoSendMessage(message);
        }

        /// <summary>
        /// Wait until the processing of all messages is completed
        /// </summary>
        public void WaitUntilProcessingIsCompleted()
        {
            lock (this)
            {
                if (_input is null)
                {
                    // Processing has already been stopped
                    return;
                }
            }

            if (!_isChild)
            {
                bool sendMessage;
                lock (this)
                {
                    sendMessage = !_otherProcessHasStoppedListening;
                }
                if (sendMessage)
                {
                    // The parent should notify the child it is about to stop.
                    // It cannot send the ChildProcess_Stop message, as it may
                    // still cancel whatever the child is doing.
                    try
                    {
                        DoSendMessage(new ChildProcess_AboutToStop());
                        _output.WaitForPipeDrain();
                    }
                    catch (IOException)
                    {
                        // Child may no longer be running
                        lock (this)
                        {
                            _otherProcessHasStoppedListening = true;
                            sendMessage = false;
                        }
                    }
                }
            }

            // Wait until all inputs have been processed.
            _inputReadingTask.Wait();

            // All done.
            SendStopAndDisposeOfPipes();
        }
        #endregion

        #region Internal implementation
        /// <summary>
        /// Get the message separator
        /// </summary>
        protected string MessageSeparator
        {
            get;
        }

        /// <summary>
        /// Start listening/sending data
        /// </summary>
        protected void StartCommunication()
        {
            _outStream = new StreamWriter(_output)
            {
                AutoFlush = true
            };
            _inputReadingTask = Task.Run(() => ReadInput());
        }


        /// <summary>
        /// Send a message. This is different from <see cref="SendMessage"/> as it does not check
        /// if processing has been stopped, and can send the <c>ChildProcess_*</c> messages.
        /// </summary>
        /// <param name="message">Message to send</param>
        /// <exception cref="ArgumentException">Message is not one of the registered types.</exception>
        protected void DoSendMessage(IMessage message)
        {
            if (message is ChildProcess_Message logMessage)
            {
                if (!_isChild || logMessage.Level < (int)_loggingLevel)
                {
                    // The parent does not send messages to the child
                    // The child only sends messages of the requested level.
                    return;
                }
            }
            else if (!_isChild && message is ChildProcess_Stop stop)
            {
                if (stop.Abort)
                {
                    // An abort message can only be sent by the parent to the child.
                    // Inform the parent's input processors that the child is requested to abort.
                    _cancellationTokenSource.Cancel();
                }
                lock (this)
                {
                    // This is the last message expected by the child
                    _otherProcessHasStoppedListening = true;
                }
            }

            int messageId = _messageTypes.IndexOf(message.GetType());
            if (messageId < 0)
            {
                throw new ArgumentException($"Messages of type '{message.GetType().FullName}' cannot be sent", nameof(message));
            }

            string data = JsonConvert.SerializeObject(message, new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            });

            lock (this)
            {
                _outStream.WriteLine(data);
                _outStream.WriteLine($"{MessageSeparator}{messageId}");
            }
        }

        /// <summary>
        /// Called if the message received is a log message.
        /// </summary>
        /// <param name="level">Level.</param>
        /// <param name="message">Text of the message.</param>
        protected virtual void LogMessage(LoggingLevel level, string message)
        {
        }

        /// <summary>
        /// Process  a single received line of text
        /// </summary>
        private void ReadInput()
        {
            var processingTasks = new Dictionary<int, Task>();
            int nextMessageIndex = 0;
            bool sendStopIfAllTasksAreCompleted = false;

            try
            {
                using (var reader = new StreamReader(_input))
                {
                    while (true)
                    {
                        string line = reader.ReadLine();
                        if (line is null)
                        {
                            continue;
                        }

                        if (line.StartsWith(MessageSeparator))
                        {
                            #region Message separator found - get the type
                            // Does the line contain a message ID?
                            if (line.Length <= MessageSeparator.Length || string.IsNullOrWhiteSpace(_messageJson))
                            {
                                // Separator preceding a message.
                                continue;
                            }

                            Type messageType = null;
                            string idString = line.Substring(MessageSeparator.Length);
                            if (int.TryParse(idString, out int messageId))
                            {
                                if (messageId >= 0 && messageId < _messageTypes.Count)
                                {
                                    messageType = _messageTypes[messageId];
                                }
                            }
                            if (messageType is null)
                            {
                                // Should not happen - mismatch between parent and child???
                                if (_isChild)
                                {
                                    DoSendMessage(new ChildProcess_Message()
                                    {
                                        Level = (int)LoggingLevel.Error,
                                        Text = $"The child process received a message with ID = {idString} that is not registered for the child process."
                                    });
                                }
                                else
                                {
                                    LogMessage(LoggingLevel.Error, $"Message received from the child process of an unregistered type (ID = {idString})");
                                }
                                continue;
                            }
                            #endregion

                            #region Deserialize the message
                            IMessage message = null;
                            try
                            {
                                message = JsonConvert.DeserializeObject(_messageJson, messageType) as IMessage;
                            }
                            catch (Exception ex)
                            {
                                // Should not happen - mismatch between parent and child???
                                if (_isChild)
                                {
                                    DoSendMessage(new ChildProcess_Message()
                                    {
                                        Level = (int)LoggingLevel.Error,
                                        Text = $"Message (received by the child process) of type '{messageType.FullName}' cannot be deserialized: {ex.Message}"
                                    });
                                }
                                else
                                {
                                    LogMessage(LoggingLevel.Error, $"Message (received from child process) of type '{messageType.FullName}' cannot be deserialized: {ex.Message}");
                                }
                                continue;
                            }
                            _messageJson = null;
                            #endregion

                            #region Process the message
                            if (message is ChildProcess_Stop stop)
                            {
                                if (_isChild && stop.Abort)
                                {
                                    _cancellationTokenSource.Cancel();
                                }
                                // Stop processing the inputs
                                break;
                            }
                            if (_isChild && message is ChildProcess_Parameters parameters)
                            {
                                _loggingLevel = (LoggingLevel)parameters.LogLevel;
                            }

                            if (message is ChildProcess_AboutToStop)
                            {
                                if (_isChild)
                                {
                                    // Parent informs child not to expect additional inputs
                                    lock (processingTasks)
                                    {
                                        if (processingTasks.Count == 0)
                                        {
                                            DoSendMessage(new ChildProcess_Stop());
                                        }
                                        else
                                        {
                                            sendStopIfAllTasksAreCompleted = true;
                                        }
                                    }
                                }
                                // else: should not happen; ignore.
                            }
                            else if (message is ChildProcess_Message logMessage)
                            {
                                // Process log messages synchronously, to preserve the order the messages are received.
                                LogMessage((LoggingLevel)logMessage.Level, logMessage.Text);
                            }
                            else
                            {
                                // Process message but do not wait for that to finish
                                lock (processingTasks)
                                {
                                    int messageIndex = nextMessageIndex++;
                                    var task = Task.Run(() =>
                                    {
                                        try
                                        {
                                            _messageProcessor(this, message, _cancellationTokenSource.Token);
                                        }
                                        finally
                                        {
                                            lock (processingTasks)
                                            {
                                                processingTasks.Remove(messageIndex);
                                                if (sendStopIfAllTasksAreCompleted && processingTasks.Count == 0)
                                                {
                                                    DoSendMessage(new ChildProcess_Stop());
                                                }
                                            }
                                        }
                                    });
                                    processingTasks[messageIndex] = task;
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            _messageJson = line;
                        }
                    }
                }
            }
            catch (IOException)
            {
            }

            // Do not end this task before all message processing is done
            Task[] runningTasks;
            lock (processingTasks)
            {
                runningTasks = processingTasks.Values.ToArray();
            }
            if (runningTasks.Length > 0)
            {
                Task.WaitAll(runningTasks);
            }
        }

        /// <summary>
        /// Inform the other party that this one is stopping, and dispose of the
        /// pipes / streams.
        /// </summary>
        private void SendStopAndDisposeOfPipes()
        {
            bool sendMessage;
            lock (this)
            {
                if (_input is null)
                {
                    return;
                }
                _input.Dispose();
                _input = null;

                sendMessage = !_otherProcessHasStoppedListening;
            }
            if (sendMessage)
            {
                try
                {
                    DoSendMessage(new ChildProcess_Stop());
                    _output.WaitForPipeDrain();
                    _outStream.Dispose();
                }
                catch (IOException)
                {
                    // Listener may no longer be running
                }
            }
            lock (this)
            {
                _outStream = null;
                _output.Dispose();
                _output = null;
            }
        }
        #endregion
    }
}
