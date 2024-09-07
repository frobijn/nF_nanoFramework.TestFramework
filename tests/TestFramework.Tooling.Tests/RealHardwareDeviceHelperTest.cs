// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework;
using nanoFramework.TestFramework.Tooling;
using TestFramework.Tooling.Tests.Helpers;

namespace TestFramework.Tooling.Tests
{
    [TestClass]
    [TestCategory("Test execution")]
    public sealed class RealHardwareDeviceHelperTest
    {
        #region Device detection
        [TestMethod]
        public void RealHardwareDevice_DeviceNotPresent()
        {
            string notConnectedPort = RealHardwareSerialPorts.GetSerialPortNames(0, 1).First();

            var logger = new LogMessengerMock();
            var actual = new List<RealHardwareDeviceHelper>();
            void AddDevice(RealHardwareDeviceHelper device)
            {
                lock (actual)
                {
                    actual.Add(device);
                }
            }

            RealHardwareDeviceHelper.GetForSelectedPorts(new string[] { notConnectedPort }, AddDevice, logger)
                .GetAwaiter().GetResult();

            logger.AssertEqual("", LoggingLevel.Error);
            Assert.AreEqual(0, actual.Count);
        }

        [TestMethod]
        [TestCategory(Constants.RealHardware_TestCategory)]
        [DoNotParallelize]
        public void RealHardwareDevice_DeviceByPort()
        {
            string realHardwarePort = RealHardwareSerialPorts.GetSerialPortNames(1, 0).First();

            var logger = new LogMessengerMock();
            var actual = new List<RealHardwareDeviceHelper>();
            void AddDevice(RealHardwareDeviceHelper device)
            {
                lock (actual)
                {
                    actual.Add(device);
                }
            }
            RealHardwareDeviceHelper.GetForSelectedPorts(new string[] { realHardwarePort }, AddDevice, logger)
                .GetAwaiter().GetResult();

            logger.AssertEqual("", LoggingLevel.Error);
            Assert.AreEqual(1, actual.Count);
            Assert.AreEqual(realHardwarePort, actual[0].SerialPort);
        }

        [TestMethod]
        [TestCategory(Constants.RealHardware_TestCategory)]
        [DoNotParallelize]
        public void RealHardwareDevice_AllAvailable()
        {
            var realHardwarePorts = RealHardwareSerialPorts.GetAllSerialPortNames().ToList();
            if (realHardwarePorts.Count == 0)
            {
                Assert.Inconclusive($"This test requires that one or more {Constants.RealHardware_Description}s are connected");
            }

            var logger = new LogMessengerMock();
            var actual = new List<RealHardwareDeviceHelper>();
            void AddDevice(RealHardwareDeviceHelper device)
            {
                lock (actual)
                {
                    actual.Add(device);
                }
            }
            RealHardwareDeviceHelper.GetAllAvailable(RealHardwareSerialPorts.ExcludeSerialPorts, AddDevice, logger)
                .GetAwaiter().GetResult();

            logger.AssertEqual("", LoggingLevel.Error);
            Assert.IsNotNull(actual);
            Assert.AreEqual(
                string.Join(";", from p in realHardwarePorts
                                 orderby p
                                 select p),
                string.Join(";", from d in actual
                                 orderby d.SerialPort
                                 select d.SerialPort)
            );
        }
        #endregion

        #region Exclusive access
        [TestMethod]
        public void RealHardwareDevice_ExclusiveAccess()
        {
            var logger = new LogMessengerMock();

            #region Exclusive access codes
            int numTasks = 0;
            int taskRunning = -1;
            void ActualTest()
            {
                var logMessenger = (LogMessenger)logger;
                int taskIndex;
                lock (logger)
                {
                    taskIndex = numTasks++;
                }
                while (numTasks < 5)
                {
                }
                bool result = RealHardwareDeviceHelper.CommunicateWithDevice("COM_Test", () =>
                {
                    int isRunning = taskRunning;
                    taskRunning = taskIndex;
                    lock (logger)
                    {
                        if (isRunning >= 0)
                        {
                            logMessenger(LoggingLevel.Error, $"Task {taskIndex} started running before task {isRunning} was completed");
                        }
                        else
                        {
                            logMessenger(LoggingLevel.Detailed, $"Task {taskIndex} started");
                        }
                    }
                    Thread.Sleep(100);
                    taskRunning = -1;
                    lock (logger)
                    {
                        logMessenger(LoggingLevel.Detailed, $"Task {taskIndex} finished");
                    }
                });
                if (!result)
                {
                    lock (logger)
                    {
                        logMessenger(LoggingLevel.Error, $"Task {taskIndex}: CommunicateWithDevice returns false");
                    }
                }
            }
            #endregion

            Task.WaitAll(
                Task.Run(ActualTest),
                Task.Run(ActualTest),
                Task.Run(ActualTest),
                Task.Run(ActualTest),
                Task.Run(ActualTest)
            );
            logger.AssertEqual("", LoggingLevel.Error);
        }
        #endregion


    }
}
