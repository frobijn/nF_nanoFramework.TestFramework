// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nanoFramework.Tools.Debugger;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Helper to het information on the available real hardware devices
    /// and to run unit tests on the devices.
    /// </summary>
    public sealed class RealHardwareDeviceHelper : TestCaseExecutionOrchestration.IRealHardwareDevice
    {
        #region Fields
        private readonly NanoDeviceBase _device;

        /// <summary>
        ///  Base name for the system-wide mutex that controls access to a device connected to a COM port.
        /// </summary>
        private const string MutexBaseName = "276545121198496AADD346A60F14EF8D_";
        #endregion

        #region Construction
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
        /// <param name="logger">Method to pass process information to the caller.</param>
        /// <param name="deviceFound">Method to receive a real hardware device that is found.
        /// The method may be called simultaneously from different threads.</param>
        /// <param name="serialPorts">The serial ports that are expected to be connected to a real hardware device,
        /// or not connected at all.</param>
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
        private static void AddSelectedPort(Action<RealHardwareDeviceHelper> deviceFound, string serialPort, bool expectNanoDevice, LogMessenger logger)
        {
            if (!(logger is null))
            {
                lock (logger)
                {
                    logger(LoggingLevel.Verbose, $"Checking for real hardware device on port {serialPort}.");
                }
            }
            var serialDebugClient = PortBase.CreateInstanceForSerial(false);
            try
            {
                serialDebugClient.AddDevice(serialPort);

                // The NanoFrameworkDevices collection is a static collection and there is no way to gain exclusive access.
                // Hope for the best...
                List<NanoDeviceBase> discoveredDevices;
                while (true)
                {
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
                                    logger(LoggingLevel.Verbose, $"The device connected to '{serialPort}' is a virtual device and not real hardware.");
                                }
                                else
                                {
                                    logger(LoggingLevel.Verbose, $"The device connected to '{serialPort}' is not recognized as a real hardware device.");
                                }
                            }
                            else
                            {
                                if (isVirtualDevice)
                                {
                                    logger(LoggingLevel.Detailed, $"Virtual device and not real hardware connected to {serialPort}.");
                                }
                                else
                                {
                                    logger(LoggingLevel.Detailed, $"Couldn't find a valid nanoDevice connected to {serialPort}.");
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
                            logger(LoggingLevel.Verbose, $"Real hardware device with target '{device.TargetName}' connected to {serialPort}.");
                        }
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
                            logger(LoggingLevel.Detailed, $"Couldn't find a valid nanoDevice connected to {serialPort}.");
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
        /// Communicate with this device and ensure the code to be executed as exclusive access to the device.
        /// At this moment it only protects against other test platform code, as other parts of the nanoFramework
        /// do not yet use this protection.
        /// </summary>
        /// <param name="communication">Code to execute while having exclusive access to the device</param>
        /// <param name="millisecondsTimeout">Maximum time in milliseconds to wait for exclusive access</param>
        /// <returns>Indicates whether the <paramref name="communication"/> has been executed. Returns <c>false</c> if exclusive access
        /// cannot be obtained within <paramref name="millisecondsTimeout"/>.</returns>
        public bool CommunicateWithDevice(Action communication, int millisecondsTimeout = Timeout.Infinite)
        {
            return CommunicateWithDevice(SerialPort, communication, millisecondsTimeout);
        }

        /// <summary>
        /// Communicate with a device connected to the serial port and ensure the code to be executed as exclusive access to the device.
        /// At this moment it only protects against other test platform code, as other parts of the nanoFramework
        /// do not yet use this protection.
        /// </summary>
        /// <param name="serialPort">Serial port the device is connected to.</param>
        /// <param name="communication">Code to execute while having exclusive access to the device</param>
        /// <param name="millisecondsTimeout">Maximum time in milliseconds to wait for exclusive access</param>
        /// <returns>Indicates whether the <paramref name="communication"/> has been executed. Returns <c>false</c> if exclusive access
        /// cannot be obtained within <paramref name="millisecondsTimeout"/>.</returns>
        public static bool CommunicateWithDevice(string serialPort, Action communication, int millisecondsTimeout = Timeout.Infinite)
        {
            var mutex = new Mutex(false, $"{MutexBaseName}_{serialPort}");
            try
            {
                if (mutex.WaitOne(millisecondsTimeout))
                {
                    communication();
                    return true;
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            return false;
        }
        #endregion
    }
}
