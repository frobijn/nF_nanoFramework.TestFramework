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
        }

        [Setup]
        public void Setup2()
        {
            OutputHelper.WriteLine($"Setup method of {GetType().FullName}");
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
    public class SkippedInTest : IDisposable
    {
        public SkippedInTest()
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
        }

        [Cleanup]
        public void Cleanup2()
        {
            OutputHelper.WriteLine($"Cleanup2 method of {GetType().FullName}");
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

    [TestClass]
    public class SkippedInConstructor : IDisposable
    {
        public SkippedInConstructor()
        {
            OutputHelper.WriteLine($"Constructor of {GetType().FullName}");
            Assert.SkipTest("Skip all tests in the test class");
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
    public class SkippedInSetup : IDisposable
    {
        public SkippedInSetup()
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
            Assert.SkipTest("Skip all tests in the test class");
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
    public class FailInFirstSetup : IDisposable
    {
        public FailInFirstSetup()
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
            Assert.Fail();
        }

        [Setup]
        public void Setup2()
        {
            OutputHelper.WriteLine($"Setup2 method of {GetType().FullName}");
            Assert.Fail("Second setup should not be run!");
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
    public class FailInFirstCleanUp : IDisposable
    {
        public FailInFirstCleanUp()
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

        [Cleanup]
        public void Cleanup2()
        {
            OutputHelper.WriteLine($"Cleanup2 method of {GetType().FullName}");
            Assert.Fail("Second cleanup should not be run!");
        }
    }
}
