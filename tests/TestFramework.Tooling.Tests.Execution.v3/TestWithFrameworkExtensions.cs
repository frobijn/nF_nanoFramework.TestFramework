// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;
using TestFramework.Tooling.Execution.Tests.TestFrameworkExtensions;

namespace TestFramework.Tooling.Execution.Tests
{
    [TestClass]
    public class TestWithFrameworkExtensions
    {
        [TestOnDeviceWithSomeFile(@"xyzzy")]
        public void TestDeviceWithSomeFile()
        {
        }
    }
}
