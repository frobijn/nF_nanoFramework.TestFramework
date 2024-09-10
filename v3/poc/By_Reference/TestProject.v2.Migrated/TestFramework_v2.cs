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
            OutputHelper.WriteLine($"{nameof(TestFramework_v2_TestMethod)} is a test method from a v3 test assembly.");
            OutputHelper.WriteLine("The test project has been migrated from TestProject.v2 by referencing the v3 packages");
            OutputHelper.WriteLine("(simulated with direct references)");

            Assert.IsNotNull(this); // In v3; assert fails in v2.
        }
    }
}
