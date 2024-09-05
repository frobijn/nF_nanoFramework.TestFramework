// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Execution.Tests
{
    [TestClass()]
    public static class StaticTestClass
    {
        [TestMethod]
        [TestCategory("TestClass demonstration")]
        public static void Method1()
        {
            OutputHelper.WriteLine("Method1");
        }

        [TestMethod]
        [TestCategory("TestClass demonstration")]
        public static void Method2()
        {
            OutputHelper.WriteLine("Method2");
        }

        [Setup]
        public static void Setup()
        {
            OutputHelper.WriteLine("Setup");
        }

        [Cleanup]
        public static void Cleanup()
        {
            OutputHelper.WriteLine("Cleanup");
        }
    }

    [TestClass(true)]
    public static class StaticTestClassSetupCleanupPerMethod
    {
        [TestMethod]
        [TestCategory("TestClass demonstration")]
        public static void Method1()
        {
            OutputHelper.WriteLine("Method1");
        }

        [TestMethod]
        [TestCategory("TestClass demonstration")]
        public static void Method2()
        {
            OutputHelper.WriteLine("Method2");
        }

        [Setup]
        public static void Setup()
        {
            OutputHelper.WriteLine("Setup");
        }

        [Cleanup]
        public static void Cleanup()
        {
            OutputHelper.WriteLine("Cleanup");
        }
    }

    [TestClass(false)]
    public class NonStaticTestClass : IDisposable
    {
        public NonStaticTestClass()
        {
            OutputHelper.WriteLine("Constructor");
        }

        public void Dispose()
        {
            OutputHelper.WriteLine("Dispose");
        }

        [TestMethod]
        [TestCategory("TestClass demonstration")]
        public void Method1()
        {
            OutputHelper.WriteLine("Method1");
            Assert.IsNotNull(this);
        }

        [TestMethod]
        [TestCategory("TestClass demonstration")]
        public void Method2()
        {
            OutputHelper.WriteLine("Method2");
            Assert.IsNotNull(this);
        }

        [Setup]
        public void Setup()
        {
            OutputHelper.WriteLine("Setup");
            Assert.IsNotNull(this);
        }

        [Cleanup]
        public void Cleanup()
        {
            OutputHelper.WriteLine("Cleanup");
            Assert.IsNotNull(this);
        }
    }

    [TestClass(true)]
    public class NonStaticTestClassSetupCleanupPerMethod : IDisposable
    {
        public NonStaticTestClassSetupCleanupPerMethod()
        {
            OutputHelper.WriteLine("Constructor");
        }

        public void Dispose()
        {
            OutputHelper.WriteLine("Dispose");
        }

        [TestMethod]
        [TestCategory("TestClass demonstration")]
        public void Method1()
        {
            OutputHelper.WriteLine("Method1");
            Assert.IsNotNull(this);
        }

        [TestMethod]
        [TestCategory("TestClass demonstration")]
        public void Method2()
        {
            OutputHelper.WriteLine("Method2");
            Assert.IsNotNull(this);
        }

        [Setup]
        public void Setup()
        {
            OutputHelper.WriteLine("Setup");
            Assert.IsNotNull(this);
        }

        [Cleanup]
        public void Cleanup()
        {
            OutputHelper.WriteLine("Cleanup");
            Assert.IsNotNull(this);
        }
    }

    [TestClass(true, true)]
    public class NonStaticTestClassInstancePerMethod : IDisposable
    {
        public NonStaticTestClassInstancePerMethod()
        {
            OutputHelper.WriteLine("Constructor");
        }

        public void Dispose()
        {
            OutputHelper.WriteLine("Dispose");
        }

        [TestMethod]
        [TestCategory("TestClass demonstration")]
        public void Method1()
        {
            OutputHelper.WriteLine("Method1");
            Assert.IsNotNull(this);
        }

        [TestMethod]
        [TestCategory("TestClass demonstration")]
        public void Method2()
        {
            OutputHelper.WriteLine("Method2");
            Assert.IsNotNull(this);
        }

        [Setup]
        public void Setup()
        {
            OutputHelper.WriteLine("Setup");
            Assert.IsNotNull(this);
        }

        [Cleanup]
        public void Cleanup()
        {
            OutputHelper.WriteLine("Cleanup");
            Assert.IsNotNull(this);
        }
    }
}
