// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Orchestration of the test cases selected to be executed:
    /// which test cases should be run on which available device?
    /// The selection of test cases is paired with the corresponding <see cref="TestFrameworkConfiguration"/>.
    /// 
    /// </summary>
    public abstract class TestCaseExecutionOrchestration
    {
        #region Fields
        private readonly Dictionary<TestCaseSelection, RealHardwareExecution> _realHardwareExecution = new Dictionary<TestCaseSelection, RealHardwareExecution>();
        private readonly Dictionary<TestCaseSelection, VirtualDeviceExecution> _virtualDeviceExecution = new Dictionary<TestCaseSelection, VirtualDeviceExecution>();
        private bool _allowAllSerialPorts;
        private readonly HashSet<string> _allowedSerialPorts = new HashSet<string>();
        private readonly HashSet<string> _excludedSerialPorts = new HashSet<string>();
        private int _maxVirtualDevices;
        private int _nextRunningTaskIndex;
        private readonly Dictionary<int, Task> _runningTasks = new Dictionary<int, Task>();
        #endregion

        #region Construction
        /// <summary>
        /// Create a new orchestrator.
        /// </summary>
        /// <param name="selection">Selection of test cases to execute.</param>
        /// <param name="logger">Logger to provide process information to the caller.</param>
        /// <param name="cancellationToken">Cancellation token that indicates whether the execution of tests should be aborted (gracefully).</param>
        protected TestCaseExecutionOrchestration(TestCaseCollection selection, LogMessenger logger, CancellationToken cancellationToken)
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

            // Executing tests on virtual devices
            RunTestsOnVirtualDevices();

            // Wait until all devices have been discovered.
            discoverDevices?.Wait();

            // Wait until all asynchronous tasks have been completed
            WaitForAsyncTasks();
        }

        /// <summary>
        /// Functionality required from the description of a real hardware device.
        /// Presented as an interface to be able to test this class without using
        /// actual hardware (<see cref="RealHardwareDeviceHelper"/>).
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
        }
        #endregion

        #region To be implemented by derived classes
        /// <summary>
        /// Add test results to the collection of test results.
        /// </summary>
        /// <param name="results">New test results to add.</param>
        /// <param name="computerName">Name of the device used to run the test; is <c>null</c> if the test has not be executed at all.</param>
        protected abstract void AddTestResults(IEnumerable<TestResult> results, string deviceName);
        #endregion

        #region Default implementation may be overridden in tests of this class
        /// <summary>
        /// Find all available real hardware devices. This method should return after all available devices have been found.
        /// </summary>
        /// <param name="excludeSerialPorts">Serial ports to exclude.</param>
        /// <param name="deviceFound">Method to call when a device is found.</param>
        protected virtual Task DiscoverAllRealHardware(IEnumerable<string> excludeSerialPorts, Action<IRealHardwareDevice> deviceFound)
        {
            return RealHardwareDeviceHelper.GetAllAvailable(excludeSerialPorts, deviceFound, Logger);
        }

        /// <summary>
        /// Find all available real hardware devices. This method should return after all available devices have been found.
        /// </summary>
        /// <param name="serialPorts">Serial ports to investigate.</param>
        /// <param name="deviceFound">Method to call when a device is found.</param>
        protected virtual Task DiscoverSelectedRealHardware(IEnumerable<string> serialPorts, Action<IRealHardwareDevice> deviceFound)
        {
            return RealHardwareDeviceHelper.GetForSelectedPorts(serialPorts, deviceFound, Logger);
        }

        /// <summary>
        /// Run the tests in the selection on a virtual device.
        /// </summary>
        /// <param name="selection">Tests to run.</param>
        /// <param name="configuration">Matching test framework configuration.</param>
        protected virtual void RunTestOnVirtualDevice(TestCaseSelection selection, TestFrameworkConfiguration configuration)
        {
            RunTestOnVirtualDevice(selection);
        }
        #endregion

        #region Orchestration implementation
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
                realHardware.Value.Configuration = TestFrameworkConfiguration.Read(Path.GetDirectoryName(realHardware.Key.AssemblyFilePath), false, Logger);

                if (!realHardware.Value.Configuration.AllowRealHardware)
                {
                    _realHardwareExecution.Remove(realHardware.Key);

                    RunAsync(() =>
                        AddTestResults(from tc in realHardware.Key.TestCases
                                       select new TestResult(tc.testCase, tc.selectionIndex, null)
                                       {
                                           Outcome = TestResult.TestOutcome.Skipped,
                                           _messages = new List<string>()
                                               {
                                                   "Test is not executed as the test framework configuration does not allow running tests on real hardware."
                                               }
                                       },
                                       null)
                    );
                    Logger.Invoke(LoggingLevel.Warning, $"Tests from '{realHardware.Key.AssemblyFilePath}' are not executed on real hardware as the test framework configuration does not allow that.");
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
            #endregion

            #region Virtual device test cases
            int logicalProcessors = Environment.ProcessorCount;
            _maxVirtualDevices = -1;

            foreach (KeyValuePair<TestCaseSelection, VirtualDeviceExecution> virtualDevice in _virtualDeviceExecution.ToList())
            {
                virtualDevice.Value.Configuration = TestFrameworkConfiguration.Read(Path.GetDirectoryName(virtualDevice.Key.AssemblyFilePath), false, Logger);

                var messages = new List<string>();

                if (!(virtualDevice.Value.Configuration.PathToLocalNanoCLR is null))
                {
                    if (!File.Exists(virtualDevice.Value.Configuration.PathToLocalNanoCLR))
                    {
                        messages.Add($"Test is not executed as '{nameof(virtualDevice.Value.Configuration.PathToLocalNanoCLR)}' is not found: '{virtualDevice.Value.Configuration.PathToLocalNanoCLR}'");
                    }
                }
                if (!(virtualDevice.Value.Configuration.PathToLocalCLRInstance is null))
                {
                    if (!File.Exists(virtualDevice.Value.Configuration.PathToLocalCLRInstance))
                    {
                        messages.Add($"Test is not executed as '{nameof(virtualDevice.Value.Configuration.PathToLocalCLRInstance)}' is not found: '{virtualDevice.Value.Configuration.PathToLocalCLRInstance}'");
                    }
                }

                if (messages.Count > 0)
                {
                    _virtualDeviceExecution.Remove(virtualDevice.Key);

                    RunAsync(() =>
                        AddTestResults(from tc in virtualDevice.Key.TestCases
                                       select new TestResult(tc.testCase, tc.selectionIndex, null)
                                       {
                                           Outcome = TestResult.TestOutcome.Skipped,
                                           _messages = messages
                                       },
                                       null)
                    );
                    Logger.Invoke(LoggingLevel.Warning, $"Tests from '{virtualDevice.Key.AssemblyFilePath}' are not executed on a virtual device as the test framework configuration has errors.");
                }
                else if (virtualDevice.Value.Configuration.MaxVirtualDevices.HasValue)
                {
                    int maxDevices = virtualDevice.Value.Configuration.MaxVirtualDevices.Value == 0 ? logicalProcessors : virtualDevice.Value.Configuration.MaxVirtualDevices.Value;
                    if (_maxVirtualDevices == -1 || maxDevices < _maxVirtualDevices)
                    {
                        _maxVirtualDevices = maxDevices;
                    }
                }
            }
            if (_maxVirtualDevices < 0)
            {
                _maxVirtualDevices = logicalProcessors;
            }
            #endregion
        }

        #region Execute tests on real hardware
        private sealed class RealHardwareExecution
        {
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
            internal Dictionary<string, DeploymentConfiguration> DeploymentConfiguration
            {
                get;
            } = new Dictionary<string, DeploymentConfiguration>();
        }

        private void RunTestsOnRealHardware(IRealHardwareDevice device)
        {
            // TODO: RunTestsOnRealHardware
            throw new NotImplementedException();
        }
        #endregion

        #region Execute tests on a virtual device
        private sealed class VirtualDeviceExecution
        {
            /// <summary>
            /// Get or set the test framework configuration for the test cases
            /// </summary>
            internal TestFrameworkConfiguration Configuration { get; set; }
        }

        /// <summary>
        /// Start as many virtual devices as necessary and as allowed.
        /// </summary>
        private void RunTestsOnVirtualDevices()
        {
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
                        if (selectionIndex > selections.Count)
                        {
                            return;
                        }
                        if (numberOfRunningVirtualDevices > _maxVirtualDevices)
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
        /// <param name="selection"></param>
        private void RunTestOnVirtualDevice(TestCaseSelection selection)
        {
            #region Initialize the virtual device and generate the unit test launcher
            var messages = new List<string>();
            var hasErrors = false;
            void LogToTestResults(LoggingLevel level, string message)
            {
                if (level >= LoggingLevel.Warning)
                {
                    messages.Add($"{level}: {message}");
                    if (level >= LoggingLevel.Error)
                    {
                        hasErrors = true;
                    }
                }
            }

            var configuration = _virtualDeviceExecution[selection].Configuration;
            UnitTestLauncherGenerator.Application application = null;
            NanoCLRHelper nanoClr = null;
            try
            {
                var generator = new UnitTestLauncherGenerator(selection, null, false, LogToTestResults);
                application = generator.GenerateAsApplication(Path.GetDirectoryName(selection.AssemblyFilePath), LogToTestResults);

                nanoClr = new NanoCLRHelper(configuration, LogToTestResults);
            }
            catch (Exception ex)
            {
                LogToTestResults(LoggingLevel.Error, $"An unexpected error prevented the execution of the test: {ex.Message}");
            }

            if (hasErrors || application is null)
            {
                LogToTestResults(LoggingLevel.Error, "Test is not executed as the virtual device could not be initialized.");
                RunAsync(() =>
                        AddTestResults(from tc in selection.TestCases
                                       select new TestResult(tc.testCase, tc.selectionIndex, null)
                                       {
                                           Outcome = TestResult.TestOutcome.Skipped,
                                           _messages = messages
                                       },
                                       null)
                    );
                Logger.Invoke(LoggingLevel.Warning, $"Tests from '{selection.AssemblyFilePath}' are not executed on the virtual device as the virtual device could not be initialized.");
            }
            #endregion

            #region Run the tests in the assembly and process the results
            // Cancellation of the virtual device execution is either a timeout, or signalled by the output processor
            var cancelNanoClr = configuration.VirtualDeviceTimeout.HasValue
                ? new CancellationTokenSource(configuration.VirtualDeviceTimeout.Value)
                : new CancellationTokenSource();

            var outputProcessor = new UnitTestsOutputParser(
                    selection,
                    null,
                    Guid.NewGuid().ToString("N"),
                    (testResults) => AddTestResults(testResults, "Virtual nanoDevice"),
                    () => cancelNanoClr.Cancel(), // Abort request honoured, nanoClr can now also stop
                    CancellationToken
               );

            nanoClr.RunAssembliesAsync(
                application.Assemblies,
                configuration.PathToLocalCLRInstance,
                configuration.Logging,
                (o) => outputProcessor.AddOutput(o),
                Logger,
                cancelNanoClr.Token)
                .GetAwaiter().GetResult();

            if (CancellationToken.IsCancellationRequested)
            {
                Logger.Invoke(LoggingLevel.Warning, $"Execution of tests from '{selection.AssemblyFilePath}' on Virtual nanoDevice cancelled on request.");
                // No outputProcessor.Flush() required; outputProcessor has already sent all results
            }
            else if (cancelNanoClr.IsCancellationRequested)
            {
                // Execution of the unit tests has timed out; do not flush the output processor
                // as it is not clear whether all information about the tests in the current test
                // class has been passed to the output processor.
            }
            else
            {
                outputProcessor.Flush();
            }
            #endregion
        }
        #endregion

        #region Helpers
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

        #endregion
    }
}
