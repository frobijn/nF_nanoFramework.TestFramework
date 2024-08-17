// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    [TestClass]
    [Trait("Test class with v3 attributes")]
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
        public void MethodWithNewTestMethods(string ledPin)
        {
            Assert.IsNotNull(ledPin);
        }
    }
}
