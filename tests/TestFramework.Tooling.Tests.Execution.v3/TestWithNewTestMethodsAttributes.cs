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
        public void MethodToRunOnRealHardware(string ledPin)
        {
            Assert.IsNotNull(ledPin);
        }

        [TestOnPlatform("esp32")]
        [DeploymentConfiguration("RGB LED pin")]
        [DataRow(42)]
        public void MethodToRunOnRealHardwareWithData(string ledPin, int data)
        {
            Assert.IsNotNull(ledPin);
            Assert.AreEqual(42, data);
        }
    }
}
