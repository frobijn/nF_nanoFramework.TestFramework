// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using nanoFramework.TestFramework;

namespace NFUnitTest
{
    [TestClass]
    public class SkipTestClass
    {
        [Setup]
        public void SetupMethodToSkip()
        {
            Debug.WriteLine("Skipping all the other methods");
            Assert.SkipTest("None of the other methods should be tested, they should all be skipped.");
        }

        [TestMethod]
        public void TestMothdWhichShouldSkip()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void TestMothdWhichShouldSkip2()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void TestMothdWhichShouldSkip3()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void TestMothdWhichShouldSkip4()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void TestMothdWhichShouldSkip5()
        {
            // Method intentionally left empty.
        }

        [TestMethod]
        public void TestMothdWhichShouldSkip6()
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
