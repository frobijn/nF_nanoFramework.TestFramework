// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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
    /// Message from the test host about the execution of its tasks
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
    /// Message to instruct the test host to stop what it is doing
    /// </summary>
    internal sealed class ChildProcess_Stop : InterProcessCommunicator.IMessage
    {
        /// <summary>
        /// Indicates whether the test host should stop immediately what it is doing.
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
        private readonly Action<InterProcessCommunicator, IMessage, CancellationToken> _messageProcessor;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private string _messageJson;
        private StreamWriter _outStream;
        private Task _inputReadingTask;
        private readonly List<Type> _messageTypes = new List<Type>
        {
            typeof(ChildProcess_Message),
            typeof(ChildProcess_Stop)
        };
        #endregion

        #region Construction / destruction
        /// <summary>
        /// Create the communicator.
        /// </summary>
        /// <param name="messageTypes">Message types that should be supported in the communication.</param>
        /// <param name="messageSeparator">Separator that is used between messages.</param>
        /// <param name="messageProcessor">Method to process the received messages.</param>
        protected InterProcessCommunicator(IEnumerable<Type> messageTypes, string messageSeparator, Action<InterProcessCommunicator, IMessage, CancellationToken> messageProcessor)
        {
            _messageProcessor = messageProcessor;
            MessageSeparator = messageSeparator;
            _messageTypes.AddRange(messageTypes);
        }

        public void Dispose()
        {
            DisposeOfPipes();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Marker interface to recognize messages
        /// </summary>
        public interface IMessage
        {
        }

        /// <summary>
        /// Send a message
        /// </summary>
        /// <typeparam name="T">Type of the message to send</typeparam>
        /// <param name="message">Message to send</param>
        public void SendMessage<T>(T message)
            where T : IMessage
        {
            int messageId = _messageTypes.IndexOf(message.GetType());
            if (messageId < 0)
            {
                throw new ArgumentException($"Messages of type {typeof(T)} cannot be sent", nameof(message));
            }

            string data = JsonConvert.SerializeObject(message, new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            });

            if (message is ChildProcess_Stop stop)
            {
                if (stop.Abort)
                {
                    _cancellationTokenSource.Cancel();
                }
            }

            lock (this)
            {
                _outStream.WriteLine(data);
                _outStream.WriteLine($"{MessageSeparator}{messageId}");
            }
        }

        /// <summary>
        /// Wait until the processing of all messages is completed
        /// </summary>
        public abstract void WaitUntilProcessingIsCompleted();
        protected void WaitUntilInputProcessingIsCompleted()
        {
            _inputReadingTask.Wait();
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
        /// <param name="inStream">Stream for the input</param>
        /// <param name="outStream">Stream for the output</param>
        protected void StartCommunication(Stream inStream, Stream outStream)
        {
            _outStream = new StreamWriter(outStream)
            {
                AutoFlush = true
            };
            _inputReadingTask = Task.Run(() => ReadInput(inStream));
        }

        /// <summary>
        /// Called if a message passes the level of required logging.
        /// </summary>
        /// <param name="level"></param>
        protected virtual void SetLogLevel(int level)
        {
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
        /// React to the reception of a <see cref="ChildProcess_Stop"/> message.
        /// </summary>
        /// <param name="abort"></param>
        protected abstract void Stop(bool abort);

        /// <summary>
        /// Process  a single received line of text
        /// </summary>
        /// <param name="data">Data received</param>
        private void ReadInput(Stream inputStream)
        {
            var processingTasks = new Dictionary<int, Task>();
            int nextMessageIndex = 0;

            try
            {
                using (var reader = new StreamReader(inputStream))
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
                            Type messageType = null;
                            // Does the line contain a message ID?
                            if (line.Length > MessageSeparator.Length)
                            {
                                if (int.TryParse(line.Substring(MessageSeparator.Length), out int messageId)
                                    && messageId >= 0
                                    && messageId < _messageTypes.Count)
                                {
                                    messageType = _messageTypes[messageId];
                                }
                            }
                            #endregion

                            #region Process the message
                            IMessage message = null;
                            if (!(messageType is null) && !(_messageJson is null))
                            {
                                try
                                {
                                    message = JsonConvert.DeserializeObject(_messageJson, messageType) as IMessage;
                                }
                                catch
                                {
                                    // Should not happen - mismatch between test host and test adapter???
                                }
                            }
                            _messageJson = null;
                            if (!(message is null))
                            {
                                if (message is ChildProcess_Stop stop)
                                {
                                    if (stop.Abort)
                                    {
                                        _cancellationTokenSource.Cancel();
                                    }
                                    Stop(stop.Abort);
                                    // Stop processing the inputs
                                    break;
                                }
                                if (message is ChildProcess_Parameters parameters)
                                {
                                    SetLogLevel(parameters.LogLevel);
                                }
                                if (message is ChildProcess_Message logMessage)
                                {
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
                                                lock (this)
                                                {
                                                    processingTasks.Remove(messageIndex);
                                                }
                                            }
                                        });
                                        processingTasks[messageIndex] = task;
                                    }
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

        protected abstract void DisposeOfPipes();
        #endregion
    }
}
