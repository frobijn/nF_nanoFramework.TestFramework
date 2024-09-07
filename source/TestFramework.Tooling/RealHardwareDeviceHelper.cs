// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using nanoFramework.Tools.Debugger;
using nanoFramework.Tools.Debugger.Extensions;
using nanoFramework.Tools.Debugger.WireProtocol;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Helper to het information on the available real hardware devices
    /// and to run unit tests on the devices.
    /// </summary>
    public sealed class RealHardwareDeviceHelper : TestsRunner.IRealHardwareDevice, IDisposable
    {
        #region Fields
        private readonly NanoDeviceBase _device;

        /// <summary>
        ///  Base name for the system-wide mutex that controls access to a device connected to a COM port.
        /// </summary>
        private const string MutexBaseName = "276545121198496AADD346A60F14EF8D_";
        // number of retries when performing a deploy operation
        private const int NumberOfRetries = 5;
        // timeout when performing a deploy operation
        private const int TimeoutMilliseconds = 1000;
        #endregion

        #region Construction / destruction
        /// <summary>
        /// Get all available real hardware devices.
        /// The devices are passed to a method so that the caller can start running code on that device.
        /// It may take quite some time for this method to finish, e.g., because it is waiting for timeouts
        /// for serial ports that are not responding.
        /// </summary>
        /// <param name="excludeSerialPorts">Ports to exclude from the examination.
        /// Pass <c>null</c> or an empty enumeration if none are excluded.</param>
        /// <param name="deviceFound">Method to receive a real hardware device that is found.
        /// The method may be called simultaneously from different threads.</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        public static async Task GetAllAvailable(IEnumerable<string> excludeSerialPorts, Action<RealHardwareDeviceHelper> deviceFound, LogMessenger logger)
        {
            if (deviceFound is null)
            {
                return;
            }
            var serialPorts = new HashSet<string>(System.IO.Ports.SerialPort.GetPortNames());
            if (!(excludeSerialPorts is null))
            {
                serialPorts.ExceptWith(excludeSerialPorts);
            }
            if (serialPorts.Count > 0)
            {
                await Task.WhenAll(
                    from serialPort in serialPorts
                    select Task.Run(() => CommunicateWithDevice(serialPort, () => AddSelectedPort(deviceFound, serialPort, false, logger)))
                );
            }
        }

        /// <summary>
        /// Get the real hardware devices, provided they are connected to one of the specified serial ports.
        /// The devices are passed to a method so that the caller can start running code on that device.
        /// It may take quite some time for this method to finish, e.g., because it is waiting for timeouts
        /// for serial ports that are not responding.
        /// </summary>
        /// <param name="serialPorts">The serial ports that are expected to be connected to a real hardware device,
        /// or not connected at all.</param>
        /// <param name="deviceFound">Method to receive a real hardware device that is found.
        /// The method may be called simultaneously from different threads.</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        /// <remarks>
        /// If a serial port is connected to something that is not a nanodevice, an error will be logged.
        /// </remarks>
        public static async Task GetForSelectedPorts(IEnumerable<string> serialPorts, Action<RealHardwareDeviceHelper> deviceFound, LogMessenger logger)
        {
            if (deviceFound is null)
            {
                return;
            }
            var availablePorts = new HashSet<string>(serialPorts);
            availablePorts.IntersectWith(System.IO.Ports.SerialPort.GetPortNames());
            if (availablePorts.Count > 0)
            {
                await Task.WhenAll(
                    from serialPort in availablePorts
                    select Task.Run(() => CommunicateWithDevice(serialPort, () => AddSelectedPort(deviceFound, serialPort, true, logger)))
                );
            }
        }

        /// <summary>
        /// Check whether a real hardware device is connected to the selected port. If that is the case, add it to the list.
        /// </summary>
        /// <param name="deviceFound">Method to receive a real hardware device that is found.
        /// The method may be called simultaneously from different threads.</param>
        /// <param name="serialPort">The serial port to look for a connected device.</param>
        /// <param name="expectNanoDevice">Indicates whether it is expected that a Nanodevice is connected to the serial port</param>
        /// <param name="logger">Method to pass process information to the caller.</param>
        /// <param name="cancellationToken">If the cancellation token is cancelled, the discovery of devices is aborted.</param>
        private static void AddSelectedPort(Action<RealHardwareDeviceHelper> deviceFound, string serialPort, bool expectNanoDevice, LogMessenger logger, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken?.IsCancellationRequested ?? false)
            {
                return;
            }
            if (!(logger is null))
            {
                lock (logger)
                {
                    logger(LoggingLevel.Detailed, $"Checking for {Constants.RealHardware_Description} on port {serialPort}.");
                }
            }
            var serialDebugClient = PortBase.CreateInstanceForSerial(false);
            try
            {
                if (cancellationToken?.IsCancellationRequested ?? false)
                {
                    return;
                }
                serialDebugClient.AddDevice(serialPort);

                // The NanoFrameworkDevices collection is a static collection and there is no way to gain exclusive access.
                // Hope for the best...
                List<NanoDeviceBase> discoveredDevices;
                while (true)
                {
                    if (cancellationToken?.IsCancellationRequested ?? false)
                    {
                        return;
                    }
                    try
                    {
                        discoveredDevices = serialDebugClient.NanoFrameworkDevices.ToList();
                        break;
                    }
                    catch
                    {
                    }
                }
                NanoDeviceBase device = (from d in discoveredDevices
                                         where d.ConnectionId == serialPort
                                         select d).FirstOrDefault();
                bool isVirtualDevice = device?.Platform.Equals("WINDOWS", StringComparison.OrdinalIgnoreCase) ?? false;
                if (device is null || isVirtualDevice)
                {
                    if (!(logger is null))
                    {
                        lock (logger)
                        {
                            if (expectNanoDevice)
                            {
                                if (isVirtualDevice)
                                {
                                    logger(LoggingLevel.Verbose, $"The device connected to '{serialPort}' is a {Constants.VirtualDevice_Description} and not a {Constants.RealHardware_Description}.");
                                }
                                else
                                {
                                    logger(LoggingLevel.Verbose, $"The device connected to '{serialPort}' is not recognized as a {Constants.RealHardware_Description}.");
                                }
                            }
                            else
                            {
                                if (isVirtualDevice)
                                {
                                    logger(LoggingLevel.Detailed, $"{Constants.VirtualDevice_Description} and not a {Constants.RealHardware_Description} connected to {serialPort}.");
                                }
                                else
                                {
                                    logger(LoggingLevel.Detailed, $"Couldn't find a {Constants.RealHardware_Description} connected to {serialPort}.");
                                }
                            }
                        }
                    }
                }
                else
                {
                    // all good here
                    lock (logger)
                    {
                        if (!(logger is null))
                        {
                            logger(LoggingLevel.Verbose, $"{Constants.RealHardware_Description} with target '{device.TargetName}' / platform '{device.Platform}' connected to {serialPort}.");
                        }
                    }
                    if (cancellationToken?.IsCancellationRequested ?? false)
                    {
                        return;
                    }
                    deviceFound(new RealHardwareDeviceHelper(serialPort, device));
                }
            }
            catch (Exception ex)
            {
                if (!(logger is null))
                {
                    lock (logger)
                    {
                        if (expectNanoDevice)
                        {
                            logger(LoggingLevel.Error, $"The device connected to '{serialPort}' did not respond as expected. Maybe try to disable the device watchers in Visual Studio Extension! If the situation persists reboot the device and/or disconnect and connect it again.");
                            logger(LoggingLevel.Detailed, $"An exception was thrown when trying to communicate with the device at '{serialPort}': {ex.Message}.");
                        }
                        else
                        {
                            logger(LoggingLevel.Detailed, $"Couldn't find a {Constants.RealHardware_Description} connected to {serialPort}.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create a helper for a single real hardware device
        /// </summary>
        /// <param name="device">Accessor for the device</param>
        private RealHardwareDeviceHelper(string serialPort, NanoDeviceBase device)
        {
            SerialPort = serialPort;
            _device = device;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _device.DebugEngine?.Dispose();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the serial port the device is connected to
        /// </summary>
        public string SerialPort
        {
            get;
        }

        /// <summary>
        /// Get the name of the target/firmware installed on the device
        /// </summary>
        public string Target
            => _device.TargetName;

        /// <summary>
        /// Get the platform of the device.
        /// </summary>
        public string Platform
            => _device.Platform;
        #endregion

        #region Methods
        /// <summary>
        /// Execute an application consisting of a set of assemblies on the device.
        /// </summary>
        /// <param name="assemblies">The assemblies to execute.</param>
        /// <param name="processOutput">Action to process the output that is provided in chunks.</param>
        /// <param name="logger">Logger to pass process information to the caller.</param>
        /// <param name="createRunCancellationToken">Cancellation token that should be created to end running the unit tests.
        /// It is called just before the execution of the unit tests is started.</param>
        /// <param name="cancellationToken">Cancellation token that should be cancelled to stop/abort the initialization of the device
        /// and running of the unit tests, e.g., if the processing of the output is complete or if running tests is cancelled.</param>
        /// <returns>Indicates whether the execution on the device was successful and did not result in an error.</returns>
        public Task<bool> RunAssembliesAsync(
            IEnumerable<AssemblyMetadata> assemblies,
            Action<string> processOutput,
            LogMessenger logger,
            Func<CancellationToken?> createRunCancellationToken,
            CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                bool result = false;
                CommunicateWithDevice(
                    () =>
                    {
                        result = DoRunAssembliesAsync(assemblies, processOutput, logger, createRunCancellationToken, cancellationToken).GetAwaiter().GetResult();
                    },
                    Timeout.Infinite,
                    cancellationToken
                );
                return result;
            });
        }
        Task<bool> TestsRunner.IRealHardwareDevice.RunAssembliesAsync(
            IEnumerable<AssemblyMetadata> assemblies,
            LoggingLevel logging,
            string reportPrefix,
            Action<string> processOutput,
            LogMessenger logger,
            Func<CancellationToken?> createRunCancellationToken,
            CancellationToken cancellationToken)
        {
            return RunAssembliesAsync(assemblies, processOutput, logger, createRunCancellationToken, cancellationToken);
        }

        /// <summary>
        /// Communicate with this device and ensure the code to be executed as exclusive access to the device.
        /// At this moment it only protects against other test platform code, as other parts of the nanoFramework
        /// do not yet use this protection.
        /// </summary>
        /// <param name="communication">Code to execute while having exclusive access to the device</param>
        /// <param name="millisecondsTimeout">Maximum time in milliseconds to wait for exclusive access</param>
        /// <param name="cancellationToken">Cancellation token that can be cancelled to stop/abort running the <paramref name="communication"/>.
        /// This method does not stop/abort execution of <paramref name="communication"/> after it has been started.</param>
        /// <returns>Indicates whether the <paramref name="communication"/> has been executed. Returns <c>false</c> if exclusive access
        /// cannot be obtained within <paramref name="millisecondsTimeout"/>, or if <paramref name="cancellationToken"/> has been cancelled
        /// before the <paramref name="communication"/> has been started.</returns>
        public bool CommunicateWithDevice(Action communication, int millisecondsTimeout = Timeout.Infinite, CancellationToken? cancellationToken = null)
        {
            return CommunicateWithDevice(SerialPort, communication, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Communicate with a device connected to the serial port and ensure the code to be executed as exclusive access to the device.
        /// At this moment it only protects against other test platform code, as other parts of the nanoFramework
        /// do not yet use this protection.
        /// </summary>
        /// <param name="serialPort">Serial port the device is connected to.</param>
        /// <param name="communication">Code to execute while having exclusive access to the device</param>
        /// <param name="millisecondsTimeout">Maximum time in milliseconds to wait for exclusive access</param>
        /// <param name="cancellationToken">Cancellation token that can be cancelled to stop/abort running the <paramref name="communication"/>.
        /// This method does not stop/abort execution of <paramref name="communication"/> after it has been started.</param>
        /// <returns>Indicates whether the <paramref name="communication"/> has been executed. Returns <c>false</c> if exclusive access
        /// cannot be obtained within <paramref name="millisecondsTimeout"/>, or if <paramref name="cancellationToken"/> was cancelled
        /// before the <paramref name="communication"/> has been started.</returns>
        public static bool CommunicateWithDevice(string serialPort, Action communication, int millisecondsTimeout = Timeout.Infinite, CancellationToken? cancellationToken = null)
        {
            var waitHandles = new List<WaitHandle>();
            var mutex = new Mutex(false, $"{MutexBaseName}_{serialPort}");
            waitHandles.Add(mutex);

            CancellationTokenSource timeOutToken = null;
            if (millisecondsTimeout > 0 && millisecondsTimeout != Timeout.Infinite)
            {
                timeOutToken = new CancellationTokenSource(millisecondsTimeout);
                waitHandles.Add(timeOutToken.Token.WaitHandle);
            }
            if (cancellationToken.HasValue)
            {
                waitHandles.Add(cancellationToken.Value.WaitHandle);
            }
            try
            {
                if (WaitHandle.WaitAny(waitHandles.ToArray()) == 0)
                {
                    communication();
                    return true;
                }
            }
            finally
            {
                mutex.ReleaseMutex();
                timeOutToken?.Dispose();
            }
            return false;
        }
        #endregion

        #region Internal implementation
        /// <summary>
        /// Execute an application consisting of a set of assemblies on the device.
        /// </summary>
        /// <param name="assemblies">The assemblies to execute. One of the assemblies must be a program.</param>
        /// <param name="processOutput">Action to process the output that is provided in chunks.</param>
        /// <param name="logger">Logger to pass process information to the caller.</param>
        /// <param name="createRunCancellationToken">Cancellation token that should be created to end running the unit tests.
        /// It is called just before the execution of the unit tests is started.</param>
        /// <param name="cancellationToken">Cancellation token that should be cancelled to stop/abort the initialization of the device
        /// and running of the unit tests, e.g., if the processing of the output is complete or if running tests is cancelled.</param>
        /// <returns>Indicates whether the execution on the device was successful and did not result in an error.</returns>
        private async Task<bool> DoRunAssembliesAsync(
            IEnumerable<AssemblyMetadata> assemblies,
            Action<string> processOutput,
            LogMessenger logger,
            Func<CancellationToken?> createRunCancellationToken,
            CancellationToken cancellationToken)
        {
            logger?.Invoke(LoggingLevel.Verbose, $"Start initialization of the device {_device.Description}");

            bool IsCancellationRequested()
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger?.Invoke(LoggingLevel.Verbose, "Device initialization is requested to be cancelled.");
                    return true;
                }
                return false;
            }
            if (IsCancellationRequested())
            {
                return false;
            }
            var timeKeeper = Stopwatch.StartNew();
            try
            {

                // check if debugger engine exists
                if (_device.DebugEngine == null)
                {
                    _device.CreateDebugEngine();
                    logger?.Invoke(LoggingLevel.Detailed, $"Debug engine created for {_device.Description} [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start]");
                }

                // Connect the debugger to the device
                for (int retryCount = 0; retryCount < NumberOfRetries; retryCount++)
                {
                    if (IsCancellationRequested())
                    {
                        return false;
                    }

                    bool connectResult = _device.DebugEngine.Connect(5000, true, true);
                    logger?.Invoke(LoggingLevel.Detailed, $"Device connect result is {connectResult}. Attempt {retryCount}/{NumberOfRetries}");

                    if (connectResult)
                    {
                        logger?.Invoke(LoggingLevel.Verbose, $"Connected to the device [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start].");
                        break;
                    }
                    else if (retryCount < NumberOfRetries)
                    {
                        // Give it a bit of time
                        await Task.Delay(100);
                    }
                    else
                    {
                        logger?.Invoke(LoggingLevel.Error, $"Couldn't connect to the device {_device.Description} [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start], please try to disable the device scanning in the Visual Studio Extension! If the situation persists reboot the device as well.");
                        return false;
                    }
                }

                // erase the device
                for (int retryCount = 0; retryCount < NumberOfRetries; retryCount++)
                {
                    if (IsCancellationRequested())
                    {
                        return false;
                    }

                    logger?.Invoke(LoggingLevel.Detailed, $"Erase deployment block storage. Attempt {retryCount}/{NumberOfRetries}.");
                    bool eraseResult = _device.Erase(
                        EraseOptions.Deployment,
                        null,
                        null);

                    logger?.Invoke(LoggingLevel.Detailed, $"Erase result is {eraseResult}.");

                    if (eraseResult)
                    {
                        logger?.Invoke(LoggingLevel.Verbose, $"Deployment block storage has been erased [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start].");
                        break;
                    }
                    else if (retryCount < NumberOfRetries)
                    {
                        // Give it a bit of time
                        await Task.Delay(400);
                    }
                    else
                    {
                        logger?.Invoke(LoggingLevel.Error, $"Couldn't erase the device [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start], please try to disable the device scanning in the Visual Studio Extension! If the situation persists reboot the device as well.");
                        return false;
                    }
                }

                if (IsCancellationRequested())
                {
                    return false;
                }

                // initial check 
                bool deviceIsInInitializeState = false;
                if (_device.DebugEngine.IsDeviceInInitializeState())
                {
                    logger?.Invoke(LoggingLevel.Detailed, "Device status verified as being in initialized state. Requesting to resume execution.");
                    // set flag
                    deviceIsInInitializeState = true;

                    // device is still in initialization state, try resume execution
                    _device.DebugEngine.ResumeExecution();
                }

                // handle the workflow required to try resuming the execution on the device
                // only required if device is not already there
                // retry 5 times with a 500ms interval between retries
                for (int retryCount = 0; retryCount++ < NumberOfRetries && deviceIsInInitializeState; retryCount++)
                {
                    if (IsCancellationRequested())
                    {
                        return false;
                    }

                    if (!_device.DebugEngine.IsDeviceInInitializeState())
                    {
                        // done here
                        deviceIsInInitializeState = false;
                        break;
                    }

                    // provide feedback to user on the 1st pass
                    if (retryCount == 0)
                    {
                        logger?.Invoke(LoggingLevel.Verbose, $"Waiting for device to initialize [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start].");
                    }
                    logger?.Invoke(LoggingLevel.Detailed, $"Waiting for device to report initialization completed ({retryCount}/{NumberOfRetries}).");

                    if (_device.DebugEngine.IsConnectedTonanoBooter)
                    {
                        logger?.Invoke(LoggingLevel.Detailed, $"Device reported running nanoBooter. Requesting to load nanoCLR.");
                        // request nanoBooter to load CLR
                        _device.DebugEngine.ExecuteMemory(0);
                    }
                    else if (_device.DebugEngine.IsConnectedTonanoCLR)
                    {
                        logger?.Invoke(LoggingLevel.Detailed, $"Device reported running nanoCLR. Requesting to reboot nanoCLR.");
                        Task.Run(() =>
                        {
                            // already running nanoCLR try rebooting the CLR
                            _device.DebugEngine.RebootDevice(RebootOptions.ClrOnly);
                        }).GetAwaiter().GetResult();
                    }
                    if (IsCancellationRequested())
                    {
                        return false;
                    }

                    // wait before next pass
                    // use a back-off strategy of increasing the wait time to accommodate slower or less responsive targets (such as networked ones)
                    await Task.Delay(TimeSpan.FromMilliseconds(TimeoutMilliseconds * (retryCount + 1)));

                    await Task.Yield();
                }

                // check if device is still in initialized state
                if (deviceIsInInitializeState)
                {
                    logger?.Invoke(LoggingLevel.Error, $"Failed to initialize device [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start].");
                    return false;
                }

                // device has left initialization state
                logger?.Invoke(LoggingLevel.Verbose, $"Device is initialized and ready [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start].");
                await Task.Yield();

                //////////////////////////////////////////////////////////
                // sanity check for devices without native assemblies ?!?!
                if (_device.DeviceInfo.NativeAssemblies.Count == 0)
                {
                    // there are no assemblies deployed?!
                    logger?.Invoke(LoggingLevel.Verbose, $"Device reporting no assemblies loaded [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start]. This can not happen. Sanity check failed.");
                    return false;
                }

                logger?.Invoke(LoggingLevel.Detailed, $"Computing deployment blob.");

                await Task.Yield();

                // Keep track of total assembly size
                long totalSizeOfAssemblies = 0;

                // now we will re-deploy all system assemblies
                List<byte[]> assemblyModules = new List<byte[]>();
                foreach (AssemblyMetadata peItem in assemblies)
                {
                    if (IsCancellationRequested())
                    {
                        return false;
                    }

                    // append to the deploy blob the assembly
                    using (FileStream fs = File.Open(peItem.NanoFrameworkAssemblyFilePath, FileMode.Open, FileAccess.Read))
                    {
                        long length = (fs.Length + 3) / 4 * 4;
                        logger?.Invoke(LoggingLevel.Detailed, $"Adding {Path.GetFileNameWithoutExtension(peItem.NanoFrameworkAssemblyFilePath)} v{peItem.Version} ({length} bytes) to deployment bundle");
                        byte[] buffer = new byte[length];

                        await Task.Yield();

                        await fs.ReadAsync(buffer, 0, (int)fs.Length);
                        assemblyModules.Add(buffer);

                        // Increment totalizer
                        totalSizeOfAssemblies += length;
                    }
                }

                if (IsCancellationRequested())
                {
                    return false;
                }
                logger?.Invoke(LoggingLevel.Verbose, $"Deploying {assemblies.Count():N0} assemblies to device [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start]... Total size in bytes is {totalSizeOfAssemblies}.");

                var deploymentLog = new StringBuilder();
                var deploymentLogger = new Progress<string>((m) =>
                {
                    logger?.Invoke(LoggingLevel.Detailed, m);
                    deploymentLog.Append(m);
                });

                // OK to skip erase as we just did that
                // no need to reboot device
                if (!_device.DebugEngine.DeploymentExecute(
                        new List<byte[]>(assemblyModules),
                        false,
                        false,
                        null,
                        deploymentLogger
                    ))
                {
                    if (IsCancellationRequested())
                    {
                        return false;
                    }
                    // if the first attempt fails, give it another try

                    // wait before next pass
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    await Task.Yield();

                    logger?.Invoke(LoggingLevel.Detailed, "Deploying assemblies. Second attempt.");
                    deploymentLog.Clear();

                    // can't skip erase as we just did that
                    // no need to reboot device
                    if (!_device.DebugEngine.DeploymentExecute(
                            new List<byte[]>(assemblyModules),
                            false,
                            false,
                            null,
                            deploymentLogger
                        ))
                    {
                        logger?.Invoke(LoggingLevel.Error, $"Deployment failed [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start]:\n{deploymentLog}");
                        return false;
                    }
                }
            }
            finally
            {
                timeKeeper.Stop();
            }
            logger?.Invoke(LoggingLevel.Verbose, $"Deployment completed [{(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms since start].");

            // Also write this to the output, as verbose logging is not added to the tests result by default.
            processOutput($"Device initialization and deployment completed in {(timeKeeper.ElapsedMilliseconds < 1 ? "< 1" : $"{timeKeeper.ElapsedMilliseconds}")} ms.\n");

            await Task.Yield();

            if (IsCancellationRequested())
            {
                return false;
            }

#if DEBUG
            var allOutput = new StringBuilder();
#endif
            void ReceiveOutput(IncomingMessage message, string text)
            {
#if DEBUG
                lock (allOutput)
                {
                    allOutput.Append(text);
                }
#endif
                processOutput($"{text}");
            }

            // attach listener for messages
            _device.DebugEngine.OnMessage += ReceiveOutput;
            try
            {
                _device.DebugEngine.RebootDevice(RebootOptions.ClrOnly);

                // Wait until completion
                var waitHandles = new List<WaitHandle>()
                {
                    cancellationToken.WaitHandle
                };
                CancellationToken? cancelRun = createRunCancellationToken();
                if (!(cancelRun is null))
                {
                    waitHandles.Add(cancelRun.Value.WaitHandle);
                }
                WaitHandle.WaitAny(waitHandles.ToArray());
            }
            finally
            {
                // Detach listener
                _device.DebugEngine.OnMessage -= ReceiveOutput;
            }
            return true;
        }
        #endregion
    }
}
