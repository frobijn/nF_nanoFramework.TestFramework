// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestFramework.Tooling.Tests
{
    /// <summary>
    /// Tests for the functionality of c involving test results.
    /// The tests use instances of the Virtual Device to actually run the tests.
    /// The real hardware devices are simulated with Virtual Devices as well.
    /// There are no tests for Virtual Device involving real hardware; use
    /// the <see cref="Tools.TestAdapterTestCasesExecutorTest"/> for that.
    /// </summary>
    [TestClass]
    public sealed class TestsRunnerExecutionTest
    {
        /// <summary>
        /// Verify that the correct selection of tests is run.  Simulated is test execution
        /// of a single test assembly on a single Virtual Device and three real hardware
        /// devices: two esp32 and one other. 
        /// </summary>
        [TestMethod]
        public void TestsRunner_TestSelection()
        {
            Assert.Inconclusive("TODO");
        }
    }
}
