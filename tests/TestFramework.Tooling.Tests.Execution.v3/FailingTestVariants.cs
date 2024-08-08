// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.NFUnitTest
{
    [TestClass]
    public class FailInConstructor
    {
        public FailInConstructor()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
            // Should be reported as inconclusive / test not run
            Assert.Fail();
        }

        [Setup]
        public void Setup()
        {
            OutputHelper.WriteLine($"Setup method of {GetType().FullName}");
        }

        [TestMethod]
        public void Test()
        {
            OutputHelper.WriteLine($"Test method of {GetType().FullName}");
        }

        [Cleanup]
        public void Cleanup()
        {
            OutputHelper.WriteLine($"Cleanup method of {GetType().FullName}");
        }
    }

    [TestClass]
    public class FailInSetup
    {
        public FailInSetup()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        [Setup]
        public void Setup()
        {
            OutputHelper.WriteLine($"Setup method of {GetType().FullName}");
            // Should be reported as inconclusive / test not run
            Assert.Fail();
        }

        [TestMethod]
        public void Test()
        {
            OutputHelper.WriteLine($"Test method of {GetType().FullName}");
        }

        [Cleanup]
        public void Cleanup()
        {
            OutputHelper.WriteLine($"Cleanup method of {GetType().FullName}");
        }
    }

    [TestClass]
    public class FailInTest
    {
        public FailInTest()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        [Setup]
        public void Setup()
        {
            OutputHelper.WriteLine($"Setup method of {GetType().FullName}");
        }

        [TestMethod]
        public void Test()
        {
            OutputHelper.WriteLine($"Test method of {GetType().FullName}");
            Assert.Fail();
        }

        [Cleanup]
        public void Cleanup()
        {
            OutputHelper.WriteLine($"Cleanup method of {GetType().FullName}");
        }
    }

    [TestClass]
    public class InconclusiveInTest
    {
        public InconclusiveInTest()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        [Setup]
        public void Setup()
        {
            OutputHelper.WriteLine($"Setup method of {GetType().FullName}");
        }

        [TestMethod]
        public void Test()
        {
            OutputHelper.WriteLine($"Test method of {GetType().FullName}");
            Assert.Inconclusive();
        }

        [Cleanup]
        public void Cleanup()
        {
            OutputHelper.WriteLine($"Cleanup method of {GetType().FullName}");
        }
    }

    [TestClass]
    public class CleanupFailedInTest
    {
        public CleanupFailedInTest()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        [Setup]
        public void Setup()
        {
            OutputHelper.WriteLine($"Setup method of {GetType().FullName}");
        }

        [TestMethod]
        public void Test()
        {
            OutputHelper.WriteLine($"Test method of {GetType().FullName}");
            Assert.CleanupFailed();
        }

        [Cleanup]
        public void Cleanup()
        {
            OutputHelper.WriteLine($"Cleanup method of {GetType().FullName}");
        }
    }

    [TestClass]
    public class FailInCleanUp
    {
        public FailInCleanUp()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        [Setup]
        public void Setup()
        {
            OutputHelper.WriteLine($"Setup method of {GetType().FullName}");
        }

        [TestMethod]
        public void Test()
        {
            OutputHelper.WriteLine($"Test method of {GetType().FullName}");
        }

        [Cleanup]
        public void CleanUp()
        {
            OutputHelper.WriteLine($"Cleanup method of {GetType().FullName}");
            // Should be reported as CleanupFailed
            Assert.Fail();
        }
    }
}
