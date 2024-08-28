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

namespace nanoFramework.TestFramework.Tooling.Tools
{
    #region Task execution
    public abstract class TestHost_Parameters
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
    public sealed class TestHost_Message : Communicator.IMessage
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
    public sealed class TestHost_Stop : Communicator.IMessage
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

    #region TestDiscoverer
    /// <summary>
    /// Parameters to start the discovery of tests with.
    /// </summary>
    public sealed class TestDiscoverer_Parameters : TestHost_Parameters, Communicator.IMessage
    {
        /// <summary>
        /// The path of the assemblies to examine to discover unit tests
        /// </summary>
        [JsonProperty("S")]
        public List<string> Sources
        {
            get; set;
        }
    }

    /// <summary>
    /// (Partial) result of the test discovery
    /// </summary>
    public sealed class TestDiscoverer_DiscoveredTests : Communicator.IMessage
    {
        public sealed class TestCase
        {
            /// <summary>
            /// Gets or sets the display name of the test case.
            /// </summary>
            [JsonProperty("D")]
            public string DisplayName
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the fully qualified name of the test case.
            /// </summary>
            [JsonProperty("N")]
            public string FullyQualifiedName
            {
                get; set;
            }

            /// <summary>
            /// The source code file path of the test.
            /// </summary>
            [JsonProperty("S")]
            public string CodeFilePath
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the line number of the test.
            /// </summary>
            [JsonProperty("L")]
            public int? LineNumber
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the collection of traits
            /// </summary>
            [JsonProperty("T")]
            public List<string> Traits
            {
                get; set;
            }
        }

        /// <summary>
        /// Gets the test container source from which the test is discovered.
        /// </summary>
        [JsonProperty("S")]
        public string Source
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the collection of test cases discovered
        /// </summary>
        [JsonProperty("T")]
        public List<TestCase> TestCases
        {
            get; set;
        }
    }
    #endregion

    #region TestExecutor
    /// <summary>
    /// Parameters to start the execution of a selection of previously discovered tests with
    /// </summary>
    public sealed class TestExecutor_TestCases_Parameters : TestHost_Parameters, Communicator.IMessage
    {
        /// <summary>
        /// Description of the test case in a test assembly.
        /// </summary>
        public sealed class TestCase
        {
            /// <summary>
            /// Gets or sets the path to the assembly that contains the test case.
            /// </summary>
            [JsonProperty("S")]
            public string AssemblyFilePath
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the display name of the test case.
            /// </summary>
            [JsonProperty("D")]
            public string DisplayName
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the fully qualified name of the test case.
            /// </summary>
            [JsonProperty("N")]
            public string FullyQualifiedName
            {
                get; set;
            }
        }

        /// <summary>
        /// Get the test cases to execute.
        /// </summary>
        [JsonProperty("T")]
        public List<TestCase> TestCases
        {
            get; set;
        }
    }

    /// <summary>
    /// (Partial) result of the execution of the tests
    /// </summary>
    public sealed class TestExecutor_TestResults : Communicator.IMessage
    {
        public sealed class TestResult
        {
            /// <summary>
            /// Gets or sets the index of the test case in the selection to be run,
            /// or 
            /// </summary>
            [JsonProperty("I")]
            public int Index
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the outcome of a test case.
            /// </summary>
            [JsonProperty("O")]
            public int Outcome
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the exception message.
            /// </summary>
            [JsonProperty("E")]
            public string ErrorMessage
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the TestResult Display name.
            /// </summary>
            [JsonProperty("D")]
            public string DisplayName
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the test result Duration.
            /// </summary>
            [JsonProperty("T")]
            public TimeSpan Duration
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets whether the test was executed on real hardware
            /// </summary>
            [JsonProperty("H")]
            public bool? ForRealHardware
            {
                get; set;
            }

            /// <summary>
            /// Gets or sets the test messages.
            /// </summary>
            [JsonProperty("M")]
            public List<string> Messages
            {
                get; set;
            }
        }

        /// <summary>
        /// Gets or sets the ComputerName the tests have been executed on
        /// </summary>
        [JsonProperty("C")]
        public string ComputerName { get; set; }

        /// <summary>
        /// Get the test results.
        /// </summary>
        [JsonProperty("R")]
        public List<TestResult> TestResults
        {
            get; set;
        }
    }
    #endregion

    #region Inter-process communication
    /// <summary>
    /// Base class for the communication between components in the role of test adapter
    /// and test host.
    /// </summary>
    public abstract class Communicator : IDisposable
    {
        #region Fields
        private readonly Action<Communicator, IMessage, CancellationToken> _messageProcessor;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private string _messageJson;
        private StreamWriter _outStream;
        private Task _inputReadingTask;
        private static readonly List<Type> s_messageTypes = new List<Type>
        {
            typeof(TestHost_Message),
            typeof(TestHost_Stop),
            typeof(TestDiscoverer_Parameters),
            typeof(TestDiscoverer_DiscoveredTests),
            typeof(TestExecutor_TestCases_Parameters),
            typeof(TestExecutor_TestResults)
        };
        #endregion

        #region Construction / destruction
        /// <summary>
        /// Create the communicator
        /// </summary>
        /// <param name="messageSeparator">Separator that is used between messages</param>
        /// <param name="messageProcessor">Method to process the received messages</param>
        protected Communicator(string messageSeparator, Action<Communicator, IMessage, CancellationToken> messageProcessor)
        {
            _messageProcessor = messageProcessor;
            MessageSeparator = messageSeparator;
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
            int messageId = s_messageTypes.IndexOf(message.GetType());
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

            if (message is TestHost_Stop stop)
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
        /// React to the reception of a <see cref="TestHost_Stop"/> message.
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
                                    && messageId < s_messageTypes.Count)
                                {
                                    messageType = s_messageTypes[messageId];
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
                                if (message is TestHost_Stop stop)
                                {
                                    if (stop.Abort)
                                    {
                                        _cancellationTokenSource.Cancel();
                                    }
                                    Stop(stop.Abort);
                                    // Stop processing the inputs
                                    break;
                                }
                                if (message is TestHost_Parameters parameters)
                                {
                                    SetLogLevel(parameters.LogLevel);
                                }
                                if (message is TestHost_Message logMessage)
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

    /// <summary>
    /// Implementation of the <see cref="Communicator"/> for a component
    /// in the role of the test adapter
    /// </summary>
    public sealed class TestAdapterCommunicator : Communicator
    {
        #region Fields
        private AnonymousPipeServerStream _adapterToTestHost;
        private AnonymousPipeServerStream _testHostToAdapter;
        private readonly LogMessenger _logger;
        #endregion

        #region Construction
        /// <summary>
        /// Method that is called to processes the messages (except for <c>TestHost_*</c> messages).
        /// </summary>
        /// <param name="message">Message to process.</param>
        /// <param name="logger">Logger to pass process information to the caller of the test adapter</param>
        /// <param name="cancellationToken">If the token is cancelled, the test host is asked to abort whatever it is doing.</param>
        public delegate void ProcessMessage(IMessage message, LogMessenger logger, CancellationToken cancellationToken);

        /// <summary>
        /// Create a communicator in the role of the test adapter.
        /// </summary>
        /// <param name="messageProcessor">Method to process the received messages.</param>
        /// <remarks>
        /// After a call to <see cref="Cancel"/>, incoming messages are still processed using <paramref name="messageProcessor"/>.
        /// The method is started with a cancellation token that indicates that cancel has been requested.
        /// </remarks>
        public TestAdapterCommunicator(ProcessMessage messageProcessor, LogMessenger logger)
            : base($"{Guid.NewGuid():N}:", (c, m, t) => messageProcessor(m, logger, t))
        {
            _logger = logger;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Start the test host process
        /// </summary>
        /// <param name="startTestHostProcess">Method that actually starts the process. The method receives three arguments
        /// that should be passed to the test host process.</param>
        public void StartTestHost(Action<string, string, string> startTestHostProcess)
        {
            _adapterToTestHost = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            _testHostToAdapter = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);

            string pipeOut = _adapterToTestHost.GetClientHandleAsString();
            string pipeIn = _testHostToAdapter.GetClientHandleAsString();
            try
            {
                startTestHostProcess(MessageSeparator, pipeOut, pipeIn);
            }
            catch
            {
                DisposeOfPipes();
                throw;
            }
            StartCommunication(_testHostToAdapter, _adapterToTestHost);
        }

        /// <summary>
        /// Cancel whatever the test host is doing
        /// </summary>
        public void Cancel()
        {
            SendMessage(new TestHost_Stop() { Abort = true });
        }

        /// <inheritdoc/>
        public override void WaitUntilProcessingIsCompleted()
        {
            if (!(_adapterToTestHost is null))
            {
                SendMessage(new TestHost_Stop() { Abort = false });
                try
                {
                    _adapterToTestHost.WaitForPipeDrain();
                }
                catch (IOException)
                {
                    // Test host may no longer be running
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
            // Confirmation that the test host has stopped processing inputs
        }

        /// <inheritdoc/>
        protected override void DisposeOfPipes()
        {
            _adapterToTestHost?.Dispose();
            _adapterToTestHost = null;
            _testHostToAdapter?.Dispose();
            _testHostToAdapter = null;
        }
        #endregion
    }

    /// <summary>
    /// Implementation of the <see cref="Communicator"/> for a component
    /// in the role of the test host
    /// </summary>
    public sealed class TestHostCommunicator : Communicator
    {
        #region Fields
        private AnonymousPipeClientStream _adapterToTestHost;
        private AnonymousPipeClientStream _testHostToAdapter;
        private LoggingLevel _loggingLevel = LoggingLevel.None;
        #endregion

        #region Construction
        /// <summary>
        /// Method that is called to processes the messages (except for <c>TestHost_*</c> messages).
        /// </summary>
        /// <param name="message">Message to process.</param>
        /// <param name="sendMessageToTestAdapter">Method to send response messages to the adapter (do not send <c>TestHost_*</c> messages)</param>
        /// <param name="logger">Logger to pass process information to the test adapter</param>
        /// <param name="cancellationToken">If the token is cancelled, processing of the message should be aborted.</param>
        public delegate void ProcessMessage(IMessage message, Action<IMessage> sendMessageToTestAdapter, LogMessenger logger, CancellationToken cancellationToken);

        /// <summary>
        /// Start the communicator
        /// </summary>
        /// <param name="argument1">The first argument passed to the test host; see <see cref="TestAdapterCommunicator.StartTestHost(Action{string, string, string})"/></param>
        /// <param name="argument2">The second argument passed to the test host; see <see cref="TestAdapterCommunicator.StartTestHost(Action{string, string, string})"/></param>
        /// <param name="argument3">The third argument passed to the test host; see <see cref="TestAdapterCommunicator.StartTestHost(Action{string, string, string})"/></param>
        /// <param name="messageProcessor">Method to process the received messages.</param>
        /// <returns></returns>
        public static TestHostCommunicator Start(string argument1, string argument2, string argument3, ProcessMessage messageProcessor)
        {
            void LogMessenger(Communicator host, LoggingLevel level, string message)
            {
                if (level >= (host as TestHostCommunicator)._loggingLevel)
                {
                    host.SendMessage(new TestHost_Message()
                    {
                        Level = (int)level,
                        Text = message
                    });
                }
            }

            var testHost = new TestHostCommunicator(
                    argument1,
                    (t, m, c) => messageProcessor(
                                    m,
                                    (rm) => t.SendMessage(rm),
                                    (ll, lm) => LogMessenger(t, ll, lm),
                                    c
                                )
                )
            {
                _adapterToTestHost = new AnonymousPipeClientStream(PipeDirection.In, argument2),
                _testHostToAdapter = new AnonymousPipeClientStream(PipeDirection.Out, argument3)
            };
            testHost.StartCommunication(testHost._adapterToTestHost, testHost._testHostToAdapter);

            return testHost;
        }
        #endregion

        #region Internal implementation
        private TestHostCommunicator(string messageSeparator, Action<Communicator, IMessage, CancellationToken> messageProcessor)
            : base(messageSeparator, messageProcessor)
        {
        }

        /// <inheritdoc/>
        protected override void SetLogLevel(int level)
            => _loggingLevel = (LoggingLevel)level;

        /// <inheritdoc/>
        public override void WaitUntilProcessingIsCompleted()
        {
            WaitUntilInputProcessingIsCompleted();
            if (!(_testHostToAdapter is null))
            {
                SendMessage(new TestHost_Stop() { Abort = false });
                try
                {
                    _testHostToAdapter.WaitForPipeDrain();
                }
                catch (IOException)
                {
                    // Test adapter may no longer be available
                }
            }
            DisposeOfPipes();
        }

        /// <inheritdoc/>
        protected override void Stop(bool abort)
        {
            // Send the stop message after the test host is done
            Task.Run(WaitUntilProcessingIsCompleted);
        }

        /// <inheritdoc/>
        protected override void DisposeOfPipes()
        {
            _adapterToTestHost?.Dispose();
            _adapterToTestHost = null;
            _testHostToAdapter?.Dispose();
            _testHostToAdapter = null;
        }
        #endregion
    }
    #endregion
}
