// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Execution.Tests
{
    [TestClass]
    public class FailInConstructor : IDisposable
    {
        public FailInConstructor()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
            // Should be reported as inconclusive / test not run
            Assert.Fail();
        }

        public void Dispose()
        {
            OutputHelper.WriteLine($"Dispose of {GetType().FullName}");
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
    public class FailInSetup : IDisposable
    {
        public FailInSetup()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        public void Dispose()
        {
            OutputHelper.WriteLine($"Dispose of {GetType().FullName}");
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
    public class FailInTest : IDisposable
    {
        public FailInTest()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        public void Dispose()
        {
            OutputHelper.WriteLine($"Dispose of {GetType().FullName}");
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
    public class InconclusiveInTest : IDisposable
    {
        public InconclusiveInTest()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        public void Dispose()
        {
            OutputHelper.WriteLine($"Dispose of {GetType().FullName}");
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
            Assert.SkipTest();
        }

        [Cleanup]
        public void Cleanup()
        {
            OutputHelper.WriteLine($"Cleanup method of {GetType().FullName}");
        }
    }

    [TestClass]
    public class CleanupFailedInTest : IDisposable
    {
        public CleanupFailedInTest()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        public void Dispose()
        {
            OutputHelper.WriteLine($"Dispose of {GetType().FullName}");
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
    public class FailInCleanUp : IDisposable
    {
        public FailInCleanUp()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        public void Dispose()
        {
            OutputHelper.WriteLine($"Dispose of {GetType().FullName}");
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
            // Should be reported as CleanupFailed
            Assert.Fail();
        }
    }

    [TestClass]
    public class FailInDispose : IDisposable
    {
        public FailInDispose()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        public void Dispose()
        {
            OutputHelper.WriteLine($"Dispose of {GetType().FullName}");
            // Should be reported as CleanupFailed
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
    public class NonFailingTest : IDisposable
    {
        public NonFailingTest()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
        }

        public void Dispose()
        {
            OutputHelper.WriteLine($"Dispose of {GetType().FullName}");
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
}
