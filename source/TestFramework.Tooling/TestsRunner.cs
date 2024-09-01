// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Orchestration of the execution of a selection of test cases:
    /// which test cases should be run on which available device?
    /// The selection of test cases is paired with the corresponding <see cref="TestFrameworkConfiguration"/>
    /// and deployment configuration. Available real hardware devices are requested and
    /// tests are scheduled to be run on real hardware devices and virtual devices.
    /// Tests are executed on different devices in parallel. The actual execution of
    /// tests is delegated to <see cref="NanoCLRHelper"/> and <see cref="RealHardwareDeviceHelper"/>.
    /// </summary>
    public abstract class TestsRunner
    {
        #region Fields
        private readonly Dictionary<TestCaseSelection, RealHardwareExecution> _realHardwareExecution = new Dictionary<TestCaseSelection, RealHardwareExecution>();
        private readonly Dictionary<TestCaseSelection, VirtualDeviceExecution> _virtualDeviceExecution = new Dictionary<TestCaseSelection, VirtualDeviceExecution>();
        private bool _allowAllSerialPorts;
        private readonly HashSet<string> _allowedSerialPorts = new HashSet<string>();
        private readonly HashSet<string> _excludedSerialPorts = new HashSet<string>();
        private int _nextRunningTaskIndex;
        private readonly Dictionary<int, Task> _runningTasks = new Dictionary<int, Task>();
        private readonly HashSet<TestCase> _testCasesWithResult = new HashSet<TestCase>();
        #endregion

        #region Construction
        /// <summary>
        /// Create a new tests runner.
        /// </summary>
        /// <param name="selection">Selection of test cases to execute.</param>
        /// <param name="logger">Logger to provide process information to the caller.</param>
        /// <param name="cancellationToken">Cancellation token that indicates whether the execution of tests should be aborted (gracefully).</param>
        protected TestsRunner(TestCaseCollection selection, LogMessenger logger, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            Logger = logger;
            foreach (TestCaseSelection tc in selection.TestOnRealHardware)
            {
                _realHardwareExecution[tc] = new RealHardwareExecution();
            }
            foreach (TestCaseSelection tc in selection.TestOnVirtualDevice)
            {
                _virtualDeviceExecution[tc] = new VirtualDeviceExecution();
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the logger to provide process information to the caller.
        /// </summary>
        protected LogMessenger Logger
        {
            get;
        }

        /// <summary>
        /// Get the cancellation token that indicates whether the execution of tests should be aborted (gracefully).
        /// </summary>
        protected CancellationToken CancellationToken
        {
            get;
        }

        /// <summary>
        /// Get the maximum number of virtual devices that can run in parallel.
        /// </summary>
        protected int MaxVirtualDevices
        {
            get;
            private set;
        }
        #endregion

        #region Public interface
        /// <summary>
        /// Orchestrate the execution of the test cases. The method exits if all tests have been 
        /// </summary>
        public void Run()
        {
            // Check whether the tests can be executed according to the test framework configuration
            FindTestFrameworkConfigurationsAndSerialPorts();
            if (_realHardwareExecution.Count == 0 && _virtualDeviceExecution.Count == 0)
            {
                Logger.Invoke(LoggingLevel.Verbose, "No test cases to execute.");
                return;
            }

            // Start discovering real hardware devices, if necessary
            Task discoverDevices = null;
            if (_allowAllSerialPorts)
            {
                discoverDevices = DiscoverAllRealHardware(
                                    _excludedSerialPorts,
                                    (device) => RunAsync(() => RunTestsOnRealHardware(device))
                                  );
            }
            else if (_allowedSerialPorts.Count > 0)
            {
                discoverDevices = DiscoverSelectedRealHardware(
                                    _allowedSerialPorts,
                                    (device) => RunAsync(() => RunTestsOnRealHardware(device))
                                  );
            }

            // Execute tests on virtual devices
            RunTestsOnVirtualDevices();

            // Wait until all devices have been discovered.
            discoverDevices?.Wait();

            // Wait until all asynchronous tasks have been completed
            // This includes running virtual devices and running tests on discovered devices
            WaitForAsyncTasks();

            // Send results for tests that could not be run because there is no suitable
            // real hardware device available.
            AddResultsForRealHardwareTestsNotExecuted();
        }

        /// <summary>
        /// Functionality required from a real hardware device.
        /// Presented as an interface to be able to test this class without using
        /// actual hardware. By default, <see cref="RealHardwareDeviceHelper"/> is used
        /// as implementation.
        /// </summary>
        public interface IRealHardwareDevice
        {
            #region Properties
            /// <summary>
            /// Get the serial port the device is connected to
            /// </summary>
            string SerialPort
            {
                get;
            }

            /// <summary>
            /// Get the name of the target/firmware installed on the device
            /// </summary>
            string Target
            {
                get;
            }

            /// <summary>
            /// Get the platform of the device.
            /// </summary>
            string Platform
            {
                get;
            }
            #endregion

            #region Method
            /// <summary>
            /// Execute an application consisting of a set of assemblies on the device.
            /// </summary>
            /// <param name="assemblies">The assemblies to execute. One of the assemblies must be a program.</param>
            /// <param name="reportPrefix">The prefix to use to report information about running unit tests.</param>
            /// <param name="processOutput">Action to process the output that is provided in chunks.</param>
            /// <param name="logger">Logger to pass process information to the caller.</param>
            /// <param name="cancellationToken">Cancellation token that should be cancelled to stop running the <paramref name="communication"/>,
            /// e.g., if all required output has been received or if running tests should be aborted.</param>
            /// <returns>Indicates whether the execution on the device was successful and did not result in an error.</returns>
            Task<bool> RunAssembliesAsync(
                IEnumerable<AssemblyMetadata> assemblies,
                string reportPrefix,
                Action<string> processOutput,
                LogMessenger logger,
                CancellationToken cancellationToken);
            #endregion
        }

        /// <summary>
        /// Functionality required from a Virtual Device.
        /// Presented as an interface to be able to test this class without using
        /// an actual Virtual Device. By default, <see cref="NanoCLRHelper"/> is used
        /// as implementation.
        /// </summary>
        public interface IVirtualDevice
        {
            /// <summary>
            /// Execute an application consisting of a set of assemblies on a new instance of the Virtual Device.
            /// </summary>
            /// <param name="assemblies">The assemblies to execute. One of the assemblies must be a program.</param>
            /// <param name="localCLRInstanceFilePath">Path to a local instance of the nanoFramework CLR. Pass <c>null</c> tio use the default CLR.</param>
            /// <param name="logging">Level of logging in the virtual device.</param>
            /// <param name="reportPrefix">The prefix to use to report information about running unit tests.</param>
            /// <param name="processOutput">Action to process the output that is provided in chunks. Pass <c>null</c> if the output is not required.</param>
            /// <param name="logger">Logger for information about starting/executing the Virtual Device.</param>
            /// <param name="cancellationToken">Cancellation token that can be cancelled to abort the application.</param>
            /// <returns>Indicates whether the execution of the Virtual Device was successful and did not result in an error.</returns>
            Task<bool> RunAssembliesAsync(
                IEnumerable<AssemblyMetadata> assemblies,
                string localCLRInstanceFilePath,
                LoggingLevel logging,
                string reportPrefix,
                Action<string> processOutput,
                LogMessenger logger,
                CancellationToken cancellationToken);
        }
        #endregion

        #region To be implemented by derived classes
        /// <summary>
        /// Add test results to the collection of test results.
        /// </summary>
        /// <param name="results">New test results to add.</param>
        /// <param name="executedOnDeviceName">Name of the device used to run the test; is <c>null</c> if the test has not be executed at all.</param>
        protected abstract void AddTestResults(IEnumerable<TestResult> results, string executedOnDeviceName);
        #endregion

        #region Default implementation may be overridden in tests of this class
        /// <summary>
        /// Find all available real hardware devices. This method should return after all available devices have been found.
        /// </summary>
        /// <param name="excludeSerialPorts">Serial ports to exclude.</param>
        /// <param name="deviceFound">Method to call when a device is found.</param>
        protected virtual async Task DiscoverAllRealHardware(IEnumerable<string> excludeSerialPorts, Action<IRealHardwareDevice> deviceFound)
        {
            await RealHardwareDeviceHelper.GetAllAvailable(excludeSerialPorts, deviceFound, Logger);
        }

        /// <summary>
        /// Find all available real hardware devices. This method should return after all available devices have been found.
        /// </summary>
        /// <param name="serialPorts">Serial ports to investigate.</param>
        /// <param name="deviceFound">Method to call when a device is found.</param>
        protected virtual async Task DiscoverSelectedRealHardware(IEnumerable<string> serialPorts, Action<IRealHardwareDevice> deviceFound)
        {
            await RealHardwareDeviceHelper.GetForSelectedPorts(serialPorts, deviceFound, Logger);
        }

        /// <summary>
        /// Create a new instance of a Virtual Device.
        /// </summary>
        /// <param name="configuration">Configuration of the virtual device.</param>
        /// <param name="logger">Logger to pass process information to the caller.</param>
        /// <returns>A new instance of the Virtual Device.</returns>
        protected virtual IVirtualDevice CreateVirtualDevice(TestFrameworkConfiguration configuration, LogMessenger logger)
        {
            return new NanoCLRHelper(configuration, logger);
        }
        #endregion

        #region Orchestration implementation

        #region Common for all devices
        /// <summary>
        /// Logger to record messages of the test platform generated during the execution of unit tests
        /// </summary>
        internal interface ITestsExecutionLogger
        {
            #region Properties
            /// <summary>
            /// Get the name of the device
            /// </summary>
            string DeviceName
            {
                get;
            }

            /// <summary>
            /// Indicate whether an error has been logged
            /// </summary>
            bool HasErrors
            {
                get;
            }

            /// <summary>
            /// Get the logged messages
            /// </summary>
            IReadOnlyList<string> LogMessages
            {
                get;
            }
            #endregion

            #region Methods
            /// <summary>
            /// Log a message
            /// </summary>
            /// <param name="level"></param>
            /// <param name="message"></param>
            void Log(LoggingLevel level, string message);
            #endregion
        }

        /// <summary>
        /// Find the test framework configurations per test assembly and handle the test cases
        /// that won't be run because of the configuration. Also determines which serial ports
        /// are allowed and have to be found.
        /// </summary>
        private void FindTestFrameworkConfigurationsAndSerialPorts()
        {
            #region Real hardware test cases
            _allowAllSerialPorts = false;

            foreach (KeyValuePair<TestCaseSelection, RealHardwareExecution> realHardware in _realHardwareExecution.ToList())
            {
                var logger = realHardware.Value as ITestsExecutionLogger;

                string projectFilePath = ProjectSourceInventory.FindProjectFilePath(realHardware.Key.AssemblyFilePath, Logger);
                if (projectFilePath is null)
                {
                    _realHardwareExecution.Remove(realHardware.Key);

                    logger.Log(LoggingLevel.Error, "Test is not executed as the project directory for the test assembly could not be determined.");

                    RunAsync(() =>
                        AddTestResults(null,
                                       from tc in realHardware.Key.TestCases
                                       select new TestResult(tc.testCase, tc.selectionIndex, null)
                                       {
                                           Outcome = TestResult.TestOutcome.Skipped
                                       },
                                       logger)
                    );
                    continue;
                }
                realHardware.Value.ProjectDirectoryPath = Path.GetDirectoryName(projectFilePath);
                realHardware.Value.Configuration = TestFrameworkConfiguration.Read(realHardware.Value.ProjectDirectoryPath, false, Logger);

                if (!realHardware.Value.Configuration.AllowRealHardware)
                {
                    _realHardwareExecution.Remove(realHardware.Key);

                    logger.Log(LoggingLevel.Error, "Test is not executed as the test framework configuration does not allow running tests on real hardware.");

                    RunAsync(() =>
                        AddTestResults(null,
                                       from tc in realHardware.Key.TestCases
                                       select new TestResult(tc.testCase, tc.selectionIndex, null)
                                       {
                                           Outcome = TestResult.TestOutcome.Skipped
                                       },
                                       logger)
                    );
                }
                else
                {
                    if (realHardware.Value.Configuration.AllowSerialPorts.Count > 0)
                    {
                        _allowedSerialPorts.UnionWith(realHardware.Value.Configuration.AllowSerialPorts);
                    }
                    else
                    {
                        _allowAllSerialPorts = true;
                    }
                    if (realHardware.Value.Configuration.ExcludeSerialPorts.Count > 0)
                    {
                        _excludedSerialPorts.UnionWith(realHardware.Value.Configuration.ExcludeSerialPorts);
                    }
                }
            }

            _excludedSerialPorts.ExceptWith(_allowedSerialPorts);
            if (_allowAllSerialPorts)
            {
                _allowedSerialPorts.Clear();
            }

            foreach (ITestsExecutionLogger logger in _realHardwareExecution.Values)
            {
                if (_allowAllSerialPorts)
                {
                    if (_excludedSerialPorts.Count == 0)
                    {
                        logger.Log(LoggingLevel.Verbose, "Execute tests on all available real hardware nanoDevices");
                    }
                    else
                    {
                        logger.Log(LoggingLevel.Verbose, $"Execute tests on all available real hardware nanoDevices not connected to {string.Join(", ", _excludedSerialPorts)}");
                    }
                }
                else
                {
                    logger.Log(LoggingLevel.Verbose, $"Execute tests on real hardware nanoDevices connected to {string.Join(", ", _allowedSerialPorts)} (if available)");
                }
            }
            #endregion

            #region Virtual device test cases
            int logicalProcessors = Environment.ProcessorCount;
            MaxVirtualDevices = -1;

            foreach (KeyValuePair<TestCaseSelection, VirtualDeviceExecution> virtualDevice in _virtualDeviceExecution.ToList())
            {
                var logger = virtualDevice.Value as ITestsExecutionLogger;

                string projectFilePath = ProjectSourceInventory.FindProjectFilePath(virtualDevice.Key.AssemblyFilePath, Logger);
                if (projectFilePath is null)
                {
                    _virtualDeviceExecution.Remove(virtualDevice.Key);

                    logger.Log(LoggingLevel.Error, "Test is not executed as the project directory for the test assembly could not be determined.");

                    RunAsync(() =>
                        AddTestResults(null,
                                       from tc in virtualDevice.Key.TestCases
                                       select new TestResult(tc.testCase, tc.selectionIndex, null)
                                       {
                                           Outcome = TestResult.TestOutcome.Skipped
                                       },
                                       logger)
                    );
                    continue;
                }
                virtualDevice.Value.ProjectDirectoryPath = Path.GetDirectoryName(projectFilePath);
                virtualDevice.Value.Configuration = TestFrameworkConfiguration.Read(virtualDevice.Value.ProjectDirectoryPath, false, Logger);

                if (!(virtualDevice.Value.Configuration.PathToLocalNanoCLR is null))
                {
                    if (!File.Exists(virtualDevice.Value.Configuration.PathToLocalNanoCLR))
                    {
                        logger.Log(LoggingLevel.Error, $"Test is not executed as '{nameof(virtualDevice.Value.Configuration.PathToLocalNanoCLR)}' is not found: '{virtualDevice.Value.Configuration.PathToLocalNanoCLR}'");
                    }
                }
                if (!(virtualDevice.Value.Configuration.PathToLocalCLRInstance is null))
                {
                    if (!File.Exists(virtualDevice.Value.Configuration.PathToLocalCLRInstance))
                    {
                        logger.Log(LoggingLevel.Error, $"Test is not executed as '{nameof(virtualDevice.Value.Configuration.PathToLocalCLRInstance)}' is not found: '{virtualDevice.Value.Configuration.PathToLocalCLRInstance}'");
                    }
                }

                if (logger.HasErrors)
                {
                    _virtualDeviceExecution.Remove(virtualDevice.Key);

                    RunAsync(() =>
                        AddTestResults(null,
                                       from tc in virtualDevice.Key.TestCases
                                       select new TestResult(tc.testCase, tc.selectionIndex, null)
                                       {
                                           Outcome = TestResult.TestOutcome.Skipped
                                       },
                                       logger)
                    );
                }
                else if (virtualDevice.Value.Configuration.MaxVirtualDevices.HasValue)
                {
                    int maxDevices = virtualDevice.Value.Configuration.MaxVirtualDevices.Value == 0 ? logicalProcessors : virtualDevice.Value.Configuration.MaxVirtualDevices.Value;
                    if (MaxVirtualDevices == -1 || maxDevices < MaxVirtualDevices)
                    {
                        MaxVirtualDevices = maxDevices;
                    }
                }
            }
            if (MaxVirtualDevices < 0)
            {
                MaxVirtualDevices = logicalProcessors;
            }
            if (MaxVirtualDevices > _virtualDeviceExecution.Count)
            {
                MaxVirtualDevices = _virtualDeviceExecution.Count;
            }
            foreach (ITestsExecutionLogger logger in _virtualDeviceExecution.Values)
            {
                logger.Log(LoggingLevel.Detailed, $"Execute tests on at most {MaxVirtualDevices} Virtual nanoDevices in parallel.");
            }
            #endregion
        }

        /// <summary>
        /// Generate the unit test launcher and initialize the device
        /// </summary>
        /// <param name="selection">Selection of tests to run on the device.</param>
        /// <param name="deploymentConfiguration">Deployment configuration for the tests.</param>
        /// <param name="projectDirectoryPath">The path to the project directory corresponding to the test assembly.</param>
        /// <param name="initializeDevice">Method to initialize the device.</param>
        /// <param name="logger">Logger for the execution of the unit tests on this device.</param>
        /// <returns>The generated application, or <c>null</c> if the tests cannot be run on the device.</returns>
        private UnitTestLauncherGenerator.Application InitializeUnitTestLauncherAndDevice(
            TestCaseSelection selection,
            DeploymentConfiguration deploymentConfiguration,
            string projectDirectoryPath,
            string serialPort,
            Action<LogMessenger> initializeDevice,
            ITestsExecutionLogger logger)
        {
            string applicationAssemblyDirectoryPath = Path.Combine(
                projectDirectoryPath,
                "obj",
                "nF",
                PathHelper.GetRelativePath(projectDirectoryPath, Path.GetDirectoryName(selection.AssemblyFilePath)),
                serialPort ?? "VD");

            UnitTestLauncherGenerator.Application application = null;
            try
            {
                var generator = new UnitTestLauncherGenerator(selection, deploymentConfiguration, false, logger.Log);
                application = generator.GenerateAsApplication(applicationAssemblyDirectoryPath, logger.Log);

                initializeDevice?.Invoke(logger.Log);
            }
            catch (Exception ex)
            {
                logger.Log(LoggingLevel.Error, $"An unexpected error prevented the execution of the test: {ex.Message}");
            }

            if (logger.HasErrors || application is null)
            {
                logger.Log(LoggingLevel.Error, $"Test is not executed as the {logger.DeviceName} could not be initialized.");
                RunAsync(() =>
                        AddTestResults(application,
                                       from tc in selection.TestCases
                                       select new TestResult(tc.testCase, tc.selectionIndex, null)
                                       {
                                           Outcome = TestResult.TestOutcome.Skipped
                                       },
                                       logger)
                    );
                application = null;
            }
            else
            {
                foreach (AssemblyMetadata assembly in application.Assemblies)
                {
                    logger.Log(LoggingLevel.Detailed, $"Deploying assembly {Path.GetFileNameWithoutExtension(assembly.NanoFrameworkAssemblyFilePath)} version {assembly.Version}{(assembly.NativeVersion is null ? "" : $", requires native assembly version {assembly.NativeVersion}")}.");
                }
            }
            return application;
        }

        /// <summary>
        /// Helper to parse the output from the device and properly handle timeouts and cancel requests.
        /// </summary>
        /// <param name="selection">Selection of tests to run on the device.</param>
        /// <param name="application">Unit test launcher application.</param>
        /// <param name="serialPort">COM port the device is connected to.</param>
        /// <param name="timeout">Timeout for running tests on the device.</param>
        /// <param name="runTests">Method to run the tests on the device.</param>
        /// <param name="logger">Logger for the execution of the unit tests on this device.</param>
        private void ParseOutputAndHandleCancellation(
            TestCaseSelection selection, UnitTestLauncherGenerator.Application application,
            string serialPort, int? timeout,
            Action<string, Action<string>, CancellationToken> runTests,
            ITestsExecutionLogger logger)
        {
            // Cancellation of the device execution is either a timeout, or signalled by the output processor
            CancellationTokenSource cancelExecutionOnDevice = timeout.HasValue
                ? new CancellationTokenSource(timeout.Value)
                : new CancellationTokenSource();

            string reportPrefix = Guid.NewGuid().ToString("N");
            var outputProcessor = new UnitTestsOutputParser(
                    selection,
                    serialPort,
                    reportPrefix,
                    (testResults) => AddTestResults(application, testResults, logger),
                    () => cancelExecutionOnDevice.Cancel(), // Device can stop running now
                    CancellationToken
               );

            runTests(reportPrefix, (o) => outputProcessor.AddOutput(o), cancelExecutionOnDevice.Token);

            if (CancellationToken.IsCancellationRequested)
            {
                logger.Log(LoggingLevel.Verbose, $"Execution of tests from '{selection.AssemblyFilePath}' on the {logger.DeviceName} was cancelled on request.");
                outputProcessor.Flush();
            }
            else if (cancelExecutionOnDevice.IsCancellationRequested)
            {
                logger.Log(LoggingLevel.Verbose, $"Execution of the unit tests on the {logger.DeviceName} was aborted after {timeout} ms.");
                outputProcessor.Flush(true);
            }
            else
            {
                outputProcessor.Flush();
            }
        }

        /// <summary>
        /// Add test results to the collection of test results.
        /// </summary>
        /// <param name="application">Application created to run the tests. Pass <c>null</c> if the application has not yet been created.</param>
        /// <param name="testResults">New test results to add.</param>
        /// <param name="logger">Logger for the execution of the unit tests on this device.</param>
        private void AddTestResults(UnitTestLauncherGenerator.Application application, IEnumerable<TestResult> testResults, ITestsExecutionLogger logger)
        {
            if (!(application is null))
            {
                foreach (TestResult testResult in testResults)
                {
                    if (application.MissingDeploymentConfigurationKeys.TryGetValue(testResult.TestCase, out HashSet<string> missingKeys))
                    {
                        if ((testResult._messages ??= new List<string>()).Count > 0)
                        {
                            testResult._messages.Add(string.Empty);
                        }
                        testResult._messages.Add("*** Deployment configuration ***");
                        testResult._messages.Add($@"No data available for keys: {string.Join(", ", from k in missingKeys
                                                                                                   orderby k
                                                                                                   select $"'{k}'")}");
                    }
                }
            }
            if (logger.LogMessages.Count > 0)
            {
                foreach (TestResult testResult in testResults)
                {
                    if ((testResult._messages ??= new List<string>()).Count > 0)
                    {
                        testResult._messages.Add(string.Empty);
                    }
                    testResult._messages.Add("*** Device initialization ***");
                    testResult._messages.AddRange(logger.LogMessages);
                }
            }

            lock (_testCasesWithResult)
            {
                _testCasesWithResult.UnionWith(from tr in testResults
                                               select tr.TestCase);
            }

            AddTestResults(testResults, logger.DeviceName);
        }

        /// <summary>
        /// Helper to run code asynchronously. Need to keep a reference to the
        /// task to be able to wait for all (still) running tasks to complete.
        /// </summary>
        /// <param name="toDo">Code to execute asynchronously</param>
        private void RunAsync(Action toDo)
        {
            lock (_runningTasks)
            {
                int taskIndex = ++_nextRunningTaskIndex;
                var task = Task.Run(() =>
                {
                    try
                    {
                        toDo();
                    }
                    catch (Exception ex)
                    {
                        Logger.Invoke(LoggingLevel.Detailed, $"Unexpected error while executing unit tests: {ex.Message}");
                    }
                    lock (_runningTasks)
                    {
                        _runningTasks.Remove(taskIndex);
                    }
                });
                _runningTasks[taskIndex] = task;
            }
        }

        /// <summary>
        /// Wait for all tasks that have been started by <see cref="RunAsync"/> to finish.
        /// </summary>
        private void WaitForAsyncTasks()
        {
            while (true)
            {
                Task[] runningTasks;
                lock (_runningTasks)
                {
                    runningTasks = _runningTasks.Values.ToArray();
                }
                if (runningTasks.Length == 0)
                {
                    break;
                }
                Task.WaitAll(runningTasks);
                // New tasks may have been started
            }
        }
        #endregion

        #region Execute tests on real hardware
        /// <summary>
        /// Context for a test assembly with tests that should be run on real hardware
        /// </summary>
        private sealed class RealHardwareExecution : ITestsExecutionLogger
        {
            #region Fields
            private bool _hasErrors;
            private readonly List<string> _logMessages = new List<string>();
            #endregion

            #region Properties
            /// <summary>
            /// Get or set the path to the project directory corresponding to the test assembly.
            /// </summary>
            internal string ProjectDirectoryPath { get; set; }

            /// <summary>
            /// Get or set the test framework configuration for the test cases
            /// </summary>
            internal TestFrameworkConfiguration Configuration
            {
                get; set;
            }

            /// <summary>
            /// Get or set the deployment configuration for a serial port
            /// </summary>
            internal Dictionary<string, RealHardwareExecutionOnDevice> OnDevice
            {
                get;
            } = new Dictionary<string, RealHardwareExecutionOnDevice>();
            #endregion

            #region ITestsExecutionLogger implementation
            /// <inheritdoc/>
            string ITestsExecutionLogger.DeviceName
                => null;

            /// <inheritdoc/>
            bool ITestsExecutionLogger.HasErrors
                => _hasErrors;

            /// <inheritdoc/>
            IReadOnlyList<string> ITestsExecutionLogger.LogMessages
                => _logMessages;

            /// <inheritdoc/>
            void ITestsExecutionLogger.Log(LoggingLevel level, string message)
            {
                if (level >= LoggingLevel.Error)
                {
                    _hasErrors = true;
                }
                if (level >= Configuration.Logging)
                {
                    if (level >= LoggingLevel.Warning)
                    {
                        _logMessages.Add($"{level}: {message}");
                    }
                    else
                    {
                        _logMessages.Add(message);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Context for a test assembly with tests that should be run on an available real hardware device.
        /// </summary>
        private sealed class RealHardwareExecutionOnDevice : ITestsExecutionLogger
        {
            #region Fields
            private readonly RealHardwareExecution _parent;
            private readonly string _deviceName;
            private bool _hasErrors;
            private readonly List<string> _logMessages = new List<string>();
            #endregion

            #region Construction
            internal RealHardwareExecutionOnDevice(RealHardwareExecution parent, string serialPort)
            {
                _parent = parent;
                _hasErrors = (parent as ITestsExecutionLogger).HasErrors;
                _logMessages.AddRange((parent as ITestsExecutionLogger).LogMessages);
                _deviceName = $"nanoDevice connected to {serialPort}";
            }
            #endregion

            #region Properties
            internal enum ExecutionStage
            {
                /// <summary>Under investigation: which tests can be run on the device?</summary>
                Investigating,
                /// <summary>Tests are being executed.</summary>
                Running,
                /// <summary>Tests have been executed / no tests need to be executed.</summary>
                Done
            }
            /// <summary>
            /// Stage of the execution on the <see cref="Device"/>
            /// </summary>
            internal ExecutionStage Stage
            {
                get; set;
            } = ExecutionStage.Investigating;

            /// <summary>
            /// Get the deployment configuration for the device.
            /// </summary>
            internal DeploymentConfiguration DeploymentConfiguration
            {
                get; set;
            }

            /// <summary>
            /// Get or set the device (with deployment information) that represents the
            /// execution environment for the tests
            /// </summary>
            internal TestDeviceProxy Device
            {
                get; set;
            }

            /// <summary>
            /// Cached result of the <see cref="TestOnRealHardwareProxy.ShouldTestOnDevice(TestDeviceProxy)"/>
            /// method for this device, per device selector.
            /// </summary>
            internal Dictionary<TestOnRealHardwareProxy, bool> ShouldRunOnDevice
            {
                get;
            } = new Dictionary<TestOnRealHardwareProxy, bool>();

            /// <summary>
            /// The test case selection filtered for execution by the <see cref="Device"/>.
            /// </summary>
            internal TestCaseSelection FilteredSelection
            {
                get; set;
            }
            #endregion

            #region ITestsExecutionLogger implementation
            /// <inheritdoc/>
            string ITestsExecutionLogger.DeviceName
                => _deviceName;

            /// <inheritdoc/>
            bool ITestsExecutionLogger.HasErrors
                => _hasErrors;

            /// <inheritdoc/>
            IReadOnlyList<string> ITestsExecutionLogger.LogMessages
                => _logMessages;

            /// <inheritdoc/>
            void ITestsExecutionLogger.Log(LoggingLevel level, string message)
            {
                if (level >= LoggingLevel.Error)
                {
                    _hasErrors = true;
                }
                if (level >= _parent.Configuration.Logging)
                {
                    if (level >= LoggingLevel.Warning)
                    {
                        _logMessages.Add($"{level}: {message}");
                    }
                    else
                    {
                        _logMessages.Add(message);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Called just after a device has been discovered. Run the tests that should be run on this device.
        /// </summary>
        /// <param name="device">Device to run the tests on.</param>
        private void RunTestsOnRealHardware(IRealHardwareDevice device)
        {
            while (true)
            {
                bool waitForInvestigations = false;

                foreach (KeyValuePair<TestCaseSelection, RealHardwareExecution> selection in _realHardwareExecution)
                {
                    RealHardwareExecutionOnDevice context = null;

                    #region Can this test assembly be investigated and executed?
                    lock (selection.Value.OnDevice)
                    {
                        // We don't want the same assembly under investigation for two devices,
                        // as at some point the data for both devices may be needed.
                        // Instead of pausing at that point, it is easier to skip the test assembly
                        // and start with the next.
                        if ((from c in selection.Value.OnDevice
                             where c.Value.Stage == RealHardwareExecutionOnDevice.ExecutionStage.Investigating
                             select c).Any())
                        {
                            waitForInvestigations = true;
                            continue;
                        };
                        if (selection.Value.OnDevice.ContainsKey(device.SerialPort))
                        {
                            // Already run in a previous iteration
                            continue;
                        }
                        selection.Value.OnDevice[device.SerialPort] = context = new RealHardwareExecutionOnDevice(selection.Value, device.SerialPort)
                        {
                            FilteredSelection = new TestCaseSelection(selection.Key.AssemblyFilePath)
                        };
                    }
                    #endregion

                    SelectTestsToRun(selection.Key, context, device);

                    if (context.FilteredSelection.TestCases.Count > 0)
                    {
                        lock (selection.Value.OnDevice)
                        {
                            context.Stage = RealHardwareExecutionOnDevice.ExecutionStage.Running;
                        }
                        RunTestsOnRealHardwareDevice(selection.Key, context, device);
                    }

                    lock (selection.Value.OnDevice)
                    {
                        context.Stage = RealHardwareExecutionOnDevice.ExecutionStage.Done;
                    }
                }

                if (waitForInvestigations)
                {
                    Task.Delay(100).GetAwaiter().GetResult();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Select the tests that are allowed to run on the device (configuration), should be run on the device and,
        /// if the test is also selected for another device, for which the <paramref name="device"/> is sufficiently
        /// different from the other device.
        /// </summary>
        /// <param name="selection">Selection of test cases that might be run on the device.</param>
        /// <param name="context">Device-related information for the test assembly.</param>
        /// <param name="device">Device to run the tests on.</param>
        private void SelectTestsToRun(TestCaseSelection selection, RealHardwareExecutionOnDevice context, IRealHardwareDevice device)
        {
            TestFrameworkConfiguration configuration = _realHardwareExecution[selection].Configuration;

            #region Is it allowed to run the tests on the device?
            if (configuration.ExcludeSerialPorts.Contains(device.SerialPort) && !configuration.AllowSerialPorts.Contains(device.SerialPort))
            {
                (context as ITestsExecutionLogger).Log(LoggingLevel.Detailed, $"A nanoDevice is connected to {device.SerialPort}, but that serial port is excluded in the configuration.");

                lock (_realHardwareExecution[selection].OnDevice)
                {
                    context.Stage = RealHardwareExecutionOnDevice.ExecutionStage.Done;
                }
                return;
            }
            #endregion

            #region This device
            string deploymentConfigurationFilePath = configuration.DeploymentConfigurationFilePath(device.SerialPort);
            context.DeploymentConfiguration = DeploymentConfiguration.Parse(deploymentConfigurationFilePath);
            if (!string.IsNullOrEmpty(deploymentConfigurationFilePath) && context.DeploymentConfiguration is null)
            {
                (context as ITestsExecutionLogger).Log(LoggingLevel.Warning, $"The deployment configuration file is not found: '{deploymentConfigurationFilePath}'.");
            }

            context.Device = new TestDeviceProxy(
                    new TestDevice(
                        device.Target,
                        device.Platform,
                        context.DeploymentConfiguration
                    )
                );
            #endregion

            #region Ínformation on other devices
            List<RealHardwareExecutionOnDevice> otherDeviceContexts;
            lock (_realHardwareExecution[selection].OnDevice)
            {
                otherDeviceContexts = (from c in _realHardwareExecution[selection].OnDevice.Values
                                       where c.Stage > RealHardwareExecutionOnDevice.ExecutionStage.Investigating
                                       select c).ToList();
            }
            #endregion

            var testsNotSelected = new List<TestResult>();

            foreach ((int selectionIndex, TestCase testCase) testCase in selection.TestCases)
            {
                #region Should the test case be run on the device?
                bool shouldRunTest = false;
                var messages = new List<string>();
                bool errorInAttributeEvaluation = false;

                foreach (TestOnRealHardwareProxy selector in testCase.testCase.RealHardwareDeviceSelectors)
                {
                    if (!context.ShouldRunOnDevice.TryGetValue(selector, out bool shouldRun))
                    {
                        try
                        {
                            context.ShouldRunOnDevice[selector] = shouldRun = selector.ShouldTestOnDevice(context.Device);
                        }
                        catch (Exception ex)
                        {
                            errorInAttributeEvaluation = true;
                            if (!(selector.Source is null))
                            {
                                messages.Add($"{selector.Source.ForMessage()}: Error: Cannot evaluate '{nameof(ITestOnRealHardware.ShouldTestOnDevice)}': {ex.Message}");
                            }
                        }
                    }
                    if (shouldRun)
                    {
                        shouldRunTest = true;
                    }
                }

                if (errorInAttributeEvaluation)
                {
                    // Go fix that first
                    messages.Add($"Test is skipped as a call to '{nameof(ITestOnRealHardware.ShouldTestOnDevice)}' fails for some of the attributes that implement '{nameof(ITestOnRealHardware)}'.");
                    testsNotSelected.Add(new TestResult(testCase.testCase, testCase.selectionIndex, device.SerialPort)
                    {
                        ErrorMessage = "Real hardware test selection failed",
                        Outcome = TestResult.TestOutcome.Skipped,
                        _messages = messages
                    });
                }
                else if (!shouldRunTest)
                {
                    if (configuration.Logging == LoggingLevel.Detailed)
                    {
                        messages.Add($"Test is skipped on this device as none of the attributes that implement '{nameof(ITestOnRealHardware)}' return true for '{nameof(ITestOnRealHardware.ShouldTestOnDevice)}'.");
                        testsNotSelected.Add(new TestResult(testCase.testCase, testCase.selectionIndex, device.SerialPort)
                        {
                            ErrorMessage = "Real hardware device not suitable",
                            Outcome = TestResult.TestOutcome.Skipped,
                            _messages = messages
                        });
                    }
                    continue;
                }
                #endregion

                #region Check whether this device is sufficiently different from another device 
                bool devicesAreEqual = false;
                foreach (RealHardwareExecutionOnDevice otherContext in otherDeviceContexts)
                {
                    if ((from tc in otherContext.FilteredSelection.TestCases
                         where tc.testCase == testCase.testCase
                         select tc).Any())
                    {
                        // The test is also running/has been run on another device
                        // Is this device different?
                        devicesAreEqual = true;
                        var areDevicesEqual = new Dictionary<TestOnRealHardwareProxy, bool>();

                        foreach (TestOnRealHardwareProxy selector in testCase.testCase.RealHardwareDeviceSelectors)
                        {
                            if (context.ShouldRunOnDevice[selector] && otherContext.ShouldRunOnDevice[selector])
                            {
                                // This selector says the test should run on both devices
                                // Are the two devices the same kind of device?
                                if (!areDevicesEqual.TryGetValue(selector, out bool areEqual))
                                {
                                    try
                                    {
                                        areDevicesEqual[selector] = areEqual = selector.AreDevicesEqual(otherContext.Device, context.Device);
                                    }
                                    catch (Exception ex)
                                    {
                                        errorInAttributeEvaluation = true;
                                        if (!(selector.Source is null))
                                        {
                                            messages.Add($"{selector.Source.ForMessage()}: Error: Cannot evaluate '{nameof(ITestOnRealHardware.AreDevicesEqual)}': {ex.Message}");
                                        }
                                    }
                                }
                                if (!areEqual)
                                {
                                    devicesAreEqual = false;
                                    break;
                                }
                            }
                        }
                        if (!devicesAreEqual)
                        {
                            break;
                        }
                    }
                }

                if (errorInAttributeEvaluation)
                {
                    // Go fix that first
                    messages.Add($"Test is skipped as a call to '{nameof(ITestOnRealHardware.AreDevicesEqual)}' fails for some of the attributes that implement '{nameof(ITestOnRealHardware)}'.");
                    testsNotSelected.Add(new TestResult(testCase.testCase, testCase.selectionIndex, device.SerialPort)
                    {
                        ErrorMessage = "Real hardware test selection failed",
                        Outcome = TestResult.TestOutcome.Skipped,
                        _messages = messages
                    });
                }
                else if (devicesAreEqual)
                {
                    if (configuration.Logging == LoggingLevel.Detailed)
                    {
                        // No need to run on this device as well
                        messages.Add($"Test is skipped on this device as it has already been selected to run on an equivalent device.");
                        testsNotSelected.Add(new TestResult(testCase.testCase, testCase.selectionIndex, device.SerialPort)
                        {
                            ErrorMessage = "Already executed on an equivalent device",
                            Outcome = TestResult.TestOutcome.Skipped,
                            _messages = messages
                        });
                    }
                    continue;
                }
                #endregion

                context.FilteredSelection._testCases.Add(testCase);
            }

            if (testsNotSelected.Count > 0)
            {
                AddTestResults(null, testsNotSelected, context);
            }
        }

        private void RunTestsOnRealHardwareDevice(TestCaseSelection selection, RealHardwareExecutionOnDevice context, IRealHardwareDevice device)
        {
            TestFrameworkConfiguration configuration = _realHardwareExecution[selection].Configuration;

            UnitTestLauncherGenerator.Application application = InitializeUnitTestLauncherAndDevice
                (
                    context.FilteredSelection,
                    context.DeploymentConfiguration,
                    _realHardwareExecution[selection].ProjectDirectoryPath,
                    device.SerialPort,
                    // Do not split the device initialisation and assembly running.
                    // We want to get exclusive access to the device, so all has to be done in a single method.
                    null,
                    context
                );
            if (application is null)
            {
                return;
            }

            ParseOutputAndHandleCancellation(
                context.FilteredSelection,
                application,
                device.SerialPort,
                configuration.RealHardwareTimeout,
                (reportPrefix, parser, ct) => device.RunAssembliesAsync(
                                                application.Assemblies,
                                                reportPrefix,
                                                parser,
                                                Logger,
                                                ct
                                            )
                                            .GetAwaiter().GetResult(),
                context
            );
        }

        /// <summary>
        /// Send test results for tests that have not been executed because of
        /// no suitable real hardware device has been found.
        /// </summary>
        private void AddResultsForRealHardwareTestsNotExecuted()
        {
            foreach (KeyValuePair<TestCaseSelection, RealHardwareExecution> selection in _realHardwareExecution)
            {
                var notRun = new List<TestResult>();

                foreach ((int selectionIndex, TestCase testCase) in selection.Key.TestCases)
                {
                    if (!_testCasesWithResult.Contains(testCase))
                    {
                        notRun.Add(new TestResult(testCase, selectionIndex, null)
                        {
                            ErrorMessage = "No suitable hardware available"
                        });
                    }
                }

                if (notRun.Count > 0)
                {
                    AddTestResults(null, notRun, selection.Value);
                }
            }
        }
        #endregion

        #region Execute tests on a virtual device
        private sealed class VirtualDeviceExecution : ITestsExecutionLogger
        {
            #region Fields
            private bool _hasErrors;
            private readonly List<string> _logMessages = new List<string>();
            #endregion

            #region Properties
            /// <summary>
            /// Get or set the path to the project directory corresponding to the test assembly.
            /// </summary>
            internal string ProjectDirectoryPath { get; set; }

            /// <summary>
            /// Get or set the test framework configuration for the test cases
            /// </summary>
            internal TestFrameworkConfiguration Configuration { get; set; }
            #endregion

            #region ITestsExecutionLogger implementation
            /// <inheritdoc/>
            string ITestsExecutionLogger.DeviceName
                => "Virtual nanoDevice";

            /// <inheritdoc/>
            bool ITestsExecutionLogger.HasErrors
                => _hasErrors;

            /// <inheritdoc/>
            IReadOnlyList<string> ITestsExecutionLogger.LogMessages
                => _logMessages;

            /// <inheritdoc/>
            void ITestsExecutionLogger.Log(LoggingLevel level, string message)
            {
                if (level >= LoggingLevel.Error)
                {
                    _hasErrors = true;
                }
                if (level >= Configuration.Logging)
                {
                    if (level >= LoggingLevel.Warning)
                    {
                        _logMessages.Add($"{level}: {message}");
                    }
                    else
                    {
                        _logMessages.Add(message);
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// Start as many virtual devices as necessary and as allowed.
        /// </summary>
        private void RunTestsOnVirtualDevices()
        {
            if (_virtualDeviceExecution.Count == 0)
            {
                return;
            }
            int numberOfRunningVirtualDevices = 0;
            var selections = _virtualDeviceExecution.Keys.ToList();
            int selectionIndex = 0;

            void StartNewVirtualDevices()
            {
                while (true)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    TestCaseSelection selection;
                    lock (_virtualDeviceExecution)
                    {
                        if (selectionIndex >= selections.Count)
                        {
                            return;
                        }
                        if (numberOfRunningVirtualDevices > MaxVirtualDevices)
                        {
                            return;
                        }
                        selection = selections[selectionIndex++];
                        numberOfRunningVirtualDevices++;
                    }
                    RunAsync(() =>
                    {
                        if (CancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        try
                        {
                            RunTestOnVirtualDevice(selection);
                        }
                        finally
                        {
                            lock (_virtualDeviceExecution)
                            {
                                numberOfRunningVirtualDevices--;
                            }
                            StartNewVirtualDevices();
                        }
                    });
                }
            }
            StartNewVirtualDevices();
        }

        /// <summary>
        /// Execute a single test assembly on a virtual device
        /// </summary>
        /// <param name="selection">Selection of tests to run on the device.</param>
        private void RunTestOnVirtualDevice(TestCaseSelection selection)
        {
            TestFrameworkConfiguration configuration = _virtualDeviceExecution[selection].Configuration;
            IVirtualDevice virtualDevice = null;

            UnitTestLauncherGenerator.Application application = InitializeUnitTestLauncherAndDevice
                (
                    selection,
                    null,
                    _virtualDeviceExecution[selection].ProjectDirectoryPath,
                    null,
                    (l) => virtualDevice = CreateVirtualDevice(configuration, l),
                    _virtualDeviceExecution[selection]
                );
            if (application is null)
            {
                return;
            }

            ParseOutputAndHandleCancellation(
                selection,
                application,
                null,
                configuration.VirtualDeviceTimeout,
                (reportPrefix, parser, ct) => virtualDevice.RunAssembliesAsync(
                                                    application.Assemblies,
                                                    configuration.PathToLocalCLRInstance,
                                                    configuration.Logging,
                                                    reportPrefix,
                                                    parser,
                                                    Logger,
                                                    ct
                                                )
                                                .GetAwaiter().GetResult(),
                _virtualDeviceExecution[selection]
            );
        }
        #endregion

        #endregion
    }
}
