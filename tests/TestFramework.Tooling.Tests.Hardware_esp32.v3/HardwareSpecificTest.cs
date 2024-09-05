// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.Hardware.Esp32;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Hardware_esp32.Tests
{
    /// <summary>
    /// Test class that uses an ESP32-specific class library.
    /// </summary>
    [TestClass]
    [TestOnPlatform("ESP32")]
    public class HardwareSpecificTest
    {
        [TestMethod]
        [TestCategory("Hardware required")]
        public void UseEsp32NativeAssembly()
        {
            OutputHelper.WriteLine($"Use ESP32 native assembly");
            Configuration.GetFunctionPin(DeviceFunction.I2C2_DATA);
        }
    }
}
