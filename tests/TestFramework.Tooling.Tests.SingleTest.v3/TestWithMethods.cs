// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Execution.Tests
{
    /// <summary>
    /// The project contains only a single test that should run on
    /// the virtual machine. The project is used to test the test adapter
    /// / test host components.
    /// </summary>
    [TestClass]
    [TestOnVirtualDevice]
    [Trait("Single test class")]
    public class SingleTestClass
    {
        [TestMethod]
        [DataRow(42)]
        [DataRow(123)]
        [Trait("Method with data rows")]
        public void DataRowTest(int testValue)
        {
            Thread.Sleep(300);
            OutputHelper.WriteLine($"DataRowTest({testValue})");
        }

        [TestMethod]
        [Trait("A test method")]
        public void MethodTest()
        {
            Thread.Sleep(300);
            OutputHelper.WriteLine($"MethodTest()");
        }
    }
}
