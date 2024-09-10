// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace TestProject.v2
{
    [TestClass]
    public class TestFramework_v2
    {
        [TestMethod]
        public void TestFramework_v2_TestMethod()
        {
            OutputHelper.WriteLine($"{nameof(TestFramework_v2_TestMethod)} is a test method from a v2 test assembly.");
            OutputHelper.WriteLine("A v2 test project can be in the same solution as a v3 test project.");

            Assert.IsNotNull(this); // Only in v2; assert fails in v3
        }
    }
}
