// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Execution.Tests
{
    [TestClass]
    public class TestWithNewTestMethodsAttributes
    {
        [TestMethod]
        [Trait("Example trait")]
        [Trait("Other trait")]
        public void MethodWithTraits()
        {
        }

        [TestOnPlatform("esp32")]
        [DeploymentConfiguration("RGB LED pin")]
        public void MethodToRunOnRealHardware(int ledPin)
        {
            Assert.AreNotEqual(-1, ledPin);
        }

        [TestOnPlatform("esp32")]
        [DeploymentConfiguration("RGB LED pin")]
        [DataRow((int)123)]
        public void MethodToRunOnRealHardwareWithData(long ledPin, int data)
        {
            Assert.AreNotEqual(-1, ledPin);
            Assert.AreEqual(123, data);
        }
    }
}
