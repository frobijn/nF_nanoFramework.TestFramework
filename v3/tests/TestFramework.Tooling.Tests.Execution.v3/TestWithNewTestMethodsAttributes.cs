// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Execution.Tests
{
    [TestClass]
    public class TestWithNewTestMethodsAttributes
    {
        [TestMethod]
        [TestCategory("Example category")]
        [TestCategory("Other category")]
        public void MethodWithCategories()
        {
        }

        [TestOnPlatform("ESP32")]
        [DeploymentConfiguration("RGB LED pin")]
        public void MethodToRunOnRealHardware(int ledPin)
        {
            OutputHelper.WriteLine($"Test method runs on ESP32 only; ledPin = {ledPin}");
            Assert.AreNotEqual(-1, ledPin);
        }

        [TestOnPlatform("ESP32")]
        [DeploymentConfiguration("RGB LED pin")]
        [DataRow((int)123)]
        public void MethodToRunOnRealHardwareWithData(long ledPin, int data)
        {
            OutputHelper.WriteLine($"[DataRow] runs on ESP32 only; ledPin = {ledPin}, data = {data}");
            Assert.AreNotEqual(-1L, ledPin);
            Assert.AreEqual(123, data);
        }
    }
}
