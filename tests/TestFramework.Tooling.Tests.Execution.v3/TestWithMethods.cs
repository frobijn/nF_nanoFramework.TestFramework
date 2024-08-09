// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    /// <summary>
    /// This class uses only the current TestFramework attributes but is
    /// analysed as a new-style test as this project uses new test attributes.
    /// </summary>
    [TestClass]
    public class TestWithMethods
    {
        [TestMethod]
        [DataRow(42)]
        [DataRow(123)]
        public void Test1(int testValue)
        {
            Thread.Sleep(1000);
            OutputHelper.WriteLine($"Test1({testValue})");
        }

        [TestMethod]
        public void Test2()
        {
            Thread.Sleep(1000);
            OutputHelper.WriteLine($"Test2()");
        }
    }
}
