// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    [TestClass]
    [TestCategory("Test class with v3 attributes")]
    public class TestWithNewTestMethodsAttributes
    {
        [TestMethod]
        [TestCategory("Example category")]
        [TestCategory("Other category")]
        public void MethodWithCategories()
        {
        }

        [TestOnPlatform("ESP32")]
        [DeploymentConfiguration("RGB LED pin", "Device ID")]
        public void MethodWithNewTestMethods(int ledPin, long ID)
        {
            Assert.IsNotNull(ledPin);
        }
    }
}
