// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace NFUnitTest
{
    /// <summary>
    /// The tests run on the Virtual nanoDevice (see <see cref="AssemblyAttributes"/>) and on hardware.
    /// </summary>
    [TestClass]
    [TestCategory("Attributes")]
    [TestCategory("Device selection")]
    public class ParallelTest
    {
        [TestOnRealHardware(false)]
        public void RunsOnAnyDeviceOnce()
        {
            Console.WriteLine("This method runs on the Virtual nanoDevice and on one of the connected Hardware nanoDevices");
        }

        [TestOnRealHardware(true)]
        public void RunsOnceForEveryTarget()
        {
            Console.WriteLine("This method runs on the Virtual nanoDevice and on each of the connected Hardware nanoDevices that has a different target/firmware");
        }

        [TestOnPlatform("ESP32")]
        public void RunsOnVirtualAndESP32()
        {
            Console.WriteLine("This method runs on the Virtual nanoDevice and on one of the connected ESP32 Hardware nanoDevices");
        }
    }
}
