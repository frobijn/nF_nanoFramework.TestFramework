// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using nanoFramework.TestFramework;

namespace NFUnitTest
{
    [TestClass]
    [TestCategory("Asserts")]
    public class SkipTestClass
    {
        [Setup]
        public void SetupMethodToSkip()
        {
            Debug.WriteLine("Skipping all the other methods");
            Assert.SkipTest("None of the other methods should be tested, they should all be skipped.");
        }

        [TestMethod]
        public void TestMethodWhichShouldSkip()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void TestMethodWhichShouldSkip2()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void TestMethodWhichShouldSkip3()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void TestMethodWhichShouldSkip4()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void TestMethodWhichShouldSkip5()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void TestMethodWhichShouldSkip6()
        {
            // Method intentionally left empty.
        }

        [Cleanup]
        public void CleanUpMethodSkip()
        {
            // Method intentionally left empty.
        }
    }
}
