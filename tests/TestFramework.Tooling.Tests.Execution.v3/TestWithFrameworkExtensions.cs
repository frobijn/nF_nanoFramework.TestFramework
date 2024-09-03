// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using nanoFramework.TestFramework;
using TestFramework.Tooling.Execution.Tests.TestFrameworkExtensions;
using TestFramework.Tooling.Tests.NFUnitTest.TestFrameworkExtensions;

namespace TestFramework.Tooling.Execution.Tests
{
    [TestClass]
    public class TestWithFrameworkExtensions
    {
        [DeploymentConfiguration("data.bin", "data.txt"), Setup]
        public void Setup(byte[] binary, string text)
        {
            Assert.IsNotNull(binary);
            Assert.IsFalse(string.IsNullOrEmpty(text));
        }

        [TestOnDeviceWithSomeFile(@"xyzzy")]
        public void TestDeviceWithSomeFile()
        {
            OutputHelper.WriteLine("Deployment configuration 'xyzzy' has a unique value");
            Thread.Sleep(1000); // To test cancel/timeout
        }

        [TestOnDeviceWithProgrammingError(false)]
        public void TestOnDeviceWithProgrammingError_ShouldTestOnDevice()
        {
        }

        [TestOnDeviceWithProgrammingError(true)]
        public void TestOnDeviceWithProgrammingError_AreDevicesEqual()
        {
        }
    }
}
